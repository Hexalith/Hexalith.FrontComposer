using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.DevMode;

/// <summary>
/// Immutable dev-mode read model describing a generated component tree element.
/// </summary>
/// <remarks>
/// This type is produced by generated projection metadata for the development overlay. It is never
/// mutated by adopter code, never persisted across renders, and never read by production builds.
/// </remarks>
/// <param name="AnnotationKey">Stable key for the annotation within the current render epoch.</param>
/// <param name="Convention">Convention descriptor that produced the annotated element.</param>
/// <param name="ContractTypeName">Full contract type name represented by the annotation.</param>
/// <param name="CurrentLevel">Highest effective customization level for this element.</param>
/// <param name="OriginatingProjectionTypeName">Full projection type name that produced the element.</param>
/// <param name="Role">Optional projection role for role-specific output.</param>
/// <param name="FieldAccessor">Optional field accessor when the annotation describes a field seam.</param>
/// <param name="Children">Child component-tree nodes for starter-template emission.</param>
/// <param name="RenderEpoch">Render epoch used to reject stale annotation events.</param>
/// <param name="ComponentTreeContractVersion">Packed component-tree contract version.</param>
/// <param name="DescriptorHash">Descriptor metadata hash used for freshness checks.</param>
/// <param name="SourceComponentIdentity">Generated source component identity used for freshness checks.</param>
/// <param name="StaleReasons">Known stale reasons for this snapshot.</param>
/// <param name="IsUnsupported">Whether this node represents an unsupported field placeholder.</param>
/// <param name="HasActiveOverride">Whether a before/after comparison is meaningful for the node.</param>
/// <param name="DiagnosticId">Optional diagnostic ID associated with the node.</param>
public sealed record ComponentTreeNode(
    string AnnotationKey,
    ConventionDescriptor Convention,
    string ContractTypeName,
    CustomizationLevel CurrentLevel,
    string OriginatingProjectionTypeName,
    ProjectionRole? Role,
    string? FieldAccessor,
    ImmutableArray<ComponentTreeNode> Children,
    long RenderEpoch,
    int ComponentTreeContractVersion,
    string DescriptorHash,
    string SourceComponentIdentity,
    ImmutableArray<ComponentTreeStaleReason> StaleReasons = default,
    bool IsUnsupported = false,
    bool HasActiveOverride = false,
    string? DiagnosticId = null) {
    /// <summary>Gets the child component-tree nodes for starter-template emission.</summary>
    public ImmutableArray<ComponentTreeNode> Children { get; init; } = Children.IsDefault
        ? ImmutableArray<ComponentTreeNode>.Empty
        : Children;

    /// <summary>Gets the known stale reasons for this snapshot.</summary>
    public ImmutableArray<ComponentTreeStaleReason> StaleReasons { get; init; } = StaleReasons.IsDefault
        ? ImmutableArray<ComponentTreeStaleReason>.Empty
        : StaleReasons;

    /// <summary>Gets a value indicating whether this node is stale.</summary>
    public bool IsStale => !StaleReasons.IsDefaultOrEmpty;

    /// <summary>Gets a value indicating whether current starter-template emission is allowed.</summary>
    public bool CanEmitStarterTemplate => !IsStale && CurrentLevel >= CustomizationLevel.Level2;

    /// <summary>
    /// Evaluates whether this node is stale compared with currently running descriptor metadata.
    /// </summary>
    public ImmutableArray<ComponentTreeStaleReason> DetectStaleReasons(
        int runningComponentTreeContractVersion,
        string? currentDescriptorHash,
        string? currentSourceComponentIdentity,
        int generatedContractVersion,
        int runningContractVersion) {
        ImmutableArray<ComponentTreeStaleReason>.Builder builder = ImmutableArray.CreateBuilder<ComponentTreeStaleReason>();

        if (string.IsNullOrWhiteSpace(AnnotationKey)
            || Convention is null
            || string.IsNullOrWhiteSpace(ContractTypeName)
            || string.IsNullOrWhiteSpace(OriginatingProjectionTypeName)
            || string.IsNullOrWhiteSpace(DescriptorHash)
            || string.IsNullOrWhiteSpace(SourceComponentIdentity)) {
            builder.Add(ComponentTreeStaleReason.MissingMetadata);
        }

        if (ComponentTreeContractVersion != runningComponentTreeContractVersion) {
            builder.Add(ComponentTreeStaleReason.ContractVersionMismatch);
        }

        if (!string.Equals(DescriptorHash, currentDescriptorHash, StringComparison.Ordinal)) {
            builder.Add(ComponentTreeStaleReason.DescriptorHashMismatch);
        }

        if (!string.Equals(SourceComponentIdentity, currentSourceComponentIdentity, StringComparison.Ordinal)) {
            builder.Add(ComponentTreeStaleReason.SourceComponentIdentityMismatch);
        }

        if (generatedContractVersion != runningContractVersion) {
            builder.Add(ComponentTreeStaleReason.GeneratedRunningContractDrift);
        }

        return builder.ToImmutable();
    }
}
