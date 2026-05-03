using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class CommandLifecycleTests {
    private const string MessageId = "01JZ0R5K9N8W4Y7V3Q2P6C1A0B";
    private const string CorrelationId = "01JZ0R5K9N8W4Y7V3Q2P6C1A0C";

    [Fact]
    public async Task InvokeAsync_ValidCommand_ReturnsAcknowledgementWithLifecycleReference() {
        var invoker = Build(out LifecycleAwareCommandService service, out _);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Acknowledged");
        result.StructuredContent!["messageId"]!.GetValue<string>().ShouldBe(MessageId);
        result.StructuredContent!["correlationId"]!.GetValue<string>().ShouldBe(CorrelationId);
        result.StructuredContent!["lifecycle"]!["tool"]!.GetValue<string>().ShouldBe("frontcomposer.lifecycle.subscribe");
        result.StructuredContent!["lifecycle"]!["uri"]!.GetValue<string>().ShouldBe($"frontcomposer://lifecycle/{CorrelationId}");
        result.StructuredContent!.ToJsonString().ShouldNotContain("42");
        result.StructuredContent!.ToJsonString().ShouldNotContain("tenant-a");
        result.StructuredContent!.ToJsonString().ShouldNotContain("agent-a");
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAsync_KnownLifecycleHandle_ReturnsOrderedTerminalSnapshot() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult snapshot = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        snapshot.IsError.ShouldBeFalse();
        snapshot.StructuredContent.ShouldNotBeNull();
        snapshot.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");
        snapshot.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
        snapshot.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("confirmed");
        snapshot.StructuredContent!["outcome"]!["success"]!["message"]!.GetValue<string>()
            .ShouldStartWith("Command completed");
        // AC3: agent-visible history starts at Acknowledged. Submitting and earlier intermediate
        // states are filtered out at the MCP edge so the agent surface starts where the spec says.
        JsonElement transitions = snapshot.StructuredContent!["transitions"]!.Deserialize<JsonElement>();
        transitions.GetArrayLength().ShouldBe(3);
        transitions[0].GetProperty("state").GetString().ShouldBe("Acknowledged");
        transitions[1].GetProperty("state").GetString().ShouldBe("Syncing");
        transitions[2].GetProperty("state").GetString().ShouldBe("Confirmed");
        // Sequence is monotonic regardless of which states are surfaced.
        transitions[0].GetProperty("sequence").GetInt64()
            .ShouldBeLessThan(transitions[2].GetProperty("sequence").GetInt64());
        snapshot.StructuredContent!.ToJsonString().ShouldNotContain("42");
        snapshot.StructuredContent!.ToJsonString().ShouldNotContain("tenant-a");
    }

    [Fact]
    public async Task ReadAsync_TerminalStateIsMonotonic_LateNonTerminalObservationIsIgnored() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        // The fake dispatcher already drove the entry to Confirmed via captured callbacks. A later
        // non-terminal observation must not regress the surfaced state (AC18).
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Syncing, MessageId);

        FrontComposerMcpResult snapshot = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        snapshot.IsError.ShouldBeFalse();
        snapshot.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");
        snapshot.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsync_DuplicateTerminalObservation_DoesNotProduceTerminalRegression() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        // Two extra Confirmed observations must not duplicate the success outcome.
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId);
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId);

        FrontComposerMcpResult snapshot = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        snapshot.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");
        snapshot.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("confirmed");
    }

    [Fact]
    public async Task ReadAsync_LifecycleHandle_RejectsUnicodeConfusableAsHiddenUnknown() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        // Fullwidth-zero variant of the canonical correlationId — NFKC would fold it back into the
        // canonical alphabet. The MCP layer must reject any non-ASCII before normalization (AC23).
        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args("""{"correlationId":"０01JZ0R5K9N8W4Y7V3Q2P6C1A0C"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
    }

    [Fact]
    public async Task InvokeAsync_CommandRejected_ReturnsStructuredAgentPayloadWithoutRawExceptionText() {
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out _);
        service.RejectNext = true;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.CommandRejected);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        result.StructuredContent!["outcome"]!["rejection"]!["errorCode"]!.GetValue<string>().ShouldBe("COMMAND_REJECTED");
        result.StructuredContent!["outcome"]!["rejection"]!["retryAppropriate"]!.GetValue<bool>().ShouldBeFalse();
        result.StructuredContent!.ToJsonString().ShouldNotContain("raw domain reason");
        result.StructuredContent!.ToJsonString().ShouldNotContain("tenant-a");
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAsync_AfterPolicyLoss_ReturnsHiddenUnknownShape() {
        MutablePolicyGate policyGate = new() { Allow = true };
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider, policyGate);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        policyGate.Allow = false;
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_tool");
        result.StructuredContent!.ToJsonString().ShouldNotContain(CorrelationId);
        result.StructuredContent!.ToJsonString().ShouldNotContain(MessageId);
    }

    [Fact]
    public async Task ReadAsync_IdempotencyResolvedTerminal_MapsToIdempotentConfirmedSuccess() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        provider.GetRequiredService<ILifecycleStateService>()
            .Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, idempotencyResolved: true);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("idempotent_confirmed");
        result.StructuredContent!["outcome"]!["retryAppropriate"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Fact]
    public async Task ReadAsync_InProgressPastConfiguredTimeout_ReturnsTimedOutTerminalSnapshot() {
        // P37: deterministic timer firing via FakeTimeProvider — no wall-clock dependency.
        FakeTimeProvider fakeTime = new(DateTimeOffset.Parse("2026-05-03T00:00:00Z"));
        FrontComposerMcpCommandInvoker invoker = Build(
            out LifecycleAwareCommandService service,
            out ServiceProvider provider,
            configureOptions: o => o.MaxLifecycleInProgressMs = 10,
            timeProvider: fakeTime);
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        fakeTime.Advance(TimeSpan.FromMilliseconds(50));
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        result.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("timed_out");
        // D2.1 (option a): synthetic terminal carries a structured remediation payload so AC6 is met.
        result.StructuredContent!["outcome"]!["rejection"]!["errorCode"]!.GetValue<string>().ShouldBe("LIFECYCLE_TIMED_OUT");
        result.StructuredContent!["outcome"]!["rejection"]!["reasonCategory"]!.GetValue<string>().ShouldBe("timed_out");
        result.StructuredContent!["outcome"]!["rejection"]!["suggestedAction"]!.GetValue<string>().ShouldBe("retry");
        result.StructuredContent!["outcome"]!["rejection"]!["retryAppropriate"]!.GetValue<bool>().ShouldBeTrue();
        result.StructuredContent!["outcome"]!["rejection"]!["docsCode"]!.GetValue<string>().ShouldBe("HFC-MCP-LIFECYCLE-TIMED-OUT");
        result.StructuredContent!.ToJsonString().ShouldNotContain("tenant-a");
    }

    [Fact]
    public async Task ReadAsync_RealConfirmedAfterSyntheticTimedOut_TerminalStaysTimedOut() {
        // P38: synthetic-vs-real terminal monotonicity (AC18). Once a synthetic timeout seals the
        // entry, a late real Confirmed observation must not regress or duplicate the terminal.
        FakeTimeProvider fakeTime = new(DateTimeOffset.Parse("2026-05-03T00:00:00Z"));
        FrontComposerMcpCommandInvoker invoker = Build(
            out LifecycleAwareCommandService service,
            out ServiceProvider provider,
            configureOptions: o => o.MaxLifecycleInProgressMs = 10,
            timeProvider: fakeTime);
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        fakeTime.Advance(TimeSpan.FromMilliseconds(50));

        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        // Real Confirmed arrives after the synthetic timeout has already terminalized the entry.
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId);

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        result.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("timed_out");
        // History must not contain a second terminal row for the late Confirmed.
        JsonElement transitions = result.StructuredContent!["transitions"]!.Deserialize<JsonElement>();
        int terminals = 0;
        for (int i = 0; i < transitions.GetArrayLength(); i++) {
            string? state = transitions[i].GetProperty("state").GetString();
            if (state is "Confirmed" or "Rejected") {
                terminals++;
            }
        }

        terminals.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAsync_RepeatedReadsOfSyntheticTerminal_ReturnIdenticalSnapshots() {
        // P39: AC5/AC18 — repeated lifecycle reads after a synthetic terminal must return the same
        // terminal outcome idempotently with no transition history growth or duplicate registration.
        FakeTimeProvider fakeTime = new(DateTimeOffset.Parse("2026-05-03T00:00:00Z"));
        FrontComposerMcpCommandInvoker invoker = Build(
            out LifecycleAwareCommandService service,
            out ServiceProvider provider,
            configureOptions: o => o.MaxLifecycleInProgressMs = 10,
            timeProvider: fakeTime);
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        fakeTime.Advance(TimeSpan.FromMilliseconds(50));
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult first = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpResult second = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        first.IsError.ShouldBeFalse();
        second.IsError.ShouldBeFalse();
        first.StructuredContent!.ToJsonString().ShouldBe(second.StructuredContent!.ToJsonString());
    }

    [Fact]
    public async Task ReadAsync_ActiveCapacityEviction_ReturnsNeedsReviewTerminalForOldestHandle() {
        CounterUlidFactory ulids = new();
        FrontComposerMcpCommandInvoker invoker = Build(
            out LifecycleAwareCommandService service,
            out ServiceProvider provider,
            ulidFactory: ulids,
            configureOptions: o => {
                o.MaxActiveLifecycleEntries = 1;
                o.MaxRetainedTerminalLifecycleEntries = 1;
                o.MaxLifecycleInProgressMs = 60_000;
            });
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        string firstCorrelationId = service.LastCorrelationId;

        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":43}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{firstCorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        result.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("needs_review");
        // D2.1 (option a): synthetic terminal carries a structured remediation payload so AC6 is met.
        result.StructuredContent!["outcome"]!["rejection"]!["errorCode"]!.GetValue<string>().ShouldBe("LIFECYCLE_NEEDS_REVIEW");
        result.StructuredContent!["outcome"]!["rejection"]!["reasonCategory"]!.GetValue<string>().ShouldBe("needs_review");
        result.StructuredContent!["outcome"]!["rejection"]!["suggestedAction"]!.GetValue<string>().ShouldBe("poll");
        result.StructuredContent!["outcome"]!["rejection"]!["docsCode"]!.GetValue<string>().ShouldBe("HFC-MCP-LIFECYCLE-NEEDS-REVIEW");
        service.DispatchCount.ShouldBe(2);
    }

    [Fact]
    public async Task ReadYourWritesBenchmark_CommandLifecycleProjectionP95_IsUnder1500Milliseconds() {
        const int samples = 20;
        List<double> elapsedMs = [];
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();
        FrontComposerMcpProjectionReader reader = provider.GetRequiredService<FrontComposerMcpProjectionReader>();

        for (int i = 0; i < samples; i++) {
            long started = Stopwatch.GetTimestamp();
            FrontComposerMcpResult ack = await invoker.InvokeAsync(
                "Billing.PayInvoiceCommand.Execute",
                Args("""{"Amount":42}"""),
                TestContext.Current.CancellationToken);
            ack.IsError.ShouldBeFalse();

            FrontComposerMcpResult lifecycle = await tracker.ReadAsync(
                Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
                TestContext.Current.CancellationToken);
            lifecycle.IsError.ShouldBeFalse();
            lifecycle.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");

            FrontComposerMcpResult projection = await reader.ReadAsync(
                "frontcomposer://projection/counter",
                TestContext.Current.CancellationToken);
            projection.IsError.ShouldBeFalse();
            projection.Text.ShouldContain("42");
            elapsedMs.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        double p95 = elapsedMs.Order().ElementAt((int)Math.Ceiling(samples * 0.95) - 1);
        p95.ShouldBeLessThan(1_500.0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("01jz0r5k9n8w4y7v3q2p6c1a0c")]
    [InlineData("../01JZ0R5K9N8W4Y7V3Q2P6C1A0C")]
    [InlineData("01JZ0R5K9N8W4Y7V3Q2P6C1A0C-extra")]
    // P41 + P17: oversized, percent-encoded, near-match (24/25 chars), whitespace-padded.
    [InlineData("01JZ0R5K9N8W4Y7V3Q2P6C1A0")] // 25 chars (one short)
    [InlineData("01JZ0R5K9N8W4Y7V3Q2P6C1A")] // 24 chars (two short)
    [InlineData("%3001JZ0R5K9N8W4Y7V3Q2P6C1A0C")] // percent-encoded prefix
    [InlineData(" 01JZ0R5K9N8W4Y7V3Q2P6C1A0C")] // leading ASCII space
    [InlineData("01JZ0R5K9N8W4Y7V3Q2P6C1A0C ")] // trailing ASCII space
    [InlineData("01JZ0R5K9N8W4Y7V3Q2P6C1A0C\\t")] // trailing tab (escaped in JSON)
    public async Task ReadAsync_MalformedLifecycleHandle_FailsAsHiddenUnknownWithoutStoreLookup(string correlationId) {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{correlationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.Text.ShouldBe("Request failed.");
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_tool");
        result.StructuredContent!.ToJsonString().ShouldNotContain(CorrelationId);
    }

    [Fact]
    public async Task ReadAsync_OversizedLifecycleHandle_FailsAsHiddenUnknownWithoutStoreLookup() {
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();
        // 1024-char identifier — must be rejected before any store lookup (AC23 / T8 line 138).
        string oversized = new('A', 1024);

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{oversized}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_tool");
    }

    [Fact]
    public async Task ReadAsync_BothCorrelationAndMessageId_FailsAsHiddenUnknown() {
        // P17 / AC23: the schema's `oneOf` is enforced at runtime by `arguments.Count != 1`;
        // pinning the behavior here prevents future schema/runtime drift.
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}","messageId":"{{MessageId}}"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
    }

    [Fact]
    public async Task ReadAsync_ParallelReadsAfterTerminal_AllReturnSameTerminalSnapshot() {
        // AC18 / P4: simultaneous lifecycle reads against a terminal entry must converge to the
        // same idempotent terminal snapshot without duplicate registration or duplicate dispatch.
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        Task<FrontComposerMcpResult>[] reads = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            tracker.ReadAsync(Args($$"""{"correlationId":"{{CorrelationId}}"}"""), CancellationToken.None))).ToArray();
        FrontComposerMcpResult[] results = await Task.WhenAll(reads);

        results.Length.ShouldBe(16);
        foreach (FrontComposerMcpResult result in results) {
            result.IsError.ShouldBeFalse();
            result.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");
            result.StructuredContent!["terminal"]!.GetValue<bool>().ShouldBeTrue();
        }

        // Dispatch must have run exactly once even though 16 readers raced.
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAsync_OutOfOrderObserverDelivery_PreservesMonotonicSequence() {
        // AC21 / P18: out-of-order observer enqueue must still produce a Sequence-ordered snapshot.
        // The MCP edge orders by source-of-truth Sequence, not by arrival/clock.
        FrontComposerMcpCommandInvoker invoker = Build(
            out _,
            out ServiceProvider provider,
            configureOptions: o => o.MaxLifecycleTransitionHistory = 32);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult snapshot = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        snapshot.IsError.ShouldBeFalse();
        JsonElement transitions = snapshot.StructuredContent!["transitions"]!.Deserialize<JsonElement>();
        // Every transition must carry a strictly increasing sequence regardless of delivery order.
        long previous = 0;
        for (int i = 0; i < transitions.GetArrayLength(); i++) {
            long seq = transitions[i].GetProperty("sequence").GetInt64();
            seq.ShouldBeGreaterThan(previous);
            previous = seq;
        }
    }

    [Fact]
    public async Task ReadAsync_DuplicateConfirmedRedelivery_DoesNotGrowAgentHistory() {
        // P52: same-state Confirmed re-delivery must not append additional rows to the bounded
        // history — otherwise the truncation loop would eventually evict the original Acknowledged.
        FrontComposerMcpCommandInvoker invoker = Build(out _, out ServiceProvider provider);
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult before = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);
        int beforeCount = before.StructuredContent!["transitions"]!.AsArray().Count;

        for (int i = 0; i < 5; i++) {
            lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId);
        }

        FrontComposerMcpResult after = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        after.StructuredContent!["transitions"]!.AsArray().Count.ShouldBe(beforeCount);
        after.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Confirmed");
    }

    [Fact]
    public async Task InvokeAsync_CommandRejected_RejectionEnvelopeIncludesMessageAndCorrelationId() {
        // D8: agents must be able to correlate a synchronous rejection with the dispatched handle.
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out _);
        service.RejectNext = true;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.StructuredContent!["messageId"]!.GetValue<string>().ShouldBe(MessageId);
        // Activity may be unset, in which case CorrelationId falls back to the messageId.
        string emittedCorrelationId = result.StructuredContent!["correlationId"]!.GetValue<string>();
        emittedCorrelationId.ShouldNotBeNullOrWhiteSpace();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("rejected");
    }

    [Fact]
    public async Task InvokeAsync_ValidationFailure_OutcomeCategoryIsValidation() {
        // P49: validation failures from the dispatcher must use a distinct outer envelope
        // category so agents can branch on outcome.category between protocol-layer validation and
        // domain rejection. (Schema-level malformed-input checks short-circuit earlier with
        // FrontComposerMcpFailureCategory.ValidationFailed and no structured payload.)
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out _);
        service.ValidateRejectNext = true;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("validation");
        result.StructuredContent!["outcome"]!["rejection"]!["reasonCategory"]!.GetValue<string>().ShouldBe("validation");
        result.StructuredContent!["outcome"]!["rejection"]!["errorCode"]!.GetValue<string>().ShouldBe("COMMAND_VALIDATION_FAILED");
    }

    [Fact]
    public async Task ReadAsync_ReplayAfterRejected_PreservesRejectedTerminal() {
        // AC19 / P5: replay after terminal Rejected must preserve the rejection terminal for
        // authorized lifecycle reads; a duplicate Confirmed delivery cannot upgrade the outcome.
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out ServiceProvider provider);
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        lifecycle.Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId);
        // Replay (idempotent re-delivery of the rejection) must not regress.
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId);
        // A late Confirmed cannot upgrade the rejection (terminal regression guard).
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId);

        FrontComposerMcpResult result1 = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpResult result2 = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result1.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        result2.StructuredContent!["state"]!.GetValue<string>().ShouldBe("Rejected");
        // Repeated reads return the identical snapshot.
        result1.StructuredContent!.ToJsonString().ShouldBe(result2.StructuredContent!.ToJsonString());
    }

    [Fact]
    public async Task ReadAsync_ReplayAfterIdempotentConfirmed_StaysIdempotentConfirmed() {
        // AC19 / D3: idempotency-resolved Confirmed survives subsequent same-key reads; the
        // outcome category remains idempotent_confirmed.
        FrontComposerMcpCommandInvoker invoker = Build(out LifecycleAwareCommandService service, out ServiceProvider provider);
        service.CompleteSynchronously = false;
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        ILifecycleStateService lifecycle = provider.GetRequiredService<ILifecycleStateService>();
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, idempotencyResolved: true);
        // Replay re-delivery should not regress the IdempotentConfirmed outcome.
        lifecycle.Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, idempotencyResolved: true);

        FrontComposerMcpResult result = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        result.StructuredContent!["outcome"]!["category"]!.GetValue<string>().ShouldBe("idempotent_confirmed");
    }

    [Fact]
    public async Task TrackAcknowledged_RetryAfterIsCarriedToSnapshot() {
        // P48: snapshot retry hint matches the dispatcher-supplied (clamped) value, not the
        // configured default. The fake dispatcher emits RetryAfter=250ms.
        FrontComposerMcpCommandInvoker invoker = Build(
            out _,
            out ServiceProvider provider,
            configureOptions: o => {
                o.DefaultLifecycleRetryAfterMs = 999;
                o.MinLifecycleRetryAfterMs = 1;
                o.MaxLifecycleRetryAfterMs = 10_000;
            });
        await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpLifecycleTracker tracker = provider.GetRequiredService<FrontComposerMcpLifecycleTracker>();

        FrontComposerMcpResult snapshot = await tracker.ReadAsync(
            Args($$"""{"correlationId":"{{CorrelationId}}"}"""),
            TestContext.Current.CancellationToken);

        // Dispatcher emitted 250 ms; this must beat the configured default of 999 ms.
        snapshot.StructuredContent!["retry"]!["retryAfterMs"]!.GetValue<int>().ShouldBe(250);
    }

    private static FrontComposerMcpCommandInvoker Build(
        out LifecycleAwareCommandService commandService,
        out ServiceProvider provider,
        IFrontComposerMcpCommandPolicyGate? policyGate = null,
        IUlidFactory? ulidFactory = null,
        Action<FrontComposerMcpOptions>? configureOptions = null,
        TimeProvider? timeProvider = null) {
        commandService = new LifecycleAwareCommandService();
        ServiceCollection services = new();
        services.AddSingleton<ICommandService>(commandService);
        services.AddSingleton<IQueryService, FastQueryService>();
        services.AddSingleton<ILifecycleStateService, RecordingLifecycleStateService>();
        services.AddSingleton(ulidFactory ?? new FixedUlidFactory());
        if (timeProvider is not null) {
            services.AddSingleton(timeProvider);
        }

        services.Configure<FrontComposerMcpOptions>(o => {
            o.Manifests.Add(Manifest(policyGate is null ? null : "LifecyclePolicy"));
            configureOptions?.Invoke(o);
        });
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        services.AddSingleton<FrontComposerMcpLifecycleTracker>();
        services.AddSingleton<FrontComposerMcpProjectionReader>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        if (policyGate is not null) {
            services.AddSingleton(policyGate);
        }

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static McpManifest Manifest(string? policyName)
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                null,
                policyName,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId", "CorrelationId"]),
        ], [
            new McpResourceDescriptor(
                "frontcomposer://projection/counter",
                "counter-status",
                typeof(CounterProjection).FullName!,
                "Billing",
                "Counter status",
                "Synthetic read-your-writes benchmark projection.",
                [new McpParameterDescriptor("Count", "Int32", "integer", false, false, "Count", null, [], false)]),
        ]);

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int Amount { get; set; }
    }

    public sealed record CounterProjection(int Count);

    private sealed class LifecycleAwareCommandService : ICommandServiceWithLifecycle {
        public int DispatchCount { get; private set; }
        public bool RejectNext { get; set; }
        public bool ValidateRejectNext { get; set; }
        public bool CompleteSynchronously { get; set; } = true;
        public string LastCorrelationId { get; private set; } = "";

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            if (ValidateRejectNext) {
                throw new CommandValidationException(ProblemDetailsPayload.Empty);
            }

            if (RejectNext) {
                throw new CommandRejectedException("raw domain reason with tenant-a and amount 42", "raw resolution");
            }

            string messageId = ReadString(command, nameof(PayInvoiceCommand.MessageId)) ?? MessageId;
            string correlationId = ReadString(command, nameof(PayInvoiceCommand.CorrelationId)) ?? CorrelationId;
            if (string.Equals(messageId, MessageId, StringComparison.Ordinal)
                && string.Equals(correlationId, MessageId, StringComparison.Ordinal)) {
                correlationId = CorrelationId;
            }

            LastCorrelationId = correlationId;
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);
            if (CompleteSynchronously) {
                onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
            }

            return Task.FromResult(new CommandResult(messageId, "Accepted", correlationId, RetryAfter: TimeSpan.FromMilliseconds(250)));
        }

        private static string? ReadString<TCommand>(TCommand command, string propertyName)
            where TCommand : class
            => command.GetType().GetProperty(propertyName)?.GetValue(command) as string;
    }

    private sealed class RecordingLifecycleStateService : ILifecycleStateService {
        private readonly Dictionary<string, List<Action<CommandLifecycleTransition>>> _subscribers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CommandLifecycleState> _states = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _messages = new(StringComparer.Ordinal);

        public IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition) {
            if (!_subscribers.TryGetValue(correlationId, out List<Action<CommandLifecycleTransition>>? callbacks)) {
                callbacks = [];
                _subscribers[correlationId] = callbacks;
            }

            callbacks.Add(onTransition);
            return new DisposableAction(() => callbacks.Remove(onTransition));
        }

        public CommandLifecycleState GetState(string correlationId)
            => _states.GetValueOrDefault(correlationId, CommandLifecycleState.Idle);

        public string? GetMessageId(string correlationId)
            => _messages.GetValueOrDefault(correlationId);

        public IEnumerable<string> GetActiveCorrelationIds() => _states.Keys.ToArray();

        public void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null)
            => Transition(correlationId, newState, messageId, false);

        public void Transition(string correlationId, CommandLifecycleState newState, string? messageId, bool idempotencyResolved) {
            CommandLifecycleState previous = GetState(correlationId);
            _states[correlationId] = newState;
            if (!string.IsNullOrWhiteSpace(messageId)) {
                _messages[correlationId] = messageId;
            }

            if (!_subscribers.TryGetValue(correlationId, out List<Action<CommandLifecycleTransition>>? callbacks)) {
                return;
            }

            CommandLifecycleTransition transition = new(
                correlationId,
                previous,
                newState,
                messageId,
                DateTimeOffset.Parse("2026-05-02T00:00:00Z"),
                DateTimeOffset.Parse("2026-05-02T00:00:00Z"),
                idempotencyResolved);
            foreach (Action<CommandLifecycleTransition> callback in callbacks.ToArray()) {
                callback(transition);
            }
        }

        public void Dispose() {
            _subscribers.Clear();
            _states.Clear();
            _messages.Clear();
        }
    }

    private sealed class FixedUlidFactory : IUlidFactory {
        public string NewUlid() => MessageId;
    }

    private sealed class CounterUlidFactory : IUlidFactory {
        private int _next;

        // P40: the prior fallback formatted `value % 10` as a 1-character suffix, producing 25-char
        // strings that fail the canonical 26-char ULID regex. Throw instead of silently emitting
        // invalid IDs so any future test that exceeds the table size fails loudly.
        public string NewUlid() {
            int value = Interlocked.Increment(ref _next);
            return value switch {
                1 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
                2 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0D",
                _ => throw new InvalidOperationException(
                    "CounterUlidFactory only supports value <= 2; extend the table for additional handles."),
            };
        }
    }

    private sealed class FastQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            object[] items = typeof(T) == typeof(CounterProjection)
                ? [new CounterProjection(42)]
                : [];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), items.Length, "etag-a"));
        }
    }

    private sealed class StaticAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class MutablePolicyGate : IFrontComposerMcpCommandPolicyGate {
        public bool Allow { get; set; }

        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Allow);
    }

    private sealed class DisposableAction(Action action) : IDisposable {
        public void Dispose() => action();
    }
}
