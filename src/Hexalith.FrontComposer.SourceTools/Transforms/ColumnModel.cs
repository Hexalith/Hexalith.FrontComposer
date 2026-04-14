using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for a single DataGrid column, derived from a PropertyModel.
/// </summary>
public sealed class ColumnModel : IEquatable<ColumnModel> {
    public ColumnModel(
        string propertyName,
        string header,
        TypeCategory typeCategory,
        string? formatHint,
        bool isNullable,
        EquatableArray<BadgeMappingEntry> badgeMappings) {
        PropertyName = propertyName;
        Header = header;
        TypeCategory = typeCategory;
        FormatHint = formatHint;
        IsNullable = isNullable;
        BadgeMappings = badgeMappings;
    }

    public string PropertyName { get; }

    public string Header { get; }

    public TypeCategory TypeCategory { get; }

    public string? FormatHint { get; }

    public bool IsNullable { get; }

    public EquatableArray<BadgeMappingEntry> BadgeMappings { get; }

    public bool Equals(ColumnModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return PropertyName == other.PropertyName
            && Header == other.Header
            && TypeCategory == other.TypeCategory
            && FormatHint == other.FormatHint
            && IsNullable == other.IsNullable
            && BadgeMappings == other.BadgeMappings;
    }

    public override bool Equals(object? obj) => Equals(obj as ColumnModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (PropertyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Header?.GetHashCode() ?? 0);
            hash = (hash * 31) + TypeCategory.GetHashCode();
            hash = (hash * 31) + (FormatHint?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsNullable.GetHashCode();
            hash = (hash * 31) + BadgeMappings.GetHashCode();
            return hash;
        }
    }
}
