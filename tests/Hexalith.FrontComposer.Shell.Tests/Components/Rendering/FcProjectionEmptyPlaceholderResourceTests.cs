using System.Globalization;
using System.Resources;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>Regression coverage for the secondary-guidance resource convention.</summary>
public sealed class FcProjectionEmptyPlaceholderResourceTests : LayoutComponentTestBase {
    private static readonly ResourceManager s_resources = new(typeof(FcProjectionEmptyPlaceholderResourceTests));

    private readonly IStringLocalizer<FcShellResources> _localizer = Substitute.For<IStringLocalizer<FcShellResources>>();

    public FcProjectionEmptyPlaceholderResourceTests() {
        _localizer[Arg.Any<string>()].Returns(call => {
            string key = call.Arg<string>();
            string? value = s_resources.GetString(key, CultureInfo.InvariantCulture);
            return new LocalizedString(key, value ?? key, resourceNotFound: value is null);
        });
        Services.Replace(ServiceDescriptor.Singleton(_localizer));
    }

    [Theory]
    [InlineData(typeof(NestedProjection), "Nested guidance")]
    [InlineData(typeof(GenericProjection<int>), "Generic guidance")]
    [InlineData(typeof(GenericProjection<string>), "Generic guidance")]
    public void ConventionFindsNestedAndGenericDefinitionResources(Type projection, string guidance) {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, projection));

        cut.Find(".fc-projection-empty-placeholder-secondary").TextContent.Trim().ShouldBe(guidance);
    }

    [Fact]
    public void ExactGenericResourceAndExplicitGuidanceKeepPrecedence() {
        AddResource(typeof(GenericProjection<int>), "Integer guidance");

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(GenericProjection<int>)));

        cut.Find(".fc-projection-empty-placeholder-secondary").TextContent.Trim().ShouldBe("Integer guidance");
        cut.Render(parameters => parameters.Add(p => p.SecondaryText, "Explicit guidance"));
        cut.Find(".fc-projection-empty-placeholder-secondary").TextContent.Trim().ShouldBe("Explicit guidance");
    }

    [Fact]
    public void MissingGenericResourceOmitsSecondaryLine() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(ProjectionWithoutGuidance<int>)));

        cut.FindAll(".fc-projection-empty-placeholder-secondary").ShouldBeEmpty();
    }

    private void AddResource(Type type, string value) {
        string key = type.FullName + "_EmptyStateSecondaryText";
        _localizer[key].Returns(new LocalizedString(key, value));
    }

    private sealed class NestedProjection;
    private sealed class GenericProjection<T>;
    private sealed class ProjectionWithoutGuidance<T>;
}
