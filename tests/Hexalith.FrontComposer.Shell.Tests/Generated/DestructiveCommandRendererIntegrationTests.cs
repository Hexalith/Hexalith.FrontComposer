using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Forms;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class DestructiveCommandRendererIntegrationTests : CommandRendererTestBase {
    [Fact]
    public async Task GeneratedRenderer_NestedModalOrigin_FailsClosedWithoutDialogOrDispatch() {
        _ = FcFocusModule.Setup<bool>("captureOverlayOrigin", _ => true).SetResult(false);
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Ok());
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => FcFocusModule.Invocations
            .Count(invocation => invocation.Identifier == "captureOverlayOrigin")
            .ShouldBe(1));
        dialogService.ShowDialogCallCount.ShouldBe(0);
        commandService.DispatchCount.ShouldBe(0);
        FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "restoreOverlayOrigin").ShouldBeFalse();
    }

    [Fact]
    public async Task GeneratedRendererOriginCaptureFailureFailsClosedWithoutDialogOrDispatch() {
        _ = FcFocusModule.Setup<bool>("captureOverlayOrigin", _ => true).SetException(new JSException("capture failed"));
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Ok());
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        // The destructive gate converts a JS failure into a fail-closed cancellation: no dialog, no
        // dispatch, no restore of an origin that was never captured, and nothing escapes the render.
        cut.WaitForAssertion(() => FcFocusModule.Invocations
            .Count(invocation => invocation.Identifier == "captureOverlayOrigin")
            .ShouldBe(1));
        dialogService.ShowDialogCallCount.ShouldBe(0);
        commandService.DispatchCount.ShouldBe(0);
        FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "restoreOverlayOrigin").ShouldBeFalse();
        Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        _ = cut.Find("form");
    }

    [Fact]
    public async Task GeneratedRenderer_CancelledDialog_PreventsCommandDispatch() {
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Cancel());
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        commandService.DispatchCount.ShouldBe(0);
        dialogService.LastDialogType.ShouldBe(typeof(FcDestructiveConfirmationDialog));
        // OF-03 — the captured invoker origin is restored exactly once after the dialog closes.
        AssertOverlayOriginRestoredOnce();
    }

    [Fact]
    public async Task GeneratedRenderer_ConfirmedDialog_AllowsExactlyOneDispatch() {
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Ok());
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => commandService.DispatchCount.ShouldBe(1));
        dialogService.ShowDialogCallCount.ShouldBe(1);
        // OF-03 — the captured invoker origin is restored exactly once after the dialog closes.
        AssertOverlayOriginRestoredOnce();
    }

    [Fact]
    public async Task GeneratedRendererTenantSwitchWhileTheDialogIsOpenFailsClosed() {
        UserContext.TenantId = "tenant-a";
        UserContext.UserId = "user-a";
        Services.Replace(ServiceDescriptor.Scoped<IFrontComposerTenantContextAccessor, FrontComposerTenantContextAccessor>());
        Services.Replace(ServiceDescriptor.Scoped<IValidatedPendingScope, ValidatedPendingScope>());
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));

        // The workspace changes while the destructive confirmation (BeforeSubmit) is open. The admission
        // scope was read before the dialog, so the confirmed command fails closed instead of binding the
        // tenant-a model to tenant-b.
        UserContext.TenantId = "tenant-b";
        await cut.InvokeAsync(() => dialogService.Complete(DialogResult.Ok()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Workspace unavailable"));
        commandService.DispatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task GeneratedRenderer_RapidSubmit_OpensOneDialogAndDispatchesOnce() {
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        dialogService.Complete(DialogResult.Ok());

        cut.WaitForAssertion(() => commandService.DispatchCount.ShouldBe(1));
        dialogService.ShowDialogCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GeneratedRendererWithoutDeclaredCopyShowsLocalizedDefaultConfirmation() {
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Cancel());
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<ArchiveWidgetCommandRenderer> cut = Render<ArchiveWidgetCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        // VG3-05 — the default copy comes from the localized Shell resources at runtime, and the
        // modal is named, described, and cancel-first. E5-23 — the default title keeps the baseline
        // "{DisplayLabel}?" copy.
        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        DialogOptions options = dialogService.LastOptions.ShouldNotBeNull();
        options.Parameters[nameof(FcDestructiveConfirmationDialog.Title)].ShouldBe("Archive Widget?");
        options.Parameters[nameof(FcDestructiveConfirmationDialog.Body)].ShouldBe("This action cannot be undone.");
        options.Parameters[nameof(FcDestructiveConfirmationDialog.DestructiveLabel)].ShouldBe("Archive Widget");
        options.Modal.ShouldBe(true);
        commandService.DispatchCount.ShouldBe(0);
    }

    private void AssertOverlayOriginRestoredOnce() {
        // The restore runs in the gate's finally block, after the last render it may cause, so it is
        // polled rather than awaited through a render.
        SpinWait.SpinUntil(
            () => FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "restoreOverlayOrigin"),
            TimeSpan.FromSeconds(5)).ShouldBeTrue();
        FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "restoreOverlayOrigin").ShouldBe(1);
    }

    private sealed class ControlledDialogService : DialogService {
        private readonly Queue<TaskCompletionSource<DialogResult>> _pendingResults = new();
        private readonly DialogResult? _immediateResult;

        public ControlledDialogService(DialogResult? immediateResult = null)
            : base(
                new ServiceCollection()
                    .AddSingleton(Substitute.For<IJSRuntime>())
                    .BuildServiceProvider(),
                Substitute.For<IFluentLocalizer>()) => _immediateResult = immediateResult;

        public IDialogService Service => this;

        public Type? LastDialogType { get; private set; }

        public DialogOptions? LastOptions { get; private set; }

        public int ShowDialogCallCount { get; private set; }

        public override Task<DialogResult> ShowDialogAsync(Type dialogComponent, DialogOptions options) {
            LastDialogType = dialogComponent;
            LastOptions = options;
            ShowDialogCallCount++;

            if (_immediateResult is { } result) {
                return Task.FromResult(result);
            }

            TaskCompletionSource<DialogResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingResults.Enqueue(pending);
            return pending.Task;
        }

        public void Complete(DialogResult result) => _pendingResults.Dequeue().SetResult(result);
    }

    private sealed class RecordingCommandService : ICommandServiceWithLifecycle {
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            return Task.FromResult(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", "Accepted"));
        }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            return Task.FromResult(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", "Accepted"));
        }
    }
}
