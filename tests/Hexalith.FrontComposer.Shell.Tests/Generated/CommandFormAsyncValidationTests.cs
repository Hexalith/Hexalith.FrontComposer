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

public sealed class CommandFormAsyncValidationTests : CommandRendererTestBase
{
    [Theory]
    [InlineData("valid")]
    [InlineData("invalid")]
    [InlineData("faulted")]
    public async Task SubmitAwaitsValidationAndDispatchesOnlyAValidResult(string outcome)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        service.DispatchAsync(Arg.Any<TwoFieldCompactCommand>(), Arg.Any<Action<CommandLifecycleObservation>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", CommandResultStatus.Accepted)));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        IValidatedPendingScope scope = Substitute.For<IValidatedPendingScope>();
        scope.Current().Returns(("counter-demo", "demo-user"));
        Services.Replace(ServiceDescriptor.Scoped<IValidatedPendingScope>(_ => scope));
        await InitializeStoreAsync().ConfigureAwait(true);
        int beforeSubmit = 0;
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>(parameters => parameters
            .Add(component => component.BeforeSubmit, () =>
            {
                beforeSubmit++;
                return Task.CompletedTask;
            }));
        EditContext context = cut.FindComponent<EditForm>().Instance.EditContext.ShouldNotBeNull();
        ValidationMessageStore messages = new(context);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int validationCount = 0;
        context.OnValidationRequested += (_, args) => args.AddAsyncValidator(async cancellationToken =>
        {
            validationCount++;
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
            if (outcome == "invalid")
            {
                messages.Add(context.Field(nameof(TwoFieldCompactCommand.Name)), "The name is unavailable.");
            }
            else if (outcome == "faulted")
            {
                throw new InvalidOperationException("Validation could not complete.");
            }
        });

        Task submit = cut.Find("form").SubmitAsync();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
            submit.IsCompleted.ShouldBeFalse();
            beforeSubmit.ShouldBe(0);
            service.ReceivedCalls().ShouldBeEmpty();
            if (outcome == "valid")
            {
                await cut.Find("form").SubmitAsync().WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
                validationCount.ShouldBe(1);
                service.ReceivedCalls().ShouldBeEmpty();
            }
        }
        finally
        {
            release.TrySetResult();
        }

        await submit.ConfigureAwait(true);
        context.IsValidationFaulted().ShouldBe(outcome == "faulted");
        cut.Instance.BeforeSubmit.ShouldNotBeNull();
        cut.WaitForAssertion(() =>
        {
            beforeSubmit.ShouldBe(outcome == "valid" ? 1 : 0);
            service.ReceivedCalls().Count().ShouldBe(outcome == "valid" ? 1 : 0, cut.Markup);
        });
        if (outcome == "invalid")
        {
            cut.Markup.ShouldContain("The name is unavailable.");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundaryChangeDuringValidationPreventsDispatchAfterValidationCompletes(bool dispose)
    {
        ICommandServiceWithLifecycleObservations service = Substitute.For<ICommandServiceWithLifecycleObservations>();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        IValidatedPendingScope scope = Substitute.For<IValidatedPendingScope>();
        scope.Current().Returns(("counter-demo", "demo-user"));
        Services.Replace(ServiceDescriptor.Scoped<IValidatedPendingScope>(_ => scope));
        await InitializeStoreAsync().ConfigureAwait(true);
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        EditContext context = cut.FindComponent<EditForm>().Instance.EditContext.ShouldNotBeNull();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.OnValidationRequested += (_, args) => args.AddAsyncValidator(async cancellationToken =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
        });

        Task submit = cut.Find("form").SubmitAsync();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
            if (dispose)
            {
                cut.Instance.Dispose();
            }
            else
            {
                scope.Current().Returns(("other-tenant", "demo-user"));
            }
        }
        finally
        {
            release.TrySetResult();
        }

        await submit.ConfigureAwait(true);
        service.ReceivedCalls().ShouldBeEmpty();
    }
}
