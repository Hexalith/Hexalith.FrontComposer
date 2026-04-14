using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class RegistrationEmitterTests {
    [Fact]
    public Task SingleProjection_Snapshot() {
        var model = new RegistrationModel("Orders", "OrderProjection", "TestDomain", null);
        string result = RegistrationEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public Task BoundedContextDisplayLabel_Snapshot() {
        var model = new RegistrationModel("Orders", "OrderProjection", "TestDomain", "Commandes");
        string result = RegistrationEmitter.Emit(model);
        return Verify(result);
    }

    [Fact]
    public void EmittedCode_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        var model = new RegistrationModel("Orders", "OrderProjection", "TestDomain", null);
        string source = RegistrationEmitter.Emit(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Registration code should parse without syntax errors");
    }
}
