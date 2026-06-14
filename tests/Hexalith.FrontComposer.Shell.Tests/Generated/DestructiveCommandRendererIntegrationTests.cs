using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class DestructiveCommandRendererIntegrationTests : CommandRendererTestBase {
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
