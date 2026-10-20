using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Records bounded AE-01..09 measurements through the existing evidence formatter and configured clock.</summary>
public sealed class TestAccessibilityEvidenceRecorder
{
    private readonly ConcurrentQueue<AccessibilityEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    /// <summary>Creates a recorder using the same options as the adopter test host.</summary>
    public TestAccessibilityEvidenceRecorder(FrontComposerTestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Gets an immutable copy of retained evidence.</summary>
    public IReadOnlyList<AccessibilityEvidence> Evidence => [.. _evidence];

    /// <summary>Records measured numeric/boolean facts. The fixed schema retains target exceptions, rationale, and keyboard paths without arbitrary text.</summary>
    public AccessibilityEvidence Record(AccessibilityEvidenceRow row, AccessibilityEvidenceContext context, AccessibilityMeasurements measurements)
    {
        if (!Enum.IsDefined(row)) { throw new ArgumentOutOfRangeException(nameof(row)); }
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(measurements);
        if (context.SurfaceNumber < 1 || context.StateNumber < 1 || !Enum.IsDefined(context.Theme)
            || row == AccessibilityEvidenceRow.AE06 && context.Theme is not (UxTheme.Light or UxTheme.Dark))
        {
            throw new ArgumentException("AE evidence requires safe positive surface/state ordinals and a supported active theme.", nameof(context));
        }
        ValidateMeasurements(row, context.Theme, measurements);
        AccessibilityEvidence evidence = new(row, _options.TimeProvider.GetUtcNow(), context,
            RedactedEvidenceFormatter.FormatAccessibility(measurements, _options));
        _evidence.Enqueue(evidence);
        while (_evidence.Count > Math.Max(0, _options.MaxEvidenceRecords) && _evidence.TryDequeue(out _)) { }
        return evidence;
    }

    private static void ValidateMeasurements(AccessibilityEvidenceRow row, UxTheme theme, AccessibilityMeasurements m)
    {
        double?[] numbers = [m.ViewportWidth, m.ZoomPercent, m.HorizontalOverflow, m.LineHeight, m.ParagraphSpacing,
            m.LetterSpacing, m.WordSpacing, m.ContrastRatio];
        bool finite = numbers.All(value => value is null || double.IsFinite(value.Value))
            && Finite(m.FocusTarget) && Finite(m.FocusIndicator)
            && m.ViewportAndScrollports.All(Finite) && m.Occluders.All(Finite)
            && m.Targets.All(t => Finite(t.Bounds) && Finite(t.EquivalentControlBounds)
                && (t.Neighbors is null || t.Neighbors.All(Finite)));
        if (!finite) { throw new ArgumentException("AE measurements must contain only finite numbers.", nameof(m)); }
        List<string> ordinalFailures = [];
        if (m.Targets.Any(t => t.TargetNumber < 1)) { ordinalFailures.Add("Targets.TargetNumber"); }
        if (m.Targets.Any(t => t.EquivalentControlNumber is <= 0)) { ordinalFailures.Add("Targets.EquivalentControlNumber"); }
        if (ordinalFailures.Count > 0)
        {
            throw new ArgumentException("AE target evidence requires positive target and equivalent-control ordinals: "
                + string.Join(", ", ordinalFailures) + ".", nameof(m));
        }
        // Setup and missing-fact diagnostics expose only fixed schema names, never caller values.
        List<string> failed = [];
        void Check(bool passed, string fact)
        {
            if (!passed) { failed.Add(fact); }
        }
        void CheckReflow()
        {
            Check(m.ContentLoss.HasValue, nameof(m.ContentLoss));
            Check(m.Clipping.HasValue, nameof(m.Clipping));
            Check(m.OperationsAvailable.HasValue, nameof(m.OperationsAvailable));
            Check(m.ReadingOrderLogical.HasValue, nameof(m.ReadingOrderLogical));
            Check(m.SelectedTabVisible.HasValue, nameof(m.SelectedTabVisible));
            Check(m.HorizontalOverflow.HasValue, nameof(m.HorizontalOverflow));
        }
        Check(m.ForcedColors is not { } forced || forced == (theme == UxTheme.ForcedColors), nameof(m.ForcedColors));
        switch (row)
        {
            case AccessibilityEvidenceRow.AE01:
            case AccessibilityEvidenceRow.AE02:
                Check(m.ViewportWidth is > 0 and <= 320, nameof(m.ViewportWidth));
                if (row == AccessibilityEvidenceRow.AE02) { Check(m.ZoomPercent >= 400, nameof(m.ZoomPercent)); }
                CheckReflow();
                break;
            case AccessibilityEvidenceRow.AE03:
                Check(m.LineHeight >= 1.5, nameof(m.LineHeight));
                Check(m.ParagraphSpacing >= 2, nameof(m.ParagraphSpacing));
                Check(m.LetterSpacing >= .12, nameof(m.LetterSpacing));
                Check(m.WordSpacing >= .16, nameof(m.WordSpacing));
                Check(m.ContentLoss.HasValue, nameof(m.ContentLoss));
                Check(m.Clipping.HasValue, nameof(m.Clipping));
                Check(m.Overlap.HasValue, nameof(m.Overlap));
                Check(m.ControlTruncation.HasValue, nameof(m.ControlTruncation));
                break;
            case AccessibilityEvidenceRow.AE04:
                Check(m.Targets.Count > 0, nameof(m.Targets));
                break;
            case AccessibilityEvidenceRow.AE05:
                Check(m.FocusTarget is not null, nameof(m.FocusTarget));
                Check(m.FocusIndicator is not null, nameof(m.FocusIndicator));
                Check(m.ViewportAndScrollports.Count > 0, nameof(m.ViewportAndScrollports));
                break;
            case AccessibilityEvidenceRow.AE06:
                Check(m.ContrastRatio.HasValue, nameof(m.ContrastRatio));
                break;
            case AccessibilityEvidenceRow.AE07:
                Check(m.ForcedColors == true, nameof(m.ForcedColors));
                Check(m.ForcedColorCuesPreserved.HasValue, nameof(m.ForcedColorCuesPreserved));
                break;
            case AccessibilityEvidenceRow.AE08:
                Check(m.ReducedMotion == true, nameof(m.ReducedMotion));
                Check(m.NonEssentialAnimationsSuppressed.HasValue, nameof(m.NonEssentialAnimationsSuppressed));
                Check(m.StateCuesPreserved.HasValue, nameof(m.StateCuesPreserved));
                Check(m.FocusChangesImmediate.HasValue, nameof(m.FocusChangesImmediate));
                Check(m.ExpirySilent.HasValue, nameof(m.ExpirySilent));
                break;
            case AccessibilityEvidenceRow.AE09:
                Check(m.Wcag22Aa.HasValue, nameof(m.Wcag22Aa));
                Check(m.FindingCount.HasValue, nameof(m.FindingCount));
                Check(m.CriticalFindingCount.HasValue, nameof(m.CriticalFindingCount));
                Check(m.AnnouncementCount.HasValue, nameof(m.AnnouncementCount));
                Check(m.KeyboardPassed.HasValue, nameof(m.KeyboardPassed));
                Check(m.AxePassed.HasValue, nameof(m.AxePassed));
                Check(m.NoHoverOnlyAction.HasValue, nameof(m.NoHoverOnlyAction));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(row));
        }
        if (failed.Count > 0)
        {
            throw new ArgumentException("AE evidence is missing required measured facts or was measured under another row's setup or theme: "
                + string.Join(", ", failed.Distinct(StringComparer.Ordinal)) + ".", nameof(m));
        }
        if (!m.Passed) { return; }
        Check(m.ContentLoss != true, nameof(m.ContentLoss));
        Check(m.Clipping != true, nameof(m.Clipping));
        Check(m.Overlap != true, nameof(m.Overlap));
        Check(m.ControlTruncation != true, nameof(m.ControlTruncation));
        Check(!(m.HorizontalOverflow > 0), nameof(m.HorizontalOverflow));
        Check(m.OperationsAvailable != false, nameof(m.OperationsAvailable));
        Check(m.ReadingOrderLogical != false, nameof(m.ReadingOrderLogical));
        Check(m.SelectedTabVisible != false, nameof(m.SelectedTabVisible));
        Check(m.ForcedColorCuesPreserved != false, nameof(m.ForcedColorCuesPreserved));
        Check(m.NonEssentialAnimationsSuppressed != false, nameof(m.NonEssentialAnimationsSuppressed));
        Check(m.StateCuesPreserved != false, nameof(m.StateCuesPreserved));
        Check(m.FocusChangesImmediate != false, nameof(m.FocusChangesImmediate));
        Check(m.ExpirySilent != false, nameof(m.ExpirySilent));
        Check(m.KeyboardPassed != false, nameof(m.KeyboardPassed));
        Check(m.AxePassed != false, nameof(m.AxePassed));
        Check(m.NoHoverOnlyAction != false, nameof(m.NoHoverOnlyAction));
        Check(m.Wcag22Aa != false, nameof(m.Wcag22Aa));
        Check(!(m.CriticalFindingCount > 0), nameof(m.CriticalFindingCount));
        if (row == AccessibilityEvidenceRow.AE06) { Check(m.ContrastRatio >= 3, nameof(m.ContrastRatio)); }
        if (row == AccessibilityEvidenceRow.AE04)
        {
            for (int i = 0; i < m.Targets.Count; i++)
            {
                failed.AddRange(UxAccessibilityAssertions.Failures(m.Targets[i]).Select(fact => $"Targets[{i + 1}].{fact}"));
            }
        }
        if (row == AccessibilityEvidenceRow.AE05)
        {
            Check(UxFocusAssertions.IsUnobscured(m.FocusTarget!, m.FocusIndicator!, m.ViewportAndScrollports, m.Occluders),
                "FocusGeometry(FocusTarget, FocusIndicator, ViewportAndScrollports, Occluders)");
        }
        if (failed.Count > 0)
        {
            throw new ArgumentException("AE evidence cannot be Passed when its measured outcomes failed: " + string.Join(", ", failed) + ".", nameof(m));
        }
    }

    private static bool Finite(UxRect? rect) => rect is null || double.IsFinite(rect.X) && double.IsFinite(rect.Y)
        && double.IsFinite(rect.Width) && double.IsFinite(rect.Height);

    /// <summary>Clears retained evidence.</summary>
    public void Reset()
    {
        while (_evidence.TryDequeue(out _)) { }
    }
}
