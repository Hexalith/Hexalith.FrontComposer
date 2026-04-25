using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;

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
    public void ActionQueueRoleUsesRoleSpecificEmptyMessage() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.ActionQueue));

        cut.Markup.ShouldContain("No orders awaiting action.");
    }

    [Fact]
    public void SecondaryTextRendersWhenProvided() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.SecondaryText, "Create the first order from an approved quote."));

        cut.Markup.ShouldContain("Create the first order from an approved quote.");
        cut.Markup.ShouldContain("fc-projection-empty-placeholder-secondary");
    }

    [Fact]
    public void CtaRendersWhenResolverFindsCommand() {
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Orders",
            BoundedContext: "Orders",
            Projections: [typeof(OrderProjection).FullName!],
            Commands: ["CreateOrderCommand"]));

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.CtaCommandName, "CreateOrderCommand"));

        cut.Markup.ShouldContain("Create Order");
        cut.Markup.ShouldContain("href=\"/domain/orders/create-order-command\"");
        cut.Markup.ShouldContain("fc-projection-empty-placeholder-cta");
    }

    [Fact]
    public void ParameterSurfaceIsFrozenForStory4_6() {
        Type t = typeof(FcProjectionEmptyPlaceholder);
        PropertyInfo? projectionType = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.ProjectionType));
        PropertyInfo? entityPluralOverride = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.EntityPluralOverride));
        PropertyInfo? role = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.Role));
        PropertyInfo? ctaCommandName = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.CtaCommandName));
        PropertyInfo? secondaryText = t.GetProperty(nameof(FcProjectionEmptyPlaceholder.SecondaryText));

        projectionType.ShouldNotBeNull();
        entityPluralOverride.ShouldNotBeNull();
        role.ShouldNotBeNull();
        ctaCommandName.ShouldNotBeNull();
        secondaryText.ShouldNotBeNull();

        projectionType!.PropertyType.ShouldBe(typeof(Type));
        role!.PropertyType.ShouldBe(typeof(ProjectionRole?));
        ctaCommandName!.PropertyType.ShouldBe(typeof(string));
        secondaryText!.PropertyType.ShouldBe(typeof(string));
    }
}
