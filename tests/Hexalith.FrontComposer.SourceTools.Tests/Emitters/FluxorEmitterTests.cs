namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

using System.Threading;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

public class FluxorEmitterTests
{
    [Fact]
    public Task FeatureAndState_Snapshot()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string result = FluxorFeatureEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public void EmittedCode_ParsesAsValidCSharp()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorFeatureEmitter.Emit(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Feature code should parse without syntax errors");
    }

    [Fact]
    public void EmittedCode_UsesFullyQualifiedFluxorTypes()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorFeatureEmitter.Emit(model);
        source.ShouldContain("Fluxor.Feature<OrderProjectionState>");
    }
}
