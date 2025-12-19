namespace CircuitBreakerDemo.CircuitBreaker;

/// <summary>
/// Represents the possible states of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - requests are allowed through.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is broken - requests fail immediately without being executed.
    /// </summary>
    Open,

    /// <summary>
    /// Testing if the service has recovered - limited requests are allowed through.
    /// </summary>
    HalfOpen
}
