namespace Hexalith.FrontComposer.Testing;

/// <summary>Safe journey provenance using caller-maintained ordinal references, never names, routes, or sensitive identifiers.</summary>
/// <param name="SurfaceNumber">Positive ordinal identifying the measured surface in the adopter's inventory.</param>
/// <param name="StateNumber">Positive ordinal identifying the measured journey/state in that inventory.</param>
/// <param name="Theme">Typed active theme.</param>
public sealed record AccessibilityEvidenceContext(int SurfaceNumber, int StateNumber, UxTheme Theme);
