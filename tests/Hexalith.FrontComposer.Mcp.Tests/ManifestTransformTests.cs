using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

public sealed class ManifestTransformTests {
    [Fact]
    public void CommandDescriptor_ExcludesDerivableFields_AndCarriesPolicy() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System.ComponentModel.DataAnnotations;
            namespace Orders;
            [Command]
            [BoundedContext("Sales")]
            [Display(Name = "Approve Order")]
            [RequiresPolicy("OrderApprover")]
            public partial class ApproveOrderCommand {
                public string MessageId { get; set; } = "";
                public string TenantId { get; set; } = "";
                [Display(Name = "Order number")]
                public string OrderNumber { get; set; } = "";
            }
            """;

        var parsed = SourceToolCompilationHelper.ParseCommand(Source, "Orders.ApproveOrderCommand").Model!;
        var descriptor = McpManifestTransform.TransformCommand(parsed);

        descriptor.ProtocolName.ShouldBe("Sales.ApproveOrderCommand.Execute");
        descriptor.Title.ShouldBe("Approve Order");
        descriptor.AuthorizationPolicyName.ShouldBe("OrderApprover");
        descriptor.Parameters.Single().Name.ShouldBe("OrderNumber");
        descriptor.Parameters.Single().Title.ShouldBe("Order number");
        descriptor.DerivablePropertyNames.ShouldContain("TenantId");
        descriptor.Parameters.Select(p => p.Name).ShouldNotContain("TenantId");
    }

    [Fact]
    public void ProjectionDescriptor_UsesStableUri_AndDisplayName() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System.ComponentModel.DataAnnotations;
            namespace Orders;
            [Projection]
            [BoundedContext("Sales")]
            [Display(Name = "Order queue")]
            public partial class OrderQueueProjection {
                [Display(Name = "Order number")]
                public string Number { get; set; } = "";
            }
            """;

        var parsed = SourceToolCompilationHelper.ParseProjection(Source, "Orders.OrderQueueProjection").Model!;
        var descriptor = McpManifestTransform.TransformProjection(parsed);

        descriptor.ProtocolUri.ShouldBe("frontcomposer://Sales/projections/OrderQueueProjection");
        descriptor.Title.ShouldBe("Order queue");
        descriptor.Fields.Single().Title.ShouldBe("Order number");
    }
}
