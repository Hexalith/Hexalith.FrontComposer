using Hexalith.FrontComposer.Contracts.Badges;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Badges;

/// <summary>
/// Story 3-5 Task 7 — interface-shape contract tests for <see cref="IActionQueueCountReader"/>.
/// Locks the public surface so an accidental signature change (e.g. <c>Task&lt;int&gt;</c> instead
/// of <c>ValueTask&lt;int&gt;</c>) breaks compile-time tests rather than adopters.
/// </summary>
public sealed class IActionQueueCountReaderContractTests {
    [Fact]
    public void Interface_ExposesGetCountAsync_WithExactSignature() {
        Type interfaceType = typeof(IActionQueueCountReader);

        System.Reflection.MethodInfo? method = interfaceType.GetMethod("GetCountAsync");
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(ValueTask<int>));

        System.Reflection.ParameterInfo[] parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(Type));
        parameters[0].Name.ShouldBe("projectionType");
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].Name.ShouldBe("cancellationToken");
    }

    [Fact]
    public void Interface_DeclaresOnlyOneMember_PreventingShapeDrift() {
        Type interfaceType = typeof(IActionQueueCountReader);
        System.Reflection.MemberInfo[] declared = interfaceType.GetMembers(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        declared.Length.ShouldBe(1);
    }
}
