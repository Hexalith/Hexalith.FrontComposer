namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftBaselineProperty(
    string name,
    string category,
    bool nullable,
    bool? derivable,
    string? displayName,
    string? description,
    int? columnPriority,
    string? fieldGroup,
    string? displayFormat,
    int? relativeTimeWindowDays,
    string? badgeSignature) {
    internal string Name { get; } = name;
    internal string Category { get; } = category;
    internal bool Nullable { get; } = nullable;
    internal bool? Derivable { get; } = derivable;
    internal string? DisplayName { get; } = displayName;
    internal string? Description { get; } = description;
    internal int? ColumnPriority { get; } = columnPriority;
    internal string? FieldGroup { get; } = fieldGroup;
    internal string? DisplayFormat { get; } = displayFormat;
    /// <summary>Story 9-1 P6 (AC7): days window for <c>FieldDisplayFormat.RelativeTime</c>; <c>null</c> when not relative-time.</summary>
    internal int? RelativeTimeWindowDays { get; } = relativeTimeWindowDays;
    /// <summary>Story 9-1 P6 (AC7): canonical signature of <c>[ProjectionBadge]</c> mappings — comma-joined <c>"EnumMember=Slot"</c> entries ordered ordinally; <c>null</c> when no badge mappings.</summary>
    internal string? BadgeSignature { get; } = badgeSignature;
}
