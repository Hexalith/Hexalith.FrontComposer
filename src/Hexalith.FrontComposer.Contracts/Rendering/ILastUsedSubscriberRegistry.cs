namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Activates generated per-command last-used subscribers on demand so generated form components
/// can remain domain-pure and avoid a hard dependency on the Shell assembly.
/// </summary>
public interface ILastUsedSubscriberRegistry {
    /// <summary>
    /// Ensures the generated subscriber is resolved and subscribed for the current scope.
    /// Subsequent calls for the same subscriber type are no-ops.
    /// </summary>
    void Ensure<TSubscriber>() where TSubscriber : class, IDisposable;
}
