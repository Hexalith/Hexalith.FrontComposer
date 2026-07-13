using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class McpManifestEmitterTests {
    [Fact]
    public void Emit_LiteralEdgeCases_CompileAndRoundTripRuntimeConstants() {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string edge = "quote:\" slash:\\ controls:\0\a\b\f\n\r\t\v next:\u0085 line:\u2028 paragraph:\u2029";
        string inputLiteral = GeneratedLiteral.Escape(edge);
        string commandSource = $$"""
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace EdgeCases;

            [Command]
            [Display(Name = "{{inputLiteral}}", Description = "{{inputLiteral}}")]// Literal edge-case input.
            public partial class LiteralCommand {
                [Display(Name = "{{inputLiteral}}", Description = "{{inputLiteral}}")]// Literal edge-case input.
                public string Value { get; set; } = string.Empty;
            }
            """;
        CommandModel command = CompilationHelper.ParseCommand(commandSource, "EdgeCases.LiteralCommand").Model!;

        string generated = McpManifestEmitter.Emit(ImmutableArray.Create(command), []);
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(generated);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(generated, cancellationToken: cancellationToken);

        compilation.GetDiagnostics(cancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("MCP manifest source with literal edge cases must compile.");
        tree.GetRoot(cancellationToken).DescendantTokens()
            .Where(token => token.IsKind(SyntaxKind.StringLiteralToken))
            .Select(token => token.ValueText)
            .ShouldContain(edge);
    }

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

        CommandModel orders = CompilationHelper.ParseCommand(OrdersSource, "Orders.ApproveCommand").Model!;
        CommandModel billing = CompilationHelper.ParseCommand(BillingSource, "Billing.ApproveCommand").Model!;

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

        DomainModel parsed = CompilationHelper.ParseProjection(Source, "Orders.OrderTimelineProjection").Model!;

        string source = McpManifestEmitter.Emit([], ImmutableArray.Create(parsed));

        source.ShouldContain("McpProjectionRenderStrategy.Timeline");
        source.ShouldNotContain("\"Timeline\",");
        // DN-2 / D23: SourceTools-emitted fingerprints declare a distinct algorithm id
        // because the build-time canonicalizer produces a key=value text blob (not JSON).
        source.ShouldContain("new SchemaFingerprint(\"frontcomposer.schema.sha256.v1.sourcetools-blob\"");
        source.ShouldContain("using Hexalith.FrontComposer.Contracts.Schema;");
    }
}
