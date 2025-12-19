namespace CircuitBreakerDemo.CircuitBreaker;

/// <summary>
/// Thread-safe metrics tracking for circuit breaker operations.
/// </summary>
public class CircuitBreakerMetrics
{
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _rejectedRequests;
    private int _consecutiveFailures;
    private int _consecutiveSuccesses;

    /// <summary>
    /// Gets the total number of requests attempted.
    /// </summary>
    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public long SuccessfulRequests => Interlocked.Read(ref _successfulRequests);

    /// <summary>
    /// Gets the number of failed requests.
    /// </summary>
    public long FailedRequests => Interlocked.Read(ref _failedRequests);

    /// <summary>
    /// Gets the number of rejected requests (when circuit is open).
    /// </summary>
    public long RejectedRequests => Interlocked.Read(ref _rejectedRequests);

    /// <summary>
    /// Gets the current number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>
    /// Gets the current number of consecutive successes (in half-open state).
    /// </summary>
    public int ConsecutiveSuccesses => _consecutiveSuccesses;

    /// <summary>
    /// Gets or sets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerState CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last state change.
    /// </summary>
    public DateTime LastStateChange { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Records a successful request.
    /// </summary>
    public void RecordSuccess()
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _successfulRequests);
        Interlocked.Increment(ref _consecutiveSuccesses);
        Interlocked.Exchange(ref _consecutiveFailures, 0);
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    public void RecordFailure()
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _failedRequests);
        Interlocked.Increment(ref _consecutiveFailures);
        Interlocked.Exchange(ref _consecutiveSuccesses, 0);
    }

    /// <summary>
    /// Records a rejected request (when circuit is open).
    /// </summary>
    public void RecordRejection()
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _rejectedRequests);
    }

    /// <summary>
    /// Resets consecutive failure counter.
    /// </summary>
    public void ResetConsecutiveFailures()
    {
        Interlocked.Exchange(ref _consecutiveFailures, 0);
    }

    /// <summary>
    /// Resets consecutive success counter.
    /// </summary>
    public void ResetConsecutiveSuccesses()
    {
        Interlocked.Exchange(ref _consecutiveSuccesses, 0);
    }

    /// <summary>
    /// Gets a formatted health status report.
    /// </summary>
    public string GetHealthStatus()
    {
        var timeSinceStateChange = DateTime.UtcNow - LastStateChange;
        var successRate = TotalRequests > 0
            ? (SuccessfulRequests * 100.0 / TotalRequests).ToString("F1")
            : "N/A";

        return $"""
            ╔════════════════════════════════════════════╗
            ║     Circuit Breaker Health Status         ║
            ╠════════════════════════════════════════════╣
            ║ State: {CurrentState,-35} ║
            ║ Time in Current State: {timeSinceStateChange.TotalSeconds,15:F1}s ║
            ╠════════════════════════════════════════════╣
            ║ Total Requests:        {TotalRequests,15} ║
            ║ Successful:            {SuccessfulRequests,15} ║
            ║ Failed:                {FailedRequests,15} ║
            ║ Rejected:              {RejectedRequests,15} ║
            ║ Success Rate:          {successRate,14}% ║
            ╠════════════════════════════════════════════╣
            ║ Consecutive Failures:  {ConsecutiveFailures,15} ║
            ║ Consecutive Successes: {ConsecutiveSuccesses,15} ║
            ╚════════════════════════════════════════════╝
            """;
    }
}
