namespace Hexalith.FrontComposer.Testing;

/// <summary>AE-04 evidence using local ordinal references rather than names, locators, or personal data.</summary>
/// <param name="TargetNumber">Target's ordinal in the caller's semantic inventory.</param>
/// <param name="Bounds">Measured pointer target bounds.</param>
/// <param name="KeyboardPath">Keys used to reach and activate this target from the surface entry.</param>
/// <param name="Exception">Exactly one supported exception for an undersized target, or null.</param>
/// <param name="Rationale">Structured reason for the exception, or null.</param>
/// <param name="Neighbors">All measured neighboring target bounds for a spacing exception; an empty collection is measured empty and null is unmeasured.</param>
/// <param name="EquivalentControlNumber">Ordinal of a separate control with the same function.</param>
/// <param name="EquivalentControlBounds">Measured conforming equivalent control bounds.</param>
public sealed record AccessibilityTargetEvidence(int TargetNumber, UxRect Bounds, IReadOnlyList<UxKeyboardKey> KeyboardPath,
    UxTargetSizeExceptionKind? Exception = null, UxTargetSizeRationale? Rationale = null,
    IReadOnlyList<UxRect>? Neighbors = null, int? EquivalentControlNumber = null, UxRect? EquivalentControlBounds = null);
