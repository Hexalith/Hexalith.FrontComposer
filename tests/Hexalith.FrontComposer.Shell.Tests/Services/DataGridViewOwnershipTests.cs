using Fluxor;

using Hexalith.FrontComposer.Shell.Services;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class DataGridViewOwnershipTests
{
    [Fact]
    public void ReplacementRegistrationInvalidatesEarlierFinalOwnerCleanup()
    {
        DataGridViewOwnership owners = DataGridViewOwnership.For(Substitute.For<IDispatcher>());
        long first = owners.Register("view");
        long coMounted = owners.Register("view");
        coMounted.ShouldBe(first);
        owners.Release("view").ShouldBeFalse();
        owners.Release("view").ShouldBeTrue();
        owners.CanDispose("view", first).ShouldBeTrue();

        long replacement = owners.Register("view");
        replacement.ShouldBeGreaterThan(first);
        owners.CanDispose("view", first).ShouldBeFalse();
        owners.Release("view").ShouldBeTrue();
        owners.CanDispose("view", replacement).ShouldBeTrue();
    }
}
