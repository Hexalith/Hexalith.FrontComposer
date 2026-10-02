using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Forms;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
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
        // BH10-01 — the refused attempt releases only its own token, which cannot clear the holder's slot.
        AssertReservationReleasedByOwner();
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
        // BH10-01 — the browser may have reserved the slot before failing, so the attempt releases it by owner.
        AssertReservationReleasedByOwner();
    }

    [Fact]
    public async Task GeneratedRendererLostCaptureReplyReleasesItsReservationByOwner() {
        // BH10-01 — a capture whose reply never arrives may still have reserved the modal slot in the
        // browser; the renderer releases it by owner token instead of leaving a durable reservation.
        _ = FcFocusModule.Setup<bool>("captureOverlayOrigin", _ => true).SetException(new TaskCanceledException("capture reply lost"));
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

        AssertReservationReleasedByOwner();
        dialogService.ShowDialogCallCount.ShouldBe(0);
        commandService.DispatchCount.ShouldBe(0);
        Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task GeneratedRendererFailedRestoreRetriesTheReleaseOnTheNextRender() {
        // BH10-01 — a restore that fails (for example during a disconnect) would leave the durable
        // reservation in the browser; the renderer retries its owner release on the next render.
        _ = FcFocusModule.SetupVoid("restoreOverlayOrigin", _ => true).SetException(new JSException("restore failed"));
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
        AssertOverlayOriginRestoredOnce();
        FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "releaseOverlayReservation").ShouldBeFalse();

        cut.Render();

        AssertReservationReleasedByOwner();
        commandService.DispatchCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedRendererHeldPreparationKeepsCompetingSettingsLaunchReserved(bool originAbandoned) {
        HoldingDerivedValueProvider derivedValues = new();
        RecordingCommandService commandService = new();
        ControlledDialogService dialogService = new(DialogResult.Cancel());
        Services.RemoveAll<IDerivedValueProvider>();
        _ = Services.AddSingleton<IDerivedValueProvider>(derivedValues);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService.Service));
        await InitializeStoreAsync();

        IRenderedComponent<DeleteWidgetCommandRenderer> cut = Render<DeleteWidgetCommandRenderer>();
        IRenderedComponent<FcSettingsButton> settings = Render<FcSettingsButton>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();
        await derivedValues.RefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        // The owner capture must already have reserved the slot when derived-value preparation begins.
        JSRuntimeInvocation capture = FcFocusModule.Invocations
            .Where(invocation => invocation.Identifier == "captureOverlayOrigin")
            .ShouldHaveSingleItem();
        capture.Arguments.Count.ShouldBe(3);
        capture.Arguments[0].ShouldBeNull();
        capture.Arguments[1].ShouldBe(false);
        capture.Arguments[2].ShouldBeOfType<string>().ShouldStartWith("fc-trigger-");
        FcFocusModule.Invocations.ShouldNotContain(invocation => invocation.Identifier == "releaseOverlayReservation"
            || invocation.Identifier == "restoreOverlayOrigin");

        // The destructive path reserves the modal slot before the real derived-value await. A shell
        // overlay therefore fails closed while preparation is held and cannot open a competing dialog.
        _ = FcFocusModule.Setup<bool>("captureOverlayOrigin", invocation => invocation.Arguments.Count == 2).SetResult(false);
        await settings.Find("[data-testid='fc-settings-button']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        dialogService.ShowDialogCallCount.ShouldBe(0);
        FcFocusModule.Invocations.ShouldContain(invocation => invocation.Identifier == "captureOverlayOrigin"
            && invocation.Arguments.Count == 2
            && Equals(invocation.Arguments[0], "fc-settings-button")
            && Equals(invocation.Arguments[1], true));

        _ = FcFocusModule.Setup<bool>("ownsOverlayReservation", _ => true).SetResult(!originAbandoned);
        derivedValues.Release();
        AssertOverlayOriginRestoredOnce();
        cut.WaitForAssertion(() => FcFocusModule.Invocations
            .Where(invocation => invocation.Identifier == "ownsOverlayReservation")
            .ShouldHaveSingleItem().Arguments.Single().ShouldBe(capture.Arguments[2]));
        if (originAbandoned) {
            // Navigation removed the invoker while preparation was held. The old attempt must not
            // open a delayed modal over the replacement interaction or dispatch its command.
            dialogService.ShowDialogCallCount.ShouldBe(0);
            commandService.DispatchCount.ShouldBe(0);
            return;
        }

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));

        // Once preparation completes and the destructive owner releases the reservation, the same
        // settings launch is admitted normally.
        _ = FcFocusModule.Setup<bool>("captureOverlayOrigin", invocation => invocation.Arguments.Count == 2).SetResult(true);
        await settings.Find("[data-testid='fc-settings-button']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        settings.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(2));
        dialogService.LastDialogType.ShouldBe(typeof(FcSettingsDialog));
        commandService.DispatchCount.ShouldBe(0);
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
    public async Task GeneratedRendererSecondPressBeforeConfirmationAnnouncesOneBlockedAttempt() {
        // Story 13.3 VG17-O1 (decided 2026-10-02) — admission is taken before BeforeSubmit (ECH-05), so the
        // second press of a double-click, which reaches the form before the confirmation makes the page
        // inert, is a blocked attempt like any same-form resubmit (VG2-05): it opens no second dialog,
        // dispatches nothing, keeps focus on the submit control, and announces AM-20 once.
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
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

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (BlockedStatus().Length == 0 && DateTime.UtcNow < deadline) {
            time.Advance(TimeSpan.FromMilliseconds(50));
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        BlockedStatus().ShouldBe("This command did not run. Another command is already in progress.");
        cut.FindAll("[data-testid='fc-command-blocked-status']").Count.ShouldBe(1);
        FcFocusModule.Invocations
            .Where(invocation => invocation.Identifier == "focusAttemptedControl")
            .ShouldHaveSingleItem()
            .Arguments[0].ShouldBeOfType<string>().ShouldEndWith("-submit", Case.Sensitive);
        dialogService.ShowDialogCallCount.ShouldBe(1);
        commandService.DispatchCount.ShouldBe(0);

        dialogService.Complete(DialogResult.Ok());

        cut.WaitForAssertion(() => commandService.DispatchCount.ShouldBe(1));
        dialogService.ShowDialogCallCount.ShouldBe(1);

        string BlockedStatus() => cut.Find("[data-testid='fc-command-blocked-status']").TextContent;
    }

    [Theory]
    [InlineData("en", "Archive Widget?", "This action cannot be undone.")]
    [InlineData("fr", "Archive Widget\u00a0?", "Cette action est irréversible.")]
    public async Task GeneratedRendererWithoutDeclaredCopyShowsLocalizedDefaultConfirmation(
        string cultureName,
        string expectedTitle,
        string expectedBody) {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo culture = new(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try {
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

            // VG3-05 — default title/body copy comes from the localized Shell resources at runtime.
            cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
            DialogOptions options = dialogService.LastOptions.ShouldNotBeNull();
            options.Parameters[nameof(FcDestructiveConfirmationDialog.Title)].ShouldBe(expectedTitle);
            options.Parameters[nameof(FcDestructiveConfirmationDialog.Body)].ShouldBe(expectedBody);
            options.Parameters[nameof(FcDestructiveConfirmationDialog.DestructiveLabel)].ShouldBe("Archive Widget");
            options.Modal.ShouldBe(true);
            commandService.DispatchCount.ShouldBe(0);
        }
        finally {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private void AssertOverlayOriginRestoredOnce() {
        // The restore runs in the gate's finally block, after the last render it may cause, so it is
        // polled rather than awaited through a render.
        SpinWait.SpinUntil(
            () => FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "restoreOverlayOrigin"),
            TimeSpan.FromSeconds(5)).ShouldBeTrue();
        FcFocusModule.Invocations.Count(invocation => invocation.Identifier == "restoreOverlayOrigin").ShouldBe(1);
    }

    private void AssertReservationReleasedByOwner() {
        // The release runs in the gate's finally block or a later render, so it is polled.
        SpinWait.SpinUntil(
            () => FcFocusModule.Invocations.Any(invocation => invocation.Identifier == "releaseOverlayReservation"),
            TimeSpan.FromSeconds(5)).ShouldBeTrue();
        string owner = FcFocusModule.Invocations
            .Last(invocation => invocation.Identifier == "captureOverlayOrigin")
            .Arguments[2].ShouldBeOfType<string>();
        owner.ShouldStartWith("fc-trigger-");
        FcFocusModule.Invocations
            .Where(invocation => invocation.Identifier == "releaseOverlayReservation")
            .ShouldHaveSingleItem()
            .Arguments.ShouldHaveSingleItem().ShouldBe(owner);
    }

    private sealed class HoldingDerivedValueProvider : IDerivedValueProvider {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public TaskCompletionSource RefreshStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<DerivedValueResult> ResolveAsync(
            Type commandType,
            string propertyName,
            ProjectionContext? context,
            CancellationToken cancellationToken = default) {
            if (Interlocked.Increment(ref _calls) > 1) {
                RefreshStarted.TrySetResult();
                await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return propertyName == nameof(DeleteWidgetCommand.MessageId)
                ? new DerivedValueResult(true, "01ARZ3NDEKTSV4RRFFQ69G5FAV")
                : new DerivedValueResult(false, null);
        }

        public void Release() => _release.TrySetResult();
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
