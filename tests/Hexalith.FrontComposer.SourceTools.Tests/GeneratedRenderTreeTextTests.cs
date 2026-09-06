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
    public void MaskSequenceArguments_MasksEverySupportedLiteralCall(string source, string expected)
        => GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(expected);

    [Theory]
    [InlineData("builder.AddContent(seq++, \"value\");")]
    [InlineData("builder.AddContent(seq ++, \"value\");")]
    [InlineData("builder.AddContent(++seq, \"value\");")]
    [InlineData("builder.AddContent(-- seq, \"value\");")]
    [InlineData("builder.AddContent(/* sequence */ ++seq, \"value\");")]
    public void MaskSequenceArguments_DoesNotMaskRuntimeCounters(string source)
        => GeneratedRenderTreeText.MaskSequenceArguments(source).ShouldBe(source);
}
