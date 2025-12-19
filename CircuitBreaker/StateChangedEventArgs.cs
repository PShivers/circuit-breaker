namespace CircuitBreakerDemo.CircuitBreaker;

/// <summary>
/// Event arguments for circuit breaker state change events.
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState FromState { get; }

    /// <summary>
    /// Gets the new state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState ToState { get; }

    /// <summary>
    /// Gets the timestamp when the state change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the reason for the state change.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the StateChangedEventArgs class.
    /// </summary>
    public StateChangedEventArgs(
        CircuitBreakerState fromState,
        CircuitBreakerState toState,
        string reason)
    {
        FromState = fromState;
        ToState = toState;
        Timestamp = DateTime.UtcNow;
        Reason = reason;
    }
}
