using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FSharp.Core;

using Shouldly;

using FArb = FsCheck.Fluent.Arb;
using FGen = FsCheck.Fluent.Gen;
using FProp = FsCheck.Fluent.Prop;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Lifecycle;

/// <summary>
/// Story 10-4 — command idempotency and replay-safety properties for <see cref="LifecycleStateService"/>.
/// Oracle: a fresh service run over the same synthetic sequence is the reference model for replay determinism,
/// and terminal notifications are counted as the observable user-visible outcome.
/// </summary>
[Trait("Category", "Property")]
[Trait("Category", "LifecycleIdempotency")]
public sealed class CommandIdempotencyPropertyTests {
    private const string DefaultReplay = "15485863,32452843,0";
    private static readonly string[] Correlations = ["corr-0", "corr-1", "corr-2", "corr-3"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
        PropertyNameCaseInsensitive = true,
    };

    [Property(
        MaxTest = 1000,
        EndSize = 64,
        MaxRejected = 0,
        Parallelism = 1,
        Replay = DefaultReplay,
        Arbitrary = [typeof(CommandSequenceArbitraries)])]
    public void Replay_preserves_terminal_outcomes_and_transition_stream(CommandSequence sequence) {
        ArgumentNullException.ThrowIfNull(sequence);

        Evaluation original = Evaluate(sequence);
        Evaluation replay = Evaluate(sequence);

        replay.FinalStates.ShouldBe(original.FinalStates);
        replay.MessageIds.ShouldBe(original.MessageIds);
        replay.VisibleOutcomeCounts.ShouldBe(original.VisibleOutcomeCounts);
        replay.IdempotentTerminalCounts.ShouldBe(original.IdempotentTerminalCounts);
        replay.TransitionTrace.ShouldBe(original.TransitionTrace);
        replay.WarningCount.ShouldBe(original.WarningCount);
        replay.InvalidTransitionCount.ShouldBe(original.InvalidTransitionCount);
    }

    [Fact]
    [Trait("Category", "NightlyProperty")]
    public void Nightly_replay_preserves_terminal_outcomes_and_transition_stream() {
        PropertyReplaySeed replaySeed = PropertyReplaySeed.FromEnvironmentOrRandom();
        Config config = Config.QuickThrowOnFailure
            .WithMaxTest(GetPropertyMaxTest(defaultValue: 10000))
            .WithMaxRejected(0)
            .WithEndSize(96)
            .WithReplay(FSharpOption<Replay>.Some(replaySeed.ToReplay()))
            .WithArbitrary([typeof(CommandSequenceArbitraries)]);

        Check.One(
            $"Nightly replay preserves lifecycle outcomes; seed={replaySeed}",
            config,
            FProp.ForAll(
                CommandSequenceArbitraries.CommandSequence(),
                (CommandSequence sequence) => Replay_preserves_terminal_outcomes_and_transition_stream(sequence)));
    }

    [Property(
        MaxTest = 1000,
        EndSize = 64,
        MaxRejected = 0,
        Parallelism = 1,
        Replay = DefaultReplay,
        Arbitrary = [typeof(CommandSequenceArbitraries)])]
    public void Exactly_one_visible_terminal_outcome_per_correlation(CommandSequence sequence) {
        ArgumentNullException.ThrowIfNull(sequence);

        Evaluation result = Evaluate(sequence);

        foreach ((string correlation, int count) in result.VisibleOutcomeCounts) {
            count.ShouldBeLessThanOrEqualTo(
                1,
                $"Correlation={correlation}; Seed={DefaultReplay}; Operations={sequence.RedactedReplay}");
        }
    }

    [Property(MaxTest = 1000, EndSize = 32, MaxRejected = 0, Parallelism = 1, Replay = DefaultReplay)]
    public void Cross_correlation_duplicate_message_id_is_warned_and_fresh(NonNegativeInt salt) {
        ArgumentNullException.ThrowIfNull(salt);
        string shared = "msg-shared-" + (salt.Item % 8);
        CommandSequence sequence = new([
            new CommandOperation(OperationKind.Submit, 0, 0, false),
            new CommandOperation(OperationKind.Acknowledge, 0, 0, false),
            new CommandOperation(OperationKind.Confirmed, 0, 0, false),
            new CommandOperation(OperationKind.Submit, 1, 0, false),
            new CommandOperation(OperationKind.Acknowledge, 1, 0, false),
        ], MessageOverride: shared);

        Evaluation result = Evaluate(sequence);

        result.FinalStates["corr-0"].ShouldBe(CommandLifecycleState.Confirmed);
        result.FinalStates["corr-1"].ShouldBe(CommandLifecycleState.Acknowledged);
        result.MessageIds["corr-1"].ShouldBe(shared);
        result.WarningCount.ShouldBeGreaterThanOrEqualTo(1);
        result.UnsafeLogFragments.ShouldBeEmpty();
    }

