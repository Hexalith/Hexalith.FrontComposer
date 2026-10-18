namespace Hexalith.FrontComposer.Testing;

/// <summary>One scheduled timer owned by the deterministic clock.</summary>
internal sealed class TestTimeProviderTimer(TestTimeProvider clock, TimerCallback callback, object? state) : ITimer
{
    private static readonly Lazy<ExecutionContext> CleanContext = new(CreateCleanContext);
    private readonly ExecutionContext? _creationContext = ExecutionContext.Capture();
    private readonly object _invocationGate = new();
    private int _disposed;
    private int _activeInvocations;
    private TaskCompletionSource? _drained;

    /// <summary>Next due monotonic tick.</summary>
    internal long DueTicks { get; set; }
    /// <summary>Recurrence interval, or an infinite/zero sentinel.</summary>
    internal long PeriodTicks { get; set; }
    /// <summary>Stable scheduling order at equal due times.</summary>
    internal long Order { get; set; }
    /// <summary>Whether the timer has been retired.</summary>
    internal bool IsDisposed { get => Volatile.Read(ref _disposed) != 0; set => Volatile.Write(ref _disposed, value ? 1 : 0); }
    /// <summary>Invokes the callback synchronously under its creation context, unless disposal has retired it.</summary>
    internal void Invoke()
    {
        lock (_invocationGate)
        {
            if (IsDisposed) { return; }
            _activeInvocations++;
        }
        try { ExecutionContext.Run(_creationContext ?? CleanContext.Value, _ => callback(state), null); }
        finally
        {
            lock (_invocationGate)
            {
                _activeInvocations--;
                if (_activeInvocations == 0) { _drained?.TrySetResult(); }
            }
        }
    }
    /// <summary>Changes the deterministic timer schedule.</summary>
    public bool Change(TimeSpan dueTime, TimeSpan period) => clock.Change(this, dueTime, period);
    /// <summary>Removes the timer from its clock.</summary>
    public void Dispose() => clock.Remove(this);
    /// <summary>Retires the timer and completes once active callbacks have drained.</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        lock (_invocationGate)
        {
            if (_activeInvocations == 0) { return ValueTask.CompletedTask; }
            _drained ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
            return new(_drained.Task);
        }
    }

    private static ExecutionContext CreateCleanContext()
    {
        ExecutionContext? context = null;
        Thread thread = new(() => context = ExecutionContext.Capture());
        if (ExecutionContext.IsFlowSuppressed()) { thread.Start(); }
        else { using (ExecutionContext.SuppressFlow()) { thread.Start(); } }
        thread.Join();
        return context ?? throw new InvalidOperationException("A clean timer execution context could not be captured.");
    }
}
