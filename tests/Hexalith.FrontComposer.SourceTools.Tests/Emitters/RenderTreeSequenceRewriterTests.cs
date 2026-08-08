using System.Globalization;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.SourceTools.Emitters;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 11.21 (ASP0006) — unit coverage for the central render-tree sequence rewriter that
/// replaces runtime <c>seq++</c> counters with emitter-assigned literals.
/// </summary>
public sealed partial class RenderTreeSequenceRewriterTests {
    [Fact]
    public void AssignLiterals_NumbersCallSitesInDocumentOrderAndDropsTheCounterDeclaration() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "x");
                    builder.CloseElement();
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        rewritten.ShouldNotContain("int seq = 0;");
        rewritten.ShouldNotContain("seq++");
        rewritten.ShouldContain("builder.OpenElement(0, \"div\");");
        rewritten.ShouldContain("builder.AddAttribute(1, \"class\", \"x\");");
    }

    [Fact]
    public void AssignLiterals_ReusesTheSameLiteralForEveryIterationOfARuntimeLoop() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    foreach (var item in Items)
                    {
                        builder.OpenComponent<Row>(seq++);
                        builder.SetKey(item.Key);
                        builder.CloseComponent();
                    }

                    builder.AddContent(seq++, "after");
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        // The loop body is one source location, so it keeps a single literal across iterations;
        // the statement after the loop gets the next one regardless of how many rows rendered.
        rewritten.ShouldContain("builder.OpenComponent<Row>(0);");
        rewritten.ShouldContain("builder.AddContent(1, \"after\");");
    }

    [Fact]
    public void AssignLiterals_KeepsConditionalBranchesOnStableNumbers() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    if (flag)
                    {
                        builder.AddContent(seq++, "a");
                    }

                    builder.AddContent(seq++, "b");
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        // This is the defect ASP0006 names: with a runtime counter "b" was 0 or 1 depending on
        // `flag`. With literals it is always 1.
        rewritten.ShouldContain("builder.AddContent(0, \"a\");");
        rewritten.ShouldContain("builder.AddContent(1, \"b\");");
    }

    [Fact]
    public void AssignLiterals_HonoursScopeOffsetsAndResets() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "head");
                    seq = 100;
                    builder.AddContent(seq++, "body");
                    builder.AddContent(seq++, "tail");
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        rewritten.ShouldNotContain("seq = 100;");
        rewritten.ShouldContain("builder.AddContent(0, \"head\");");
        rewritten.ShouldContain("builder.AddContent(100, \"body\");");
        rewritten.ShouldContain("builder.AddContent(101, \"tail\");");
    }

    [Fact]
    public void AssignLiterals_NumbersShadowedNestedScopesIndependently() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "outer");
                    RenderFragment body = (RenderTreeBuilder inner) =>
                    {
                        int seq = 50;
                        inner.AddContent(seq++, "inner");
                        inner.AddContent(seq++, "inner2");
                    };
                    builder.AddContent(seq++, "outer2");
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        rewritten.ShouldContain("builder.AddContent(0, \"outer\");");
        rewritten.ShouldContain("inner.AddContent(50, \"inner\");");
        rewritten.ShouldContain("inner.AddContent(51, \"inner2\");");
        rewritten.ShouldContain("builder.AddContent(1, \"outer2\");");
    }

    [Fact]
    public void AssignLiterals_LeavesACounterAloneWhenItEscapesByReference() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        // Fail-safe: an unsupported usage leaves the whole counter untouched so the emitted code
        // always keeps compiling. The ASP0006 finding is then visible rather than silently broken.
        rewritten.ShouldBe(source);
    }

    [Fact]
    public void AssignLiterals_LeavesNonSequenceLocalsAlone() {
        const string source = """
            class C
            {
                void M()
                {
                    int count = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        count++;
                    }

                    Use(count);
                }
            }
            """;

        RenderTreeSequenceRewriter.AssignLiterals(source).ShouldBe(source);
    }

    [Fact]
    public void AssignLiterals_RewritesInsideDebugOnlyBlocksSoBothConfigurationsCompile() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
            #if DEBUG
                    builder.AddContent(seq++, "debug");
            #endif
                    builder.AddContent(seq++, "always");
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiterals(source);

        rewritten.ShouldNotContain("seq++");
        rewritten.ShouldContain("builder.AddContent(0, \"debug\");");
        rewritten.ShouldContain("builder.AddContent(1, \"always\");");
    }

    [Fact]
    public void AssignLiterals_LeavesTheDocumentAloneWhenACounterIsOnlyReadFromAnElseBranch() {
        // The rewriter parses with DEBUG defined, so the #else body is disabled trivia it cannot see.
        // Removing `int seq = 0;` would leave the Release branch reading an undeclared counter.
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
            #if DEBUG
                    builder.AddContent(seq++, "debug");
            #else
                    builder.AddContent(seq++, "release");
            #endif
                }
            }
            """;

        RenderTreeSequenceRewriter.AssignLiterals(source).ShouldBe(source);
    }

    [Fact]
    public void AssignLiterals_LeavesTheDocumentAloneWhenACounterIsOnlyReadFromAnElifBranch() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
            #if DEBUG
                    builder.AddContent(seq++, "debug");
            #elif TRACE
                    builder.AddContent(seq++, "trace");
            #endif
                }
            }
            """;

        RenderTreeSequenceRewriter.AssignLiterals(source).ShouldBe(source);
    }

    [Fact]
    public void AssignLiterals_LeavesTheDocumentAloneWhenACounterIsUsedInsideANonDebugConditional() {
        // #if NET8_0_OR_GREATER is compiled out by this parse, so the increment inside it is
        // invisible: numbering the visible sites and deleting the declaration would break the other
        // configuration.
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "always");
            #if NET8_0_OR_GREATER
                    builder.AddContent(seq++, "modern");
            #endif
                }
            }
            """;

        RenderTreeSequenceRewriter.AssignLiterals(source).ShouldBe(source);
    }

    [Fact]
    public void AssignLiterals_LeavesTheSourceUnchangedWhenItDoesNotParse() {
        // Every edit is a span into the parsed tree. Error recovery moves those spans, so a document
        // that did not parse must be handed back byte-for-byte instead of being spliced.
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.OpenElement(seq++, "div"
                    builder.AddAttribute(seq++, "class", "x");
            """;

        RenderTreeSequenceRewriter.AssignLiterals(source).ShouldBe(source);
    }

    [Fact]
    public void Apply_FailsLoudlyWhenTwoEditsOverlap() {
        // Edits are applied back-to-front; overlapping spans would splice the document at an offset
        // the second edit never measured and silently corrupt generated text.
        List<RenderTreeSequenceRewriter.TextEdit> overlapping =
        [
            new RenderTreeSequenceRewriter.TextEdit(TextSpan.FromBounds(0, 5), "A"),
            new RenderTreeSequenceRewriter.TextEdit(TextSpan.FromBounds(3, 8), "B"),
        ];

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.TextEdit.Apply("0123456789", overlapping));

        thrown.Message.ShouldContain("overlap");
    }

    [Fact]
    public void Apply_AppliesNonOverlappingEditsBackToFront() {
        List<RenderTreeSequenceRewriter.TextEdit> edits =
        [
            new RenderTreeSequenceRewriter.TextEdit(TextSpan.FromBounds(0, 2), "A"),
            new RenderTreeSequenceRewriter.TextEdit(TextSpan.FromBounds(4, 6), "B"),
        ];

        RenderTreeSequenceRewriter.TextEdit.Apply("0123456789", edits).ShouldBe("A23B6789");
    }

    [Fact]
    public void ContainsRuntimeSequenceArgument_SeparatesASurvivingCounterFromAnAssignedLiteral() {
        const string failedSafe = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;
        const string rewritten = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.AddContent(0, "a");
                    for (int index = 0; index < 3; index++)
                    {
                        builder.AddContent(1, index);
                    }
                }
            }
            """;

        RenderTreeSequenceRewriter.ContainsRuntimeSequenceArgument(failedSafe).ShouldBeTrue();

        // An unrelated `index++` in a loop header is not a sequence argument.
        RenderTreeSequenceRewriter.ContainsRuntimeSequenceArgument(rewritten).ShouldBeFalse();
    }

    [Fact]
    public void AssignLiteralsOrFail_FailsTheGenerationOnlyWhenTheRewriteFailedSafe() {
        const string failsSafe = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;
        const string rewritable = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq++, "a");
                }
            }
            """;

        // The emitters no longer bracket their output with the ASP0006 control pair, so a fail-safe
        // rewrite would otherwise ship an ASP0006 warning — a build break under TreatWarningsAsErrors
        // — into a consumer that has no control for it. Re-emitting the control is not available
        // either: an emitted control is an analyzer-policy control the approved ledger says this
        // emitter no longer carries. So the generation fails, naming the exact call site.
        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafe));
        thrown.Message.ShouldContain("builder.AddContent(seq++, \"a\")");
        thrown.Message.ShouldContain("ASP0006");

        string clean = RenderTreeSequenceRewriter.AssignLiteralsOrFail(rewritable);
        clean.ShouldNotContain("ASP0006");
        clean.ShouldContain("builder.AddContent(0, \"a\");");
    }

    [Fact]
    public void AssignLiteralsOrFail_DetectsSpacedIncrementLeftByFailSafe() {
        // A leftover `seq ++` (space before ++) must still trip OrFail; the prefilter used to walk
        // only identifier characters immediately before `++` and would skip the Roslyn scan.
        const string failsSafeSpaced = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(seq ++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafeSpaced));
        thrown.Message.ShouldContain("builder.AddContent(seq ++, \"a\")");
        thrown.Message.ShouldContain("ASP0006");
    }

    [Fact]
    public void RuntimeSequenceArgumentPattern_MatchesSpacedAndTightIncrementArguments() {
        // Pins the packaged / ShouldUseLiteralRenderTreeSequences gate independently of OrFail.
        // Dropping \s* from the regex would leave AssignLiteralsOrFail_DetectsSpacedIncrementLeftByFailSafe
        // green while spaced leftovers bypass the consumer scan.
        RuntimeSequenceArgumentPattern().IsMatch("""builder.AddContent(seq ++, "a")""").ShouldBeTrue();
        RuntimeSequenceArgumentPattern().IsMatch("""builder.AddContent(seq++, "a")""").ShouldBeTrue();
        RuntimeSequenceArgumentPattern().IsMatch("""builder.AddContent(0, "a")""").ShouldBeFalse();
    }

    [Fact]
    public void AssignLiterals_IsDeterministicAndIdempotent() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "x");
                    builder.CloseElement();
                }
            }
            """;

        string first = RenderTreeSequenceRewriter.AssignLiterals(source);
        string second = RenderTreeSequenceRewriter.AssignLiterals(source);

        second.ShouldBe(first);
        RenderTreeSequenceRewriter.AssignLiterals(first).ShouldBe(first);
    }

    /// <summary>
    /// Asserts that generated source carries only emitter-assigned literal sequence arguments, that
    /// it did not fall back to an ASP0006 control, and that it still parses without syntax errors in
    /// <em>both</em> consumer configurations.
    /// </summary>
    /// <param name="generatedSource">Emitted C# source.</param>
    /// <remarks>
    /// Parsing only with the default options leaves <c>#if DEBUG</c> blocks as disabled text, so the
    /// dev-mode regions the rewriter renumbered would never be inspected. Both configurations are
    /// parsed because the rewriter assigns literals under <c>DEBUG</c> while a Release consumer
    /// compiles the same document without it.
    /// </remarks>
    internal static void ShouldUseLiteralRenderTreeSequences(string generatedSource) {
        Match runtimeSequence = RuntimeSequenceArgumentPattern().Match(generatedSource);
        runtimeSequence.Success.ShouldBeFalse(
            "Generated source still passes a runtime render-tree sequence argument at "
            + DescribeSite(generatedSource, runtimeSequence.Index)
            + ". ASP0006 requires a compile-time constant that identifies a source location.");

        generatedSource.ShouldNotContain("ASP0006");

        ShouldParseCleanly(generatedSource, CSharpParseOptions.Default, "Release (DEBUG undefined)");
        ShouldParseCleanly(
            generatedSource,
            CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG"),
            "Debug (DEBUG defined)");
    }

    private static void ShouldParseCleanly(string generatedSource, CSharpParseOptions options, string configuration) {
        Diagnostic[] diagnostics = [.. CSharpSyntaxTree.ParseText(generatedSource, options).GetDiagnostics()];

        diagnostics.ShouldBeEmpty(
            $"Generated source does not parse cleanly under {configuration}: "
            + string.Join(
                " | ",
                diagnostics.Select(diagnostic =>
                    diagnostic.Id
                    + " @ "
                    + DescribeSite(generatedSource, diagnostic.Location.SourceSpan.Start)
                    + " - "
                    + diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))));
    }

    /// <summary>Names an offset by line number and line text so a failure identifies the site.</summary>
    /// <param name="source">The source the offset belongs to.</param>
    /// <param name="index">A zero-based character offset.</param>
    /// <returns>A human-readable description of the site.</returns>
    private static string DescribeSite(string source, int index) {
        if (index < 0 || index > source.Length) {
            return "offset " + index.ToString(CultureInfo.InvariantCulture);
        }

        int lineNumber = 1;
        for (int position = 0; position < index; position++) {
            if (source[position] == '\n') {
                lineNumber++;
            }
        }

        int lineStart = index == 0 ? 0 : source.LastIndexOf('\n', index - 1) + 1;
        int lineEnd = source.IndexOf('\n', index);
        if (lineEnd < 0) {
            lineEnd = source.Length;
        }

        return "line "
            + lineNumber.ToString(CultureInfo.InvariantCulture)
            + ": "
            + source[lineStart..lineEnd].Trim();
    }

    [GeneratedRegex(
        @"\.(?:OpenElement|OpenComponent|AddAttribute|AddContent|AddMarkupContent|OpenRegion|AddMultipleAttributes|AddElementReferenceCapture|AddComponentReferenceCapture)(?:<[^(]*>)?\(\s*[A-Za-z_][A-Za-z0-9_]*\s*\+\+",
        RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeSequenceArgumentPattern();
}
