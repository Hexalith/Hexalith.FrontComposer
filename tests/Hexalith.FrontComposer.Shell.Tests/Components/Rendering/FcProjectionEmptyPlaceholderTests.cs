using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T4.5 / ADR-053 — <see cref="FcProjectionEmptyPlaceholder"/> tests. Minimal
/// placeholder (Story 4.6 replaces the body). Parameter surface frozen + append-only.
/// </summary>
public sealed class FcProjectionEmptyPlaceholderTests : LayoutComponentTestBase {
    private sealed class OrderProjection { }

    public FcProjectionEmptyPlaceholderTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersHumanizedEntityPluralAndMessage() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection)));

        // Message template: "No {0} yet." with entity plural = "orders" (Projection suffix stripped + "s" added).
        cut.Markup.ShouldContain("No orders yet.");
    }

    [Fact]
    public void RendersAriaLabelForAssistiveTech() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection)));

        cut.Markup.ShouldContain("aria-label=\"No orders found\"");
        cut.Markup.ShouldContain("role=\"status\"");
    }

    [Fact]
    public void RendersFrenchAriaLabelForAssistiveTech() {
        CultureInfo french = new("fr-FR");
        CultureInfo.CurrentCulture = french;
        CultureInfo.CurrentUICulture = french;

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection)));

        cut.Markup.ShouldContain("aria-label=\"Aucun résultat trouvé pour orders\"");
    }

    [Fact]
    public void EntityPluralOverrideWinsWhenProvided() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.EntityPluralOverride, "shipments"));

        cut.Markup.ShouldContain("No shipments yet.");
    }

    [Fact]
    public void RoleParameterIsAcceptedButIgnoredInV1Body() {
        // Story 4-1 H4 — Role is present + optional + typed ProjectionRole? in 4-1; Story 4.6
        // will consume it for role-differentiated CTA copy without generator changes.
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.ActionQueue));

        cut.Markup.ShouldContain("No orders yet.");
        // Role not yet rendered into the body in 4-1; only its presence as a parameter matters.
    }

    [Fact]
    public void ParameterSurfaceIsFrozenForStory4_6() {
        Type t = typeof(FcProjectionEmptyPlaceholder);
        PropertyInfo? projectionType = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.ProjectionType));
        PropertyInfo? entityPluralOverride = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.EntityPluralOverride));
        PropertyInfo? role = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.Role));

        projectionType.ShouldNotBeNull();
        entityPluralOverride.ShouldNotBeNull();
        role.ShouldNotBeNull();

        projectionType!.PropertyType.ShouldBe(typeof(Type));
        role!.PropertyType.ShouldBe(typeof(ProjectionRole?));
    }
}
