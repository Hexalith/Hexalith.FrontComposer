using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Fluxor;
using Fluxor.DependencyInjection;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Infrastructure.Storage;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.Services.DerivedValues;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Validation;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ETagCache;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Extension methods for registering FrontComposer Shell services.
/// </summary>
public static class ServiceCollectionExtensions
{

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
        var commandGroups = new Dictionary<string, CommandGroup>(StringComparer.Ordinal);

        foreach (Type type in domainAssembly.GetExportedTypes())
        {
            if (type.Name.EndsWith("LastUsedSubscriber", StringComparison.Ordinal)
                && typeof(IDisposable).IsAssignableFrom(type))
            {
                services.TryAdd(ServiceDescriptor.Scoped(type, type));
            }

            // Story 2-3 Decision D5 — auto-register per-command {Command}LifecycleBridge types discovered
            // in the domain assembly so LifecycleBridgeRegistry.Ensure<T>() can resolve them via DI.
            if (type.Name.EndsWith("LifecycleBridge", StringComparison.Ordinal)
                && typeof(IDisposable).IsAssignableFrom(type))
            {
                services.TryAdd(ServiceDescriptor.Scoped(type, type));
            }

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
                _ = services.AddSingleton(new DomainRegistrationAction(registerMethod));
            }
            else if (hasManifest || registerMethod is not null)
            {
                _ = services.AddSingleton(new DomainRegistrationWarning(
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

            _ = services.AddSingleton(new DomainRegistrationAction(registry => registry.RegisterDomain(manifest)));
        }

        return services;
    }

    /// <summary>
    /// Registers Fluxor state management, storage services, and all FrontComposer Shell dependencies.
    /// <para>
    /// <b>Important:</b> <c>FrontComposerShell</c> now owns the
    /// <c>&lt;Fluxor.Blazor.Web.StoreInitializer /&gt;</c> placement. Adopters should render
    /// the shell from their layout instead of mounting a second initializer manually.
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
    /// labels. <see cref="AddHexalithFrontComposerQuickstart"/> is available for hosts that want
    /// the framework-owned localization defaults in a single fluent call.
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
        Action<FluxorOptions>? configureFluxor = null)
    {
        _ = services.AddLogging();
        _ = services.AddFluxor(o =>
        {
            _ = o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly);
            configureFluxor?.Invoke(o);
        });
        // Story 3-1 ADR-030 — IStorageService moves from Singleton to Scoped. RemoveAll
        // guarantees no stale descriptor survives even if an adopter pre-registered, then
        // AddScoped (not TryAddScoped — we want the registration to be authoritative) installs
        // LocalStorageService as the default for both Blazor Server and WASM hosts. Test hosts
        // override with InMemoryStorageService via services.Replace.
        // Breaking lifetime change — ships as v0.2.0-preview. Adopters with Singleton captures
        // must migrate to Scoped or capture IServiceScopeFactory; Counter.Web enables
        // ValidateScopes = true on its host builder so future regressions fail at boot.
        services.RemoveAll<IStorageService>();
        _ = services.AddScoped<IStorageService, LocalStorageService>();
        _ = services.AddSingleton<IFrontComposerRegistry, FrontComposerRegistry>();
        // P-27 (DN1-c): EmptyStateCtaResolver throws at construction if the registered
        // IFrontComposerRegistry does not also implement IFrontComposerCommandWriteAccessRegistry.
        // The optional-companion fallback (returning true for every command) silently surfaces
        // read-only Query types as empty-state CTAs in production; adopters MUST opt-in by
        // implementing the companion interface (or by registering a different registry that does).
        services.TryAddScoped<IEmptyStateCtaResolver, EmptyStateCtaResolver>();

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
        if (offendingPopoverRegistry is not null)
        {
            throw new InvalidOperationException(
                $"InlinePopoverRegistry must be registered as Scoped (found: {offendingPopoverRegistry.Lifetime}). "
                + "Singleton or Transient registration would cross-leak popovers between user circuits.");
        }

        services.TryAddScoped<Hexalith.FrontComposer.Contracts.Rendering.InlinePopoverRegistry>();

        // Default no-op ICommandPageContext — adopter-hosted pages override via scoped registration.
        services.TryAddScoped<ICommandPageContext, NullCommandPageContext>();

        // Story 3-4 D2 / Task 1.5 — IShortcutService is Scoped (per-circuit / per-user; mirrors
        // IStorageService ADR-030). The registrar is also Scoped so its idempotency flag (D24) lives
        // for one circuit's lifetime.
        services.TryAddScoped<IShortcutService, ShortcutService>();
        services.TryAddScoped<FrontComposerShortcutRegistrar>();

