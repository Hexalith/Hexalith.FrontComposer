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

        System.Reflection.PropertyInfo? property = interfaceType.GetProperty("ActionQueueTypes");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IReadOnlyList<Type>));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void Interface_DeclaresOnlyOneMember_PreventingShapeDrift() {
        Type interfaceType = typeof(IActionQueueProjectionCatalog);
        System.Reflection.MemberInfo[] declared = interfaceType.GetMembers(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        // 1 property + its getter accessor = 2 members.
        declared.Length.ShouldBe(2);
    }
}
