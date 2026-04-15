using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests;

public class CommandServiceExtensionsTests {
    [Fact]
    public async Task DispatchAsync_CallbackOverload_FallsBackToBaseContract_WhenLifecycleIsUnsupported() {
        BasicCommandService service = new();
        using CancellationTokenSource cts = new();
        bool callbackInvoked = false;

        CommandResult result = await service.DispatchAsync(
            new object(),
            onLifecycleChange: (_, _) => callbackInvoked = true,
            cancellationToken: cts.Token);

        result.MessageId.ShouldBe("basic-message");
        service.ObservedToken.ShouldBe(cts.Token);
        callbackInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task DispatchAsync_CallbackOverload_UsesLifecycleAwareImplementation_WhenAvailable() {
        LifecycleAwareCommandService service = new();
        using CancellationTokenSource cts = new();
        List<CommandLifecycleState> observedStates = [];

        CommandResult result = await ((ICommandService)service).DispatchAsync(
            new object(),
            onLifecycleChange: (state, _) => observedStates.Add(state),
            cancellationToken: cts.Token);

        result.MessageId.ShouldBe("lifecycle-message");
        service.ObservedToken.ShouldBe(cts.Token);
        observedStates.ShouldBe([CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed]);
    }

    private sealed class BasicCommandService : ICommandService {
        public CancellationToken ObservedToken { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            ObservedToken = cancellationToken;
            return Task.FromResult(new CommandResult("basic-message", "Accepted"));
        }
    }

    private sealed class LifecycleAwareCommandService : ICommandServiceWithLifecycle {
        public CancellationToken ObservedToken { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            ObservedToken = cancellationToken;
            return Task.FromResult(new CommandResult("legacy-message", "Accepted"));
        }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            ObservedToken = cancellationToken;
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, "lifecycle-message");
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, "lifecycle-message");
            return Task.FromResult(new CommandResult("lifecycle-message", "Accepted"));
        }
    }
}
