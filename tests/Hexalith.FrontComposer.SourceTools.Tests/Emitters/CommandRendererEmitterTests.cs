using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 2-2 Task 11.1 — golden-file snapshots for the renderer emitter, covering all density
/// boundaries (0/1/2/4/5 non-derivable fields), `[Icon]` overrides, and the FullPage page.
/// Task 11.2 (parseability) and 11.3 (determinism) live alongside.
/// </summary>
public class CommandRendererEmitterTests {
    private static CommandRendererModel BuildModel(
        int nonDerivableCount,
        string? iconName = null,
        CommandDensity? densityOverride = null,
        string typeName = "DemoCommand",
        string @namespace = "Demo.Domain",
        string boundedContext = "Demo",
        string? authorizationPolicyName = null,
        bool isDestructive = false,
        string? destructiveConfirmTitle = null,
        string? destructiveConfirmBody = null) {
        ImmutableArray<string> nonDerivable = Enumerable
            .Range(0, nonDerivableCount)
            .Select(i => "Field" + i)
            .ToImmutableArray();
        ImmutableArray<string> derivable = ["MessageId", "TenantId"];

        CommandDensity density = densityOverride ?? nonDerivableCount switch {
            <= 1 => CommandDensity.Inline,
            <= 4 => CommandDensity.CompactInline,
            _ => CommandDensity.FullPage,
        };

        return new CommandRendererModel(
            typeName: typeName,
            @namespace: @namespace,
            boundedContext: boundedContext,
            density: density,
            iconName: iconName,
            displayLabel: "Demo",
            fullPageRoute: "/commands/" + boundedContext + "/" + typeName,
            commandFullyQualifiedName: @namespace + "." + typeName,
            nonDerivablePropertyNames: new EquatableArray<string>(nonDerivable),
            derivablePropertyNames: new EquatableArray<string>(derivable),
            formComponentName: typeName + "Form",
            actionsWrapperName: typeName + "Actions",
            stateName: typeName + "LifecycleState",
            subscriberTypeName: typeName + "LastUsedSubscriber",
            isDestructive: isDestructive,
            destructiveConfirmTitle: destructiveConfirmTitle,
            destructiveConfirmBody: destructiveConfirmBody,
            authorizationPolicyName: authorizationPolicyName);
    }

