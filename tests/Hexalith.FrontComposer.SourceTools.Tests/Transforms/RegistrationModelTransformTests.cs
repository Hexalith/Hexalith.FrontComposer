using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
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

    private static DomainModel Model(string typeName, string ns, string? boundedContext, string? displayLabel = null)
                                    => new(typeName, ns, boundedContext, displayLabel, null,
            new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty));
}
