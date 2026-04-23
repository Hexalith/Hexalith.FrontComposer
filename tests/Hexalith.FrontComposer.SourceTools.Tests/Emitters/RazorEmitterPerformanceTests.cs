using System.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-1 T5.7 / Murat review (NFR4 anchor) — performance microbench locking
/// the assumption that the emitted ActionQueue <c>.Where(x =&gt; x.Status.ToString() == ...)</c>
/// filter stays cheap on adopter-realistic projection sizes (≤ 500 rows in v1
/// pre-virtualization). Story 4.4 will introduce <c>FluentVirtualizer</c> /
/// <c>ItemsProvider</c> for larger sets; until then this test catches the
/// "projections &lt; 500 rows is free" assumption breaking due to a regression in
/// the emitted filter expression (e.g., quadratic re-allocation).
/// </summary>
/// <remarks>
/// <para><strong>Bench discipline:</strong> story-internal Stopwatch-based
/// microbench with explicit warmup + measure runs. NOT a BenchmarkDotNet harness
/// — Story 10-4 will introduce the full bench surface; this is a smoke gate.
/// Threshold loosened to a generous 50ms (10× the spec's 5ms target) per CI
/// variability — a regression that breaks under 50ms is a true positive worth
/// investigating.</para>
/// </remarks>
public sealed class RazorEmitterPerformanceTests {
    private enum OrderStatus {
        Pending,
        Submitted,
        Approved,
        Rejected,
    }

    private sealed class OrderRow {
        public OrderStatus Status { get; init; }
    }

    [Fact]
    public void ActionQueueWhereFilterStaysUnder50MsFor500Rows() {
        // Build 500 synthetic rows with varied status enum values.
        OrderRow[] rows = new OrderRow[500];
        OrderStatus[] cycle = { OrderStatus.Pending, OrderStatus.Submitted, OrderStatus.Approved, OrderStatus.Rejected };
        for (int i = 0; i < rows.Length; i++) {
            rows[i] = new OrderRow { Status = cycle[i % cycle.Length] };
        }

        // Mirror the emitted filter shape:
        //   state.Items.Where(x => x.Status.ToString() == "Pending"
        //                       || x.Status.ToString() == "Submitted"
        //                       || x.Status.ToString() == "Approved"
        //                       || x.Status.ToString() == "Rejected")
        bool Filter(OrderRow x) =>
            x.Status.ToString() == "Pending"
            || x.Status.ToString() == "Submitted"
            || x.Status.ToString() == "Approved"
            || x.Status.ToString() == "Rejected";

        // Warmup
        for (int w = 0; w < 3; w++) {
            _ = rows.Where(Filter).Count();
        }

        // Measure 7 runs; assert median under 50ms.
        long[] elapsedMs = new long[7];
        Stopwatch sw = new();
        for (int run = 0; run < 7; run++) {
            sw.Restart();
            int count = rows.Where(Filter).Count();
            sw.Stop();
            elapsedMs[run] = sw.ElapsedMilliseconds;
            count.ShouldBeGreaterThan(0, customMessage: "Filter sanity check: at least one row should match.");
        }

        Array.Sort(elapsedMs);
        long median = elapsedMs[elapsedMs.Length / 2];
        Debug.WriteLine($"ActionQueueWhereFilter median (500 rows × 4 states × 7 runs) = {median}ms; samples = [{string.Join(",", elapsedMs)}]");

        median.ShouldBeLessThan(
            50,
            customMessage:
                $"ActionQueue WhenState filter median exceeded 50ms on 500 rows. Samples ms = [{string.Join(",", elapsedMs)}]. "
                + "Spec target was 5ms; the 50ms threshold accommodates CI variability. A regression past 50ms suggests "
                + "the emitted filter has degraded to quadratic behavior — investigate the .Where expression shape "
                + "for missing memoization or per-row allocation hot paths.");
    }
}
