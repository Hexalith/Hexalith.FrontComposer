using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public class CommandFormTransformTests {
    [Theory]
    [InlineData("String", FormFieldTypeCategory.TextInput)]
    [InlineData("Int32", FormFieldTypeCategory.NumberInput)]
    [InlineData("Int64", FormFieldTypeCategory.NumberInput)]
    [InlineData("Decimal", FormFieldTypeCategory.DecimalInput)]
    [InlineData("Double", FormFieldTypeCategory.DecimalInput)]
    [InlineData("Single", FormFieldTypeCategory.DecimalInput)]
    [InlineData("Boolean", FormFieldTypeCategory.Switch)]
    [InlineData("DateTime", FormFieldTypeCategory.DatePicker)]
    [InlineData("DateTimeOffset", FormFieldTypeCategory.DatePicker)]
    [InlineData("DateOnly", FormFieldTypeCategory.DatePicker)]
    [InlineData("TimeOnly", FormFieldTypeCategory.TextInput)]
    [InlineData("Enum", FormFieldTypeCategory.Select)]
    [InlineData("Guid", FormFieldTypeCategory.MonospaceText)]
    public void Transform_MapsTypeCategory(string typeName, FormFieldTypeCategory expected) {
        PropertyModel property = BuildProperty(name: "Value", typeName: typeName);
        CommandModel command = BuildCommand(nonDerivable: [property]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Single().TypeCategory.ShouldBe(expected);
    }

    [Fact]
    public void Transform_UnsupportedField_MapsToPlaceholder() {
        PropertyModel property = BuildProperty(name: "Unsupported", typeName: "System.Object", isUnsupported: true);
        CommandModel command = BuildCommand(nonDerivable: [property]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Single().TypeCategory.ShouldBe(FormFieldTypeCategory.Placeholder);
    }

    [Fact]
    public void Transform_UsesDisplayNameWhenSet() {
        PropertyModel property = BuildProperty(name: "CustomerName", typeName: "String", displayName: "Customer Name");
        CommandModel command = BuildCommand(nonDerivable: [property]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Single().StaticLabel.ShouldBe("Customer Name");
    }

    [Fact]
    public void Transform_HumanizesCamelCaseWhenDisplayAbsent() {
        PropertyModel property = BuildProperty(name: "CustomerName", typeName: "String");
        CommandModel command = BuildCommand(nonDerivable: [property]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Single().StaticLabel.ShouldBe("Customer Name");
    }

    [Fact]
    public void Transform_ButtonLabel_SimpleCommandName() {
        CommandModel command = BuildCommand(typeName: "Foo");

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.ButtonLabel.ShouldBe("Send Foo");
    }

    [Fact]
    public void Transform_ButtonLabel_MultiWordCommandName() {
        CommandModel command = BuildCommand(typeName: "IncrementCounterCommand");

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.ButtonLabel.ShouldBe("Send Increment Counter Command");
    }

    [Fact]
    public void Transform_ButtonLabel_UsesDisplayNameIfPresent() {
        CommandModel command = BuildCommand(typeName: "SomeCommand", displayName: "Place Order");

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.ButtonLabel.ShouldBe("Send Place Order");
    }

    [Fact]
    public void Transform_SkipsDerivableProperties() {
        PropertyModel messageId = BuildProperty(name: "MessageId", typeName: "String");
        PropertyModel amount = BuildProperty(name: "Amount", typeName: "Int32");
        CommandModel command = BuildCommand(nonDerivable: [amount], derivable: [messageId]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Select(f => f.PropertyName).ShouldBe(new[] { "Amount" });
    }

    [Fact]
    public void Transform_ZeroNonDerivableFields_ProducesEmptyFieldArray() {
        PropertyModel messageId = BuildProperty(name: "MessageId", typeName: "String");
        CommandModel command = BuildCommand(derivable: [messageId]);

        CommandFormModel result = CommandFormTransform.Transform(command);

        result.Fields.Count.ShouldBe(0);
    }

    [Fact]
    public void HumanizeAndTruncateEnumMember_TruncatesLongNames() {
        string input = "ThisIsAnExtremelyLongEnumMemberName";

        string result = CommandFormTransform.HumanizeAndTruncateEnumMember(input);

        result.Length.ShouldBeLessThanOrEqualTo(CommandFormTransform.EnumOptionLabelMaxLength);
        result.ShouldEndWith("\u2026");
    }

    [Fact]
    public void HumanizeAndTruncateEnumMember_ShortName_NotTruncated() {
        string result = CommandFormTransform.HumanizeAndTruncateEnumMember("InProgress");
        result.ShouldBe("In Progress");
    }

    [Fact]
    public void FormFieldModel_IEquatable_SameValues_Equal() {
        FormFieldModel a = new("A", "String", FormFieldTypeCategory.TextInput, "A", false, true, null);
        FormFieldModel b = new("A", "String", FormFieldTypeCategory.TextInput, "A", false, true, null);

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void FormFieldModel_IEquatable_DifferentCategory_NotEqual() {
        FormFieldModel a = new("A", "String", FormFieldTypeCategory.TextInput, "A", false, true, null);
        FormFieldModel b = new("A", "String", FormFieldTypeCategory.NumberInput, "A", false, true, null);

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void CommandFormModel_IEquatable_SameFields_Equal() {
        CommandFormModel a = BuildFormModel();
        CommandFormModel b = BuildFormModel();

        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void CommandFormModel_IEquatable_DifferentButtonLabel_NotEqual() {
        CommandFormModel a = BuildFormModel();
        CommandFormModel b = new(a.TypeName, a.Namespace, a.BoundedContext, a.CommandFullyQualifiedName, "Different", a.Fields);

        a.Equals(b).ShouldBeFalse();
    }

    private static CommandFormModel BuildFormModel() {
        FormFieldModel field = new("Amount", "Int32", FormFieldTypeCategory.NumberInput, "Amount", false, true, null);
        EquatableArray<FormFieldModel> fields = new(ImmutableArray.Create(field));
        return new CommandFormModel("FooCommand", "TestDomain", "Test", "TestDomain.FooCommand", "Send Foo Command", fields);
    }

    private static PropertyModel BuildProperty(
        string name,
        string typeName,
        bool isNullable = false,
        bool isUnsupported = false,
        string? displayName = null,
        string? enumFqn = null) {
        return new PropertyModel(
            name,
            typeName,
            isNullable,
            isUnsupported,
            displayName,
            new EquatableArray<BadgeMappingEntry>(ImmutableArray<BadgeMappingEntry>.Empty),
            enumFqn);
    }

    private static CommandModel BuildCommand(
        string typeName = "TestCommand",
        string @namespace = "TestDomain",
        string? boundedContext = null,
        string? displayName = null,
        IReadOnlyList<PropertyModel>? nonDerivable = null,
        IReadOnlyList<PropertyModel>? derivable = null) {
        IReadOnlyList<PropertyModel> all = ((derivable ?? Array.Empty<PropertyModel>()).Concat(nonDerivable ?? Array.Empty<PropertyModel>())).ToList();

        return new CommandModel(
            typeName,
            @namespace,
            boundedContext,
            null,
            displayName,
            new EquatableArray<PropertyModel>(all.ToImmutableArray()),
            new EquatableArray<PropertyModel>((derivable ?? Array.Empty<PropertyModel>()).ToImmutableArray()),
            new EquatableArray<PropertyModel>((nonDerivable ?? Array.Empty<PropertyModel>()).ToImmutableArray()));
    }
}
