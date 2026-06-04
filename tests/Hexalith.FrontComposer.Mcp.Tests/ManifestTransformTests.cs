using Hexalith.FrontComposer.Contracts.Mcp;
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
    public void CommandDescriptor_CarriesStableCommandToolMetadata_AndServerControlledDerivables() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System.ComponentModel.DataAnnotations;
            namespace Orders.Write;
            [Command]
            [BoundedContext("Sales")]
            [Display(Name = "Approve Order")]
            [RequiresPolicy("OrderApprover")]
            public partial class ApproveOrderCommand {
                public string TenantId { get; set; } = "";
                public string UserId { get; set; } = "";
                public string MessageId { get; set; } = "";
                public string CommandId { get; set; } = "";
                public string CorrelationId { get; set; } = "";
                [Display(Name = "Order number")]
                public string OrderNumber { get; set; } = "";
                public object Metadata { get; set; } = new();
            }
            """;

        var parsed = SourceToolCompilationHelper.ParseCommand(Source, "Orders.Write.ApproveOrderCommand").Model!;
        var descriptor = McpManifestTransform.TransformCommand(parsed);

        descriptor.ProtocolName.ShouldBe("Sales.ApproveOrderCommand.Execute");
        descriptor.CommandTypeName.ShouldBe("Orders.Write.ApproveOrderCommand");
        descriptor.BoundedContext.ShouldBe("Sales");
        descriptor.Title.ShouldBe("Approve Order");
        descriptor.AuthorizationPolicyName.ShouldBe("OrderApprover");
        descriptor.Parameters.Select(p => p.Name).ShouldBe(["OrderNumber", "Metadata"]);
        descriptor.Parameters.Single(p => p.Name == "Metadata").IsUnsupported.ShouldBeTrue();
        descriptor.DerivablePropertyNames.ShouldContain("TenantId");
        descriptor.DerivablePropertyNames.ShouldContain("UserId");
        descriptor.DerivablePropertyNames.ShouldContain("MessageId");
        descriptor.DerivablePropertyNames.ShouldContain("CommandId");
        descriptor.DerivablePropertyNames.ShouldContain("CorrelationId");
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

    [Fact]
    public void ProjectionDescriptor_CarriesSourceToolsRenderMetadata() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System;
            using System.ComponentModel;
            using System.ComponentModel.DataAnnotations;
            namespace Orders;
            public enum QueueStatus {
                [ProjectionBadge(BadgeSlot.Warning)]
                Pending,
                [ProjectionBadge(BadgeSlot.Success)]
                Approved
            }
            [Projection]
            [ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Pending")]
            [ProjectionEmptyStateCta("CreateOrderCommand")]
            [BoundedContext("Sales")]
            [Display(Name = "Order queue", GroupName = "Orders")]
            public partial class OrderQueueProjection {
                [Display(Name = "Order number", Description = "Stable public order identifier")]
                public string Number { get; set; } = "";
                public QueueStatus Status { get; set; }
                [RelativeTime(14)]
                public DateTimeOffset UpdatedAt { get; set; }
            }
            """;

        var parsed = SourceToolCompilationHelper.ParseProjection(Source, "Orders.OrderQueueProjection").Model!;
        var descriptor = McpManifestTransform.TransformProjection(parsed);

        descriptor.RenderStrategy.ShouldBe("ActionQueue");
        descriptor.EntityPluralLabel.ShouldBe("Orders");
        descriptor.EmptyStateCtaCommandName.ShouldBe("CreateOrderCommand");
        descriptor.Fields.Select(f => f.Name).ShouldBe(["Number", "Status", "UpdatedAt"]);
        descriptor.Fields[0].Description.ShouldBe("Stable public order identifier");
        descriptor.Fields[1].BadgeMappings.ShouldNotBeNull();
        descriptor.Fields[1].BadgeMappings!["Pending"].ShouldBe("Warning");
        descriptor.Fields[1].BadgeMappings!["Approved"].ShouldBe("Success");
        descriptor.Fields[2].DisplayFormat.ShouldBe("RelativeTime");
    }

    [Fact]
    public void PublicResourceDescriptor_UsesTypedRenderStrategy() {
        Type propertyType = typeof(McpResourceDescriptor)
            .GetProperty(nameof(McpResourceDescriptor.RenderStrategy))!
            .PropertyType;

        propertyType.ShouldBe(typeof(McpProjectionRenderStrategy));
    }
}
