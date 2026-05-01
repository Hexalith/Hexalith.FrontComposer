using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionViewOverrides;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Public Level 4 full projection-view replacement registration extensions.
/// </summary>
public static class ProjectionViewOverrideServiceCollectionExtensions {
    /// <summary>
    /// Registers a full projection-view body replacement component for a projection type and
    /// optional role.
    /// </summary>
    /// <typeparam name="TProjection">Projection type whose generated body can be replaced.</typeparam>
    /// <typeparam name="TComponent">Razor component type with a compatible <c>Context</c> parameter.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="role">Optional projection role. Omit for a role-agnostic replacement.</param>
    /// <param name="registrationSource">Optional caller-provided source for diagnostics.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddViewOverride<
        TProjection,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TComponent>(
        this IServiceCollection services,
        ProjectionRole? role = null,
        [CallerMemberName] string? registrationSource = null)
        where TComponent : IComponent {
        ArgumentNullException.ThrowIfNull(services);

        ProjectionViewOverrideDescriptor descriptor = new(
            ProjectionType: typeof(TProjection),
            Role: role,
            ComponentType: typeof(TComponent),
            ContractVersion: ProjectionViewOverrideContractVersion.Current,
            RegistrationSource: registrationSource ?? "<unknown>");

        services.TryAddSingleton<IProjectionViewOverrideRegistry, ProjectionViewOverrideRegistry>();
        _ = services.AddSingleton(new ProjectionViewOverrideDescriptorSource([descriptor]));
        return services;
    }
}