    [Property(
        MaxTest = 1000,
        EndSize = 64,
        MaxRejected = 0,
        Parallelism = 1,
        Replay = DefaultReplay,
        Arbitrary = [typeof(CommandSequenceArbitraries)])]
    public void Reconnect_retry_and_stale_observations_keep_terminal_states_monotonic(CommandSequence sequence) {
        ArgumentNullException.ThrowIfNull(sequence);

        Evaluation result = Evaluate(sequence);

        foreach ((string correlation, IReadOnlyList<CommandLifecycleState> states) in result.StatesByCorrelation) {
            bool sawTerminal = false;
            foreach (CommandLifecycleState state in states) {
                if (state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected) {
                    sawTerminal = true;
                    continue;
                }

                if (sawTerminal && state != CommandLifecycleState.Idle) {
                    throw new Xunit.Sdk.XunitException(
                        $"Terminal state regressed. Correlation={correlation}; State={state}; Seed={DefaultReplay}; Operations={sequence.RedactedReplay}");
                }
            }
        }
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Counterexample_fixtures_replay_as_deterministic_regressions() {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "../../../Infrastructure/Lifecycle/Fixtures/command-idempotency-counterexamples.json");
        path = Path.GetFullPath(path);

        CounterexampleCatalog catalog = JsonSerializer.Deserialize<CounterexampleCatalog>(
            File.ReadAllText(path),
            JsonOptions) ?? throw new InvalidOperationException("Could not read counterexample catalog.");

        foreach (CounterexampleFixture fixture in catalog.Fixtures) {
            CommandSequence sequence = CommandSequence.FromFixture(fixture);
            Evaluation result = Evaluate(sequence);
            string correlation = fixture.MinimalSequence[0].Correlation;

            result.FinalStates[correlation].ToString().ShouldBe(fixture.ExpectedTerminalState);
            result.VisibleOutcomeCounts[correlation].ShouldBe(fixture.ExpectedVisibleOutcomeCount);
            result.UnsafeLogFragments.ShouldBeEmpty();
        }
    }

