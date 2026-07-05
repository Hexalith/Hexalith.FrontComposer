using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class CommandFormEmitterTests {
    private static CommandFluxorModel BuildFluxor(string typeName = "IncrementCommand", string @namespace = "Counter.Domain") => new(
            typeName,
            @namespace,
            typeName + "LifecycleState",
            typeName + "LifecycleFeature",
            typeName + "Actions",
            typeName + "Reducers",
            @namespace + "." + typeName,
            @namespace + "." + typeName + "LifecycleState");

    private static CommandFormModel BuildForm(
        IEnumerable<FormFieldModel> fields,
        string typeName = "IncrementCommand",
        string @namespace = "Counter.Domain",
        string? authorizationPolicyName = null) => new(
            typeName,
            @namespace,
            null,
            @namespace + "." + typeName,
            "Send " + typeName,
            new EquatableArray<FormFieldModel>(fields.ToImmutableArray()),
            authorizationPolicyName);

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
        source.ShouldContain("IncrementCommandActions.RejectedAction(correlationId, ex.Message, ex.Resolution, ex.ErrorCode, ex.ReasonCategory, ex.SuggestedAction, ex.DocsCode)");
    }

    [Fact]
    public void Emit_ForwardsTypedRejectionDetailsToLifecycleWrapper() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("builder.AddAttribute(seq++, \"RejectionDetails\", BuildFcLifecycleRejectionDetails());");
        source.ShouldContain("private CommandRejectionDetails? BuildFcLifecycleRejectionDetails()");
        source.ShouldContain("LifecycleState.Value.RejectionErrorCode");
        source.ShouldContain("LifecycleState.Value.RejectionReasonCategory");
        source.ShouldContain("LifecycleState.Value.RejectionSuggestedAction");
        source.ShouldContain("LifecycleState.Value.RejectionDocsCode");
    }

    [Fact]
    public void Emit_SubmitAllocatesCorrelationIdWithUlidFactory() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("[Inject] private IUlidFactory UlidFactory { get; set; } = default!;");
        source.ShouldContain("var correlationId = UlidFactory.NewUlid();");
        source.ShouldNotContain("var correlationId = Guid.NewGuid().ToString();");
    }

    [Fact]
    public void Emit_SubmitEnsuresLifecycleBridgeAndLastUsedBeforeSubmittedDispatch() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int bridgeEnsureIndex = source.IndexOf(
            "LifecycleBridgeRegistry.Ensure<IncrementCommandLifecycleBridge>();",
            StringComparison.Ordinal);
        int subscriberEnsureIndex = source.IndexOf(
            "LastUsedSubscriberRegistry.Ensure<IncrementCommandLastUsedSubscriber>();",
            StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf(
            "Dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(correlationId, _model));",
            StringComparison.Ordinal);

        bridgeEnsureIndex.ShouldBeGreaterThanOrEqualTo(0);
        subscriberEnsureIndex.ShouldBeGreaterThan(bridgeEnsureIndex);
        dispatchIndex.ShouldBeGreaterThan(subscriberEnsureIndex);
    }

    [Fact]
    public void Emit_PlaceholderFieldRendersFieldNameAndType() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Raw", "System.Object", FormFieldTypeCategory.Placeholder, "Raw", true, false, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("OpenComponent<global::Hexalith.FrontComposer.Shell.Components.Rendering.FcFieldPlaceholder>");
        source.ShouldContain("__b.AddAttribute(cseq++, \"FieldName\", \"Raw\");");
        source.ShouldContain("__b.AddAttribute(cseq++, \"TypeName\", \"System.Object\");");
        source.ShouldContain("FluentButton");
    }

    [Fact]
    public void Emit_PolicyProtectedCommand_ChecksAuthorizationBeforeBeforeSubmitAndDispatch() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            authorizationPolicyName: "OrderApprover");

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("ICommandAuthorizationEvaluator");
        source.ShouldContain("IStringLocalizer<global::Hexalith.FrontComposer.Shell.Resources.FcShellResources>");
        // Pass-2 P1: surface is now a closed-set enum, not a free-form string literal.
        source.ShouldContain("CommandAuthorizationSurface.GeneratedForm");
        source.ShouldContain("UnauthorizedCommandWarningTitle");
        source.ShouldContain("UnauthorizedCommandWarningMessage");
        source.ShouldContain("protected override async Task OnInitializedAsync()");
        source.ShouldContain("RefreshPresentationAuthorizationAsync");
        source.ShouldContain("|| !_authorizationPresentationReady");
        source.ShouldContain("|| !_authorizationPresentationAllowed");
        int authIndex = source.IndexOf("CommandAuthorizationEvaluator.EvaluateAsync", StringComparison.Ordinal);
        int beforeSubmitIndex = source.IndexOf("if (BeforeSubmit is not null)", StringComparison.Ordinal);
        int submittedIndex = source.IndexOf(".SubmittedAction", StringComparison.Ordinal);
        authIndex.ShouldBeGreaterThan(0);
        authIndex.ShouldBeLessThan(beforeSubmitIndex);
        authIndex.ShouldBeLessThan(submittedIndex);
        source.ShouldContain("CommandWarningKind.Forbidden");
    }

    [Fact]
    public void Emit_PolicyProtectedCommand_RechecksAuthorizationAfterBeforeSubmitBeforeDispatch() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            authorizationPolicyName: "OrderApprover");

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int firstAuthorizationIndex = source.IndexOf("var authorization = await CommandAuthorizationEvaluator.EvaluateAsync", StringComparison.Ordinal);
        int beforeSubmitIndex = source.IndexOf("await BeforeSubmit().ConfigureAwait(false);", StringComparison.Ordinal);
        int secondAuthorizationIndex = source.IndexOf("var authorizationPostBeforeSubmit = await CommandAuthorizationEvaluator.EvaluateAsync", StringComparison.Ordinal);
        int correlationIndex = source.IndexOf("var correlationId = UlidFactory.NewUlid();", StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf("CommandService.DispatchAsync", StringComparison.Ordinal);

        firstAuthorizationIndex.ShouldBeGreaterThan(0);
        beforeSubmitIndex.ShouldBeGreaterThan(firstAuthorizationIndex);
        secondAuthorizationIndex.ShouldBeGreaterThan(beforeSubmitIndex);
        correlationIndex.ShouldBeGreaterThan(secondAuthorizationIndex);
        dispatchIndex.ShouldBeGreaterThan(correlationIndex);
    }

    [Fact]
    public void Emit_RegistersPendingCommandOnlyAfterAcceptedDispatchResult() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("[Inject] private global::Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandStateService PendingCommandState { get; set; } = default!;");
        source.ShouldContain("if (string.Equals(result.Status, \"Accepted\", StringComparison.OrdinalIgnoreCase))");
        source.ShouldContain("PendingCommandState.Register(new global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistration(");
        source.ShouldContain("CorrelationId: correlationId,");
        source.ShouldContain("MessageId: result.MessageId,");
        source.ShouldContain("CommandTypeName: typeof(Counter.Domain.IncrementCommand).FullName ?? nameof(Counter.Domain.IncrementCommand),");

        int dispatchResultIndex = source.IndexOf("var result = await CommandService.DispatchAsync(", StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandState.Register(new global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistration(", StringComparison.Ordinal);
        int acknowledgedIndex = source.IndexOf("IncrementCommandActions.AcknowledgedAction(correlationId, result.MessageId)", StringComparison.Ordinal);

        registerIndex.ShouldBeGreaterThan(dispatchResultIndex);
        acknowledgedIndex.ShouldBeGreaterThan(registerIndex);
    }

    [Fact]
    public void Emit_PendingRegistrationDoesNotFabricateRuntimeRowIdentityMetadata() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("[CascadingParameter] private global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity? PendingCommandRowIdentity { get; set; }");
        source.ShouldContain("Row identity is registered only when a generated/runtime row");
        source.ShouldContain("ProjectionTypeName: PendingCommandRowIdentity?.ProjectionTypeName,");
        source.ShouldContain("LaneKey: PendingCommandRowIdentity?.LaneKey,");
        source.ShouldContain("EntityKey: PendingCommandRowIdentity?.EntityKey,");
        source.ShouldContain("ExpectedStatusSlot: PendingCommandRowIdentity?.ExpectedStatusSlot,");
        source.ShouldContain("PriorStatusSlot: PendingCommandRowIdentity?.PriorStatusSlot));");
        source.ShouldNotContain("ProjectionTypeName: typeof(");
        source.ShouldNotContain("EntityKey: _model");
    }

    [Fact]
    public void Emit_RetryableDispatchWarningResetsIdleWithoutPendingRegistrationOrAcknowledgementInCatch() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int warningCatchIndex = source.IndexOf("catch (CommandWarningException ex)", StringComparison.Ordinal);
        int resetIndex = source.IndexOf("ResetToIdleAction(correlationId)", warningCatchIndex, StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandState.Register", warningCatchIndex, StringComparison.Ordinal);
        int acknowledgedIndex = source.IndexOf("AcknowledgedAction", warningCatchIndex, StringComparison.Ordinal);

        source.ShouldContain("CommandWarningKind.RetryableDispatchFailed");
        warningCatchIndex.ShouldBeGreaterThan(0);
        resetIndex.ShouldBeGreaterThan(warningCatchIndex);
        registerIndex.ShouldBe(-1);
        acknowledgedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Emit_InjectsCommandExecutionAdmissionGate() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("[Inject] private global::Hexalith.FrontComposer.Shell.State.PendingCommands.ICommandExecutionAdmissionGate CommandExecutionAdmissionGate { get; set; } = default!;");
        source.ShouldContain("CommandExecutionAdmissionGate.TryAcquire(new global::Hexalith.FrontComposer.Shell.State.PendingCommands.CommandExecutionAdmissionRequest(");
        source.ShouldContain("SetCommandInProgressWarning(admission.DenialReason);");
        source.ShouldContain("CommandFeedbackPublisher.PublishWarning(_serverWarning);");
    }

    [Fact]
    public void Emit_CommandExecutionAdmissionRunsAfterBeforeSubmitBeforeSideEffects() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int beforeSubmitIndex = source.IndexOf("await BeforeSubmit().ConfigureAwait(false);", StringComparison.Ordinal);
        int admissionIndex = source.IndexOf("CommandExecutionAdmissionGate.TryAcquire", StringComparison.Ordinal);
        int correlationIndex = source.IndexOf("var correlationId = UlidFactory.NewUlid();", StringComparison.Ordinal);
        int submittedIndex = source.IndexOf("IncrementCommandActions.SubmittedAction(correlationId, _model)", StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf("CommandService.DispatchAsync", StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandState.Register", StringComparison.Ordinal);

        admissionIndex.ShouldBeGreaterThan(beforeSubmitIndex);
        correlationIndex.ShouldBeGreaterThan(admissionIndex);
        submittedIndex.ShouldBeGreaterThan(admissionIndex);
        dispatchIndex.ShouldBeGreaterThan(admissionIndex);
        registerIndex.ShouldBeGreaterThan(dispatchIndex);
    }

    [Fact]
    public void Emit_CommandExecutionAdmissionReleasesInFinally() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int tryIndex = source.IndexOf("try", source.IndexOf("Command submitted.", StringComparison.Ordinal), StringComparison.Ordinal);
        int finallyIndex = source.IndexOf("finally", tryIndex, StringComparison.Ordinal);
        int disposeIndex = source.IndexOf("admission.Dispose();", finallyIndex, StringComparison.Ordinal);

        tryIndex.ShouldBeGreaterThan(0);
        finallyIndex.ShouldBeGreaterThan(tryIndex);
        disposeIndex.ShouldBeGreaterThan(finallyIndex);
    }

    [Fact]
    public void Emit_SubmitEnsuresLastUsedSubscriberBeforeSubmittedDispatch() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int ensureIndex = source.IndexOf(
            "LastUsedSubscriberRegistry.Ensure<IncrementCommandLastUsedSubscriber>();",
            StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf(
            "Dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(correlationId, _model));",
            StringComparison.Ordinal);

        ensureIndex.ShouldBeGreaterThanOrEqualTo(0);
        dispatchIndex.ShouldBeGreaterThan(ensureIndex);
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

        source.ShouldContain("private string ResolveLabel(string propertyName, string staticLabel, bool hasExplicitDisplay)");
        source.ShouldContain("Localizer[propertyName]");
    }

    [Fact]
    public void Emit_DoesNotLogModelInstance() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Decision D15: never log _model. Passing the command to CommandService is allowed.
        source.Split('\n')
            .Where(line => line.Contains("Logger?", StringComparison.Ordinal))
            .ShouldAllBe(line => !line.Contains("_model", StringComparison.Ordinal));
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
    public void Emit_NullableNumericField_LiftsCultureToStringThroughNullConditional() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Quantity", "Int32", FormFieldTypeCategory.NumberInput, "Quantity", true, false, null),
            new FormFieldModel("DiscountAmount", "Decimal", FormFieldTypeCategory.DecimalInput, "Discount Amount", true, false, null),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Nullable<T> exposes no ToString(IFormatProvider) overload — the emitted Value binding
        // must lift through `?.` or the adopter's generated form fails to compile (CS1501).
        source.ShouldContain("_QuantityString ?? _model.Quantity?.ToString(CultureInfo.CurrentCulture)");
        source.ShouldContain("_DiscountAmountString ?? _model.DiscountAmount?.ToString(CultureInfo.CurrentCulture)");
    }

    [Fact]
    public void Emit_NonNullableNumericField_KeepsDirectCultureToString() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("_AmountString ?? _model.Amount.ToString(CultureInfo.CurrentCulture)");
        source.ShouldNotContain("_model.Amount?.ToString");
    }

    [Fact]
    public void Emit_EndToEnd_NullableNumericCommand_CompilesSuccessfully() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CommandParseResult parse = CompilationHelper.ParseCommand(CommandTestSources.NullableNumericCommand, "TestDomain.AdjustOrderCommand");

        _ = parse.Model.ShouldNotBeNull();
        CommandFluxorModel fluxor = CommandFluxorTransform.Transform(parse.Model);
        CommandFormModel form = CommandFormTransform.Transform(parse.Model);
        string source = CommandFormEmitter.Emit(form, fluxor);

        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();
        source.ShouldContain("?.ToString(CultureInfo.CurrentCulture)");
    }

    [Fact]
    public void Emit_SubmitBlocksWhenClientParseErrorsExist() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("if (HasClientParseErrors())");
        source.ShouldContain("_editContext?.NotifyValidationStateChanged();");
    }

    [Fact]
    public void Emit_OnConfirmedIsGuardedBySubmittedCorrelationId() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("private string? _submittedCorrelationId;");
        source.ShouldContain("string.Equals(currentCorrelationId, _submittedCorrelationId, StringComparison.Ordinal)");
        source.ShouldContain("_submittedCorrelationId = correlationId;");
    }

    [Fact]
    public void Emit_FormRootDoesNotHardcodeMaxWidth() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldNotContain("max-width: 720px");
    }

    [Fact]
    public void Emit_InvokesBeforeSubmitHookWhenProvided() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("[Parameter] public Func<Task>? BeforeSubmit { get; set; }");
        source.ShouldContain("if (BeforeSubmit is not null)");
        source.ShouldContain("await BeforeSubmit().ConfigureAwait(false);");
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

    [Fact]
    public Task CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("TenantId", "String", FormFieldTypeCategory.TextInput, "Tenant Id", false, true, null),
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        return Verify(source);
    }

    [Fact]
    public Task CommandForm_ShowFieldsOnly_RendersOnlyNamedFields() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        return Verify(source);
    }

    [Fact]
    public void Emit_FromParsedCommandWithDerivableFields_EmitsOnlyNonDerivableEditableInputs() {
        CommandParseResult parse = CompilationHelper.ParseCommand(CommandTestSources.WellKnownAndAttributedDerivableCommand, "TestDomain.KitchenSinkWithDerivedFromCommand");

        _ = parse.Model.ShouldNotBeNull();
        CommandFluxorModel fluxor = CommandFluxorTransform.Transform(parse.Model);
        CommandFormModel form = CommandFormTransform.Transform(parse.Model);
        string source = CommandFormEmitter.Emit(form, fluxor);

        form.Fields.Select(f => f.PropertyName).ShouldBe(new[] { "Payload" });
        source.ShouldContain("// Field: Payload");
        source.ShouldContain("ResolveLabel(\"Payload\"");
        source.ShouldNotContain("// Field: RequestIp");
        source.ShouldNotContain("ResolveLabel(\"RequestIp\"");
        source.ShouldNotContain("// Field: TenantId");
        source.ShouldNotContain("ResolveLabel(\"TenantId\"");
    }
}
