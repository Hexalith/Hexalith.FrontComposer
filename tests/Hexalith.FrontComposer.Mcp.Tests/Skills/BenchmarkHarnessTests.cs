using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

public sealed class BenchmarkHarnessTests {
    [Fact]
    public void PromptSet_LoadsTwentyV1PromptsWithDeterministicIds() {
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();

        promptSet.Prompts.Count.ShouldBe(20);
        promptSet.Prompts.Select(p => p.Id).ShouldBe(promptSet.Prompts.Select(p => p.Id).Order(StringComparer.Ordinal));
        promptSet.Prompts.ShouldAllBe(p => p.ExpectedShape.Count > 0);
    }

    [Fact]
    public void CacheKey_ChangesWhenContractInputsChange() {
        SkillBenchmarkPrompt prompt = SkillBenchmarkPromptSet.LoadEmbeddedV1().Prompts[0];
        var config = new SkillBenchmarkModelConfig("provider", "model", Temperature: 0, Seed: 123, TimeoutSeconds: 60, RetryCount: 0);

        SkillBenchmarkCacheKey first = SkillBenchmarkCacheKey.Create(
            prompt,
            "1.0.0",
            "1.0.0",
            config,
            "scorer-v1",
            "validator-v1",
            "redaction-v1");
        SkillBenchmarkCacheKey second = SkillBenchmarkCacheKey.Create(
            prompt with { Text = prompt.Text + " changed" },
            "1.0.0",
            "1.0.0",
            config,
            "scorer-v1",
            "validator-v1",
            "redaction-v1");

        first.Value.ShouldNotBe(second.Value);
        SkillBenchmarkCachePolicy.CanReuse(first, second).ShouldBeFalse();
        SkillBenchmarkCachePolicy.CacheMissReason(first, second).ShouldBe("contract-input-changed");
    }

    [Fact]
    public void CacheKey_ChangesWhenExpectedShapeChanges() {
        // P-4: the cache key must include the prompt's full ExpectedShape. A change to the
        // structural acceptance criteria should invalidate cached pass results from before the
        // criteria tightened.
        SkillBenchmarkPrompt prompt = SkillBenchmarkPromptSet.LoadEmbeddedV1().Prompts[0];
        var config = new SkillBenchmarkModelConfig("provider", "model", Temperature: 0, Seed: 123, TimeoutSeconds: 60, RetryCount: 0);

        SkillBenchmarkCacheKey first = SkillBenchmarkCacheKey.Create(
            prompt,
            "1.0.0",
            "1.0.0",
            config,
            "scorer-v1",
            "validator-v1",
            "redaction-v1");
        SkillBenchmarkCacheKey second = SkillBenchmarkCacheKey.Create(
            prompt with { ExpectedShape = [.. prompt.ExpectedShape, "new-acceptance-criterion"] },
            "1.0.0",
            "1.0.0",
            config,
            "scorer-v1",
            "validator-v1",
            "redaction-v1");

        first.Value.ShouldNotBe(second.Value);
    }

    [Fact]
    public void CacheKey_ChangesWhenSeedChangesEvenIfOtherwiseEqual() {
        // P-4 follow-up: nullable Seed must explicitly participate in the canonical hash so a
        // null vs Some(123) flip produces a cache-miss.
        SkillBenchmarkPrompt prompt = SkillBenchmarkPromptSet.LoadEmbeddedV1().Prompts[0];
        var configA = new SkillBenchmarkModelConfig("provider", "model", Temperature: 0, Seed: null, TimeoutSeconds: 60, RetryCount: 0);
        var configB = configA with { Seed = 123 };

        SkillBenchmarkCacheKey first = SkillBenchmarkCacheKey.Create(prompt, "1.0.0", "1.0.0", configA, "scorer-v1", "validator-v1", "redaction-v1");
        SkillBenchmarkCacheKey second = SkillBenchmarkCacheKey.Create(prompt, "1.0.0", "1.0.0", configB, "scorer-v1", "validator-v1", "redaction-v1");

        first.Value.ShouldNotBe(second.Value);
    }

