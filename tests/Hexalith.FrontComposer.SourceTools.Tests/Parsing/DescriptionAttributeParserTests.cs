using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public sealed class DescriptionAttributeParserTests {
    [Fact]
    public void Parse_DescriptionAttribute_OnProperty_FlowsToPropertyModelDescription() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                [Description("Customer-facing order name")]
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBe("Customer-facing order name");
    }

    [Fact]
    public void Parse_DisplayDescription_OnProperty_FlowsToPropertyModelDescription() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                [Display(Description = "Used by fulfillment operators")]
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBe("Used by fulfillment operators");
    }

    [Fact]
    public void Parse_DescriptionAttribute_WinsOverDisplayDescription() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel;
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                [Display(Description = "Display description")]
                [Description("ComponentModel description")]
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBe("ComponentModel description");
    }

    [Fact]
    public void Parse_WhitespaceDescription_IsTreatedAsAbsent() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel;
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                [Description("   ")]
                [Display(Description = "   ")]
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBeNull();
    }

    [Fact]
    public void Parse_AbsentDescription_IsNull() {
        ParseResult result = ParseDescriptionProjection("""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBeNull();
    }

    [Fact]
    public void Parse_TypeDescription_IsIgnoredForPropertyDescriptions() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            [Description("Projection-level description")]
            public partial class DescriptionProjection
            {
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.Description.ShouldBeNull();
    }

    [Fact]
    public void Parse_DisplayNameAndDescription_ParseIndependently() {
        ParseResult result = ParseDescriptionProjection("""
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            public partial class DescriptionProjection
            {
                [Display(Name = "Order name", Description = "Visible on invoices")]
                public string Name { get; set; } = "";
            }
            """);

        PropertyModel property = GetProperty(result, "Name");
        property.DisplayName.ShouldBe("Order name");
        property.Description.ShouldBe("Visible on invoices");
    }

    [Fact]
    public void Parse_ProjectionEmptyStateCtaAttribute_FlowsToDomainModel() {
        ParseResult result = ParseDescriptionProjection("""
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders")]
            [Projection]
            [ProjectionEmptyStateCta("CreateOrderCommand")]
            public partial class DescriptionProjection
            {
                public string Name { get; set; } = "";
            }
            """);

        result.Model.ShouldNotBeNull().EmptyStateCtaCommandTypeName.ShouldBe("CreateOrderCommand");
    }

    private static ParseResult ParseDescriptionProjection(string source) {
        ParseResult result = CompilationHelper.ParseProjection(source, "TestDomain.DescriptionProjection");
        result.Diagnostics.AsImmutableArray().ShouldBeEmpty();
        return result;
    }

    private static PropertyModel GetProperty(ParseResult result, string name)
        => result.Model.ShouldNotBeNull().Properties.AsImmutableArray().Single(p => p.Name == name);
}
