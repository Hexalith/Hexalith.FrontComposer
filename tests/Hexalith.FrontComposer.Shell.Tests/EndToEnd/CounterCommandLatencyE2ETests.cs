using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.EndToEnd;

/// <summary>
/// TDD RED-phase Playwright/Aspire end-to-end latency gate for Story 2-4 AC9 (NFR1 / NFR2).
/// Measures the <c>click → Confirmed FluentMessageBar visible</c> latency on the Counter
/// sample under the Aspire topology. All tests carry the <c>E2E</c> trait so CI can gate
/// them separately from the default unit pass.
/// </summary>
/// <remarks>
/// <para>
/// These tests use the wrapper's DOM signal (FluentMessageBar Intent=Success visible) as the
/// "Confirmed in UI" end-point — NOT an internal Fluxor subscription — because NFR1/NFR2
/// measure the USER-VISIBLE confirmation, not the backend lifecycle state (Story 2-4 D2/D6).
/// </para>
/// <para>
/// Prereq (Task 6.1): add <c>&lt;PackageReference Include="Microsoft.Playwright" /&gt;</c>
/// to <c>Hexalith.FrontComposer.Shell.Tests.csproj</c> and register the matching
/// <c>PackageVersion</c> in <c>Directory.Packages.props</c>. When unavailable, the tests
/// <c>Assert.Skip</c> rather than silently pass — never a green false positive.
/// </para>
/// </remarks>
[Trait("Category", "E2E")]
public sealed class CounterCommandLatencyE2ETests {
    private const int ColdActorClicks = 50;
    private const int WarmActorClicks = 100;
    private const double ColdP95BudgetMs = 800.0;
    private const double WarmP50BudgetMs = 400.0;

    [Fact(Skip = "TDD RED — Story 2-4 Task 6.1 / AC9 / NFR1: cold-actor P95 click→Confirmed latency must be < 800 ms.")]
    public async Task CounterLatency_ColdActor_P95_Under_800ms() {
        SkipIfPlaywrightUnavailable();

#pragma warning disable CA2007 // ConfigureAwait not required in test code; xUnit1030 forbids ConfigureAwait(false)
        await using CounterAspireHarness harness = await CounterAspireHarness.StartColdAsync();
        double[] latenciesMs = await harness.MeasureClickToConfirmedAsync(ColdActorClicks);
#pragma warning restore CA2007

        double p95 = Percentile(latenciesMs, 0.95);
        p95.ShouldBeLessThan(ColdP95BudgetMs, $"Cold-actor P95 latency budget (<{ColdP95BudgetMs}ms, NFR1) breached: observed={p95:F1}ms across {ColdActorClicks} clicks.");
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 6.1 / AC9 / NFR2: warm-actor P50 click→Confirmed latency must be < 400 ms.")]
    public async Task CounterLatency_WarmActor_P50_Under_400ms() {
        SkipIfPlaywrightUnavailable();

#pragma warning disable CA2007
        await using CounterAspireHarness harness = await CounterAspireHarness.StartWarmAsync();
        double[] latenciesMs = await harness.MeasureClickToConfirmedAsync(WarmActorClicks);
#pragma warning restore CA2007

        double p50 = Percentile(latenciesMs, 0.50);
        p50.ShouldBeLessThan(WarmP50BudgetMs, $"Warm-actor P50 latency budget (<{WarmP50BudgetMs}ms, NFR2) breached: observed={p50:F1}ms across {WarmActorClicks} clicks.");
    }

    private static double Percentile(double[] samples, double p) {
        double[] ordered = [.. samples.OrderBy(x => x)];
        int idx = (int)Math.Clamp(Math.Ceiling(p * ordered.Length) - 1, 0, ordered.Length - 1);
        return ordered[idx];
    }

    private static void SkipIfPlaywrightUnavailable() {
        // Microsoft.Playwright is added by Task 6.1. When not yet referenced, fail loudly via
        // Assert.Skip — never silently green-pass, per memory/feedback_no_manual_validation.md.
        Type? playwrightType = Type.GetType("Microsoft.Playwright.Playwright, Microsoft.Playwright");
        if (playwrightType is null) {
            Assert.Skip("Microsoft.Playwright package not referenced — Story 2-4 Task 6.1 must add it before AC9 activates.");
        }
    }

    /// <summary>
    /// Placeholder harness surface — Task 6.1 authors the real Aspire bootstrap + Playwright
    /// browser session. Tests reference this type so the test file compiles in the red phase.
    /// </summary>
    private sealed class CounterAspireHarness : IAsyncDisposable {
        public static Task<CounterAspireHarness> StartColdAsync() => throw new NotImplementedException("Task 6.1: Aspire AppHost bootstrap + fresh actor state.");

        public static Task<CounterAspireHarness> StartWarmAsync() => throw new NotImplementedException("Task 6.1: Aspire AppHost bootstrap + prior actor-warm-up run.");

        public Task<double[]> MeasureClickToConfirmedAsync(int clickCount) => throw new NotImplementedException("Task 6.1: click Increment, wait for FluentMessageBar[intent=success], record elapsed.");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
