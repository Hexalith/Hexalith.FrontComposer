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
        : this(reason, resolution, details: null, problem: null) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRejectedException"/> class.
    /// </summary>
    /// <param name="reason">The domain-specific reason for rejection. Becomes <see cref="Exception.Message"/>.</param>
    /// <param name="resolution">User-facing guidance on how to recover from the rejection.</param>
    /// <param name="details">Optional typed rejection metadata.</param>
    public CommandRejectedException(string reason, string resolution, CommandRejectionDetails? details)
        : this(reason, resolution, details, problem: null) {
    }

    /// <summary>
    /// Creates a rejection from the bounded, plain-text problem projection so support-safe field
    /// maps remain available to generated forms without making the legacy nullable-details
    /// constructor ambiguous for callers that pass <see langword="null"/>.
    /// </summary>
    /// <param name="reason">The domain-specific rejection reason.</param>
    /// <param name="resolution">Operator-facing recovery guidance.</param>
    /// <param name="problem">The bounded problem projection.</param>
    /// <returns>A rejection carrying the bounded problem projection.</returns>
    public static CommandRejectedException FromProblem(
        string reason,
        string resolution,
        ProblemDetailsPayload problem) {
#if NETSTANDARD2_0
        if (problem is null) {
            throw new ArgumentNullException(nameof(problem));
        }
#else
        ArgumentNullException.ThrowIfNull(problem);
#endif

        return new CommandRejectedException(reason, resolution, problem.RejectionDetails, problem);
    }

    private CommandRejectedException(
        string reason,
        string resolution,
        CommandRejectionDetails? details,
        ProblemDetailsPayload? problem)
        : base(reason) {
        Resolution = resolution;
        Details = details ?? CommandRejectionDetails.FromOptional(
            errorCode: null,
            reasonCategory: null,
            suggestedAction: null,
            docsCode: null,
            fallbackSuggestedAction: resolution);
        Problem = (problem ?? ProblemDetailsPayload.Empty) with { RejectionDetails = Details };
    }

    /// <summary>
    /// Gets user-facing guidance that describes how to recover from the rejection.
    /// </summary>
    public string Resolution { get; }

    /// <summary>
    /// Gets typed plain-text metadata for the rejection.
    /// </summary>
    public CommandRejectionDetails Details { get; }

    /// <summary>
    /// Gets the bounded problem projection, including support-safe field and form error maps.
    /// </summary>
    public ProblemDetailsPayload Problem { get; }

    /// <summary>Gets the stable rejection code.</summary>
    public string ErrorCode => Details.ErrorCode;

    /// <summary>Gets the rejection reason category.</summary>
    public string ReasonCategory => Details.ReasonCategory;

    /// <summary>Gets the suggested operator action.</summary>
    public string SuggestedAction => Details.SuggestedAction;

    /// <summary>Gets the associated documentation code.</summary>
    public string DocsCode => Details.DocsCode;
}