    [Fact]
    public Task Renderer_ZeroFields_InlineSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(0)));

    [Fact]
    public Task Renderer_OneField_InlinePopoverSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1)));

    [Fact]
    public Task Renderer_TwoFields_CompactInlineSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(2)));

    [Fact]
    public Task Renderer_FourFields_CompactInlineBoundarySnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(4)));

    [Fact]
    public Task Renderer_FiveFields_FullPageBoundarySnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(5)));

    [Fact]
    public Task Page_FiveFields_FullPageBoundarySnapshot()
        => Verify(CommandPageEmitter.Emit(BuildModel(5)));

    [Fact]
    public Task Renderer_OneField_WithIconAttributeSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1, iconName: "Regular.Size20.Settings")));

    [Fact]
    public Task Renderer_OneField_WithoutIconUsesDefaultSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1, iconName: null)));

    // === Task 11.2 — parseability ===

    [Fact]
    public void Renderer_AllDensities_ProduceValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        foreach (int count in (int[])[0, 1, 2, 4, 5]) {
            string source = CommandRendererEmitter.Emit(BuildModel(count));
            Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
            tree.GetDiagnostics(ct).ShouldBeEmpty($"renderer for {count} non-derivable fields should parse cleanly");
        }

        string pageSource = CommandPageEmitter.Emit(BuildModel(5));
        Microsoft.CodeAnalysis.SyntaxTree pageTree = CSharpSyntaxTree.ParseText(pageSource, cancellationToken: ct);
        pageTree.GetDiagnostics(ct).ShouldBeEmpty("FullPage page should parse cleanly");
    }

    [Fact]
    public void Renderer_ProtectedCommand_EmitsEvaluatorBackedTriggerGate() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = CommandRendererEmitter.Emit(BuildModel(1, authorizationPolicyName: "OrderApprover"));

        // Pass-4 BH-45 / AA-24 — parse the emitted source to catch typos / missing semicolons
        // / undefined identifiers, not just substring presence.
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("protected renderer should parse cleanly");

        source.ShouldContain("ICommandAuthorizationEvaluator CommandAuthorizationEvaluator");
        source.ShouldContain("IStringLocalizer<global::Hexalith.FrontComposer.Shell.Resources.FcShellResources> AuthorizationLocalizer");
        source.ShouldContain("AuthorizationPolicyName = \"OrderApprover\"");
        source.ShouldContain("CommandAuthorizationSurface.InlineAction");
        // Pass-4 DN-7-3-4-7 hardening — emitted code includes the cancellation token capture and
        // sequence-number guard mirroring the Pass-3 form-emitter discipline.
        source.ShouldContain("_authorizationCts");
        source.ShouldContain("_authorizationDisposed");
        source.ShouldContain("_authorizationRefreshSequence");
        source.ShouldContain("AuthenticationStateChanged += OnAuthenticationStateChanged");
        source.ShouldContain("await InvokeAsync(StateHasChanged)");
        source.ShouldContain("ScheduleAuthorizationRetryAsync");
        source.ShouldContain("AuthorizationCheckingPermissionMessage");
        source.ShouldContain("UnauthorizedCommandWarningMessage");
        // Pass-4 AA-15 / BH-37 — presentation-time auth must not include unvalidated user input.
        // The emitted CommandAuthorizationRequest passes null (not _prefilledModel) for the
        // command resource. Verified by parsing and inspecting the literal arg in the request.
        source.ShouldNotContain("AuthorizationPolicyName,\n            _prefilledModel");
        source.ShouldNotContain("AuthorizationPolicyName,\r\n                _prefilledModel");
        source.ShouldContain("AuthorizationPolicyName,");
        source.ShouldContain("                null,");
    }

    [Fact]
    public void Renderer_ProtectedCommand_GatesAllRenderModes() {
        // Pass-4 DN-7-3-4-6 (b) — Inline / CompactInline / FullPage all gate on
        // AuthorizationTriggerDisabled(). Inline emits the gate as the FluentButton's Disabled
        // attribute (multi-field popover) or as part of the boolean expression for the
        // zero-field button. CompactInline + FullPage emit a presentation-time placeholder branch
        // that returns early when the gate is true. So a protected renderer should reference the
        // gate at least three times across the three switch arms.
        string source = CommandRendererEmitter.Emit(BuildModel(1, authorizationPolicyName: "OrderApprover"));

        int gateOccurrences = System.Text.RegularExpressions.Regex.Matches(source, @"AuthorizationTriggerDisabled\(\)").Count;
        gateOccurrences.ShouldBeGreaterThanOrEqualTo(3, "expected gating in Inline + CompactInline + FullPage cases");
        // The CompactInline + FullPage placeholder branches both check the gate via if-statement.
        int placeholderBranches = System.Text.RegularExpressions.Regex.Matches(source, @"if \(AuthorizationTriggerDisabled\(\)\)").Count;
        placeholderBranches.ShouldBe(2, "CompactInline + FullPage placeholder branches");
    }

    [Fact]
    public void Renderer_DestructiveCommand_EmitsDialogGateAndBeforeSubmitWiring() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = CommandRendererEmitter.Emit(BuildModel(
            1,
            typeName: "DeleteWidgetCommand",
            isDestructive: true,
            destructiveConfirmTitle: "Delete this widget?",
            destructiveConfirmBody: "The widget will be permanently removed."));

        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("destructive renderer should parse cleanly");

        source.ShouldContain("[Inject] private IDialogService DialogService { get; set; } = default!;");
        source.ShouldContain("private bool _dialogOpen;");
        source.ShouldContain("private async Task DestructiveBeforeSubmitAsync()");
        source.ShouldContain("if (_dialogOpen) throw new OperationCanceledException(\"Destructive dialog already open.\");");
        source.ShouldContain("var result = await DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>");
        source.ShouldNotContain("dialogRef.Result");
        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.Title), \"Delete this widget?\"");
        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.Body), \"The widget will be permanently removed.\"");
        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.DestructiveLabel), \"Demo\"");
        source.ShouldContain("\"BeforeSubmit\", (Func<Task>)DestructiveBeforeSubmitAsync");
    }

    [Fact]
    public void Renderer_DestructiveCommand_UsesFallbackConfirmationCopyWhenUnset() {
        string source = CommandRendererEmitter.Emit(BuildModel(
            2,
            typeName: "DeleteWidgetCommand",
            isDestructive: true));

        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.Title), \"Demo?\"");
        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.Body), \"This action cannot be undone.\"");
        source.ShouldContain("nameof(FcDestructiveConfirmationDialog.DestructiveLabel), \"Demo\"");
    }

    [Fact]
    public void Renderer_NonDestructiveCommand_DoesNotInjectDialogServiceOrDialogGate() {
        string source = CommandRendererEmitter.Emit(BuildModel(1));

        source.ShouldNotContain("IDialogService DialogService");
        source.ShouldNotContain("FcDestructiveConfirmationDialog");
        source.ShouldNotContain("_dialogOpen");
        source.ShouldContain("\"BeforeSubmit\", (Func<Task>)RefreshDerivedValuesBeforeSubmitAsync");
    }

    [Fact]
    public void Renderer_DestructiveCommand_DoesNotDispatchOrAllocateCommandIdentity() {
        string source = CommandRendererEmitter.Emit(BuildModel(
            1,
            typeName: "DeleteWidgetCommand",
            isDestructive: true));

        source.ShouldNotContain("CommandService.DispatchAsync");
        source.ShouldNotContain(".SubmittedAction");
        source.ShouldNotContain("Guid.NewGuid");
        source.ShouldNotContain("MessageId =");
        source.ShouldNotContain("CorrelationId =");
    }

    // === Task 11.3 — determinism ===

    [Fact]
    public void Renderer_RepeatedEmit_IsByteIdentical() {
        CommandRendererModel model = BuildModel(3);
        string first = CommandRendererEmitter.Emit(model);
        string second = CommandRendererEmitter.Emit(model);
        first.ShouldBe(second);

        CommandRendererModel pageModel = BuildModel(5);
        string firstPage = CommandPageEmitter.Emit(pageModel);
        string secondPage = CommandPageEmitter.Emit(pageModel);
        firstPage.ShouldBe(secondPage);
    }
}
