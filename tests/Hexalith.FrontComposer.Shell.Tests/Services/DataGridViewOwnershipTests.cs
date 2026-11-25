using Fluxor;

using Hexalith.FrontComposer.Shell.Services;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class DataGridViewOwnershipTests
{
    [Fact]
    public void StableHostScopesPreserveSlotsWhenDuplicateViewsRemountInReverseOrder()
    {
        DataGridViewOwnership owners = DataGridViewOwnership.For(Substitute.For<IDispatcher>());
        _ = owners.Register("view", "first", out int first);
        _ = owners.Register("view", "second", out int second);
        owners.Release("view", "first", first).ShouldBeFalse();
        owners.Release("view", "second", second).ShouldBeTrue();

        _ = owners.Register("view", "second", out int remountedSecond);
        _ = owners.Register("view", "first", out int remountedFirst);

        remountedFirst.ShouldBe(first);
        remountedSecond.ShouldBe(second);
        string firstId = DataGridViewOwnership.CreatePanelId("view", "first", remountedFirst);
        firstId.ShouldNotBe(DataGridViewOwnership.CreatePanelId("view", "second", remountedSecond));
    }

    [Fact]
    public void RowPanelIdsDistinguishTimestampsWithinTheSameSecond()
    {
        DateTime stamp = new(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc);
        string first = DataGridViewOwnership.CreatePanelId("view", string.Empty, 0, stamp);
        string second = DataGridViewOwnership.CreatePanelId("view", string.Empty, 0, stamp.AddTicks(1));

        first.ShouldNotBe(second);
    }

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
