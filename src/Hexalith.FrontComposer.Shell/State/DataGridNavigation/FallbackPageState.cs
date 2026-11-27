using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Reads one immutable scope, request and page snapshot for fallback admission.</summary>
internal sealed class FallbackPageState(IState<LoadedPageState> pages)
{
    /// <summary>Reads the latest scope generation.</summary>
    public long ScopeGeneration => pages.Value.ScopeGeneration;

    /// <summary>Reads the scope and request from the same state snapshot.</summary>
    public (long Scope, string Identity, long? Generation) Request(string viewKey)
        => Request(pages.Value, viewKey);

    /// <summary>Reads the successful-load sequence of the current fallback page.</summary>
    public long ReadSequence(string viewKey, int skip) => pages.Value.SuccessfulReadSequenceByPage.GetValueOrDefault((viewKey, skip));

    /// <summary>Checks page presence for manually constructed schedulers.</summary>
    public bool HasPage(string viewKey, int skip) => pages.Value.PagesByKey.ContainsKey((viewKey, skip));

    /// <summary>Reads the request and scope from a supplied reducer snapshot.</summary>
    public static (long Scope, string Identity, long? Generation) Request(LoadedPageState state, string viewKey)
        => (state.ScopeGeneration, state.ActiveRequestIdentityByView.GetValueOrDefault(viewKey, string.Empty),
            state.ActiveRequestGenerationByView.TryGetValue(viewKey, out long generation) ? generation : null);
}
