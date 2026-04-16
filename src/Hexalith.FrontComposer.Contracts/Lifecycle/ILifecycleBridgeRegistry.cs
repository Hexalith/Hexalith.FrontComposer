namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Activates generated per-command lifecycle bridges on demand so generated form components can
/// remain domain-pure and avoid a hard dependency on the Shell assembly (Story 2-3 Decision D5).
/// </summary>
/// <remarks>
/// Implementations MUST be thread-safe and idempotent. Two concurrent renders calling
/// <see cref="Ensure{TBridge}"/> for the same bridge type MUST resolve and subscribe at most one
/// instance; otherwise each Fluxor action would be forwarded twice and
/// <see cref="ILifecycleStateService.Transition(string, CommandLifecycleState, string?)"/>
/// would double-count outcome notifications.
/// </remarks>
public interface ILifecycleBridgeRegistry {
    /// <summary>
    /// Ensures the generated bridge is resolved and subscribed for the current scope. Subsequent
    /// calls for the same bridge type are no-ops. Thread-safe.
    /// </summary>
    /// <typeparam name="TBridge">The generated bridge type.</typeparam>
    void Ensure<TBridge>() where TBridge : class, IDisposable;
}
