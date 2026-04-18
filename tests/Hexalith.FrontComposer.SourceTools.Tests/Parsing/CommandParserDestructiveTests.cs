using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

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
    public void Parse_ExpandedDestructivePatternWipe_EmitsHFC1020() {
        // Story 2-5 Red Team Attack-2: expanded regex covers Wipe/Erase/Drop/Truncate.
        CommandParseResult result = CompilationHelper.ParseCommand(ExpandedNameWipeCommand, "TestDomain.WipeCacheCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldContain(d => d.Id == "HFC1020");
    }
}
