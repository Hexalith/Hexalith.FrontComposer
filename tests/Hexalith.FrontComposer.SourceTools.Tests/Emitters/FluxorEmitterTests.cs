using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class FluxorEmitterTests {
    [Fact]
    public Task FeatureAndState_Snapshot() {
        var model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string result = FluxorFeatureEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public void EmittedCode_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        var model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorFeatureEmitter.Emit(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Feature code should parse without syntax errors");
    }

    [Fact]
    public void EmittedCode_UsesFullyQualifiedFluxorTypes() {
        var model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorFeatureEmitter.Emit(model);
        source.ShouldContain("Fluxor.Feature<OrderProjectionState>");
    }
}
