using System.Globalization;
using System.Resources;

using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Resources;

/// <summary>
/// Story 3-1 Task 10.7 (D12 / AC7) — resource parity + culture round-trip tests.
/// </summary>
public sealed class FcShellResourcesTests {
    [Fact]
    public void CanonicalKeysHaveFrenchCounterparts() {
        ResourceManager manager = new(typeof(FcShellResources));
        ResourceSet enSet = manager.GetResourceSet(new CultureInfo("en"), createIfNotExists: true, tryParents: true)!;
        ResourceSet frSet = manager.GetResourceSet(new CultureInfo("fr"), createIfNotExists: true, tryParents: false)!;

        HashSet<string> enKeys = [];
        foreach (System.Collections.DictionaryEntry entry in enSet) {
            enKeys.Add((string)entry.Key);
        }

        HashSet<string> frKeys = [];
        foreach (System.Collections.DictionaryEntry entry in frSet) {
            frKeys.Add((string)entry.Key);
        }

        string[] missing = [.. enKeys.Where(k => !frKeys.Contains(k))];
        missing.ShouldBeEmpty();
    }

    [Fact]
    public void ThemeToggleAriaLabelResolvesInFrench() {
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();
        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

        CultureInfo previous = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            localizer["ThemeToggleAriaLabel"].Value.ShouldBe("Changer de thème");
        }
        finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void AddHexalithShellLocalizationRegistersLocalizer() {
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();

        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

        localizer.ShouldNotBeNull();
        localizer["AppTitle"].Value.ShouldBe("Hexalith FrontComposer");
    }

    private static ServiceProvider BuildLocalizedProvider() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddLocalization();
        _ = Shell.Extensions.ServiceCollectionExtensions.AddHexalithShellLocalization(services);
        return services.BuildServiceProvider();
    }
}
