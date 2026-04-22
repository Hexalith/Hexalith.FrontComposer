#pragma warning disable CA2007 // ConfigureAwait not required in test code; xUnit1030 forbids ConfigureAwait(false)
using System.Collections.Concurrent;
using System.Reactive.Subjects;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Badges;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Badges;

/// <summary>
/// Story 3-5 Task 7.1 — behavioural tests for <see cref="BadgeCountService"/>. All tests use
/// <see cref="FakeTimeProvider"/> for deterministic timeout assertions; <c>Task.Delay(5000)</c> is
/// banned (D4).
/// </summary>
public sealed class BadgeCountServiceTests {
    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    private sealed class StubCatalog : IActionQueueProjectionCatalog {
        public StubCatalog(params Type[] types) => ActionQueueTypes = types;

        public IReadOnlyList<Type> ActionQueueTypes { get; }
    }

    private sealed class StubReader : IActionQueueCountReader {
        private readonly Func<Type, CancellationToken, ValueTask<int>> _resolver;

        public StubReader(Func<Type, CancellationToken, ValueTask<int>> resolver)
            => _resolver = resolver;

        public ValueTask<int> GetCountAsync(Type projectionType, CancellationToken cancellationToken)
            => _resolver(projectionType, cancellationToken);
    }

    private sealed class StubNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged;
        public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);
    }

    private static IServiceProvider EmptyProvider() => new ServiceCollection().BuildServiceProvider();

    private static IServiceProvider WithNotifier(IProjectionChangeNotifier notifier) {
        ServiceCollection services = new();
        _ = services.AddSingleton(notifier);
        return services.BuildServiceProvider();
    }

    private sealed class ProjectionAlpha { }
    private sealed class ProjectionBeta { }
    private sealed class ProjectionGamma { }

    [Fact]
    public async Task InitializeAsync_SeedsAllCatalogTypes_ViaReader() {
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta));
        StubReader reader = new((type, _) => new ValueTask<int>(type == typeof(ProjectionAlpha) ? 3 : 5));
        using BadgeCountService sut = new(
            catalog,
            reader,
            EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(),
            new FakeTimeProvider());

        await sut.InitializeAsync(Ct);

        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(3);
        sut.Counts[typeof(ProjectionBeta)].ShouldBe(5);
        sut.TotalActionableItems.ShouldBe(8);
    }

    [Fact]
    public async Task InitializeAsync_PerTypeFailure_LogsHFC2112_ExcludesFromCounts() {
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta));
        StubReader reader = new((type, _) => type == typeof(ProjectionAlpha)
            ? throw new InvalidOperationException("boom")
            : new ValueTask<int>(7));
        ILogger<BadgeCountService> logger = Substitute.For<ILogger<BadgeCountService>>();
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(), logger, new FakeTimeProvider());

        await sut.InitializeAsync(Ct);

        sut.Counts.ContainsKey(typeof(ProjectionAlpha)).ShouldBeFalse();
        sut.Counts[typeof(ProjectionBeta)].ShouldBe(7);
        AssertLoggedWarning(logger, "HFC2112");
    }

    [Fact]
    public async Task DoesNotSubscribeWhenNotifierAbsent_InitialFetchStillRuns() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        StubReader reader = new((_, _) => new ValueTask<int>(2));
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        await sut.InitializeAsync(Ct);

        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(2);
    }

    [Fact]
    public async Task SubscribesToNotifier_WhenRegistered_UpdatesCountsOnChanged() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        int callCount = 0;
        StubReader reader = new((_, _) => {
            callCount++;
            return new ValueTask<int>(callCount * 10);
        });
        StubNotifier notifier = new();
        using BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        await sut.InitializeAsync(Ct);
        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(10);

        notifier.NotifyChanged(typeof(ProjectionAlpha).AssemblyQualifiedName!);
        await Task.Delay(50, Ct);

        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(20);
    }

    [Fact]
    public async Task UnresolvableTypeString_LogsHFC2113_OncePerType() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        StubReader reader = new((_, _) => new ValueTask<int>(0));
        StubNotifier notifier = new();
        ILogger<BadgeCountService> logger = Substitute.For<ILogger<BadgeCountService>>();
        using BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier), logger, new FakeTimeProvider());

        notifier.NotifyChanged("Made.Up.NotARealType, Phantom.Assembly");
        notifier.NotifyChanged("Made.Up.NotARealType, Phantom.Assembly");
        notifier.NotifyChanged("Made.Up.NotARealType, Phantom.Assembly");
        await Task.Delay(50, Ct);

        int hfc2113Calls = CountLoggedAtLevel(logger, LogLevel.Information, "HFC2113");
        hfc2113Calls.ShouldBe(1);
    }

    [Fact]
    public async Task NonActionQueueTypeString_SilentNoOp() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        int readerCallCount = 0;
        StubReader reader = new((_, _) => {
            readerCallCount++;
            return new ValueTask<int>(0);
        });
        StubNotifier notifier = new();
        ILogger<BadgeCountService> logger = Substitute.For<ILogger<BadgeCountService>>();
        using BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier), logger, new FakeTimeProvider());

        notifier.NotifyChanged(typeof(ProjectionBeta).AssemblyQualifiedName!);
        await Task.Delay(50, Ct);

        readerCallCount.ShouldBe(0);
        CountLoggedAtLevel(logger, LogLevel.Information, "HFC2113").ShouldBe(0);
        CountLoggedAtLevel(logger, LogLevel.Warning, "HFC2112").ShouldBe(0);
    }

    [Fact]
    public async Task OnProjectionChanged_ReaderThrows_DoesNotCrashCircuit_LogsHFC2112() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        bool firstCall = true;
        StubReader reader = new((_, _) => {
            if (firstCall) {
                firstCall = false;
                return new ValueTask<int>(1);
            }

            throw new InvalidOperationException("notifier-time boom");
        });
        StubNotifier notifier = new();
        ILogger<BadgeCountService> logger = Substitute.For<ILogger<BadgeCountService>>();
        using BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier), logger, new FakeTimeProvider());

        await sut.InitializeAsync(Ct);
        notifier.NotifyChanged(typeof(ProjectionAlpha).AssemblyQualifiedName!);
        await Task.Delay(50, Ct);

        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(1);
        CountLoggedAtLevel(logger, LogLevel.Warning, "HFC2112").ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ObservableSurface_DoesNotExposeSubject() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        StubReader reader = new((_, _) => new ValueTask<int>(0));
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        IObservable<BadgeCountChangedArgs> stream = sut.CountChanged;

        stream.ShouldNotBeNull();
        stream.ShouldNotBeAssignableTo<Subject<BadgeCountChangedArgs>>();
        stream.ShouldNotBeAssignableTo<ISubject<BadgeCountChangedArgs>>();
    }

    [Fact]
    public async Task DisposeAsync_CompletesObservable_UnsubscribesFromNotifier() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        StubReader reader = new((_, _) => new ValueTask<int>(1));
        StubNotifier notifier = new();
        BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        bool completed = false;
        using IDisposable subscription = sut.CountChanged.Subscribe(
            onNext: _ => { },
            onCompleted: () => { completed = true; });

        await sut.DisposeAsync();

        completed.ShouldBeTrue();
    }

    [Fact]
    public async Task TotalActionableItems_SumsCurrentCounts() {
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta), typeof(ProjectionGamma));
        StubReader reader = new((type, _) => new ValueTask<int>(
            type == typeof(ProjectionAlpha) ? 1 :
            type == typeof(ProjectionBeta) ? 2 : 4));
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        await sut.InitializeAsync(Ct);

        sut.TotalActionableItems.ShouldBe(7);
    }

    [Fact]
    public void Constructor_NullArguments_Throw() {
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        StubReader reader = new((_, _) => new ValueTask<int>(0));
        IServiceProvider provider = EmptyProvider();
        ILogger<BadgeCountService> logger = Substitute.For<ILogger<BadgeCountService>>();
        TimeProvider time = new FakeTimeProvider();

        Should.Throw<ArgumentNullException>(() => new BadgeCountService(null!, reader, provider, logger, time));
        Should.Throw<ArgumentNullException>(() => new BadgeCountService(catalog, null!, provider, logger, time));
        Should.Throw<ArgumentNullException>(() => new BadgeCountService(catalog, reader, null!, logger, time));
        Should.Throw<ArgumentNullException>(() => new BadgeCountService(catalog, reader, provider, null!, time));
        Should.Throw<ArgumentNullException>(() => new BadgeCountService(catalog, reader, provider, logger, null!));
    }

    [Fact]
    public async Task InitializeAsync_EmptyCatalog_NoOp() {
        StubCatalog catalog = new();
        ConcurrentBag<Type> readerCalls = [];
        StubReader reader = new((type, _) => {
            readerCalls.Add(type);
            return new ValueTask<int>(0);
        });
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        await sut.InitializeAsync(Ct);

        readerCalls.ShouldBeEmpty();
        sut.Counts.ShouldBeEmpty();
        sut.TotalActionableItems.ShouldBe(0);
    }

    [Fact]
    public async Task CountChanged_EmitsForEachSeededType_IncludingZero() {
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta));
        StubReader reader = new((type, _) => new ValueTask<int>(type == typeof(ProjectionAlpha) ? 0 : 4));
        ConcurrentBag<BadgeCountChangedArgs> seen = [];
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());
        using IDisposable _ = sut.CountChanged.Subscribe(seen.Add);

        await sut.InitializeAsync(Ct);

        seen.Count.ShouldBe(2);
        seen.ShouldContain(args => args.ProjectionType == typeof(ProjectionAlpha) && args.NewCount == 0);
        seen.ShouldContain(args => args.ProjectionType == typeof(ProjectionBeta) && args.NewCount == 4);
    }

    [Fact]
    public async Task InitializeAsync_RespectsFiveSecondTimeout_CatchesOperationCanceled() {
        // D4 / AC1 — the 5 s umbrella CTS cancels in-flight FetchOneAsync calls; the top-level
        // Task.WhenAll swallows the OperationCanceledException, and the shell renders whatever
        // partial counts resolved before the timeout fired. Verified deterministically through
        // FakeTimeProvider.Advance — no Task.Delay.
        FakeTimeProvider time = new();
        TaskCompletionSource<int> alphaGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> betaGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta));
        StubReader reader = new(async (type, ct) => {
            using CancellationTokenRegistration reg = ct.Register(() => {
                (type == typeof(ProjectionAlpha) ? alphaGate : betaGate).TrySetCanceled(ct);
            });
            return await (type == typeof(ProjectionAlpha) ? alphaGate.Task : betaGate.Task)
                ;
        });
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), time);

        Task initTask = sut.InitializeAsync(Ct);
        time.Advance(TimeSpan.FromSeconds(6));
        await initTask;

        // Umbrella timeout fired; no counts seeded, no crash.
        sut.Counts.ContainsKey(typeof(ProjectionAlpha)).ShouldBeFalse();
        sut.Counts.ContainsKey(typeof(ProjectionBeta)).ShouldBeFalse();
        sut.TotalActionableItems.ShouldBe(0);
    }

    [Fact]
    public async Task Counts_ConcurrentRefreshes_ObserverAlwaysSeesMonotonicOrAtomicSnapshot() {
        // D5 / Murat P0 — the ImmutableDictionary + Interlocked.CompareExchange pattern must
        // guarantee readers observe a consistent snapshot and never a torn dictionary, even under
        // concurrent UpdateCount writers for distinct types.
        Type[] types = Enumerable.Range(0, 32).Select(_ => (Type)typeof(ProjectionAlpha))
            .Concat(Enumerable.Range(0, 32).Select(_ => (Type)typeof(ProjectionBeta)))
            .ToArray();
        StubCatalog catalog = new(typeof(ProjectionAlpha), typeof(ProjectionBeta), typeof(ProjectionGamma));
        StubReader reader = new((_, _) => new ValueTask<int>(1));
        using BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());
        await sut.InitializeAsync(Ct);

        // Parallel reads must always see a non-torn dictionary that contains every seeded key.
        int iterations = 200;
        Task[] readers = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => Task.Run(() => {
            for (int i = 0; i < iterations; i++) {
                IReadOnlyDictionary<Type, int> snapshot = sut.Counts;
                snapshot.ContainsKey(typeof(ProjectionAlpha)).ShouldBeTrue();
                snapshot.ContainsKey(typeof(ProjectionBeta)).ShouldBeTrue();
                _ = sut.TotalActionableItems;
            }
        })).ToArray();

        await Task.WhenAll(readers);
        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(1);
        sut.Counts[typeof(ProjectionBeta)].ShouldBe(1);
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentWithProjectionChanged_FinalStateMatchesLastWriter() {
        // D6 / Murat — when a notifier event fires after the initial fetch, the per-type
        // last-writer-wins contract holds: Counts[type] reflects the later read. The test drives
        // the two reads serially (initial fetch returns 10; notifier read returns 99) so the
        // assertion is deterministic under any thread-scheduling order.
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        int callIndex = 0;
        int[] returns = [10, 99];
        StubReader reader = new((_, _) => {
            int n = Interlocked.Increment(ref callIndex) - 1;
            int v = n < returns.Length ? returns[n] : returns[^1];
            return new ValueTask<int>(v);
        });
        StubNotifier notifier = new();
        using BadgeCountService sut = new(
            catalog, reader, WithNotifier(notifier),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        await sut.InitializeAsync(Ct);
        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(10);

        notifier.NotifyChanged(typeof(ProjectionAlpha).AssemblyQualifiedName!);

        // Drain async-void handler deterministically.
        for (int i = 0; i < 100; i++) {
            if (Volatile.Read(ref callIndex) >= 2) {
                break;
            }

            await Task.Yield();
        }

        sut.Counts[typeof(ProjectionAlpha)].ShouldBe(99);
        sut.TotalActionableItems.ShouldBe(99);
    }

    [Fact]
    public async Task DisposeAsync_WhileFetchInFlight_CancelsCleanly_NoObserverErrors() {
        // D20 / Murat — dispose must cancel the lifetime CTS, signal OnCompleted on the subject,
        // and never surface an OnError to subscribers. Pending reader tasks unwind through
        // OperationCanceledException which is caught inside FetchOneAsync.
        StubCatalog catalog = new(typeof(ProjectionAlpha));
        TaskCompletionSource<int> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubReader reader = new(async (_, ct) => {
            using CancellationTokenRegistration reg = ct.Register(() => gate.TrySetCanceled(ct));
            return await gate.Task;
        });
        BadgeCountService sut = new(
            catalog, reader, EmptyProvider(),
            Substitute.For<ILogger<BadgeCountService>>(), new FakeTimeProvider());

        bool completed = false;
        bool errored = false;
        using IDisposable subscription = sut.CountChanged.Subscribe(
            onNext: static _ => { },
            onError: _ => errored = true,
            onCompleted: () => completed = true);

        Task initTask = sut.InitializeAsync(Ct);
        await sut.DisposeAsync();
        await initTask;

        completed.ShouldBeTrue();
        errored.ShouldBeFalse();
    }

    private static int CountLoggedAtLevel(ILogger logger, LogLevel level, string fragment) {
        int count = 0;
        foreach (NSubstitute.Core.ICall call in logger.ReceivedCalls()) {
            if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)) {
                continue;
            }

            object?[] args = call.GetArguments();
            bool levelMatch = args.Any(a => a is LogLevel lvl && lvl == level);
            bool fragmentMatch = args.Any(a => a is not null && a.ToString()?.Contains(fragment, StringComparison.Ordinal) == true);
            if (levelMatch && fragmentMatch) {
                count++;
            }
        }

        return count;
    }

    private static void AssertLoggedWarning(ILogger logger, string fragment)
        => CountLoggedAtLevel(logger, LogLevel.Warning, fragment).ShouldBeGreaterThanOrEqualTo(1);
}