        // Story 3-4 — CommandPaletteEffects must be discoverable by Fluxor's effect scan AND
        // available as a concrete instance for IDisposable cleanup on circuit teardown. Scoped per
        // ADR-030 precedent.
        services.TryAddScoped<CommandPaletteEffects>();

        // Story 3-5 D1 / D2 / D3 — badge count producer seam. Consumed via nullable DI by
        // Story 3-4's FcPaletteResultList (activates automatically when this registration lands)
        // and by Story 3-5's FcHomeDirectory + FrontComposerNavigation via Fluxor state.
        services.TryAddSingleton<IActionQueueProjectionCatalog>(sp =>
#pragma warning disable IL2026 // RequiresUnreferencedCode — Reflection catalog documented as AOT-incompatible (G22 / Story 9-1 analyzer).
            new ReflectionActionQueueProjectionCatalog(
                AppDomain.CurrentDomain.GetAssemblies(),
                sp.GetRequiredService<ILogger<ReflectionActionQueueProjectionCatalog>>()));
#pragma warning restore IL2026
        services.TryAddScoped<IActionQueueCountReader, NullActionQueueCountReader>();
        _ = services.AddScoped<IBadgeCountService, BadgeCountService>();

        // Story 3-5 D9 / ADR-046 — capability-discovery effects mirror CommandPaletteEffects
        // (Scoped per ADR-030; concrete instance held for IDisposable bridge teardown).
        services.TryAddScoped<CapabilityDiscoveryEffects>();

        // Story 3-6 D20 / ADR-049 — IScopeReadinessGate + ScopeFlipObserverEffect (per-circuit
        // scoped so the Interlocked tiebreaker observes the same "already-dispatched" state
        // across Fluxor's concurrent effect-handler invocations).
        services.TryAddScoped<IScopeReadinessGate, ScopeReadinessGate>();
        services.TryAddScoped<ScopeFlipObserverEffect>();

        // Story 3-6 ADR-050 — DataGrid per-view persistence effects (Scoped; concrete instance
        // held for IDisposable cleanup of debounce CTSes on circuit teardown).
        services.TryAddScoped<DataGridNavigationEffects>();

        // Story 4-3 T3 / T6 — filter-surface effects + DataGrid focus-scope service.
        services.TryAddScoped<FilterEffects>();
        services.TryAddScoped<Services.DataGridFocusScope>();

        // Story 4-4 D2 / D3 / D10 — virtualization effects + page-loader boundary (Story 4-4 D16).
        // Default fail-fast loader keeps wiring compilable while surfacing missing adopter paging
        // integration as an explicit LoadPageFailedAction once the server-side lane is needed.
        services.TryAddScoped<LoadedPageReducers>();
        services.TryAddScoped<LoadPageEffects>();
        services.TryAddScoped<ScrollPersistenceEffect>();
        services.TryAddScoped<ColumnVisibilityPersistenceEffect>();
        services.TryAddScoped<IProjectionPageLoader, NullProjectionPageLoader>();
        services.TryAddScoped<DataGridScrollInterop>();

        // Story 5-2 T2 — opportunistic ETag cache layered on IStorageService. Scoped per
        // circuit / per user (mirrors LocalStorageService lifetime). Adopters that wire
        // EventStore via AddHexalithEventStore inherit this default automatically.
        services.TryAddScoped<IETagCache, ETagCacheService>();
        services.TryAddScoped<IProjectionConnectionState, ProjectionConnectionStateService>();
        services.TryAddScoped<IProjectionFallbackRefreshScheduler, ProjectionFallbackRefreshScheduler>();

        // Story 5-2 D8 — fail-fast default IAuthRedirector. Scoped because adopter
        // implementations typically capture per-circuit NavigationManager. The default throws
        // on invocation so 401 responses never silently disappear.
        services.TryAddScoped<IAuthRedirector, NoOpAuthRedirector>();

        // Story 5-2 T5 — framework-owned warning publisher. Scoped per circuit; subscribers
        // detach via the disposable returned by Subscribe. The validation applicator is
        // exposed as a stateless static helper (no DI registration needed).
        services.TryAddScoped<ICommandFeedbackPublisher, CommandFeedbackPublisher>();

