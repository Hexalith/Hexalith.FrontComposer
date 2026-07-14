using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftBaselineContract(
    string sourcePath,
    string family,
    string type,
    string boundedContext,
    string? displayName,
    string? displayGroupName,
    string? role,
    string? icon,
    bool? destructive,
    string? requiresPolicy,
    string? emptyStateCtaCommandTypeName,
    ImmutableArray<DriftBaselineProperty> properties) {
    internal string SourcePath { get; } = sourcePath;
    internal string Family { get; } = family;
    internal string Type { get; } = type;
    internal string BoundedContext { get; } = boundedContext;
    internal string? DisplayName { get; } = displayName;
    internal string? DisplayGroupName { get; } = displayGroupName;
    internal string? Role { get; } = role;
    internal string? Icon { get; } = icon;
    internal bool? Destructive { get; } = destructive;
    internal string? RequiresPolicy { get; } = requiresPolicy;
    /// <summary>Story 9-1 P6 (AC7): contract-level <c>[ProjectionEmptyStateCta]</c> target command type name.</summary>
    internal string? EmptyStateCtaCommandTypeName { get; } = emptyStateCtaCommandTypeName;
    internal ImmutableArray<DriftBaselineProperty> Properties { get; } = properties;

    internal string IdentityWithoutContext => Family + "|" + Type;
    internal string IdentityWithContext => Family + "|" + Type + "|" + BoundedContext;
}
