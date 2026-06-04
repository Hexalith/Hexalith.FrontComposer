namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Thrown by <see cref="ICommandService"/> implementations when a command fails
/// domain validation. The rejection reason becomes the exception <see cref="Exception.Message"/>,
/// and <see cref="Resolution"/> carries user-facing guidance for recovery.
/// </summary>
public class CommandRejectedException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRejectedException"/> class.
    /// </summary>
    /// <param name="reason">The domain-specific reason for rejection. Becomes <see cref="Exception.Message"/>.</param>
    /// <param name="resolution">User-facing guidance on how to recover from the rejection.</param>
    public CommandRejectedException(string reason, string resolution)
        : this(reason, resolution, details: null) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRejectedException"/> class.
    /// </summary>
    /// <param name="reason">The domain-specific reason for rejection. Becomes <see cref="Exception.Message"/>.</param>
    /// <param name="resolution">User-facing guidance on how to recover from the rejection.</param>
    /// <param name="details">Optional typed rejection metadata.</param>
    public CommandRejectedException(string reason, string resolution, CommandRejectionDetails? details)
        : base(reason) {
        Resolution = resolution;
        Details = details ?? CommandRejectionDetails.FromOptional(
            errorCode: null,
            reasonCategory: null,
            suggestedAction: null,
            docsCode: null,
            fallbackSuggestedAction: resolution);
    }

    /// <summary>
    /// Gets user-facing guidance that describes how to recover from the rejection.
    /// </summary>
    public string Resolution { get; }

    /// <summary>
    /// Gets typed plain-text metadata for the rejection.
    /// </summary>
    public CommandRejectionDetails Details { get; }

    /// <summary>Gets the stable rejection code.</summary>
    public string ErrorCode => Details.ErrorCode;

    /// <summary>Gets the rejection reason category.</summary>
    public string ReasonCategory => Details.ReasonCategory;

    /// <summary>Gets the suggested operator action.</summary>
    public string SuggestedAction => Details.SuggestedAction;

    /// <summary>Gets the associated documentation code.</summary>
    public string DocsCode => Details.DocsCode;
}
