using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for Razor DataGrid component generation.
/// </summary>
public sealed class RazorModel : IEquatable<RazorModel> {
    public RazorModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        EquatableArray<ColumnModel> columns,
        ProjectionRenderStrategy strategy = ProjectionRenderStrategy.Default,
        EquatableArray<string> whenStates = default,
        string? entityLabel = null,
        string? entityPluralLabel = null,
        string? emptyStateCtaCommandName = null)
        : this(typeName, @namespace, boundedContext, columns, strategy, whenStates, entityLabel, entityPluralLabel,
            emptyStateCtaCommandName, null) {
    }

    internal RazorModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        EquatableArray<ColumnModel> columns,
        ProjectionRenderStrategy strategy,
        EquatableArray<string> whenStates,
        string? entityLabel,
        string? entityPluralLabel,
        string? emptyStateCtaCommandName,
        NestedProjectionIdentity? nestedProjection) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        Columns = columns;
        Strategy = strategy;
        WhenStates = whenStates;
        EntityLabel = entityLabel;
        EntityPluralLabel = entityPluralLabel;
        EmptyStateCtaCommandName = emptyStateCtaCommandName;
        NestedProjection = nestedProjection;
    }

    public string TypeName { get; }

    private NestedProjectionIdentity? NestedProjection { get; }

    /// <summary>Gets the C# reference to the projection.</summary>
    internal string TypeReference => NestedProjection?.SourceName ?? TypeName;

    /// <summary>Gets the CLR identity used by runtime projection APIs.</summary>
    internal string ProjectionTypeName => NestedProjection?.MetadataName
        ?? (string.IsNullOrEmpty(Namespace) ? TypeName : Namespace + "." + TypeName);

    /// <summary>Gets the generated view and state type stem.</summary>
    internal string ArtifactName => NestedProjection?.ArtifactName ?? TypeName;

    public string Namespace { get; }

    /// <summary>Gets the namespace containing the emitted artifacts.</summary>
    internal string GeneratedNamespace => NestedProjection?.GeneratedNamespace ?? Namespace;

    public string? BoundedContext { get; }

    public EquatableArray<ColumnModel> Columns { get; }

    /// <summary>
    /// Gets the render strategy selected by <see cref="RazorModelTransform"/> (Story 4-1 D4).
    /// </summary>
    public ProjectionRenderStrategy Strategy { get; }

    /// <summary>
    /// Gets the trimmed, split, non-empty state-enum member names extracted from
    /// <c>[ProjectionRole(..., WhenState = "A,B")]</c> (Story 4-1 D3 / AC9). Empty array
    /// when the attribute specified no filter.
    /// </summary>
    public EquatableArray<string> WhenStates { get; }

    public string? EntityLabel { get; }

    public string? EntityPluralLabel { get; }

    public string? EmptyStateCtaCommandName { get; }

    public bool Equals(RazorModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && NestedProjection == other.NestedProjection
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && Strategy == other.Strategy
            && WhenStates == other.WhenStates
            && EntityLabel == other.EntityLabel
            && EntityPluralLabel == other.EntityPluralLabel
            && EmptyStateCtaCommandName == other.EmptyStateCtaCommandName
            && Columns == other.Columns;
    }

    public override bool Equals(object? obj) => Equals(obj as RazorModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (NestedProjection?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + Strategy.GetHashCode();
            hash = (hash * 31) + WhenStates.GetHashCode();
            hash = (hash * 31) + (EntityLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (EntityPluralLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (EmptyStateCtaCommandName?.GetHashCode() ?? 0);
            hash = (hash * 31) + Columns.GetHashCode();
            return hash;
        }
    }
}
