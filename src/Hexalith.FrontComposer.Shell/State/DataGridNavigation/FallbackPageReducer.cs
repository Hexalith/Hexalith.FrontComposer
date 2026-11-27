using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Registers fallback page comparisons with the loaded-page feature.</summary>
internal sealed class FallbackPageReducer(LoadedPageReducers reducers)
{
    /// <summary>Applies an admitted fallback read to the current loaded-page state.</summary>
    [ReducerMethod]
    public LoadedPageState Reduce(LoadedPageState state, CompareFallbackPageAction action)
    {
        lock (action.AdmissionGate)
        {
            return reducers.CompareFallbackPage(state, action);
        }
    }

    /// <summary>Completes the admission after Fluxor publishes the reducer state.</summary>
    [EffectMethod]
    public static Task CompleteAsync(CompareFallbackPageAction action, IDispatcher dispatcher)
    {
        if (action.Notification is { } notification)
        {
            dispatcher.Dispatch(notification);
        }

        _ = action.Result.TrySetResult(action.Outcome);
        return Task.CompletedTask;
    }
}
