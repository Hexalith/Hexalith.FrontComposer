namespace Hexalith.FrontComposer.Testing;

/// <summary>VR linked-summary, field-relationship, and preserved-input assertions without application selectors.</summary>
public static class UxValidationAssertions
{
    /// <summary>Checks measured canonical summary copy/count, complete invalid-only links, declared groups/order, and prior stable targets.</summary>
    public static void AssertLinkedSummary(UxValidationSummarySnapshot summary, string accessibleName,
        IReadOnlyList<UxFieldSnapshot> fields, IReadOnlyList<string> linkedControlIds,
        IReadOnlyList<UxFieldDeclaration> declaredFields, IReadOnlyList<UxFieldGroupDeclaration> declaredGroups,
        IReadOnlyList<UxFieldIdentity> priorIdentities, bool mappedRejection = false)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(linkedControlIds);
        ArgumentNullException.ThrowIfNull(declaredFields);
        ArgumentNullException.ThrowIfNull(declaredGroups);
        ArgumentNullException.ThrowIfNull(priorIdentities);
        UxFieldSnapshot[] invalid = fields.Where(f => f.Invalid).ToArray();
        UxAnnouncement message = UxAnnouncementCatalog.Expected(mappedRejection ? "AM-19" : "AM-18", "validation", "summary",
            [summary.ErrorCount], variant: summary.ErrorCount == 1 ? "single" : null);
        UxAnnouncementAssertions.AssertChannel(message, summary.Semantics, accessibleName);
        string[] invalidControls = invalid.Select(f => f.ControlId).ToArray();
        string[] distinctLinks = linkedControlIds.Distinct(StringComparer.Ordinal).ToArray();
        int[] linkOrder = linkedControlIds.Select(id => Array.IndexOf(invalidControls, id)).ToArray();
        bool valid = summary.ErrorCount > 0 && summary.ErrorCount >= linkedControlIds.Count && summary.Text == message.Copy
            && fields.Count == declaredFields.Count && priorIdentities.Count == fields.Count
            && (declaredGroups.Count > 0 || fields.Count == 0)
            && fields.Select(f => f.FieldKey).Distinct(StringComparer.Ordinal).Count() == fields.Count
            && fields.Select(f => f.ControlId).Distinct(StringComparer.Ordinal).Count() == fields.Count
            && invalid.Select(f => f.ErrorId).Distinct(StringComparer.Ordinal).Count() == invalid.Length
            && !invalid.Any(f => fields.Any(control => control.ControlId == f.ErrorId))
            && declaredGroups.Select(g => g.GroupId).Distinct(StringComparer.Ordinal).Count() == declaredGroups.Count
            && priorIdentities.Select(f => f.FieldKey).Distinct(StringComparer.Ordinal).Count() == fields.Count
            && fields.Select(f => f.FieldKey).SequenceEqual(declaredFields.Select(f => f.FieldKey), StringComparer.Ordinal)
            && declaredGroups.SelectMany(g => g.FieldKeys).SequenceEqual(declaredFields.Select(f => f.FieldKey), StringComparer.Ordinal)
            && invalidControls.SequenceEqual(distinctLinks, StringComparer.Ordinal)
            && linkOrder.SequenceEqual(linkOrder.Order());
        for (int i = 0; valid && i < fields.Count; i++)
        {
            UxFieldSnapshot field = fields[i];
            UxFieldDeclaration declaration = declaredFields[i];
            UxFieldGroupDeclaration? group = declaredGroups.SingleOrDefault(g => g.GroupId == declaration.GroupId);
            UxFieldIdentity? prior = priorIdentities.SingleOrDefault(f => f.FieldKey == field.FieldKey);
            valid = group is not null && prior is not null && !string.IsNullOrWhiteSpace(field.FieldKey)
                && !string.IsNullOrWhiteSpace(field.ControlId) && field.ControlId == prior.ControlId
                && (string.IsNullOrWhiteSpace(field.ErrorId) || string.IsNullOrWhiteSpace(prior.ErrorId) || field.ErrorId == prior.ErrorId)
                && field.VisibleLabel == declaration.VisibleLabel
                && !string.IsNullOrWhiteSpace(field.VisibleLabel) && field.AccessibleName.Contains(field.VisibleLabel, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(group.GroupId) && field.GroupId == group.GroupId
                && group.FieldKeys.Contains(field.FieldKey, StringComparer.Ordinal)
                && !string.IsNullOrWhiteSpace(group.VisibleLabel) && field.GroupVisibleLabel == group.VisibleLabel
                && !string.IsNullOrWhiteSpace(group.Name) && field.GroupName == group.Name
                && !string.IsNullOrWhiteSpace(group.Description) && field.GroupDescription == group.Description && field.GroupRole is "group" or "radiogroup"
                && (!field.Invalid || !string.IsNullOrWhiteSpace(field.ErrorId)
                    && field.DescribedBy.Split([' ', '\t', '\n', '\r', '\f'], StringSplitOptions.RemoveEmptyEntries)
                        .Contains(field.ErrorId, StringComparer.Ordinal));
        }
        if (!valid)
        {
            throw new InvalidOperationException("VR-01/02 canonical summary, invalid-only links, declared group/order, labels, or prior stable targets drifted.");
        }
    }

    /// <summary>Asserts exact useful input preservation; values and field identifiers never appear in failures.</summary>
    public static void AssertInputPreserved(IReadOnlyDictionary<string, string?> before, IReadOnlyDictionary<string, string?> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (before.Count != after.Count || before.Any(pair => !after.TryGetValue(pair.Key, out string? value) || pair.Value != value))
        {
            throw new InvalidOperationException("VR input changed during a failure, recovery, or Stay/Escape decision.");
        }
    }
}
