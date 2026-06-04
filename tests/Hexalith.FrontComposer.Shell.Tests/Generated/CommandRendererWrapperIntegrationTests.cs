using System.Security.Claims;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-4 Task 5.6 — the generated renderer output of every density must include the
/// <c>fc-lifecycle-wrapper</c> marker now that <see cref="CommandFormEmitter"/> wraps its
/// emitted <c>&lt;EditForm&gt;</c> in <c>&lt;FcLifecycleWrapper&gt;</c> (Task 4.1).
/// </summary>
public sealed class CommandRendererWrapperIntegrationTests : CommandRendererTestBase {
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
        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

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
            second.Markup.ShouldContain("Command already in progress", Case.Insensitive);
            second.Markup.ShouldContain("blocked name");
            warnings.Count.ShouldBe(1);
            warnings[0].Detail.ShouldNotBeNull().ShouldNotContain("queued", Case.Insensitive);
        });

        service.AllowDispatch.SetResult();
        first.WaitForAssertion(() => pending.Snapshot().Count.ShouldBe(1));
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
            second.Markup.ShouldContain("Command already in progress", Case.Insensitive);
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
    public async Task ProtectedGeneratedForm_BlockedAuthorization_PreservesInputAndBlocksSideEffects(
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
            cut.Markup.ShouldContain("blocked name");
            cut.Markup.ShouldContain("value=\"9\"", Case.Insensitive);
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
            where TCommand : class {
            return await DispatchAsync(command, onLifecycleChange: null, cancellationToken).ConfigureAwait(false);
        }

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
}
