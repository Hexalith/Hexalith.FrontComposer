using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>
/// Story 1.1 AC2 — the bootstrap fail-fast guard. Verifies the
/// <see cref="FrontComposerBootstrapValidator"/> NAMES the missing/mis-ordered <c>AddHexalith*</c>
/// call, that the ordering markers register in call order through the real entry points, and that
/// the guard is idempotent against duplicate registration.
/// </summary>
public sealed class FrontComposerBootstrapGuardTests {
    // ── Validator unit tests (synthesised markers, in DI insertion order) ──────────────────────────

    [Fact]
    public void Validate_QuickstartDomainEventStore_DoesNotThrow()
        => Should.NotThrow(() => FrontComposerBootstrapValidator.Validate(Markers(
            FrontComposerBootstrapStage.Quickstart,
            FrontComposerBootstrapStage.Domain,
            FrontComposerBootstrapStage.EventStore)));

    [Fact]
    public void Validate_QuickstartOnly_DoesNotThrow()
        // AC3 — an empty shell with neither a domain nor an EventStore is a valid bootstrap.
        => Should.NotThrow(() => FrontComposerBootstrapValidator.Validate(Markers(
            FrontComposerBootstrapStage.Quickstart)));

    [Fact]
    public void Validate_QuickstartThenEventStore_NoDomain_DoesNotThrow()
        // AddHexalithDomain<T>() is optional — must NOT be required.
        => Should.NotThrow(() => FrontComposerBootstrapValidator.Validate(Markers(
            FrontComposerBootstrapStage.Quickstart,
            FrontComposerBootstrapStage.EventStore)));

    [Fact]
    public void Validate_EventStoreBeforeQuickstart_ThrowsNamingBothCalls() {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(Markers(
                FrontComposerBootstrapStage.EventStore,
                FrontComposerBootstrapStage.Quickstart)));

