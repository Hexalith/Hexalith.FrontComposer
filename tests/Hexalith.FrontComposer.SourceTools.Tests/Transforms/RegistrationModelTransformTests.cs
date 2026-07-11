using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public class RegistrationModelTransformTests {

    [Fact]
    public void Transform_BoundedContextExtracted() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "MyApp.Orders", "Orders"));
        result.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public void Transform_DisplayLabel_IsNullWhenNotProvided() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "MyApp.Orders", "Orders"));
        result.DisplayLabel.ShouldBeNull();
    }

    [Fact]
    public void Transform_DisplayLabel_PropagatedFromDomainModel() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "MyApp.Orders", "Orders", "Commandes"));
        result.DisplayLabel.ShouldBe("Commandes");
    }

    [Fact]
    public void Transform_NoBoundedContext_EmptyNamespace_FallsBackToGlobal() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "", null));
        result.BoundedContext.ShouldBe("Global");
    }

    [Fact]
    public void Transform_NoBoundedContext_FallsBackToNamespaceLastSegment() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "MyApp.Orders", null));
        result.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public void Transform_NoBoundedContext_SimpleNamespace() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "Orders", null));
        result.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public void Transform_PreservesTypeName() {
        RegistrationModel result = RegistrationModelTransform.Transform(Model("OrderProjection", "MyApp.Orders", "Orders"));
        result.TypeName.ShouldBe("OrderProjection");
    }

    [Fact]
    public void TransformCommand_InlineCommand_UsesDefaultContextAndKnownEmptyRouteMembership() {
        CommandModel command = CompilationHelper.ParseCommand(
            CommandTestSources.SingleStringFieldCommand,
            "TestDomain.SetNameCommand").Model!;

        RegistrationModel result = RegistrationModelTransform.TransformCommand(command);

        result.BoundedContext.ShouldBe("Default");
        result.IsCommand.ShouldBeTrue();
        result.HasFullPageRoute.ShouldBeFalse();
    }

    [Fact]
    public void TransformCommand_FullPageCommand_CarriesRouteMembership() {
        CommandModel command = CompilationHelper.ParseCommand(
            CommandTestSources.MultiFieldCommand,
            "TestDomain.PlaceOrderCommand").Model!;

        RegistrationModel result = RegistrationModelTransform.TransformCommand(command);

        result.BoundedContext.ShouldBe("Orders");
        result.HasFullPageRoute.ShouldBeTrue();
    }

    [Fact]
    public void Equality_FullPageMembershipDiffers_ModelsAreNotEqual() {
        var inline = new RegistrationModel("Orders", "SubmitOrderCommand", "TestDomain", null, isCommand: true);
        var fullPage = new RegistrationModel("Orders", "SubmitOrderCommand", "TestDomain", null, isCommand: true, hasFullPageRoute: true);

        inline.ShouldNotBe(fullPage);
        inline.GetHashCode().ShouldNotBe(fullPage.GetHashCode());
    }

    private static DomainModel Model(string typeName, string ns, string? boundedContext, string? displayLabel = null)
                                    => new(typeName, ns, boundedContext, displayLabel, null,
            new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty));
}
