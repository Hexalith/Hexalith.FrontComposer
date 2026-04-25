using System.Collections.Generic;

using Hexalith.FrontComposer.SourceTools.Transforms;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Story 4-5 T6.5 / D9 / AC5 — partition output for
/// <see cref="RoleBodyHelpers.ResolveFieldGroups"/>. Carries the group name (or
/// <see langword="null"/> for the catch-all "Additional details" bucket) and the
/// ordered column list.
/// </summary>
public sealed class FieldGroupBucket {
    /// <summary>Initializes a new instance of the <see cref="FieldGroupBucket"/> class.</summary>
    public FieldGroupBucket(string? groupName, IReadOnlyList<ColumnModel> columns) {
        GroupName = groupName;
        Columns = columns;
    }

    /// <summary>
    /// Gets the declared group name, or <see langword="null"/> for the catch-all bucket
    /// (rendered with the <c>FieldGroupCatchAllTitle</c> resource value at emit time).
    /// </summary>
    public string? GroupName { get; }

    /// <summary>Gets the columns belonging to this group, in declaration order.</summary>
    public IReadOnlyList<ColumnModel> Columns { get; }
}
