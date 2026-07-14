namespace Hexalith.FrontComposer.Mcp.Skills;

internal sealed record SkillBenchmarkPromptSetDto(
    string Version,
    IReadOnlyList<SkillBenchmarkPromptDto> Prompts);
