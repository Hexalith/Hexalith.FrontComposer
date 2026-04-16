namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Activates generated per-command last-used subscribers on demand so generated form components
/// can remain domain-pure and avoid a hard dependency on the Shell assembly.
/// </summary>
/// <remarks>
/// Implementations MUST be thread-safe and idempotent. Two concurrent renders calling
/// <see cref="Ensure{TSubscriber}"/> for the same subscriber type MUST resolve and subscribe
/// at most one instance; otherwise the <c>Confirmed</c> lifecycle event would fire twice and
/// double-write to per-user persistence.
/// </remarks>
public interface ILastUsedSubscriberRegistry {
    /// <summary>
    /// Ensures the generated subscriber is resolved and subscribed for the current scope.
    /// Subsequent calls for the same subscriber type are no-ops. Thread-safe.
    /// </summary>
    /// <typeparam name="TSubscriber">The generated subscriber type.</typeparam>
    void Ensure<TSubscriber>() where TSubscriber : class, IDisposable;
}
