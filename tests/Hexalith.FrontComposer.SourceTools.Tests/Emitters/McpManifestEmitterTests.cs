using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class McpManifestEmitterTests {
    [Fact]
    public void Emit_CommandDescriptors_OnePerCommand_WithNamespaceDisambiguationForDuplicateBaseNames() {
        const string OrdersSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System.ComponentModel.DataAnnotations;
            namespace Orders;
            [Command]
            [BoundedContext("Sales")]
            [Display(Name = "Approve Order")]
            [RequiresPolicy("OrderApprover")]
            public partial class ApproveCommand {
                public string TenantId { get; set; } = "";
                public string UserId { get; set; } = "";
                public string MessageId { get; set; } = "";
                public string CommandId { get; set; } = "";
                public string CorrelationId { get; set; } = "";
                [Display(Name = "Order number")]
                public string OrderNumber { get; set; } = "";
            }
            """;
        const string BillingSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Billing;
            [Command]
            [BoundedContext("Sales")]
            public partial class ApproveCommand {
                public string MessageId { get; set; } = "";
                public decimal Amount { get; set; }
            }
            """;

        var orders = CompilationHelper.ParseCommand(OrdersSource, "Orders.ApproveCommand").Model!;
        var billing = CompilationHelper.ParseCommand(BillingSource, "Billing.ApproveCommand").Model!;

        string source = McpManifestEmitter.Emit(ImmutableArray.Create(orders, billing), []);

        source.Split("new McpCommandDescriptor(").Length.ShouldBe(3);
        source.ShouldContain("\"Sales.Orders.ApproveCommand.Execute\"");
        source.ShouldContain("\"Sales.Billing.ApproveCommand.Execute\"");
        source.ShouldContain("\"Orders.ApproveCommand\"");
        source.ShouldContain("\"Billing.ApproveCommand\"");
        source.ShouldContain("\"OrderApprover\"");
        source.ShouldContain("\"OrderNumber\"");
        source.ShouldContain("\"TenantId\"");
        source.ShouldContain("\"UserId\"");
        source.ShouldContain("\"MessageId\"");
        source.ShouldContain("\"CommandId\"");
        source.ShouldContain("\"CorrelationId\"");
        source.ShouldContain("new SchemaFingerprint(\"frontcomposer.schema.sha256.v1.sourcetools-blob\"");
        source.ShouldNotContain("\"Sales.ApproveCommand.Execute\"");
    }

    [Fact]
    public void Emit_ProjectionDescriptor_UsesTypedRenderStrategyLiteral() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Orders;
            [Projection]
            [BoundedContext("Sales")]
            [ProjectionRole(ProjectionRole.Timeline)]
            public partial class OrderTimelineProjection {
                public string Number { get; set; } = "";
            }
            """;

        var parsed = CompilationHelper.ParseProjection(Source, "Orders.OrderTimelineProjection").Model!;

        string source = McpManifestEmitter.Emit([], ImmutableArray.Create(parsed));

        source.ShouldContain("McpProjectionRenderStrategy.Timeline");
        source.ShouldNotContain("\"Timeline\",");
        // DN-2 / D23: SourceTools-emitted fingerprints declare a distinct algorithm id
        // because the build-time canonicalizer produces a key=value text blob (not JSON).
        source.ShouldContain("new SchemaFingerprint(\"frontcomposer.schema.sha256.v1.sourcetools-blob\"");
        source.ShouldContain("using Hexalith.FrontComposer.Contracts.Schema;");
    }
}
