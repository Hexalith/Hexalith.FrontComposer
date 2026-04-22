using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Badges;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Badges;

[ProjectionRole(ProjectionRole.ActionQueue)]
public class CatalogTestActionQueueProjectionA { }

[ProjectionRole(ProjectionRole.ActionQueue)]
public class CatalogTestActionQueueProjectionB { }

[ProjectionRole(ProjectionRole.StatusOverview)]
public class CatalogTestStatusOverviewProjection { }

[ProjectionRole(ProjectionRole.ActionQueue)]
public abstract class CatalogTestAbstractActionQueueProjection { }

public class CatalogTestUnattributedClass { }

public sealed class ReflectionActionQueueProjectionCatalogTests {
    [Fact]
    public void Discover_WithInjectedAssemblyList_ReturnsOnlyProvided() {
        // R2 — synthetic assembly list passed via ctor; no AppDomain dependency.
        Assembly[] assemblies = [typeof(ReflectionActionQueueProjectionCatalogTests).Assembly];
        ReflectionActionQueueProjectionCatalog sut = new(
            assemblies,
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>());

        IReadOnlyList<Type> result = sut.ActionQueueTypes;

        result.ShouldContain(typeof(CatalogTestActionQueueProjectionA));
        result.ShouldContain(typeof(CatalogTestActionQueueProjectionB));
    }

    [Fact]
    public void Discover_SkipsNonActionQueueRoles_AndUnattributedTypes() {
        Assembly[] assemblies = [typeof(ReflectionActionQueueProjectionCatalogTests).Assembly];
        ReflectionActionQueueProjectionCatalog sut = new(
            assemblies,
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>());

        IReadOnlyList<Type> result = sut.ActionQueueTypes;

        result.ShouldNotContain(typeof(CatalogTestStatusOverviewProjection));
        result.ShouldNotContain(typeof(CatalogTestUnattributedClass));
    }

    [Fact]
    public void Discover_SkipsAbstractClasses() {
        Assembly[] assemblies = [typeof(ReflectionActionQueueProjectionCatalogTests).Assembly];
        ReflectionActionQueueProjectionCatalog sut = new(
            assemblies,
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>());

        IReadOnlyList<Type> result = sut.ActionQueueTypes;

        result.ShouldNotContain(typeof(CatalogTestAbstractActionQueueProjection));
    }

    [Fact]
    public void CachesResult_OnRepeatedReads_SameInstance() {
        Assembly[] assemblies = [typeof(ReflectionActionQueueProjectionCatalogTests).Assembly];
        ReflectionActionQueueProjectionCatalog sut = new(
            assemblies,
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>());

        IReadOnlyList<Type> first = sut.ActionQueueTypes;
        IReadOnlyList<Type> second = sut.ActionQueueTypes;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void EmptyAssemblyList_ReturnsEmptyResult() {
        ReflectionActionQueueProjectionCatalog sut = new(
            Array.Empty<Assembly>(),
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>());

        sut.ActionQueueTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_OnNullAssemblies() {
        Should.Throw<ArgumentNullException>(() => new ReflectionActionQueueProjectionCatalog(
            null!,
            Substitute.For<ILogger<ReflectionActionQueueProjectionCatalog>>()));
    }
}
