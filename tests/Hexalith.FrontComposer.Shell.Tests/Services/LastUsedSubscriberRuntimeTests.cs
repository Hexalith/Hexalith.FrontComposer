using System.Collections.Concurrent;
using System.Reflection;

using Counter.Domain;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class LastUsedSubscriberRuntimeTests {
    private const int SubscriberTtlMinutes = 5;
    private const int SubscriberMaxInFlight = 16;

    [Fact]
    public async Task Subscriber_Registers_OnFirstEnsureCall() {
        int activations = 0;
        (ServiceProvider provider, _, _) = await CreateProviderAsync(
            registerDomain: true,
            configure: services => {
                services.Replace(ServiceDescriptor.Scoped<IncrementCommandLastUsedSubscriber>(sp => {
                    activations++;
                    return ActivatorUtilities.CreateInstance<IncrementCommandLastUsedSubscriber>(sp);
                }));
            });

        using (provider) {
            activations.ShouldBe(0);

            provider.GetRequiredService<ILastUsedSubscriberRegistry>()
                .Ensure<IncrementCommandLastUsedSubscriber>();

            activations.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Subscriber_Unsubscribes_OnRegistryDispose() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, _) = await CreateProviderAsync(registerDomain: true);

        using (provider) {
            LastUsedSubscriberRegistry registry = provider.GetRequiredService<LastUsedSubscriberRegistry>();
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            registry.Ensure<IncrementCommandLastUsedSubscriber>();
            registry.Dispose();

            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-1", CreateCommand(1)));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-1"));

            recorder.Invocations.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Submitted_Then_Confirmed_CallsRecordWithTypedCommand() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock);
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            IncrementCommand command = CreateCommand(5);
            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-1", command));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-1"));

            recorder.Invocations.Count.ShouldBe(1);
            recorder.Invocations[0].CommandType.ShouldBe(typeof(IncrementCommand));
            recorder.Invocations[0].Command.ShouldBeSameAs(command);
        }
    }

    [Fact]
    public async Task Ensure_CalledTwice_RegistersOnce() {
        int activations = 0;
        (ServiceProvider provider, _, _) = await CreateProviderAsync(
            registerDomain: true,
            configure: services => {
                services.Replace(ServiceDescriptor.Scoped<IncrementCommandLastUsedSubscriber>(sp => {
                    activations++;
                    return ActivatorUtilities.CreateInstance<IncrementCommandLastUsedSubscriber>(sp);
                }));
            });

        using (provider) {
            ILastUsedSubscriberRegistry registry = provider.GetRequiredService<ILastUsedSubscriberRegistry>();

            registry.Ensure<IncrementCommandLastUsedSubscriber>();
            registry.Ensure<IncrementCommandLastUsedSubscriber>();

            activations.ShouldBe(1);
        }
    }

    [Fact]
    public async Task InterleavedSubmits_OrderedConfirms_PreservesCorrelation() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock);
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            IncrementCommand commandA = CreateCommand(1, "msg-a");
            IncrementCommand commandB = CreateCommand(2, "msg-b");

            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-a", commandA));
            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-b", commandB));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-a"));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-b"));

            recorder.Invocations.Count.ShouldBe(2);
            recorder.Invocations[0].Command.ShouldBeSameAs(commandA);
            recorder.Invocations[1].Command.ShouldBeSameAs(commandB);
        }
    }

    [Fact]
    public async Task InterleavedSubmits_OutOfOrderConfirms_PreservesCorrelation() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock);
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            IncrementCommand commandA = CreateCommand(1, "msg-a");
            IncrementCommand commandB = CreateCommand(2, "msg-b");

            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-a", commandA));
            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-b", commandB));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-b"));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-a"));

            recorder.Invocations.Count.ShouldBe(2);
            recorder.Invocations[0].Command.ShouldBeSameAs(commandB);
            recorder.Invocations[1].Command.ShouldBeSameAs(commandA);
        }
    }

    [Fact]
    public async Task SubmittedWithoutConfirmed_LaterSubmit_NoStaleReplay() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock);
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            IncrementCommand commandA = CreateCommand(1, "msg-a");
            IncrementCommand commandB = CreateCommand(2, "msg-b");

            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-a", commandA));
            clock.Advance(TimeSpan.FromMinutes(SubscriberTtlMinutes + 1));
            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-b", commandB));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-b"));
            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-a"));

            recorder.Invocations.Count.ShouldBe(1);
            recorder.Invocations[0].Command.ShouldBeSameAs(commandB);
        }
    }

    [Fact]
    public async Task DisposedMidFlight_NoExceptionNoGhostRecord() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock);

            dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction("corr-a", CreateCommand(1)));
            subscriber.Dispose();

            Should.NotThrow(() => dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-a")));
            recorder.Invocations.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ExceedsMaxInFlight_EvictsOldestAndLogsWarning() {
        (ServiceProvider provider, TestLastUsedRecorder recorder, FakeTimeProvider clock) = await CreateProviderAsync();

        using (provider) {
            TestLogger<IncrementCommandLastUsedSubscriber> logger = new();
            using IncrementCommandLastUsedSubscriber subscriber = CreateSubscriber(provider, recorder, clock, logger);
            IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

            for (int i = 0; i <= SubscriberMaxInFlight; i++) {
                dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction($"corr-{i}", CreateCommand(i, $"msg-{i}")));
                clock.Advance(TimeSpan.FromSeconds(1));
            }

            GetPendingCount(subscriber).ShouldBe(SubscriberMaxInFlight);
            logger.WarningMessages.ShouldContain(m => m.Contains("D38 cap reached", StringComparison.Ordinal));

            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction("corr-0"));
            recorder.Invocations.ShouldBeEmpty();

            dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction($"corr-{SubscriberMaxInFlight}"));
            recorder.Invocations.Count.ShouldBe(1);
        }
    }

    private static IncrementCommandLastUsedSubscriber CreateSubscriber(
        IServiceProvider provider,
        TestLastUsedRecorder recorder,
        TimeProvider clock,
        ILogger<IncrementCommandLastUsedSubscriber>? logger = null) {
        return new IncrementCommandLastUsedSubscriber(
            provider.GetRequiredService<IActionSubscriber>(),
            recorder,
            logger,
            clock);
    }

    private static async Task<(ServiceProvider Provider, TestLastUsedRecorder Recorder, FakeTimeProvider Clock)> CreateProviderAsync(
        bool registerDomain = false,
        Action<IServiceCollection>? configure = null) {
        TestLastUsedRecorder recorder = new();
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero));

        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(o => o.ScanAssemblies(typeof(CounterDomain).Assembly));
        if (registerDomain) {
            _ = services.AddHexalithDomain<CounterDomain>();
        }

        services.Replace(ServiceDescriptor.Scoped<ILastUsedRecorder>(_ => recorder));
        _ = services.AddSingleton<TimeProvider>(clock);
        configure?.Invoke(services);

        ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IStore>().InitializeAsync().ConfigureAwait(false);
        return (provider, recorder, clock);
    }

    private static IncrementCommand CreateCommand(int amount, string? messageId = null) {
        return new IncrementCommand {
            Amount = amount,
            MessageId = messageId ?? $"msg-{amount}",
            TenantId = "tenant-a",
        };
    }

    private static int GetPendingCount(IncrementCommandLastUsedSubscriber subscriber) {
        FieldInfo field = typeof(IncrementCommandLastUsedSubscriber).GetField("_pending", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected generated _pending field.");
        object pending = field.GetValue(subscriber)
            ?? throw new InvalidOperationException("Expected generated pending dictionary instance.");
        PropertyInfo count = pending.GetType().GetProperty("Count")
            ?? throw new InvalidOperationException("Expected Count property on generated pending dictionary.");
        return (int)(count.GetValue(pending) ?? 0);
    }

    private sealed class TestLastUsedRecorder : ILastUsedRecorder {
        public List<RecordedInvocation> Invocations { get; } = [];

        public Task RecordAsync<TCommand>(TCommand command) where TCommand : class {
            Invocations.Add(new RecordedInvocation(typeof(TCommand), command));
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedInvocation(Type CommandType, object Command);

    private sealed class FakeTimeProvider : TimeProvider {
        private DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private sealed class TestLogger<T> : ILogger<T> {
        public List<string> WarningMessages { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Warning) {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
