using System.Security.Claims;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-4 Task 5.6 — the generated renderer output of every density must include the
/// <c>fc-lifecycle-wrapper</c> marker now that <see cref="CommandFormEmitter"/> wraps its
/// emitted <c>&lt;EditForm&gt;</c> in <c>&lt;FcLifecycleWrapper&gt;</c> (Task 4.1).
/// </summary>
public sealed class CommandRendererWrapperIntegrationTests : CommandRendererTestBase {
    [Fact]
    public async Task GeneratedForm_FieldsSharingGroupRenderOneNamedContainerWithDescriptionsInDeclaredOrder() {
        await InitializeStoreAsync();

        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> groups = cut.FindAll("fieldset[data-fc-field-group='Change details']");
            groups.Count.ShouldBe(1);
            AngleSharp.Dom.IElement group = groups[0];
            AngleSharp.Dom.IElement legend = group.QuerySelector("legend").ShouldNotBeNull();
            legend.TextContent.ShouldBe("Change details");
            group.GetAttribute("aria-labelledby").ShouldBe(legend.Id);

            AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> fields = group.QuerySelectorAll("[data-fc-validation-field='true']");
            fields.Length.ShouldBe(2);
            fields[0].GetAttribute("name").ShouldBe("RecordId");
            fields[1].GetAttribute("name").ShouldBe("Reason");

            foreach (AngleSharp.Dom.IElement field in fields) {
                string[] describedByIds = field.GetAttribute("aria-describedby").ShouldNotBeNull().Split(' ');
                describedByIds.Length.ShouldBe(2);
                describedByIds.All(id => cut.FindAll("#" + id).Count == 1).ShouldBeTrue();
            }
        });
    }

    [Fact]
    public async Task Renderer_CompactInline_Markup_Contains_FcLifecycleWrapper_Class() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_Inline_Markup_Contains_FcLifecycleWrapper_Class() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_FullPage_Markup_Contains_FcLifecycleWrapper_Class() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task GeneratedForm_SubmitAccepted_RendersLifecyclePhasesAndKeepsFormPresent() {
        ControlledCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Configure<FcShellOptions>(o => {
            o.SyncPulseThresholdMs = 50;
            o.StillSyncingThresholdMs = 500;
            o.TimeoutActionThresholdMs = 5_000;
        });
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "accepted name",
                Amount = 7,
            }));

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submitting", Case.Insensitive);
            cut.Find("form").ShouldNotBeNull();
        });

        service.AllowAcknowledge.SetResult();
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission acknowledged", Case.Insensitive);
            cut.Markup.ShouldContain("fc-acknowledged-badge", Case.Insensitive);
            cut.Find("form").ShouldNotBeNull();
        });

        string correlationId = state.Value.CorrelationId.ShouldNotBeNull();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new TwoFieldCompactCommandActions.SyncingAction(correlationId)));
        cut.WaitForAssertion(() => {
            cut.Find(".fc-lifecycle-wrapper").GetAttribute("class")!.ShouldContain("fc-lifecycle-pulse");
            cut.Find("form").ShouldNotBeNull();
        });

        await cut.InvokeAsync(() => dispatcher.Dispatch(new TwoFieldCompactCommandActions.ConfirmedAction(correlationId)));
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission confirmed", Case.Insensitive);
            cut.Find("form").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task GeneratedForm_SubmitRejected_RendersTypedFieldsAndKeepsFormEditable() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new RejectingCommandService()));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "existing name",
                Amount = 7,
            }));

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission rejected", Case.Insensitive);
            cut.Markup.ShouldContain("ORDER_LOCKED");
            cut.Markup.ShouldContain("Concurrency");
            cut.Markup.ShouldContain("Reload before retrying");
            cut.Markup.ShouldContain("FC-CMD-409");
            cut.Find("form").ShouldNotBeNull();
            cut.Markup.ShouldContain("existing name");
            cut.Markup.ShouldContain("value=\"7\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task UnmappedRejectionEditRetryFocusesFirstEditableWithinWrapper() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new RejectingCommandService()));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "preserved name",
                Amount = 7,
            }));

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='fc-rejection-edit-retry']"));

        cut.Find("[data-testid='fc-rejection-edit-retry']").Click();

        cut.WaitForAssertion(() => FcFocusModule.Invocations
            .Count(invocation => invocation.Identifier == "focusFirstEditableWithin")
            .ShouldBe(1));
        FcFocusModule.Invocations
            .Any(invocation => invocation.Identifier == "focusFirstEditable")
            .ShouldBeFalse();
    }

    [Fact]
    public async Task GeneratedForm_ClientValidation_RendersOneCompleteLinkedNonLiveSummary() {
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = string.Empty,
                Amount = 7,
            }));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> summaries = cut.FindAll("[data-testid='fc-validation-summary']");
            summaries.Count.ShouldBe(1);
            summaries[0].HasAttribute("aria-live").ShouldBeFalse();
            summaries[0].GetAttribute("role").ShouldNotBe("alert");
            cut.FindAll("[data-fc-validation-target]").ShouldNotBeEmpty();
            cut.Markup.ShouldContain("value=\"7\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task GeneratedForm_MappedRejection_FocusesLinkedNonLiveSummaryAndSuppressesGenericRejectionAnnouncement() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new MappedRejectingCommandService()));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "preserved name",
                Amount = 7,
            }));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
            state.Value.HasMappedFieldErrors.ShouldBeTrue();
            AngleSharp.Dom.IElement summary = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
            summary.GetAttribute("role").ShouldNotBe("alert");
            summary.HasAttribute("aria-live").ShouldBeFalse();
            cut.FindAll("[data-fc-phase='rejected']").ShouldBeEmpty();
            cut.Find("[data-fc-validation-target]").GetAttribute("href").ShouldNotBeNull().ShouldContain("-Name");
            cut.Markup.ShouldContain("preserved name");
            cut.Markup.ShouldContain("value=\"7\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task GeneratedForm_RetryExhaustion_RendersWarningPreservesInputAndDoesNotRegisterPending() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new RetryExhaustedCommandService()));
        await InitializeStoreAsync();
        List<CommandFeedbackWarning> warnings = [];
        using IDisposable subscription = Services.GetRequiredService<ICommandFeedbackPublisher>().Subscribe(warnings.Add);
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "preserved name",
                Amount = 7,
            }));

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            state.Value.State.ShouldBe(CommandLifecycleState.Idle);
            pending.Snapshot().ShouldBeEmpty();
            warnings.ShouldContain(w => w.Kind == CommandWarningKind.RetryableDispatchFailed);
            cut.Markup.ShouldContain("Command was not accepted");
            cut.Markup.ShouldContain("EventStore did not accept the command", Case.Insensitive);
            cut.Markup.ShouldContain("Retry after 1 seconds.");
            cut.Markup.ShouldContain("preserved name");
            cut.Markup.ShouldContain("value=\"7\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task GeneratedForms_RapidSecondSubmit_BlocksBeforeDispatchLifecycleAndPendingMutation() {
        BlockingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<CommandFeedbackWarning> warnings = [];
        using IDisposable subscription = Services.GetRequiredService<ICommandFeedbackPublisher>().Subscribe(warnings.Add);
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IState<FourFieldCompactCommandLifecycleState> secondState = Services.GetRequiredService<IState<FourFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> first = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "first",
                Amount = 1,
            }));
        IRenderedComponent<FourFieldCompactCommandForm> second = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand {
                Name = "blocked name",
                Description = "blocked description",
                Amount = 2,
                Priority = 3,
            }));

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        second.Find("form").Submit();

        second.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            pending.Snapshot().ShouldBeEmpty();
            secondState.Value.State.ShouldBe(CommandLifecycleState.Idle);
            AngleSharp.Dom.IElement status = second.Find("[role='status'][aria-live='polite']");
            status.GetAttribute("aria-atomic").ShouldBe("true");
            status.TextContent.ShouldContain("This command did not run. Another command is already in progress.", Case.Insensitive);
            second.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
            second.FindAll("fluent-message-bar").ShouldBeEmpty();
            second.Markup.ShouldContain("blocked name");
            warnings.Count.ShouldBe(1);
            warnings[0].Detail.ShouldNotBeNull().ShouldNotContain("queued", Case.Insensitive);
        });

        service.AllowDispatch.SetResult();
        first.WaitForAssertion(() => pending.Snapshot().Count.ShouldBe(1));
    }

    [Fact]
    public async Task GeneratedForms_InvalidLaterSubmitReportsConcurrencyWithoutValidationFocusOrDispatch() {
        BlockingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<CommandFeedbackWarning> warnings = [];
        using IDisposable subscription = Services.GetRequiredService<ICommandFeedbackPublisher>().Subscribe(warnings.Add);
        IRenderedComponent<FourFieldCompactCommandForm> first = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand { Name = "first" }));
        IRenderedComponent<TwoFieldCompactCommandForm> invalid = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = string.Empty, Amount = 7 }));

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        int validationFocusCount = FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome");
        invalid.Find("form").Submit();

        invalid.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            invalid.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
            invalid.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
            warnings.Count.ShouldBe(1);
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome")
                .ShouldBe(validationFocusCount);
            FcFocusModule.Invocations.ShouldContain(invocation => invocation.Identifier == "focusElementById"
                && invocation.Arguments.Count > 0
                && invocation.Arguments[0] != null
                && invocation.Arguments[0]!.ToString()!.EndsWith("-submit", StringComparison.Ordinal));
        });

        service.AllowDispatch.SetResult();
    }

    [Fact]
    public async Task GeneratedForm_BlockedSubmitOmitsViewActionWhenNoActiveLifecycleTargetIsMounted() {
        _ = FcFocusModule.Setup<bool>("hasActiveLifecycle", _ => true).SetResult(false);
        BlockingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<FourFieldCompactCommandForm> first = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand { Name = "first" }));
        IRenderedComponent<TwoFieldCompactCommandForm> blocked = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "second", Amount = 7 }));

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        blocked.Find("form").Submit();

        blocked.WaitForAssertion(() => {
            _ = blocked.Find("[data-testid='fc-command-blocked']");
            blocked.FindAll("[aria-label='View active command']").ShouldBeEmpty();
            service.DispatchCount.ShouldBe(1);
        });

        service.AllowDispatch.SetResult();
    }

    [Fact]
    public async Task GeneratedForm_AcceptanceAfterScopeSwitch_SkipsPendingRegistrationAndLogs() {
        BlockingCommandService service = new();
        ILogger<TwoFieldCompactCommandForm> logger = EnabledLoggerSubstitute.Create<TwoFieldCompactCommandForm>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Replace(ServiceDescriptor.Scoped<ILogger<TwoFieldCompactCommandForm>>(_ => logger));
        Services.AddScoped<IValidatedPendingScope>(_ => new TestValidatedScope(() =>
            (UserContext.TenantId!, UserContext.UserId!)));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "old-scope", Amount = 1 }));

        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        UserContext.TenantId = "new-tenant";
        Services.GetRequiredService<IDispatcher>().Dispatch(new ScopeChangedAction());
        service.AllowDispatch.SetResult();

        cut.WaitForAssertion(() => {
            pending.Snapshot().ShouldBeEmpty();
            logger.ReceivedCalls().ShouldContain(call => call.GetArguments().OfType<EventId>()
                .Any(id => id.Id == 5905));
        });
    }

    [Fact]
    public async Task GeneratedForms_PendingCommandBlocksUntilTerminalResolution() {
        SequencedCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> first = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand {
                Name = "first",
                Amount = 1,
            }));
        IRenderedComponent<FourFieldCompactCommandForm> second = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand {
                Name = "second",
                Description = "second description",
                Amount = 2,
                Priority = 3,
            }));

        first.Find("form").Submit();
        first.WaitForAssertion(() => pending.Snapshot().Single().Status.ShouldBe(PendingCommandStatus.Pending));

        second.Find("form").Submit();
        second.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            second.Markup.ShouldContain("This command did not run. Another command is already in progress.", Case.Insensitive);
        });

        pending.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(SequencedCommandService.FirstMessageId))
            .Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        second.Find("form").Submit();

        second.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(2);
            pending.GetByMessageId(SequencedCommandService.SecondMessageId).ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task ProtectedGeneratedForm_AllowedAuthorization_DispatchesAfterSubmitChecks() {
        var evaluator = new FixedAuthorizationEvaluator(CommandAuthorizationDecision.Allowed("corr-allowed"));
        RecordingCommandService service = new();
        RegisterAuthorization(evaluator);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();

        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand {
                Name = "approved name",
                Amount = 7,
            }));

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            pending.Snapshot().Single().MessageId.ShouldBe(RecordingCommandService.MessageId);
        });
        service.LastCommand.ShouldNotBeNull();
        service.LastCommand!.Name.ShouldBe("approved name");
        evaluator.Requests.ShouldContain(r => r.SourceSurface == CommandAuthorizationSurface.GeneratedForm);
    }

    [Theory]
    [MemberData(nameof(BlockingAuthorizationDecisions))]
    public async Task ProtectedGeneratedForm_BlockedAuthorization_ReplacesControlsAndBlocksSideEffects(
        CommandAuthorizationDecision decision,
        CommandWarningKind expectedWarningKind) {
        var evaluator = new FixedAuthorizationEvaluator(decision);
        RecordingCommandService service = new();
        RegisterAuthorization(evaluator);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        List<CommandFeedbackWarning> warnings = [];
        using IDisposable subscription = Services.GetRequiredService<ICommandFeedbackPublisher>().Subscribe(warnings.Add);
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IState<ProtectedTwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<ProtectedTwoFieldCompactCommandLifecycleState>>();

        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand {
                Name = "blocked name",
                Amount = 9,
            }));

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(0);
            pending.Snapshot().ShouldBeEmpty();
            state.Value.State.ShouldBe(CommandLifecycleState.Idle);
            cut.FindAll("input").ShouldBeEmpty();
            cut.FindAll("fluent-button").ShouldBeEmpty();
            AngleSharp.Dom.IElement heading = cut.Find("[id$='-authorization-heading']");
            heading.GetAttribute("tabindex").ShouldBe("-1");
            cut.Find("section[aria-labelledby]").HasAttribute("aria-live").ShouldBeFalse();
            warnings.ShouldNotBeEmpty();
            warnings.ShouldContain(w => w.Kind == expectedWarningKind);
        });
    }

    [Fact]
    public async Task ProtectedGeneratedRenderers_AllModes_SurfaceAuthorizationGating() {
        var evaluator = new FixedAuthorizationEvaluator(CommandAuthorizationDecision.Pending("corr-pending"));
        RegisterAuthorization(evaluator);
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedOneFieldInlineCommandRenderer> inline = Render<ProtectedOneFieldInlineCommandRenderer>();
        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> compact = Render<ProtectedTwoFieldCompactCommandRenderer>();
        IRenderedComponent<ProtectedFiveFieldFullPageCommandRenderer> fullPage = Render<ProtectedFiveFieldFullPageCommandRenderer>();

        inline.WaitForAssertion(() => inline.Markup.ShouldContain("Checking permission", Case.Insensitive));
        compact.WaitForAssertion(() => compact.Markup.ShouldContain("Checking permission", Case.Insensitive));
        fullPage.WaitForAssertion(() => fullPage.Markup.ShouldContain("Checking permission", Case.Insensitive));
        evaluator.Requests.Select(r => r.SourceSurface).ShouldContain(CommandAuthorizationSurface.InlineAction);
        evaluator.Requests.Select(r => r.SourceSurface).ShouldContain(CommandAuthorizationSurface.CompactInlineAction);
        evaluator.Requests.Select(r => r.SourceSurface).ShouldContain(CommandAuthorizationSurface.FullPage);
    }

    public static TheoryData<CommandAuthorizationDecision, CommandWarningKind> BlockingAuthorizationDecisions()
        => new() {
            { CommandAuthorizationDecision.Denied("corr-denied"), CommandWarningKind.Forbidden },
            { CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, "corr-failed"), CommandWarningKind.Forbidden },
            { CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, "corr-canceled"), CommandWarningKind.Forbidden },
        };

    private void RegisterAuthorization(FixedAuthorizationEvaluator evaluator) {
        Services.Replace(ServiceDescriptor.Singleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider()));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
    }

    private sealed class ControlledCommandService : ICommandServiceWithLifecycle {
        public TaskCompletionSource DispatchStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AllowAcknowledge { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => await DispatchAsync(command, onLifecycleChange: null, cancellationToken).ConfigureAwait(false);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchStarted.TrySetResult();
            await AllowAcknowledge.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", "Accepted");
        }
    }

    private sealed class RejectingCommandService : ICommandServiceWithLifecycle {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw new CommandRejectedException(
                "Order locked",
                "Please retry.",
                new CommandRejectionDetails("ORDER_LOCKED", "Concurrency", "Reload before retrying", "FC-CMD-409"));
    }

    private sealed class MappedRejectingCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] NameErrors = ["Name conflicts with current state."];

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw CommandRejectedException.FromProblem(
                "Name rejected",
                "Correct the linked field and retry.",
                new ProblemDetailsPayload(
                    "Name rejected",
                    "Correct the linked field and retry.",
                    409,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) {
                        ["Name"] = NameErrors,
                    },
                    Array.Empty<string>()));
    }

    private sealed class RetryExhaustedCommandService : ICommandServiceWithLifecycle {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw new CommandWarningException(
                CommandWarningKind.RetryableDispatchFailed,
                new ProblemDetailsPayload(
                    "Command was not accepted",
                    "EventStore did not accept the command after retrying a transient dispatch failure. Review the current data and submit again when ready.",
                    null,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                    Array.Empty<string>()),
                TimeSpan.FromSeconds(1));
    }

    private sealed class RecordingCommandService : ICommandServiceWithLifecycle {
        public const string MessageId = "01CRZ3NDEKTSV4RRFFQ69G5FAV";

        public int DispatchCount { get; private set; }

        public ProtectedTwoFieldCompactCommand? LastCommand { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            if (command is ProtectedTwoFieldCompactCommand typed) {
                LastCommand = typed;
            }

            return Task.FromResult(new CommandResult(MessageId, "Accepted"));
        }
    }

    private sealed class BlockingCommandService : ICommandServiceWithLifecycle {
        public TaskCompletionSource DispatchStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AllowDispatch { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            DispatchStarted.TrySetResult();
            await AllowDispatch.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", "Accepted");
        }
    }

    private sealed class SequencedCommandService : ICommandServiceWithLifecycle {
        public const string FirstMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        public const string SecondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";

        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            string messageId = DispatchCount == 1 ? FirstMessageId : SecondMessageId;
            return Task.FromResult(new CommandResult(messageId, "Accepted"));
        }
    }

    private sealed class FixedAuthorizationEvaluator(CommandAuthorizationDecision decision) : ICommandAuthorizationEvaluator {
        public List<CommandAuthorizationRequest> Requests { get; } = [];

        public Task<CommandAuthorizationDecision> EvaluateAsync(
            CommandAuthorizationRequest request,
            CancellationToken cancellationToken = default) {
            Requests.Add(request);
            return Task.FromResult(decision);
        }
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider {
        private static readonly AuthenticationState State = new(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test")));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(State);
    }

    private sealed class TestValidatedScope(Func<(string TenantId, string UserId)?> current) : IValidatedPendingScope {
        public (string TenantId, string UserId)? Current() => current();
    }
}
