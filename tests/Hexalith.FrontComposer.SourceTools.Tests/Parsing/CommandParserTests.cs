using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public class CommandParserTests {
    [Fact]
    public void Parse_SingleStringFieldCommand_SeparatesMessageIdFromName() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.SingleStringFieldCommand, "TestDomain.SetNameCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.TypeName.ShouldBe("SetNameCommand");
        result.Model.Namespace.ShouldBe("TestDomain");
        result.Model.Properties.Count.ShouldBe(2);
        result.Model.DerivableProperties.Select(p => p.Name).ShouldBe(new[] { "MessageId" });
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldBe(new[] { "Name" });
    }

    [Fact]
    public void Parse_MultiFieldCommand_ClassifiesDerivableAndNonDerivable() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.MultiFieldCommand, "TestDomain.PlaceOrderCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.BoundedContext.ShouldBe("Orders");
        result.Model.DerivableProperties.Select(p => p.Name).ShouldBe(new[] { "MessageId", "TenantId" }, ignoreOrder: true);
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldBe(new[] { "CustomerName", "Quantity", "TotalAmount", "Expedited", "OrderedAt" }, ignoreOrder: true);
    }

    [Fact]
    public void Parse_RecordPositionalCommand_CapturesPositionalParams() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.RecordPositionalCommand, "TestDomain.IncrementCounterCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.Properties.Select(p => p.Name).ShouldContain("MessageId");
        result.Model.Properties.Select(p => p.Name).ShouldContain("Amount");
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldContain("Amount");
        result.Model.DerivableProperties.Select(p => p.Name).ShouldContain("MessageId");
    }

    [Fact]
    public void Parse_RecordPropertyCommand_CapturesInitProperties() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.RecordPropertyCommand, "TestDomain.DecrementCounterCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.Properties.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_EmptyCommand_ProducesModelWithSingleDerivableProperty() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.EmptyCommand, "TestDomain.EmptyCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.NonDerivableProperties.Count.ShouldBe(0);
        result.Model.DerivableProperties.Count.ShouldBe(1);
    }

    [Fact]
    public void Parse_MissingMessageId_EmitsHFC1006() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.MissingMessageIdCommand, "TestDomain.NoMessageIdCommand");

        result.Diagnostics.Select(d => d.Id).ShouldContain("HFC1006");
    }

    [Fact]
    public void Parse_DerivedFromAttribute_ClassifiesPropertyAsDerivable() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.DerivedFromAttributeCommand, "TestDomain.AnnotatedDerivedCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.DerivableProperties.Select(p => p.Name).ShouldContain("RequestIp");
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldContain("UserName");
    }

    [Theory]
    [InlineData("MessageId")]
    [InlineData("CommandId")]
    [InlineData("CorrelationId")]
    [InlineData("TenantId")]
    [InlineData("UserId")]
    [InlineData("Timestamp")]
    [InlineData("CreatedAt")]
    [InlineData("ModifiedAt")]
    public void Parse_WellKnownDerivableProperty_IsClassifiedAsDerivable(string propertyName) {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.WellKnownDerivableCommand, "TestDomain.KitchenSinkCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.DerivableProperties.Select(p => p.Name).ShouldContain(propertyName);
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldNotContain(propertyName);
    }

    [Fact]
    public void Parse_WellKnownDerivableCommand_PayloadIsNotDerivable() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.WellKnownDerivableCommand, "TestDomain.KitchenSinkCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.NonDerivableProperties.Select(p => p.Name).ShouldContain("Payload");
    }

    [Fact]
    public void Parse_BaseRecordWithMessageId_InheritsFromBase() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.BaseRecordWithMessageId, "TestDomain.ChildCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.Properties.Select(p => p.Name).ShouldContain("MessageId");
        result.Diagnostics.Select(d => d.Id).ShouldNotContain("HFC1006");
    }

    [Fact]
    public void Parse_BaseClassWithMessageId_InheritsFromBase() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.BaseClassWithMessageId, "TestDomain.ChildClassCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.Properties.Select(p => p.Name).ShouldContain("MessageId");
        result.Diagnostics.Select(d => d.Id).ShouldNotContain("HFC1006");
    }

    [Fact]
    public void Parse_DisplayAttribute_CapturesDisplayName() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.DisplayAttributeCommand, "TestDomain.DisplayLabeledCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Model.DisplayName.ShouldBe("Place Order");
        PropertyModel customerName = result.Model.Properties.Single(p => p.Name == "CustomerName");
        customerName.DisplayName.ShouldBe("Customer Name");
    }

    [Fact]
    public void Parse_UnsupportedFieldType_EmitsHFC1002() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.UnsupportedFieldCommand, "TestDomain.UnsupportedCommand");

        _ = result.Model.ShouldNotBeNull();
        result.Diagnostics.Select(d => d.Id).ShouldContain("HFC1002");
        PropertyModel raw = result.Model.Properties.Single(p => p.Name == "Raw");
        raw.IsUnsupported.ShouldBeTrue();
    }

    [Fact]
    public void Parse_TooManyProperties_WarningThreshold_EmitsHFC1007Warning() {
        string source = CommandTestSources.TooManyPropertiesCommand(CommandParser.NonDerivableWarningThreshold + 1);
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.TooManyPropertiesCommand");

        DiagnosticInfo? hfc1007 = result.Diagnostics.FirstOrDefault(d => d.Id == "HFC1007");
        _ = hfc1007.ShouldNotBeNull();
        hfc1007.Severity.ShouldBe("Warning");
    }

    [Fact]
    public void Parse_TooManyProperties_ErrorThreshold_EmitsHFC1007Error() {
        string source = CommandTestSources.TooManyPropertiesCommand(CommandParser.NonDerivableErrorThreshold + 1);
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.TooManyPropertiesCommand");

        DiagnosticInfo? hfc1007 = result.Diagnostics.FirstOrDefault(d => d.Id == "HFC1007");
        _ = hfc1007.ShouldNotBeNull();
        hfc1007.Severity.ShouldBe("Error");
    }

    [Fact]
    public void Parse_StructCommand_EmitsHFC1004() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.StructCommand, "TestDomain.StructCommand");

        result.Diagnostics.Select(d => d.Id).ShouldContain("HFC1004");
    }

    [Fact]
    public void Parse_SystemNamespaceCommand_EmitsHFC1004() {
        CommandParseResult result = CompilationHelper.ParseCommand(CommandTestSources.SystemNamespaceCommand, "System.FakeNamespace.SystemNamespaceCommand");

        result.Diagnostics.Select(d => d.Id).ShouldContain("HFC1004");
    }

    [Fact]
    public void CommandModel_IEquatable_DifferentTypeName_NotEqual() {
        CommandModel left = BuildModel("A");
        CommandModel right = BuildModel("B");

        left.Equals(right).ShouldBeFalse();
        (left.GetHashCode() == right.GetHashCode()).ShouldBeFalse();
    }

    [Fact]
    public void CommandModel_IEquatable_SameValues_Equal() {
        CommandModel left = BuildModel("A");
        CommandModel right = BuildModel("A");

        left.Equals(right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CommandModel_IEquatable_IsSymmetric() {
        CommandModel left = BuildModel("A");
        CommandModel right = BuildModel("A");

        left.Equals(right).ShouldBe(right.Equals(left));
    }

    private static CommandModel BuildModel(string typeName) {
        EquatableArray<BadgeMappingEntry> noBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);
        PropertyModel property = new("MessageId", "String", false, false, null, noBadges);
        EquatableArray<PropertyModel> all = new(ImmutableArray.Create(property));
        EquatableArray<PropertyModel> derivable = new(ImmutableArray.Create(property));
        EquatableArray<PropertyModel> nonDerivable = new(ImmutableArray<PropertyModel>.Empty);

        return new CommandModel(
            typeName,
            "TestDomain",
            "TestContext",
            null,
            null,
            all,
            derivable,
            nonDerivable);
    }
}
