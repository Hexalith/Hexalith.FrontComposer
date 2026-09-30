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
using Microsoft.Extensions.Time.Testing;

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
            legend.TextContent.Trim().ShouldBe("Change details");
            group.GetAttribute("aria-labelledby").ShouldBe(legend.Id);
            // BH3-09 — the group is Fluent-styled: Fluent 2 tokens and a FluentText legend.
            group.GetAttribute("style").ShouldNotBeNull().ShouldContain("var(--colorNeutralStroke2)");
            _ = legend.QuerySelector("fluent-text").ShouldNotBeNull();

            AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> fields = group.QuerySelectorAll("[data-fc-validation-field='true']");
            fields.Length.ShouldBe(2);
            fields[0].GetAttribute("name").ShouldBe("RecordId");
            fields[1].GetAttribute("name").ShouldBe("Reason");

            foreach (AngleSharp.Dom.IElement field in fields) {
                // Descriptions render once, through the editor's own Fluent field message slot.
                AngleSharp.Dom.IElement description = cut.FindAll("#" + field.Id + "-description").ShouldHaveSingleItem();
                description.Closest("[slot='message']").ShouldNotBeNull();
                description.Closest("fluent-field").ShouldBe(field.Closest("fluent-field"));
                field.GetAttribute("data-fc-invalid").ShouldBe("false");
                field.HasAttribute("aria-describedby").ShouldBeFalse();
                field.HasAttribute("aria-invalid").ShouldBeFalse();
            }

            cut.FindAll(".fc-command-field-message").ShouldBeEmpty();
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
        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new GroupedFieldsCommand {
                RecordId = string.Empty,
                Reason = "kept reason",
            }));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> summaries = cut.FindAll("[data-testid='fc-validation-summary']");
            summaries.Count.ShouldBe(1);
            summaries[0].HasAttribute("aria-live").ShouldBeFalse();
            summaries[0].GetAttribute("role").ShouldNotBe("alert");
            AngleSharp.Dom.IElement link = cut.Find("[data-fc-validation-target]");
            link.GetAttribute("data-fc-validation-target").ShouldNotBeNull().ShouldEndWith("-RecordId");
            link.TextContent.ShouldContain("Record ID");
            cut.Markup.ShouldContain("value=\"kept reason\"", Case.Insensitive);
            // VG10-02 — a described field shows its error exactly once, in Fluent's error styling,
            // and its declared description still renders beside it.
            AngleSharp.Dom.IElement recordField = cut.Find("[name='RecordId']").Closest("fluent-field").ShouldNotBeNull();
            recordField.QuerySelectorAll(".fluent-validation-message").Length.ShouldBe(1);
            System.Text.RegularExpressions.Regex.Count(recordField.TextContent, "The Record ID field is required\\.").ShouldBe(1);
            recordField.QuerySelector("[id$='-RecordId-description']").ShouldNotBeNull()
                .TextContent.Trim().ShouldBe("Record to change.");
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
            // VG5-01 — the generic AM-14 rejection bar and its recovery actions are absent, not merely silent.
            cut.FindAll("[data-testid='fc-rejected']").ShouldBeEmpty();
            cut.FindAll("[data-testid='fc-rejection-edit-retry']").ShouldBeEmpty();
            _ = cut.Find("[data-testid='fc-rejected-mapped']");
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
        IRenderedComponent<GroupedFieldsCommandForm> invalid = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new GroupedFieldsCommand { RecordId = string.Empty, Reason = "blocked reason" }));

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        int validationFocusCount = FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome");
        invalid.Find("form").Submit();

        invalid.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            invalid.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
            // ECH-05 — no validation ran: no field error was added and the entered value is kept.
            invalid.Markup.ShouldNotContain("field is required");
            invalid.Markup.ShouldContain("blocked reason");
            invalid.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
            warnings.Count.ShouldBe(1);
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome")
                .ShouldBe(validationFocusCount);
            AttemptedFocusRequests().ShouldBe(1);
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
            blocked.Find("[data-testid='fc-command-blocked-status']").TextContent.ShouldBe(BlockedMessage);
            blocked.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
            service.DispatchCount.ShouldBe(1);
        });

        service.AllowDispatch.SetResult();
    }

    [Fact]
    public async Task GeneratedFormViewActiveCommandWithdrawsInertActionAndReturnsFocusToAttemptedSubmit() {
        _ = FcFocusModule.Setup<bool>("focusActiveLifecycle", _ => true).SetResult(false);
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
        blocked.WaitForAssertion(() => _ = blocked.Find("[data-testid='fc-view-active-command']"));
        int attemptedFocusCount = AttemptedFocusRequests();

        blocked.Find("[data-testid='fc-view-active-command']").Click();

        // ECH-06 / BH-11 — an inert action is withdrawn and focus returns to the attempted submit.
        blocked.WaitForAssertion(() => {
            blocked.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
            blocked.Find("[data-testid='fc-command-blocked-status']").TextContent.ShouldNotBeEmpty();
            AttemptedFocusRequests().ShouldBe(attemptedFocusCount + 1);
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

        // AM-26 / FM-01 — the initial presentation denial replaces the controls without moving focus.
        cut.WaitForAssertion(() => _ = cut.Find("[id$='-authorization-heading']"));
        HeadingFocusRequests().ShouldBe(0);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(0);
            pending.Snapshot().ShouldBeEmpty();
            state.Value.State.ShouldBe(CommandLifecycleState.Idle);
            // The denial now answers the operator's submit, so its heading takes focus exactly once.
            HeadingFocusRequests().ShouldBe(1);
            cut.FindAll("input").ShouldBeEmpty();
            cut.FindAll("fluent-button").ShouldBeEmpty();
            AngleSharp.Dom.IElement heading = cut.Find("[id$='-authorization-heading']");
            heading.GetAttribute("tabindex").ShouldBe("-1");
            cut.Find("section[aria-labelledby]").HasAttribute("aria-live").ShouldBeFalse();
            warnings.ShouldNotBeEmpty();
            warnings.ShouldContain(w => w.Kind == expectedWarningKind);
        });

        int HeadingFocusRequests() => FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusElementById"
            && invocation.Arguments.Count > 0
            && invocation.Arguments[0]?.ToString()?.EndsWith("-authorization-heading", StringComparison.Ordinal) == true);
    }

    [Theory]
    [MemberData(nameof(TransientAuthorizationFailures))]
    public async Task ProtectedGeneratedFormTransientAuthorizationFailureKeepsInputsAndRetryWarning(CommandAuthorizationReason reason) {
        var evaluator = new MutableAuthorizationEvaluator(CommandAuthorizationDecision.Blocked(reason, "corr-transient"));
        RecordingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand { Name = "kept name", Amount = 9 }));

        cut.WaitForAssertion(() => _ = cut.Find("fluent-message-bar"));
        cut.Find("form").Submit();

        // BH2-05 / BH3-10 — only a genuine denial replaces the form; a transient failure, including a
        // check still pending at submit, keeps the entered values and the retry warning.
        cut.WaitForAssertion(() => {
            evaluator.Requests.Count.ShouldBeGreaterThanOrEqualTo(2);
            service.DispatchCount.ShouldBe(0);
            cut.FindAll("[id$='-authorization-heading']").ShouldBeEmpty();
            cut.Markup.ShouldContain("kept name");
            cut.Markup.ShouldContain("value=\"9\"");
            cut.FindAll("[data-fc-validation-field='true']").Count.ShouldBe(2);
            cut.Find("fluent-message-bar").TextContent.ShouldContain("retry", Case.Insensitive);
            cut.Find("[id$='-submit']").HasAttribute("disabled").ShouldBeFalse();
        });
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusElementById"
            && invocation.Arguments.Count > 0
            && invocation.Arguments[0]!.ToString()!.EndsWith("-authorization-heading", StringComparison.Ordinal));

        evaluator.Decision = CommandAuthorizationDecision.Allowed("corr-retry");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => service.DispatchCount.ShouldBe(1));
    }

    [Fact]
    public async Task ProtectedGeneratedFormPendingAuthorizationKeepsSubmitDisabled() {
        var evaluator = new FixedAuthorizationEvaluator(CommandAuthorizationDecision.Pending("corr-pending"));
        RegisterAuthorization(evaluator);
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand { Name = "pending name", Amount = 9 }));

        cut.WaitForAssertion(() => {
            cut.Find("fluent-message-bar").TextContent.ShouldContain("Checking permission", Case.Insensitive);
            cut.Find("[id$='-submit']").HasAttribute("disabled").ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GeneratedFormDispatchForbiddenReplacesControlsAndFocusesDeniedHeading() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new ForbiddenCommandService()));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "denied name", Amount = 3 }));

        cut.Find("form").Submit();

        // VG2-03 / VG2-04 — a service-boundary 403 replaces the form with a focused, support-safe
        // denial heading and marks no field invalid.
        cut.WaitForAssertion(() => {
            state.Value.State.ShouldBe(CommandLifecycleState.Idle);
            AngleSharp.Dom.IElement heading = cut.Find("[id$='-authorization-heading']");
            heading.GetAttribute("tabindex").ShouldBe("-1");
            // AA10-02 — the focused heading and body are the Shell's localized denial copy naming the
            // action; the server's problem title and detail never reach the denial card.
            heading.TextContent.ShouldBe("Permission required");
            AngleSharp.Dom.IElement card = cut.Find("section[data-fc-authorization-denied='true']");
            card.QuerySelector("p").ShouldNotBeNull().TextContent.ShouldStartWith("You do not have permission to ");
            card.TextContent.ShouldNotContain("Not allowed");
            card.TextContent.ShouldNotContain("You cannot run this command.");
            // The form denial card is a named group, not a region landmark, alert, or status.
            card.GetAttribute("role").ShouldBe("group");
            card.GetAttribute("aria-labelledby").ShouldBe(heading.Id);
            card.HasAttribute("aria-live").ShouldBeFalse();
            cut.FindAll("[data-fc-validation-field='true']").ShouldBeEmpty();
            cut.FindAll("[data-fc-invalid='true']").ShouldBeEmpty();
            cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusElementById"
                && Equals(invocation.Arguments[0], heading.Id)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task GeneratedFormServerValidationGlobalErrorsRenderInTheFocusedSummary() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new ServerValidationCommandService()));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "server name", Amount = 5 }));

        cut.Find("form").Submit();

        // VG2-02 — allowlisted field errors and form-level errors share the one focused summary.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            summary.TextContent.ShouldContain("The name is already taken.");
            summary.TextContent.ShouldContain("The request could not be processed.");
            cut.Find("[data-fc-validation-target]").GetAttribute("data-fc-validation-target").ShouldNotBeNull()
                .ShouldEndWith("-Name");
            cut.Find("[name='Name']").GetAttribute("data-fc-invalid").ShouldBe("true");
            cut.Find("[name='Amount']").GetAttribute("data-fc-invalid").ShouldBe("false");
            // BH3-09 — the Fluent field shows the mapped error exactly once, in Fluent's error styling.
            AngleSharp.Dom.IElement nameField = cut.Find("[name='Name']").Closest("fluent-field").ShouldNotBeNull();
            nameField.QuerySelectorAll(".fluent-validation-message").Length.ShouldBe(1);
            System.Text.RegularExpressions.Regex.Count(nameField.TextContent, "The name is already taken.").ShouldBe(1);
            cut.Markup.ShouldContain("server name");
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome").ShouldBe(1);
        });
    }

    [Fact]
    public async Task GeneratedFormUnmappedRejectionTextNeverAppearsInALaterClientSummary() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new UnmappedProblemRejectingCommandService()));
        await InitializeStoreAsync();
        GroupedFieldsCommand model = new() { RecordId = "FC-1", Reason = "first reason" };
        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, model));

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='fc-rejection-edit-retry']"));
        cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();

        model.RecordId = string.Empty;
        cut.Find("form").Submit();

        // VR-03 / VG2-10 — unmapped rejection text stays in its lifecycle region.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            summary.GetAttribute("data-fc-validation-kind").ShouldBe("client-validation");
            summary.TextContent.ShouldNotContain("Stale global rejection text.");
            summary.TextContent.ShouldNotContain("Stale rejection detail.");
            cut.FindAll("[data-fc-validation-target]").Count.ShouldBe(1);
        });
    }

    [Fact]
    public async Task GeneratedFormServerFormLevelErrorsAreResetBeforeALaterClientSummary() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new ServerValidationCommandService()));
        await InitializeStoreAsync();
        GroupedFieldsCommand model = new() { RecordId = "FC-1", Reason = "first reason" };
        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, model));

        cut.Find("form").Submit();

        // The server map names no field of this command, so both messages are form-level summary text.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            summary.TextContent.ShouldContain("The request could not be processed.");
            summary.TextContent.ShouldContain("The name is already taken.");
        });

        model.RecordId = string.Empty;
        cut.Find("form").Submit();

        // VG3-04 — the earlier attempt's form-level errors never reach this client-validation summary.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            summary.GetAttribute("data-fc-validation-kind").ShouldBe("client-validation");
            summary.TextContent.ShouldNotContain("The request could not be processed.");
            summary.TextContent.ShouldNotContain("The name is already taken.");
            cut.FindAll("[data-fc-validation-target]").Count.ShouldBe(1);
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome").ShouldBe(2);
        });
    }

    [Fact]
    public async Task GeneratedFormSameFormResubmitDuringItsOwnCommandAnnouncesEachAttemptWithoutDispatch() {
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        BlockingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "only form", Amount = 4 }));

        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        string correlationId = state.Value.CorrelationId.ShouldNotBeNull();

        // VG5-03 — the submit control is not lifecycle-disabled while its own command is in flight, so a
        // real press reaches SubmitAsync (bUnit's form.Submit() would bypass a disabled button).
        cut.WaitForAssertion(() => state.Value.State.ShouldNotBe(CommandLifecycleState.Idle));
        cut.Find("[id$='-submit']").HasAttribute("disabled").ShouldBeFalse();

        // VG2-05 — every blocked re-press of the same form clears and re-sets AM-20 once.
        for (int attempt = 1; attempt <= 2; attempt++) {
            cut.Find("[id$='-submit']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("form").Submit();
            cut.WaitForAssertion(() => BlockedStatus(cut).ShouldBeEmpty());
            await AdvanceUntilAsync(time, () => BlockedStatus(cut).Length > 0);
            BlockedStatus(cut).ShouldBe(BlockedMessage);
        }

        service.DispatchCount.ShouldBe(1);
        state.Value.CorrelationId.ShouldBe(correlationId);
        state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        cut.FindAll("[data-testid='fc-command-blocked-status']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
        cut.Markup.ShouldContain("only form");
        AttemptedFocusRequests().ShouldBe(2);

        service.AllowDispatch.SetResult();

        static string BlockedStatus(IRenderedComponent<TwoFieldCompactCommandForm> component)
            => component.Find("[data-testid='fc-command-blocked-status']").TextContent;
    }

    [Fact]
    public async Task GeneratedFormAdmittedLaterAttemptClearsTheEarlierBlockedOutcome() {
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        // The held first command settles as an authoritative rejection, so no accepted pending command
        // keeps the admission gate closed for the later attempt.
        HeldRejectingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<FourFieldCompactCommandForm> first = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand { Name = "first" }));
        IRenderedComponent<TwoFieldCompactCommandForm> later = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "later", Amount = 2 }));

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        later.Find("form").Submit();
        await AdvanceUntilAsync(time, () => LaterStatus().Length > 0);
        LaterStatus().ShouldBe(BlockedMessage);
        later.Find("[data-testid='fc-command-blocked-outcome']").GetAttribute("data-fc-blocked").ShouldBe("true");

        service.AllowDispatch.SetResult();
        ICommandExecutionAdmissionGate admissionGate = Services.GetRequiredService<ICommandExecutionAdmissionGate>();
        SpinWait.SpinUntil(() => {
            using CommandExecutionAdmission probe = admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("settlement-probe"));
            return probe.IsAdmitted;
        }, TimeSpan.FromSeconds(2)).ShouldBeTrue();

        later.Find("form").Submit();

        // VG3-03 — an admitted attempt withdraws the earlier AM-20 outcome before it dispatches.
        later.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(2);
            LaterStatus().ShouldBeEmpty();
            later.Find("[data-testid='fc-command-blocked-outcome']").GetAttribute("data-fc-blocked").ShouldBe("false");
        });

        string LaterStatus() => later.Find("[data-testid='fc-command-blocked-status']").TextContent;
    }

    [Fact]
    public async Task ZeroFieldInlineRendererOwnsTheBlockedOutcomeBesideItsTrigger() {
        // The held first command settles as an authoritative rejection, so the admission gate reopens
        // for the later trigger activation.
        HeldRejectingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<FourFieldCompactCommandForm> first = Render<FourFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new FourFieldCompactCommand { Name = "first" }));
        IRenderedComponent<ZeroFieldInlineCommandRenderer> renderer = Render<ZeroFieldInlineCommandRenderer>();

        first.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        renderer.WaitForAssertion(() => renderer.Find("fluent-button").HasAttribute("disabled").ShouldBeFalse());
        string triggerId = renderer.Find("fluent-button").Id.ShouldNotBeNull();
        renderer.Find("fluent-button").Click();

        // VG2-13 — the hidden zero-field form hands AM-20 to a visible node beside the trigger.
        renderer.WaitForAssertion(() => {
            AngleSharp.Dom.IElement status = renderer.Find("[data-testid='fc-command-blocked-status']");
            status.TextContent.ShouldBe(BlockedMessage);
            status.Closest("[style*='display:none']").ShouldBeNull();
            renderer.Find("[data-testid='fc-command-blocked-outcome']").TagName.ShouldBe("SPAN");
            renderer.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
            service.DispatchCount.ShouldBe(1);
            FcFocusModule.Invocations.ShouldContain(invocation => invocation.Identifier == "focusAttemptedControl"
                && Equals(invocation.Arguments[0], triggerId));
        });

        service.AllowDispatch.SetResult();
        ICommandExecutionAdmissionGate admissionGate = Services.GetRequiredService<ICommandExecutionAdmissionGate>();
        SpinWait.SpinUntil(() => {
            using CommandExecutionAdmission probe = admissionGate.TryAcquire(new CommandExecutionAdmissionRequest("settlement-probe"));
            return probe.IsAdmitted;
        }, TimeSpan.FromSeconds(2)).ShouldBeTrue();

        renderer.FindAll("fluent-button").Single(button => button.Id == triggerId).Click();

        // VG4-05 — the admitted later activation withdraws the renderer-owned AM-20 outcome.
        renderer.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(2);
            renderer.Find("[data-testid='fc-command-blocked-status']").TextContent.ShouldBeEmpty();
            renderer.Find("[data-testid='fc-command-blocked-outcome']").GetAttribute("data-fc-blocked").ShouldBe("false");
        });
    }

    [Fact]
    public async Task GeneratedFormNumericParseErrorReachesTheSummaryAndClearsOnAValidNumber() {
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "kept name", Amount = 3 }));

        cut.Find("fluent-text-input[name='Amount']").Change("not a number");
        cut.Find("form").Submit();

        // VG4-02 — the numeric parse error is an EditContext message: it is linked from the summary to
        // the rendered editor and marks that editor invalid.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement link = cut.Find("[data-testid='fc-validation-summary'] [data-fc-validation-target]");
            AngleSharp.Dom.IElement amount = cut.Find("fluent-text-input[name='Amount']");
            link.TextContent.Trim().ShouldBe("Amount: Invalid number format.");
            link.GetAttribute("data-fc-validation-target").ShouldBe(amount.Id);
            amount.GetAttribute("data-fc-invalid").ShouldBe("true");
            // VG10-02 — the split numeric binding still shows the parse error exactly once at the field.
            AngleSharp.Dom.IElement amountField = amount.Closest("fluent-field").ShouldNotBeNull();
            amountField.QuerySelectorAll(".fluent-validation-message").Length.ShouldBe(1);
            System.Text.RegularExpressions.Regex.Count(amountField.TextContent, "Invalid number format\\.").ShouldBe(1);
        });

        cut.Find("fluent-text-input[name='Amount']").Change("12");

        cut.WaitForAssertion(() => {
            cut.FindAll("[data-fc-validation-target]").ShouldBeEmpty();
            AngleSharp.Dom.IElement amount = cut.Find("fluent-text-input[name='Amount']");
            amount.GetAttribute("data-fc-invalid").ShouldBe("false");
            amount.Closest("fluent-field").ShouldNotBeNull().QuerySelectorAll(".fluent-validation-message").Length.ShouldBe(0);
        });
        cut.Markup.ShouldContain("kept name");
    }

    [Fact]
    public async Task GeneratedFormMappedRejectionListsItsFormLevelMessageBesideTheFieldLink() {
        const string formLevelError = "The order is locked by another operator.";
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new MappedRejectingCommandService(formLevelError)));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "preserved name", Amount = 7 }));

        cut.Find("form").Submit();

        // VG4-03 — a mapped rejection keeps its form-level message as an unlinked entry beside the
        // linked field error, and both count toward the summary total.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
            AngleSharp.Dom.IElement[] entries = [.. summary.QuerySelectorAll("li")];
            entries.Length.ShouldBe(2);
            entries[0].QuerySelector("a[data-fc-validation-target]").ShouldNotBeNull()
                .GetAttribute("data-fc-validation-target").ShouldNotBeNull().ShouldEndWith("-Name");
            entries[1].QuerySelector("a").ShouldBeNull();
            entries[1].TextContent.Trim().ShouldBe(formLevelError);
            summary.TextContent.ShouldContain("2 errors.");
        });

        cut.Find("fluent-text-input[name='Name']").Change("edited name");

        // Editing the mapped field clears only that rejection entry immediately; the form-level
        // outcome remains visible and the field is no longer exposed as invalid before resubmission.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
            AngleSharp.Dom.IElement[] entries = [.. summary.QuerySelectorAll("li")];
            entries.Length.ShouldBe(1);
            entries[0].TextContent.Trim().ShouldBe(formLevelError);
            summary.QuerySelectorAll("[data-fc-validation-target]").ShouldBeEmpty();
            cut.Find("[name='Name']").GetAttribute("data-fc-invalid").ShouldBe("false");
        });
    }

    [Fact]
    public async Task GeneratedFormUnchangedRetryAfterAMappedRejectionDispatchesAgain() {
        MappedThenAcceptingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "unchanged name", Amount = 7 }));

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => {
            _ = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
            cut.Find("[name='Name']").GetAttribute("data-fc-invalid").ShouldBe("true");
        });

        cut.Find("form").Submit();

        // VG5-O1 / E5-06 — the mapped messages answered the previous attempt only: an unchanged retry
        // is admitted, dispatches, and is never relabeled as client validation (VR-02 "retry").
        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(2);
            cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
            cut.Find("[name='Name']").GetAttribute("data-fc-invalid").ShouldBe("false");
        });
        cut.Markup.ShouldNotContain("Validation failed");
        cut.Markup.ShouldContain("unchanged name");
    }

    [Fact]
    public async Task GeneratedFormServerValidationStillBlocksAnUnchangedRetry() {
        RecordingServerValidationCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "server name", Amount = 5 }));

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-validation-summary']").TextContent.ShouldContain("The name is already taken."));

        cut.Find("form").Submit();

        // The 400 validation store is unchanged by the mapped-rejection store: its field message still
        // blocks an unchanged retry until the operator edits the field.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            summary.TextContent.ShouldContain("The name is already taken.");
            FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusValidationOutcome").ShouldBe(2);
        });
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task GeneratedFormMessagesOnHiddenFieldsRenderAsUnlinkedSummaryEntries() {
        await InitializeStoreAsync();
        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new GroupedFieldsCommand { RecordId = string.Empty, Reason = "kept reason" })
            .Add(p => p.ShowFieldsOnly, new[] { nameof(GroupedFieldsCommand.Reason) }));
        cut.FindAll("[name='RecordId']").ShouldBeEmpty();

        cut.Find("form").Submit();

        // BH5-05 — RecordId is not rendered on this surface, so its error is listed without a link to an
        // absent control, and it still counts toward the summary total.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement summary = cut.Find("[data-testid='fc-validation-summary']");
            AngleSharp.Dom.IElement entry = summary.QuerySelectorAll("li").ShouldHaveSingleItem();
            entry.QuerySelector("a").ShouldBeNull();
            entry.TextContent.ShouldContain("Record ID");
            summary.QuerySelectorAll("[data-fc-validation-target]").ShouldBeEmpty();
            summary.TextContent.ShouldContain("One error.");
        });
        cut.Markup.ShouldContain("value=\"kept reason\"", Case.Insensitive);
    }

    [Fact]
    public async Task GeneratedFormRejectionMappedOnlyToAHiddenFieldTakesTheUnmappedRecoveryPath() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new MappedRejectingCommandService()));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "hidden name", Amount = 7 })
            .Add(p => p.ShowFieldsOnly, new[] { nameof(TwoFieldCompactCommand.Amount) }));

        cut.Find("form").Submit();

        // AA7-01 — Name is not rendered on this surface, so the operator can neither see nor edit the only
        // mapped field. The rejection takes the unmapped path: the generic rejection bar with its AM-14
        // phase and every recovery action, and no summary naming the hidden field.
        cut.WaitForAssertion(() => {
            state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
            state.Value.HasMappedFieldErrors.ShouldBeFalse();
            _ = cut.Find("[data-fc-phase='rejected']");
            _ = cut.Find("[data-testid='fc-rejected']");
            _ = cut.Find("[data-testid='fc-rejection-edit-retry']");
            _ = cut.Find("[data-testid='fc-rejection-return']");
            cut.FindAll("[data-testid='fc-rejected-mapped']").ShouldBeEmpty();
            cut.FindAll("[data-testid='fc-validation-summary']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("Name conflicts with current state.");
            cut.Markup.ShouldContain("value=\"7\"", Case.Insensitive);
        });
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusValidationOutcome");
    }

    [Fact]
    public async Task GeneratedFormRejectionMappedToVisibleAndHiddenFieldsStaysMapped() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new VisibleAndHiddenMappedRejectingCommandService()));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state = Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new TwoFieldCompactCommand { Name = "hidden name", Amount = 7 })
            .Add(p => p.ShowFieldsOnly, new[] { nameof(TwoFieldCompactCommand.Amount) }));

        cut.Find("form").Submit();

        // AA7-01 / BH5-05 — a rendered field received a message, so the rejection stays mapped: the
        // visible field is linked, and the hidden field's message stays readable as an unlinked entry.
        cut.WaitForAssertion(() => {
            state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
            state.Value.HasMappedFieldErrors.ShouldBeTrue();
            AngleSharp.Dom.IElement summary = cut.Find("[data-fc-validation-kind='mapped-server-rejection']");
            AngleSharp.Dom.IElement[] entries = [.. summary.QuerySelectorAll("li")];
            entries.Length.ShouldBe(2);
            AngleSharp.Dom.IElement link = entries[0].QuerySelector("a").ShouldNotBeNull();
            link.GetAttribute("data-fc-validation-target").ShouldNotBeNull().ShouldEndWith("-Amount");
            link.TextContent.ShouldContain("Amount exceeds the allowed limit.");
            entries[1].QuerySelector("a").ShouldBeNull();
            entries[1].TextContent.Trim().ShouldBe("Name conflicts with current state.");
            cut.FindAll("[data-testid='fc-rejected']").ShouldBeEmpty();
            _ = cut.Find("[data-testid='fc-rejected-mapped']");
        });
    }

    [Fact]
    public async Task ProtectedRendererPendingRefreshThenDenialKeepsTheArmedHeadingFocus() {
        MutableAuthorizationEvaluator evaluator = new(CommandAuthorizationDecision.Allowed("corr-allowed"));
        NotifyingAuthenticationStateProvider authentication = new();
        Services.Replace(ServiceDescriptor.Singleton<AuthenticationStateProvider>(authentication));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> cut = Render<ProtectedTwoFieldCompactCommandRenderer>();
        cut.WaitForAssertion(() => _ = cut.Find("form"));

        evaluator.Decision = CommandAuthorizationDecision.Pending("corr-pending");
        await cut.InvokeAsync(authentication.Notify);

        // E5-24 / AA5-03 — the pending check replaces the form with its checking card, which is not a denial.
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Checking permission", Case.Insensitive);
            cut.FindAll("[data-fc-authorization-denied]").ShouldBeEmpty();
        });
        FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "captureFocusBeforeReplacement").ShouldBe(1);

        evaluator.Decision = CommandAuthorizationDecision.Denied("corr-revoked");

        // E5-08 — the scheduled retry carries the armed replacement, so the final denial of the form the
        // operator was using still reaches the capture-then-verify heading focus. The focus request runs
        // after the denial's last render, so it is polled rather than awaited through a render.
        string headingId = string.Empty;
        cut.WaitForAssertion(
            () => headingId = cut.Find("section[data-fc-authorization-denied='true'] h2[tabindex='-1']").Id.ShouldNotBeNull(),
            TimeSpan.FromSeconds(5));
        SpinWait.SpinUntil(
            () => FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "focusReplacementHeading"),
            TimeSpan.FromSeconds(5)).ShouldBeTrue();
        FcFocusModule.Invocations.Single(invocation => invocation.Identifier == "focusReplacementHeading")
            .Arguments.ShouldBe([headingId, headingId]);
        FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "captureFocusBeforeReplacement").ShouldBe(1);
    }

    [Fact]
    public async Task SameGeneratedFormAndProtectedRendererInstancesUseDistinctIds() {
        RegisterAuthorization(new FixedAuthorizationEvaluator(CommandAuthorizationDecision.Denied("corr-denied")));
        await InitializeStoreAsync();
        IRenderedComponent<GroupedFieldsCommandForm> firstForm = Render<GroupedFieldsCommandForm>();
        IRenderedComponent<GroupedFieldsCommandForm> secondForm = Render<GroupedFieldsCommandForm>();
        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> firstRenderer = Render<ProtectedTwoFieldCompactCommandRenderer>();
        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> secondRenderer = Render<ProtectedTwoFieldCompactCommandRenderer>();

        firstForm.Find("form").Submit();
        secondForm.Find("form").Submit();

        // VG4-08 — every instance owns its DOM ids, so links, focus and descriptions never resolve
        // into a sibling instance of the same command.
        string firstSummary = string.Empty;
        string secondSummary = string.Empty;
        firstForm.WaitForAssertion(() => firstSummary = firstForm.Find("[data-testid='fc-validation-summary']").Id.ShouldNotBeNull());
        secondForm.WaitForAssertion(() => secondSummary = secondForm.Find("[data-testid='fc-validation-summary']").Id.ShouldNotBeNull());
        string firstFormId = firstForm.Find("[data-fc-command-form='true']").Id.ShouldNotBeNull();
        string secondFormId = secondForm.Find("[data-fc-command-form='true']").Id.ShouldNotBeNull();
        firstFormId.ShouldNotBe(secondFormId);
        firstSummary.ShouldNotBe(secondSummary);
        firstForm.Find("[data-fc-validation-target]").GetAttribute("data-fc-validation-target")
            .ShouldNotBe(secondForm.Find("[data-fc-validation-target]").GetAttribute("data-fc-validation-target"));

        string firstHeading = string.Empty;
        string secondHeading = string.Empty;
        firstRenderer.WaitForAssertion(() => firstHeading = firstRenderer.Find("section[aria-labelledby] h2[tabindex='-1']").Id.ShouldNotBeNull());
        secondRenderer.WaitForAssertion(() => secondHeading = secondRenderer.Find("section[aria-labelledby] h2[tabindex='-1']").Id.ShouldNotBeNull());
        firstHeading.ShouldNotBe(secondHeading);
    }

    [Fact]
    public async Task GeneratedSwitchDatePickerAndEnumSelectHonorTheFieldContract() {
        // The Fluent date picker's calendar and the Fluent select initialize through their own interop.
        JSInterop.SetupModule("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DateTime/FluentCalendar.razor.js").Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("Microsoft.FluentUI.Blazor.Components.Select.Initialize", _ => true).SetVoidResult();
        await InitializeStoreAsync();
        IRenderedComponent<FieldContractEditorsCommandForm> cut = Render<FieldContractEditorsCommandForm>();
        string formId = cut.Find("[data-fc-command-form='true']").Id.ShouldNotBeNull();

        // VG7-02 — a null nullable enum selects no option instead of showing the zero member.
        AngleSharp.Dom.IElement escalationSelect = cut.Find("[name='" + nameof(FieldContractEditorsCommand.EscalationPriority) + "']");
        escalationSelect.QuerySelectorAll("fluent-option").Length.ShouldBe(3);
        escalationSelect.QuerySelectorAll("fluent-option[selected]").ShouldBeEmpty();

        // VG4-10 — non-text editors carry the same rendered field contract as text inputs.
        foreach (string property in new[] { nameof(FieldContractEditorsCommand.DueDate), nameof(FieldContractEditorsCommand.Priority), nameof(FieldContractEditorsCommand.Urgent) }) {
            AngleSharp.Dom.IElement editor = cut.FindAll("[data-fc-validation-field='true']")
                .Where(element => element.Id == formId + "-" + property)
                .ShouldHaveSingleItem();
            editor.GetAttribute("data-fc-invalid").ShouldBe("false");
        }

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            string dateId = formId + "-" + nameof(FieldContractEditorsCommand.DueDate);
            AngleSharp.Dom.IElement date = cut.FindAll("[data-fc-validation-field='true']").Single(element => element.Id == dateId);
            date.GetAttribute("data-fc-invalid").ShouldBe("true");
            AngleSharp.Dom.IElement link = cut.Find("[data-testid='fc-validation-summary'] [data-fc-validation-target]");
            link.GetAttribute("data-fc-validation-target").ShouldBe(dateId);
            link.TextContent.ShouldContain("Due date");

            // VG7-02 — the required nullable enum's own Fluent field renders its validation message once,
            // because the select is bound to the nullable model property rather than a proxy.
            string escalationId = formId + "-" + nameof(FieldContractEditorsCommand.EscalationPriority);
            AngleSharp.Dom.IElement escalation = cut.FindAll("[data-fc-validation-field='true']").Single(element => element.Id == escalationId);
            escalation.GetAttribute("data-fc-invalid").ShouldBe("true");
            AngleSharp.Dom.IElement escalationField = escalation.Closest("fluent-field").ShouldNotBeNull();
            escalationField.QuerySelectorAll(".fluent-validation-message").ShouldHaveSingleItem()
                .TextContent.Trim().ShouldBe("The Escalation priority field is required.");
        });
    }

    [Fact]
    public async Task ProtectedRendererInitialDenialNeverMovesFocus() {
        var evaluator = new FixedAuthorizationEvaluator(CommandAuthorizationDecision.Denied("corr-initial"));
        RegisterAuthorization(evaluator);
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> cut = Render<ProtectedTwoFieldCompactCommandRenderer>();

        // BH2-04 — an initially denied renderer renders its replacement silently (FM-01).
        cut.WaitForAssertion(() => _ = cut.Find("section[aria-labelledby] h2[tabindex='-1']"));
        // The renderer denial card is a named group, not a region landmark per denied row.
        AngleSharp.Dom.IElement card = cut.Find("section[data-fc-authorization-denied='true']");
        card.GetAttribute("role").ShouldBe("group");
        card.HasAttribute("aria-live").ShouldBeFalse();
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "captureFocusBeforeReplacement");
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusReplacementHeading");
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusOverlayEntry");
    }

    [Fact]
    public async Task ProtectedRendererBackgroundDenialFocusesHeadingOnlyThroughReplacementCheck() {
        MutableAuthorizationEvaluator evaluator = new(CommandAuthorizationDecision.Allowed("corr-allowed"));
        NotifyingAuthenticationStateProvider authentication = new();
        Services.Replace(ServiceDescriptor.Singleton<AuthenticationStateProvider>(authentication));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandRenderer> cut = Render<ProtectedTwoFieldCompactCommandRenderer>();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "captureFocusBeforeReplacement");

        evaluator.Decision = CommandAuthorizationDecision.Denied("corr-revoked");
        await cut.InvokeAsync(authentication.Notify);

        // A background refresh may focus the heading only through the replacement check, which
        // verifies that the replaced form held focus (AM-26).
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement heading = cut.Find("section[aria-labelledby] h2[tabindex='-1']");
            JSRuntimeInvocation capture = FcFocusModule.Invocations.Single(invocation => invocation.Identifier == "captureFocusBeforeReplacement"
                && Equals(invocation.Arguments[0], heading.Id));
            JSRuntimeInvocation focus = FcFocusModule.Invocations.Single(invocation => invocation.Identifier == "focusReplacementHeading"
                && Equals(invocation.Arguments[0], heading.Id));
            capture.Arguments[0].ShouldBe(heading.Id);
            focus.Arguments.ShouldBe([heading.Id, heading.Id]);
        });
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusElementById"
            && invocation.Arguments[0]!.ToString()!.Contains("authorization", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProtectedFormBackgroundDenialFocusesHeadingOnlyThroughReplacementCheck() {
        MutableAuthorizationEvaluator evaluator = new(CommandAuthorizationDecision.Allowed("corr-allowed"));
        NotifyingAuthenticationStateProvider authentication = new();
        Services.Replace(ServiceDescriptor.Singleton<AuthenticationStateProvider>(authentication));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand { Name = "standalone", Amount = 1 }));
        cut.WaitForAssertion(() => cut.FindAll("[data-fc-validation-field='true']").Count.ShouldBe(2));
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "captureFocusBeforeReplacement");

        evaluator.Decision = CommandAuthorizationDecision.Denied("corr-revoked");
        await cut.InvokeAsync(authentication.Notify);

        // VG3-02 / BH3-03 — a standalone form replaced by a background denial focuses its heading only
        // through the capture-then-verify replacement check, never directly.
        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement heading = cut.Find("[id$='-authorization-heading']");
            JSRuntimeInvocation capture = FcFocusModule.Invocations.Single(invocation => invocation.Identifier == "captureFocusBeforeReplacement");
            capture.Arguments[0].ShouldBe(heading.Id);
            JSRuntimeInvocation focus = FcFocusModule.Invocations.Single(invocation => invocation.Identifier == "focusReplacementHeading");
            focus.Arguments.ShouldBe([heading.Id, heading.Id]);
            cut.FindAll("[data-fc-validation-field='true']").ShouldBeEmpty();
        });
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "focusElementById"
            && invocation.Arguments[0]!.ToString()!.EndsWith("-authorization-heading", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ClearedAuthorizationDenials))]
    public async Task ProtectedFormReturnsWithItsEnteredValuesWhenADenialClears(
        CommandAuthorizationDecision block,
        CommandAuthorizationDecision restore,
        string? expectedWarning) {
        MutableAuthorizationEvaluator evaluator = new(CommandAuthorizationDecision.Allowed("corr-allowed"));
        NotifyingAuthenticationStateProvider authentication = new();
        Services.Replace(ServiceDescriptor.Singleton<AuthenticationStateProvider>(authentication));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => evaluator));
        await InitializeStoreAsync();

        IRenderedComponent<ProtectedTwoFieldCompactCommandForm> cut = Render<ProtectedTwoFieldCompactCommandForm>(parameters => parameters
            .Add(p => p.InitialValue, new ProtectedTwoFieldCompactCommand { Name = "initial name", Amount = 4 }));
        cut.WaitForAssertion(() => cut.FindAll("[data-fc-validation-field='true']").Count.ShouldBe(2));
        cut.Find("fluent-text-input[name='Name']").Change("edited name");

        evaluator.Decision = block;
        await cut.InvokeAsync(authentication.Notify);
        cut.WaitForAssertion(() => {
            _ = cut.Find("section[data-fc-authorization-denied='true'] [id$='-authorization-heading']");
            cut.FindAll("[data-fc-validation-field='true']").ShouldBeEmpty();
        });

        evaluator.Decision = restore;
        await cut.InvokeAsync(authentication.Notify);

        // VG7-01 — a denial or sign-in requirement that clears gives the operator the form back, with the
        // values entered before the denial, instead of a permanent "Permission required" card.
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-fc-authorization-denied]").ShouldBeEmpty();
            cut.FindAll("[id$='-authorization-heading']").ShouldBeEmpty();
            IReadOnlyList<AngleSharp.Dom.IElement> editors = cut.FindAll("[data-fc-validation-field='true']");
            editors.Count.ShouldBe(2);
            editors[0].GetAttribute("name").ShouldBe("Name");
            editors[0].GetAttribute("value").ShouldBe("edited name");
            editors[1].GetAttribute("value").ShouldBe("4");
            if (expectedWarning is null) {
                cut.FindAll("fluent-message-bar").ShouldBeEmpty();
            }
            else {
                cut.Find("fluent-message-bar").TextContent.ShouldContain(expectedWarning, Case.Insensitive);
            }
        });
    }

    [Fact]
    public async Task GeneratedFormOmitsADeclaredGroupWhenEveryMemberIsHidden() {
        await InitializeStoreAsync();

        IRenderedComponent<GroupedFieldsCommandForm> cut = Render<GroupedFieldsCommandForm>(parameters => parameters
            .Add(p => p.ShowFieldsOnly, new[] { nameof(GroupedFieldsCommand.MessageId) }));

        // VG7-03 / VG4-09 — ShowFieldsOnly names neither group member, so no empty "Change details"
        // fieldset renders without fields.
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.FindAll("fieldset[data-fc-field-group]").ShouldBeEmpty();
        cut.FindAll("[data-fc-validation-field='true']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Change details");
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
            { CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Unauthenticated, "corr-unauthenticated"), CommandWarningKind.Forbidden },
        };

    public static TheoryData<CommandAuthorizationDecision, CommandAuthorizationDecision, string?> ClearedAuthorizationDenials()
        => new() {
            { CommandAuthorizationDecision.Denied("corr-denied"), CommandAuthorizationDecision.Allowed("corr-restored"), null },
            { CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Unauthenticated, "corr-unauthenticated"), CommandAuthorizationDecision.Allowed("corr-restored"), null },
            { CommandAuthorizationDecision.Denied("corr-denied"), CommandAuthorizationDecision.Pending("corr-pending"), "Checking permission" },
        };

    public static TheoryData<CommandAuthorizationReason> TransientAuthorizationFailures()
        => new() {
            CommandAuthorizationReason.MissingService,
            CommandAuthorizationReason.MissingPolicy,
            CommandAuthorizationReason.StaleTenantContext,
            CommandAuthorizationReason.HandlerFailed,
            CommandAuthorizationReason.Canceled,
            CommandAuthorizationReason.CatalogInconsistent,
            CommandAuthorizationReason.Pending,
        };

    private const string BlockedMessage = "This command did not run. Another command is already in progress.";

    private int AttemptedFocusRequests() => FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "focusAttemptedControl"
        && invocation.Arguments.Count > 0
        && invocation.Arguments[0]?.ToString()?.EndsWith("-submit", StringComparison.Ordinal) == true);

    private static async Task AdvanceUntilAsync(FakeTimeProvider time, Func<bool> condition) {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline) {
            time.Advance(TimeSpan.FromMilliseconds(50));
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

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

    private sealed class MappedRejectingCommandService(params string[] globalErrors) : ICommandServiceWithLifecycle {
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
                    globalErrors));
    }

    private sealed class VisibleAndHiddenMappedRejectingCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] NameErrors = ["Name conflicts with current state."];
        private static readonly string[] AmountErrors = ["Amount exceeds the allowed limit."];

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw CommandRejectedException.FromProblem(
                "Change rejected",
                "Correct the linked fields and retry.",
                new ProblemDetailsPayload(
                    "Change rejected",
                    "Correct the linked fields and retry.",
                    409,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) {
                        ["Name"] = NameErrors,
                        ["Amount"] = AmountErrors,
                    },
                    Array.Empty<string>()));
    }

    private sealed class MappedThenAcceptingCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] NameErrors = ["Name conflicts with current state."];

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
            if (DispatchCount == 1) {
                throw CommandRejectedException.FromProblem(
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

            return Task.FromResult(new CommandResult("01DRZ3NDEKTSV4RRFFQ69G5FAV", "Accepted"));
        }
    }

    private sealed class RecordingServerValidationCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] NameErrors = ["The name is already taken."];

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
            throw new CommandValidationException(new ProblemDetailsPayload(
                "Validation failed",
                null,
                400,
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) {
                    ["Name"] = NameErrors,
                },
                Array.Empty<string>()));
        }
    }

    private sealed class ForbiddenCommandService : ICommandServiceWithLifecycle {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw new CommandWarningException(
                CommandWarningKind.Forbidden,
                new ProblemDetailsPayload(
                    "Not allowed",
                    "You cannot run this command.",
                    403,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                    Array.Empty<string>()));
    }

    private sealed class ServerValidationCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] NameErrors = ["The name is already taken."];
        private static readonly string[] GlobalErrors = ["The request could not be processed."];

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw new CommandValidationException(new ProblemDetailsPayload(
                "Validation failed",
                null,
                400,
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) {
                    ["Name"] = NameErrors,
                },
                GlobalErrors));
    }

    private sealed class UnmappedProblemRejectingCommandService : ICommandServiceWithLifecycle {
        private static readonly string[] GlobalErrors = ["Stale global rejection text."];

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class
            => throw CommandRejectedException.FromProblem(
                "Record locked",
                "Reload the record and retry.",
                new ProblemDetailsPayload(
                    "Record locked",
                    "Stale rejection detail.",
                    409,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                    GlobalErrors));
    }

    private sealed class MutableAuthorizationEvaluator(CommandAuthorizationDecision decision) : ICommandAuthorizationEvaluator {
        public CommandAuthorizationDecision Decision { get; set; } = decision;

        public List<CommandAuthorizationRequest> Requests { get; } = [];

        public Task<CommandAuthorizationDecision> EvaluateAsync(
            CommandAuthorizationRequest request,
            CancellationToken cancellationToken = default) {
            Requests.Add(request);
            return Task.FromResult(Decision);
        }
    }

    private sealed class NotifyingAuthenticationStateProvider : AuthenticationStateProvider {
        private static readonly AuthenticationState State = new(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test")));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(State);

        public void Notify() => NotifyAuthenticationStateChanged(Task.FromResult(State));
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

    private sealed class HeldRejectingCommandService : ICommandServiceWithLifecycle {
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
            throw new CommandRejectedException("Record locked", "Reload the record and retry.");
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
