using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Replaces runtime <c>RenderTreeBuilder</c> sequence counters in emitted source with literals
/// assigned at generator execution time.
/// </summary>
/// <remarks>
/// <para>
/// ASP0006 requires every <c>RenderTreeBuilder</c> sequence argument to be a compile-time constant
/// that identifies a <em>source location</em>, not a value produced by execution. The emitters build
/// their render trees with local <c>int seq = 0; ... builder.AddAttribute(seq++, ...)</c> counters,
/// which is exactly the pattern the rule forbids: a conditionally emitted frame shifts every
/// following number and defeats the Blazor diff.
/// </para>
/// <para>
/// This rewriter is the single, central numbering scheme for all emitters. Running it once over the
/// finished text — rather than threading an allocator through several hundred call sites — keeps the
/// emitters readable, keeps every emitted call site numbered by its position in the generated
/// document, and makes a loop body reuse the same literal on every iteration, which is what the
/// Blazor render-tree diff expects of a repeated source location.
/// </para>
/// <para>
/// The transform is intentionally conservative and fails safe. A counter is rewritten only when
/// <em>every</em> reference to it, in the whole document, is either a postfix increment used directly
/// as a call argument or a scope-reset assignment of a constant. Any other reference — a
/// <c>ref</c> argument, a bare read, a compound assignment — leaves that counter completely
/// untouched, so the emitted code always keeps compiling.
/// </para>
/// <para>
/// The text is parsed with <c>DEBUG</c> defined so <c>#if DEBUG</c> dev-mode blocks are live syntax
/// rather than disabled trivia. Their sequence numbers are therefore assigned too, and a Release
/// consumer simply skips those literals, leaving a numbering gap — the intended behaviour, since
/// literals denote source locations rather than execution order.
/// </para>
/// <para>
/// Parsing under a single configuration is only safe while that configuration sees the whole
/// document. A counter referenced solely from an <c>#else</c>, an <c>#elif</c>, or a non-<c>DEBUG</c>
/// <c>#if</c> region is invisible to this parse, so its declaration could be deleted while a live
/// branch still reads it. The rewriter therefore refuses to touch a document that contains any such
/// directive, and independently refuses to touch a counter whose name appears in disabled-directive
/// text. It also refuses to rewrite text it could not parse cleanly: rewriting spans computed from an
/// error-recovered tree would corrupt the emitted document.
/// </para>
/// </remarks>
internal static class RenderTreeSequenceRewriter {
    private static readonly CSharpParseOptions ParseOptions =
        new CSharpParseOptions(LanguageVersion.Latest)
            .WithPreprocessorSymbols("DEBUG");

    /// <summary>
    /// <c>RenderTreeBuilder</c> members whose first argument is the sequence number ASP0006 governs.
    /// </summary>
    private static readonly HashSet<string> SequenceMethodNames = new HashSet<string>(StringComparer.Ordinal) {
        "OpenElement",
        "OpenComponent",
        "AddAttribute",
        "AddContent",
        "AddMarkupContent",
        "OpenRegion",
        "AddMultipleAttributes",
        "AddElementReferenceCapture",
        "AddComponentReferenceCapture",
    };

    /// <summary>
    /// Rewrites runtime sequence counters in <paramref name="source"/> into literals.
    /// </summary>
    /// <param name="source">Generated C# source text.</param>
    /// <returns>
    /// The source with every eligible counter declaration removed and every counter increment
    /// replaced by the literal for that source location. Ineligible counters are returned unchanged.
    /// </returns>
    public static string AssignLiterals(string source) {
        if (string.IsNullOrEmpty(source) || source.IndexOf("++", StringComparison.Ordinal) < 0) {
            return source;
        }

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, ParseOptions);
        SyntaxNode root = tree.GetRoot();

        // Fail safe on unparseable text: every edit below is a span into this tree, so an
        // error-recovered shape would silently relocate or truncate generated code.
        foreach (Diagnostic diagnostic in tree.GetDiagnostics()) {
            if (diagnostic.Severity == DiagnosticSeverity.Error) {
                return source;
            }
        }

        // Fail safe on any conditional region this parse cannot see (see the type remarks).
        if (HasUnseenConditionalRegion(root)) {
            return source;
        }

        List<CounterScope> scopes = CollectCounterScopes(root);
        if (scopes.Count == 0) {
            return source;
        }

