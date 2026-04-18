namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Companion style constants for the <see cref="Typography"/> tokens (Story 3-1 D2). The
/// Fluent UI v5 <c>FluentText</c> <c>Font</c> parameter is the primary mechanism for switching to a
/// monospace family, but non-FluentText consumers (e.g. <c>&lt;code&gt;</c> blocks, inline HTML)
/// can reference <see cref="CodeFontFamily"/> directly in a <c>style</c> attribute.
/// </summary>
public static class TypographyStyle
{
    /// <summary>
    /// CSS <c>font-family</c> stack used for code-style content. Prioritises Cascadia Code on
    /// Windows, then Cascadia Mono, Consolas, and Courier New as fallbacks, terminating with
    /// the generic <c>monospace</c> family.
    /// </summary>
    public const string CodeFontFamily = "'Cascadia Code', 'Cascadia Mono', Consolas, 'Courier New', monospace";
}
