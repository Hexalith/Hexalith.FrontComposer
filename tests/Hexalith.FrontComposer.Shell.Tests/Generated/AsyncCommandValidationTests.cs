using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Exercises a real generated form across asynchronous validation and disposal.</summary>
public sealed class AsyncCommandValidationTests : CommandRendererTestBase {
    /// <summary>Only a still-mounted form may dispatch after its held validator completes.</summary>
    /// <param name="dispose">Whether the form is disposed while validation is pending.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldValidationCannotDispatchFromADisposedForm(bool dispose) {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        _ = service.DispatchAsync(Arg.Any<OneFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", CommandResultStatus.Accepted)));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync().ConfigureAwait(true);
        IRenderedComponent<OneFieldInlineCommandForm> cut = Render<OneFieldInlineCommandForm>();
        EditContext editContext = cut.FindComponent<EditForm>().Instance.EditContext.ShouldNotBeNull();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        editContext.OnValidationRequested += (_, args) => args.AddAsyncValidator(async _ => {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(true);
        });
        MethodInfo submit = typeof(OneFieldInlineCommandForm).GetMethod("SubmitAsync", BindingFlags.NonPublic | BindingFlags.Instance).ShouldNotBeNull();
        Task submission = cut.InvokeAsync(() => (Task)submit.Invoke(cut.Instance, [true])!);
        try {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
            submission.IsCompleted.ShouldBeFalse();
            if (dispose) await cut.InvokeAsync(cut.Instance.Dispose).ConfigureAwait(true);
        }
        finally {
            release.TrySetResult();
        }
        await submission.ConfigureAwait(true);
        _ = service.Received(dispose ? 0 : 1).DispatchAsync(Arg.Any<OneFieldInlineCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>());
        using CommandExecutionAdmission lease = Services.GetRequiredService<ICommandExecutionAdmissionGate>()
            .TryAcquire(new CommandExecutionAdmissionRequest("after-validation"));
        if (dispose) lease.IsAdmitted.ShouldBeTrue();
    }
}
