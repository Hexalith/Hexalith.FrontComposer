using Counter.Domain;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

internal sealed class Epic9CreateProjectionEffects(Epic9ProjectionReleaseGate? releaseGate = null)
{
    private readonly Dictionary<string, (string CounterId, int InitialValue)> _pending = new(StringComparer.Ordinal);

    [EffectMethod]
    public Task OnSubmitted(CreateCounterCommandActions.SubmittedAction action, IDispatcher dispatcher)
    {
        _pending[action.CorrelationId] = (action.Command.CounterId, action.Command.InitialValue);
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task OnConfirmed(CreateCounterCommandActions.ConfirmedAction action, IDispatcher dispatcher)
    {
        if (!_pending.Remove(action.CorrelationId, out (string CounterId, int InitialValue) submitted))
        {
            return;
        }

        if (releaseGate is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(true);
        }
        else
        {
            await releaseGate.WaitAsync().ConfigureAwait(true);
        }
        CounterProjection created = new()
        {
            Id = submitted.CounterId,
            Count = submitted.InitialValue,
            LastUpdated = DateTimeOffset.UtcNow,
        };
        dispatcher.Dispatch(new CounterProjectionLoadedAction(action.CorrelationId, [created]));
    }
}
