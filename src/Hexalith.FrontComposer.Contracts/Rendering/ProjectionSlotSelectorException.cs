namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Thrown when a Level 3 slot selector is not a direct projection property access.
/// </summary>
public sealed class ProjectionSlotSelectorException : ArgumentException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    public ProjectionSlotSelectorException()
        : base() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    /// <param name="message">Deterministic teaching diagnostic message.</param>
    public ProjectionSlotSelectorException(string message)
        : base(message, "field") {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    /// <param name="message">Deterministic teaching diagnostic message.</param>
    /// <param name="paramName">The invalid parameter name.</param>
    public ProjectionSlotSelectorException(string message, string? paramName)
        : base(message, paramName ?? "field") {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    /// <param name="message">Deterministic teaching diagnostic message.</param>
    /// <param name="innerException">Underlying parsing or expression-tree failure.</param>
    public ProjectionSlotSelectorException(string message, Exception? innerException)
        : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotSelectorException"/> class.
    /// </summary>
    /// <param name="message">Deterministic teaching diagnostic message.</param>
    /// <param name="paramName">The invalid parameter name.</param>
    /// <param name="innerException">Underlying parsing or expression-tree failure.</param>
    public ProjectionSlotSelectorException(string message, string? paramName, Exception? innerException)
        : base(message, paramName ?? "field", innerException) {
    }
}
