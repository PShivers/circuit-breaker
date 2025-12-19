namespace CircuitBreakerDemo.Services;

/// <summary>
/// Simulates an unreliable external service for testing the circuit breaker.
/// </summary>
public class UnreliableService
{
    private readonly Random _random = new();
    private int _callCount;

    /// <summary>
    /// Gets or sets the failure rate (0.0 to 1.0). Default is 0.5 (50% failure rate).
    /// </summary>
    public double FailureRate { get; set; } = 0.5;

    /// <summary>
    /// Gets the total number of calls made to this service.
    /// </summary>
    public int CallCount => _callCount;

    /// <summary>
    /// Simulates an API call that may fail based on the configured failure rate.
    /// </summary>
    public async Task<string> CallApiAsync()
    {
        Interlocked.Increment(ref _callCount);

        // Simulate network delay
        await Task.Delay(_random.Next(50, 150));

        // Randomly fail based on failure rate
        if (_random.NextDouble() < FailureRate)
        {
            throw new HttpRequestException($"Service call failed (simulated failure)");
        }

        return $"Success! Call #{CallCount} completed at {DateTime.UtcNow:HH:mm:ss.fff}";
    }

    /// <summary>
    /// Resets the call counter.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _callCount, 0);
    }
}