        // Story 4-5 D2 / T3.3 — ExpandedRowFeature is auto-discovered by the AddFluxor
        // ScanAssemblies(typeof(FrontComposerThemeState).Assembly) call above (line 158).
        // The static ExpandedRowReducers class is also picked up by the Fluxor reducer
        // scanner; no explicit registration needed for the ephemeral feature.

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
    /// Chains Shell request-localization defaults into the adopter's DI pipeline (Story 3-1
    /// D24 / AC7). Adopters are expected to have called <c>services.AddLocalization()</c> before
    /// this — the framework deliberately does NOT double-add <see cref="AddLocalization"/> so
    /// adopters retain authoritative control over <see cref="Microsoft.Extensions.Localization.LocalizationOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>FcShellResources</c>'s resx files are picked up by the default ASP.NET Core resource
    /// convention (resx BaseName == typeof(T).FullName). Register a different <see cref="IStringLocalizer{T}"/>
    /// implementation via <c>services.Replace</c> if you need to swap sources (e.g. a database-backed
    /// localizer for an enterprise whitelabel deployment).
    /// </para>
    /// <para>
    /// Use the fluent chain:
    /// <code>
    /// services.AddLocalization()
    ///         .AddHexalithShellLocalization()
    ///         .AddHexalithFrontComposer();
    /// </code>
    /// Or use the <see cref="AddHexalithFrontComposerQuickstart"/> sugar for a one-line setup.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional request-localization customization hook.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithShellLocalization(
        this IServiceCollection services,
        Action<RequestLocalizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<RequestLocalizationOptions>, ShellRequestLocalizationOptionsSetup>());
        if (configure is not null)
        {
            _ = services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Opt-IN sugar that chains <c>services.AddLocalization()</c> + <see cref="AddHexalithShellLocalization"/>
    /// + <see cref="AddHexalithFrontComposer"/> into a single call (Story 3-1 D28). Intended for
    /// first-time adopters: the granular 3-call path stays the non-deprecated primary API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Idempotent against duplicate <c>AddLocalization()</c> calls per ASP.NET Core conventions —
    /// the underlying registration uses <c>TryAdd</c> internally so calling it twice is a no-op.
    /// </para>
    /// <para>
    /// Pair with <c>builder.Host.UseDefaultServiceProvider(o =&gt; o.ValidateScopes = true)</c>
    /// so Singleton-captures of <see cref="IStorageService"/> (now Scoped per ADR-030) fail at
    /// boot instead of leaking a single circuit's writes across tenants.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureFluxor">Optional Fluxor configuration (assembly scans etc.).</param>
    /// <param name="configureLocalization">Optional request-localization customization hook.</param>
    /// <returns>The service collection for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "Quickstart delegates to AddHexalithFrontComposer, which is separately annotated.")]
    public static IServiceCollection AddHexalithFrontComposerQuickstart(
        this IServiceCollection services,
        Action<FluxorOptions>? configureFluxor = null,
        Action<RequestLocalizationOptions>? configureLocalization = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        // AddLocalization is idempotent (TryAdd under the hood), so chaining it here never collides
        // with an adopter's own call.
        _ = services.AddLocalization();
        // P-26 (DN2-a, Pass-3 review fix): empty-state CTA wraps in <AuthorizeView> per AC2.5.
        // <AuthorizeView> requires IAuthorizationService; without it, an empty-state CTA render
        // throws InvalidOperationException at first user-visible empty grid. AddAuthorizationCore
        // is idempotent (TryAdd) and brings only the contract pieces (no policy evaluation
        // pipeline, no authentication scheme), so a quickstart adopter without auth still gets
        // the anonymous-user fall-through (default <AuthorizeView Policy=null> → user must be
        // authenticated → anonymous skips the CTA gracefully).
        _ = services.AddAuthorizationCore();
        _ = AddHexalithShellLocalization(services, configureLocalization);
        _ = AddHexalithFrontComposer(services, configureFluxor);
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
        where T : class, IDerivedValueProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        ServiceDescriptor descriptor = ServiceDescriptor.Describe(typeof(IDerivedValueProvider), typeof(T), lifetime);

        // Prepend so custom providers come first in enumeration order.
        services.Insert(0, descriptor);
        return services;
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

    private sealed class CommandGroup
    {

        public CommandGroup(string? displayName) => DisplayName = displayName;

        public List<string> Commands { get; } = [];
        public string? DisplayName { get; }
    }

    private sealed class ShellRequestLocalizationOptionsSetup(IOptions<FcShellOptions> shellOptions)
        : IConfigureOptions<RequestLocalizationOptions>
    {

        public void Configure(RequestLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            FcShellOptions shell = shellOptions.Value;
            CultureInfo defaultCulture = new(shell.DefaultCulture);
            IReadOnlyList<string> configuredCultures = shell.SupportedCultures;
            List<CultureInfo> supportedCultures = [.. configuredCultures.Select(static culture => new CultureInfo(culture))];

            options.DefaultRequestCulture = new RequestCulture(defaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        }
    }
}
