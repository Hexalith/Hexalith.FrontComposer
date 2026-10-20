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
        // Diagnostics name failed checks and 1-based field positions, never field keys, IDs, labels, or values.
        List<string> failed = [];
        void Check(bool passed, string check)
        {
            if (!passed) { failed.Add(check); }
        }
        Check(summary.ErrorCount > 0 && summary.ErrorCount >= linkedControlIds.Count && summary.Text == message.Copy, "Summary");
        Check(fields.Count == declaredFields.Count
            && fields.Select(f => f.FieldKey).SequenceEqual(declaredFields.Select(f => f.FieldKey), StringComparer.Ordinal), "DeclaredFields");
        Check((declaredGroups.Count > 0 || fields.Count == 0)
            && declaredGroups.Select(g => g.GroupId).Distinct(StringComparer.Ordinal).Count() == declaredGroups.Count
            && declaredGroups.SelectMany(g => g.FieldKeys).SequenceEqual(declaredFields.Select(f => f.FieldKey), StringComparer.Ordinal), "DeclaredGroups");
        Check(fields.Select(f => f.FieldKey).Distinct(StringComparer.Ordinal).Count() == fields.Count
            && fields.Select(f => f.ControlId).Distinct(StringComparer.Ordinal).Count() == fields.Count, "FieldIdentity");
        Check(invalid.Select(f => f.ErrorId).Distinct(StringComparer.Ordinal).Count() == invalid.Length
            && !invalid.Any(f => fields.Any(control => control.ControlId == f.ErrorId)), "ErrorTargets");
        Check(priorIdentities.Count == fields.Count
            && priorIdentities.Select(f => f.FieldKey).Distinct(StringComparer.Ordinal).Count() == fields.Count, "PriorTargets");
        Check(invalidControls.SequenceEqual(distinctLinks, StringComparer.Ordinal) && linkOrder.SequenceEqual(linkOrder.Order()), "Links");
        // Only structural failures prevent safe indexing or unique group/prior lookups.
        bool fieldStructureValid = !failed.Any(check => check is "DeclaredFields" or "DeclaredGroups" or "FieldIdentity" or "PriorTargets");
        for (int i = 0; fieldStructureValid && i < fields.Count; i++)
        {
            UxFieldSnapshot field = fields[i];
            UxFieldDeclaration declaration = declaredFields[i];
            UxFieldGroupDeclaration? group = declaredGroups.SingleOrDefault(g => g.GroupId == declaration.GroupId);
            UxFieldIdentity? prior = priorIdentities.SingleOrDefault(f => f.FieldKey == field.FieldKey);
            string at = $"Field[{i + 1}].";
            Check(prior is not null && !string.IsNullOrWhiteSpace(field.FieldKey)
                && !string.IsNullOrWhiteSpace(field.ControlId) && field.ControlId == prior.ControlId, at + "ControlId");
            Check(prior is null || string.IsNullOrWhiteSpace(field.ErrorId) || string.IsNullOrWhiteSpace(prior.ErrorId)
                || field.ErrorId == prior.ErrorId, at + "ErrorId");
            Check(field.VisibleLabel == declaration.VisibleLabel
                && !string.IsNullOrWhiteSpace(field.VisibleLabel) && NormalizeWhitespace(field.AccessibleName).Contains(NormalizeWhitespace(field.VisibleLabel), StringComparison.Ordinal), at + "Label");
            Check(group is not null && !string.IsNullOrWhiteSpace(group.GroupId) && field.GroupId == group.GroupId
                && group.FieldKeys.Contains(field.FieldKey, StringComparer.Ordinal)
                && !string.IsNullOrWhiteSpace(group.VisibleLabel) && field.GroupVisibleLabel == group.VisibleLabel
                && !string.IsNullOrWhiteSpace(group.Name) && field.GroupName == group.Name
                && !string.IsNullOrWhiteSpace(group.Description) && field.GroupDescription == group.Description && field.GroupRole is "group" or "radiogroup", at + "Group");
            // A valid field has no current error target; an invalid one describes its control with it.
            Check(field.Invalid
                ? !string.IsNullOrWhiteSpace(field.ErrorId) && field.DescribedBy.Split([' ', '\t', '\n', '\r', '\f'], StringSplitOptions.RemoveEmptyEntries)
                    .Contains(field.ErrorId, StringComparer.Ordinal)
                : string.IsNullOrWhiteSpace(field.ErrorId), at + "ErrorLink");
        }
        if (failed.Count > 0)
        {
            throw new InvalidOperationException($"VR-01/02 canonical summary, invalid-only links, declared group/order, labels, or prior stable targets drifted: {string.Join(", ", failed)}.");
        }
    }

    private static string NormalizeWhitespace(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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
