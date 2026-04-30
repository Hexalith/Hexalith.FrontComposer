namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Thrown when a Level 3 slot selector is not a direct projection property access.
/// </summary>
public sealed class ProjectionSlotSelectorException : ArgumentException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    /// <param name="message">Deterministic teaching diagnostic message.</param>
    /// <param name="paramName">The invalid parameter name.</param>
    public ProjectionSlotSelectorException(string message, string? paramName = "field")
        : base(message, paramName) {
    }
}
