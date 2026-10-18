namespace Hexalith.FrontComposer.Testing;

/// <summary>A deterministic clock whose timers run synchronously in due-time order on advancement.</summary>
public sealed class TestTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<TestTimeProviderTimer> _timers = [];
    private readonly DateTimeOffset _start;
    private long _ticks;
    private long _nextOrder;
    private bool _advancing;

    /// <summary>Creates a clock at the supplied instant, or the Unix epoch.</summary>
    public TestTimeProvider(DateTimeOffset? start = null) => _start = (start ?? DateTimeOffset.UnixEpoch).ToUniversalTime();

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) { return _start.AddTicks(_ticks); }
    }

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        lock (_gate) { return _ticks; }
    }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        TestTimeProviderTimer timer = new(this, callback, state);
        _ = timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>Advances time without sleeping. Timers created or changed by callbacks participate in the same advance.</summary>
    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        long target;
        lock (_gate)
        {
            if (_advancing) { throw new InvalidOperationException("Clock advancement cannot be nested or concurrent."); }
            target = checked(_ticks + elapsed.Ticks);
            _ = _start.AddTicks(target);
            _advancing = true;
        }
        try
        {
            while (true)
            {
                TestTimeProviderTimer? timer;
                lock (_gate)
                {
                    timer = _timers.Where(t => t.DueTicks <= target)
                        .OrderBy(t => t.DueTicks).ThenBy(t => t.Order).FirstOrDefault();
                    if (timer is null) { _ticks = target; break; }
                    _ticks = timer.DueTicks;
                    _timers.Remove(timer);
                    if (timer.PeriodTicks > 0)
                    {
                        timer.DueTicks = checked(_ticks + timer.PeriodTicks);
                        timer.Order = ++_nextOrder;
                        _timers.Add(timer);
                    }
                }
                // Adopter callbacks may acquire locks whose owners change/dispose timers.
                timer.Invoke();
            }
        }
        finally { lock (_gate) { _advancing = false; } }
    }

    /// <summary>Changes one owned timer under the clock gate.</summary>
    internal bool Change(TestTimeProviderTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        ValidateTimeout(dueTime);
        ValidateTimeout(period);
        lock (_gate)
        {
            if (timer.IsDisposed) { return false; }
            _timers.Remove(timer);
            timer.PeriodTicks = period.Ticks;
            if (dueTime != Timeout.InfiniteTimeSpan)
            {
                timer.DueTicks = checked(_ticks + dueTime.Ticks);
                timer.Order = ++_nextOrder;
                _timers.Add(timer);
            }
            return true;
        }
    }

    /// <summary>Retires one owned timer.</summary>
    internal void Remove(TestTimeProviderTimer timer)
    {
        lock (_gate)
        {
            timer.IsDisposed = true;
            _timers.Remove(timer);
        }
    }

    private static void ValidateTimeout(TimeSpan value)
    {
        if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
