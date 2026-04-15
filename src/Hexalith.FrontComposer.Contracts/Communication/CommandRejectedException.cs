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
        : base(reason) {
        Resolution = resolution;
    }

    /// <summary>
    /// Gets user-facing guidance that describes how to recover from the rejection.
    /// </summary>
    public string Resolution { get; }
}