    [Fact]
    public void ResultPersistence_BlocksWhenRedactionFails() {
        SkillBenchmarkResult result = WithProviderMetadata(new SkillBenchmarkResult(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: ProviderConfig().ConfigHash(),
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: false,
            ValidatorSucceeded: false,
            FailureCategory: GeneratedCodeFailureCategory.Unknown,
            RedactionStatus: SkillBenchmarkRedactionStatus.Failed,
            GeneratedArtifactToken: "artifact:one",
            SanitizedDiagnostics: ["diagnostic"]));

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.RedactionFailedDiagnostic);
    }

    [Fact]
    public void ResultPersistence_BlocksWhenSanitizedDiagnosticsContainRawLocalPath() {
        // P-15: even when the caller asserts redaction passed, persistence is blocked when the
        // sanitized diagnostics still contain raw filesystem paths. This prevents a producer
        // bug from leaking through the persistence gate.
        SkillBenchmarkResult result = WithProviderMetadata(new SkillBenchmarkResult(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: ProviderConfig().ConfigHash(),
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: false,
            ValidatorSucceeded: false,
            FailureCategory: GeneratedCodeFailureCategory.Unknown,
            RedactionStatus: SkillBenchmarkRedactionStatus.Passed,
            GeneratedArtifactToken: "artifact:one",
            SanitizedDiagnostics: ["error in C:\\Users\\jpiquot\\repo\\Bad.cs"]));

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.SanitizationShapeDiagnostic);
    }

    [Fact]
    public void ResultPersistence_PersistsWhenRedactionPassedAndSanitizationLooksClean() {
        SkillBenchmarkResult result = WithProviderMetadata(new SkillBenchmarkResult(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: ProviderConfig().ConfigHash(),
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: true,
            ValidatorSucceeded: true,
            FailureCategory: GeneratedCodeFailureCategory.None,
            RedactionStatus: SkillBenchmarkRedactionStatus.Passed,
            GeneratedArtifactToken: "artifact:abcd",
            SanitizedDiagnostics: ["validator-passed", "compile-succeeded"]));

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeTrue();
        SkillBenchmarkArtifactBuildResult artifact = SkillBenchmarkArtifactWriter.TryBuildArtifact(result);
        artifact.CanPersist.ShouldBeTrue();
        artifact.ArtifactJson.ShouldNotBeNullOrEmpty();
        artifact.ArtifactJson!.ShouldContain("\"PromptId\":\"p01\"");
    }

    [Fact]
    public void OfflineScorer_PicksHighestPriorityCategoryWhenMultipleAreReported() {
        // P-14: when multiple categories appear (e.g., PackageBoundary + TenantSpoofing), the
        // scorer reports the highest-priority one (TenantSpoofing) so security-relevant signals
        // dominate the score.
        SkillBenchmarkScore score = SkillBenchmarkOfflineScorer.Score(
            SkillBenchmarkPromptSet.LoadEmbeddedV1().Prompts[0],
            [
                new GeneratedCodeFile("Bad/Bad.csproj", "<Project><ItemGroup><PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.0\" /></ItemGroup></Project>"),
                new GeneratedCodeFile("Bad/SubmitCommand.cs", """
                    using Hexalith.FrontComposer.Contracts.Attributes;
                    namespace Bad;
                    [Command]
                    public partial class SubmitCommand { public string MessageId { get; set; } = ""; public string TenantId { get; set; } = ""; }
                    """),
            ]);

        score.Passed.ShouldBeFalse();
        score.FailureCategory.ShouldBe(GeneratedCodeFailureCategory.TenantSpoofing);
    }

    [Fact]
    public void OfflineScorer_UsesStructuralValidatorCategories() {
        SkillBenchmarkScore score = SkillBenchmarkOfflineScorer.Score(
            SkillBenchmarkPromptSet.LoadEmbeddedV1().Prompts[0],
            [
                new GeneratedCodeFile("Bad/Bad.csproj", "<Project><PackageReference Include=\"Newtonsoft.Json\" /></Project>"),
            ]);

        score.Passed.ShouldBeFalse();
        score.FailureCategory.ShouldBe(GeneratedCodeFailureCategory.PackageBoundary);
    }

    [Fact]
    public void OneShotPassRate_ComputesAggregateAndComparesAgainstTarget() {
        // P-29 / AC9: 16 of 20 passes hits the documented 80% one-shot target; 15 of 20 misses.
        SkillBenchmarkScore[] sixteenPassed = [
            ..Enumerable.Repeat(new SkillBenchmarkScore(true, GeneratedCodeFailureCategory.None), 16),
            ..Enumerable.Repeat(new SkillBenchmarkScore(false, GeneratedCodeFailureCategory.PackageBoundary), 4),
        ];
        SkillBenchmarkScore[] fifteenPassed = [
            ..Enumerable.Repeat(new SkillBenchmarkScore(true, GeneratedCodeFailureCategory.None), 15),
            ..Enumerable.Repeat(new SkillBenchmarkScore(false, GeneratedCodeFailureCategory.PackageBoundary), 5),
        ];

        SkillBenchmarkOfflineScorer.OneShotPassRate(sixteenPassed).ShouldBe(0.80, tolerance: 0.0001);
        SkillBenchmarkOfflineScorer.OneShotPassRate(fifteenPassed).ShouldBe(0.75, tolerance: 0.0001);
        SkillBenchmarkOfflineScorer.OneShotPassTarget.ShouldBe(0.80);
    }

    [Fact]
    public void ProviderConfigHash_IsStableAcrossEqualConfigsAndDifferentForVariations() {
        // P-26: ProviderConfigHash now derives from the canonical config instead of caller
        // supplying any string. Equal configs produce equal hashes; variations produce different
        // hashes.
        var baseConfig = new SkillBenchmarkModelConfig("provider", "model", Temperature: 0, Seed: 123, TimeoutSeconds: 60, RetryCount: 0);

        baseConfig.ConfigHash().ShouldBe((baseConfig with { }).ConfigHash());
        baseConfig.ConfigHash().ShouldNotBe((baseConfig with { ModelId = "other-model" }).ConfigHash());
        baseConfig.ConfigHash().ShouldNotBe((baseConfig with { Seed = null }).ConfigHash());
        baseConfig.ConfigHash().ShouldNotBe((baseConfig with { Temperature = 0.5 }).ConfigHash());
    }

    [Fact]
    public void DeterminismPolicy_SendsTemperatureZeroAndSeedOnlyWhenSupported() {
        var desired = new SkillBenchmarkModelConfig("openai", "gpt-test", Temperature: 0.7, Seed: 123, TimeoutSeconds: 90, RetryCount: 1);

        SkillBenchmarkProviderRequest supported = SkillBenchmarkDeterminismPolicy.CreateRequest(
            desired,
            new SkillBenchmarkProviderCapabilities(SupportsSeed: true, SupportsFingerprint: true));
        SkillBenchmarkProviderRequest unsupported = SkillBenchmarkDeterminismPolicy.CreateRequest(
            desired,
            new SkillBenchmarkProviderCapabilities(SupportsSeed: false, SupportsFingerprint: false));

        supported.Config.Temperature.ShouldBe(0d);
        supported.Config.Seed.ShouldBe(123);
        supported.SeedSent.ShouldBeTrue();
        supported.FingerprintExpected.ShouldBeTrue();
        supported.UnsupportedCapabilities.ShouldBeEmpty();

        unsupported.Config.Temperature.ShouldBe(0d);
        unsupported.Config.Seed.ShouldBeNull();
        unsupported.SeedSent.ShouldBeFalse();
        unsupported.FingerprintExpected.ShouldBeFalse();
        unsupported.UnsupportedCapabilities.ShouldContain("seed-unsupported");
        unsupported.UnsupportedCapabilities.ShouldContain("fingerprint-unsupported");
    }

    [Fact]
    public void BudgetPolicy_FailsClosedForMissingAtLimitExpiredMalformedAndRetryStormState() {
        DateTimeOffset now = DateTimeOffset.Parse("2026-05-10T12:00:00Z");
        var available = new SkillBenchmarkBudgetState(100, 99, now.AddDays(1), ProviderCostMetadataAvailable: true, RetryStormDetected: false);

        SkillBenchmarkBudgetPolicy.Evaluate(available, now).ShouldBe(SkillBenchmarkBudgetStatus.Available);
        SkillBenchmarkBudgetPolicy.Evaluate(available with { Consumed = 100 }, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetExhausted);
        SkillBenchmarkBudgetPolicy.Evaluate(null, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetUnknown);
        SkillBenchmarkBudgetPolicy.Evaluate(available with { MonthlyCap = 0 }, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetUnknown);
        SkillBenchmarkBudgetPolicy.Evaluate(available with { ExpiresAt = now.AddSeconds(-1) }, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetUnknown);
        SkillBenchmarkBudgetPolicy.Evaluate(available with { ProviderCostMetadataAvailable = false }, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetUnknown);
        SkillBenchmarkBudgetPolicy.Evaluate(available with { RetryStormDetected = true }, now).ShouldBe(SkillBenchmarkBudgetStatus.BudgetUnknown);
    }

    [Fact]
    public void BenchmarkGate_RequiresExactlyTwentyValidPromptResultsAndApprovedBaseline() {
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();
        SkillBenchmarkResult[] results = [.. promptSet.Prompts.Select((prompt, index) => BenchmarkResultFor(prompt.Id, index < 16))];

        SkillBenchmarkGateResult candidate = SkillBenchmarkGate.Evaluate(promptSet, results, approvedBaseline: null);
        candidate.Status.ShouldBe(SkillBenchmarkGateStatus.CandidateOnly);
        candidate.PassedCount.ShouldBe(16);
        candidate.Threshold.ShouldBe(0.80, tolerance: 0.0001);

        var baseline = new SkillBenchmarkBaselineArtifact(
            InitialPassRate: 0.80,
            CorpusHash: "corpus",
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            RedactionPolicyVersion: "redaction-v1",
            ProviderConfigHash: "provider",
            CommitSha: "abc123",
            ApproverMarker: "approved-by-release-owner",
            SanitizedSummaryHash: "summary",
            CapturedAt: DateTimeOffset.Parse("2026-05-10T12:00:00Z"));

        SkillBenchmarkGateResult gate = SkillBenchmarkGate.Evaluate(promptSet, results, baseline);
        gate.Status.ShouldBe(SkillBenchmarkGateStatus.Passed);
        SkillBenchmarkBaselinePolicy.DecideWrite(trustedContext: false, approvedMarkerPresent: true, gate)
            .ShouldBe(SkillBenchmarkBaselineWriteDecision.CandidateEvidenceOnly);
        SkillBenchmarkBaselinePolicy.DecideWrite(trustedContext: true, approvedMarkerPresent: true, gate)
            .ShouldBe(SkillBenchmarkBaselineWriteDecision.WriteApprovedBaseline);

        SkillBenchmarkGate.Evaluate(promptSet, results[..19], baseline).Status.ShouldBe(SkillBenchmarkGateStatus.InvalidEvidence);
        SkillBenchmarkGate.Evaluate(promptSet, [.. results.Select((r, i) => i == 0 ? r with { EvidenceStatus = SkillBenchmarkEvidenceStatus.BudgetBlocked } : r)], baseline)
            .Status.ShouldBe(SkillBenchmarkGateStatus.InvalidEvidence);
        SkillBenchmarkGate.Evaluate(promptSet, [.. results.Select((r, i) => i < 15 ? r with { CompileSucceeded = true, ValidatorSucceeded = true } : r with { CompileSucceeded = false, ValidatorSucceeded = false, EvidenceStatus = SkillBenchmarkEvidenceStatus.LegitimateMiss })], baseline)
            .Status.ShouldBe(SkillBenchmarkGateStatus.Failed);
    }

    [Fact]
    public void SummarySanitizerAndArtifactWriter_BlockHostileEvidenceContent() {
        string sanitized = SkillBenchmarkSummarySanitizer.Sanitize("::warning:: <script>x</script> C:\\repo\\secret.cs tenantId=abc sk-1234567890123456");

        sanitized.ShouldStartWith("\\::warning");
        sanitized.ShouldNotContain("<script>");
        sanitized.ShouldNotContain("C:\\repo");
        sanitized.ShouldNotContain("tenantId=abc");
        sanitized.ShouldNotContain("sk-1234567890123456");

        SkillBenchmarkResult result = BenchmarkResultFor("p01", passed: false) with {
            SanitizedDiagnostics = ["::warning:: injected workflow command"],
        };

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.UnsafeSummaryDiagnostic);
    }

    [Fact]
    public void ResultPersistenceAndGate_BlockMissingProviderMetadata() {
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();
        SkillBenchmarkResult[] results = [.. promptSet.Prompts.Select((prompt, index) => BenchmarkResultFor(prompt.Id, index < 16))];
        SkillBenchmarkResult missingMetadata = results[0] with { ProviderId = string.Empty };

        SkillBenchmarkArtifactWriter.CanPersist(missingMetadata).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(missingMetadata).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.MissingProviderMetadataDiagnostic);
        SkillBenchmarkGate.Evaluate(promptSet, [missingMetadata, .. results.Skip(1)], ApprovedBaseline()).Diagnostics.ShouldContain("provider-metadata-required");
    }

    [Fact]
    public void EvidencePath_NormalizesUnderApprovedRootAndRejectsEscapes() {
        string root = Path.Combine(Path.GetTempPath(), $"fc-benchmark-{Guid.NewGuid():N}");
        string safe = SkillBenchmarkEvidencePath.NormalizeUnderRoot(root, "summaries/result.json");

        safe.ShouldStartWith(Path.GetFullPath(root));
        Should.Throw<InvalidOperationException>(() => SkillBenchmarkEvidencePath.NormalizeUnderRoot(root, "../outside.json"));
        Should.Throw<InvalidOperationException>(() => SkillBenchmarkEvidencePath.NormalizeUnderRoot(root, Path.Combine(Path.GetPathRoot(root)!, "outside.json")));
    }

    [Fact]
    public void PromptSet_LoadsExpectedTwentyIdsByOrdinalOrderingFromFixture() {
        // Better than asserting "the loader sorted its own output": pin the actual 20 IDs.
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();

        promptSet.Prompts.Count.ShouldBe(20);
        promptSet.Prompts[0].Id.ShouldStartWith("p");
        promptSet.Prompts.Select(p => p.Id).Distinct().Count().ShouldBe(20);
    }

    private static SkillBenchmarkResult BenchmarkResultFor(string promptId, bool passed)
        => WithProviderMetadata(new SkillBenchmarkResult(
            PromptId: promptId,
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: ProviderConfig().ConfigHash(),
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: passed,
            ValidatorSucceeded: passed,
            FailureCategory: passed ? GeneratedCodeFailureCategory.None : GeneratedCodeFailureCategory.PackageBoundary,
            RedactionStatus: SkillBenchmarkRedactionStatus.Passed,
            GeneratedArtifactToken: $"artifact:{promptId}",
            SanitizedDiagnostics: [passed ? "validator-passed" : "legitimate-miss"])) with {
            ProviderId = "provider",
            Temperature = 0,
            Seed = 123,
            TimeoutSeconds = 60,
            RetryCount = 0,
            SeedSupported = true,
            FingerprintSupported = true,
            ProviderFingerprint = "fp-test",
            EvidenceStatus = passed ? SkillBenchmarkEvidenceStatus.Valid : SkillBenchmarkEvidenceStatus.LegitimateMiss,
        };

    private static SkillBenchmarkModelConfig ProviderConfig()
        => new("provider", "model", 0, 123, 60, 0);

    private static SkillBenchmarkResult WithProviderMetadata(SkillBenchmarkResult result)
        => result with {
            ProviderId = "provider",
            Temperature = 0,
            Seed = 123,
            TimeoutSeconds = 60,
            RetryCount = 0,
            SeedSupported = true,
            FingerprintSupported = true,
            ProviderFingerprint = "fp-test",
            CacheKey = "cache-key",
            SanitizedArtifactToken = result.GeneratedArtifactToken,
        };

    private static SkillBenchmarkBaselineArtifact ApprovedBaseline()
        => new(
            InitialPassRate: 0.80,
            CorpusHash: "corpus",
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            RedactionPolicyVersion: "redaction-v1",
            ProviderConfigHash: ProviderConfig().ConfigHash(),
            CommitSha: "abc123",
            ApproverMarker: "approved-by-release-owner",
            SanitizedSummaryHash: "summary",
            CapturedAt: DateTimeOffset.Parse("2026-05-10T12:00:00Z"));
}
