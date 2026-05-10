using Hexalith.FrontComposer.Shell.Components.Specimens;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Specimens;

public class FrontComposerSpecimenRouteGateTests {
    [Theory]
    [InlineData("Production", "true", false)]
    [InlineData("Staging", "true", false)]
    [InlineData("Development", "false", false)]
    [InlineData("Test", "false", false)]
    [InlineData("Development", "true", true)]
    [InlineData("Test", "true", true)]
    public void IsEnabledRequiresExplicitConfigurationAndDevelopmentOrTestEnvironment(
        string environmentName,
        string enabled,
        bool expected) {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                [FrontComposerSpecimenRoutes.EnabledConfigurationKey] = enabled,
            })
            .Build();

        FrontComposerSpecimenRoutes.IsEnabled(configuration, new TestHostEnvironment(environmentName))
            .ShouldBe(expected);
    }

    [Fact]
    public void RouteContractStaysStableForPlaywrightManifest() {
        FrontComposerSpecimenRoutes.TypeSpecimen.ShouldBe("/__frontcomposer/specimens/type");
        FrontComposerSpecimenRoutes.DataFormattingSpecimen.ShouldBe("/__frontcomposer/specimens/data-formatting");
        FrontComposerSpecimenRoutes.OwnerStoryKey.ShouldBe("10-2-accessibility-ci-gates-and-visual-specimen-verification");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Hexalith.FrontComposer.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}
