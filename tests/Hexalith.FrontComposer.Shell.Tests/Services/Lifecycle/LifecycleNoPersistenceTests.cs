using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>
/// Story 2-3 Task 11.7 — non-persistence invariant. <see cref="LifecycleStateService"/> is ephemeral
/// (architecture.md §536) and MUST NEVER touch <see cref="IStorageService"/>. A storage substitute
/// that throws on any call would fail the test if the service wrote to it.
/// </summary>
public class LifecycleNoPersistenceTests {
    [Fact]
    public void LifecycleStateService_DoesNotWriteToIStorageService() {
        IStorageService throwingStorage = Substitute.For<IStorageService>();
        throwingStorage
            .When(s => s.SetAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("lifecycle state must not persist"));
        throwingStorage
            .When(s => s.GetAsync<object?>(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("lifecycle state must not read storage"));

        using LifecycleStateService service = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()));

        Should.NotThrow(() => {
            service.Transition("c1", CommandLifecycleState.Submitting);
            service.Transition("c1", CommandLifecycleState.Acknowledged, "M");
            service.Transition("c1", CommandLifecycleState.Syncing);
            service.Transition("c1", CommandLifecycleState.Confirmed);
        });

        throwingStorage.ReceivedCalls().ShouldBeEmpty();
    }
}
