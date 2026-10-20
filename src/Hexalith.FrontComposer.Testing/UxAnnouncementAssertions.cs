namespace Hexalith.FrontComposer.Testing;

/// <summary>Exact AM sequence, channel, and fake-time coalescing assertions with support-safe diagnostics.</summary>
public static class UxAnnouncementAssertions
{
    /// <summary>Asserts exact ordered messages, count, operation/state identities, and speech channels.</summary>
    public static void AssertSequence(IReadOnlyList<UxAnnouncement> actual, params UxAnnouncement[] expected)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);
        int count = Math.Max(actual.Count, expected.Length);
        for (int i = 0; i < count; i++)
        {
            UxAnnouncement? wanted = i < expected.Length ? expected[i] : null;
            UxAnnouncement? observed = i < actual.Count ? actual[i] : null;
            if (wanted != observed)
            {
                // Never include copy, names, result IDs, or backend data from the observed value; name only the differing fields.
                string differing = wanted is null || observed is null ? "Presence" : string.Join(", ", new (string Name, bool Same)[] {
                    (nameof(UxAnnouncement.RowId), wanted.RowId == observed.RowId), (nameof(UxAnnouncement.Channel), wanted.Channel == observed.Channel),
                    (nameof(UxAnnouncement.Copy), wanted.Copy == observed.Copy), (nameof(UxAnnouncement.OperationId), wanted.OperationId == observed.OperationId),
                    (nameof(UxAnnouncement.StateIdentity), wanted.StateIdentity == observed.StateIdentity) }.Where(f => !f.Same).Select(f => f.Name));
                throw new InvalidOperationException($"Announcement mismatch at {i}: expected {SafeLabel(wanted)}; observed {SafeLabel(observed)}; differing {differing}. Counts {expected.Length}/{actual.Count}.");
            }
        }
        AssertDistinctChannels(actual);
    }

    /// <summary>Rejects duplicate speech and live-plus-focus paths for the same operation/state event.</summary>
    public static void AssertDistinctChannels(IReadOnlyList<UxAnnouncement> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        HashSet<(string Operation, string State)> seen = [];
        HashSet<string> terminalOperations = new(StringComparer.Ordinal);
        foreach (UxAnnouncement item in events)
        {
            ValidateRowChannel(item);
            bool terminal = item.RowId is "AM-13" or "AM-14" or "AM-15" or "AM-16" or "AM-19" or "AM-24";
            if (!seen.Add((item.OperationId, item.StateIdentity)))
            {
                throw new InvalidOperationException($"Duplicate speech event or wrong channel: {SafeLabel(item)} repeats one operation/state event.");
            }
            if (terminal && !terminalOperations.Add(item.OperationId))
            {
                throw new InvalidOperationException($"Conflicting terminal speech: {SafeLabel(item)} is a second terminal outcome for one operation.");
            }
        }
    }

    /// <summary>Asserts role/name before channel attributes; focus summaries/headings must have no live attributes.</summary>
    public static void AssertChannel(UxAnnouncement expected, UxAccessibilitySnapshot node, string accessibleName, int speechOwnerCount = 1, int inheritedResultCountSpeechCount = 0)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(accessibleName);
        ValidateRowChannel(expected);
        bool focused = expected.Channel is UxAnnouncementChannel.FocusedSummary or UxAnnouncementChannel.FocusedHeading;
        string role = expected.Channel switch
        {
            UxAnnouncementChannel.FocusedSummary => "group",
            UxAnnouncementChannel.FocusedHeading => "heading",
            _ => "status",
        };
        // role=status is implicitly polite and atomic; an explicit attribute still overrides it.
        string? live = node.AriaLive ?? (node.Role == "status" ? "polite" : null);
        string? atomic = node.AriaAtomic ?? (node.Role == "status" ? "true" : null);
        List<string> failed = [];
        void Check(bool passed, string check)
        {
            if (!passed) { failed.Add(check); }
        }
        Check(node.Role == role, "Role");
        Check(node.Name == accessibleName && (!focused || !string.IsNullOrWhiteSpace(accessibleName)), "Name");
        Check(expected.Channel != UxAnnouncementChannel.FocusedHeading || accessibleName == expected.Copy, "HeadingCopy");
        Check(node.IsFocused == focused, "Focus");
        Check(focused ? node.AriaLive is null : live == "polite", "AriaLive");
        Check(focused ? node.AriaAtomic is null : expected.Channel != UxAnnouncementChannel.PoliteComboboxStatus || atomic == "true", "AriaAtomic");
        Check(!focused || node.TabIndex == "-1", "TabIndex");
        Check(speechOwnerCount == 1, "SpeechOwnerCount");
        Check(inheritedResultCountSpeechCount == 0, "InheritedResultCountSpeech");
        if (failed.Count > 0)
        {
            throw new InvalidOperationException($"Role/name or speech channel mismatch for {SafeLabel(expected)}: {string.Join(", ", failed)}.");
        }
    }

    /// <summary>Checks the current trailing window at 249/250 ms, last eligible result, and exact prior sequence.</summary>
    public static void AssertCoalescingBoundary(TestTimeProvider clock, Func<IReadOnlyList<UxAnnouncement>> observe,
        UxAnnouncement lastEligible, params UxAnnouncement[] prior)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(observe);
        clock.Advance(UxTiming.CoalescingWindow - TimeSpan.FromMilliseconds(1));
        AssertSequence(observe(), prior);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        AssertSequence(observe(), [.. prior, lastEligible]);
    }

    /// <summary>Checks immediate terminal/summary/navigation-failure delivery and cancellation through the pending window.</summary>
    public static void AssertImmediateCancellation(TestTimeProvider clock, Func<IReadOnlyList<UxAnnouncement>> observe,
        UxAnnouncement decisive, params UxAnnouncement[] prior)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(observe);
        AssertSequence(observe(), [.. prior, decisive]);
        clock.Advance(UxTiming.CoalescingWindow);
        AssertSequence(observe(), [.. prior, decisive]);
    }

    private static void ValidateRowChannel(UxAnnouncement item)
    {
        UxAnnouncementChannel canonical = item.RowId switch
        {
            "AM-18" or "AM-19" => UxAnnouncementChannel.FocusedSummary,
            "AM-26" or "AM-29" => UxAnnouncementChannel.FocusedHeading,
            "AM-28" => UxAnnouncementChannel.PoliteComboboxStatus,
            _ => UxAnnouncementChannel.PoliteStatus,
        };
        if (item.Channel != canonical || item.RowId is not { Length: 5 }
            || !item.RowId.StartsWith("AM-", StringComparison.Ordinal)
            || item.RowId[3] is < '0' or > '9' || item.RowId[4] is < '0' or > '9'
            || !int.TryParse(item.RowId.AsSpan(3), out int number) || number is < 1 or > 31)
        {
            throw new InvalidOperationException($"Canonical AM row/channel mismatch: {SafeLabel(item)}.");
        }
    }

    private static string SafeLabel(UxAnnouncement? item)
    {
        string row = item?.RowId is { Length: 5 } id && id.StartsWith("AM-", StringComparison.Ordinal)
            && id[3] is >= '0' and <= '9' && id[4] is >= '0' and <= '9'
            && int.TryParse(id.AsSpan(3), out int number) && number is >= 1 and <= 31 ? id : "<unknown AM row>";
        return item is null ? "<missing>" : $"{row}/{(Enum.IsDefined(item.Channel) ? item.Channel.ToString() : "<unknown channel>")}";
    }
}
