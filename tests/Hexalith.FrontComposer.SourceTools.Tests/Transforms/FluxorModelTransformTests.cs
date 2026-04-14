namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

using Xunit;

public class FluxorModelTransformTests
{
    private static DomainModel Model(string typeName)
        => new DomainModel(typeName, "TestDomain", "Test", null, null,
            new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty));

    [Fact]
    public void Transform_StateName_IsDerivedFromTypeName()
    {
        FluxorModel result = FluxorModelTransform.Transform(Model("OrderProjection"));
        result.StateName.ShouldBe("OrderProjectionState");
    }

    [Fact]
    public void Transform_FeatureName_IsDerivedFromTypeName()
    {
        FluxorModel result = FluxorModelTransform.Transform(Model("OrderProjection"));
        result.FeatureName.ShouldBe("OrderProjectionFeature");
    }

    [Fact]
    public void Transform_PreservesTypeNameAndNamespace()
    {
        DomainModel model = new DomainModel("OrderProjection", "MyApp.Orders", "Orders", null, null,
            new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty));
        FluxorModel result = FluxorModelTransform.Transform(model);

        result.TypeName.ShouldBe("OrderProjection");
        result.Namespace.ShouldBe("MyApp.Orders");
    }
}
