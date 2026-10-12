using Fluxor;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class ProjectionStateScopeOwnershipTests
{
    [Fact]
    public void LatePriorUserCannotReplaceCurrentGenerationOwner()
    {
        ProjectionStateScopeOwnership ownership = ProjectionStateScopeOwnership.For(Substitute.For<IDispatcher>());
        TenantContextSnapshot userA = new("tenant", "user-a", true, "correlation-a");
        TenantContextSnapshot userB = new("tenant", "user-b", true, "correlation-b");

        ownership.IsCurrent("projection", "tenant", "user-a", userA, 4).ShouldBeTrue();
        ownership.IsCurrent("projection", "tenant", "user-b", userB, 5).ShouldBeTrue();
        ownership.IsCurrent("projection", "tenant", "user-a", userB, 5).ShouldBeFalse();
        ownership.IsCurrent("projection", "tenant", "user-b", userB, 5).ShouldBeTrue();
    }
}
