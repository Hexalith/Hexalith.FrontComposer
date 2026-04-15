using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class CommandFormEmitterTests {
    private static CommandFluxorModel BuildFluxor(string typeName = "IncrementCommand", string @namespace = "Counter.Domain") {
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

    private static CommandFormModel BuildForm(IEnumerable<FormFieldModel> fields, string typeName = "IncrementCommand", string @namespace = "Counter.Domain") {
        return new CommandFormModel(
            typeName,
            @namespace,
            null,
            @namespace + "." + typeName,
            "Send " + typeName,
            new EquatableArray<FormFieldModel>(fields.ToImmutableArray()));
    }

    [Fact]
    public void Emit_ProducesValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
    }

    [Fact]
    public void Emit_ProducesDeterministicOutputForSameInput() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        CommandFluxorModel fluxor = BuildFluxor();

        string first = CommandFormEmitter.Emit(form, fluxor);
        string second = CommandFormEmitter.Emit(form, fluxor);

        first.ShouldBe(second);
    }

    [Fact]
    public void Emit_RendersAllFieldCategoriesWithoutErrors() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        FormFieldModel[] fields = [
            new("StringField", "String", FormFieldTypeCategory.TextInput, "String Field", false, true, null),
            new("IntField", "Int32", FormFieldTypeCategory.NumberInput, "Int Field", false, true, null),
            new("DecimalField", "Decimal", FormFieldTypeCategory.DecimalInput, "Decimal Field", false, true, null),
            new("BoolField", "Boolean", FormFieldTypeCategory.Switch, "Bool Field", false, false, null),
            new("DateField", "DateTime", FormFieldTypeCategory.DatePicker, "Date Field", false, true, null),
            new("IdField", "Guid", FormFieldTypeCategory.MonospaceText, "Id Field", false, true, null),
            new("UnknownField", "System.Object", FormFieldTypeCategory.Placeholder, "Unknown Field", true, false, null),
        ];

        string source = CommandFormEmitter.Emit(BuildForm(fields), BuildFluxor());

        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
    }

    [Fact]
    public void Emit_IncludesEditFormAndDataAnnotationsValidator() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Name", "String", FormFieldTypeCategory.TextInput, "Name", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("OpenComponent<EditForm>");
        source.ShouldContain("OpenComponent<DataAnnotationsValidator>");
        source.ShouldContain("OpenComponent<FluentValidationSummary>");
    }

    [Fact]
    public void Emit_ButtonDisabledWhenNotIdle() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("LifecycleState.Value.State != CommandLifecycleState.Idle");
    }

    [Fact]
    public void Emit_SubmitDispatchesSubmittedThenAcknowledged() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("IncrementCommandActions.SubmittedAction(correlationId, _model)");
        source.ShouldContain("IncrementCommandActions.AcknowledgedAction(correlationId, result.MessageId)");
        source.ShouldContain("IncrementCommandActions.SyncingAction(correlationId)");
        source.ShouldContain("IncrementCommandActions.ConfirmedAction(correlationId)");
        source.ShouldContain("IncrementCommandActions.RejectedAction(correlationId, ex.Message, ex.Resolution)");
    }

    [Fact]
    public void Emit_IncludesCancellationTokenSourceDisposal() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("_cts?.Cancel();");
        source.ShouldContain("_cts?.Dispose();");
    }

    [Fact]
    public void Emit_IncludesResolveLabelHelper() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("private string ResolveLabel(string propertyName, string staticLabel)");
        source.ShouldContain("Localizer[propertyName]");
    }

    [Fact]
    public void Emit_DoesNotLogModelInstance() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Decision D15: never log _model
        source.ShouldNotContain("_model,\n");
        source.ShouldNotContain("LogInformation(\"Submitted command {Model}\"");
    }

    [Fact]
    public void Emit_NumericFieldEmitsBackingStateAndHandler() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("_AmountString");
        source.ShouldContain("_AmountParseError");
        source.ShouldContain("OnAmountChanged(string? value)");
        source.ShouldContain("int.TryParse(value,");
    }

    [Fact]
    public void Emit_EndToEnd_FromParsedCommand_CompilesSuccessfully() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CommandParseResult parse = CompilationHelper.ParseCommand(CommandTestSources.MultiFieldCommand, "TestDomain.PlaceOrderCommand");

        _ = parse.Model.ShouldNotBeNull();
        CommandFluxorModel fluxor = CommandFluxorTransform.Transform(parse.Model);
        CommandFormModel form = CommandFormTransform.Transform(parse.Model);
        string source = CommandFormEmitter.Emit(form, fluxor);

        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
    }
}
