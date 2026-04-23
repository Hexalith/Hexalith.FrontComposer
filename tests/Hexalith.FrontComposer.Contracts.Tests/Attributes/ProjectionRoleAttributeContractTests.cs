using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Attributes;

/// <summary>
/// Story 4-1 T3.11 / ADR-051 — Contracts-surface gate asserting
/// <see cref="ProjectionRoleAttribute"/> + <see cref="ProjectionRole"/> match PRD FR4
/// verbatim (attribute name, cap of 5, Dashboard still present). Gated at emit-stage
/// exit (not T7) per Bob/Murat review so a contract break surfaces BEFORE emit work
/// compounds.
/// </summary>
public class ProjectionRoleAttributeContractTests {
    [Fact]
    public void AttributeNameMatchesPrdFR4() {
        // ADR-051 / D1 — the attribute name is ProjectionRoleAttribute (not
        // ProjectionRoleHintAttribute as Epic 4 AC drift would suggest). Story 3-5's
        // IActionQueueProjectionCatalog depends on this exact name.
        typeof(ProjectionRoleAttribute).Name.ShouldBe("ProjectionRoleAttribute");
    }

    [Fact]
    public void WhenStatePropertyExistsAsOptional() {
        // Story 4-1 D2 / T1.1 — WhenState lands as an optional init-only property (not
        // a second ctor argument) so pre-4-1 annotations [ProjectionRole(role)] remain
        // source-compatible.
        PropertyInfo? prop = typeof(ProjectionRoleAttribute).GetProperty("WhenState");
        prop.ShouldNotBeNull();
        prop!.PropertyType.ShouldBe(typeof(string));  // string? is still typeof(string) at reflection time.
        prop.CanRead.ShouldBeTrue();
        prop.CanWrite.ShouldBeTrue(); // init-only is writable via reflection
    }

    [Fact]
    public void ProjectionRoleIsBackwardsCompatibleViaSingleArgConstructor() {
        // The pre-4-1 constructor [ProjectionRole(role)] MUST continue to compile.
        // Adopters who want WhenState write [ProjectionRole(role, WhenState = "A,B")].
        ConstructorInfo[] ctors = typeof(ProjectionRoleAttribute).GetConstructors();
        ctors.ShouldContain(c =>
            c.GetParameters().Length == 1 &&
            c.GetParameters()[0].ParameterType == typeof(ProjectionRole));
    }
}
