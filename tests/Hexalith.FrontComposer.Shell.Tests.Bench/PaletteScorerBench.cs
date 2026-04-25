// Story 3-7 T2 — BenchmarkDotNet harness for PaletteScorer (AC5).
//
// AC5 spec budget: PaletteScorer.Score runs in < 100 μs per candidate on a 1 000-candidate
// synthetic registry. D4 guardrail (asserted by PaletteScorerBenchAdapter) is 2× the spec
// budget — < 200 μs per-candidate median — to tolerate measurement noise without flaking.
//
// The synthetic registry is generated deterministically (seeded Random) so per-run results
// are comparable; BDN's own warmup + iteration discipline still removes JIT/GC outliers.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

using Hexalith.FrontComposer.Shell.State.CommandPalette;

namespace Hexalith.FrontComposer.Shell.Tests.Bench;

[MemoryDiagnoser]
public class PaletteScorerBench {
    public const int CandidateCount = 1_000;

    [Params("cou", "view", "olv", "zzz")]
    public string Query { get; set; } = "cou";

    private string[] _candidates = [];

    [GlobalSetup]
    public void Setup() => _candidates = SyntheticRegistry.Build(CandidateCount, seed: 42);

    /// <summary>
    /// Scores the query against the full 1 000-candidate registry. The
    /// <c>OperationsPerInvoke = CandidateCount</c> hint instructs BDN to normalise the
    /// reported statistics by candidate count, so <c>ResultStatistics.Percentiles.P95</c>
    /// is the per-candidate p95 in nanoseconds (Pass-1 DN6=a fix). The aggregate-pass
    /// metric is recovered by multiplying back by <see cref="CandidateCount"/>.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CandidateCount)]
    public int ScoreFullRegistry() {
        int sum = 0;
        for (int i = 0; i < _candidates.Length; i++) {
            sum += PaletteScorer.Score(Query, _candidates[i]);
        }

        return sum;
    }
}
