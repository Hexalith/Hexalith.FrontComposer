namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkPrompt(
    string Id,
    string Text,
    IReadOnlyList<string> ExpectedShape);
