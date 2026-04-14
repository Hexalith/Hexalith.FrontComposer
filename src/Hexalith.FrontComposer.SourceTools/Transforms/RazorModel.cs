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
        EquatableArray<ColumnModel> columns) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        Columns = columns;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public EquatableArray<ColumnModel> Columns { get; }

    public bool Equals(RazorModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && Columns == other.Columns;
    }

    public override bool Equals(object? obj) => Equals(obj as RazorModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + Columns.GetHashCode();
            return hash;
        }
    }
}
