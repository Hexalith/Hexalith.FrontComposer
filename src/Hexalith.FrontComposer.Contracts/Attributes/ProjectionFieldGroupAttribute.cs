namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Story 4-5 D9 — declares a named group for a projection property's secondary detail field.
/// Properties sharing a <see cref="GroupName"/> render as a single <c>FluentAccordionItem</c>
/// labeled with that group; properties with no <see cref="ProjectionFieldGroupAttribute"/>
/// fall into the "Additional details" catch-all section.
/// </summary>
/// <remarks>
/// Group ordering uses first-declared-property precedence within a projection (stable —
/// matches Story 4-4 D17 sort discipline). Inherited properties append after derived-type
/// properties per Roslyn's <c>ITypeSymbol.GetMembers()</c> walk order. Case-insensitive
/// collision with the reserved catch-all name <c>"Additional details"</c> emits HFC1030
/// at parse stage (Information; fail-soft pass-through). Applies only to projections that
/// render a detail body — Default / ActionQueue / StatusOverview / Dashboard
/// (via 4-5's expand-in-row host) and DetailRecord (via 4-1's role body); Timeline
/// projections emit HFC1031 Information at emit stage when annotated, since Timeline has
/// no detail surface.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionFieldGroupAttribute : Attribute {
    /// <summary>Initializes a new instance of the <see cref="ProjectionFieldGroupAttribute"/> class.</summary>
    /// <param name="groupName">Non-empty group label rendered as the <c>FluentAccordionItem</c> heading.</param>
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
