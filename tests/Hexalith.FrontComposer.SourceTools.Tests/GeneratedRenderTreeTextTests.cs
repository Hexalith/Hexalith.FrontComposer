using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests;

/// <summary>
/// Direct coverage for masking generated render-tree sequence literals without hiding runtime
/// counters.
/// </summary>
public sealed class GeneratedRenderTreeTextTests {
    [Theory]
    [InlineData("builder.OpenElement(12, \"div\");", "builder.OpenElement(#, \"div\");")]
    [InlineData("builder.OpenComponent<Component>(12);", "builder.OpenComponent<Component>(#);")]
    [InlineData("builder.AddAttribute(12, \"class\", \"value\");", "builder.AddAttribute(#, \"class\", \"value\");")]
    [InlineData("builder.AddContent(12, \"value\");", "builder.AddContent(#, \"value\");")]
    [InlineData("builder.AddMarkupContent(12, \"<span />\");", "builder.AddMarkupContent(#, \"<span />\");")]
    [InlineData("builder.OpenRegion(12);", "builder.OpenRegion(#);")]
    [InlineData("builder.AddMultipleAttributes(12, attributes);", "builder.AddMultipleAttributes(#, attributes);")]
    [InlineData("builder.AddElementReferenceCapture(12, capture);", "builder.AddElementReferenceCapture(#, capture);")]
    [InlineData("builder.AddComponentReferenceCapture(12, capture);", "builder.AddComponentReferenceCapture(#, capture);")]
    [InlineData("builder.AddComponentParameter(12, \"Value\", value);", "builder.AddComponentParameter(#, \"Value\", value);")]
    [InlineData("builder.AddContent(textContent: \"value\", sequence: /* seq */ 12);", "builder.AddContent(textContent: \"value\", sequence: /* seq */ #);")]
    public void MaskSequenceArguments_MasksEverySupportedLiteralCall(string source, string expected)
        => GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(expected);

    [Theory]
    [InlineData("builder.AddContent(seq++, \"value\");")]
    [InlineData("builder.AddContent(seq ++, \"value\");")]
    [InlineData("builder.AddContent(++seq, \"value\");")]
    [InlineData("builder.AddContent(-- seq, \"value\");")]
    [InlineData("builder.AddContent(/* sequence */ ++seq, \"value\");")]
    [InlineData("builder.AddContent(12 + seq, \"value\");")]
    [InlineData("builder.AddContent(12 /* inside */ + seq, \"value\");")]
    [InlineData("builder.AddContent(textContent: \"value\", sequence: 12 + seq);")]
    [InlineData("// builder.AddContent(12, value);")]
    public void MaskSequenceArguments_LeavesNonliteralArgumentsAndCommentsUntouched(string source)
        => GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(source);

    [Fact]
    public void MaskSequenceArguments_MasksLiteralCallsWithoutHidingInterveningRuntimeCounter() {
        const string source = """
            int unrelatedValue = 42;
            builder.OpenElement(0, "div");
            builder.AddAttribute(12, "class", "value");
            builder.AddContent(seq++, "runtime");
            builder.AddMarkupContent(345, "<span />");
            builder.AddContent(678, unrelatedValue);
            builder.CloseElement();
            """;
        const string expected = """
            int unrelatedValue = 42;
            builder.OpenElement(#, "div");
            builder.AddAttribute(#, "class", "value");
            builder.AddContent(seq++, "runtime");
            builder.AddMarkupContent(#, "<span />");
            builder.AddContent(#, unrelatedValue);
            builder.CloseElement();
            """;

        GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(expected);
    }

    [Fact]
    public void MaskSequenceArguments_HandlesDebugAndReleaseWithoutHidingConditionalArithmetic() {
        const string source = """
            #if DEBUG
            builder.AddContent(12, "debug");
            #else
            builder.AddContent(34, "release");
            #endif
            builder.AddContent(56
            #if DEBUG
                + seq
            #endif
                , "conditional expression");
            """;
        const string expected = """
            #if DEBUG
            builder.AddContent(#, "debug");
            #else
            builder.AddContent(#, "release");
            #endif
            builder.AddContent(56
            #if DEBUG
                + seq
            #endif
                , "conditional expression");
            """;

        GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(expected);
    }

}
