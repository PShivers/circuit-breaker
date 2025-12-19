namespace CircuitBreakerDemo.CircuitBreaker;

/// <summary>
/// Configuration settings for the circuit breaker.
/// </summary>
public class CircuitBreakerConfig
{
    /// <summary>
    /// Gets or sets the name of the circuit breaker (for logging and identification).
    /// </summary>
    public string Name { get; set; } = "CircuitBreaker";

    /// <summary>
    /// Gets or sets the number of consecutive failures before the circuit opens.
    /// Default is 5.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the number of consecutive successes needed in half-open state to close the circuit.
    /// Default is 2.
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;

    /// <summary>
    /// Gets or sets how long the circuit stays open before transitioning to half-open.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    public void Validate()
    {
        if (FailureThreshold <= 0)
            throw new ArgumentException("FailureThreshold must be greater than 0", nameof(FailureThreshold));

        if (SuccessThreshold <= 0)
            throw new ArgumentException("SuccessThreshold must be greater than 0", nameof(SuccessThreshold));

        if (OpenTimeout <= TimeSpan.Zero)
            throw new ArgumentException("OpenTimeout must be greater than zero", nameof(OpenTimeout));

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be null or empty", nameof(Name));
    }
}
