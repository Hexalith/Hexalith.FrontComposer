using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

// Public so NSubstitute (Castle DynamicProxy) can generate a proxy. Lives at namespace scope
// because nested-private interfaces can't be proxied even with InternalsVisibleTo.
public interface IWriteAwareRegistry : IFrontComposerRegistry, IFrontComposerCommandWriteAccessRegistry { }

public sealed class EmptyStateCtaResolverTests {
    [BoundedContext("Orders")]
    [ProjectionEmptyStateCta("ShipOrderCommand")]
    private sealed class ExplicitOrderProjection { }

    [BoundedContext("Orders")]
    private sealed class OrderProjection { }

    private sealed class UnboundedProjection { }

    [Fact]
    public void ExplicitAttribute_ReturnsNamedCommand() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(ExplicitOrderProjection), "CreateOrderCommand", "ShipOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(ExplicitOrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("ShipOrderCommand");
        cta.CommandDisplayName.ShouldBe("Ship Order");
        cta.CommandRoute.ShouldBe("/domain/orders/ship-order-command");
    }

    [Fact]
    public void ResolveExplicit_OverridesProjectionAttribute() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(ExplicitOrderProjection), "CreateOrderCommand", "ShipOrderCommand"));

        EmptyStateCta? cta = resolver.ResolveExplicit(typeof(ExplicitOrderProjection), "CreateOrderCommand");

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("CreateOrderCommand");
    }

    [Fact]
    public void NoAttribute_OneCommandInBoundedContext_ReturnsThatCommand() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection), "ApproveOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("ApproveOrderCommand");
    }

    [Fact]
    public void NoAttribute_CreationVerbPrefixWinsThenAlphabetical() {
        // Spec D4: partition into matches vs non-matches, order each alphabetically.
        // "AddOrderItemCommand" and "CreateOrderCommand" both have creation prefixes →
        // partition winner is the alphabetically first within the matches: "AddOrderItemCommand".
        EmptyStateCtaResolver resolver = CreateResolver(Manifest(
            "Orders",
            typeof(OrderProjection),
            "AddOrderItemCommand",
            "CancelOrderCommand",
            "CreateOrderCommand",
            "ShipOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("AddOrderItemCommand");
    }

    [Fact]
    public void NoAttribute_NoCreationPrefix_FallsBackAlphabetically() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest(
            "Orders",
            typeof(OrderProjection),
            "CancelOrderCommand",
            "ApproveOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("ApproveOrderCommand");
    }

    [Fact]
    public void NoCommandsInBoundedContext_ReturnsNull() {
        // Story 4-6 review fix: this test name was previously misleading ("ReadOnlyBoundedContext")
        // — it only proves "no commands → null". Real read-only filtering is in
        // ReadOnlyCommands_FilteredByWriteAccessRegistry below.
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection)));

        resolver.Resolve(typeof(OrderProjection)).ShouldBeNull();
    }

    [Fact]
    public void ReadOnlyCommands_FilteredByWriteAccessRegistry() {
        // When the registry implements IFrontComposerCommandWriteAccessRegistry the resolver
        // honors its filter. "GetOrdersQuery" returns false → excluded; "CreateOrderCommand"
        // returns true → included as the resolved CTA.
        IWriteAwareRegistry registry = Substitute.For<IWriteAwareRegistry>();
        registry.GetManifests().Returns([Manifest("Orders", typeof(OrderProjection), "GetOrdersQuery", "CreateOrderCommand")]);
        registry.IsCommandWritable("GetOrdersQuery").Returns(false);
        registry.IsCommandWritable("CreateOrderCommand").Returns(true);

        EmptyStateCtaResolver resolver = new(registry, Substitute.For<ILogger<EmptyStateCtaResolver>>());

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandFqn.ShouldBe("CreateOrderCommand");
    }

    [Fact]
    public void AllCommandsReadOnly_ReturnsNull() {
        IWriteAwareRegistry registry = Substitute.For<IWriteAwareRegistry>();
        registry.GetManifests().Returns([Manifest("Orders", typeof(OrderProjection), "GetOrdersQuery", "ListOrdersQuery")]);
        registry.IsCommandWritable(Arg.Any<string>()).Returns(false);

        EmptyStateCtaResolver resolver = new(registry, Substitute.For<ILogger<EmptyStateCtaResolver>>());

        resolver.Resolve(typeof(OrderProjection)).ShouldBeNull();
    }

    [Fact]
    public void ProjectionWithoutBoundedContext_ReturnsNull() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection), "CreateOrderCommand"));

        resolver.Resolve(typeof(UnboundedProjection)).ShouldBeNull();
    }

    [Fact]
    public void ExplicitUnknownCommand_ReturnsNull() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection), "CreateOrderCommand"));

        resolver.ResolveExplicit(typeof(OrderProjection), "MissingCommand").ShouldBeNull();
    }

    [Fact]
    public void RegistryThrows_ReturnsNull() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(_ => throw new InvalidOperationException("boom"));
        var resolver = new EmptyStateCtaResolver(registry, Substitute.For<ILogger<EmptyStateCtaResolver>>());

        resolver.Resolve(typeof(OrderProjection)).ShouldBeNull();
    }

    [Fact]
    public void HumanizeCommandName_HandlesAcronymBoundaries() {
        // Acronym + suffix-Lowercase: URLImportCommand → "URL Import" (vs the pre-fix algorithm
        // that produced "URLImport" by missing the upper→upper-followed-by-lower boundary).
        // The manifest uses bounded-context "Orders" which matches OrderProjection's
        // [BoundedContext("Orders")] attribute; the test then asserts on display-name format.
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection), "URLImportCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandDisplayName.ShouldBe("URL Import");
    }

    private static EmptyStateCtaResolver CreateResolver(params DomainManifest[] manifests) {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(manifests);
        return new EmptyStateCtaResolver(registry, Substitute.For<ILogger<EmptyStateCtaResolver>>());
    }

    private static DomainManifest Manifest(string boundedContext, Type projectionType, params string[] commands)
        => new(
            Name: boundedContext,
            BoundedContext: boundedContext,
            Projections: [projectionType.FullName ?? projectionType.Name],
            Commands: commands);
}
