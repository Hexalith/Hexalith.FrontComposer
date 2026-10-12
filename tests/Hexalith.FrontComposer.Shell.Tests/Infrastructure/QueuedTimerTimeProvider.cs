using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure;

/// <summary>Retains callbacks to simulate ticks queued before their timers were disposed.</summary>
internal sealed class QueuedTimerTimeProvider : TimeProvider
{
    /// <summary>Gets the captured timer callbacks, including callbacks for disposed timers.</summary>
    internal List<Action> Callbacks { get; } = [];

    /// <summary>When set, <see cref="GetUtcNow"/> returns this clock instead of the system clock.</summary>
    internal DateTimeOffset? UtcNow { get; set; }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => UtcNow ?? base.GetUtcNow();

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Callbacks.Add(() => callback(state));
        return Substitute.For<ITimer>();
    }
}
