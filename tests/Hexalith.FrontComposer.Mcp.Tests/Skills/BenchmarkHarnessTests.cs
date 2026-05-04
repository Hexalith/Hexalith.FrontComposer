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
        SkillBenchmarkArtifactWriter.TryBuildArtifact(result).Diagnostics.ShouldContain("redaction-not-passed");
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
}
