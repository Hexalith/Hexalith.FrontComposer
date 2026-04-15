using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class CommandFluxorEmitterTests {
    private static CommandFluxorModel BuildModel(string typeName = "IncrementCommand", string @namespace = "Counter.Domain") {
        return new CommandFluxorModel(
            typeName,
            @namespace,
            typeName + "LifecycleState",
            typeName + "LifecycleFeature",
            typeName + "Actions",
            typeName + "Reducers",
            @namespace + "." + typeName,
            @namespace + "." + typeName + "LifecycleState");
    }

    [Fact]
    public void Actions_Emit_ContainsAllFiveLifecycleRecords() {
        string source = CommandFluxorActionsEmitter.Emit(BuildModel());

        source.ShouldContain("sealed record SubmittedAction");
        source.ShouldContain("sealed record AcknowledgedAction");
        source.ShouldContain("sealed record SyncingAction");
        source.ShouldContain("sealed record ConfirmedAction");
        source.ShouldContain("sealed record RejectedAction");
    }

    [Fact]
    public void Actions_Emit_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = CommandFluxorActionsEmitter.Emit(BuildModel());
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
    }

    [Fact]
    public void Feature_Emit_ContainsStateRecordFeatureAndReducers() {
        string source = CommandFluxorFeatureEmitter.Emit(BuildModel());

        source.ShouldContain("sealed record IncrementCommandLifecycleState");
        source.ShouldContain("class IncrementCommandLifecycleFeature : Fluxor.Feature<IncrementCommandLifecycleState>");
        source.ShouldContain("static class IncrementCommandReducers");
        source.ShouldContain("[Fluxor.ReducerMethod]");
    }

    [Fact]
    public void Feature_Emit_UsesFullyQualifiedStateName() {
        string source = CommandFluxorFeatureEmitter.Emit(BuildModel());

        source.ShouldContain("GetName() => \"Counter.Domain.IncrementCommandLifecycleState\"");
    }

    [Fact]
    public void Feature_Emit_ReducersCoverAllFiveStates() {
        string source = CommandFluxorFeatureEmitter.Emit(BuildModel());

        source.ShouldContain("CommandLifecycleState.Submitting");
        source.ShouldContain("CommandLifecycleState.Acknowledged");
        source.ShouldContain("CommandLifecycleState.Syncing");
        source.ShouldContain("CommandLifecycleState.Confirmed");
        source.ShouldContain("CommandLifecycleState.Rejected");
    }

    [Fact]
    public void Feature_Emit_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = CommandFluxorFeatureEmitter.Emit(BuildModel());
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
    }

    [Fact]
    public void Feature_Emit_DifferentNamespaces_ProduceDistinctGetNameValues() {
        CommandFluxorModel fromNsA = BuildModel("Same", "Domain.A");
        CommandFluxorModel fromNsB = BuildModel("Same", "Domain.B");

        string a = CommandFluxorFeatureEmitter.Emit(fromNsA);
        string b = CommandFluxorFeatureEmitter.Emit(fromNsB);

        a.ShouldContain("\"Domain.A.SameLifecycleState\"");
        b.ShouldContain("\"Domain.B.SameLifecycleState\"");
    }

    [Fact]
    public void CommandFluxorModel_IEquatable_SameValues_Equal() {
        CommandFluxorModel a = BuildModel();
        CommandFluxorModel b = BuildModel();

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void CommandFluxorModel_IEquatable_DifferentTypeName_NotEqual() {
        CommandFluxorModel a = BuildModel("A");
        CommandFluxorModel b = BuildModel("B");

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void CommandFluxorTransform_ProducesExpectedNames() {
        CommandModel command = new(
            "FooCommand",
            "Foo.Bar",
            null,
            null,
            null,
            new EquatableArray<PropertyModel>(System.Collections.Immutable.ImmutableArray<PropertyModel>.Empty),
            new EquatableArray<PropertyModel>(System.Collections.Immutable.ImmutableArray<PropertyModel>.Empty),
            new EquatableArray<PropertyModel>(System.Collections.Immutable.ImmutableArray<PropertyModel>.Empty));

        CommandFluxorModel model = CommandFluxorTransform.Transform(command);

        model.StateName.ShouldBe("FooCommandLifecycleState");
        model.FeatureName.ShouldBe("FooCommandLifecycleFeature");
        model.ActionsWrapperName.ShouldBe("FooCommandActions");
        model.ReducersClassName.ShouldBe("FooCommandReducers");
        model.FeatureQualifiedName.ShouldBe("Foo.Bar.FooCommandLifecycleState");
        model.CommandFullyQualifiedName.ShouldBe("Foo.Bar.FooCommand");
    }

    [Fact]
    public void Actions_Emit_ContainsCorrelationIdOnEveryRecord() {
        string source = CommandFluxorActionsEmitter.Emit(BuildModel());

        source.ShouldContain("SubmittedAction(string CorrelationId,");
        source.ShouldContain("AcknowledgedAction(string CorrelationId,");
        source.ShouldContain("SyncingAction(string CorrelationId)");
        source.ShouldContain("ConfirmedAction(string CorrelationId)");
        source.ShouldContain("RejectedAction(string CorrelationId,");
    }

    [Fact]
    public void Feature_Emit_InitialStateIsIdle() {
        string source = CommandFluxorFeatureEmitter.Emit(BuildModel());

        source.ShouldContain("CommandLifecycleState.Idle");
    }
}
