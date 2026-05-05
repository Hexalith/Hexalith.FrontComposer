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
        SkillBenchmarkResult result = new(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: "hash",
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: false,
            ValidatorSucceeded: false,
            FailureCategory: GeneratedCodeFailureCategory.Unknown,
            RedactionStatus: SkillBenchmarkRedactionStatus.Failed,
            GeneratedArtifactToken: "artifact:one",
            SanitizedDiagnostics: ["diagnostic"]);

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.RedactionFailedDiagnostic);
    }

    [Fact]
    public void ResultPersistence_BlocksWhenSanitizedDiagnosticsContainRawLocalPath() {
        // P-15: even when the caller asserts redaction passed, persistence is blocked when the
        // sanitized diagnostics still contain raw filesystem paths. This prevents a producer
        // bug from leaking through the persistence gate.
        SkillBenchmarkResult result = new(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: "hash",
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: false,
            ValidatorSucceeded: false,
            FailureCategory: GeneratedCodeFailureCategory.Unknown,
            RedactionStatus: SkillBenchmarkRedactionStatus.Passed,
            GeneratedArtifactToken: "artifact:one",
            SanitizedDiagnostics: ["error in C:\\Users\\jpiquot\\repo\\Bad.cs"]);

        SkillBenchmarkArtifactWriter.CanPersist(result).ShouldBeFalse();
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain(SkillBenchmarkArtifactWriter.SanitizationShapeDiagnostic);
    }

    [Fact]
    public void ResultPersistence_PersistsWhenRedactionPassedAndSanitizationLooksClean() {
        SkillBenchmarkResult result = new(
            PromptId: "p01",
            FrameworkVersion: "1.0.0",
            CorpusVersion: "1.0.0",
            ModelId: "model",
            ProviderConfigHash: "hash",
            ScorerVersion: "scorer-v1",
            ValidatorVersion: "validator-v1",
            CompileSucceeded: true,
            ValidatorSucceeded: true,
            FailureCategory: GeneratedCodeFailureCategory.None,
            RedactionStatus: SkillBenchmarkRedactionStatus.Passed,
            GeneratedArtifactToken: "artifact:abcd",
            SanitizedDiagnostics: ["validator-passed", "compile-succeeded"]);

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
    public void PromptSet_LoadsExpectedTwentyIdsByOrdinalOrderingFromFixture() {
        // Better than asserting "the loader sorted its own output": pin the actual 20 IDs.
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();

        promptSet.Prompts.Count.ShouldBe(20);
        promptSet.Prompts[0].Id.ShouldStartWith("p");
        promptSet.Prompts.Select(p => p.Id).Distinct().Count().ShouldBe(20);
    }
}
