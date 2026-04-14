using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Parsing;
/// <summary>
/// Intermediate representation of a [Projection]-annotated domain type,
/// produced by the Parse stage. Pure data -- no Roslyn symbol references.
/// </summary>
public sealed class DomainModel : IEquatable<DomainModel> {
    public DomainModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string? boundedContextDisplayLabel,
        string? projectionRole,
        EquatableArray<PropertyModel> properties) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        BoundedContextDisplayLabel = boundedContextDisplayLabel;
        ProjectionRole = projectionRole;
        Properties = properties;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public string? BoundedContextDisplayLabel { get; }

    public string? ProjectionRole { get; }

    public EquatableArray<PropertyModel> Properties { get; }

    public bool Equals(DomainModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && BoundedContextDisplayLabel == other.BoundedContextDisplayLabel
            && ProjectionRole == other.ProjectionRole
            && Properties == other.Properties;
    }

    public override bool Equals(object? obj) => Equals(obj as DomainModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContextDisplayLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionRole?.GetHashCode() ?? 0);
            hash = (hash * 31) + Properties.GetHashCode();
            return hash;
        }
    }

}

/// <summary>
/// IR representation of a single property on a [Projection]-annotated type.
/// </summary>
public sealed class PropertyModel : IEquatable<PropertyModel> {
    public PropertyModel(
        string name,
        string typeName,
        bool isNullable,
        bool isUnsupported,
        string? displayName,
        EquatableArray<BadgeMappingEntry> badgeMappings) {
        Name = name;
        TypeName = typeName;
        IsNullable = isNullable;
        IsUnsupported = isUnsupported;
        DisplayName = displayName;
        BadgeMappings = badgeMappings;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public bool IsUnsupported { get; }

    public string? DisplayName { get; }

    public EquatableArray<BadgeMappingEntry> BadgeMappings { get; }

    public bool Equals(PropertyModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Name == other.Name
            && TypeName == other.TypeName
            && IsNullable == other.IsNullable
            && IsUnsupported == other.IsUnsupported
            && DisplayName == other.DisplayName
            && BadgeMappings == other.BadgeMappings;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Name?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsNullable.GetHashCode();
            hash = (hash * 31) + IsUnsupported.GetHashCode();
            hash = (hash * 31) + (DisplayName?.GetHashCode() ?? 0);
            hash = (hash * 31) + BadgeMappings.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Maps an enum member to a badge slot, extracted from [ProjectionBadge] on enum fields.
/// </summary>
public sealed class BadgeMappingEntry : IEquatable<BadgeMappingEntry> {
    public BadgeMappingEntry(string enumMemberName, string slot) {
        EnumMemberName = enumMemberName;
        Slot = slot;
    }

    public string EnumMemberName { get; }

    public string Slot { get; }

    public bool Equals(BadgeMappingEntry? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return EnumMemberName == other.EnumMemberName && Slot == other.Slot;
    }

    public override bool Equals(object? obj) => Equals(obj as BadgeMappingEntry);

    public override int GetHashCode() {
        unchecked {
            return (((17 * 31) + (EnumMemberName?.GetHashCode() ?? 0)) * 31) + (Slot?.GetHashCode() ?? 0);
        }
    }
}

/// <summary>
/// Result of the Parse stage: a domain model (if valid) plus any diagnostics.
/// Diagnostics travel as data because SourceProductionContext is only available in RegisterSourceOutput.
/// </summary>
public sealed class ParseResult : IEquatable<ParseResult> {
    public ParseResult(DomainModel? model, EquatableArray<DiagnosticInfo> diagnostics) {
        Model = model;
        Diagnostics = diagnostics;
    }

    public DomainModel? Model { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(ParseResult? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        bool modelsEqual = (Model is null && other.Model is null)
            || (Model is not null && Model.Equals(other.Model));

        return modelsEqual && Diagnostics == other.Diagnostics;
    }

    public override bool Equals(object? obj) => Equals(obj as ParseResult);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Model?.GetHashCode() ?? 0);
            hash = (hash * 31) + Diagnostics.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Serializable diagnostic information -- no Roslyn Location references.
/// </summary>
public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo> {
    public DiagnosticInfo(string id, string message, string severity, string filePath, int line, int column) {
        Id = id;
        Message = message;
        Severity = severity;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public string Id { get; }

    public string Message { get; }

    public string Severity { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }

    public bool Equals(DiagnosticInfo? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Id == other.Id
            && Message == other.Message
            && Severity == other.Severity
            && FilePath == other.FilePath
            && Line == other.Line
            && Column == other.Column;
    }

    public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Id?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Message?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Severity?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FilePath?.GetHashCode() ?? 0);
            hash = (hash * 31) + Line.GetHashCode();
            hash = (hash * 31) + Column.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Converts the serialized diagnostic coordinates back into a Roslyn location.
    /// </summary>
    /// <returns>The diagnostic location, or <see cref="Location.None"/> when unavailable.</returns>
    public Location ToLocation() {
        if (string.IsNullOrWhiteSpace(FilePath) || Line < 0 || Column < 0) {
            return Location.None;
        }

        LinePosition position = new(Line, Column);
        LinePositionSpan lineSpan = new(position, position);
        return Location.Create(FilePath, new TextSpan(0, 0), lineSpan);
    }
}
