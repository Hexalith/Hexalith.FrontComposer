using Hexalith.FrontComposer.SourceTools.Emitters;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Tests;

/// <summary>
/// Helpers for asserting on generated <c>RenderTreeBuilder</c> code without pinning the
/// emitter-assigned sequence literals introduced by Story 11.21 (ASP0006).
/// </summary>
internal static class GeneratedRenderTreeText {
    /// <summary>
    /// Replaces each literal sequence argument with <c>#</c> so assertions can anchor on the call
    /// shape. Runtime expressions remain visible, including expressions whose first token is a number.
    /// </summary>
    /// <param name="generatedSource">Generated C# source or an excerpt.</param>
    /// <returns>The source with literal sequence arguments masked.</returns>
    public static string MaskSequenceArguments(string generatedSource) {
        SyntaxNode root = CSharpSyntaxTree.ParseText(
            generatedSource,
            CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG")).GetRoot();
        HashSet<TextSpan> literals = [];
        HashSet<TextSpan> nonliterals = [];
        CollectSequenceSpans(root, literals, nonliterals);
        if (root.DescendantTrivia().Any(static trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia))) {
            CollectSequenceSpans(CSharpSyntaxTree.ParseText(generatedSource).GetRoot(), literals, nonliterals);
        }

        // A directive can change the enclosing expression while leaving the literal token active.
        List<RenderTreeSequenceRewriter.TextEdit> edits = [];
        foreach (TextSpan literal in literals) {
            if (!nonliterals.Any(nonliteral => nonliteral.OverlapsWith(literal))) {
                edits.Add(new RenderTreeSequenceRewriter.TextEdit(literal, "#"));
            }
        }

        return RenderTreeSequenceRewriter.TextEdit.Apply(generatedSource, edits);
    }

    private static void CollectSequenceSpans(SyntaxNode root, HashSet<TextSpan> literals, HashSet<TextSpan> nonliterals) {
        foreach (var (_, sequence) in RenderTreeSequenceRewriter.GetSequenceArguments(root)) {
            _ = (RenderTreeSequenceRewriter.IsSequenceLiteral(sequence) ? literals : nonliterals).Add(sequence.Span);
        }
    }
}
