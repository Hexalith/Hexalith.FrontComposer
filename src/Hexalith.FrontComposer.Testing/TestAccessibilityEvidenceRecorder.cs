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
        ValidateMeasurements(row, measurements);
        AccessibilityEvidence evidence = new(row, _options.TimeProvider.GetUtcNow(), context,
            RedactedEvidenceFormatter.FormatAccessibility(measurements, _options));
        _evidence.Enqueue(evidence);
        while (_evidence.Count > Math.Max(0, _options.MaxEvidenceRecords) && _evidence.TryDequeue(out _)) { }
        return evidence;
    }

    private static void ValidateMeasurements(AccessibilityEvidenceRow row, AccessibilityMeasurements m)
    {
        double?[] numbers = [m.ViewportWidth, m.ZoomPercent, m.HorizontalOverflow, m.LineHeight, m.ParagraphSpacing,
            m.LetterSpacing, m.WordSpacing, m.ContrastRatio];
        bool finite = numbers.All(value => value is null || double.IsFinite(value.Value))
            && Finite(m.FocusTarget) && Finite(m.FocusIndicator)
            && m.ViewportAndScrollports.All(Finite) && m.Occluders.All(Finite)
            && m.Targets.All(t => Finite(t.Bounds) && Finite(t.EquivalentControlBounds)
                && (t.Neighbors is null || t.Neighbors.All(Finite)));
        if (!finite) { throw new ArgumentException("AE measurements must contain only finite numbers.", nameof(m)); }
        bool reflow = m.ContentLoss.HasValue && m.Clipping.HasValue && m.OperationsAvailable.HasValue
            && m.ReadingOrderLogical.HasValue && m.SelectedTabVisible.HasValue && m.HorizontalOverflow.HasValue;
        bool complete = row switch
        {
            AccessibilityEvidenceRow.AE01 => m.ViewportWidth.HasValue && reflow,
            AccessibilityEvidenceRow.AE02 => m.ViewportWidth.HasValue && m.ZoomPercent.HasValue && reflow,
            AccessibilityEvidenceRow.AE03 => m.LineHeight.HasValue && m.ParagraphSpacing.HasValue && m.LetterSpacing.HasValue
                && m.WordSpacing.HasValue && m.ContentLoss.HasValue && m.Clipping.HasValue && m.Overlap.HasValue && m.ControlTruncation.HasValue,
            AccessibilityEvidenceRow.AE04 => m.Targets.Count > 0,
            AccessibilityEvidenceRow.AE05 => m.FocusTarget is not null && m.FocusIndicator is not null && m.ViewportAndScrollports.Count > 0,
            AccessibilityEvidenceRow.AE06 => m.ContrastRatio.HasValue,
            AccessibilityEvidenceRow.AE07 => m.ForcedColors.HasValue && m.ForcedColorCuesPreserved.HasValue,
            AccessibilityEvidenceRow.AE08 => m.ReducedMotion.HasValue && m.NonEssentialAnimationsSuppressed.HasValue
                && m.StateCuesPreserved.HasValue && m.FocusChangesImmediate.HasValue && m.ExpirySilent.HasValue,
            AccessibilityEvidenceRow.AE09 => m.Wcag22Aa.HasValue && m.FindingCount.HasValue && m.CriticalFindingCount.HasValue
                && m.AnnouncementCount.HasValue && m.KeyboardPassed.HasValue && m.AxePassed.HasValue && m.NoHoverOnlyAction.HasValue,
            _ => false,
        };
        bool contradicted = m.Passed && (m.ContentLoss == true || m.Clipping == true || m.AxePassed == false
            || m.KeyboardPassed == false || m.CriticalFindingCount > 0);
        if (contradicted) { throw new ArgumentException("AE evidence cannot be Passed when its measured outcomes failed.", nameof(m)); }
        if (!complete) { throw new ArgumentException("AE evidence is missing required measured setup, geometry, or outcomes.", nameof(m)); }
    }

    private static bool Finite(UxRect? rect) => rect is null || double.IsFinite(rect.X) && double.IsFinite(rect.Y)
        && double.IsFinite(rect.Width) && double.IsFinite(rect.Height);

    /// <summary>Clears retained evidence.</summary>
    public void Reset()
    {
        while (_evidence.TryDequeue(out _)) { }
    }
}
