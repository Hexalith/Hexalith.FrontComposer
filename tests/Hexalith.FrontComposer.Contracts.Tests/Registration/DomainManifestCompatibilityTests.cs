using Hexalith.FrontComposer.Contracts.Registration;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Registration;

public sealed class DomainManifestCompatibilityTests {
    [Fact]
    public void CommandPolicyMetadataEnumeratesOnlyDeclaredPoliciesByQualifiedTypeName()
    {
        const string approveCommand = "Orders.ApproveCommand";
        const string rejectCommand = "Orders.RejectCommand";
        const string browseCommand = "Orders.BrowseCommand";
        DomainManifest legacyManifest = new("Orders", "Orders", [], [approveCommand, rejectCommand, browseCommand]);
        DomainManifest manifest = legacyManifest with {
            CommandPolicies = new Dictionary<string, string>(StringComparer.Ordinal) {
                [approveCommand] = "Orders.Approve",
                [rejectCommand] = "Orders.Reject",
            },
        };

        (string _, string _, IReadOnlyList<string> _, IReadOnlyList<string> commands,
            IReadOnlyDictionary<string, string>? policies, _, _, _) = manifest;

        commands.ShouldBe([approveCommand, rejectCommand, browseCommand]);
        policies.ShouldNotBeNull();
        policies.OrderBy(policy => policy.Key, StringComparer.Ordinal).ShouldBe([
            KeyValuePair.Create(approveCommand, "Orders.Approve"),
            KeyValuePair.Create(rejectCommand, "Orders.Reject"),
        ]);
        legacyManifest.CommandPolicies.ShouldBeEmpty();
        // A caller with nullable annotations disabled can still pass null at runtime.
        (manifest with { CommandPolicies = null! }).CommandPolicies.ShouldBeEmpty();
        new DomainManifest("Orders", "Orders", [], [browseCommand], null).CommandPolicies.ShouldBeEmpty();
    }

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
