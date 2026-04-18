namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a command class as destructive, requiring a pre-submit confirmation dialog
/// (Story 2-5 Decision D1 / ADR-026). Opt-in classification — name heuristics
/// (Delete*/Remove*/Purge*) surface an advisory analyzer diagnostic (HFC1020)
/// but are never the sole signal. UX-DR37 / UX-DR58.
/// </summary>
/// <remarks>
/// The attribute is consumed at build time by <c>CommandParser</c>; Story 2-5 D2 places
/// the runtime confirmation surface in <c>CommandRendererEmitter</c> (pre-submit gate)
/// — <c>FcLifecycleWrapper</c> remains post-submit only.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DestructiveAttribute : Attribute {
    /// <summary>
    /// Gets or sets the optional confirmation dialog title override. When <see langword="null"/>,
    /// the renderer falls back to <c>{DisplayLabel}?</c> (humanized command name).
    /// </summary>
    public string? ConfirmationTitle { get; init; }

    /// <summary>
    /// Gets or sets the optional confirmation dialog body override. When <see langword="null"/>,
    /// the renderer uses a localized default ("This action cannot be undone.").
    /// </summary>
    public string? ConfirmationBody { get; init; }
}
