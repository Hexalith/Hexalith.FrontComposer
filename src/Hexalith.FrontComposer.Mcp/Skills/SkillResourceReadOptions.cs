namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>
/// P-27: bounded-response policy for skill resource reads. The default cap mirrors the
/// projection-renderer markdown budget; hosts that need a different value can construct the
/// provider with a custom cap. Reads exceeding the cap return <c>Failure(SkillResourceTooLarge)</c>
/// rather than truncating, because skill content is reference material and a partial fence can
/// mislead an agent.
/// </summary>
public sealed record SkillResourceReadOptions(int MaxCharacters) {
    public const int DefaultMaxCharacters = 32 * 1024;

    public static SkillResourceReadOptions Default { get; } = new(DefaultMaxCharacters);
}
