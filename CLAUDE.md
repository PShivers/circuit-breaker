# Circuit Breaker Pattern - C# Implementation

This project demonstrates a from-scratch implementation of the Circuit Breaker pattern in C# with .NET 8, including comprehensive demo scenarios.

## Project Overview

The Circuit Breaker pattern prevents cascading failures in distributed systems by:
- **Detecting failures** and opening the circuit after a threshold
- **Failing fast** when the circuit is open (no wasted resources)
- **Testing recovery** by transitioning to half-open state
- **Automatic recovery** when the service becomes healthy again

## Project Structure

```
circuit-breaker/
├── CircuitBreakerDemo.csproj          # .NET 8 console application
├── Program.cs                          # Entry point with 6 demo scenarios
├── CircuitBreaker/
│   ├── CircuitBreaker.cs              # Core implementation with async support
│   ├── CircuitBreakerState.cs         # Enum: Closed, Open, HalfOpen
│   ├── CircuitBreakerConfig.cs        # Configuration (thresholds, timeouts)
│   ├── CircuitBreakerMetrics.cs       # Thread-safe metrics tracking
│   └── StateChangedEventArgs.cs       # Event data for state transitions
└── Services/
    └── UnreliableService.cs           # Mock service for testing (simulates failures)
```

## Build and Run Commands

```bash
# Navigate to the directory
cd circuit-breaker/

# Build the project
dotnet build CircuitBreakerDemo.csproj

# Run the demo application
dotnet run --project CircuitBreakerDemo.csproj

# Clean build artifacts
dotnet clean
```

## Architecture

### Core Components

**CircuitBreakerState**
- Three states: `Closed` (normal), `Open` (failing fast), `HalfOpen` (testing recovery)

**CircuitBreakerConfig**
- `FailureThreshold`: Number of consecutive failures before opening (default: 5)
- `SuccessThreshold`: Successes needed in half-open to close (default: 2)
- `OpenTimeout`: How long to stay open before trying half-open (default: 30s)
- `Name`: Circuit breaker identifier for logging

**CircuitBreaker**
- Main implementation with `ExecuteAsync<T>()` method
- Thread-safe using `SemaphoreSlim` for async operations
- Automatic state transitions based on success/failure patterns
- Timer-based transition from Open → HalfOpen
- Event notifications on state changes

**CircuitBreakerMetrics**
- Thread-safe counters using `Interlocked` operations
- Tracks: total/successful/failed/rejected requests
- Monitors: consecutive failures/successes, current state, state duration
- Provides formatted health status report

### State Transition Logic

```
Closed → Open
  Trigger: Consecutive failures ≥ FailureThreshold
  Action: Start timeout timer, reject all requests

Open → HalfOpen
  Trigger: OpenTimeout elapsed
  Action: Allow limited requests to test service health

HalfOpen → Closed
  Trigger: Consecutive successes ≥ SuccessThreshold
  Action: Resume normal operation

HalfOpen → Open
  Trigger: Any failure during testing
  Action: Return to open state, restart timer
```

### Thread Safety

- `SemaphoreSlim` for async-safe state access during transitions
- `Interlocked` operations for atomic metric counter updates
- Locking during state transitions to prevent race conditions
- Safe for concurrent operations across multiple threads

## Demo Scenarios

The console application demonstrates six scenarios:

1. **Normal Operation** - Low failure rate, circuit stays closed
2. **Circuit Opens** - High failure rate triggers threshold, circuit opens
3. **Fast Fail** - Rejected requests while circuit is open (no actual calls made)
4. **Successful Recovery** - Service recovers, circuit closes after testing
5. **Failed Recovery** - Service still failing in half-open, circuit reopens
6. **Concurrent Calls** - Multiple async calls demonstrating thread safety

### Console Output Features

- Color-coded states: Green (Closed), Red (Open), Yellow (HalfOpen)
- Timestamped log entries with millisecond precision
- Real-time state transition notifications with reasons
- Metrics dashboard showing health status after each scenario

## Usage Examples

