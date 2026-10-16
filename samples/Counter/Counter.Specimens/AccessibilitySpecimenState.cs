using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Counter.Specimens;

/// <summary>Fixed, circuit-local scenarios registered only by the gated Test host.</summary>
public sealed class AccessibilitySpecimenState
{
    private TaskCompletionSource<ProjectionPageResult>? _page;

    /// <summary>Gets the currently held deterministic page result.</summary>
    public Task<ProjectionPageResult>? Page => _page?.Task;

    /// <summary>Begins a held result without manufacturing projection banners.</summary>
    public void BeginPage()
    {
        CancelPage();
        _page = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Completes the held query with fixed safe rows and an optional result cap.</summary>
    /// <param name="capped">Whether the fixed total exceeds the unfiltered result limit.</param>
    public void CompletePage(bool capped = false)
        => _page?.TrySetResult(new ProjectionPageResult([new Counter.Domain.CounterProjection { Id = "FC-AE-PAGE", Count = 3 }], capped ? 10001 : 1, null));

    /// <summary>Cancels a held result when its specimen leaves the page.</summary>
    public void CancelPage()
    {
        _page?.TrySetCanceled();
        _page = null;
    }

    /// <summary>Gets the finite safe command outcomes.</summary>
    public static IReadOnlyList<string> Outcomes { get; } = ["validation", "returned-rejection", "exception-rejection", "forbidden", "unavailable", "blank-unavailable", "rate-limited", "blank-rate-limited", "transport", "admission", "policy", "accepted"];

    /// <summary>Gets the selected outcome.</summary>
    public string Outcome { get; private set; } = "validation";

    /// <summary>Gets the number of actual dispatch attempts.</summary>
    public int DispatchCount { get; private set; }

    /// <summary>Selects a fixed outcome without notifying authorization caches.</summary>
    /// <param name="outcome">An allowlisted scenario name.</param>
    public void Select(string outcome)
    {
        if (!Outcomes.Contains(outcome, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }
        Outcome = outcome;
    }

    /// <summary>Records an actual service dispatch, independently of UI feedback.</summary>
    public void RecordDispatch() => DispatchCount++;
}
