namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Assigns a <see cref="BadgeSlot"/> to an enum field, controlling
/// which badge color the renderer applies for that field value.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionBadgeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionBadgeAttribute"/> class.
    /// </summary>
    /// <param name="slot">The badge slot.</param>
    public ProjectionBadgeAttribute(BadgeSlot slot)
    {
        Slot = slot;
    }

    /// <summary>
    /// Gets the badge slot.
    /// </summary>
    public BadgeSlot Slot { get; }
}
