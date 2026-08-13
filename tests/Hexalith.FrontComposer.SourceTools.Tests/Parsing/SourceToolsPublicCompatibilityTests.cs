using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public sealed class SourceToolsPublicCompatibilityTests {
    [Fact]
    public void CommandModels_PreStory94ConstructorSignaturesRemainAvailable() {
        Type propertyArray = typeof(EquatableArray<PropertyModel>);
        Type fieldArray = typeof(EquatableArray<FormFieldModel>);

        typeof(CommandModel).GetConstructor([
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string),
            propertyArray,
            propertyArray,
            propertyArray,
            typeof(string),
            typeof(bool),
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(int),
            typeof(int),
        ]).ShouldNotBeNull();

        typeof(CommandFormModel).GetConstructor([
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(string),
            fieldArray,
            typeof(string),
        ]).ShouldNotBeNull();
    }
}
