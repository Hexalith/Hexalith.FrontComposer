using System.Globalization;
using System.Reflection;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T4.5 / ADR-053 + Story 4-6 review patches — <see cref="FcProjectionEmptyPlaceholder"/>
/// tests. Parameter surface frozen + append-only; Story 4-6 ships the full anatomy + auth-gated CTA.
/// </summary>
public sealed class FcProjectionEmptyPlaceholderTests : LayoutComponentTestBase {
    private sealed class OrderProjection { }

    [ProjectionEmptyStateCta(nameof(NewShipmentCommand))]
    private sealed class ShipmentProjection { }

    private sealed class ReadOnlyProjection { }

    // Pass-4 BH-30 — keep test command types private (no need to widen visibility for bUnit).
    private sealed class CreateOrderCommand { }

    private sealed class NewShipmentCommand { }

    private readonly BunitAuthorizationContext _auth;
    private readonly TestCommandAuthorizationEvaluator _authorizationEvaluator = new();

    public FcProjectionEmptyPlaceholderTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        // bUnit forbids service registration after the provider is built; AddAuthorization()
        // must run BEFORE EnsureStoreInitialized triggers the first service resolution. The
        // returned context is mutable across tests via SetAuthorized / SetNotAuthorized.
        _auth = AddAuthorization();
        _ = Services.AddAuthorizationCore(options =>
            options.AddPolicy("OrderApprover", policy => policy.RequireAuthenticatedUser()));
        Services.Replace(ServiceDescriptor.Scoped<ICommandAuthorizationEvaluator>(_ => _authorizationEvaluator));
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersHumanizedEntityPluralAndMessage() {
        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection)));

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

        // P-8 (Pass-3 review fix): FR EmptyStateAriaLabelTemplate aligned to spec "Aucun {0} trouvé".
        cut.Markup.ShouldContain("aria-label=\"Aucun orders trouvé\"");
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
    public void CtaRendersWhenResolverFindsCommandAndUserAuthenticated() {
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Orders",
            BoundedContext: "Orders",
            Projections: [typeof(OrderProjection).FullName!],
            Commands: ["CreateOrderCommand"]));
        _auth.SetAuthorized("test-user");

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.CtaCommandName, "CreateOrderCommand"));

        cut.Markup.ShouldContain("Send your first Create Order");
        cut.Markup.ShouldContain("href=\"/domain/orders/create-order-command\"");
        cut.Markup.ShouldContain("fc-projection-empty-placeholder-cta");
    }

    [Fact]
    public void CtaIsHiddenForAnonymousUsers_AC2_5() {
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Orders",
            BoundedContext: "Orders",
            Projections: [typeof(OrderProjection).FullName!],
            Commands: ["CreateOrderCommand"]));
        _auth.SetNotAuthorized();

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.CtaCommandName, "CreateOrderCommand"));

        // Empty-state copy still renders for anonymous users; the CTA anchor does not.
        cut.Markup.ShouldContain("No orders yet.");
        cut.Markup.ShouldNotContain("fc-projection-empty-placeholder-cta");
        cut.Markup.ShouldNotContain("Send your first");
    }

    [Fact]
    public void PolicyProtectedCtaUsesEvaluatorBackedResourceAuthorization() {
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Orders",
            BoundedContext: "Orders",
            Projections: [typeof(OrderProjection).FullName!],
            Commands: [typeof(ProtectedCreateOrderCommand).FullName!],
            CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                [typeof(ProtectedCreateOrderCommand).FullName!] = "OrderApprover",
            }));
        _auth.SetAuthorized("test-user");
        Services.GetRequiredService<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>()
            .ResolveExplicit(typeof(OrderProjection), typeof(ProtectedCreateOrderCommand).FullName!)
            .ShouldNotBeNull()
            .AuthorizationPolicy.ShouldBe("OrderApprover");

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.CtaCommandName, typeof(ProtectedCreateOrderCommand).FullName!));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Send your first Protected Create Order"));
        cut.Markup.ShouldContain("fc-projection-empty-placeholder-cta");
        cut.WaitForAssertion(() => _authorizationEvaluator.LastRequest.ShouldNotBeNull());
        _authorizationEvaluator.LastRequest!.SourceSurface.ShouldBe(CommandAuthorizationSurface.EmptyStateCta);
    }

    [Fact]
    public void PolicyProtectedCtaIsHiddenForUnauthorizedUser() {
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Orders",
            BoundedContext: "Orders",
            Projections: [typeof(OrderProjection).FullName!],
            Commands: [typeof(ProtectedCreateOrderCommand).FullName!],
            CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                [typeof(ProtectedCreateOrderCommand).FullName!] = "OrderApprover",
            }));
        _authorizationEvaluator.Decision = CommandAuthorizationDecision.Denied("corr-denied");
        _auth.SetNotAuthorized();

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.CtaCommandName, typeof(ProtectedCreateOrderCommand).FullName!));

        // Pass-4 BH-28 — assert the evaluator was actually consulted, not that the CTA is missing
        // for the wrong reason (e.g., still in Pending state, or the gate was bypassed).
        cut.WaitForAssertion(() => _authorizationEvaluator.LastRequest.ShouldNotBeNull());
        _authorizationEvaluator.LastRequest!.SourceSurface.ShouldBe(CommandAuthorizationSurface.EmptyStateCta);
        _authorizationEvaluator.LastRequest!.BoundedContext.ShouldBe("Orders");
        cut.Markup.ShouldNotContain("fc-projection-empty-placeholder-cta");
    }

    [Fact]
    public void NoCtaWhenProjectionHasNoRegisteredCommands() {
        // Bounded context exists but registers no commands → resolver returns null → no CTA.
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Reports",
            BoundedContext: "Reports",
            Projections: [typeof(ReadOnlyProjection).FullName!],
            Commands: []));
        _auth.SetAuthorized("test-user");

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(ReadOnlyProjection)));

        cut.Markup.ShouldContain("No read onlys yet.");
        cut.Markup.ShouldNotContain("fc-projection-empty-placeholder-cta");
    }

    [Fact]
    public void ProjectionEmptyStateCtaAttribute_OverridesBoundedContextFallback() {
        // BC fallback would pick the alphabetically-first creation-verb command (CreateOrder),
        // but the projection carries [ProjectionEmptyStateCta(nameof(NewShipmentCommand))]
        // which must take precedence per Story 4-6 D5/D6.
        Services.GetRequiredService<IFrontComposerRegistry>().RegisterDomain(new DomainManifest(
            Name: "Logistics",
            BoundedContext: "Logistics",
            Projections: [typeof(ShipmentProjection).FullName!],
            Commands: ["CreateOrderCommand", "NewShipmentCommand"]));
        _auth.SetAuthorized("test-user");

        IRenderedComponent<FcProjectionEmptyPlaceholder> cut = Render<FcProjectionEmptyPlaceholder>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(ShipmentProjection)));

        cut.Markup.ShouldContain("Send your first New Shipment");
        cut.Markup.ShouldNotContain("Create Order");
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

    private sealed class TestCommandAuthorizationEvaluator : ICommandAuthorizationEvaluator {
        public CommandAuthorizationDecision Decision { get; set; } = CommandAuthorizationDecision.Allowed("corr-allowed");

        public CommandAuthorizationRequest? LastRequest { get; private set; }

        public Task<CommandAuthorizationDecision> EvaluateAsync(
            CommandAuthorizationRequest request,
            CancellationToken cancellationToken = default) {
            LastRequest = request;
            return Task.FromResult(Decision);
        }
    }
}

// Pass-4 BH-29 — test command type is internal at namespace scope (was public at file scope which
// polluted the test assembly's public API). Stays top-level so its FullName matches a real
// production command shape (CommandRouteBuilder.IsInternalRoute rejects nested-type `+` separators).
internal sealed class ProtectedCreateOrderCommand { }
