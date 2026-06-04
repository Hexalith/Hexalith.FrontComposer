using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services;

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
}
