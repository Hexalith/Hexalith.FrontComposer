using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Canonical SS truth and VR-05 did-not-run assertions over measured surface data.</summary>
public static class UxStateAssertions
{
    private static readonly string[] States = [
        "NoRegistrations", "Loading", "Empty", "Data", "NoTenant", "NoAccess", "Loading", "Empty", "Data",
        "Stale", "Reconnecting", "FallbackPolling", "SlowQuery", "MaxItems", "FilterNoMatches",
        "Submitting", "Acknowledged", "Syncing", "Confirmed", "Rejected", "IdempotentConfirmed", "NeedsReview", "Warning", "Degraded",
        "BlockedSubmit", "DegradedExhausted", "Bootstrap", "StartupFailure", "RouteFailure", "Loading", "SelectedTab", "InvalidTabFallback",
        "DisabledTab", "QueryFailure", "Offline", "Ready", "ValidationBlocked", "AuthorizationDenied", "AbandonmentDecision",
        "PaletteOpen", "PaletteNoResults", "PaletteActivationFailure", "SettingsOpen", "DestructiveConfirmationOpen", "AbandonmentWarning",
        "OverlayClosed", "OriginRemoved", "FreshRow", "FilterHiddenDetail",
    ];
    private static readonly HashSet<int> NonTerminalRows = [2, 7, 10, 11, 12, 13, 16, 17, 18, 24, 27, 30, 35, 39, 40, 41, 43, 44, 45, 48];

    /// <summary>Asserts named role, exact canonical state and subject terminality for SS-01..49; lifecycle rows retain distinct machine identities.</summary>
    public static void AssertState(string rowId, UxSurfaceSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(rowId);
        if (rowId is not { Length: 5 } || !rowId.StartsWith("SS-", StringComparison.Ordinal)
            || rowId[3] is < '0' or > '9' || rowId[4] is < '0' or > '9'
            || !int.TryParse(rowId.AsSpan(3), out int number) || number is < 1 or > 49)
        {
            throw new ArgumentOutOfRangeException(nameof(rowId));
        }
        string state = States[number - 1];
        bool lifecycle = number is >= 16 and <= 24 or 26;
        bool lifecycleMatches = !lifecycle || Enum.TryParse(state, out CommandLifecycleState expected) && actual.Lifecycle == expected;
        UxSurfaceFacts? facts = actual.Facts;
        bool hiddenByPolicy = number == 6 && facts is { Visible: false, PolicyHidden: true, SuccessClaim: false };
        bool measured = hiddenByPolicy || facts is { Visible: true } && facts.SuccessClaim == (number is 19 or 21)
            && MatchesFacts(number, facts);
        bool focusOnly = number is 5 or 6 or 28 or 37 or 38 && !hiddenByPolicy;
        bool focusSemantics = !focusOnly || actual.Semantics.IsFocused
            && actual.Semantics.Role == (number == 37 ? "group" : "heading")
            && actual.Semantics.TabIndex == "-1" && actual.Semantics.AriaLive is null && actual.Semantics.AriaAtomic is null;
        bool silent = number is 31 or 33 or 36 or 39 or 40 or 43 or 44 or 45 or 46 or 47;
        bool silentSemantics = !silent || actual.Semantics.AriaLive is null && !IsLiveRole(actual.Semantics.Role);
        bool tabSemantics = number is not (31 or 32 or 33) || actual.Semantics.Role == "tab";
        bool restoredSemantics = number is not (46 or 47) || actual.Semantics.IsFocused
            && (number != 47 || actual.Semantics.Role == "heading" && actual.Semantics.TabIndex == "-1");
        bool readySemantics = number != 36 || !actual.Dispatched && !actual.Queued
            && actual.Lifecycle is null or CommandLifecycleState.Idle;
        bool modalSemantics = number is not (40 or 43 or 44) || actual.Semantics.Role == "dialog";
        bool hiddenSemantics = !hiddenByPolicy || !actual.Semantics.IsFocused && actual.Semantics.AriaLive is null && !IsLiveRole(actual.Semantics.Role);
        if (!measured || !focusSemantics || !silentSemantics || !tabSemantics || !restoredSemantics || !readySemantics || !modalSemantics || !hiddenSemantics || actual.State != state || actual.Terminal == NonTerminalRows.Contains(number)
            || !hiddenByPolicy && (string.IsNullOrWhiteSpace(actual.Semantics.Role) || string.IsNullOrWhiteSpace(actual.Semantics.Name))
            || !lifecycleMatches || number is 25 or 37 or 38 && (actual.Dispatched || actual.Queued))
        {
            throw new InvalidOperationException($"{rowId} surface state, terminality, semantic role/name, or lifecycle truth drifted.");
        }
    }

