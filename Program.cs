using CircuitBreakerDemo.CircuitBreaker;
using CircuitBreakerDemo.Services;

namespace CircuitBreakerDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         Circuit Breaker Pattern Demo Application            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Create circuit breaker with custom configuration
        var config = new CircuitBreakerConfig
        {
            Name = "ExternalApiCircuitBreaker",
            FailureThreshold = 3,      // Open after 3 consecutive failures
            SuccessThreshold = 2,       // Close after 2 consecutive successes in half-open
            OpenTimeout = TimeSpan.FromSeconds(5)  // Try half-open after 5 seconds
        };

        using var circuitBreaker = new CircuitBreaker.CircuitBreaker(config);
        var service = new UnreliableService();

        // Subscribe to state change events
        circuitBreaker.StateChanged += OnStateChanged;

        try
        {
            await RunScenario1_NormalOperation(circuitBreaker, service);
            await Task.Delay(1000);

            await RunScenario2_CircuitOpens(circuitBreaker, service);
            await Task.Delay(1000);

            await RunScenario3_FastFail(circuitBreaker, service);
            await Task.Delay(1000);

            await RunScenario4_Recovery(circuitBreaker, service);
            await Task.Delay(1000);

            await RunScenario5_FailedRecovery(circuitBreaker, service);
            await Task.Delay(1000);

            await RunScenario6_ConcurrentCalls(circuitBreaker, service);
        }
        finally
        {
            circuitBreaker.StateChanged -= OnStateChanged;
        }

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    Demo Complete                            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    }

    static async Task RunScenario1_NormalOperation(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 1: Normal Operation (Low Failure Rate)");

        service.FailureRate = 0.1; // 10% failure rate
        service.Reset();

        for (int i = 0; i < 5; i++)
        {
            await MakeCall(cb, service, i + 1);
            await Task.Delay(200);
        }

        PrintMetrics(cb);
    }

    static async Task RunScenario2_CircuitOpens(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 2: Circuit Opens (High Failure Rate)");

        service.FailureRate = 1.0; // 100% failure rate
        service.Reset();

        Console.WriteLine("Making calls with 100% failure rate...");

        for (int i = 0; i < 5; i++)
        {
            await MakeCall(cb, service, i + 1);
            await Task.Delay(200);
        }

        PrintMetrics(cb);
    }

    static async Task RunScenario3_FastFail(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 3: Fast Fail (Circuit is Open)");

        Console.WriteLine("Attempting calls while circuit is OPEN...");

        for (int i = 0; i < 3; i++)
        {
            await MakeCall(cb, service, i + 1);
            await Task.Delay(200);
        }

        PrintMetrics(cb);
    }

    static async Task RunScenario4_Recovery(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 4: Successful Recovery");

        Console.WriteLine("Waiting for circuit to transition to HALF-OPEN...");
        Console.WriteLine($"(Waiting 5 seconds for timeout)");
        await Task.Delay(6000); // Wait for open timeout + buffer

        service.FailureRate = 0.0; // 0% failure rate (service recovered)
        Console.WriteLine();
        Console.WriteLine("Service has recovered! Making calls in HALF-OPEN state...");

        for (int i = 0; i < 3; i++)
        {
            await MakeCall(cb, service, i + 1);
            await Task.Delay(200);
        }

        PrintMetrics(cb);
    }

    static async Task RunScenario5_FailedRecovery(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 5: Failed Recovery Attempt");

        // First, open the circuit again
        service.FailureRate = 1.0;
        Console.WriteLine("Opening circuit again...");

        for (int i = 0; i < 4; i++)
        {
            await MakeCall(cb, service, i + 1);
            await Task.Delay(200);
        }

        Console.WriteLine();
        Console.WriteLine("Waiting for HALF-OPEN state...");
        await Task.Delay(6000);

        // Service still failing
        Console.WriteLine();
        Console.WriteLine("Attempting call in HALF-OPEN state, but service still failing...");
        await MakeCall(cb, service, 1);

        PrintMetrics(cb);
    }

    static async Task RunScenario6_ConcurrentCalls(CircuitBreaker.CircuitBreaker cb, UnreliableService service)
    {
        PrintScenarioHeader("Scenario 6: Concurrent Calls (Thread Safety)");

        // Wait for circuit to be half-open or closed
        Console.WriteLine("Waiting for circuit to be testable...");
        await Task.Delay(6000);

        service.FailureRate = 0.3; // 30% failure rate

        Console.WriteLine("Making 10 concurrent calls...");

        var tasks = Enumerable.Range(1, 10)
            .Select(i => MakeCall(cb, service, i))
            .ToArray();

        await Task.WhenAll(tasks);

        PrintMetrics(cb);
    }

    static async Task MakeCall(CircuitBreaker.CircuitBreaker cb, UnreliableService service, int callNumber)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var stateColor = GetStateColor(cb.State);

        try
        {
            var result = await cb.ExecuteAsync(async () => await service.CallApiAsync());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{timestamp}] [{stateColor}{cb.State,-9}\u001b[0m] Call #{callNumber}: ✓ {result}");
            Console.ResetColor();
        }
        catch (CircuitBreakerOpenException ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{timestamp}] [{stateColor}{cb.State,-9}\u001b[0m] Call #{callNumber}: ⚠ REJECTED - {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] [{stateColor}{cb.State,-9}\u001b[0m] Call #{callNumber}: ✗ FAILED - {ex.Message}");
            Console.ResetColor();
        }
    }

    static void OnStateChanged(object? sender, StateChangedEventArgs e)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var fromColor = GetStateColor(e.FromState);
        var toColor = GetStateColor(e.ToState);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"════════════════════════════════════════════════════════════════");
        Console.WriteLine($"[{timestamp}] STATE TRANSITION: {fromColor}{e.FromState}\u001b[0m → {toColor}{e.ToState}\u001b[0m");
        Console.WriteLine($"Reason: {e.Reason}");
        Console.WriteLine($"════════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
    }

    static string GetStateColor(CircuitBreakerState state)
    {
        return state switch
        {
            CircuitBreakerState.Closed => "\u001b[32m",    // Green
            CircuitBreakerState.Open => "\u001b[31m",      // Red
            CircuitBreakerState.HalfOpen => "\u001b[33m",  // Yellow
            _ => "\u001b[0m"
        };
    }

    static void PrintScenarioHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ {title,-60} ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    static void PrintMetrics(CircuitBreaker.CircuitBreaker cb)
    {
        Console.WriteLine();
        Console.WriteLine(cb.Metrics.GetHealthStatus());
    }
}