### Basic Usage

```csharp
// Create configuration
var config = new CircuitBreakerConfig
{
    Name = "MyServiceCircuitBreaker",
    FailureThreshold = 3,
    SuccessThreshold = 2,
    OpenTimeout = TimeSpan.FromSeconds(10)
};

// Create circuit breaker
using var circuitBreaker = new CircuitBreaker(config);

// Subscribe to state changes
circuitBreaker.StateChanged += (sender, e) =>
{
    Console.WriteLine($"State changed: {e.FromState} → {e.ToState}");
    Console.WriteLine($"Reason: {e.Reason}");
};

// Execute operations
try
{
    var result = await circuitBreaker.ExecuteAsync(async () =>
    {
        // Your async operation here
        return await CallExternalServiceAsync();
    });
}
catch (CircuitBreakerOpenException)
{
    // Circuit is open, handle fast-fail
}
catch (Exception ex)
{
    // Actual operation failed
}
```

### Accessing Metrics

```csharp
// Get metrics
var metrics = circuitBreaker.Metrics;
Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
Console.WriteLine($"Success Rate: {metrics.SuccessfulRequests * 100.0 / metrics.TotalRequests}%");
Console.WriteLine($"Current State: {metrics.CurrentState}");

// Print formatted health status
Console.WriteLine(metrics.GetHealthStatus());
```

## Key Implementation Details

### Async/Await Support
- All operations are fully asynchronous using `async`/`await`
- Uses `SemaphoreSlim` instead of locks for async compatibility
- Supports `CancellationToken` for operation cancellation

### Configuration Validation
- Validates all configuration values on creation
- Throws `ArgumentException` for invalid thresholds or timeouts
- Ensures circuit breaker name is provided

### Resource Management
- Implements `IDisposable` pattern
- Properly disposes of timer and semaphore
- Prevents resource leaks with finalizer suppression

### Error Handling
- Custom `CircuitBreakerOpenException` for rejected requests
- Preserves original exceptions when circuit is closed
- Clear error messages including circuit breaker name

## Extending the Circuit Breaker

### Adding Custom Failure Detection

```csharp
// You can wrap the circuit breaker with custom logic
bool IsTransientError(Exception ex) =>
    ex is HttpRequestException or TimeoutException;

try
{
    return await circuitBreaker.ExecuteAsync(operation);
}
catch (Exception ex) when (!IsTransientError(ex))
{
    // Don't count non-transient errors toward failure threshold
    throw;
}
```

### Multiple Circuit Breakers

```csharp
// Create separate circuit breakers for different services
var paymentCB = new CircuitBreaker(new CircuitBreakerConfig
{
    Name = "PaymentService",
    FailureThreshold = 5,
    OpenTimeout = TimeSpan.FromSeconds(30)
});

var inventoryCB = new CircuitBreaker(new CircuitBreakerConfig
{
    Name = "InventoryService",
    FailureThreshold = 3,
    OpenTimeout = TimeSpan.FromSeconds(60)
});
```

## Design Patterns Used

- **Circuit Breaker Pattern**: Core pattern implementation
- **Observer Pattern**: State change notifications via events
- **Strategy Pattern**: Configurable thresholds and timeouts
- **Singleton-like State**: Single state machine per instance
- **Dispose Pattern**: Proper resource cleanup

## Dependencies

- .NET 8.0 SDK (LTS)
- No external NuGet packages required
- Uses only BCL (Base Class Library) types

## Testing Notes

The `UnreliableService` class simulates various failure scenarios:
- Configurable failure rate (0.0 to 1.0)
- Random delays to simulate network latency
- Controllable behavior for testing all state transitions
- Reset capability for running multiple test scenarios

## Performance Considerations

- Minimal overhead when circuit is closed (single semaphore wait)
- Zero external calls when circuit is open (fast-fail)
- Thread-safe without blocking (uses SemaphoreSlim, not Monitor)
- Efficient metric tracking with Interlocked operations
- Timer-based recovery testing (no polling overhead)
