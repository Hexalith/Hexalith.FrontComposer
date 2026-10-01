using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        source.ShouldContain("[\"novalidate\"] = \"novalidate\"");
        source.ShouldContain("OpenComponent<DataAnnotationsValidator>");
        source.ShouldContain("OpenComponent<FcValidationSummary>");
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.AddAttribute(#, \"EditContext\", _editContext);");
        masked.ShouldNotContain("__b.AddAttribute(#, \"Model\", (object)_model);");
    }

    [Fact]
    public void Emit_ActiveCommandAllowsAttemptSoConcurrencyOutcomeCanBeAnnounced() {
        CommandFormModel form = BuildForm(System.Array.Empty<FormFieldModel>());
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        source.ShouldContain("!_interactiveReady");
        source.ShouldContain("CreateCommandBlockedWarning(reason)");
        source.ShouldContain("await _blockedOutcome.PresentAsync().ConfigureAwait(false);");
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.OpenComponent<FcCommandBlockedOutcome>(#);");
        masked.ShouldContain("__b.AddAttribute(#, \"AttemptedControlId\", _formDomId + \"-submit\");");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("OrderApprover")]
    public void EmitSubmitButtonIsNeverLifecycleDisabled(string? authorizationPolicyName) {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            authorizationPolicyName: authorizationPolicyName);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // VG5-03 — the submit control's Disabled expression never reads the lifecycle state, for policy
        // and non-policy forms alike: a press during an in-flight command must reach SubmitAsync (AM-20).
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        int submit = masked.IndexOf("__b.AddAttribute(#, \"Id\", _formDomId + \"-submit\");", StringComparison.Ordinal);
        submit.ShouldBeGreaterThanOrEqualTo(0);
        int disabled = masked.IndexOf("\"Disabled\",", submit, StringComparison.Ordinal);
        int childContent = masked.IndexOf("\"ChildContent\"", disabled, StringComparison.Ordinal);
        disabled.ShouldBeGreaterThan(submit);
        string expression = masked[disabled..childContent];
        expression.ShouldContain("!_interactiveReady");
        expression.ShouldNotContain("LifecycleState");
        expression.ShouldNotContain("CommandLifecycleState");
        if (authorizationPolicyName is null) {
            expression.ShouldNotContain("_authorizationPresentation");
        }
        else {
            expression.ShouldContain("|| !_authorizationPresentationReady");
            expression.ShouldContain("|| !_authorizationPresentationAllowed");
        }
    }

    [Fact]
    public void EmitMappedRejectionUsesItsOwnStoreClearedWhenTheNextAttemptIsAdmitted() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Name", "String", FormFieldTypeCategory.TextInput, "Name", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // VG5-O1 / E5-06 — a mapped rejection writes a dedicated store that is cleared after admission and
        // before validation, so an unchanged retry dispatches; 400 validation keeps the server store.
        source.ShouldContain("private ValidationMessageStore? _rejectionValidationMessages;");
        source.ShouldContain("_rejectionValidationMessages = new ValidationMessageStore(_editContext);");
        source.ShouldContain("ServerValidationApplicator.ApplyRejection(_rejectionValidationMessages, ex, _serverValidationAllowlist, _model!);");
        source.ShouldNotContain("ServerValidationApplicator.ApplyRejection(_serverValidationMessages");
        source.ShouldContain("ServerValidationApplicator.Apply(_serverValidationMessages, ex, _serverValidationAllowlist, _model!);");
        source.ShouldContain("_rejectionValidationMessages?.Clear(e.FieldIdentifier);");

        int submit = source.IndexOf("private async Task SubmitAsync(bool validateBeforeDispatch)", StringComparison.Ordinal);
        int admission = source.IndexOf("if (!admission.IsAdmitted)", submit, StringComparison.Ordinal);
        int clear = source.IndexOf("_rejectionValidationMessages?.Clear();", submit, StringComparison.Ordinal);
        int validate = source.IndexOf("!_editContext.Validate()", submit, StringComparison.Ordinal);
        clear.ShouldBeGreaterThan(admission);
        clear.ShouldBeLessThan(validate);
    }

    [Fact]
    public void EmitValidationDescriptorsShareTheFieldVisibilityPredicate() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("TenantId", "String", FormFieldTypeCategory.TextInput, "Tenant ID", false, true, null),
            new FormFieldModel("RecordId", "String", FormFieldTypeCategory.TextInput, "Record ID", false, true, null, fieldGroup: "Purge details"),
            new FormFieldModel("Reason", "String", FormFieldTypeCategory.TextInput, "Reason", false, true, null, fieldGroup: "Purge details"),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // BH5-05 — a descriptor exists only while its field renders under DerivableFieldsHidden /
        // ShowFieldsOnly, so a hidden field's message stays an unlinked summary entry.
        source.ShouldContain("private bool IsFieldRendered(string commandPropertyName)");
        source.ShouldContain("=> (!DerivableFieldsHidden || !IsDerivableField(commandPropertyName))");
        source.ShouldContain("&& (ShowFieldsOnly is null || System.Array.IndexOf(ShowFieldsOnly, commandPropertyName) >= 0);");
        int descriptors = source.IndexOf("private FcValidationFieldDescriptor[] BuildValidationFields()", StringComparison.Ordinal);
        int descriptorsEnd = source.IndexOf("return fields.ToArray();", descriptors, StringComparison.Ordinal);
        descriptorsEnd.ShouldBeGreaterThan(descriptors);
        string body = source[descriptors..descriptorsEnd];
        foreach (string property in new[] { "TenantId", "RecordId", "Reason" }) {
            body.ShouldContain("if (IsFieldRendered(\"" + property + "\")) fields.Add(new(\"" + property + "\"");
            source.ShouldContain("            if (IsFieldRendered(\"" + property + "\"))");
        }

        Regex.Count(body, @"fields\.Add\(").ShouldBe(3);
        // BH7-13 — descriptors carry no error id; fc-focus.js owns the Fluent error node's id.
        body.ShouldNotContain("-error");
        source.ShouldContain("                IsFieldRendered(\"RecordId\")");
        source.ShouldContain("                || IsFieldRendered(\"Reason\")");
        source.ShouldNotContain("!IsDerivableField(\"");

        // AA7-01 — a rejection counts as mapped only through the same rendered-field predicate, and a
        // rejection mapped only to hidden fields clears its store and takes the unmapped path.
        int rendered = source.IndexOf("private bool HasRenderedRejectionMessage()", StringComparison.Ordinal);
        rendered.ShouldBeGreaterThan(0);
        string renderedBody = source[rendered..source.IndexOf("        return false;", rendered, StringComparison.Ordinal)];
        foreach (string property in new[] { "TenantId", "RecordId", "Reason" }) {
            renderedBody.ShouldContain("if (IsFieldRendered(\"" + property + "\") && _rejectionValidationMessages[new FieldIdentifier(_model, \"" + property + "\")].Any()) return true;");
        }

        int rejection = source.IndexOf("catch (CommandRejectedException ex)", StringComparison.Ordinal);
        int mapped = source.IndexOf("hasMappedFieldErrors = rejectionValidation.HasMappedFieldErrors && HasRenderedRejectionMessage();", rejection, StringComparison.Ordinal);
        int clearHidden = source.IndexOf("_rejectionValidationMessages.Clear();", mapped, StringComparison.Ordinal);
        int dispatch = source.IndexOf(".RejectedAction(correlationId", rejection, StringComparison.Ordinal);
        mapped.ShouldBeGreaterThan(rejection);
        clearHidden.ShouldBeGreaterThan(mapped);
        clearHidden.ShouldBeLessThan(dispatch);
    }

    [Fact]
    public void EmitRejectionMappingWithoutEditableFieldsNeverCountsAsMapped() {
        CommandFormModel form = BuildForm([]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // AA7-01 — a surface with no linkable editor keeps every rejection on the unmapped path.
        source.ShouldContain("private static bool HasRenderedRejectionMessage() => false;");
        source.ShouldContain("hasMappedFieldErrors = rejectionValidation.HasMappedFieldErrors && HasRenderedRejectionMessage();");
    }

    [Fact]
    public void EmitNullableSwitchBindsAProxyAndNullableEnumBindsTheNullableModelProperty() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Enabled", "Boolean", FormFieldTypeCategory.Switch, "Enabled", false, false, null),
            new FormFieldModel("NotifyOwner", "Boolean", FormFieldTypeCategory.Switch, "Notify owner", true, false, null),
            new FormFieldModel("Priority", "Enum", FormFieldTypeCategory.Select, "Priority", false, true, "Counter.Domain.Priority"),
            new FormFieldModel("Escalation", "Enum", FormFieldTypeCategory.Select, "Escalation", true, false, "Counter.Domain.Priority"),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);

        // AA5-01 — a nullable bool binds a non-nullable proxy (the numeric split-binding pattern), and
        // ValidationFieldFor keeps validation on the nullable model property, so the form compiles.
        source.ShouldContain("private bool _NotifyOwnerProxy => _model.NotifyOwner ?? false;");
        masked.ShouldContain("__b.AddAttribute(#, \"Value\", _NotifyOwnerProxy);");
        masked.ShouldContain("__b.AddAttribute(#, \"ValueExpression\", (global::System.Linq.Expressions.Expression<Func<bool>>)(() => _NotifyOwnerProxy));");
        masked.ShouldContain("__b.AddAttribute(#, \"ValidationFieldFor\", (global::System.Linq.Expressions.Expression<Func<bool?>>)(() => _model.NotifyOwner));");
        masked.ShouldContain("EventCallback.Factory.Create<bool>(this, v => { _model.NotifyOwner = v; NotifyClientFieldChanged(\"NotifyOwner\"); })");

        // VG7-02 — a nullable enum binds FluentSelect<TEnum?, TEnum?> to the nullable model property, so
        // the select's own Fluent field renders the model field's validation message (the select ignores
        // ValidationFieldFor) and a null value selects no option.
        masked.ShouldContain("__b.OpenComponent<FluentSelect<Counter.Domain.Priority?, Counter.Domain.Priority?>>(#);");
        masked.ShouldContain("__b.AddAttribute(#, \"Value\", _model.Escalation);");
        masked.ShouldContain("__b.AddAttribute(#, \"ValueExpression\", (global::System.Linq.Expressions.Expression<Func<Counter.Domain.Priority?>>)(() => _model.Escalation));");
        masked.ShouldContain("EventCallback.Factory.Create<Counter.Domain.Priority?>(this, v => { _model.Escalation = v; NotifyClientFieldChanged(\"Escalation\"); })");
        source.ShouldContain("System.Linq.Enumerable.Select(System.Enum.GetValues<Counter.Domain.Priority>(), static value => (Counter.Domain.Priority?)value)");
        source.ShouldNotContain("_EscalationProxy");

        // Non-nullable editors keep their direct model binding and need no proxy.
        masked.ShouldContain("__b.OpenComponent<FluentSelect<Counter.Domain.Priority, Counter.Domain.Priority>>(#);");
        masked.ShouldContain("__b.AddAttribute(#, \"ValueExpression\", (global::System.Linq.Expressions.Expression<Func<bool>>)(() => _model.Enabled));");
        masked.ShouldContain("__b.AddAttribute(#, \"ValueExpression\", (global::System.Linq.Expressions.Expression<Func<Counter.Domain.Priority>>)(() => _model.Priority));");
        source.ShouldNotContain("_EnabledProxy");
        source.ShouldNotContain("_PriorityProxy");
        Regex.Count(masked, "\"ValidationFieldFor\"").ShouldBe(1);
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
        source.ShouldContain("IncrementCommandActions.RejectedAction(correlationId, ex.Message, ex.Resolution, ex.ErrorCode, ex.ReasonCategory, ex.SuggestedAction, ex.DocsCode, hasMappedFieldErrors)");
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
        source.ShouldContain("CommandExecutionAdmissionGate.TryAcquire(CreateAdmissionRequest())");
        source.ShouldContain("PresentBlockedSubmissionAsync(admission.DenialReason, admission.BlockingMessageId)");
        source.ShouldContain("CommandFeedbackPublisher.PublishWarning(CreateCommandBlockedWarning(reason));");
        // Story 13.3 BH2-06 — AM-20 speaks through the always-mounted shell status node, never through
        // a live region inserted together with its text.
        source.ShouldContain("OpenComponent<FcCommandBlockedOutcome>");
        source.ShouldNotContain("[\"aria-live\"] = \"polite\"");
    }

    [Fact]
    public void EmitEverySubmitChecksConcurrencyBeforeValidationCanPublishFeedback() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Name", "String", FormFieldTypeCategory.TextInput, "Name", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);

        // ECH-05 — one OnSubmit path; lifecycle and admission checks run before validation can focus a summary.
        masked.ShouldContain("__wrap.AddAttribute(#, \"OnSubmit\", EventCallback.Factory.Create<EditContext>(this, async _ => await OnSubmitAsync()));");
        masked.ShouldNotContain("\"OnValidSubmit\"");
        masked.ShouldNotContain("\"OnInvalidSubmit\"");
        source.ShouldContain("private Task OnSubmitAsync() => SubmitAsync(validateBeforeDispatch: true);");
        source.ShouldContain("RegisterExternalSubmit(() => _ = SubmitAsync(validateBeforeDispatch: false));");
        int submitIndex = source.IndexOf("private async Task SubmitAsync(bool validateBeforeDispatch)", StringComparison.Ordinal);
        int lifecycleGateIndex = source.IndexOf("await PresentBlockedSubmissionAsync(", submitIndex, StringComparison.Ordinal);
        int admissionIndex = source.IndexOf("CommandExecutionAdmissionGate.TryAcquire(CreateAdmissionRequest())", submitIndex, StringComparison.Ordinal);
        int admissionDeniedIndex = source.IndexOf("PresentBlockedSubmissionAsync(admission.DenialReason, admission.BlockingMessageId)", submitIndex, StringComparison.Ordinal);
        int validateIndex = source.IndexOf("!_editContext.Validate()", submitIndex, StringComparison.Ordinal);
        int summaryIndex = source.IndexOf("ShowValidationSummaryAsync(FcValidationSummaryKind.ClientValidation)", validateIndex, StringComparison.Ordinal);
        submitIndex.ShouldBeGreaterThanOrEqualTo(0);
        lifecycleGateIndex.ShouldBeGreaterThan(submitIndex);
        admissionIndex.ShouldBeGreaterThan(lifecycleGateIndex);
        admissionDeniedIndex.ShouldBeGreaterThan(admissionIndex);
        validateIndex.ShouldBeGreaterThan(admissionDeniedIndex);
        summaryIndex.ShouldBeGreaterThan(validateIndex);
        Regex.Count(source, @"_editContext\.Validate\(\)").ShouldBe(1);
    }

    [Fact]
    public void Emit_CommandExecutionAdmissionRunsBeforeBeforeSubmitAndSideEffects() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int validSubmitIndex = source.IndexOf("private async Task SubmitAsync(bool validateBeforeDispatch)", StringComparison.Ordinal);
        int beforeSubmitIndex = source.IndexOf("await BeforeSubmit().ConfigureAwait(false);", StringComparison.Ordinal);
        int admissionIndex = source.IndexOf("CommandExecutionAdmissionGate.TryAcquire", validSubmitIndex, StringComparison.Ordinal);
        int admissionTryIndex = source.IndexOf("try\n        {\n        if (!admission.IsAdmitted)", admissionIndex, StringComparison.Ordinal);
        int correlationIndex = source.IndexOf("var correlationId = UlidFactory.NewUlid();", StringComparison.Ordinal);
        int submittedIndex = source.IndexOf("IncrementCommandActions.SubmittedAction(correlationId, _model)", StringComparison.Ordinal);
        int dispatchIndex = source.IndexOf("CommandService.DispatchWithLifecycleObservationsAsync", StringComparison.Ordinal);
        int registerIndex = source.IndexOf("PendingCommandOutcomeResolver.AssociateAccepted", StringComparison.Ordinal);

        admissionIndex.ShouldBeLessThan(beforeSubmitIndex);
        admissionTryIndex.ShouldBeGreaterThan(admissionIndex);
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
        foreach (string placeholder in new[] { "{ViewKey}", "{EntityKey}", "{PriorStatus}", "{ExpectedStatus}", "{TenantId}", "{UserId}", "{Exception}" }) {
            source.ShouldNotContain("Command target resolution succeeded. " + placeholder, Case.Sensitive);
            source.ShouldNotContain("Command target resolution failed closed. Category={Category} " + placeholder, Case.Sensitive);
        }

        int assignedTarget = source.IndexOf("var commandTarget = targetResolution.Target;", StringComparison.Ordinal);
        int dispatchCall = source.IndexOf(
            "CommandService.DispatchWithLifecycleObservationsAsync",
            assignedTarget,
            StringComparison.Ordinal);
        assignedTarget.ShouldBeGreaterThan(0);
        dispatchCall.ShouldBeGreaterThan(assignedTarget);
        // One scope read both validates and binds the dispatch; a second read could observe the next scope.
        string dispatchWindow = source[assignedTarget..dispatchCall];
        dispatchWindow.ShouldContain("var dispatchScope = validatedScope?.Current();", Case.Sensitive);
        dispatchWindow.ShouldContain("dispatchOrigin == dispatchCurrent", Case.Sensitive);
        dispatchWindow.ShouldNotContain("IsAdmissionScopeCurrent()", Case.Sensitive);
        dispatchWindow.ShouldNotContain("throw new OperationCanceledException();", Case.Sensitive);

        string[] actualCategories = Regex.Matches(
                source + statusMoveSource + sameSource,
                @"FailCommandTargetResolution\(""(?<category>[^""]+)""\)",
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
        int cancelCheck = source.IndexOf("cancellationToken.ThrowIfCancellationRequested();", resolved, StringComparison.Ordinal);
        int success = source.IndexOf("TryLogCommandTargetResolutionSucceeded();", cancelCheck, StringComparison.Ordinal);
        int returned = source.IndexOf("return resolution;", success, StringComparison.Ordinal);
        resolved.ShouldBeGreaterThan(0);
        cancelCheck.ShouldBeGreaterThan(resolved);
        success.ShouldBeGreaterThan(cancelCheck);
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
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "HFC1016");
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
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "HFC1016");
    }

    [Fact]
    public void Emit_CommandExecutionAdmissionReleasesInFinally() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CommandFormModel form = BuildForm([
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // Story 11.21: anchor on the submitted-log CALL SITE, not on the message template. The
        // template now lives in the cached LoggerMessage delegate emitted at the end of the class.
        const string SubmittedLogCall = "LogCommandSubmitted(Logger, correlationId);";
        int submittedLogIndex = source.IndexOf(SubmittedLogCall, StringComparison.Ordinal);
        submittedLogIndex.ShouldBeGreaterThan(0);

        // DW-683: larger identifiers containing try/finally must not satisfy the keyword anchors.
        string inspectedSource = source.Insert(
            submittedLogIndex + SubmittedLogCall.Length,
            " int retryKeywordCollision = 0; int finallyKeywordCollision = retryKeywordCollision;");
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(inspectedSource, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty();

        SyntaxNode root = tree.GetRoot(ct);
        int misleadingTryIndex = inspectedSource.IndexOf("try", submittedLogIndex, StringComparison.Ordinal);
        int misleadingFinallyIndex = inspectedSource.IndexOf("finally", misleadingTryIndex, StringComparison.Ordinal);
        misleadingTryIndex.ShouldBeGreaterThan(submittedLogIndex);
        misleadingFinallyIndex.ShouldBeGreaterThan(misleadingTryIndex);
        root.FindToken(misleadingTryIndex).IsKind(SyntaxKind.IdentifierToken).ShouldBeTrue();
        root.FindToken(misleadingFinallyIndex).IsKind(SyntaxKind.IdentifierToken).ShouldBeTrue();

        InvocationExpressionSyntax disposeInvocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is IdentifierNameSyntax identifier
                && identifier.Identifier.ValueText == "admission"
                && memberAccess.Name.Identifier.ValueText == "Dispose"
                && invocation.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(method => method.Identifier.ValueText == "SubmitAsync"));
        FinallyClauseSyntax disposalFinally = disposeInvocation.Ancestors()
            .OfType<FinallyClauseSyntax>()
            .Single();
        TryStatementSyntax admissionTry = disposalFinally.Parent.ShouldBeOfType<TryStatementSyntax>();
        TryStatementSyntax lifecycleCleanupTry = admissionTry.Block.Statements
            .OfType<TryStatementSyntax>()
            .Single(statement => statement.SpanStart > submittedLogIndex);
        FinallyClauseSyntax lifecycleCleanupFinally = lifecycleCleanupTry.Finally.ShouldNotBeNull();

        admissionTry.Finally.ShouldBe(disposalFinally);
        admissionTry.Block.Span.Contains(submittedLogIndex).ShouldBeTrue();
        disposalFinally.Block.Span.Contains(disposeInvocation.Span).ShouldBeTrue();
        admissionTry.TryKeyword.SpanStart.ShouldBeLessThan(submittedLogIndex);
        submittedLogIndex.ShouldBeLessThan(lifecycleCleanupTry.TryKeyword.SpanStart);
        misleadingTryIndex.ShouldBeLessThan(lifecycleCleanupTry.TryKeyword.SpanStart);
        misleadingFinallyIndex.ShouldBeLessThan(lifecycleCleanupFinally.FinallyKeyword.SpanStart);
        lifecycleCleanupTry.TryKeyword.SpanStart.ShouldBeLessThan(lifecycleCleanupFinally.FinallyKeyword.SpanStart);
        lifecycleCleanupFinally.FinallyKeyword.SpanStart.ShouldBeLessThan(disposalFinally.FinallyKeyword.SpanStart);
        disposalFinally.FinallyKeyword.Span.End.ShouldBeLessThan(disposeInvocation.SpanStart);
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
        masked.ShouldContain("__b.OpenComponent<FluentTextInput>(#)");
        source.ShouldContain("EventCallback.Factory.Create<string?>(this, OnAmountChanged)");
        source.ShouldContain("NotifyClientFieldChanged(\"Amount\")");
        masked.ShouldContain("__b.AddAttribute(#, \"Required\", true)");
        source.ShouldContain("int.TryParse(value,");
    }

    [Fact]
    public void Emit_TextFieldEmitsFluentImmediateInputHandler() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.OpenComponent<FluentTextInput>(#)");
        source.ShouldContain("EventCallback.Factory.Create<string?>(this, value => { _model.Note = value; NotifyClientFieldChanged(\"Note\"); })");
        source.ShouldContain("NotifyClientFieldChanged(\"Note\")");
        masked.ShouldContain("__b.AddAttribute(#, \"Required\", false)");
    }

    [Fact]
    public void Emit_DescribedAndGroupedFieldsPreserveEscapedMetadataAndDeclaredOrder() {
        CommandFormModel form = BuildForm([
            new FormFieldModel(
                "FirstNote",
                "String",
                FormFieldTypeCategory.TextInput,
                "First Note",
                true,
                false,
                null,
                fieldGroup: "Primary \"workflow\"",
                description: "Explain the \"first\" value.\nKeep its declared line break."),
            new FormFieldModel(
                "SecondNote",
                "String",
                FormFieldTypeCategory.TextInput,
                "Second Note",
                true,
                false,
                null,
                fieldGroup: "Secondary workflow",
                description: "Explain the second value."),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int firstField = source.IndexOf("// Field: FirstNote", StringComparison.Ordinal);
        int secondField = source.IndexOf("// Field: SecondNote", StringComparison.Ordinal);
        firstField.ShouldBeGreaterThanOrEqualTo(0);
        secondField.ShouldBeGreaterThan(firstField);

        string escapedGroup = GeneratedLiteral.Escape("Primary \"workflow\"");
        string escapedDescription = GeneratedLiteral.Escape("Explain the \"first\" value.\nKeep its declared line break.");
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__b.OpenElement(#, \"fieldset\")");
        masked.ShouldContain("__b.AddAttribute(#, \"data-fc-field-group\", \"" + escapedGroup + "\")");
        masked.ShouldContain("__b.AddAttribute(#, \"aria-labelledby\", _formDomId + \"-field-group-1\")");
        masked.ShouldContain("__b.OpenElement(#, \"legend\")");
        masked.ShouldContain("__b.AddAttribute(#, \"id\", _formDomId + \"-field-group-1\")");
        source.ShouldContain(escapedDescription);

        // Story 13.3 BH3-09 — the group is Fluent-styled (Fluent 2 tokens and a FluentText legend).
        masked.ShouldContain("__b.AddAttribute(#, \"style\", \"margin: 0; padding: var(--spacingVerticalM) var(--spacingHorizontalM);");
        masked.ShouldContain("border: var(--strokeWidthThin) solid var(--colorNeutralStroke2)");
        masked.ShouldContain("__b.AddAttribute(#, \"Weight\", TextWeight.Semibold);");

        // The description is rendered inside the editor through Fluent field messaging, with a stable id.
        string firstFieldSource = GeneratedRenderTreeText.MaskSequenceArguments(source[firstField..secondField]);
        int inputStart = firstFieldSource.IndexOf("__b.OpenComponent<FluentTextInput>", StringComparison.Ordinal);
        int messageTemplate = firstFieldSource.IndexOf("__b.AddAttribute(#, \"MessageTemplate\", (RenderFragment)(__fieldMessage =>", StringComparison.Ordinal);
        int descriptionId = firstFieldSource.IndexOf("__fieldMessage.AddAttribute(#, \"id\", _formDomId + \"-FirstNote-description\");", StringComparison.Ordinal);
        int inputEnd = firstFieldSource.IndexOf("__b.CloseComponent();", inputStart, StringComparison.Ordinal);
        inputStart.ShouldBeGreaterThanOrEqualTo(0);
        messageTemplate.ShouldBeGreaterThan(inputStart);
        descriptionId.ShouldBeGreaterThan(messageTemplate);
        descriptionId.ShouldBeLessThan(inputEnd);
        masked.ShouldContain("__fieldMessage.AddAttribute(#, \"id\", _formDomId + \"-SecondNote-description\");");

        // BH3-08 / BH3-09 — no host ARIA that cannot reach the shadow control, and no second error node:
        // the Fluent field renders each error once.
        masked.ShouldNotContain("\"aria-describedby\"");
        masked.ShouldNotContain("\"aria-invalid\"");
        masked.ShouldNotContain("__b.AddAttribute(#, \"id\", _formDomId + \"-FirstNote-error\")");
        masked.ShouldContain("__b.AddAttribute(#, \"MessageCondition\", FluentFieldCondition.Always);");
        masked.ShouldNotContain("fc-command-field-message");
    }

    [Fact]
    public void EmitEveryEditorExposesInvalidStateForShadowControlProjection() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Name", "String", FormFieldTypeCategory.TextInput, "Name", false, true, null),
            new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null),
            new FormFieldModel("Enabled", "Boolean", FormFieldTypeCategory.Switch, "Enabled", false, false, null),
            new FormFieldModel("Due", "DateTime", FormFieldTypeCategory.DatePicker, "Due", false, false, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);

        // BH2-11 — the invalid state comes from every validation store, not only the parse error.
        masked.ShouldContain("__b.AddAttribute(#, \"data-fc-invalid\", IsFieldInvalid(\"Name\") ? \"true\" : \"false\");");
        masked.ShouldContain("__b.AddAttribute(#, \"data-fc-invalid\", !string.IsNullOrEmpty(_AmountParseError) || IsFieldInvalid(\"Amount\") ? \"true\" : \"false\");");
        masked.ShouldContain("__b.AddAttribute(#, \"data-fc-invalid\", IsFieldInvalid(\"Enabled\") ? \"true\" : \"false\");");
        masked.ShouldContain("__b.AddAttribute(#, \"data-fc-invalid\", IsFieldInvalid(\"Due\") ? \"true\" : \"false\");");
        source.ShouldContain("_editContext.GetValidationMessages(new FieldIdentifier(_model, commandPropertyName)).Any()");

        // The EditContext is the single error source: Fluent's built-in focus-loss "required" message
        // never shows error text for a field that reports valid.
        Regex.Count(masked, "__b.AddAttribute\\(#, \"MessageCondition\", FluentFieldCondition.Never\\);").ShouldBe(4);

        // BH3-08 — fc-focus.js projects that state and the description/error relationship onto the
        // focusable control inside each Fluent editor's shadow root. Failed installation is retried.
        source.ShouldContain("\"observeFieldAccessibility\", _formDomId");
        int afterRender = source.IndexOf("protected override async Task OnAfterRenderAsync(bool firstRender)", StringComparison.Ordinal);
        int observe = source.IndexOf("await ObserveFieldAccessibilityAsync().ConfigureAwait(false);", afterRender, StringComparison.Ordinal);
        observe.ShouldBeGreaterThan(afterRender);
        int retryGuard = source.IndexOf("if (!_fieldAccessibilityObserved)", afterRender, StringComparison.Ordinal);
        retryGuard.ShouldBeGreaterThan(afterRender);
        retryGuard.ShouldBeLessThan(observe);
        source.ShouldContain("if (await module.InvokeAsync<bool>(\"observeFieldAccessibility\", _formDomId).ConfigureAwait(false))");
        source.ShouldContain("_fieldAccessibilityObserved = true;");

        // Numeric editors keep their model-field validation association (loop 2 KEEP).
        source.ShouldContain("\"ValidationFieldFor\", (global::System.Linq.Expressions.Expression<Func<Int32>>)(() => _model.Amount)");

        string withoutFields = CommandFormEmitter.Emit(BuildForm(System.Array.Empty<FormFieldModel>()), BuildFluxor());
        withoutFields.ShouldNotContain("observeFieldAccessibility");
        withoutFields.ShouldNotContain("_fieldAccessibilityObserved");
    }

    [Theory]
    [InlineData("First.Domain", "Foo", "fc-command-form-First-Domain-Foo-")]
    [InlineData("First.Domain", "FooCommand", "fc-command-form-First-Domain-FooCommand-")]
    [InlineData("Second.Domain", "Foo", "fc-command-form-Second-Domain-Foo-")]
    public void Emit_DomIds_IncludeTheFullCommandIdentityWithoutChangingLifecycleIdentity(string commandNamespace, string typeName, string expectedPrefix)
    {
        string source = CommandFormEmitter.Emit(BuildForm([], typeName, commandNamespace), BuildFluxor(typeName, commandNamespace));

        source.ShouldContain("private readonly string _formDomId = \"" + expectedPrefix + "\"");
        GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldContain("builder.AddAttribute(#, \"CommandId\", \"foo\");");
    }

    [Fact]
    public void Emit_FocusInteropExplicitlyHandlesCircuitTeardown() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
        ]);

        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        int focusHelper = source.IndexOf("private async Task FocusElementAsync(string id)", StringComparison.Ordinal);
        focusHelper.ShouldBeGreaterThanOrEqualTo(0);
        source.IndexOf("catch (global::Microsoft.JSInterop.JSDisconnectedException) { }", focusHelper, StringComparison.Ordinal)
            .ShouldBeGreaterThan(focusHelper);
        System.Text.RegularExpressions.Regex.Count(
                source,
                @"catch \(global::Microsoft\.JSInterop\.JSDisconnectedException\)")
            .ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Emit_FieldsSharingADeclaredGroupUseOneVisibleProgrammaticContainer() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("RecordId", "String", FormFieldTypeCategory.TextInput, "Record ID", false, true, null, fieldGroup: "Purge details", description: "Record to purge."),
            new FormFieldModel("Reason", "String", FormFieldTypeCategory.TextInput, "Reason", false, true, null, fieldGroup: "Purge details", description: "Why the purge is required."),
        ]);

        string source = GeneratedRenderTreeText.MaskSequenceArguments(CommandFormEmitter.Emit(form, BuildFluxor()));

        Regex.Count(source, "data-fc-field-group").ShouldBe(1);
        Regex.Count(source, "__b.OpenElement\\(#, \\\"fieldset\\\"\\)").ShouldBe(1);
        Regex.Count(source, "__b.OpenElement\\(#, \\\"legend\\\"\\)").ShouldBe(1);
        source.ShouldContain("__legend.AddContent(#, \"Purge details\")");
        source.IndexOf("// Field: RecordId", StringComparison.Ordinal)
            .ShouldBeLessThan(source.IndexOf("// Field: Reason", StringComparison.Ordinal));
    }

    [Fact]
    public void EmitSummaryDescriptorsFollowRenderedOrderForGroupMembersDeclaredApart() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("RecordId", "String", FormFieldTypeCategory.TextInput, "Record ID", false, true, null, fieldGroup: "Purge details"),
            new FormFieldModel("Note", "String", FormFieldTypeCategory.TextInput, "Note", true, false, null),
            new FormFieldModel("Reason", "String", FormFieldTypeCategory.TextInput, "Reason", false, true, null, fieldGroup: "Purge details"),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // BH2-08 — a declared group is hoisted to its first member, and the summary follows that DOM order.
        int recordField = source.IndexOf("// Field: RecordId", StringComparison.Ordinal);
        int reasonField = source.IndexOf("// Field: Reason", StringComparison.Ordinal);
        int noteField = source.IndexOf("// Field: Note", StringComparison.Ordinal);
        recordField.ShouldBeLessThan(reasonField);
        reasonField.ShouldBeLessThan(noteField);
        int descriptors = source.IndexOf("private FcValidationFieldDescriptor[] BuildValidationFields()", StringComparison.Ordinal);
        int recordDescriptor = source.IndexOf("new(\"RecordId\"", descriptors, StringComparison.Ordinal);
        int reasonDescriptor = source.IndexOf("new(\"Reason\"", descriptors, StringComparison.Ordinal);
        int noteDescriptor = source.IndexOf("new(\"Note\"", descriptors, StringComparison.Ordinal);
        descriptors.ShouldBeGreaterThanOrEqualTo(0);
        recordDescriptor.ShouldBeLessThan(reasonDescriptor);
        reasonDescriptor.ShouldBeLessThan(noteDescriptor);
        CommandFormEmitter.OrderFieldsForRender(form.Fields.AsImmutableArray())
            .Select(field => field.PropertyName)
            .ShouldBe(["RecordId", "Reason", "Note"]);
    }

    [Fact]
    public void EmitUnmappedRejectionTextNeverFeedsALaterClientSummary() {
        CommandFormModel form = BuildForm([
            new FormFieldModel("Name", "String", FormFieldTypeCategory.TextInput, "Name", false, true, null),
        ]);
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // VR-03 / VG2-10 — form-level text from an earlier attempt is dropped once a later attempt is admitted.
        int submit = source.IndexOf("private async Task SubmitAsync(bool validateBeforeDispatch)", StringComparison.Ordinal);
        int admission = source.IndexOf("if (!admission.IsAdmitted)", submit, StringComparison.Ordinal);
        int reset = source.IndexOf("_serverFormLevelErrors = System.Array.Empty<string>();", admission, StringComparison.Ordinal);
        int validate = source.IndexOf("!_editContext.Validate()", submit, StringComparison.Ordinal);
        reset.ShouldBeGreaterThan(admission);
        reset.ShouldBeLessThan(validate);
        source.ShouldContain("_serverFormLevelErrors = hasMappedFieldErrors ? rejectionValidation.UnmappedMessages : System.Array.Empty<string>();");
        source.ShouldNotContain("_serverFormLevelErrors = rejectionValidation.UnmappedMessages;");
    }

    [Fact]
    public void EmitPolicyFormKeepsInputsForTransientAuthorizationFailuresAndFocusesOnlyActivationDenials() {
        CommandFormModel form = BuildForm(
            [new FormFieldModel("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null)],
            "IncrementCommand",
            "Counter.Domain",
            "OrderApprover");
        string source = CommandFormEmitter.Emit(form, BuildFluxor());

        // BH2-05 / BH2-04 — only a genuine denial replaces the form, and only an operator submit focuses it.
        source.ShouldContain("_authorizationDenied = !infrastructureFailure;");
        source.ShouldContain("CommandAuthorizationReason.CatalogInconsistent");
        source.ShouldContain("CommandAuthorizationReason.Pending;");
        Regex.Count(source, @"SetAuthorizationWarning\([^;]*operatorActivation: true\)").ShouldBe(2);
        Regex.Count(source, @"SetAuthorizationWarning\([^;]*operatorActivation: false\)").ShouldBe(1);
        // E4-18 — a background denial (operatorActivation: false) OR-keeps an operator submit's pending
        // heading focus rather than overwriting it with false before it renders.
        source.ShouldContain("_authorizationFocusPending = _authorizationDenied && (_authorizationFocusPending || operatorActivation);");
        source.ShouldNotContain("_authorizationFocusPending = operatorActivation && _authorizationDenied;");
        source.ShouldContain("if (!_authorizationDenied && sequence > 1)");
        source.ShouldContain("\"captureFocusBeforeReplacement\", _formDomId + \"-authorization-heading\"");
        source.ShouldContain("\"focusReplacementHeading\", headingId, headingId");

        // The form denial card is a named group, not a region landmark per denied form.
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);
        masked.ShouldContain("__denied.AddAttribute(#, \"role\", \"group\");");

        // BH3-01 — Ready=false and the refresh stamp precede the capture round trip.
        int refresh = source.IndexOf("private async Task RefreshPresentationAuthorizationAsync()", StringComparison.Ordinal);
        int notReady = source.IndexOf("_authorizationPresentationReady = false;", refresh, StringComparison.Ordinal);
        int stamp = source.IndexOf("Interlocked.Increment(ref _authorizationRefreshSequence)", refresh, StringComparison.Ordinal);
        int capture = source.IndexOf("CaptureFocusBeforeReplacementAsync()", refresh, StringComparison.Ordinal);
        notReady.ShouldBeGreaterThan(refresh);
        stamp.ShouldBeGreaterThan(notReady);
        capture.ShouldBeGreaterThan(stamp);
    }

    [Fact]
    public void EmitBlockedSubmitHandsOutcomeToHostWhenTheFormIsNotPerceivable() {
        string source = CommandFormEmitter.Emit(BuildForm(System.Array.Empty<FormFieldModel>()), BuildFluxor());
        string masked = GeneratedRenderTreeText.MaskSequenceArguments(source);

        // VG2-13 — a hidden zero-field form hands its AM-20 outcome to the renderer.
        source.ShouldContain("[Parameter] public EventCallback<bool> OnBlockedSubmission { get; set; }");
        source.ShouldContain("await InvokeAsync(() => OnBlockedSubmission.InvokeAsync(true));");
        source.ShouldContain("await InvokeAsync(() => OnBlockedSubmission.InvokeAsync(false));");
        masked.ShouldContain("if (!OnBlockedSubmission.HasDelegate)");
        int present = source.IndexOf("private async Task PresentBlockedSubmissionAsync(", StringComparison.Ordinal);
        int presentEnd = source.IndexOf("private async Task FocusElementAsync(", present, StringComparison.Ordinal);
        string presentBody = source[present..presentEnd];
        presentBody.ShouldNotContain("Validate()");
        presentBody.ShouldNotContain("Dispatcher.Dispatch");
        presentBody.ShouldNotContain("ShowAndFocusAsync");

        // BH4-04 — a scope-unavailable attempt uses the AM-20 attempted-control rule instead of forcing
        // focus onto the submit button, so Enter in a field keeps that field focused.
        presentBody.ShouldContain("await FocusAttemptedControlAsync(_formDomId + \"-submit\").ConfigureAwait(false);");
        presentBody.ShouldNotContain("FocusElementAsync(");
        source.ShouldContain("_ = await module.InvokeAsync<bool>(\"focusAttemptedControl\", id, false).ConfigureAwait(false);");
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
        source.ShouldContain("\"ValidationFieldFor\", (global::System.Linq.Expressions.Expression<Func<Int32?>>)(() => _model.Quantity)");
        source.ShouldContain("\"ValidationFieldFor\", (global::System.Linq.Expressions.Expression<Func<Decimal?>>)(() => _model.DiscountAmount)");
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
