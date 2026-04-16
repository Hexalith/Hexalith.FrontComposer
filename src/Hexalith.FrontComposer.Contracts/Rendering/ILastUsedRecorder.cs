namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 2-2 Decision D28 — abstraction consumed by the generated per-command
/// <c>{CommandTypeName}LastUsedSubscriber</c> so the subscriber lives in the command's assembly
/// (domain-pure) without taking a hard reference on Shell. Shell's <c>LastUsedValueProvider</c>
/// implements this interface.
/// </summary>
public interface ILastUsedRecorder {
    /// <summary>Records the command's public, non-system properties under the current (tenant, user) pair.</summary>
    Task RecordAsync<TCommand>(TCommand command) where TCommand : class;
}
