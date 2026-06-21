namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Opaque, metadata-only detail for a projection-changed notification received from EventStore.
/// <para>
/// This is FrontComposer's own DTO, deliberately <b>not</b> the EventStore
/// <c>Hexalith.EventStore.Client.Projections.ProjectionChangedDetail</c> type: this Contracts
/// kernel multi-targets <c>netstandard2.0</c> and cannot reference the net10-only EventStore
/// client. The two are <i>wire-compatible</i> (same <c>(type, tenant, scope, metadata)</c> shape
/// on the SignalR <c>ProjectionChangedDetail</c> method), not type-compatible.
/// </para>
/// <para>
/// <see cref="Metadata"/> is opaque to the framework — consumers (for example an AI chat client)
/// interpret it; FrontComposer adds no domain knowledge and treats values as metadata only.
/// </para>
/// </summary>
/// <param name="ProjectionType">The projection type name (kebab-case).</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="GroupScope">
/// The sub-tenant group scope the message was scoped to (for example, a conversation id), or
/// <see langword="null"/> for a tenant-wide notification.
/// </param>
/// <param name="Metadata">Opaque, bounded metadata key/value pairs carried alongside the change.</param>
public sealed record ProjectionChangedDetail(
    string ProjectionType,
    string TenantId,
    string? GroupScope,
    IReadOnlyDictionary<string, string> Metadata);
