using System.Reflection;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.ProjectionContext;

/// <summary>
/// Story 4-1 T3.12 / C2 — documentation gate for the
/// <see cref="Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext"/> shape
/// inherited from Story 2-2 D27.
/// </summary>
/// <remarks>
/// <para>The original story text expected a <c>readonly record struct</c> shape so the
/// per-row cascade (D14) would be allocation-free. Story 2-2 actually shipped
/// <see cref="Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext"/> as a
/// <c>sealed record</c> (class) — value-equatable via record semantics (so the
/// <c>IsFixed="true"</c> cascade still suppresses unnecessary re-renders) but one
/// allocation per row per render pass rather than zero.</para>
/// <para>Per D14 guidance: 4-1 does NOT change the Story 2-2 contract. The
/// <c>IsFixed="true"</c> cascade is still correct; the allocation cost is acceptable
/// for v1. A Story 2-2 follow-up is filed under Known Gaps to convert
/// <see cref="Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext"/> to a
/// <c>readonly record struct</c> once Story 4.4 virtualization has profiled the actual
/// per-row cost. This test documents that state so a future drift toward a mutable
/// class is caught.</para>
/// </remarks>
public class ProjectionContextTypeTests {
    [Fact]
    public void IsValueEquatableViaRecordSemantics() {
        // Records (class or struct) provide structural equality.
        Type t = typeof(Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext);
        bool hasEqualityContract = t.GetProperty(
            "EqualityContract",
            BindingFlags.NonPublic | BindingFlags.Instance) is not null;

        hasEqualityContract.ShouldBeTrue(
            "ProjectionContext must preserve record semantics for structural equality — see Story 4-1 D14.");
    }

    [Fact]
    public void IsSealedToProhibitSubclassing() {
        // Story 2-2 D27 — the cascading-parameter shape is frozen; adopter subclassing
        // would break value equality invariants and the incremental cache contract.
        typeof(Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void CurrentShapeIsSealedRecordClassNotStruct_DocumentsDeferralToStory2_2Followup() {
        // Story 4-1 T3.12 — documents the current Story 2-2 shape for Story 4-1's dev
        // agent. Expected state: ProjectionContext is a sealed record (reference type).
        // A follow-up may convert it to readonly record struct; the D14 cascade is
        // correct under either shape.
        Type t = typeof(Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext);
        t.IsValueType.ShouldBeFalse(
            "ProjectionContext is currently a sealed record (class). If this ever becomes a "
            + "struct, update Story 4-1 D14 to celebrate the zero-allocation cascade.");
    }
}
