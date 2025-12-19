namespace CircuitBreakerDemo.CircuitBreaker;

/// <summary>
/// Exception thrown when the circuit breaker is open and rejects a request.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string circuitBreakerName)
        : base($"Circuit breaker '{circuitBreakerName}' is OPEN - request rejected")
    {
    }
}

/// <summary>
/// Implements the circuit breaker pattern to prevent cascading failures.
/// </summary>
public class CircuitBreaker : IDisposable
{
    private readonly CircuitBreakerConfig _config;
    private readonly CircuitBreakerMetrics _metrics;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CircuitBreakerState _state;
    private Timer? _resetTimer;
    private bool _disposed;

    /// <summary>
    /// Event raised when the circuit breaker changes state.
    /// </summary>
    public event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets the current metrics for this circuit breaker.
    /// </summary>
    public CircuitBreakerMetrics Metrics => _metrics;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State => _state;

    /// <summary>
    /// Initializes a new instance of the CircuitBreaker class.
    /// </summary>
    public CircuitBreaker(CircuitBreakerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _metrics = new CircuitBreakerMetrics
        {
            CurrentState = CircuitBreakerState.Closed,
            LastStateChange = DateTime.UtcNow
        };

        _state = CircuitBreakerState.Closed;
    }

    /// <summary>
    /// Executes an operation through the circuit breaker.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Check if circuit is open
            if (_state == CircuitBreakerState.Open)
            {
                _metrics.RecordRejection();
                throw new CircuitBreakerOpenException(_config.Name);
            }

            // Execute the operation
            try
            {
                var result = await operation();
                await OnSuccess();
                return result;
            }
            catch (Exception)
            {
                await OnFailure();
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Handles successful operation execution.
    /// </summary>
    private async Task OnSuccess()
    {
        _metrics.RecordSuccess();

        // If in half-open state, check if we should close the circuit
        if (_state == CircuitBreakerState.HalfOpen)
        {
            if (_metrics.ConsecutiveSuccesses >= _config.SuccessThreshold)
            {
                await TransitionTo(
                    CircuitBreakerState.Closed,
                    $"Success threshold reached ({_config.SuccessThreshold} consecutive successes)");
            }
        }
    }

    /// <summary>
    /// Handles failed operation execution.
    /// </summary>
    private async Task OnFailure()
    {
        _metrics.RecordFailure();

        if (_state == CircuitBreakerState.HalfOpen)
        {
            // Any failure in half-open state reopens the circuit
            await TransitionTo(
                CircuitBreakerState.Open,
                "Failure detected in half-open state");
        }
        else if (_state == CircuitBreakerState.Closed)
        {
            // Check if we've exceeded the failure threshold
            if (_metrics.ConsecutiveFailures >= _config.FailureThreshold)
            {
                await TransitionTo(
                    CircuitBreakerState.Open,
                    $"Failure threshold exceeded ({_config.FailureThreshold} consecutive failures)");
            }
        }
    }

    /// <summary>
    /// Transitions the circuit breaker to a new state.
    /// </summary>
    private async Task TransitionTo(CircuitBreakerState newState, string reason)
    {
        if (_state == newState)
            return;

        var oldState = _state;
        _state = newState;
        _metrics.CurrentState = newState;
        _metrics.LastStateChange = DateTime.UtcNow;

        // Reset counters based on the new state
        if (newState == CircuitBreakerState.Closed)
        {
            _metrics.ResetConsecutiveFailures();
            _metrics.ResetConsecutiveSuccesses();
            StopResetTimer();
        }
        else if (newState == CircuitBreakerState.Open)
        {
            _metrics.ResetConsecutiveSuccesses();
            StartResetTimer();
        }
        else if (newState == CircuitBreakerState.HalfOpen)
        {
            _metrics.ResetConsecutiveFailures();
            _metrics.ResetConsecutiveSuccesses();
            StopResetTimer();
        }

        // Raise state changed event
        StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState, reason));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Starts the timer to transition from Open to HalfOpen.
    /// </summary>
    private void StartResetTimer()
    {
        StopResetTimer();

        _resetTimer = new Timer(
            async _ => await OnResetTimerElapsed(),
            null,
            _config.OpenTimeout,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Stops the reset timer.
    /// </summary>
    private void StopResetTimer()
    {
        _resetTimer?.Dispose();
        _resetTimer = null;
    }

    /// <summary>
    /// Handles the reset timer elapsed event.
    /// </summary>
    private async Task OnResetTimerElapsed()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state == CircuitBreakerState.Open)
            {
                await TransitionTo(
                    CircuitBreakerState.HalfOpen,
                    $"Open timeout elapsed ({_config.OpenTimeout.TotalSeconds}s)");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Disposes the circuit breaker and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        StopResetTimer();
        _semaphore?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
