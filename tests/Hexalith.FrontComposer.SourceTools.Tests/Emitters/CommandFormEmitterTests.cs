using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public class CommandFormEmitterTests {
    private static readonly string[] ExpectedPayloadField = ["Payload"];

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
        string? authorizationPolicyName = null,
        CommandTargetModel? commandTarget = null) => new(
            typeName,
            @namespace,
            null,
            @namespace + "." + typeName,
            "Send " + typeName,
            new EquatableArray<FormFieldModel>(fields.ToImmutableArray()),
            authorizationPolicyName,
            commandTarget);

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
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.AddAttribute(#, \"EditContext\", _editContext);");
        masked.ShouldNotContain("__b.AddAttribute(#, \"Model\", (object)_model);");
    }

    [Fact]
    public void Emit_ButtonDisabledWhenNotIdle() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("!_interactiveReady");
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
    public void Emit_TerminalLifecycleCallbackUsesOnlyPendingOutcomeResolver() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("PendingCommandOutcomeResolver.BufferBeforeAccepted(correlationId, pendingOutcomeObservation)");
        source.ShouldContain("PendingCommandOutcomeResolver.Resolve(pendingOutcomeObservation)");
        source.ShouldContain("Materiality = observation.Materiality");
        source.ShouldContain("terminalApplied = System.Threading.Volatile.Read(ref acceptedTerminalAssociation) == 1");
        source.ShouldContain("dispatchTerminalAction = terminalApplied && LifecycleState.Value.State != observation.State;");
        source.ShouldContain("&& !terminalApplied) return;");
        source.ShouldContain("if (terminalApplied)");
        source.ShouldContain("System.Threading.Interlocked.Exchange(ref lifecycleCallbackClosed, 1);");
        source.ShouldContain("(!dispatchTerminalAction && System.Threading.Volatile.Read(ref lifecycleCallbackClosed) == 1)");
        source.ShouldNotContain("PendingCommandState.ResolveTerminal");

        int resolveIndex = source.IndexOf(
            "PendingCommandOutcomeResolver.Resolve(pendingOutcomeObservation)",
            StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf(
            "Dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(correlationId));",
            StringComparison.Ordinal);

        resolveIndex.ShouldBeGreaterThan(0);
        dispatchIndex.ShouldBeGreaterThan(resolveIndex);
    }

    [Fact]
    public void Emit_ForwardsTypedRejectionDetailsToLifecycleWrapper() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        GeneratedRenderTreeText.MaskSequenceArguments(source)
            .ShouldContain("builder.AddAttribute(#, \"RejectionDetails\", BuildFcLifecycleRejectionDetails());");
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
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.AddAttribute(#, \"FieldName\", \"Raw\");");
        masked.ShouldContain("__b.AddAttribute(#, \"TypeName\", \"System.Object\");");
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
        int dispatchIndex = source.IndexOf("CommandService.DispatchWithLifecycleObservationsAsync", StringComparison.Ordinal);

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

        source.ShouldContain("[Inject] private global::Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandOutcomeCoordinator PendingCommandOutcomeResolver { get; set; } = default!;");
        source.ShouldContain("bool accepted = string.Equals(result.Status, \"Accepted\", StringComparison.OrdinalIgnoreCase);");
        source.ShouldContain("PendingCommandOutcomeResolver.AssociateAccepted(new global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistration(");
        source.ShouldContain("CorrelationId: correlationId,");
        source.ShouldContain("MessageId: result.MessageId,");
        source.ShouldContain("CommandTypeName: typeof(Counter.Domain.IncrementCommand).FullName ?? nameof(Counter.Domain.IncrementCommand))");
        source.ShouldContain("ProjectionTypeName = commandTarget?.ProjectionTypeName,");
        source.ShouldContain("LaneKey = commandTarget?.ViewKey,");
        source.ShouldContain("EntityKey = commandTarget?.EntityKey,");
        source.ShouldContain("ExpectedStatusSlot = commandTarget?.ExpectedStatus,");
        source.ShouldContain("PriorStatusSlot = commandTarget?.PriorStatus,");

        int dispatchResultIndex = source.IndexOf("var result = await CommandService.DispatchWithLifecycleObservationsAsync(", StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandOutcomeResolver.AssociateAccepted(new global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistration(", StringComparison.Ordinal);
        int acknowledgedIndex = source.IndexOf("IncrementCommandActions.AcknowledgedAction(correlationId, result.MessageId)", StringComparison.Ordinal);

        registerIndex.ShouldBeGreaterThan(dispatchResultIndex);
        acknowledgedIndex.ShouldBeGreaterThan(registerIndex);
    }

    [Fact]
    public void Emit_UndeclaredCommandDoesNotConsumeAmbientRowIdentity() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldNotContain("PendingCommandRowIdentity");
        source.ShouldContain("CommandTargetSnapshot? commandTarget = null;");
        source.ShouldContain("TargetSnapshot = commandTarget");
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
        int registerIndex = source.IndexOf("PendingCommandOutcomeResolver.AssociateAccepted", warningCatchIndex, StringComparison.Ordinal);
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
        int cleanupTryIndex = source.IndexOf("try\n        {\n        var correlationId", StringComparison.Ordinal);
        int correlationIndex = source.IndexOf("var correlationId = UlidFactory.NewUlid();", StringComparison.Ordinal);
        int submittedIndex = source.IndexOf("IncrementCommandActions.SubmittedAction(correlationId, _model)", StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf("CommandService.DispatchWithLifecycleObservationsAsync", StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandOutcomeResolver.AssociateAccepted", StringComparison.Ordinal);

        admissionIndex.ShouldBeGreaterThan(beforeSubmitIndex);
        cleanupTryIndex.ShouldBeGreaterThan(admissionIndex);
        correlationIndex.ShouldBeGreaterThan(admissionIndex);
        submittedIndex.ShouldBeGreaterThan(admissionIndex);
        dispatchIndex.ShouldBeGreaterThan(admissionIndex);
        registerIndex.ShouldBeGreaterThan(dispatchIndex);
    }

    [Fact]
    public void Emit_SameAsSourceTargetCapturesBeforeDispatchWithoutFallback() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            commandTarget: new CommandTargetModel(
                "global::Counter.Domain.CounterProjection",
                CommandTargetResolutionMode.SameAsSource,
                CommandTargetChangeKind.Update,
                "counter-counts",
                null));

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int capture = source.IndexOf("var targetResolution = await ResolveCommandTargetAsync(_model, cts.Token)", StringComparison.Ordinal);
        int dispatch = source.IndexOf("var result = await CommandService.DispatchWithLifecycleObservationsAsync(", StringComparison.Ordinal);
        capture.ShouldBeGreaterThan(0);
        dispatch.ShouldBeGreaterThan(capture);
        source.ShouldContain("PendingCommandRowIdentity");
        source.ShouldContain("CommandTargetChangeKind.Update");
        source.ShouldNotContain("PendingCommandState.ResolveTerminal");
    }

    [Fact]
    public void Emit_CommandTargetTelemetryUsesClosedRedactedCompletionContract() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            commandTarget: new CommandTargetModel(
                "global::Counter.Domain.CounterProjection",
                CommandTargetResolutionMode.Provider,
                CommandTargetChangeKind.Create,
                "counter-counts",
                null));

        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        string statusMoveSource = CommandFormEmitter.Emit(
            BuildForm(
                [],
                commandTarget: new CommandTargetModel(
                    "global::Counter.Domain.CounterProjection",
                    CommandTargetResolutionMode.Provider,
                    CommandTargetChangeKind.StatusMove,
                    "counter-counts",
                    "active")),
            BuildFluxor());
        string sameSource = CommandFormEmitter.Emit(
            BuildForm(
                [],
                commandTarget: new CommandTargetModel(
                    "global::Counter.Domain.CounterProjection",
                    CommandTargetResolutionMode.SameAsSource,
                    CommandTargetChangeKind.Update,
                    "counter-counts",
                    null)),
            BuildFluxor());

        source.ShouldContain("[Inject] private ILogger<IncrementCommandForm>? Logger { get; set; }");
        source.ShouldContain("new global::Microsoft.Extensions.Logging.EventId(5912, \"CommandFormTargetResolutionFailed\")");
        source.ShouldContain("global::Microsoft.Extensions.Logging.LogLevel.Warning");
        source.ShouldContain("\"Command target resolution failed closed. Category={Category}\"");
        source.ShouldContain("new global::Microsoft.Extensions.Logging.EventId(5913, \"CommandFormTargetResolutionSucceeded\")");
        source.ShouldContain("global::Microsoft.Extensions.Logging.LogLevel.Information");
        source.ShouldContain("\"Command target resolution succeeded.\"");
        source.ShouldContain("LogCommandTargetResolutionFailed(Logger, category)");
        source.ShouldContain("LogCommandTargetResolutionSucceeded(Logger)");
        source.ShouldContain("catch (Exception ex) when (!IsFatalCommandTargetResolutionException(ex)) { }");
        source.ShouldContain("aggregate.Flatten().InnerExceptions, IsFatalCommandTargetResolutionException");
        source.ShouldNotContain("Command target resolution succeeded. {", Case.Sensitive);
        source.ShouldNotContain("Command target resolution failed closed. Category={Category} {", Case.Sensitive);

        string[] actualCategories = Regex.Matches(
                source + statusMoveSource + sameSource,
                "\\\"(?<category>(?:projection-view|provider|same-source|status|target|view)-[a-z-]+)\\\"",
                RegexOptions.CultureInvariant)
            .Select(static match => match.Groups["category"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expectedCategories = [
            "projection-view-mismatch",
            "provider-busy",
            "provider-duplicate",
            "provider-failed",
            "provider-invalid",
            "provider-missing",
            "provider-timeout",
            "same-source-unavailable",
            "status-mismatch",
            "status-move-incomplete",
            "target-failed",
            "view-mismatch",
        ];
        actualCategories.ShouldBe(expectedCategories, ignoreOrder: false);

        int resolved = source.IndexOf("var resolution = await ResolveCommandTargetCoreAsync", StringComparison.Ordinal);
        int success = source.IndexOf("TryLogCommandTargetResolutionSucceeded();", StringComparison.Ordinal);
        int returned = source.IndexOf("return resolution;", success, StringComparison.Ordinal);
        resolved.ShouldBeGreaterThan(0);
        success.ShouldBeGreaterThan(resolved);
        returned.ShouldBeGreaterThan(success);
    }

    [Fact]
    public void Emit_FixedExpectedStatusRequiresExactNonNullSourceAndProviderValues() {
        CommandTargetModel sameSourceTarget = new(
            "global::Counter.Domain.CounterProjection",
            CommandTargetResolutionMode.SameAsSource,
            CommandTargetChangeKind.Update,
            "counter-counts",
            "Approved");
        CommandTargetModel providerTarget = new(
            "global::Counter.Domain.CounterProjection",
            CommandTargetResolutionMode.Provider,
            CommandTargetChangeKind.Update,
            "counter-counts",
            "Approved");

        string sameSource = CommandFormEmitter.Emit(
            BuildForm([], commandTarget: sameSourceTarget),
            BuildFluxor());
        string provider = CommandFormEmitter.Emit(
            BuildForm([], commandTarget: providerTarget),
            BuildFluxor());

        sameSource.ShouldContain("if (!string.Equals(sourceExpectedStatus, \"Approved\", StringComparison.Ordinal))");
        sameSource.ShouldNotContain("sourceExpectedStatus is not null &&");
        provider.ShouldContain("if (!string.Equals(providerExpectedStatus, \"Approved\", StringComparison.Ordinal))");
        provider.ShouldNotContain("providerExpectedStatus is not null &&");
    }

    [Fact]
    public void Emit_UnacceptedCleanupClearsLocalsFinallyAndContainsOnlyNonFatalCoordinatorFailures() {
        string source = CommandFormEmitter.Emit(BuildForm([]), BuildFluxor());

        int discard = source.IndexOf("PendingCommandOutcomeResolver.DiscardBufferedByOwner(ownerId);", StringComparison.Ordinal);
        int filter = source.IndexOf("catch (Exception ex) when (!IsFatalCommandCleanupException(ex))", discard, StringComparison.Ordinal);
        int finallyIndex = source.IndexOf("finally", filter, StringComparison.Ordinal);
        int clearIds = source.IndexOf("messageIds.Clear();", finallyIndex, StringComparison.Ordinal);
        int clearOrder = source.IndexOf("messageIdOrder.Clear();", finallyIndex, StringComparison.Ordinal);

        discard.ShouldBeGreaterThan(0);
        filter.ShouldBeGreaterThan(discard);
        finallyIndex.ShouldBeGreaterThan(filter);
        clearIds.ShouldBeGreaterThan(finallyIndex);
        clearOrder.ShouldBeGreaterThan(clearIds);
        source.ShouldContain("exception is global::System.OutOfMemoryException");
        source.ShouldContain("exception is global::System.AggregateException aggregate");
        source.ShouldContain("global::System.Linq.Enumerable.Any(aggregate.Flatten().InnerExceptions, IsFatalCommandCleanupException)");
    }

    [Fact]
    public void Emit_AcceptedAssociationFailureKeepsSyncingWithoutResetToIdle() {
        string source = CommandFormEmitter.Emit(BuildForm([]), BuildFluxor());

        source.ShouldContain("catch (Exception ex) when (!IsFatalCommandCleanupException(ex))");
        int associationFailed = source.IndexOf("if (!acceptedAssociationSucceeded)", StringComparison.Ordinal);
        int mergedTerminal = source.IndexOf("MergedTerminal", associationFailed, StringComparison.Ordinal);
        associationFailed.ShouldBeGreaterThan(0);
        mergedTerminal.ShouldBeGreaterThan(associationFailed);
        string associationBlock = source[associationFailed..mergedTerminal];
        associationBlock.ShouldContain("Transport accepted; keep Syncing/pending so polling and convergence continue.");
        associationBlock.ShouldContain("LogCommandAcknowledgedDispatchSkipped");
        associationBlock.ShouldNotContain("ResetToIdleAction(correlationId)");
    }

    [Fact]
    public void Emit_ProviderTargetClonesAndInvokesOffThreadWithHardDeadlineAndCallerCancellation() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            commandTarget: new CommandTargetModel(
                "global::Counter.Domain.CounterProjection",
                CommandTargetResolutionMode.Provider,
                CommandTargetChangeKind.Create,
                "counter-counts",
                null));

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("var resolution = await ResolveCommandTargetCoreAsync(command, cancellationToken).ConfigureAwait(false);");
        source.ShouldContain("catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        source.ShouldContain("throw;");
        source.ShouldContain("catch (Exception ex) when (!IsFatalCommandTargetResolutionException(ex))");
        source.ShouldContain("return (command, FailCommandTargetResolution(\"target-failed\"))");
        source.ShouldContain("[Inject] private global::System.IServiceProvider CommandTargetServiceProvider");
        source.ShouldNotContain("CommandTargetIdentityProviders { get; set; }");
        source.ShouldContain("CommandTargetServiceProvider.GetService(typeof(");
        source.ShouldContain("var transportCommand = CloneCommandForTargetProvider(command);");
        source.ShouldContain("var providerCommand = CloneCommandForTargetProvider(transportCommand);");
        source.ShouldContain("ConditionalWeakTable<global::Hexalith.FrontComposer.Shell.State.PendingCommands.ICommandExecutionAdmissionGate, CommandTargetProviderWorkerState>");
        source.ShouldContain("_commandTargetProviderWorkers.GetValue(CommandExecutionAdmissionGate");
        source.ShouldContain("Interlocked.CompareExchange(ref providerWorker.Active, 1, 0)");
        source.ShouldContain("return new Counter.Domain.IncrementCommand");
        source.ShouldContain("Amount = command.Amount,");
        source.ShouldNotContain("JsonSerializer");
        source.ShouldNotContain("System.Reflection");
        source.ShouldContain("var deadlineToken = deadline.Token;");
        source.ShouldContain("resolution = Task.Run(");
        source.ShouldContain("providers[0].ResolveAsync(providerCommand, deadlineToken)");
        source.ShouldNotContain("providers[0].ResolveAsync(providerCommand, deadline.Token)");
        source.ShouldContain("CancellationToken.None);");
        source.ShouldContain("_ = resolution.ContinueWith(");
        source.ShouldContain("_ = task.Exception;");
        source.ShouldContain("Interlocked.Exchange(ref providerWorker.Active, 0)");
        source.ShouldNotContain("_commandTargetProviderWorkerActive");
        source.ShouldContain("TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously");
        source.ShouldContain("var providerResult = await resolution.WaitAsync(");
        source.ShouldContain("identity = providerResult.Identity;");
        source.ShouldContain("TimeSpan.FromMilliseconds(timeoutMs)");
        source.ShouldContain("cancellationToken).ConfigureAwait(false);");
        source.ShouldContain("try { deadline.Cancel(); } catch (ObjectDisposedException) { }");
        source.ShouldContain("if (resolution is null)");
        source.ShouldContain("return (frozenCommand ?? command, FailCommandTargetResolution(\"target-failed\"));");

        int workerIndex = source.IndexOf("resolution = Task.Run(", StringComparison.Ordinal);
        int deadlineTokenIndex = source.IndexOf("var deadlineToken = deadline.Token;", StringComparison.Ordinal);
        int providerResolutionIndex = source.IndexOf("CommandTargetServiceProvider.GetService(typeof(", StringComparison.Ordinal);
        int cloneIndex = source.IndexOf("var transportCommand = CloneCommandForTargetProvider(command);", StringComparison.Ordinal);
        deadlineTokenIndex.ShouldBeLessThan(workerIndex);
        providerResolutionIndex.ShouldBeGreaterThan(workerIndex);
        cloneIndex.ShouldBeGreaterThan(workerIndex);
        source.ShouldContain("var commandForDispatch = targetResolution.Command;");
        source.ShouldContain("var commandTarget = targetResolution.Target;");
        source.ShouldContain("CommandService.DispatchWithLifecycleObservationsAsync(\n                commandForDispatch,");
        source.ShouldContain("PendingCommandOutcomeResolver.DiscardBufferedByOwner(ownerId);");
        source.ShouldNotContain("PendingCommandOutcomeResolver.DiscardBuffered(oldest);");
    }

    [Fact]
    public void Parse_ProviderTargetWithReadOnlyDerivedPropertyRejectsCommandBeforeEmission() {
        const string commandSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Counter.Domain;
            [Projection]
            public sealed class CounterProjection { }
            [Command]
            [CommandTarget(typeof(CounterProjection), CommandTargetResolutionMode.Provider, CommandTargetChangeKind.Create)]
            public sealed class CreateCounterCommand
            {
                [DerivedFrom(DerivedFromSource.Context)]
                public string TenantId { get; } = string.Empty;
                public string Name { get; set; } = string.Empty;
            }
            """;
        CommandParseResult result = CompilationHelper.ParseCommand(
            commandSource,
            "Counter.Domain.CreateCounterCommand");

        result.Model.ShouldBeNull();
        result.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1016")
            .Message.ShouldContain("public non-init setter");
    }

    [Fact]
    public void Parse_ProviderTargetWithInitOnlyDerivedPropertyRejectsCommandBeforeEmission() {
        const string commandSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Counter.Domain;
            [Projection]
            [BoundedContext("Counter")]
            public sealed class CounterProjection { }
            [Command]
            [CommandTarget(typeof(CounterProjection), CommandTargetResolutionMode.Provider, CommandTargetChangeKind.Create)]
            public sealed class CreateCounterCommand
            {
                [DerivedFrom(DerivedFromSource.MessageId)]
                public string MessageId { get; init; } = string.Empty;
                public string Name { get; set; } = string.Empty;
            }
            """;
        CommandParseResult result = CompilationHelper.ParseCommand(
            commandSource,
            "Counter.Domain.CreateCounterCommand");

        result.Model.ShouldBeNull();
        result.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1016")
            .Message.ShouldContain("public non-init setter");
    }

    [Fact]
    public void Emit_CommandExecutionAdmissionReleasesInFinally() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Story 11.21: anchor on the submitted-log CALL SITE, not on the message template. The
        // template now lives in the cached LoggerMessage delegate emitted at the end of the class.
        int submittedLogIndex = source.IndexOf("LogCommandSubmitted(Logger, correlationId);", StringComparison.Ordinal);
        submittedLogIndex.ShouldBeGreaterThan(0);

        int tryIndex = source.IndexOf("try", submittedLogIndex, StringComparison.Ordinal);
        tryIndex.ShouldBeGreaterThan(0);

        int finallyIndex = source.IndexOf("finally", tryIndex, StringComparison.Ordinal);
        finallyIndex.ShouldBeGreaterThan(tryIndex);

        int disposeIndex = source.IndexOf("admission.Dispose();", finallyIndex, StringComparison.Ordinal);
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
    public void Emit_DisposeCleansLifetimeAndHandlersInFinallyAfterResetDispatch() {
        string source = CommandFormEmitter.Emit(BuildForm([]), BuildFluxor());
        int dispose = source.IndexOf("public void Dispose()", StringComparison.Ordinal);
        int reset = source.IndexOf("ResetToIdleAction(_submittedCorrelationId)", dispose, StringComparison.Ordinal);
        int finallyIndex = source.IndexOf("finally", reset, StringComparison.Ordinal);
        int ctsDispose = source.IndexOf("_cts?.Dispose();", finallyIndex, StringComparison.Ordinal);
        int unsubscribe = source.IndexOf("LifecycleState.StateChanged -= OnStateChanged;", finallyIndex, StringComparison.Ordinal);

        reset.ShouldBeGreaterThan(dispose);
        finallyIndex.ShouldBeGreaterThan(reset);
        ctsDispose.ShouldBeGreaterThan(finallyIndex);
        unsubscribe.ShouldBeGreaterThan(ctsDispose);
    }

    [Fact]
    public void Emit_DisposalPreservesAcceptedResolverOwnedLifecycle() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("private int _acceptedAssociationSucceeded;");
        source.ShouldContain("System.Threading.Interlocked.Exchange(ref _acceptedAssociationSucceeded, 1);");
        source.ShouldContain("if (System.Threading.Volatile.Read(ref _acceptedAssociationSucceeded) == 0");
    }

    [Fact]
    public void Emit_IncludesResolveLabelHelper() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Story 11.21 CA1507 — the parameter names the command model property, not a member of the
        // generated form, so it must not be called `propertyName`.
        source.ShouldContain("private string ResolveLabel(string commandPropertyName, string staticLabel, bool hasExplicitDisplay)");
        source.ShouldContain("Localizer[commandPropertyName]");
    }

    [Fact]
    public void Emit_DoesNotLogModelInstance() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Decision D15: never log _model. Passing the command to CommandService is allowed.
        // Story 11.21: logging goes through cached LoggerMessage delegates, so the guard matches the
        // "(Logger, ...)" call sites instead of the old null-conditional "Logger?" shape. The message
        // templates are emitter-authored constants, so the command instance can only leak through an
        // argument at one of these call sites.
        string[] loggingLines = [.. source.Split('\n')
            .Where(line => line.Contains("(Logger,", StringComparison.Ordinal))];

        loggingLines.ShouldNotBeEmpty();
        loggingLines.ShouldAllBe(line => !line.Contains("_model", StringComparison.Ordinal));
        source.ShouldNotContain("{Model}");
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
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.OpenElement(#, \"input\")");
        source.ShouldContain("EventCallback.Factory.Create<ChangeEventArgs>(this, e => OnAmountChanged(e.Value?.ToString()))");
        source.ShouldContain("NotifyClientFieldChanged(\"Amount\")");
        masked.ShouldNotContain("__b.AddAttribute(#, \"required\"");
        source.ShouldContain("int.TryParse(value,");
    }

    [Fact]
    public void Emit_TextFieldEmitsRawInputHandler() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.OpenElement(#, \"input\")");
        source.ShouldContain("EventCallback.Factory.Create<ChangeEventArgs>(this, e => { _model.Note = e.Value?.ToString(); NotifyClientFieldChanged(\"Note\"); })");
        source.ShouldContain("NotifyClientFieldChanged(\"Note\")");
        masked.ShouldNotContain("__b.AddAttribute(#, \"required\"");
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
        source.ShouldContain("IsDirty = false;");
        source.ShouldContain("_editContext?.MarkAsUnmodified();");
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

        form.Fields.Select(f => f.PropertyName).ShouldBe(ExpectedPayloadField);
        source.ShouldContain("// Field: Payload");
        source.ShouldContain("ResolveLabel(\"Payload\"");
        source.ShouldNotContain("// Field: RequestIp");
        source.ShouldNotContain("ResolveLabel(\"RequestIp\"");
        source.ShouldNotContain("// Field: TenantId");
        source.ShouldNotContain("ResolveLabel(\"TenantId\"");
    }

    [Fact]
    public void Emit_UsesLiteralRenderTreeSequencesInsteadOfRuntimeCounters() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
        ]);

        RenderTreeSequenceRewriterTests.ShouldUseLiteralRenderTreeSequences(
            CommandFormEmitter.Emit(form, BuildFluxor()));
    }

    [Fact]
    public void Emit_IdempotentDisposeSuppressesFinalization() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Story 11.21 CA1816 — the guard still short-circuits a repeat call, and the suppression is
        // the last statement of the first (and only effective) pass.
        source.ShouldContain("if (_disposed) return;");
        source.ShouldContain("System.GC.SuppressFinalize(this);");
        source.IndexOf("System.GC.SuppressFinalize(this);", StringComparison.Ordinal)
            .ShouldBeGreaterThan(source.IndexOf("if (_disposed) return;", StringComparison.Ordinal));
    }

    [Fact]
    public void Emit_ClientParseErrorHelperIsStaticOnlyWhenNoFieldCanFailToParse() {
        string withoutNumericFields = CommandFormEmitter.Emit(
            BuildForm([new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null)]),
            BuildFluxor());
        string withNumericField = CommandFormEmitter.Emit(
            BuildForm([new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)]),
            BuildFluxor());

        withoutNumericFields.ShouldContain("private static bool HasClientParseErrors()");
        withNumericField.ShouldContain("private bool HasClientParseErrors()");
        withNumericField.ShouldNotContain("private static bool HasClientParseErrors()");
    }

}
