namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed class EmptySkillCorpusBaselineProvider : ISkillCorpusBaselineProvider {
    public SkillCorpusSnapshot? GetBaseline() => null;
}
