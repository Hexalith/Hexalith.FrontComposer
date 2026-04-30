using System.Linq.Expressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Public Level 3 field-slot registration extensions.
/// </summary>
public static class ProjectionSlotServiceCollectionExtensions {
    /// <summary>
    /// Registers a custom component for one generated projection field.
    /// </summary>
    /// <typeparam name="TProjection">Projection type owning the field.</typeparam>
    /// <typeparam name="TField">Selected field type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="field">Direct property selector such as <c>x =&gt; x.Priority</c>.</param>
    /// <param name="componentType">Razor component type with a compatible <c>Context</c> parameter.</param>
    /// <param name="role">Optional projection role. Omit for a role-agnostic slot.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSlotOverride<TProjection, TField>(
        this IServiceCollection services,
        Expression<Func<TProjection, TField>> field,
        Type componentType,
        ProjectionRole? role = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(componentType);

        ProjectionSlotFieldIdentity identity = ProjectionSlotSelector.Parse(field);
        ProjectionSlotDescriptor descriptor = new(
            ProjectionType: typeof(TProjection),
            FieldName: identity.Name,
            FieldType: identity.FieldType,
            Role: role,
            ComponentType: componentType,
            ContractVersion: ProjectionSlotContractVersion.Current);

        _ = services.AddSingleton(new ProjectionSlotDescriptorSource([descriptor]));
        return services;
    }
}
