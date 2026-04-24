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
        EquatableArray<BadgeMappingEntry> badgeMappings,
        EquatableArray<string> enumMemberNames = default,
        int? priority = null) {
        PropertyName = propertyName;
        Header = header;
        TypeCategory = typeCategory;
        FormatHint = formatHint;
        IsNullable = isNullable;
        BadgeMappings = badgeMappings;
        EnumMemberNames = enumMemberNames;
        Priority = priority;
    }

    public string PropertyName { get; }

    public string Header { get; }

    public TypeCategory TypeCategory { get; }

    public string? FormatHint { get; }

    public bool IsNullable { get; }

    public EquatableArray<BadgeMappingEntry> BadgeMappings { get; }

    public EquatableArray<string> EnumMemberNames { get; }

    /// <summary>
    /// Story 4-4 T6.3 / D14 / D17 — declared <c>[ColumnPriority]</c> value carried through from
    /// <see cref="PropertyModel.ColumnPriority"/>. <see langword="null"/> materialises as
    /// <see cref="int.MaxValue"/> at sort time; the Transform stage applies
    /// <c>(Priority ?? int.MaxValue, DeclarationOrder)</c> stable sort.
    /// </summary>
    public int? Priority { get; }

    /// <summary>
    /// Story 4-3 D14 — derived gate for column-header filter affordance. True for
    /// Text / Numeric / Enum / DateTime; false for Boolean / Collection / Unsupported.
    /// Derived from <see cref="TypeCategory"/> so IR byte-stability is preserved
    /// (no ctor parameter, no additional equality contribution).
    /// </summary>
    public bool SupportsFilter => TypeCategory switch {
        TypeCategory.Text => true,
        TypeCategory.Numeric => true,
        TypeCategory.Enum => true,
        TypeCategory.DateTime => true,
        _ => false,
    };

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
            && BadgeMappings == other.BadgeMappings
            && EnumMemberNames == other.EnumMemberNames
            && Priority == other.Priority;
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
            hash = (hash * 31) + EnumMemberNames.GetHashCode();
            hash = (hash * 31) + (Priority?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
