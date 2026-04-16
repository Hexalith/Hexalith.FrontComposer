using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Fluxor;
using Fluxor.DependencyInjection;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Extension methods for registering FrontComposer Shell services.
/// </summary>
public static class ServiceCollectionExtensions {

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
        where T : class {
        Assembly domainAssembly = typeof(T).Assembly;
        BoundedContextAttribute? markerContext = typeof(T).GetCustomAttribute<BoundedContextAttribute>();
        var commandGroups = new Dictionary<string, CommandGroup>(StringComparer.Ordinal);

        foreach (Type type in domainAssembly.GetExportedTypes()) {
            if (type.Name.EndsWith("LastUsedSubscriber", StringComparison.Ordinal)
                && typeof(IDisposable).IsAssignableFrom(type)) {
                services.TryAdd(ServiceDescriptor.Scoped(type, type));
            }

            // Story 2-3 Decision D5 — auto-register per-command {Command}LifecycleBridge types discovered
            // in the domain assembly so LifecycleBridgeRegistry.Ensure<T>() can resolve them via DI.
            if (type.Name.EndsWith("LifecycleBridge", StringComparison.Ordinal)
                && typeof(IDisposable).IsAssignableFrom(type)) {
                services.TryAdd(ServiceDescriptor.Scoped(type, type));
            }

            if (!type.Name.EndsWith("Registration", StringComparison.Ordinal)) {
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

            if (hasManifest && registerMethod is not null) {
                _ = services.AddSingleton(new DomainRegistrationAction(registerMethod));
            }
            else if (hasManifest || registerMethod is not null) {
                _ = services.AddSingleton(new DomainRegistrationWarning(
                    type.FullName ?? type.Name,
                    hasManifest,
                    registerMethod is not null));
            }

            CollectCommandRegistration(type, markerContext, commandGroups);
        }

        foreach ((string boundedContext, CommandGroup group) in commandGroups) {
            DomainManifest manifest = new(
                group.DisplayName ?? boundedContext,
                boundedContext,
                [],
                [.. group.Commands]);

            _ = services.AddSingleton(new DomainRegistrationAction(registry => registry.RegisterDomain(manifest)));
        }

        return services;
    }

    /// <summary>
    /// Registers Fluxor state management, storage services, and all FrontComposer Shell dependencies.
    /// <para>
    /// <b>Important:</b> The consuming application must place
    /// <c>&lt;Fluxor.Blazor.Web.StoreInitializer /&gt;</c> in its root layout component.
    /// The Shell is a Razor Class Library and cannot place it automatically.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// DI scope divergence: on Blazor Server, services are scoped per circuit;
    /// on Blazor WebAssembly, services are scoped per application instance.
    /// </para>
    /// <para>
    /// Adopters must call <c>services.AddLocalization()</c> themselves when they use
    /// <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> for command-form
    /// labels. Shell no longer pulls in the ASP.NET Core shared framework automatically so
    /// non-web consumers are not forced to carry it.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureFluxor">Optional callback to configure additional Fluxor options (e.g., scan consumer assemblies).</param>
    /// <returns>The service collection for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "Story 2-4 FcShellOptions is a single concrete type with preserved properties; DataAnnotations validation stays trim-safe.")]
    public static IServiceCollection AddHexalithFrontComposer(
        this IServiceCollection services,
        Action<FluxorOptions>? configureFluxor = null) {
        _ = services.AddLogging();
        _ = services.AddFluxor(o => {
            _ = o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly);
            configureFluxor?.Invoke(o);
        });
        _ = services.AddSingleton<IStorageService, InMemoryStorageService>();
        _ = services.AddSingleton<IFrontComposerRegistry, FrontComposerRegistry>();

        // Default stub command service (ADR-010). Adopters replace it via Story 5.1's AddHexalithEventStore().
        services.TryAddScoped<ICommandService, StubCommandService>();
        _ = services.Configure<StubCommandServiceOptions>(_ => { });

        // Story 2-2 Decision D33 — DataGridNav LRU cap is seeded from FcShellOptions.DataGridNavCap
        // into DataGridNavigationState.Cap by DataGridNavigationFeature at first state construction
        // (Group D code review W1 resolution — no mutable process-static).
        // Story 2-4 Task 3.2 / 3.4 — layer ordered-threshold validator AFTER [Range] annotations.
        _ = services.AddOptions<FcShellOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();

        // Story 2-4 — TimeProvider is required by FcLifecycleWrapper + LifecycleThresholdTimer.
        // Register the system provider if the adopter has not already.
        services.TryAddSingleton(TimeProvider.System);

        // Story 2-2 Task 3.5a — dev diagnostic sink (per-circuit scope).
        services.TryAddScoped<IDiagnosticSink, InMemoryDiagnosticSink>();

        // Story 2-2 Decision D31 — default fail-closed IUserContextAccessor; adopters replace
        // this with a real accessor (HTTP-claims / AuthenticationStateProvider / demo stub).
        services.TryAddScoped<IUserContextAccessor, NullUserContextAccessor>();

        // Story 2-2 Decision D35 — per-circuit subscriber registry (idempotent + lazy).
        services.TryAddScoped<LastUsedSubscriberRegistry>();
        services.TryAddScoped<ILastUsedSubscriberRegistry>(sp => sp.GetRequiredService<LastUsedSubscriberRegistry>());

        // Story 2-3 Decision D2/D3 — ULID factory (Singleton; NUlid wrapper is stateless).
        services.TryAddSingleton<IUlidFactory, UlidFactory>();

        // Story 2-3 — options binding for LifecycleOptions (Chaos CM7 defensive wire-up).
        _ = services.AddOptions<LifecycleOptions>();

        // Story 2-3 Decision D12 — scoped lifecycle state service (per-circuit / per-user).
        services.TryAddScoped<ILifecycleStateService, LifecycleStateService>();

        // Story 2-3 Decision D5 — per-circuit bridge registry (idempotent + lazy; mirrors D35 pattern).
        services.TryAddScoped<LifecycleBridgeRegistry>();
        services.TryAddScoped<ILifecycleBridgeRegistry>(sp => sp.GetRequiredService<LifecycleBridgeRegistry>());

        // Story 2-2 Decision D25 — cached expand-in-row JS module (scoped, lazy import).
        services.TryAddScoped<IExpandInRowJSModule, ExpandInRowJSModule>();

        // Story 2-2 Decision D37 — at-most-one Inline popover registry (Contracts/Rendering).
        // The registry MUST be Scoped (per circuit). Singleton would cross-leak popovers between
        // user circuits because the "currently-open" reference would be process-wide. Reject any
        // pre-existing non-Scoped registration loudly. We scan every matching descriptor (not just
        // the first) so duplicate registrations with mixed lifetimes — e.g. Scoped registered first,
        // Singleton appended later — still trip the throw, since DI resolves the last-registered
        // descriptor.
        ServiceDescriptor? offendingPopoverRegistry = services.FirstOrDefault(
            d => d.ServiceType == typeof(Hexalith.FrontComposer.Contracts.Rendering.InlinePopoverRegistry)
                && d.Lifetime != ServiceLifetime.Scoped);
        if (offendingPopoverRegistry is not null) {
            throw new InvalidOperationException(
                $"InlinePopoverRegistry must be registered as Scoped (found: {offendingPopoverRegistry.Lifetime}). "
                + "Singleton or Transient registration would cross-leak popovers between user circuits.");
        }

        services.TryAddScoped<Hexalith.FrontComposer.Contracts.Rendering.InlinePopoverRegistry>();

        // Default no-op ICommandPageContext — adopter-hosted pages override via scoped registration.
        services.TryAddScoped<ICommandPageContext, NullCommandPageContext>();

        // Story 2-2 Decision D24 — register IDerivedValueProvider chain in the exact order:
        // 1. System → 2. ProjectionContext → 3. ExplicitDefault → 4. LastUsed → 5. ConstructorDefault.
        // Registered via AddScoped (scoped per circuit in Blazor Server; per-app in WASM). Providers
        // with pure state may safely be scoped. Resolution order = registration order.
        _ = services.AddScoped<IDerivedValueProvider, SystemValueProvider>();
        _ = services.AddScoped<IDerivedValueProvider, ProjectionContextProvider>();
        _ = services.AddScoped<IDerivedValueProvider, ExplicitDefaultValueProvider>();
        _ = services.AddScoped<LastUsedValueProvider>();
        _ = services.AddScoped<IDerivedValueProvider>(sp => sp.GetRequiredService<LastUsedValueProvider>());
        _ = services.AddScoped<ILastUsedRecorder>(sp => sp.GetRequiredService<LastUsedValueProvider>());
        _ = services.AddScoped<IDerivedValueProvider, ConstructorDefaultValueProvider>();

        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="IDerivedValueProvider"/> at the HEAD of the chain so it wins
    /// over all built-ins (Story 2-2 ADR-014). Scoped by default; adopter may supply
    /// <see cref="ServiceLifetime.Singleton"/> for pure providers.
    /// </summary>
    public static IServiceCollection AddDerivedValueProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T : class, IDerivedValueProvider {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor descriptor = ServiceDescriptor.Describe(typeof(IDerivedValueProvider), typeof(T), lifetime);

        // Prepend so custom providers come first in enumeration order.
        services.Insert(0, descriptor);
        return services;
    }

    private static void CollectCommandRegistration(
        Type type,
        BoundedContextAttribute? markerContext,
        Dictionary<string, CommandGroup> commandGroups) {
        if (type.GetCustomAttribute<CommandAttribute>() is null) {
            return;
        }

        BoundedContextAttribute? commandContext = type.GetCustomAttribute<BoundedContextAttribute>();
        string? boundedContext = commandContext?.Name ?? markerContext?.Name;
        if (string.IsNullOrWhiteSpace(boundedContext)) {
            return;
        }

        if (!commandGroups.TryGetValue(boundedContext, out CommandGroup? group)) {
            group = new CommandGroup(commandContext?.DisplayLabel ?? markerContext?.DisplayLabel);
            commandGroups[boundedContext] = group;
        }

        string commandTypeName = type.FullName ?? type.Name;
        if (!group.Commands.Contains(commandTypeName)) {
            group.Commands.Add(commandTypeName);
        }
    }

    private static bool HasStaticManifestMember(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type) {
        PropertyInfo? prop = type.GetProperty("Manifest", BindingFlags.Public | BindingFlags.Static);
        if (prop is not null && prop.PropertyType == typeof(DomainManifest)) {
            return true;
        }

        FieldInfo? field = type.GetField("Manifest", BindingFlags.Public | BindingFlags.Static);
        return field is not null && field.FieldType == typeof(DomainManifest);
    }

    private sealed class CommandGroup {

        public CommandGroup(string? displayName) => DisplayName = displayName;

        public List<string> Commands { get; } = [];
        public string? DisplayName { get; }
    }
}
