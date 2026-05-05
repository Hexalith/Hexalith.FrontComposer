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
        // DN-2 / D23: SourceTools-emitted fingerprints declare a distinct algorithm id
        // because the build-time canonicalizer produces a key=value text blob (not JSON).
        source.ShouldContain("new SchemaFingerprint(\"frontcomposer.schema.sha256.v1.sourcetools-blob\"");
        source.ShouldContain("using Hexalith.FrontComposer.Contracts.Schema;");
    }
}
