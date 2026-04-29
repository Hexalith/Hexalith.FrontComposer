using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public sealed class Level1FormatAnnotationParserTests {
    [Theory]
    [InlineData("System.DateTime")]
    [InlineData("System.DateTimeOffset")]
    [InlineData("System.DateTime?")]
    [InlineData("System.DateTimeOffset?")]
    public void Parse_RelativeTime_OnDateTimeLikeProperty_FlowsToPropertyModel(string typeName) {
        ParseResult result = ParseFormatProjection($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                [RelativeTime]
                public {{typeName}} LastChanged { get; set; }
            }
            """);

        PropertyModel property = GetProperty(result, "LastChanged");
        property.DisplayFormat.ShouldBe(FieldDisplayFormat.RelativeTime);
        property.RelativeTimeWindowDays.ShouldBe(7);
    }

    [Theory]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("float")]
    [InlineData("decimal?")]
    public void Parse_Currency_OnSupportedNumericProperty_FlowsToPropertyModel(string typeName) {
        ParseResult result = ParseFormatProjection($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                [Currency]
                public {{typeName}} Amount { get; set; }
            }
            """);

        GetProperty(result, "Amount").DisplayFormat.ShouldBe(FieldDisplayFormat.Currency);
    }

    [Fact]
    public void Parse_ConflictingFormatAnnotations_EmitOneWarningAndFallbackToDefault() {
        ParseResult result = CompilationHelper.ParseProjection("""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                [RelativeTime]
                [Currency]
                public decimal Amount { get; set; }
            }
            """, "TestDomain.FormatProjection");

        result.Diagnostics.Count.ShouldBe(1);
        result.Diagnostics[0].Id.ShouldBe("HFC1032");
        result.Diagnostics[0].Message.ShouldContain("What:");
        result.Diagnostics[0].Message.ShouldContain("Expected:");
        result.Diagnostics[0].Message.ShouldContain("Got:");
        result.Diagnostics[0].Message.ShouldContain("Fix:");
        result.Diagnostics[0].Message.ShouldContain("DocsLink:");
        result.Diagnostics[0].Message.ShouldContain("Fallback:");
        GetProperty(result, "Amount").DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
    }

    [Theory]
    [InlineData("[RelativeTime]", "string", "DateTime or DateTimeOffset")]
    [InlineData("[Currency]", "System.DateTime", "decimal, double, or float")]
    public void Parse_InvalidFormatAnnotation_EmitsWarningAndFallbackToDefault(
        string attribute,
        string typeName,
        string expectedFamily) {
        ParseResult result = CompilationHelper.ParseProjection($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                {{attribute}}
                public {{typeName}} Value { get; set; } = default!;
            }
            """, "TestDomain.FormatProjection");

        result.Diagnostics.Count.ShouldBe(1);
        result.Diagnostics[0].Id.ShouldBe("HFC1032");
        result.Diagnostics[0].Message.ShouldContain(expectedFamily);
        result.Diagnostics[0].Message.ShouldContain("Fallback:");
        GetProperty(result, "Value").DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
    }

    private static ParseResult ParseFormatProjection(string source) {
        ParseResult result = CompilationHelper.ParseProjection(source, "TestDomain.FormatProjection");
        result.Diagnostics.AsImmutableArray().ShouldBeEmpty();
        return result;
    }

    private static PropertyModel GetProperty(ParseResult result, string name)
        => result.Model.ShouldNotBeNull().Properties.AsImmutableArray().Single(p => p.Name == name);
}
