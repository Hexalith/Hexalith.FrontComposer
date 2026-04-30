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

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F1 — out-of-range / non-int [RelativeTime(N)] argument
    //   must emit HFC1032 at compile time; the runtime constructor guard does
    //   not run on AttributeData. Fail-soft fallback to FieldDisplayFormat.Default.
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MaxValue)]
    public void Parse_RelativeTimeWithOutOfRangeWindow_EmitsHfc1032AndFallsBackToDefault(int days) {
        ParseResult result = CompilationHelper.ParseProjection($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                [RelativeTime({{days}})]
                public System.DateTime LastChanged { get; set; }
            }
            """, "TestDomain.FormatProjection");

        result.Diagnostics.Count.ShouldBe(1);
        result.Diagnostics[0].Id.ShouldBe("HFC1032");
        result.Diagnostics[0].Message.ShouldContain("[RelativeTime(N)]");
        result.Diagnostics[0].Message.ShouldContain("between 1 and 365");
        result.Diagnostics[0].Message.ShouldContain("Fallback:");
        GetProperty(result, "LastChanged").DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
        GetProperty(result, "LastChanged").RelativeTimeWindowDays.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    public void Parse_RelativeTimeWithBoundedWindow_PropagatesToPropertyModel(int days) {
        ParseResult result = ParseFormatProjection($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class FormatProjection
            {
                [RelativeTime({{days}})]
                public System.DateTime LastChanged { get; set; }
            }
            """);

        GetProperty(result, "LastChanged").RelativeTimeWindowDays.ShouldBe(days);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F21 / T9 / AC9 — dev-loop evidence. Two compilations
    //   with annotation differences must produce different PropertyModels so
    //   the Roslyn incremental-generator cache invalidates and downstream
    //   Transform/Emit re-run. `dotnet watch` rides on this contract.
    // ──────────────────────────────────────────────────────────────────────────

    private const string DevLoopSourceHeader = """
        using Hexalith.FrontComposer.Contracts.Attributes;

        namespace TestDomain;

        [BoundedContext("Orders")]
        [Projection]
        public partial class DevLoopProjection
        {
        """;
    private const string DevLoopSourceFooter = """
            public System.DateTime LastChanged { get; set; }
        }
        """;

    [Fact]
    public void Parse_AddingRelativeTimeAnnotation_ChangesPropertyModel() {
        PropertyModel without = GetProperty(ParseDevLoop(string.Empty), "LastChanged");
        PropertyModel withAnn = GetProperty(ParseDevLoop("[RelativeTime]"), "LastChanged");

        without.DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
        withAnn.DisplayFormat.ShouldBe(FieldDisplayFormat.RelativeTime);
        without.Equals(withAnn).ShouldBeFalse();
    }

    [Fact]
    public void Parse_RemovingRelativeTimeAnnotation_RevertsPropertyModel() {
        PropertyModel withAnn = GetProperty(ParseDevLoop("[RelativeTime]"), "LastChanged");
        PropertyModel without = GetProperty(ParseDevLoop(string.Empty), "LastChanged");

        without.DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
        without.Equals(withAnn).ShouldBeFalse();
    }

    [Fact]
    public void Parse_ChangingRelativeTimeWindowArgument_InvalidatesPropertyModel() {
        PropertyModel sevenDays = GetProperty(ParseDevLoop("[RelativeTime(7)]"), "LastChanged");
        PropertyModel fourteenDays = GetProperty(ParseDevLoop("[RelativeTime(14)]"), "LastChanged");

        sevenDays.RelativeTimeWindowDays.ShouldBe(7);
        fourteenDays.RelativeTimeWindowDays.ShouldBe(14);
        sevenDays.Equals(fourteenDays).ShouldBeFalse();
    }

    [Fact]
    public void Parse_IdenticalSourceCompiledTwice_ProducesEqualPropertyModels() {
        // Cache hit — Roslyn incremental generator pipeline must see the same key.
        PropertyModel first = GetProperty(ParseDevLoop("[RelativeTime(14)]"), "LastChanged");
        PropertyModel second = GetProperty(ParseDevLoop("[RelativeTime(14)]"), "LastChanged");

        first.Equals(second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    private static ParseResult ParseDevLoop(string annotationLine)
        => CompilationHelper.ParseProjection(
            DevLoopSourceHeader + "    " + annotationLine + Environment.NewLine + DevLoopSourceFooter,
            "TestDomain.DevLoopProjection");

    private static ParseResult ParseFormatProjection(string source) {
        ParseResult result = CompilationHelper.ParseProjection(source, "TestDomain.FormatProjection");
        result.Diagnostics.AsImmutableArray().ShouldBeEmpty();
        return result;
    }

    private static PropertyModel GetProperty(ParseResult result, string name)
        => result.Model.ShouldNotBeNull().Properties.AsImmutableArray().Single(p => p.Name == name);
}
