using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

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
        cta.CommandTypeName.ShouldBe("ShipOrderCommand");
        cta.CommandDisplayName.ShouldBe("Ship Order");
        cta.CommandRoute.ShouldBe("/domain/orders/ship-order-command");
    }

    [Fact]
    public void ExplicitParameter_OverridesProjectionAttribute() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(ExplicitOrderProjection), "CreateOrderCommand", "ShipOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(ExplicitOrderProjection), "CreateOrderCommand");

        cta.ShouldNotBeNull();
        cta.CommandTypeName.ShouldBe("CreateOrderCommand");
    }

    [Fact]
    public void NoAttribute_OneCommandInBoundedContext_ReturnsThatCommand() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection), "ApproveOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandTypeName.ShouldBe("ApproveOrderCommand");
    }

    [Fact]
    public void NoAttribute_CreationVerbPrefixWinsInDeclaredRankOrder() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest(
            "Orders",
            typeof(OrderProjection),
            "AddOrderItemCommand",
            "CancelOrderCommand",
            "CreateOrderCommand",
            "ShipOrderCommand"));

        EmptyStateCta? cta = resolver.Resolve(typeof(OrderProjection));

        cta.ShouldNotBeNull();
        cta.CommandTypeName.ShouldBe("CreateOrderCommand");
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
        cta.CommandTypeName.ShouldBe("ApproveOrderCommand");
    }

    [Fact]
    public void ReadOnlyBoundedContext_ReturnsNull() {
        EmptyStateCtaResolver resolver = CreateResolver(Manifest("Orders", typeof(OrderProjection)));

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

        resolver.Resolve(typeof(OrderProjection), "MissingCommand").ShouldBeNull();
    }

    [Fact]
    public void RegistryThrows_ReturnsNull() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(_ => throw new InvalidOperationException("boom"));
        var resolver = new EmptyStateCtaResolver(registry, Substitute.For<ILogger<EmptyStateCtaResolver>>());

        resolver.Resolve(typeof(OrderProjection)).ShouldBeNull();
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
