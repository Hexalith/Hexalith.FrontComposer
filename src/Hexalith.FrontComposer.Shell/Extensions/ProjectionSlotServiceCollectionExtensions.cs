using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Public Level 3 field-slot registration extensions.
/// </summary>
/// <remarks>
/// These extensions register one <see cref="ProjectionSlotDescriptorSource"/> per call so the
/// runtime registry observes every override. They also call
/// <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}(IServiceCollection)"/>
/// for <see cref="IProjectionSlotRegistry"/> so adopters who only call <c>AddSlotOverride</c>
/// without invoking the framework's main bootstrap extension still get a working registry.
/// </remarks>
public static class ProjectionSlotServiceCollectionExtensions {
    /// <summary>
    /// Registers a custom component for one generated projection field using a typed component
    /// generic constraint. Prefer this overload — incompatible component types fail at compile
    /// time instead of at app startup.
    /// </summary>
    /// <typeparam name="TProjection">Projection type owning the field.</typeparam>
    /// <typeparam name="TField">Selected field type.</typeparam>
    /// <typeparam name="TComponent">Razor component type with a compatible <c>Context</c> parameter.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="field">Direct property selector such as <c>x =&gt; x.Priority</c>.</param>
    /// <param name="role">Optional projection role. Omit for a role-agnostic slot.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSlotOverride<
        TProjection,
        TField,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TComponent>(
        this IServiceCollection services,
        Expression<Func<TProjection, TField>> field,
        ProjectionRole? role = null)
        where TComponent : IComponent
        => services.AddSlotOverride(field, typeof(TComponent), role);

    /// <summary>
    /// Registers a custom component for one generated projection field. The
    /// <paramref name="componentType"/> overload exists for codegen scenarios where the
    /// component type is only known dynamically; prefer the typed
    /// <c>AddSlotOverride&lt;TProjection,TField,TComponent&gt;</c> overload at adopter call sites.
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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

        // GB-P15 — make AddSlotOverride self-sufficient so adopter code that only adds slots
        // without calling the framework's main bootstrap extension still gets a working
        // registry. TryAddSingleton is idempotent and harmless when the bootstrap also runs.
        services.TryAddSingleton<IProjectionSlotRegistry, ProjectionSlotRegistry>();
        _ = services.AddSingleton(new ProjectionSlotDescriptorSource([descriptor]));
        return services;
    }
}