    private static Evaluation Evaluate(CommandSequence sequence) {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero));
        CapturingLifecycleLogger logger = new();
        using LifecycleStateService service = new(
            global::Microsoft.Extensions.Options.Options.Create(new LifecycleOptions { MessageIdCacheCapacity = 32 }),
            time,
            logger);

        Dictionary<string, List<CommandLifecycleTransition>> transitions = Correlations.ToDictionary(c => c, _ => new List<CommandLifecycleTransition>());
        List<IDisposable> subscriptions = [];
        foreach (string correlation in Correlations) {
            subscriptions.Add(service.Subscribe(correlation, transitions[correlation].Add));
        }

        try {
            foreach (CommandOperation operation in sequence.Operations) {
                Apply(service, operation, sequence.MessageOverride);
                time.Advance(TimeSpan.FromMilliseconds(1));
            }
        }
        finally {
            foreach (IDisposable subscription in subscriptions) {
                subscription.Dispose();
            }
        }

        Dictionary<string, CommandLifecycleState> finalStates = Correlations.ToDictionary(c => c, service.GetState);
        Dictionary<string, string?> messageIds = Correlations.ToDictionary(c => c, service.GetMessageId);
        Dictionary<string, int> visibleOutcomes = transitions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count(t => IsTerminal(t.NewState) && !t.IdempotencyResolved));
        Dictionary<string, int> idempotentTerminals = transitions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count(t => IsTerminal(t.NewState) && t.IdempotencyResolved));
        Dictionary<string, IReadOnlyList<CommandLifecycleState>> statesByCorrelation = transitions.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<CommandLifecycleState>) pair.Value.Select(t => t.NewState).ToArray());
        string[] trace = transitions
            .SelectMany(pair => pair.Value.Select(t => $"{pair.Key}:{t.PreviousState}->{t.NewState}:idempotent={t.IdempotencyResolved}:message={Redact(t.MessageId)}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] unsafeLogs = logger.Messages
            .Select(TestUnsafeText)
            .Where(static value => value is not null)
            .Select(static value => value!)
            .ToArray();

        return new Evaluation(
            finalStates,
            messageIds,
            visibleOutcomes,
            idempotentTerminals,
            trace,
            statesByCorrelation,
            logger.Messages.Count(m => m.Contains("HFC2005", StringComparison.Ordinal)),
            logger.Messages.Count(m => m.Contains("HFC2004", StringComparison.Ordinal)),
            unsafeLogs);
    }

    private static void Apply(LifecycleStateService service, CommandOperation operation, string? messageOverride) {
        string correlation = Correlations[operation.CorrelationIndex % Correlations.Length];
        string messageId = messageOverride ?? $"msg-{operation.MessageIndex % 6}";
        switch (operation.Kind) {
            case OperationKind.Submit:
                service.Transition(correlation, CommandLifecycleState.Submitting, messageId);
                break;
            case OperationKind.Acknowledge:
                service.Transition(correlation, CommandLifecycleState.Acknowledged, messageId);
                break;
            case OperationKind.Syncing:
                service.Transition(correlation, CommandLifecycleState.Syncing, messageId);
                break;
            case OperationKind.Confirmed:
                service.Transition(correlation, CommandLifecycleState.Confirmed, messageId, operation.IdempotencyResolved);
                break;
            case OperationKind.Rejected:
                service.Transition(correlation, CommandLifecycleState.Rejected, messageId, operation.IdempotencyResolved);
                break;
            case OperationKind.DuplicateTerminal:
                service.Transition(correlation, CommandLifecycleState.Confirmed, messageId, operation.IdempotencyResolved);
                service.Transition(correlation, CommandLifecycleState.Confirmed, messageId, idempotencyResolved: true);
                break;
            case OperationKind.ReconnectObservation:
                service.Transition(correlation, CommandLifecycleState.Syncing, messageId);
                break;
            case OperationKind.RetryObservation:
                service.Transition(correlation, CommandLifecycleState.Submitting, messageId);
                break;
            case OperationKind.StaleObservation:
                service.Transition(correlation, CommandLifecycleState.Acknowledged, messageId);
                break;
            case OperationKind.ResetToIdle:
                service.Transition(correlation, CommandLifecycleState.Idle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static bool IsTerminal(CommandLifecycleState state) =>
        state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;

    private static int GetPropertyMaxTest(int defaultValue) =>
        int.TryParse(Environment.GetEnvironmentVariable("FC_PROPERTY_MAX_TEST"), out int value) && value > 0
            ? value
            : defaultValue;

    private sealed record PropertyReplaySeed(ulong Seed, ulong Gamma, int Size) {
        public static PropertyReplaySeed FromEnvironmentOrRandom() {
            string? configured = Environment.GetEnvironmentVariable("FC_PROPERTY_REPLAY");
            if (!string.IsNullOrWhiteSpace(configured) && TryParse(configured, out PropertyReplaySeed? parsed)) {
                return parsed!;
            }

            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            ulong seed = BinaryPrimitives.ReadUInt64LittleEndian(bytes[..8]);
            ulong gamma = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]) | 1UL;
            return new(seed, gamma, Size: 0);
        }

        public Replay ToReplay() =>
            new(new Rnd(Seed, Gamma), FSharpOption<int>.Some(Size));

        public override string ToString() =>
            $"{Seed},{Gamma},{Size}";

        private static bool TryParse(string value, out PropertyReplaySeed? replaySeed) {
            string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 3
                && ulong.TryParse(parts[0], out ulong seed)
                && ulong.TryParse(parts[1], out ulong gamma)
                && int.TryParse(parts[2], out int size)) {
                replaySeed = new(seed, gamma, size);
                return true;
            }

            replaySeed = null;
            return false;
        }
    }

    private static string Redact(string? value) =>
        value is null ? "<none>" : value.StartsWith("msg-", StringComparison.Ordinal) ? value : "<redacted>";

    private static string? TestUnsafeText(string text) {
        if (text.Contains("C:\\Users\\", StringComparison.OrdinalIgnoreCase)) {
            return "machine-local path";
        }

        if (text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
            || text.Contains("password", StringComparison.OrdinalIgnoreCase)
            || text.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || text.Contains("token", StringComparison.OrdinalIgnoreCase)) {
            return "secret-like value";
        }

        return null;
    }

    private sealed record Evaluation(
        IReadOnlyDictionary<string, CommandLifecycleState> FinalStates,
        IReadOnlyDictionary<string, string?> MessageIds,
        IReadOnlyDictionary<string, int> VisibleOutcomeCounts,
        IReadOnlyDictionary<string, int> IdempotentTerminalCounts,
        IReadOnlyList<string> TransitionTrace,
        IReadOnlyDictionary<string, IReadOnlyList<CommandLifecycleState>> StatesByCorrelation,
        int WarningCount,
        int InvalidTransitionCount,
        IReadOnlyList<string> UnsafeLogFragments);

    public static class CommandSequenceArbitraries {
        public static Arbitrary<CommandSequence> CommandSequence() {
            Gen<CommandOperation> operation = FGen.Select(
                FGen.Choose(0, 4096),
                CommandOperation.FromToken);
            Gen<CommandSequence> sequence = FGen.Select(
                FGen.NonEmptyListOf(operation),
                operations => new CommandSequence(operations.Take(40).ToArray()));

            return FArb.From(sequence, Shrink);
        }

        private static IEnumerable<CommandSequence> Shrink(CommandSequence sequence) {
            if (sequence.Operations.Count <= 1) {
                yield break;
            }

            yield return new CommandSequence(sequence.Operations.Take(Math.Max(1, sequence.Operations.Count / 2)).ToArray());
            yield return new CommandSequence(sequence.Operations.Where(o => o.Kind != OperationKind.ResetToIdle).DefaultIfEmpty(sequence.Operations[0]).ToArray());
        }
    }

    public sealed record CommandSequence(
        IReadOnlyList<CommandOperation> Operations,
        string? MessageOverride = null) {
        public string RedactedReplay => string.Join(";", Operations.Select(static op => op.ToString()));

        internal static CommandSequence FromFixture(CounterexampleFixture fixture) =>
            new(fixture.MinimalSequence.Select(CommandOperation.FromFixture).ToArray());
    }

    public sealed record CommandOperation(
        OperationKind Kind,
        int CorrelationIndex,
        int MessageIndex,
        bool IdempotencyResolved) {
        public static CommandOperation FromToken(int token) {
            int value = Math.Abs(token);
            return new CommandOperation(
                (OperationKind) (value % 10),
                (value / 10) % Correlations.Length,
                (value / 40) % 6,
                (value & 1) == 0);
        }

        internal static CommandOperation FromFixture(CounterexampleStep step) {
            int correlation = Array.IndexOf(Correlations, step.Correlation);
            if (correlation < 0) {
                correlation = 0;
            }

            int message = step.MessageId.StartsWith("msg-", StringComparison.Ordinal)
                && int.TryParse(step.MessageId.AsSpan(4), out int parsed)
                    ? parsed
                    : 0;

            return new CommandOperation(step.Operation, correlation, message, IdempotencyResolved: false);
        }

        public override string ToString() =>
            $"{Kind}(corr-{CorrelationIndex % Correlations.Length},msg-{MessageIndex % 6},idempotent={IdempotencyResolved})";
    }

    [JsonConverter(typeof(JsonStringEnumConverter<OperationKind>))]
    public enum OperationKind {
        Submit,
        Acknowledge,
        Syncing,
        Confirmed,
        Rejected,
        DuplicateTerminal,
        ReconnectObservation,
        RetryObservation,
        StaleObservation,
        ResetToIdle,
    }

    internal sealed record CounterexampleCatalog(CounterexampleFixture[] Fixtures);

    internal sealed record CounterexampleFixture(
        string Name,
        string PropertyName,
        string Seed,
        int Size,
        CounterexampleStep[] MinimalSequence,
        string ExpectedTerminalState,
        int ExpectedVisibleOutcomeCount,
        string RelatedBugOrRationale,
        string RetentionReason);

    internal sealed record CounterexampleStep(
        OperationKind Operation,
        string Correlation,
        string MessageId);

    private sealed class CapturingLifecycleLogger : ILogger<LifecycleStateService> {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Messages.Add(formatter(state, exception));
        }
    }
}
