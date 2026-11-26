using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

/// <summary>
/// Story 2-5 Task 8.5 — parser-side destructive classification + HFC1020/HFC1021 analyzer coverage
/// (AC4 / AC8 / D1 / D20 / ADR-026).
/// </summary>
public class CommandParserDestructiveTests {
    private const string DestructiveWithField = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[Destructive]
public class DeleteSomethingCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}";

    private const string DestructiveWithZeroFields = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[Destructive]
public class DeleteEverythingCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    private const string DeleteNamedWithoutAttribute = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class DeleteOrderCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}";

    private const string NonDestructiveBenignName = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class ResetCounterCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string CounterId { get; set; } = string.Empty;
}";

    private const string DestructiveWithConfirmationArgs = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[Destructive(ConfirmationTitle = ""Really delete this order?"", ConfirmationBody = ""The order and all its lines go away."")]
public class DropOrderCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}";

    private const string ExpandedNameWipeCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class WipeCacheCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}";

    [Fact]
    public void Parse_DestructiveAttribute_SetsIsDestructive() {
        CommandParseResult result = CompilationHelper.ParseCommand(DestructiveWithField, "TestDomain.DeleteSomethingCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.IsDestructive.ShouldBeTrue();
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1021");
    }

    [Fact]
    public void Parse_DestructiveAttributeWithArgs_CapturesTitleAndBody() {
        CommandParseResult result = CompilationHelper.ParseCommand(DestructiveWithConfirmationArgs, "TestDomain.DropOrderCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.IsDestructive.ShouldBeTrue();
        result.Model.DestructiveConfirmTitle.ShouldBe("Really delete this order?");
        result.Model.DestructiveConfirmBody.ShouldBe("The order and all its lines go away.");
    }

    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void Parse_DeleteNamed_WithoutAttribute_EmitsHFC1020Info() {
        CommandParseResult result = CompilationHelper.ParseCommand(DeleteNamedWithoutAttribute, "TestDomain.DeleteOrderCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.IsDestructive.ShouldBeFalse();
        DiagnosticInfo? hfc1020 = result.Diagnostics.FirstOrDefault(d => d.Id == "HFC1020");
        _ = hfc1020.ShouldNotBeNull();
        hfc1020.Severity.ShouldBe("Info");
        hfc1020.Message.ShouldContain("DeleteOrderCommand");
        hfc1020.Message.ShouldContain("[Destructive]");
    }

    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void Parse_DestructiveWithZeroFields_EmitsHFC1021Error() {
        CommandParseResult result = CompilationHelper.ParseCommand(DestructiveWithZeroFields, "TestDomain.DeleteEverythingCommand");

        // Parse halts on HFC1021 per story D1/AC4 contract.
        result.Model.ShouldBeNull();
        DiagnosticInfo? hfc1021 = result.Diagnostics.FirstOrDefault(d => d.Id == "HFC1021");
        _ = hfc1021.ShouldNotBeNull();
        hfc1021.Severity.ShouldBe("Error");
        hfc1021.Message.ShouldContain("destructive");
    }

    [Fact]
    public void Parse_BenignNonDestructiveName_DoesNotEmitHFC1020() {
        CommandParseResult result = CompilationHelper.ParseCommand(NonDestructiveBenignName, "TestDomain.ResetCounterCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1020");
    }

    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void Parse_ExpandedDestructivePatternWipe_EmitsHFC1020() {
        // Story 2-5 Red Team Attack-2: the heuristic covers Wipe/Erase/Drop/Truncate.
        CommandParseResult result = CompilationHelper.ParseCommand(ExpandedNameWipeCommand, "TestDomain.WipeCacheCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldContain(d => d.Id == "HFC1020");
    }

    [Theory]
    [InlineData("Delete2Items")]
    [InlineData("Remove0Entries")]
    [InlineData("Wipe9")]
    public void ParseWithDestructiveVerbAndDigitEmitsHfc1020(string name) {
        const string originalName = "DeleteOrderCommand";
        CommandParseResult result = CompilationHelper.ParseCommand(
            DeleteNamedWithoutAttribute.Replace(originalName, name, StringComparison.Ordinal),
            "TestDomain." + name);

        CommandModel model = result.Model.ShouldNotBeNull();
        model.IsDestructive.ShouldBeFalse();
        result.Diagnostics.Single(d => d.Id == "HFC1020").Severity.ShouldBe("Info");
    }

    [Fact]
    public void ParseWithDestructiveAttributeAndDigitOmitsHfc1020() {
        const string name = "Delete2Items";
        CommandParseResult result = CompilationHelper.ParseCommand(
            DestructiveWithField.Replace("DeleteSomethingCommand", name, StringComparison.Ordinal),
            "TestDomain." + name);

        CommandModel model = result.Model.ShouldNotBeNull();
        model.IsDestructive.ShouldBeTrue();
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1020");
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("DeleteableCommand")]
    [InlineData("deleteOrderCommand")]
    [InlineData("CanDeleteOrderCommand")]
    [InlineData("Delete_OrderCommand")]
    public void ParseWithoutDestructiveWordBoundaryOmitsHfc1020(string name) {
        const string originalName = "DeleteOrderCommand";
        CommandParseResult result = CompilationHelper.ParseCommand(
            DeleteNamedWithoutAttribute.Replace(originalName, name, StringComparison.Ordinal),
            "TestDomain." + name);

        _ = result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1020");
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Remove")]
    [InlineData("Purge")]
    [InlineData("Wipe")]
    [InlineData("Erase")]
    [InlineData("Drop")]
    [InlineData("Truncate")]
    [Trait("Category", "MutationErrorRegression")]
    public void Parse_CurrentDestructiveVerbPattern_EmitsHFC1020Info(string verb) {
        string source = $$"""
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class {{verb}}WidgetCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string WidgetId { get; set; } = string.Empty;
}
""";

        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain." + verb + "WidgetCommand");

        _ = result.Model.ShouldNotBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(d => d.Id == "HFC1020");
        diagnostic.Severity.ShouldBe("Info");
    }
}
