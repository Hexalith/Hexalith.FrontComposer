namespace Hexalith.FrontComposer.Testing;

/// <summary>Caller-measured control/error and programmatic field-group relationships in observed DOM order.</summary>
/// <param name="FieldKey">Stable declaration identity, independent of generated DOM IDs.</param>
/// <param name="ControlId">Current control target.</param>
/// <param name="VisibleLabel">Visible field label.</param>
/// <param name="AccessibleName">Computed control name, including any useful extra context.</param>
/// <param name="GroupId">Measured owning field-group identifier.</param>
/// <param name="GroupVisibleLabel">Visible group label.</param>
/// <param name="GroupName">Computed group name.</param>
/// <param name="GroupDescription">Computed group description.</param>
/// <param name="GroupRole">Measured owning group role.</param>
/// <param name="Invalid">Observed invalid state.</param>
/// <param name="ErrorId">Current error target, empty for a valid field without an error.</param>
/// <param name="DescribedBy">Observed ASCII-whitespace-separated aria-describedby IDREFs.</param>
public sealed record UxFieldSnapshot(string FieldKey, string ControlId, string VisibleLabel, string AccessibleName,
    string GroupId, string GroupVisibleLabel, string GroupName, string GroupDescription, string GroupRole,
    bool Invalid, string ErrorId, string DescribedBy);
