namespace Hexalith.FrontComposer.Testing;

/// <summary>OF/FM origin capture, fallback, and AE-05 measured geometry assertions.</summary>
public static class UxFocusAssertions
{
    /// <summary>Captures the connected enabled active element immediately before shortcut/pointer activation.</summary>
    public static UxFocusTarget CaptureOrigin(UxFocusTarget activeElement)
    {
        ArgumentNullException.ThrowIfNull(activeElement);
        if (!Usable(activeElement) || string.IsNullOrWhiteSpace(activeElement.Role)
            || string.IsNullOrWhiteSpace(activeElement.Name) || activeElement.TagName.Equals("body", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OF-01..04 origin must be a connected, enabled, named active control.");
        }
        return activeElement;
    }

    /// <summary>Asserts the direct origin or route h1 (FM-04/05), or named form heading (FM-11), after close/Stay/Escape.</summary>
    public static void AssertRestored(UxFocusTarget captured, UxFocusTarget? currentOrigin,
        UxFocusTarget actual, UxFocusTarget fallback, string matrixRow)
    {
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(fallback);
        if (matrixRow is not ("FM-04" or "FM-05" or "FM-11"))
        {
            throw new ArgumentOutOfRangeException(nameof(matrixRow));
        }
        bool originUsable = HasUsableOrigin(currentOrigin, captured);
        UxFocusTarget expected = ResolveTarget(currentOrigin, captured, fallback);
        ArgumentNullException.ThrowIfNull(expected);
        bool validFallback = originUsable || fallback.Role == "heading"
            && (matrixRow == "FM-11" || fallback.TagName.Equals("h1", StringComparison.OrdinalIgnoreCase));
        if (!validFallback || !Usable(actual) || !Usable(expected)
            || actual.TagName.Equals("body", StringComparison.OrdinalIgnoreCase)
            || !ReferenceEquals(actual.Handle, expected.Handle) || actual.Role != expected.Role || actual.Name != expected.Name
            || string.IsNullOrWhiteSpace(actual.Name))
        {
            throw new InvalidOperationException($"{matrixRow} focus did not return to the captured origin or required heading fallback.");
        }
    }

    /// <summary>Asserts that the entire target and painted indicator fit every viewport/scrollport and intersect no app-owned occluder.</summary>
    public static void AssertUnobscured(UxRect target, UxRect focusIndicator,
        IReadOnlyList<UxRect> viewportAndScrollports, IReadOnlyList<UxRect> occluders)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(focusIndicator);
        ArgumentNullException.ThrowIfNull(viewportAndScrollports);
        ArgumentNullException.ThrowIfNull(occluders);
        if (viewportAndScrollports.Count == 0 || !Valid(target) || !Valid(focusIndicator)
            || viewportAndScrollports.Any(r => !Valid(r) || !Contains(r, target) || !Contains(r, focusIndicator))
            || occluders.Any(r => !Valid(r) || Intersects(r, target) || Intersects(r, focusIndicator)))
        {
            string evidence = RedactedEvidenceFormatter.FormatAccessibility(new()
            {
                FocusTarget = target, FocusIndicator = focusIndicator,
                ViewportAndScrollports = viewportAndScrollports, Occluders = occluders,
            }, new() { MaxDiagnosticPayloadCharacters = 2_048 });
            throw new InvalidOperationException("AE-05/FM focus target or full painted indicator is clipped, obscured, or has invalid geometry. " + evidence);
        }
    }

    private static bool HasUsableOrigin(UxFocusTarget? origin, UxFocusTarget captured)
        => origin is not null && ReferenceEquals(origin.Handle, captured.Handle) && Usable(origin);
    private static UxFocusTarget ResolveTarget(UxFocusTarget? origin, UxFocusTarget captured, UxFocusTarget fallback)
        => origin is not null && HasUsableOrigin(origin, captured) ? origin : fallback;
    private static bool Usable(UxFocusTarget? target) => target is not null && target.Handle is not null && target.Connected && target.Enabled && target.Visible;
    private static bool Valid(UxRect r) => double.IsFinite(r.X) && double.IsFinite(r.Y)
        && double.IsFinite(r.Width) && double.IsFinite(r.Height) && r.Width > 0 && r.Height > 0
        && double.IsFinite(r.X + r.Width) && double.IsFinite(r.Y + r.Height);
    private static bool Contains(UxRect outer, UxRect inner) => inner.X >= outer.X && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width && inner.Y + inner.Height <= outer.Y + outer.Height;
    private static bool Intersects(UxRect a, UxRect b) => a.X < b.X + b.Width && a.X + a.Width > b.X
        && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
}