        List<TextEdit> edits = [];
        foreach (CounterScope scope in scopes) {
            if (!scope.IsRewritable) {
                continue;
            }

            edits.Add(TextEdit.RemoveStatement(source, scope.Declaration));
            int next = scope.InitialValue;
            foreach (CounterReference reference in scope.References.OrderBy(static r => r.Position)) {
                if (reference.ResetValue.HasValue) {
                    next = reference.ResetValue.Value;
                    edits.Add(TextEdit.RemoveStatement(source, reference.Statement!));
                    continue;
                }

                edits.Add(new TextEdit(
                    reference.Span,
                    next.ToString(CultureInfo.InvariantCulture)));
                next++;
            }
        }

        return TextEdit.Apply(source, edits);
    }

    /// <summary>
    /// Rewrites runtime sequence counters into literals and fails the generation when the fail-safe
    /// leaves a runtime sequence argument behind.
    /// </summary>
    /// <param name="source">Generated C# source text.</param>
    /// <returns>The rewritten source, which is always free of runtime sequence arguments.</returns>
    /// <exception cref="InvalidOperationException">
    /// A runtime sequence argument survived the rewrite.
    /// </exception>
    /// <remarks>
    /// The emitters no longer bracket their output with the ASP0006 disable/restore pair, so a
    /// fail-safe rewrite would otherwise ship a <c>seq++</c> sequence argument into a consumer that
    /// has no control covering it — an ASP0006 warning, and a build break under
    /// <c>TreatWarningsAsErrors</c>, inside generated code the consumer cannot edit. Failing here
    /// turns that into a generator failure naming the exact call site instead. Re-emitting the pragma
    /// pair is not an option: an emitted control is an analyzer-policy control, and the approved
    /// exception ledger records this emitter as carrying none.
    /// </remarks>
    public static string AssignLiteralsOrFail(string source) {
        string rewritten = AssignLiterals(source);
        string? survivingCallSite = FindRuntimeSequenceArgument(rewritten);
        if (survivingCallSite is null) {
            return rewritten;
        }

        throw new InvalidOperationException(
            "Render-tree sequence literal assignment failed safe and left a runtime sequence argument in generated output: "
            + survivingCallSite
            + ". Emitting it would raise ASP0006 in the consumer build, which has no control covering generated code.");
    }

    /// <summary>
    /// Reports whether any <c>RenderTreeBuilder</c> call in <paramref name="source"/> still passes a
    /// sequence argument produced by execution rather than by this rewriter.
    /// </summary>
    /// <param name="source">Emitted C# source text.</param>
    /// <returns><see langword="true"/> when a runtime sequence argument survived.</returns>
    internal static bool ContainsRuntimeSequenceArgument(string source)
        => FindRuntimeSequenceArgument(source) is not null;

    /// <summary>
    /// Finds the first <c>RenderTreeBuilder</c> call whose sequence argument is still produced by
    /// execution.
    /// </summary>
    /// <param name="source">Emitted C# source text.</param>
    /// <returns>The offending call text, or <see langword="null"/> when there is none.</returns>
    private static string? FindRuntimeSequenceArgument(string source) {
        if (string.IsNullOrEmpty(source) || !HasIncrementInFirstArgumentPosition(source)) {
            return null;
        }

        SyntaxNode root = CSharpSyntaxTree.ParseText(source, ParseOptions).GetRoot();
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (invocation.ArgumentList.Arguments.Count == 0) {
                continue;
            }

            string? invokedName = InvokedMemberName(invocation.Expression);
            if (invokedName is null || !SequenceMethodNames.Contains(invokedName)) {
                continue;
            }

            ExpressionSyntax sequenceArgument = invocation.ArgumentList.Arguments[0].Expression;
            foreach (SyntaxNode node in sequenceArgument.DescendantNodesAndSelf()) {
                if (node.IsKind(SyntaxKind.PostIncrementExpression)
                    || node.IsKind(SyntaxKind.PreIncrementExpression)
                    || node.IsKind(SyntaxKind.PostDecrementExpression)
                    || node.IsKind(SyntaxKind.PreDecrementExpression)) {
                    return invocation.ToString();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Cheap text prefilter for <see cref="FindRuntimeSequenceArgument"/>: reports whether any
    /// <c>++</c>/<c>--</c> operator applies to an identifier that opens an argument list.
    /// </summary>
    /// <remarks>
    /// A sequence argument is always the first argument, so the identifier it increments is preceded
    /// by <c>(</c>. Loop counters (<c>for (int i = 0; i &lt; n; i++)</c>) and accumulators
    /// (<c>count++;</c>) are not, which keeps a well-formed rewrite from paying for a second parse of
    /// the whole document.
    /// </remarks>
    private static bool HasIncrementInFirstArgumentPosition(string source) {
        int index = source.IndexOf("++", StringComparison.Ordinal);
        while (index >= 0) {
            if (StartsArgumentList(source, index)) {
                return true;
            }

            index = source.IndexOf("++", index + 2, StringComparison.Ordinal);
        }

        index = source.IndexOf("--", StringComparison.Ordinal);
        while (index >= 0) {
            if (StartsArgumentList(source, index)) {
                return true;
            }

            index = source.IndexOf("--", index + 2, StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    /// Reports whether the identifier ending at <paramref name="operatorIndex"/> is the first thing
    /// inside an argument list, or is itself prefixed by an operator that opens one.
    /// </summary>
    /// <remarks>
    /// Whitespace between the identifier and the operator (<c>seq ++</c>) is skipped so a fail-safe
    /// leftover with spaces cannot bypass the OrFail Roslyn scan via this prefilter.
    /// </remarks>
    private static bool StartsArgumentList(string source, int operatorIndex) {
        // Prefix form: `(++seq, ...)` or `( ++seq, ...)`.
        int cursor = operatorIndex;
        while (cursor > 0 && char.IsWhiteSpace(source[cursor - 1])) {
            cursor--;
        }

        if (cursor > 0 && source[cursor - 1] == '(') {
            return true;
        }

        int start = cursor;
        while (start > 0 && IsIdentifierCharacter(source[start - 1])) {
            start--;
        }

        return start != cursor && start > 0 && source[start - 1] == '(';
    }

    /// <summary>Extracts the invoked member's simple name, ignoring any generic argument list.</summary>
    private static string? InvokedMemberName(ExpressionSyntax expression) {
        SimpleNameSyntax? name = expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax simple => simple,
            _ => null,
        };

        return name?.Identifier.ValueText;
    }

    /// <summary>
    /// Reports whether the document carries a conditional region this single-configuration parse
    /// cannot see: an <c>#else</c>, an <c>#elif</c>, or an <c>#if</c> on anything but bare
    /// <c>DEBUG</c>.
    /// </summary>
    /// <remarks>
    /// Walks the directive chain rather than the syntax tree so a document without directives — the
    /// common case — costs a single flag check.
    /// </remarks>
    private static bool HasUnseenConditionalRegion(SyntaxNode root) {
        if (!root.ContainsDirectives) {
            return false;
        }

        for (DirectiveTriviaSyntax? directive = root.GetFirstDirective();
            directive is not null;
            directive = directive.GetNextDirective()) {
            switch (directive) {
                case ElseDirectiveTriviaSyntax:
                case ElifDirectiveTriviaSyntax:
                    return true;
                case IfDirectiveTriviaSyntax ifDirective
                    when ifDirective.Condition is not IdentifierNameSyntax identifier
                        || !string.Equals(identifier.Identifier.ValueText, "DEBUG", StringComparison.Ordinal):
                    return true;
                default:
                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Concatenates every disabled-directive region in the document, which this parse sees only as
    /// raw trivia.
    /// </summary>
    /// <returns>The disabled text, or <see langword="null"/> when the document has none.</returns>
    private static string? CollectDisabledText(SyntaxNode root) {
        if (!root.ContainsDirectives) {
            return null;
        }

        StringBuilder? disabled = null;
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true)) {
            if (!trivia.IsKind(SyntaxKind.DisabledTextTrivia)) {
                continue;
            }

            disabled ??= new StringBuilder();
            _ = disabled.Append(trivia.ToFullString());
        }

        return disabled?.ToString();
    }

    private static bool ContainsWholeWord(string text, string word) {
        int index = text.IndexOf(word, StringComparison.Ordinal);
        while (index >= 0) {
            bool leftBoundary = index == 0 || !IsIdentifierCharacter(text[index - 1]);
            int end = index + word.Length;
            bool rightBoundary = end >= text.Length || !IsIdentifierCharacter(text[end]);
            if (leftBoundary && rightBoundary) {
                return true;
            }

            index = text.IndexOf(word, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsIdentifierCharacter(char value)
        => char.IsLetterOrDigit(value) || value == '_';

    /// <summary>
    /// Collects every local <c>int</c> declaration that could be a render-tree sequence counter and
    /// classifies all of its references.
    /// </summary>
    private static List<CounterScope> CollectCounterScopes(SyntaxNode root) {
        List<CounterScope> scopes = [];
        foreach (LocalDeclarationStatementSyntax statement in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()) {
            if (statement.Declaration.Variables.Count != 1) {
                continue;
            }

            if (statement.Declaration.Type is not PredefinedTypeSyntax predefined
                || !predefined.Keyword.IsKind(SyntaxKind.IntKeyword)) {
                continue;
            }

            VariableDeclaratorSyntax declarator = statement.Declaration.Variables[0];
            if (declarator.Initializer is null
                || !TryGetIntLiteral(declarator.Initializer.Value, out int initialValue)) {
                continue;
            }

            if (statement.Parent is null) {
                continue;
            }

            scopes.Add(new CounterScope(
                declarator.Identifier.ValueText,
                statement,
                statement.Parent,
                initialValue));
        }

        if (scopes.Count == 0) {
            return scopes;
        }

        Dictionary<string, List<CounterScope>> byName = [];
        foreach (CounterScope scope in scopes) {
            if (!byName.TryGetValue(scope.Name, out List<CounterScope>? sameName)) {
                sameName = [];
                byName[scope.Name] = sameName;
            }

            sameName.Add(scope);
        }

        foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            if (!byName.TryGetValue(identifier.Identifier.ValueText, out List<CounterScope>? candidates)) {
                continue;
            }

            CounterScope? owner = ResolveOwner(candidates, identifier);
            if (owner is null) {
                continue;
            }

            if (identifier.Parent is PostfixUnaryExpressionSyntax postfix
                && postfix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken)
                && postfix.Parent is ArgumentSyntax argument
                && argument.RefKindKeyword.IsKind(SyntaxKind.None)) {
                owner.References.Add(CounterReference.Increment(postfix.Span));
                continue;
            }

            if (identifier.Parent is AssignmentExpressionSyntax assignment
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                && assignment.Left == identifier
                && assignment.Parent is ExpressionStatementSyntax resetStatement
                && TryGetIntLiteral(assignment.Right, out int resetValue)) {
                owner.References.Add(CounterReference.Reset(resetStatement, resetValue));
                continue;
            }

            owner.IsRewritable = false;
        }

        // Second layer over HasUnseenConditionalRegion: a counter whose name occurs in text this
        // configuration compiled out has references the syntax walk above never saw, so removing its
        // declaration could break the branch the other configuration compiles.
        string? disabledText = CollectDisabledText(root);
        foreach (CounterScope scope in scopes) {
            if (scope.References.Count == 0) {
                scope.IsRewritable = false;
                continue;
            }

            if (disabledText is not null && ContainsWholeWord(disabledText, scope.Name)) {
                scope.IsRewritable = false;
            }
        }

        return scopes;
    }

    /// <summary>
    /// Binds an identifier to the innermost counter declaration it can refer to, honouring the
    /// shadowing the emitters rely on (an outer <c>seq</c> plus a nested lambda-local <c>seq</c>).
    /// </summary>
    private static CounterScope? ResolveOwner(List<CounterScope> candidates, IdentifierNameSyntax identifier) {
        foreach (SyntaxNode ancestor in identifier.Ancestors()) {
            foreach (CounterScope scope in candidates) {
                if (scope.ScopeNode.RawKind == ancestor.RawKind
                    && scope.ScopeNode.FullSpan == ancestor.FullSpan
                    && scope.Declaration.SpanStart < identifier.SpanStart) {
                    return scope;
                }
            }
        }

        return null;
    }

    private static bool TryGetIntLiteral(ExpressionSyntax expression, out int value) {
        if (expression is LiteralExpressionSyntax literal
            && literal.Token.IsKind(SyntaxKind.NumericLiteralToken)
            && literal.Token.Value is int parsed) {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private sealed class CounterScope(string name, LocalDeclarationStatementSyntax declaration, SyntaxNode scopeNode, int initialValue) {
        public string Name { get; } = name;

        public LocalDeclarationStatementSyntax Declaration { get; } = declaration;

        public SyntaxNode ScopeNode { get; } = scopeNode;

        public int InitialValue { get; } = initialValue;

        public bool IsRewritable { get; set; } = true;

        public List<CounterReference> References { get; } = [];
    }

    private sealed class CounterReference {
        private CounterReference(int position, TextSpan span, StatementSyntax? statement, int? resetValue) {
            Position = position;
            Span = span;
            Statement = statement;
            ResetValue = resetValue;
        }

        public int Position { get; }

        public TextSpan Span { get; }

        public StatementSyntax? Statement { get; }

        public int? ResetValue { get; }

        public static CounterReference Increment(TextSpan span)
            => new(span.Start, span, null, null);

        public static CounterReference Reset(StatementSyntax statement, int value)
            => new(statement.SpanStart, statement.Span, statement, value);
    }

    /// <summary>A single span replacement to apply to the emitted document.</summary>
    /// <param name="span">The span to replace.</param>
    /// <param name="replacement">The text to write in its place; empty erases the span.</param>
    internal sealed class TextEdit(TextSpan span, string replacement) {
        /// <summary>Gets the span this edit replaces.</summary>
        public TextSpan Span { get; } = span;

        /// <summary>Gets the text written in place of <see cref="Span"/>.</summary>
        public string Replacement { get; } = replacement;

        /// <summary>
        /// Builds an edit that erases a statement, taking the whole physical line when the statement
        /// is the only content on it so the generated text keeps its original shape.
        /// </summary>
        /// <param name="source">The document the statement belongs to.</param>
        /// <param name="statement">The statement to erase.</param>
        /// <returns>The erasing edit.</returns>
        public static TextEdit RemoveStatement(string source, StatementSyntax statement) {
            int start = statement.SpanStart;
            int end = statement.Span.End;

            int lineStart = start;
            while (lineStart > 0 && source[lineStart - 1] != '\n') {
                lineStart--;
            }

            int lineEnd = end;
            while (lineEnd < source.Length && source[lineEnd] != '\n') {
                lineEnd++;
            }

            bool onlyContentOnLine =
                IsWhiteSpaceRange(source, lineStart, start)
                && IsWhiteSpaceRange(source, end, lineEnd);

            return onlyContentOnLine
                ? new TextEdit(TextSpan.FromBounds(lineStart, Math.Min(lineEnd + 1, source.Length)), string.Empty)
                : new TextEdit(statement.Span, string.Empty);
        }

        /// <summary>
        /// Applies every edit from the end of the document backwards so earlier spans keep their
        /// offsets.
        /// </summary>
        /// <param name="source">The document to edit.</param>
        /// <param name="edits">The edits to apply; they must not overlap.</param>
        /// <returns>The edited document.</returns>
        /// <exception cref="InvalidOperationException">
        /// Two edits overlap. Applying them back-to-front would splice the generated text at an
        /// offset the second edit never measured, so the rewriter fails loudly rather than emitting a
        /// corrupted document.
        /// </exception>
        public static string Apply(string source, List<TextEdit> edits) {
            if (edits is null) {
                throw new ArgumentNullException(nameof(edits));
            }

            if (edits.Count == 0) {
                return source;
            }

            List<TextEdit> ordered = [.. edits.OrderByDescending(static e => e.Span.Start)];
            for (int index = 1; index < ordered.Count; index++) {
                TextEdit later = ordered[index - 1];
                TextEdit earlier = ordered[index];
                if (earlier.Span.End > later.Span.Start) {
                    throw new InvalidOperationException(
                        "Render-tree sequence edits overlap: ["
                        + earlier.Span.Start.ToString(CultureInfo.InvariantCulture) + ", "
                        + earlier.Span.End.ToString(CultureInfo.InvariantCulture) + ") and ["
                        + later.Span.Start.ToString(CultureInfo.InvariantCulture) + ", "
                        + later.Span.End.ToString(CultureInfo.InvariantCulture) + ").");
                }
            }

            StringBuilder builder = new(source);
            foreach (TextEdit edit in ordered) {
                _ = builder.Remove(edit.Span.Start, edit.Span.Length);
                if (edit.Replacement.Length > 0) {
                    _ = builder.Insert(edit.Span.Start, edit.Replacement);
                }
            }

            return builder.ToString();
        }

        private static bool IsWhiteSpaceRange(string source, int start, int end) {
            for (int index = start; index < end; index++) {
                if (!char.IsWhiteSpace(source[index])) {
                    return false;
                }
            }

            return true;
        }
    }
}
