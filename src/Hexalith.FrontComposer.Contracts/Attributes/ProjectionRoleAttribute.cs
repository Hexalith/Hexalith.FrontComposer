namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Assigns a <see cref="ProjectionRole"/> to a projection class, controlling
/// which rendering strategy the source generator applies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionRoleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionRoleAttribute"/> class.
    /// </summary>
    /// <param name="role">The projection role.</param>
    public ProjectionRoleAttribute(ProjectionRole role)
    {
        Role = role;
    }

    /// <summary>
    /// Gets the projection role.
    /// </summary>
    public ProjectionRole Role { get; }
}
