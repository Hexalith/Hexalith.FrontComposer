using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.DevMode;
using Hexalith.FrontComposer.Shell.Services.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

public sealed class AddFrontComposerDevModeExtensionsTests {
#if DEBUG
    // D2 (Story 9-2): Development-time registrations are guarded by `#if DEBUG` for compile-time elimination
    // in Release. This test exercises the Debug-build registration path and is therefore DEBUG-scoped.
    // The Release-build behavior (no dev-mode services even in Development environment) is covered by the
    // production / staging tests below, which pass under both configurations.
    [Fact]
    public void AddFrontComposerDevMode_RegistersDevModeServicesInDevelopment() {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));

        services.AddFrontComposerDevMode();

        services.ShouldContain(d => d.ServiceType == typeof(IDevModeOverlayController) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IRazorEmitter) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IClipboardJSModule) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IDevModeAnnotationSnapshotVisitor) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(ICustomizationContractMismatchDiagnosticProvider) && d.Lifetime == ServiceLifetime.Scoped);
    }
#endif

#if DEBUG
    [Fact]
    public void AddFrontComposerDevMode_RegistersDevModeServicesWithFactoryRegisteredDevelopmentEnvironment() {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(_ => new TestHostEnvironment("Development"));

        services.AddFrontComposerDevMode();

        services.ShouldContain(d => d.ServiceType == typeof(IDevModeOverlayController) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IRazorEmitter) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IClipboardJSModule) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(IDevModeAnnotationSnapshotVisitor) && d.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(d => d.ServiceType == typeof(ICustomizationContractMismatchDiagnosticProvider) && d.Lifetime == ServiceLifetime.Scoped);
    }
#endif

#if !DEBUG
    [Fact]
    public void AddFrontComposerDevMode_ReleaseDevelopmentEnvironmentResolvesNoContractMismatchProvider() {
        ServiceCollection services = [];
        services.AddLogging();

        services.AddFrontComposerDevMode(new TestHostEnvironment("Development"));

        services.ShouldNotContain(d => d.ServiceType == typeof(ICustomizationContractMismatchDiagnosticProvider));
    }
#endif

    [Fact]
    public void AddFrontComposerDevMode_IsNoOpOutsideDevelopment() {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Production"));

        services.AddFrontComposerDevMode();

        services.ShouldNotContain(d => d.ServiceType == typeof(IDevModeOverlayController));
        services.ShouldNotContain(d => d.ServiceType == typeof(IRazorEmitter));
        services.ShouldNotContain(d => d.ServiceType == typeof(IClipboardJSModule));
        services.ShouldNotContain(d => d.ServiceType == typeof(IDevModeAnnotationSnapshotVisitor));
    }

    [Fact]
    public void AddFrontComposerDevMode_ProductionEnvironmentResolvesNoDevModeServices() {
        // DN3 / AC2 production-exclusion smoke — explicit overload with Production env must
        // result in null DI resolutions for every dev-mode-only contract. This is the runtime
        // half of the AC2 defense-in-depth pair (the #if DEBUG half is enforced by compilation).
        ServiceCollection services = [];
        services.AddLogging();

        services.AddFrontComposerDevMode(new TestHostEnvironment("Production"));

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IDevModeOverlayController>().ShouldBeNull();
        scope.ServiceProvider.GetService<IRazorEmitter>().ShouldBeNull();
        scope.ServiceProvider.GetService<IClipboardJSModule>().ShouldBeNull();
        scope.ServiceProvider.GetService<IDevModeAnnotationSnapshotVisitor>().ShouldBeNull();
        scope.ServiceProvider.GetService<ICustomizationContractMismatchDiagnosticProvider>().ShouldBeNull();
    }

    [Fact]
    public void AddFrontComposerDevMode_StagingEnvironmentResolvesNoDevModeServices() {
        // DN3 / AC11 — non-Development, non-Production environments (Staging / QA / custom names)
        // must also fail-closed. IHostEnvironment.IsDevelopment() returns true ONLY for
        // EnvironmentName == "Development", so any other value should suppress registration.
        ServiceCollection services = [];
        services.AddLogging();

        services.AddFrontComposerDevMode(new TestHostEnvironment("Staging"));

        services.ShouldNotContain(d => d.ServiceType == typeof(IDevModeOverlayController));
        services.ShouldNotContain(d => d.ServiceType == typeof(IRazorEmitter));
        services.ShouldNotContain(d => d.ServiceType == typeof(IClipboardJSModule));
        services.ShouldNotContain(d => d.ServiceType == typeof(ICustomizationContractMismatchDiagnosticProvider));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
