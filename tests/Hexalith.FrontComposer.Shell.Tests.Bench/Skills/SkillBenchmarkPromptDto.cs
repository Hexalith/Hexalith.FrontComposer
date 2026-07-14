namespace Hexalith.FrontComposer.Mcp.Skills;

internal sealed record SkillBenchmarkPromptDto(
    string Id,
    string Text,
    IReadOnlyList<string> ExpectedShape);
