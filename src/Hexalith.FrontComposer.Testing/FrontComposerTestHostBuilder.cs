using System.Globalization;
using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Composable handle for the services and fakes registered by <see cref="FrontComposerTestHostServiceCollectionExtensions"/>.
/// </summary>
public sealed class FrontComposerTestHostBuilder
    : IDisposable {
    private readonly IDisposable _cultureScope;
    private bool _disposed;

    internal FrontComposerTestHostBuilder(
        BunitContext context,
        FrontComposerTestOptions options,
        FrontComposerTestUserContextAccessor userContext,
        TestCommandService commandService,
        TestQueryService queryService,
        TestProjectionPageLoader pageLoader,
        TestFaultEvidenceRecorder faultRecorder,
        TestAuthorizationEvaluator authorizationEvaluator,
        IDisposable cultureScope,
        bool storeInitialized) {
        Context = context;
        Options = options;
        UserContext = userContext;
        CommandService = commandService;
        QueryService = queryService;
        PageLoader = pageLoader;
        FaultRecorder = faultRecorder;
        AuthorizationEvaluator = authorizationEvaluator;
        StoreInitialized = storeInitialized;
        _cultureScope = cultureScope;
    }

    /// <summary>Gets the bUnit context configured by this builder.</summary>
    public BunitContext Context { get; }

    /// <summary>Gets the resolved test-host options.</summary>
    public FrontComposerTestOptions Options { get; }

    /// <summary>Gets the mutable user context for this test context.</summary>
    public FrontComposerTestUserContextAccessor UserContext { get; }

    /// <summary>Gets the fake command service and captured command evidence.</summary>
    public TestCommandService CommandService { get; }

    /// <summary>Gets the fake query service and captured query evidence.</summary>
    public TestQueryService QueryService { get; }

    /// <summary>Gets the fake page loader and captured page-load evidence.</summary>
    public TestProjectionPageLoader PageLoader { get; }

    /// <summary>Gets the deterministic fault provider used by reconnection tests.</summary>
    public TestFaultEvidenceRecorder FaultRecorder { get; }

    /// <summary>Gets the exact deterministic authorization evaluator registered in DI.</summary>
    public TestAuthorizationEvaluator AuthorizationEvaluator { get; }

    internal bool StoreInitialized { get; }

    /// <summary>
    /// Adds a generated domain assembly to the Fluxor scan list before the service provider is locked.
    /// </summary>
    /// <typeparam name="TMarker">A marker type from the generated domain assembly.</typeparam>
    /// <returns>The same builder for chaining.</returns>
    public FrontComposerTestHostBuilder AddDomainAssembly<TMarker>()
        where TMarker : class {
        Assembly assembly = typeof(TMarker).Assembly;
        if (!Options.DomainAssemblies.Contains(assembly)) {
            Options.DomainAssemblies.Add(assembly);
            _ = Context.Services.AddFluxor(o => o.ScanAssemblies(assembly));
            _ = Context.Services.AddHexalithDomain<TMarker>();
        }

        return this;
    }

    /// <summary>
    /// Validates that Testing, Shell, Contracts, and optional SourceTools packages use one aligned major/minor version.
    /// </summary>
    /// <param name="sourceToolsAssembly">Optional SourceTools assembly when a test host uses generator-driver helpers.</param>
    /// <exception cref="InvalidOperationException">Thrown when package versions are incompatible.</exception>
    public void ValidateVersionAlignment(Assembly? sourceToolsAssembly = null) {
        Assembly testing = GetType().Assembly;
        Assembly contracts = typeof(IUserContextAccessor).Assembly;
        Assembly shell = typeof(IProjectionPageLoader).Assembly;
        Assembly[] assemblies = sourceToolsAssembly is null
            ? [testing, contracts, shell]
            : [testing, contracts, shell, sourceToolsAssembly];

        (int Major, int Minor) expected = MajorMinor(testing);
        string[] mismatches = assemblies
            .Select(a => (Assembly: a, Version: MajorMinor(a)))
            .Where(x => x.Version != expected)
            .Select(x => $"{x.Assembly.GetName().Name} {x.Assembly.GetName().Version} expected {expected.Major}.{expected.Minor}.x")
            .ToArray();

        if (mismatches.Length > 0) {
            throw new InvalidOperationException(
                "FrontComposer test host package versions must align before rendering. " +
                string.Join("; ", mismatches));
        }
    }

    internal static IDisposable ApplyCulture(CultureInfo culture) {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        return new CultureScope(originalCulture, originalUICulture);
    }

    private static (int Major, int Minor) MajorMinor(Assembly assembly) {
        Version version = assembly.GetName().Version ?? new Version(0, 0);
        return (version.Major, version.Minor);
    }

    /// <summary>
    /// Restores culture settings applied by the test host.
    /// </summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _cultureScope.Dispose();
        _disposed = true;
    }

    private sealed class CultureScope(CultureInfo originalCulture, CultureInfo originalUICulture) : IDisposable {
        public void Dispose() {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}

/// <summary>
/// Service-collection extensions for the adopter-facing FrontComposer test host.
/// </summary>
public static class FrontComposerTestHostServiceCollectionExtensions {
    /// <summary>
    /// Registers FrontComposer Shell defaults plus deterministic test fakes in a bUnit context.
    /// </summary>
    /// <param name="services">The bUnit service collection.</param>
    /// <param name="context">The bUnit context whose JSInterop mode should be configured.</param>
    /// <param name="configure">Optional test-host configuration callback.</param>
    /// <returns>A builder exposing registered fakes and options.</returns>
    public static FrontComposerTestHostBuilder AddFrontComposerTestHost(
        this IServiceCollection services,
        BunitContext context,
        Action<FrontComposerTestOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(context);

        FrontComposerTestOptions options = new();
        configure?.Invoke(options);

        if (options.StoreInitialization == StoreInitializationMode.DuringHostSetup) {
            throw new InvalidOperationException(
                "StoreInitializationMode.DuringHostSetup requires AddFrontComposerTestHostAsync; synchronous setup never blocks on asynchronous store initialization.");
        }

        context.JSInterop.Mode = options.JSInteropMode;
        IDisposable cultureScope = FrontComposerTestHostBuilder.ApplyCulture(options.Culture);

        FrontComposerTestUserContextAccessor userContext = new() {
            TenantId = options.TestTenantId,
            UserId = options.TestUserId,
        };
        TestCommandPageContext commandPageContext = new(options.CommandName, options.BoundedContext, options.ReturnPath);
        TestCommandService commandService = new(userContext, commandPageContext, options);
        TestQueryService queryService = new(options);
        TestProjectionPageLoader pageLoader = new(options);
        TestFaultEvidenceRecorder faultRecorder = new(options);
        TestAuthorizationEvaluator authorizationEvaluator = new();

        _ = services.AddLocalization();
        _ = services.AddFluentUIComponents();
        _ = services.AddHexalithFrontComposer(o => {
            foreach (Assembly assembly in options.DomainAssemblies) {
                _ = o.ScanAssemblies(assembly);
            }
        });

        _ = services.Replace(ServiceDescriptor.Scoped(_ => userContext));
        _ = services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        _ = services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => userContext));
        _ = services.Replace(ServiceDescriptor.Scoped<ICommandPageContext>(_ => commandPageContext));
        _ = services.Replace(ServiceDescriptor.Scoped<ICommandServiceWithLifecycle>(_ => commandService));
        _ = services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        _ = services.Replace(ServiceDescriptor.Scoped<IQueryService>(_ => queryService));
        _ = services.Replace(ServiceDescriptor.Scoped<IProjectionPageLoader>(_ => pageLoader));
        _ = services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => authorizationEvaluator));
        _ = services.Replace(ServiceDescriptor.Scoped(_ => commandService));
        _ = services.Replace(ServiceDescriptor.Scoped(_ => queryService));
        _ = services.Replace(ServiceDescriptor.Scoped(_ => pageLoader));
        _ = services.Replace(ServiceDescriptor.Singleton(options.TimeProvider));
        _ = services.AddSingleton(faultRecorder);
        _ = services.AddSingleton(authorizationEvaluator);

        return new FrontComposerTestHostBuilder(
            context,
            options,
            userContext,
            commandService,
            queryService,
            pageLoader,
            faultRecorder,
            authorizationEvaluator,
            cultureScope,
            false);
    }

    /// <summary>Registers the test host and asynchronously initializes Fluxor when requested.</summary>
    public static async Task<FrontComposerTestHostBuilder> AddFrontComposerTestHostAsync(
        this IServiceCollection services,
        BunitContext context,
        Action<FrontComposerTestOptions>? configure = null,
        CancellationToken cancellationToken = default) {
        StoreInitializationMode requestedMode = StoreInitializationMode.OnDemand;
        CultureInfo requestedCulture = CultureInfo.CurrentCulture;
        FrontComposerTestHostBuilder host = services.AddFrontComposerTestHost(context, options => {
            configure?.Invoke(options);
            requestedMode = options.StoreInitialization;
            requestedCulture = options.Culture;
            options.StoreInitialization = StoreInitializationMode.OnDemand;
            // Culture is process/async-context state. Keep it unchanged across store initialization,
            // then apply the requested scope only after the await succeeds.
            options.Culture = CultureInfo.CurrentCulture;
        });
        if (requestedMode != StoreInitializationMode.DuringHostSetup) {
            return new FrontComposerTestHostBuilder(
                host.Context, host.Options, host.UserContext, host.CommandService, host.QueryService,
                host.PageLoader, host.FaultRecorder, host.AuthorizationEvaluator,
                new CompositeDisposable(host, FrontComposerTestHostBuilder.ApplyCulture(requestedCulture)), false);
        }

        try {
            cancellationToken.ThrowIfCancellationRequested();
            await context.Services.GetRequiredService<IStore>().InitializeAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new FrontComposerTestHostBuilder(
                host.Context, host.Options, host.UserContext, host.CommandService, host.QueryService,
                host.PageLoader, host.FaultRecorder, host.AuthorizationEvaluator,
                new CompositeDisposable(host, FrontComposerTestHostBuilder.ApplyCulture(requestedCulture)), true);
        }
        catch {
            host.Dispose();
            throw;
        }
    }

    private sealed class CompositeDisposable(IDisposable first, IDisposable second) : IDisposable {
        public void Dispose() {
            second.Dispose();
            first.Dispose();
        }
    }

    private sealed class TestCommandPageContext(string commandName, string boundedContext, string? returnPath)
        : ICommandPageContext {
        public string CommandName { get; set; } = commandName;

        public string BoundedContext { get; set; } = boundedContext;

        public string? ReturnPath { get; set; } = returnPath;
    }
}
