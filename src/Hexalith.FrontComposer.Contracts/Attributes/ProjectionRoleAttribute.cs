namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Assigns a <see cref="ProjectionRole"/> to a projection class, controlling
/// which rendering strategy the source generator applies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionRoleAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionRoleAttribute"/> class.
    /// </summary>
    /// <param name="role">The projection role.</param>
    public ProjectionRoleAttribute(ProjectionRole role) => Role = role;

    /// <summary>
    /// Gets the projection role.
    /// </summary>
    public ProjectionRole Role { get; }

    /// <summary>
    /// Gets an optional CSV of state-enum member names used by the
    /// <see cref="ProjectionRole.ActionQueue"/> strategy to filter rows at render time
    /// (Story 4-1 D2 / AC9). Parsed by <c>AttributeParser</c>: trimmed, case-sensitive
    /// against the enum member set, unknown members emit HFC1022 warning and flow
    /// through as always-no-match. Empty string / null means "no state filter".
    /// </summary>
    /// <example><c>[ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Pending,Submitted")]</c></example>
    public string? WhenState { get; init; }
}
