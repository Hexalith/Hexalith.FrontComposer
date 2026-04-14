namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

using System.Threading;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

public class FluxorActionsEmitterTests
{
    [Fact]
    public Task Actions_Snapshot()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string result = FluxorActionsEmitter.EmitActions(model);
        return Verify(result);
    }

    [Fact]
    public Task Reducers_Snapshot()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string result = FluxorActionsEmitter.EmitReducers(model);
        return Verify(result);
    }

    [Fact]
    public void EmittedActions_ParseAsValidCSharp()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorActionsEmitter.EmitActions(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Actions code should parse without syntax errors");
    }

    [Fact]
    public void EmittedReducers_ParseAsValidCSharp()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorActionsEmitter.EmitReducers(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted Reducers code should parse without syntax errors");
    }

    [Fact]
    public void EmittedActions_UsePastTenseNaming()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorActionsEmitter.EmitActions(model);
        source.ShouldContain("OrderProjectionLoadRequestedAction");
        source.ShouldContain("OrderProjectionLoadedAction");
        source.ShouldContain("OrderProjectionLoadFailedAction");
    }

    [Fact]
    public void EmittedReducers_UseReducerMethodAttribute()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorActionsEmitter.EmitReducers(model);
        source.ShouldContain("[Fluxor.ReducerMethod]");
    }

    [Fact]
    public void EmittedReducers_CorrectStateTransitions()
    {
        FluxorModel model = new FluxorModel("OrderProjection", "TestDomain", "OrderProjectionState", "OrderProjectionFeature");
        string source = FluxorActionsEmitter.EmitReducers(model);
        source.ShouldContain("IsLoading = true, Error = null");
        source.ShouldContain("IsLoading = false, Items = action.Items, Error = null");
        source.ShouldContain("IsLoading = false, Error = action.Error");
    }
}
