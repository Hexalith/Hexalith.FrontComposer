using System.Reflection;

using Hexalith.FrontComposer.Mcp.Skills;
using Hexalith.FrontComposer.Shell.Tests.Bench.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Bench.Governance;

[Trait("Category", "Governance")]
public sealed class BenchmarkHarnessGovernanceTests {
    private static readonly string[] RequiredFactNames = [
        "PromptSet_LoadsTwentyV1PromptsWithDeterministicIds",
        "CacheKey_ChangesWhenContractInputsChange",
        "CacheKey_ChangesWhenExpectedShapeChanges",
        "CacheKey_ChangesWhenSeedChangesEvenIfOtherwiseEqual",
        "ResultPersistence_BlocksWhenRedactionFails",
        "ResultPersistence_BlocksWhenSanitizedDiagnosticsContainRawLocalPath",
        "ResultPersistence_PersistsWhenRedactionPassedAndSanitizationLooksClean",
        "OfflineScorer_PicksHighestPriorityCategoryWhenMultipleAreReported",
        "OfflineScorer_UsesStructuralValidatorCategories",
        "OneShotPassRate_ComputesAggregateAndComparesAgainstTarget",
        "ProviderConfigHash_IsStableAcrossEqualConfigsAndDifferentForVariations",
        "DeterminismPolicy_SendsTemperatureZeroAndSeedOnlyWhenSupported",
        "BudgetPolicy_FailsClosedForMissingAtLimitExpiredMalformedAndRetryStormState",
        "BenchmarkGate_RequiresExactlyTwentyValidPromptResultsAndApprovedBaseline",
        "SummarySanitizerAndArtifactWriter_BlockHostileEvidenceContent",
        "ResultPersistenceAndGate_BlockMissingProviderMetadata",
        "EvidencePath_NormalizesUnderApprovedRootAndRejectsEscapes",
        "PromptSet_LoadsExpectedTwentyIdsByOrdinalOrderingFromFixture",
    ];

    [Fact]
    public void BenchmarkHarness_DeclaresExactRequiredPerformanceFacts() {
        MethodInfo[] facts = typeof(BenchmarkHarnessTests).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(FactAttribute)))
            .ToArray();
        SkillBenchmarkPromptSet promptSet = SkillBenchmarkPromptSet.LoadEmbeddedV1();

        facts.Select(method => method.Name).Order(StringComparer.Ordinal).ShouldBe(
            RequiredFactNames.Order(StringComparer.Ordinal));
        foreach (MethodInfo factMethod in facts) {
            FactAttribute fact = factMethod.GetCustomAttribute<FactAttribute>()!;
            fact.Explicit.ShouldBeFalse($"{factMethod.Name} must execute in the benchmark lane.");
            fact.Skip.ShouldBeNull($"{factMethod.Name} must not be skipped.");
            fact.SkipType.ShouldBeNull($"{factMethod.Name} must not use conditional skip metadata.");
            fact.SkipUnless.ShouldBeNull($"{factMethod.Name} must not use conditional skip metadata.");
            fact.SkipWhen.ShouldBeNull($"{factMethod.Name} must not use conditional skip metadata.");
        }

        typeof(BenchmarkHarnessTests).CustomAttributes.ShouldContain(attribute =>
            attribute.AttributeType == typeof(TraitAttribute)
            && attribute.ConstructorArguments.Count == 2
            && string.Equals(attribute.ConstructorArguments[0].Value as string, "Category", StringComparison.Ordinal)
            && string.Equals(attribute.ConstructorArguments[1].Value as string, "Performance", StringComparison.Ordinal));
        promptSet.Prompts.Count.ShouldBe(20);
        typeof(SkillBenchmarkPromptSet).Assembly.GetManifestResourceNames().ShouldContain(
            "Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json");
    }
}
