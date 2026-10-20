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
        UxSurfaceFacts? facts = actual.Facts;
        UxAccessibilitySnapshot semantics = actual.Semantics;
        bool hiddenByPolicy = number == 6 && facts is { Visible: false, PolicyHidden: true, SuccessClaim: false };
        bool focusOnly = number is 5 or 6 or 28 or 37 or 38 && !hiddenByPolicy;
        bool silent = number is 31 or 33 or 36 or 39 or 40 or 43 or 44 or 45 or 46 or 47;
        // Diagnostics name failed checks and required facts, never observed names or identities.
        List<string> failed = hiddenByPolicy ? [] : FactFailures(number, facts);
        void Check(bool passed, string check)
        {
            if (!passed) { failed.Add(check); }
        }
        Check(actual.State == state, "State");
        Check(actual.Terminal != NonTerminalRows.Contains(number), "Terminal");
        Check(!lifecycle || Enum.TryParse(state, out CommandLifecycleState expected) && actual.Lifecycle == expected, "Lifecycle");
        Check(hiddenByPolicy || !string.IsNullOrWhiteSpace(semantics.Role) && !string.IsNullOrWhiteSpace(semantics.Name), "Semantics.RoleName");
        Check(!focusOnly || semantics.IsFocused && semantics.Role == (number == 37 ? "group" : "heading")
            && semantics.TabIndex == "-1" && semantics.AriaLive is null && semantics.AriaAtomic is null, "Semantics.FocusOnly");
        Check(!silent || semantics.Role != "alert" && (semantics.AriaLive == "off"
            || semantics.AriaLive is null && !IsLiveRole(semantics.Role)), "Semantics.Silent");
        Check(number is not (31 or 32 or 33) || semantics.Role == "tab", "Semantics.TabRole");
        Check(number is not (46 or 47) || semantics.IsFocused
            && (number != 47 || semantics.Role == "heading" && semantics.TabIndex == "-1"), "Semantics.RestoredFocus");
        Check(number is not (40 or 43 or 44) || semantics.Role == "dialog", "Semantics.DialogRole");
        Check(!hiddenByPolicy || !semantics.IsFocused && semantics.AriaLive is null && !IsLiveRole(semantics.Role), "Semantics.PolicyHidden");
        // Blocked, ready, validation-blocked and denied attempts never dispatch, queue, or enter the command lifecycle.
        Check(number is not (25 or 36 or 37 or 38) || !actual.Dispatched && !actual.Queued
            && actual.Lifecycle is null or CommandLifecycleState.Idle, "DidNotRun");
        if (failed.Count > 0)
        {
            throw new InvalidOperationException($"{rowId} surface state, terminality, semantic role/name, or lifecycle truth drifted: {string.Join(", ", failed)}.");
        }
    }

    private static bool IsLiveRole(string role) => role is "alert" or "status" or "log";

    private static List<string> FactFailures(int row, UxSurfaceFacts? f)
    {
        if (f is null) { return ["Facts"]; }
        (string Name, bool? Actual, bool Expected)[] required = [
            (nameof(UxSurfaceFacts.Visible), f.Visible, true), (nameof(UxSurfaceFacts.SuccessClaim), f.SuccessClaim, row is 19 or 21),
            .. RequiredFacts(row, f)];
        return [.. required.Where(fact => fact.Actual != fact.Expected).Select(fact => "Facts." + fact.Name)];
    }

    private static (string Name, bool? Actual, bool Expected)[] RequiredFacts(int row, UxSurfaceFacts f)
    {
        (string, bool?, bool) Empty(bool expected) => (nameof(f.Empty), f.Empty, expected);
        (string, bool?, bool) Failure(bool expected) => (nameof(f.FailureVisible), f.FailureVisible, expected);
        (string, bool?, bool) Cue(bool expected) => (nameof(f.StateCueVisible), f.StateCueVisible, expected);
        (string, bool?, bool) Filters(bool expected) => (nameof(f.FiltersVisible), f.FiltersVisible, expected);
        (string, bool?, bool) Polling(bool expected) => (nameof(f.PollingActive), f.PollingActive, expected);
        (string, bool?, bool) Route(bool expected) => (nameof(f.CurrentRoutePreserved), f.CurrentRoutePreserved, expected);
        (string, bool?, bool) Selected(bool expected) => (nameof(f.Selected), f.Selected, expected);
        (string, bool?, bool) Disabled(bool expected) => (nameof(f.Disabled), f.Disabled, expected);
        (string, bool?, bool) Editable(bool expected) => (nameof(f.Editable), f.Editable, expected);
        (string, bool?, bool) Modal(bool expected) => (nameof(f.Modal), f.Modal, expected);
        (string, bool?, bool) Restored(bool expected) => (nameof(f.FocusRestored), f.FocusRestored, expected);
        return row switch
        {
            1 or 3 or 8 => [Empty(true), Failure(false)],
            2 or 7 or 27 or 30 => [(nameof(f.LoadingVisible), f.LoadingVisible, true)],
            4 or 9 => [(nameof(f.DataVisible), f.DataVisible, true), Empty(false), Failure(false)],
            5 or 6 or 20 or 22 or 28 or 38 => [Failure(true)],
            10 or 11 or 12 or 13 or 14 or 16 or 17 or 18 or 23 => [Cue(true)],
            19 or 21 => [Cue(true), Failure(false)],
            15 => [Empty(true), Filters(true), Failure(false)],
            24 => [Cue(true), Polling(true), Failure(true)],
            25 => [Failure(true), (nameof(f.OriginalOperationVisible), f.OriginalOperationVisible, true)],
            26 => [Failure(true), Polling(false)],
            29 or 42 => [Failure(true), Route(true)],
            31 => [Selected(true), Disabled(false), (nameof(f.PanelVisible), f.PanelVisible, true), (nameof(f.PanelOwnedByTab), f.PanelOwnedByTab, true)],
            32 => [Selected(true), Disabled(false), (nameof(f.PanelVisible), f.PanelVisible, true), (nameof(f.PanelOwnedByTab), f.PanelOwnedByTab, true),
                (nameof(f.InvalidTargetSelected), f.InvalidTargetSelected, false)],
            33 => [Disabled(true)],
            34 => [Failure(true), Empty(false)],
            35 => [Failure(true), Cue(true)],
            36 => [Editable(true)],
            37 => [Failure(true), Editable(true)],
            39 or 45 => [(nameof(f.InFlow), f.InFlow, true), Modal(false), Editable(true)],
            40 or 43 or 44 => [Modal(true)],
            41 => [Empty(true), Editable(true)],
            46 => [Restored(true), Route(true)],
            47 => [Restored(true), Route(true), Disabled(false)],
            48 => [(nameof(f.UpdatedCueVisible), f.UpdatedCueVisible, true)],
            49 => [(nameof(f.DetailHidden), f.DetailHidden, true), Filters(true)],
            _ => [("Row", null, true)],
        };
    }

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
