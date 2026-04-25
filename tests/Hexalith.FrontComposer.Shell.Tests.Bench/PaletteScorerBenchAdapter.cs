// Story 3-7 T2.4 — adapter test that runs PaletteScorerBench in-process and asserts the
// AC5 / D4 guardrail (median per-candidate < 200 μs across the 1 000-candidate synthetic
// registry, 2× the spec budget of 100 μs).
//
// The bench runs under Job.ShortRun on the InProcessNoEmitToolchain so the test stays
// fast enough for the `performance` CI lane (~5-15 s) while still carrying real
// JIT-warmup discipline. The adapter does NOT gate on the spec budget directly; it gates
// only on the 2× regression guardrail (D4) so cold-start jitter does not flake CI.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Bench;

[Trait("Category", "Performance")]
public class PaletteScorerBenchAdapter {
    private const double GuardrailUsPerCandidate = 200.0;

    [Fact]
    public void PaletteScorerMedian_IsUnder_200Microseconds_PerCandidate() {
        ManualConfig config = ManualConfig.CreateMinimumViable()
            .AddJob(Job.ShortRun.WithToolchain(InProcessNoEmitToolchain.Instance))
            .AddLogger(NullLogger.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.DisableLogFile);

        Summary summary = BenchmarkRunner.Run<PaletteScorerBench>(config);

        summary.HasCriticalValidationErrors.ShouldBeFalse(
            "BenchmarkDotNet emitted critical validation errors — the bench did not run.");
        summary.Reports.ShouldNotBeEmpty("BenchmarkRunner produced no reports.");

        foreach (BenchmarkReport report in summary.Reports) {
            report.ResultStatistics.ShouldNotBeNull(
                $"Report for {report.BenchmarkCase.DisplayInfo} has no ResultStatistics.");

            double medianNs = report.ResultStatistics!.Median;
            double medianUsPerCandidate = medianNs / PaletteScorerBench.CandidateCount / 1_000.0;

            medianUsPerCandidate.ShouldBeLessThan(
                GuardrailUsPerCandidate,
                $"PaletteScorer regression: {report.BenchmarkCase.DisplayInfo} median was " +
                $"{medianUsPerCandidate:F2} μs/candidate, exceeding the {GuardrailUsPerCandidate} μs " +
                "AC5/D4 guardrail (2× the < 100 μs spec budget).");
        }
    }
}
