namespace Hexalith.FrontComposer.Shell.Tests.State;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Tests verifying DI registration via <see cref="ServiceCollectionExtensions.AddHexalithFrontComposer"/>.
/// </summary>
public class FluxorRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddHexalithFrontComposer();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStore()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        provider.GetService<IStore>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIDispatcher()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        provider.GetService<IDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesAllStateTypes()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        provider.GetService<IState<FrontComposerThemeState>>().ShouldNotBeNull();
        provider.GetService<IState<FrontComposerDensityState>>().ShouldNotBeNull();
    }

    [Fact]
    public void FluxorRegistration_AddHexalithFrontComposer_ResolvesIStorageService()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider();

        // Act & Assert
        provider.GetService<IStorageService>().ShouldNotBeNull();
        provider.GetService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
    }
}
