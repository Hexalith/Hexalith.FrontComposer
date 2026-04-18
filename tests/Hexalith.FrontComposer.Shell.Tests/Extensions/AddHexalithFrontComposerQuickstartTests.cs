using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>
/// Story 3-1 Task 10.14 (D28) — Quickstart sugar extension tests.
/// </summary>
public sealed class AddHexalithFrontComposerQuickstartTests
{
    [Fact]
    public void ResolvesIStorageServiceAsScoped()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IStorageService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);

        using IServiceScope scope = provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IStorageService>().ShouldNotBeNull();
    }

    [Fact]
    public void ResolvesIStringLocalizer()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();
        localizer.ShouldNotBeNull();
    }

    [Fact]
    public void IdempotentAgainstDuplicateAddLocalization()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddLocalization(); // adopter registers FIRST
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Both services resolve cleanly — no duplicate-registration exception.
        _ = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>().ShouldNotBeNull();
        _ = scope.ServiceProvider.GetRequiredService<IStorageService>().ShouldNotBeNull();
    }

    [Fact]
    public void ConfiguresRequestLocalizationFromShellOptions()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.Configure<FcShellOptions>(o =>
        {
            o.DefaultCulture = "fr";
            o.SupportedCultures = ["fr", "en"];
        });
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        RequestLocalizationOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<RequestLocalizationOptions>>()
            .Value;

        options.DefaultRequestCulture.Culture.Name.ShouldBe("fr");
        options.SupportedCultures!.Select(c => c.Name).ShouldBe(["fr", "en"], ignoreOrder: true);
        options.SupportedUICultures!.Select(c => c.Name).ShouldBe(["fr", "en"], ignoreOrder: true);
    }
}
