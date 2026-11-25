using System.Reflection;

using Hexalith.FrontComposer.Contracts.Badges;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Badges;

/// <summary>
/// Story 3-5 Task 7 — interface-shape contract tests for <see cref="IActionQueueProjectionCatalog"/>.
/// Locks the public surface so accidental shape changes break compile-time tests, not adopters.
/// </summary>
public sealed class IActionQueueProjectionCatalogContractTests {
    [Fact]
    public void Interface_ExposesActionQueueTypes_AsReadOnlyListOfType() {
        Type interfaceType = typeof(IActionQueueProjectionCatalog);

        PropertyInfo? property = interfaceType.GetProperty(nameof(IActionQueueProjectionCatalog.ActionQueueTypes));
        _ = property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IReadOnlyList<Type>));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void Interface_DeclaresOnlyOneMember_PreventingShapeDrift() {
        Type interfaceType = typeof(IActionQueueProjectionCatalog);
        const BindingFlags declaredPublic =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        PropertyInfo property = interfaceType.GetProperties(declaredPublic).ShouldHaveSingleItem();

        interfaceType.GetMembers(declaredPublic)
            .Where(member => !member.Equals(property.GetMethod))
            .ShouldHaveSingleItem()
            .ShouldBe(property);
    }
}
