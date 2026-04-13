namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Customization gradient support. Placeholder for v1 -- method set is provisional.
/// Generic convenience methods will be added as extension methods in Shell.
/// </summary>
public interface IOverrideRegistry
{
    /// <summary>
    /// Registers a customization override for a projection type.
    /// </summary>
    /// <param name="projectionType">The projection type name to override.</param>
    /// <param name="overrideType">The override category (e.g., "slot", "view").</param>
    /// <param name="implementationType">The implementation type providing the override.</param>
    void Register(string projectionType, string overrideType, Type implementationType);

    /// <summary>
    /// Resolves a customization override for a projection type.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="overrideType">The override category.</param>
    /// <returns>The implementation type, or <c>null</c> if no override is registered.</returns>
    Type? Resolve(string projectionType, string overrideType);
}
