using System.Globalization;

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
public sealed class RenderTreeSequenceRewriterTests {
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
    public void AssignLiteralsOrFail_DetectsIncrementAfterBlockCommentTrivia() {
        const string failsSafeWithComment = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(/* sequence */ ++seq, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafeWithComment));

        thrown.Message.ShouldContain("builder.AddContent(/* sequence */ ++seq, \"a\")");
        thrown.Message.ShouldContain("ASP0006");
    }

    [Fact]
    public void AssignLiteralsOrFail_DetectsIncrementAfterLineCommentTrivia() {
        const string failsSafeWithComment = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(
                        // sequence
                        seq++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafeWithComment));

        thrown.Message.ShouldContain("builder.AddContent");
        thrown.Message.ShouldContain("// sequence");
        thrown.Message.ShouldContain("seq++, \"a\"");
        thrown.Message.ShouldContain("ASP0006");
    }

    [Fact]
    public void AssignLiteralsOrFail_DetectsPostfixIncrementAfterBlockCommentTrivia() {
        const string failsSafeWithComment = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(/* sequence */ seq++, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafeWithComment));

        thrown.Message.ShouldContain("builder.AddContent(/* sequence */ seq++, \"a\")");
        thrown.Message.ShouldContain("ASP0006");
    }

    [Fact]
    public void AssignLiteralsOrFail_DetectsPrefixIncrementAfterLineCommentTrivia() {
        const string failsSafeWithComment = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddContent(
                        // sequence
                        ++seq, "a");
                    Helper(builder, ref seq);
                }
            }
            """;

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(failsSafeWithComment));

        thrown.Message.ShouldContain("builder.AddContent");
        thrown.Message.ShouldContain("// sequence");
        thrown.Message.ShouldContain("++seq, \"a\"");
        thrown.Message.ShouldContain("ASP0006");
    }

    [Theory]
    [InlineData("""builder.AddContent(/* sequence */ ++seq, "a")""")]
    [InlineData("""builder.AddContent(/* sequence */ seq++, "a")""")]
    [InlineData("""builder.AddContent(seq /* c */ ++, "a")""")]
    public void ShouldUseLiteralRenderTreeSequences_RejectsIncrementAfterBlockCommentTrivia(string invocation) {
        string source = "class C { void BuildRenderTree(RenderTreeBuilder builder) { "
            + invocation
            + "; } }";

        Should.Throw<ShouldAssertException>(() => ShouldUseLiteralRenderTreeSequences(source));
        RenderTreeSequenceRewriter.FindRuntimeSequenceArgument(source).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("seq++")]
    [InlineData("++seq")]
    public void ShouldUseLiteralRenderTreeSequences_RejectsIncrementAfterLineCommentTrivia(string increment) {
        string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.AddContent(
                        // sequence
                        INCREMENT, "a");
                }
            }
            """.Replace("INCREMENT", increment, StringComparison.Ordinal);

        Should.Throw<ShouldAssertException>(() => ShouldUseLiteralRenderTreeSequences(source));
        RenderTreeSequenceRewriter.FindRuntimeSequenceArgument(source).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("builder.AddContent(seq, \"a\")")]
    [InlineData("builder.AddContent(12 + seq, \"a\")")]
    [InlineData("builder.AddContent(GetSequence(), \"a\")")]
    [InlineData("builder.AddContent(--seq, \"a\")")]
    [InlineData("builder.AddContent(textContent: \"a\", sequence: seq)")]
    [InlineData("builder.AddComponentParameter(name: \"Value\", value: 42, sequence: seq)")]
    public void AssignLiteralsOrFail_RejectsNonliteralSequenceArguments(string invocation) {
        string source = "class C { void BuildRenderTree(RenderTreeBuilder builder) { " + invocation + "; } }";

        InvalidOperationException failure = Should.Throw<InvalidOperationException>(
            () => RenderTreeSequenceRewriter.AssignLiteralsOrFail(source));
        failure.Message.ShouldContain(invocation);
        Should.Throw<ShouldAssertException>(() => ShouldUseLiteralRenderTreeSequences(source));
    }

    [Fact]
    public void AssignLiteralsOrFail_AcceptsReorderedLiteralSequenceAndRewritesComponentParameterCounter() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
                    int seq = 0;
                    builder.AddComponentParameter(name: "Value", value: 42, sequence: seq++);
                    builder.AddContent(textContent: GetText(), sequence: 1);
                }
            }
            """;

        string rewritten = RenderTreeSequenceRewriter.AssignLiteralsOrFail(source);

        rewritten.ShouldContain("""builder.AddComponentParameter(name: "Value", value: 42, sequence: 0)""");
        rewritten.ShouldContain("builder.AddContent(textContent: GetText(), sequence: 1)");
        ShouldUseLiteralRenderTreeSequences(rewritten);
    }

    [Theory]
    [InlineData("!DEBUG", "Release (DEBUG undefined)")]
    [InlineData("DEBUG", "Debug (DEBUG defined)")]
    public void LiteralSequenceChecks_RejectConfigurationSpecificRuntimeExpression(string condition, string configuration) {
        string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
            #if CONDITION
                    builder.AddContent(GetSequence(), "runtime");
            #else
                    builder.AddContent(0, "literal");
            #endif
                }
            }
            """.Replace("CONDITION", condition, StringComparison.Ordinal);

        ShouldAssertException failure = Should.Throw<ShouldAssertException>(() => ShouldUseLiteralRenderTreeSequences(source));

        failure.Message.ShouldContain(configuration);
        failure.Message.ShouldContain("GetSequence()");
        Should.Throw<InvalidOperationException>(() => RenderTreeSequenceRewriter.AssignLiteralsOrFail(source))
            .Message.ShouldContain("GetSequence()");
    }

    [Fact]
    public void ShouldUseLiteralRenderTreeSequences_ChecksNet11ConsumerBranch() {
        const string source = """
            class C
            {
                void BuildRenderTree(RenderTreeBuilder builder)
                {
            #if NET11_0_OR_GREATER
                    builder.AddContent(GetSequence(), "net11");
            #else
                    builder.AddContent(0, "previous target");
            #endif
                }
            }
            """;

        Should.Throw<ShouldAssertException>(() => ShouldUseLiteralRenderTreeSequences(source))
            .Message.ShouldContain("GetSequence()");
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
    internal static void ShouldUseLiteralRenderTreeSequences(string generatedSource) {
        generatedSource.ShouldNotContain("ASP0006");

        CSharpParseOptions releaseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols("TRACE");
        ShouldParseAndUseLiterals(generatedSource, releaseOptions, "Release (DEBUG undefined)");
        ShouldParseAndUseLiterals(
            generatedSource,
            releaseOptions.WithPreprocessorSymbols("TRACE", "DEBUG"),
            "Debug (DEBUG defined)");
        if (generatedSource.Contains("NET11_0_OR_GREATER", StringComparison.Ordinal)) {
            ShouldParseAndUseLiterals(
                generatedSource,
                CSharpParseOptions.Default.WithPreprocessorSymbols("NET11_0_OR_GREATER", "TRACE"),
                ".NET 11 Release");
            ShouldParseAndUseLiterals(
                generatedSource,
                CSharpParseOptions.Default.WithPreprocessorSymbols("NET11_0_OR_GREATER", "TRACE", "DEBUG"),
                ".NET 11 Debug");
        }
    }

    private static void ShouldParseAndUseLiterals(string generatedSource, CSharpParseOptions options, string configuration) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(generatedSource, options);
        Diagnostic[] diagnostics = [.. tree.GetDiagnostics()];

        diagnostics.ShouldBeEmpty(
            $"Generated source does not parse cleanly under {configuration}: "
            + string.Join(
                " | ",
                diagnostics.Select(diagnostic =>
                    diagnostic.Id
                    + " @ "
                    + DescribeSite(generatedSource, diagnostic.Location.SourceSpan.Start)
                    + " - "
                    + diagnostic.GetMessage(CultureInfo.InvariantCulture))));

        string? survivingCall = RenderTreeSequenceRewriter.FindRuntimeSequenceArgument(tree.GetRoot());
        survivingCall.ShouldBeNull($"Generated source under {configuration} passes a runtime sequence argument: {survivingCall}");
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
}
