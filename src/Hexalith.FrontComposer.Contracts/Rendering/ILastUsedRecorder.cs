namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 2-2 Decision D28 — abstraction consumed by the generated per-command
/// <c>{CommandTypeName}LastUsedSubscriber</c> so the subscriber lives in the command's assembly
/// (domain-pure) without taking a hard reference on Shell. Shell's <c>LastUsedValueProvider</c>
/// implements this interface.
/// </summary>
public interface ILastUsedRecorder {
    /// <summary>Records the command's public, non-system properties under the current (tenant, user) pair.</summary>
    /// <typeparam name="TCommand">The command CLR type whose values are being recorded.</typeparam>
    /// <param name="command">The command instance to record. Implementations MUST throw <see cref="ArgumentNullException"/> when null.</param>
    /// <param name="cancellationToken">
    /// Token observed during the persistence write so circuit teardown / tenant switch can abort an in-flight write.
    /// Cancellation observed AFTER the unauthenticated no-op gate but BEFORE the per-property storage loop;
    /// once the loop starts, cancellation between writes leaves storage partially updated (best-effort, non-transactional).
    /// </param>
    /// <returns>A task that completes when the recording attempt finishes (no-op when the (tenant, user) context is unauthenticated per D31).</returns>
    Task RecordAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;
}
