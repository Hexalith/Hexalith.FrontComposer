namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Story 4-5 D9 — declares a named group for a projection property's secondary detail field, or
/// for a command property on a generated command form.
/// On a projection, properties sharing a <see cref="GroupName"/> render as a single
/// <c>FluentAccordionItem</c> labeled with that group; properties with no
/// <see cref="ProjectionFieldGroupAttribute"/> fall into the "Additional details" catch-all section.
/// On a command (Story 13.3), properties sharing a <see cref="GroupName"/> render inside one
/// <c>fieldset</c> whose legend is the group name, in declared order.
/// </summary>
/// <remarks>
/// <para>
/// Projections: group ordering uses first-declared-property precedence within a projection
/// (stable — matches Story 4-4 D17 sort discipline). Inherited properties append after
/// derived-type properties per Roslyn's <c>ITypeSymbol.GetMembers()</c> walk order.
/// Case-insensitive collision with the reserved catch-all name <c>"Additional details"</c> emits
/// HFC1030 at parse stage (Information; fail-soft pass-through). Grouping applies only to
/// projections that render a detail body — Default / ActionQueue / StatusOverview / Dashboard
/// (via 4-5's expand-in-row host) and DetailRecord (via 4-1's role body); Timeline projections
/// emit HFC1031 Information at emit stage when annotated, since Timeline has no detail surface.
/// </para>
/// <para>
/// Commands (Story 13.3 VR-01): a generated command form renders each declared group once, as a
/// <c>fieldset</c> positioned at its first member, with all of its members in declared order. Later
/// members are pulled up to that position, so an ungrouped property declared between two members of
/// a group renders after that group: <c>A(G), B, C(G)</c> renders as <c>A, C, B</c>. Ungrouped
/// properties otherwise keep their relative declared order. The "Additional details" catch-all,
/// HFC1030, and HFC1031 apply to projections only.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionFieldGroupAttribute : Attribute {
    /// <summary>Initializes a new instance of the <see cref="ProjectionFieldGroupAttribute"/> class.</summary>
    /// <param name="groupName">
    /// Non-empty group label rendered as the projection <c>FluentAccordionItem</c> heading and the
    /// generated command-form <c>fieldset</c> legend.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="groupName"/> is null, empty, or whitespace.</exception>
    public ProjectionFieldGroupAttribute(string groupName) {
        if (string.IsNullOrWhiteSpace(groupName)) {
            throw new ArgumentException("Group name cannot be null, empty, or whitespace.", nameof(groupName));
        }

        GroupName = groupName;
    }

    /// <summary>Gets the declared group name.</summary>
    public string GroupName { get; }
}
