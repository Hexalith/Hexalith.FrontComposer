using Hexalith.FrontComposer.Contracts.Registration;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Registration;

public sealed class DomainManifestCompatibilityTests {
    [Fact]
    public void FullPageMetadata_PreservesEightValueConstructionAndDeconstruction() {
        DomainManifest manifest = new(
            "Orders",
            "Orders",
            ["Orders.ListProjection"],
            ["Orders.CreateCommand"],
            null,
            "OrdersIcon",
            "OrdersName",
            typeof(DomainManifestCompatibilityTests)) {
            FullPageCommands = ["Orders.CreateCommand"],
        };

        (string name, string boundedContext, IReadOnlyList<string> projections, IReadOnlyList<string> commands, _, _, _, _) = manifest;

        name.ShouldBe("Orders");
        boundedContext.ShouldBe("Orders");
        projections.ShouldBe(["Orders.ListProjection"]);
        commands.ShouldBe(["Orders.CreateCommand"]);
        manifest.FullPageCommands.ShouldBe(["Orders.CreateCommand"]);
    }
}
