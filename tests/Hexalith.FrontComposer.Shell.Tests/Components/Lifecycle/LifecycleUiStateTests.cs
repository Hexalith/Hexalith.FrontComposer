using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 Task 5.1a — pure-function unit tests for <see cref="LifecycleUiState.From"/>.
/// No bUnit, no renderer — exercises the (CommandLifecycleState, LifecycleTimerPhase)
/// mapping table end-to-end and covers the D22 XSS-by-default contract.
/// </summary>
public sealed class LifecycleUiStateTests {
    private const string CorrId = "corr-unit-001";
    private static readonly DateTimeOffset Anchor = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);

    private static CommandLifecycleTransition T(CommandLifecycleState previous, CommandLifecycleState next, bool idempotencyResolved = false)
        => new(CorrId, previous, next, "01HXXXXXXXXXXXXXXXXXXXXXXX", Anchor, Anchor, idempotencyResolved);

    [Fact]
    public void Idle_phase_NoPulse_produces_no_announcement_no_pulse_no_message_bar() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Idle, CommandLifecycleState.Idle), LifecycleTimerPhase.NoPulse);

        state.Current.ShouldBe(CommandLifecycleState.Idle);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.NoPulse);
        state.RejectionMessage.ShouldBeNull();
    }

    [Fact]
    public void Submitting_phase_NoPulse_produces_submitting_announcement_polite_no_pulse() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Idle, CommandLifecycleState.Submitting), LifecycleTimerPhase.NoPulse);

        state.Current.ShouldBe(CommandLifecycleState.Submitting);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.NoPulse);
    }

    [Fact]
    public void Acknowledged_phase_NoPulse_produces_no_announcement_no_pulse() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Submitting, CommandLifecycleState.Acknowledged), LifecycleTimerPhase.NoPulse);

        state.Current.ShouldBe(CommandLifecycleState.Acknowledged);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.NoPulse);
    }

    [Fact]
    public void Syncing_phase_Pulse_preserves_pulse_phase() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing), LifecycleTimerPhase.Pulse);

        state.Current.ShouldBe(CommandLifecycleState.Syncing);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.Pulse);
    }

    [Fact]
    public void Syncing_phase_StillSyncing_preserves_still_syncing_phase() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing), LifecycleTimerPhase.StillSyncing);

        state.Current.ShouldBe(CommandLifecycleState.Syncing);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.StillSyncing);
    }

    [Fact]
    public void Syncing_phase_ActionPrompt_preserves_action_prompt_phase() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing), LifecycleTimerPhase.ActionPrompt);

        state.Current.ShouldBe(CommandLifecycleState.Syncing);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.ActionPrompt);
    }

    [Fact]
    public void Confirmed_forces_timer_phase_Terminal_regardless_of_input_phase() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed), LifecycleTimerPhase.ActionPrompt);

        state.Current.ShouldBe(CommandLifecycleState.Confirmed);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.Terminal);
    }

    [Fact]
    public void Rejected_forces_timer_phase_Terminal_regardless_of_input_phase() {
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected), LifecycleTimerPhase.Pulse);

        state.Current.ShouldBe(CommandLifecycleState.Rejected);
        state.TimerPhase.ShouldBe(LifecycleTimerPhase.Terminal);
    }

    [Fact]
    public void Rejected_carries_parameter_RejectionMessage_when_populated() {
        const string DomainCopy = "Approval failed: insufficient inventory.";
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected), LifecycleTimerPhase.Terminal, DomainCopy);

        state.RejectionMessage.ShouldBe(DomainCopy);
    }

    [Fact]
    public void IdempotencyResolved_true_on_Confirmed_produces_same_output_as_fresh_Confirmed_in_v01() {
        LifecycleUiState fresh = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, idempotencyResolved: false), LifecycleTimerPhase.Terminal);
        LifecycleUiState idempotent = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, idempotencyResolved: true), LifecycleTimerPhase.Terminal);

        fresh.Current.ShouldBe(idempotent.Current);
        fresh.TimerPhase.ShouldBe(idempotent.TimerPhase);
        idempotent.IdempotencyResolved.ShouldBeTrue();
        fresh.IdempotencyResolved.ShouldBeFalse();
    }

    [Fact]
    public void RejectionMessage_carries_raw_string_for_Blazor_plain_text_rendering_D22_XSS() {
        // Story 2-4 D22 — the mapper must preserve the adversarial string unchanged; Blazor's
        // default `@expression` rendering HTML-encodes it at render time. This test asserts
        // the contract at the data layer (plain string in, plain string out — no MarkupString).
        const string Script = "<script>alert(1)</script>";
        LifecycleUiState state = LifecycleUiState.From(T(CommandLifecycleState.Syncing, CommandLifecycleState.Rejected), LifecycleTimerPhase.Terminal, Script);

        state.RejectionMessage.ShouldBe(Script);
        state.RejectionMessage.ShouldBeAssignableTo<string>();
    }
}
