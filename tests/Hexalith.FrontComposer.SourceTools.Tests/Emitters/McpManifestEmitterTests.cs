using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class McpManifestEmitterTests {
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
        source.ShouldContain("new SchemaFingerprint(\"frontcomposer.schema.sha256.canonical-json.v1\"");
        source.ShouldContain("using Hexalith.FrontComposer.Contracts.Schema;");
    }
}
