using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.SourceTools.Tests;

/// <summary>
/// Helpers for asserting on generated <c>RenderTreeBuilder</c> code without pinning the
/// emitter-assigned sequence literals introduced by Story 11.21 (ASP0006).
/// </summary>
internal static partial class GeneratedRenderTreeText {
    /// <summary>
    /// Replaces the literal sequence argument of every generated <c>RenderTreeBuilder</c> call with
    /// <c>#</c> so a behavioural assertion can anchor on the shape of the call rather than on a
    /// number that shifts whenever a neighbouring frame is added or removed.
    /// </summary>
    /// <remarks>
    /// A runtime counter argument (<c>seq++</c>) is deliberately left untouched: an assertion written
    /// against <c>#</c> therefore still fails if ASP0006-violating emission ever comes back, so the
    /// mask cannot make a regression invisible.
    /// </remarks>
    /// <param name="generatedSource">Generated C# source.</param>
    /// <returns>The source with literal sequence arguments masked.</returns>
    public static string MaskSequenceArguments(string generatedSource)
        => SequenceArgumentPattern().Replace(generatedSource, "${call}#");

    [GeneratedRegex(
        @"(?<call>\.(?:OpenElement|OpenComponent|AddAttribute|AddContent|AddMarkupContent|OpenRegion|AddMultipleAttributes|AddElementReferenceCapture|AddComponentReferenceCapture)(?:<[^(]*>)?\()\d+",
        RegexOptions.CultureInvariant)]
    private static partial Regex SequenceArgumentPattern();
}
