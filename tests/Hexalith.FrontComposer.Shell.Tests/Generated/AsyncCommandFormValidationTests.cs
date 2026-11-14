using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Exercises the asynchronous validation boundary in actual generated forms.</summary>
public sealed class AsyncCommandFormValidationTests : CommandRendererTestBase {
    [Theory]
    [InlineData("valid", true)]
    [InlineData("invalid", false)]
    [InlineData("cancel-token", false)]
    [InlineData("dispose", false)]
    [InlineData("throw-cancellation", false)]
    [InlineData("replace-context", false)]
    public async Task PendingValidationOnlyCurrentValidAttemptDispatches(string outcome, bool shouldDispatch) {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(
                CommandWarningKind.RateLimited, ProblemDetailsPayload.Empty, TimeSpan.FromSeconds(1))));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        EditContext? editContext = null;
        int beforeSubmitCalls = 0;
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(component => component.InitialValue, new TwoFieldCompactCommand { Name = "Valid name", Amount = 7 })
            .Add(component => component.BeforeSubmit, () => { beforeSubmitCalls++; return Task.CompletedTask; })
            .Add(component => component.OnEditContextReady,
                EventCallback.Factory.Create<EditContext>(this, readyContext => editContext = readyContext)));
        EditContext context = editContext.ShouldNotBeNull();
        ValidationMessageStore messages = new(context);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken validationToken = default;
        context.OnValidationRequested += (_, args) => args.AddAsyncValidator(async token => {
            validationToken = token;
            entered.TrySetResult();
            // Deliberately ignore cancellation: the form must still reject late completion.
            await release.Task.ConfigureAwait(false);
            if (outcome == "invalid") {
                messages.Add(new FieldIdentifier(context.Model, "Name"), "Async validation rejected the name.");
            }
            if (outcome == "throw-cancellation") {
                throw new OperationCanceledException(token);
            }
        });
        MethodInfo submitMethod = typeof(TwoFieldCompactCommandForm)
            .GetMethod("SubmitAsync", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull();
        Task submit = cut.InvokeAsync(() => (Task)submitMethod.Invoke(cut.Instance, [true])!);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        beforeSubmitCalls.ShouldBe(0);
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        try {
            if (outcome == "dispose") {
                await cut.InvokeAsync(cut.Instance.Dispose);
                validationToken.IsCancellationRequested.ShouldBeTrue();
            }
            else if (outcome == "cancel-token") {
                var cts = (CancellationTokenSource)typeof(TwoFieldCompactCommandForm)
                    .GetField("_cts", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().GetValue(cut.Instance)!;
                await cut.InvokeAsync(cts.Cancel);
                validationToken.IsCancellationRequested.ShouldBeTrue();
            }
            else if (outcome == "replace-context") {
                await cut.InvokeAsync(() => typeof(TwoFieldCompactCommandForm)
                    .GetField("_editContext", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull()
                    .SetValue(cut.Instance, new EditContext(new TwoFieldCompactCommand())));
            }
        }
        finally {
            release.TrySetResult();
        }
        await submit.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        beforeSubmitCalls.ShouldBe(shouldDispatch ? 1 : 0);
        _ = service.Received(shouldDispatch ? 1 : 0).DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        using CommandExecutionAdmission admission = Services.GetRequiredService<ICommandExecutionAdmissionGate>()
            .TryAcquire(new CommandExecutionAdmissionRequest("next-command", "Next command"));
        admission.IsAdmitted.ShouldBeTrue("every completed validation path must release its admission lease");
    }
    [Theory]
    [InlineData("hidden")]
    [InlineData("blocked")]
    public async Task AsynchronousHostCallbackKeepsSubmitOnRenderer(string callback) {
        ICommandServiceWithLifecycleObservations service = RegisterWarningService();
        await InitializeStoreAsync();
        TaskCompletionSource hostEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource hostRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource validationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource validationRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EditContext? editContext = null;
        async Task HostCallbackAsync() {
            hostEntered.TrySetResult();
            await hostRelease.Task.ConfigureAwait(false);
        }
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => {
            parameters.Add(component => component.InitialValue, new TwoFieldCompactCommand { Name = "Valid name", Amount = 7 })
                .Add(component => component.OnEditContextReady,
                    EventCallback.Factory.Create<EditContext>(this, readyContext => editContext = readyContext));
            if (callback == "hidden") {
                parameters.Add(component => component.OnHiddenOutcome,
                    EventCallback.Factory.Create<string?>(this, _ => HostCallbackAsync()));
            }
            else {
                parameters.Add(component => component.OnBlockedSubmission,
                    EventCallback.Factory.Create<bool>(this, _ => HostCallbackAsync()));
            }
        });
        if (callback == "blocked") {
            typeof(TwoFieldCompactCommandForm).GetField("_hostBlockedOutcomePresented", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(cut.Instance, true);
        }
        editContext.ShouldNotBeNull().OnValidationRequested += (_, args) => args.AddAsyncValidator(async _ => {
            validationEntered.TrySetResult();
            await validationRelease.Task.ConfigureAwait(false);
        });
        Task submit = InvokeSubmitAsync(cut);
        await hostEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        validationEntered.Task.IsCompleted.ShouldBeFalse();
        hostRelease.TrySetResult();
        await validationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        validationRelease.TrySetResult();
        await submit.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        _ = service.Received(1).DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task FieldChangeInvalidatesPendingValidationAndFreshRetryDispatchesValidatedValues() {
        ICommandServiceWithLifecycleObservations service = RegisterWarningService();
        await InitializeStoreAsync();
        TwoFieldCompactCommand model = new() { Name = "Original valid name", Amount = 7 };
        EditContext? editContext = null;
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(component => component.InitialValue, model)
            .Add(component => component.OnEditContextReady,
                EventCallback.Factory.Create<EditContext>(this, readyContext => editContext = readyContext)));
        EditContext context = editContext.ShouldNotBeNull();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<string?> validatedNames = [];
        context.OnValidationRequested += (_, args) => args.AddAsyncValidator(async _ => {
            validatedNames.Add(model.Name);
            if (validatedNames.Count == 1) {
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
            }
        });
        Task first = InvokeSubmitAsync(cut);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.InvokeAsync(() => {
            model.Name = "Changed valid name";
            context.NotifyFieldChanged(new FieldIdentifier(model, nameof(model.Name)));
        });
        release.TrySetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        _ = service.DidNotReceive().DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        using (CommandExecutionAdmission admission = Services.GetRequiredService<ICommandExecutionAdmissionGate>()
            .TryAcquire(new CommandExecutionAdmissionRequest("next-command", "Next command"))) {
            admission.IsAdmitted.ShouldBeTrue();
        }
        await InvokeSubmitAsync(cut).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        validatedNames.ShouldBe(["Original valid name", "Changed valid name"]);
        _ = service.Received(1).DispatchAsync(Arg.Is<TwoFieldCompactCommand>(command => command.Name == "Changed valid name"),
            Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
    }

    private ICommandServiceWithLifecycleObservations RegisterWarningService() {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult>(new CommandWarningException(
                CommandWarningKind.RateLimited, ProblemDetailsPayload.Empty, TimeSpan.FromSeconds(1))));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        return service;
    }

    private static Task InvokeSubmitAsync(IRenderedComponent<TwoFieldCompactCommandForm> cut)
        => cut.InvokeAsync(() => (Task)typeof(TwoFieldCompactCommandForm)
            .GetMethod("SubmitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cut.Instance, [true])!);

}
