namespace Hexalith.FrontComposer.Shell.Extensions;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Fluxor;
using Fluxor.DependencyInjection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering FrontComposer Shell services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Fluxor state management, storage services, and all FrontComposer Shell dependencies.
    /// <para>
    /// <b>Important:</b> The consuming application must place
    /// <c>&lt;Fluxor.Blazor.Web.StoreInitializer /&gt;</c> in its root layout component.
    /// The Shell is a Razor Class Library and cannot place it automatically.
    /// </para>
    /// </summary>
    /// <remarks>
    /// DI scope divergence: on Blazor Server, services are scoped per circuit;
    /// on Blazor WebAssembly, services are scoped per application instance.
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureFluxor">Optional callback to configure additional Fluxor options (e.g., scan consumer assemblies).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithFrontComposer(
        this IServiceCollection services,
        Action<FluxorOptions>? configureFluxor = null)
    {
        services.AddLogging();
        services.AddFluxor(o =>
        {
            o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly);
            configureFluxor?.Invoke(o);
        });
        services.AddSingleton<IStorageService, InMemoryStorageService>();
        services.AddSingleton<IFrontComposerRegistry, FrontComposerRegistry>();
        return services;
    }

    /// <summary>
    /// Discovers and registers generated domain registration classes from the assembly containing <typeparamref name="T"/>.
    /// Each class whose name ends in "Registration" and exposes both a static <c>Manifest</c> property
    /// of type <see cref="DomainManifest"/> and a static <c>RegisterDomain(IFrontComposerRegistry)</c> method
    /// is registered for invocation when the registry is constructed.
    /// </summary>
    /// <typeparam name="T">A marker type in the domain assembly to scan.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    [RequiresUnreferencedCode("Domain discovery uses reflection to scan assembly types at runtime.")]
    public static IServiceCollection AddHexalithDomain<T>(this IServiceCollection services)
        where T : class
    {
        Assembly domainAssembly = typeof(T).Assembly;
        BoundedContextAttribute? markerContext = typeof(T).GetCustomAttribute<BoundedContextAttribute>();
        Dictionary<string, CommandGroup> commandGroups = new(StringComparer.Ordinal);

        foreach (Type type in domainAssembly.GetExportedTypes())
        {
            if (!type.Name.EndsWith("Registration", StringComparison.Ordinal))
            {
                CollectCommandRegistration(type, markerContext, commandGroups);
                continue;
            }

            bool hasManifest = HasStaticManifestMember(type);
            MethodInfo? registerMethod = type.GetMethod(
                "RegisterDomain",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IFrontComposerRegistry)],
                null);

            if (hasManifest && registerMethod is not null)
            {
                services.AddSingleton(new DomainRegistrationAction(registerMethod));
            }
            else if (hasManifest || registerMethod is not null)
            {
                services.AddSingleton(new DomainRegistrationWarning(
                    type.FullName ?? type.Name,
                    hasManifest,
                    registerMethod is not null));
            }

            CollectCommandRegistration(type, markerContext, commandGroups);
        }

        foreach ((string boundedContext, CommandGroup group) in commandGroups)
        {
            DomainManifest manifest = new(
                group.DisplayName ?? boundedContext,
                boundedContext,
                [],
                [.. group.Commands]);

            services.AddSingleton(new DomainRegistrationAction(registry => registry.RegisterDomain(manifest)));
        }

        return services;
    }

    private static bool HasStaticManifestMember(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        PropertyInfo? prop = type.GetProperty("Manifest", BindingFlags.Public | BindingFlags.Static);
        if (prop is not null && prop.PropertyType == typeof(DomainManifest))
        {
            return true;
        }

        FieldInfo? field = type.GetField("Manifest", BindingFlags.Public | BindingFlags.Static);
        return field is not null && field.FieldType == typeof(DomainManifest);
    }

    private static void CollectCommandRegistration(
        Type type,
        BoundedContextAttribute? markerContext,
        Dictionary<string, CommandGroup> commandGroups)
    {
        if (type.GetCustomAttribute<CommandAttribute>() is null)
        {
            return;
        }

        BoundedContextAttribute? commandContext = type.GetCustomAttribute<BoundedContextAttribute>();
        string? boundedContext = commandContext?.Name ?? markerContext?.Name;
        if (string.IsNullOrWhiteSpace(boundedContext))
        {
            return;
        }

        if (!commandGroups.TryGetValue(boundedContext, out CommandGroup? group))
        {
            group = new CommandGroup(commandContext?.DisplayLabel ?? markerContext?.DisplayLabel);
            commandGroups[boundedContext] = group;
        }

        string commandTypeName = type.FullName ?? type.Name;
        if (!group.Commands.Contains(commandTypeName))
        {
            group.Commands.Add(commandTypeName);
        }
    }

    private sealed class CommandGroup
    {
        public CommandGroup(string? displayName)
        {
            DisplayName = displayName;
        }

        public string? DisplayName { get; }

        public List<string> Commands { get; } = [];
    }
}
