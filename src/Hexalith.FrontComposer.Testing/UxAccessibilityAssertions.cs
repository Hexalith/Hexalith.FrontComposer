namespace Hexalith.FrontComposer.Testing;

/// <summary>AE-04 pointer-target measurements and the five recorded exceptions.</summary>
public static class UxAccessibilityAssertions
{
    /// <summary>Checks 24 by 24 CSS pixels or exactly one supported, evidenced exception with a keyboard path.</summary>
    public static void AssertTargetSize(AccessibilityTargetEvidence target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(target.Bounds);
        ArgumentNullException.ThrowIfNull(target.KeyboardPath);
        List<string> failed = Failures(target);
        if (failed.Count > 0)
        {
            throw new InvalidOperationException("AE-04 target size, exception, rationale, equivalent control, or keyboard path checks failed: "
                + string.Join(", ", failed) + ".");
        }
    }

    /// <summary>Names the failed AE-04 checks without measured values.</summary>
    internal static List<string> Failures(AccessibilityTargetEvidence target)
    {
        bool sized = Valid(target.Bounds) && Sized(target.Bounds);
        bool keyboard = target.KeyboardPath is { Count: > 0 } path && path.All(Enum.IsDefined);
        bool exception = target.Exception switch
        {
            UxTargetSizeExceptionKind.Inline => target.Rationale == UxTargetSizeRationale.InlineText,
            UxTargetSizeExceptionKind.UserAgentControl => target.Rationale == UxTargetSizeRationale.UserAgentOwned,
            UxTargetSizeExceptionKind.Essential => target.Rationale == UxTargetSizeRationale.EssentialPresentation,
            UxTargetSizeExceptionKind.Equivalent => target.Rationale == UxTargetSizeRationale.SameFunctionConformingControl
                && target.EquivalentControlNumber is > 0 && target.EquivalentControlNumber != target.TargetNumber
                && target.EquivalentControlBounds is { } equivalent && Valid(equivalent) && Sized(equivalent),
            UxTargetSizeExceptionKind.Spacing => target.Rationale == UxTargetSizeRationale.SeparatedFromNeighbors
                && target.Neighbors is not null && HasSpacing(target.Bounds, target.Neighbors),
            _ => false,
        };
        List<string> failed = [];
        if (!Valid(target.Bounds)) { failed.Add("Bounds"); }
        if (target.TargetNumber < 1) { failed.Add("TargetNumber"); }
        if (!keyboard) { failed.Add("KeyboardPath"); }
        if (sized && (target.Exception is not null || target.Rationale is not null)) { failed.Add("UnneededException"); }
        if (!sized && !exception) { failed.Add(target.Exception is { } kind && Enum.IsDefined(kind) ? kind + "Exception" : "Exception"); }
        return failed;
    }

    private static bool HasSpacing(UxRect bounds, IReadOnlyList<UxRect> neighbors)
    {
        double x = bounds.X + bounds.Width / 2, y = bounds.Y + bounds.Height / 2;
        return neighbors.All(n => Valid(n) && CircleClear(x, y, n)
            && (Sized(n) || Math.Pow(x - n.X - n.Width / 2, 2) + Math.Pow(y - n.Y - n.Height / 2, 2) >= 24 * 24));
    }
    private static bool CircleClear(double x, double y, UxRect rect)
        => Math.Pow(x - Math.Clamp(x, rect.X, rect.X + rect.Width), 2)
            + Math.Pow(y - Math.Clamp(y, rect.Y, rect.Y + rect.Height), 2) >= 12 * 12;
    private static bool Sized(UxRect r) => r.Width >= 24 && r.Height >= 24;
    private static bool Valid(UxRect? r) => r is not null && double.IsFinite(r.X) && double.IsFinite(r.Y)
        && double.IsFinite(r.Width) && double.IsFinite(r.Height) && r.Width > 0 && r.Height > 0;
}