        ex.Message.ShouldContain("AddHexalithEventStore");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
        ex.Message.ShouldContain("mis-ordered");
    }

    [Fact]
    public void Validate_DomainBeforeQuickstart_ThrowsNamingBothCalls() {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(Markers(
                FrontComposerBootstrapStage.Domain,
                FrontComposerBootstrapStage.Quickstart)));

        ex.Message.ShouldContain("AddHexalithDomain");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
    }

    [Fact]
    public void Validate_EventStoreBeforeDomain_ThrowsNamingBoth() {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(Markers(
                FrontComposerBootstrapStage.Quickstart,
                FrontComposerBootstrapStage.EventStore,
                FrontComposerBootstrapStage.Domain)));

        ex.Message.ShouldContain("AddHexalithEventStore");
        ex.Message.ShouldContain("AddHexalithDomain");
    }

    [Fact]
    public void Validate_MissingQuickstart_OnlyEventStore_ThrowsNamingForgottenQuickstart() {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(Markers(
                FrontComposerBootstrapStage.EventStore)));

        ex.Message.ShouldContain("AddHexalithEventStore");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
        ex.Message.ShouldContain("incomplete");
    }

    [Fact]
    public void Validate_MissingQuickstart_OnlyDomain_ThrowsNamingForgottenQuickstart() {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(Markers(
                FrontComposerBootstrapStage.Domain)));

        ex.Message.ShouldContain("AddHexalithDomain");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
    }

    [Fact]
    public void Validate_NullMarkers_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => FrontComposerBootstrapValidator.Validate(null!));

    [Fact]
    public void Validate_NoMarkersAtAll_ThrowsNamingForgottenQuickstart() {
        // Defensive: an empty marker set (no FrontComposer entry point ran) is treated identically to
        // a missing foundational call — name AddHexalithFrontComposerQuickstart() as the fix.
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate([]));

        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
        ex.Message.ShouldContain("incomplete");
        // The empty path must NOT falsely claim a downstream call was made.
        ex.Message.ShouldNotContain("AddHexalithDomain");
        ex.Message.ShouldNotContain("was called but");
    }

    // ── Hosted-gate tests (the IHostedService that runs the validator at host start) ────────────────

    [Fact]
    public async Task Gate_StartAsync_ValidMarkers_DoesNotThrow() {
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.Quickstart, FrontComposerBootstrapStage.Domain),
            NullLogger<FrontComposerBootstrapValidationGate>.Instance);

        await Should.NotThrowAsync(() => gate.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Gate_StartAsync_MisorderedMarkers_ThrowsNamedInvalidOperationException() {
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.EventStore, FrontComposerBootstrapStage.Quickstart),
            NullLogger<FrontComposerBootstrapValidationGate>.Instance);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => gate.StartAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("AddHexalithEventStore");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
    }

    [Fact]
    public async Task Gate_StartAsync_MisorderedMarkers_LogsErrorWithNamedMessage_BeforeThrowing() {
        // Task 1 explicitly requires the gate to write the message to the logger AND throw it
        // (mirrors CustomizationContractValidationGate). Pin the log channel so the named diagnostic
        // is not silently dropped before the throw.
        CapturingLogger<FrontComposerBootstrapValidationGate> logger = new();
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.EventStore, FrontComposerBootstrapStage.Quickstart),
            logger);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => gate.StartAsync(TestContext.Current.CancellationToken));

        CapturingLogger<FrontComposerBootstrapValidationGate>.Entry error =
            logger.Entries.ShouldHaveSingleItem();
        error.Level.ShouldBe(LogLevel.Error);
        error.Message.ShouldContain("AddHexalithEventStore");
        error.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
    }

    [Fact]
    public async Task Gate_StartAsync_ValidMarkers_LogsNothing() {
        CapturingLogger<FrontComposerBootstrapValidationGate> logger = new();
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.Quickstart),
            logger);

        await Should.NotThrowAsync(() => gate.StartAsync(TestContext.Current.CancellationToken));

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Gate_StartAsync_CancelledToken_ThrowsOperationCanceled() {
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.Quickstart),
            NullLogger<FrontComposerBootstrapValidationGate>.Instance);

        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => gate.StartAsync(cts.Token));
    }

    [Fact]
    public async Task Gate_StopAsync_DoesNotThrow() {
        FrontComposerBootstrapValidationGate gate = new(
            Markers(FrontComposerBootstrapStage.Quickstart),
            NullLogger<FrontComposerBootstrapValidationGate>.Instance);

        await Should.NotThrowAsync(() => gate.StopAsync(TestContext.Current.CancellationToken));
    }

    // ── Real-entry-point registration tests ─────────────────────────────────────────────────────────

    [Fact]
    public void ThreeCallBootstrap_RegistersMarkersInQuickstartDomainEventStoreOrder() {
        ServiceCollection services = BuildThreeCall();

        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerBootstrapStage[] stages = [.. provider
            .GetRequiredService<IEnumerable<IFrontComposerBootstrapMarker>>()
            .Select(m => m.Stage)];

        stages.ShouldBe([
            FrontComposerBootstrapStage.Quickstart,
            FrontComposerBootstrapStage.Domain,
            FrontComposerBootstrapStage.EventStore,
        ]);
    }

    [Fact]
    public void Quickstart_RegistersSingleQuickstartMarker_AndSingleHostedGate() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();

        services.Count(d => d.ImplementationType == typeof(QuickstartBootstrapMarker)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(FrontComposerBootstrapValidationGate)).ShouldBe(1);
    }

    [Fact]
    public void DuplicateQuickstart_DoesNotDoubleRegisterMarkerOrGate() {
        // Edge case (a): calling Quickstart twice must not double-register or double-throw.
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithFrontComposerQuickstart();

        services.Count(d => d.ImplementationType == typeof(QuickstartBootstrapMarker)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(FrontComposerBootstrapValidationGate)).ShouldBe(1);

        using ServiceProvider provider = services.BuildServiceProvider();
        Should.NotThrow(() => FrontComposerBootstrapValidator.Validate(
            provider.GetRequiredService<IEnumerable<IFrontComposerBootstrapMarker>>()));
    }

    [Fact]
    public void MisorderedRealCalls_EventStoreBeforeQuickstart_ValidatorThrowsNamingBoth() {
        // End-to-end ordering detection through the real entry points (markers carry insertion order).
        ServiceCollection services = [];
        _ = services.AddHexalithEventStore(o => {
            o.BaseAddress = new Uri("http://localhost:9/");
            o.RequireAccessToken = false;
        });
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(
                provider.GetRequiredService<IEnumerable<IFrontComposerBootstrapMarker>>()));

        ex.Message.ShouldContain("AddHexalithEventStore");
        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
    }

    [Fact]
    public void EventStoreAlone_RegistersGate_AndValidatorThrowsNamingForgottenQuickstart() {
        // The headline AC2 "a required one is missing" case through the REAL wiring: a host that wires
        // only AddHexalithEventStore (forgetting AddHexalithFrontComposerQuickstart) must still fail
        // fast. The gate is registered by AddHexalithEventStore so StartAsync can run at host start,
        // and the validator names the forgotten foundational call instead of an opaque DI error.
        ServiceCollection services = [];
        _ = services.AddHexalithEventStore(o => {
            o.BaseAddress = new Uri("http://localhost:9/");
            o.RequireAccessToken = false;
        });

        services.Count(d => d.ImplementationType == typeof(FrontComposerBootstrapValidationGate)).ShouldBe(1);

        using ServiceProvider provider = services.BuildServiceProvider();
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => FrontComposerBootstrapValidator.Validate(
                provider.GetRequiredService<IEnumerable<IFrontComposerBootstrapMarker>>()));

        ex.Message.ShouldContain("AddHexalithFrontComposerQuickstart");
        ex.Message.ShouldContain("incomplete");
    }

    [Fact]
    public void GranularAddHexalithFrontComposer_RegistersQuickstartMarkerAndGate() {
        // The Quickstart ordering marker lives on the foundational AddHexalithFrontComposer() call, not
        // on the AddHexalithFrontComposerQuickstart() wrapper — so the granular 3-call path is guarded
        // identically. Pin that the foundational call alone produces a valid (non-throwing) bootstrap.
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposer();

        services.Count(d => d.ImplementationType == typeof(QuickstartBootstrapMarker)).ShouldBe(1);
        services.Count(d => d.ImplementationType == typeof(FrontComposerBootstrapValidationGate)).ShouldBe(1);

        using ServiceProvider provider = services.BuildServiceProvider();
        Should.NotThrow(() => FrontComposerBootstrapValidator.Validate(
            provider.GetRequiredService<IEnumerable<IFrontComposerBootstrapMarker>>()));
    }

    private static ServiceCollection BuildThreeCall() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithDomain<CounterDomain>();
        _ = services.AddHexalithEventStore(o => {
            o.BaseAddress = new Uri("http://localhost:9/");
            o.RequireAccessToken = false;
        });
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        return services;
    }

    private static IEnumerable<IFrontComposerBootstrapMarker> Markers(params FrontComposerBootstrapStage[] stages)
        => [.. stages.Select<FrontComposerBootstrapStage, IFrontComposerBootstrapMarker>(static stage => stage switch
        {
            FrontComposerBootstrapStage.Quickstart => new QuickstartBootstrapMarker(),
            FrontComposerBootstrapStage.Domain => new DomainBootstrapMarker(),
            FrontComposerBootstrapStage.EventStore => new EventStoreBootstrapMarker(),
            _ => throw new ArgumentOutOfRangeException(nameof(stages), stage, "Unknown bootstrap stage."),
        })];

    /// <summary>Minimal in-memory logger so the gate's "log AND throw" contract can be asserted.</summary>
    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(new Entry(logLevel, formatter(state, exception)));

        public sealed record Entry(LogLevel Level, string Message);
    }
}
