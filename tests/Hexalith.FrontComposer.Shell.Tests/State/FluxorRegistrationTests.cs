using System.Linq;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;

/// <summary>
/// Tests verifying DI registration via <see cref="ServiceCollectionExtensions.AddHexalithFrontComposer"/>.
/// </summary>
public class FluxorRegistrationTests {

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesAllStateTypes() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IState<FrontComposerThemeState>>().ShouldNotBeNull();
        _ = provider.GetService<IState<FrontComposerDensityState>>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIDispatcher() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStorageService() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IStorageService>().ShouldNotBeNull();
        _ = provider.GetService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStore() {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        _ = provider.GetService<IStore>().ShouldNotBeNull();
    }

    [Fact]
    public void Fluxor_AssemblyScan_NoDuplicateRegistration() {
        ServiceCollection services = new();

        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();

        services.Count(d => d.ServiceType == typeof(IStore)).ShouldBe(1);
    }

    private static ServiceProvider BuildProvider() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();
        return services.BuildServiceProvider();
    }
}