    private static bool IsLiveRole(string role) => role is "alert" or "status" or "log";

    private static bool MatchesFacts(int row, UxSurfaceFacts f) => row switch
    {
        1 or 3 or 8 => f.Empty == true && f.FailureVisible == false,
        2 or 7 or 27 or 30 => f.LoadingVisible == true,
        4 or 9 => f.DataVisible == true && f.Empty == false && f.FailureVisible == false,
        5 or 6 or 20 or 22 or 28 or 38 => f.FailureVisible == true,
        10 or 11 or 12 or 13 or 14 or 16 or 17 or 18 or 19 or 21 or 23 => f.StateCueVisible == true,
        15 => f.Empty == true && f.FiltersVisible == true && f.FailureVisible == false,
        24 => f.StateCueVisible == true && f.PollingActive == true && f.FailureVisible == true,
        25 => f.FailureVisible == true && f.OriginalOperationVisible == true,
        26 => f.FailureVisible == true && f.PollingActive == false,
        29 or 42 => f.FailureVisible == true && f.CurrentRoutePreserved == true,
        31 => f.Selected == true && f.Disabled == false && f.PanelVisible == true && f.PanelOwnedByTab == true,
        32 => f.Selected == true && f.Disabled == false && f.PanelVisible == true && f.PanelOwnedByTab == true && f.InvalidTargetSelected == false,
        33 => f.Disabled == true,
        34 => f.FailureVisible == true && f.Empty == false,
        35 => f.FailureVisible == true && f.StateCueVisible == true,
        36 => f.Editable == true,
        37 => f.FailureVisible == true && f.Editable == true,
        39 or 45 => f.InFlow == true && f.Modal == false && f.Editable == true,
        40 or 43 or 44 => f.Modal == true,
        41 => f.Empty == true && f.Editable == true,
        46 => f.FocusRestored == true && f.CurrentRoutePreserved == true,
        47 => f.FocusRestored == true && f.CurrentRoutePreserved == true && f.Disabled == false,
        48 => f.UpdatedCueVisible == true,
        49 => f.DetailHidden == true && f.FiltersVisible == true,
        _ => false,
    };

    /// <summary>Asserts AM-20 feedback, one speech path, no dispatch/queue, and retained attempted-control focus.</summary>
    public static void AssertBlockedSubmit(UxSurfaceSnapshot state, UxFocusTarget attemptedControl,
        UxFocusTarget focusedControl, IReadOnlyList<UxAnnouncement> announcements)
    {
        ArgumentNullException.ThrowIfNull(attemptedControl);
        ArgumentNullException.ThrowIfNull(focusedControl);
        ArgumentNullException.ThrowIfNull(announcements);
        AssertState("SS-25", state);
        if (attemptedControl.Handle is null || focusedControl.Handle is null
            || !ReferenceEquals(attemptedControl.Handle, focusedControl.Handle)
            || !focusedControl.Connected || !focusedControl.Enabled || !focusedControl.Visible
            || focusedControl.Role != attemptedControl.Role || focusedControl.Name != attemptedControl.Name
            || string.IsNullOrWhiteSpace(focusedControl.Name))
        {
            throw new InvalidOperationException("VR-05 attempted submit control did not retain usable focus.");
        }
        UxAnnouncement actual = announcements.Count == 1 ? announcements[0]
            : throw new InvalidOperationException("AM-20 must announce once for the blocked attempt.");
        UxAnnouncementAssertions.AssertSequence(announcements,
            UxAnnouncementCatalog.Expected("AM-20", actual.OperationId, actual.StateIdentity));
    }
}
