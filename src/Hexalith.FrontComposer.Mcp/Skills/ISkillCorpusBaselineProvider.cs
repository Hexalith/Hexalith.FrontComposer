namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>
/// P-41: minimal baseline-provider seam so a release pipeline can compare a current corpus
/// snapshot to a prior baseline and trigger the migration-guide guardrail when public API
/// references drift. Story 8-5 ships the seam + a stub; baseline persistence (loading prior
/// snapshots from package output) is intentionally deferred — this is the framework hook that
/// release tooling will populate.
/// </summary>
public interface ISkillCorpusBaselineProvider {
    SkillCorpusSnapshot? GetBaseline();
}
