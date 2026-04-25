// Story 3-7 T2.4 — adapter test that runs PaletteScorerBench in-process and asserts the
// AC5 gates (per-candidate p95 + aggregate-pass) with the D4 2× guardrail headroom.
//
// AC5 spec: PaletteScorer.Score runs in < 100 μs per candidate AND aggregate < 100 ms over
// 1 000 candidates. D4 ratifies a 2× regression guardrail so cold-start jitter does not
// flake CI: per-candidate p95 < 200 μs AND aggregate p95 < 200 ms.
//
// PaletteScorerBench declares `OperationsPerInvoke = CandidateCount` so BDN's reported
// statistics are already per-candidate (in nanoseconds). The aggregate is recovered by
// multiplying back by CandidateCount.
//
// Pass-1 review (DN6=a) replaced the earlier `Median / N` proxy with a true per-candidate
// p95 read directly from `report.ResultStatistics.Percentiles.P95`.
//
// Bench runs under Job.ShortRun on the InProcessNoEmitToolchain so the test stays fast
// enough for the `performance` CI lane (~5–15 s) while still carrying real JIT-warmup
// discipline. The bench project's AssemblyInfo.cs sets `[CollectionBehavior(DisableTest-
// Parallelization = true)]` so a sibling fact cannot starve the bench mid-iteration
// (Pass-1 P16 fix).

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Bench;

[Trait("Category", "Performance")]
public class PaletteScorerBenchAdapter {
    private const double PerCandidateGuardrailUs = 200.0;
    private const double AggregatePassGuardrailMs = 200.0;

    [Fact]
    public void PaletteScorerP95_PerCandidate_IsUnder_200Microseconds() {
        Summary summary = RunBench();

        foreach (BenchmarkReport report in summary.Reports) {
            Statistics stats = AssertReportStatistics(report);
            double p95Ns = stats.Percentiles.P95;
            double p95UsPerCandidate = p95Ns / 1_000.0;

            p95UsPerCandidate.ShouldBeLessThan(
                PerCandidateGuardrailUs,
                $"PaletteScorer per-candidate p95 regression: {report.BenchmarkCase.DisplayInfo} " +
                $"p95 was {p95UsPerCandidate:F2} μs/candidate, exceeding the {PerCandidateGuardrailUs} μs " +
                "AC5/D4 guardrail (2× the < 100 μs spec budget).");
        }
    }

    [Fact]
    public void PaletteScorerAggregate_IsUnder_200Milliseconds_PerPass() {
        Summary summary = RunBench();

        foreach (BenchmarkReport report in summary.Reports) {
            Statistics stats = AssertReportStatistics(report);
            double p95Ns = stats.Percentiles.P95;
            double aggregateMs = p95Ns * PaletteScorerBench.CandidateCount / 1_000_000.0;

            aggregateMs.ShouldBeLessThan(
                AggregatePassGuardrailMs,
                $"PaletteScorer aggregate-pass p95 regression: {report.BenchmarkCase.DisplayInfo} " +
                $"aggregate was {aggregateMs:F1} ms/pass over {PaletteScorerBench.CandidateCount} " +
                $"candidates, exceeding the {AggregatePassGuardrailMs} ms AC5/D4 guardrail " +
                "(2× the < 100 ms spec budget).");
        }
    }

    private static Summary RunBench() {
        ManualConfig config = ManualConfig.CreateMinimumViable()
            .AddJob(Job.ShortRun.WithToolchain(InProcessNoEmitToolchain.Instance))
            .AddLogger(NullLogger.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.DisableLogFile);

        Summary summary = BenchmarkRunner.Run<PaletteScorerBench>(config);

        summary.HasCriticalValidationErrors.ShouldBeFalse(
            "BenchmarkDotNet emitted critical validation errors — the bench did not run.");
        summary.Reports.ShouldNotBeEmpty("BenchmarkRunner produced no reports.");
        return summary;
    }

    private static Statistics AssertReportStatistics(BenchmarkReport report) {
        report.ResultStatistics.ShouldNotBeNull(
            $"Report for {report.BenchmarkCase.DisplayInfo} has no ResultStatistics.");
        return report.ResultStatistics!;
    }
}
