namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>
/// Story 5-2 D6 / T5 — framework-owned warning channel that lets generated forms (and any
/// other Shell consumer) surface 403 / 404 / 429 responses through a MessageBar without
/// overloading <c>FcLifecycleWrapper</c>'s lifecycle states.
/// </summary>
/// <remarks>
/// Implementations are scoped per circuit / per user. Subscribers detach by disposing the
/// returned <see cref="IDisposable"/> token. Warning publication MUST NOT mutate the ETag
/// cache, lifecycle state, or validation state — those are explicitly out of scope per D6.
/// </remarks>
public interface ICommandFeedbackPublisher {
    /// <summary>Publishes a warning notification to all current subscribers.</summary>
    void PublishWarning(CommandFeedbackWarning warning);

    /// <summary>Subscribes to warning notifications. Dispose the returned token to unsubscribe.</summary>
    IDisposable Subscribe(Action<CommandFeedbackWarning> handler);
}
