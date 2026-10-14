using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Generated;

using Microsoft.Extensions.Time.Testing;
using System.Reflection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Announcements;

public sealed class FreshRowAnnouncementLedgerTests
{
    [Fact]
    public void TransitionIsClaimedOnceAcrossViewsButDistinctEntitiesAndMessagesRemainIndependent()
    {
        FakeTimeProvider time = new();
        FreshRowAnnouncementLedger ledger = new(time, new Epic9UserContextAccessor());
        NewItemIndicatorEntry entry = new("view-one", "row-one", "message-one", time.GetUtcNow());
        ledger.TryClaim(entry).ShouldBeTrue();
        ledger.TryClaim(entry with { ViewKey = "view-two" }).ShouldBeFalse();
        ledger.TryClaim(entry with { EntityKey = "row-two" }).ShouldBeTrue();
        ledger.TryClaim(entry with { MessageId = "message-two" }).ShouldBeTrue();
        time.Advance(TimeSpan.FromMilliseconds(9999));
        ledger.TryClaim(entry).ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.TryClaim(entry).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EquivalentCopiesRetainOriginAcrossViewsScopesAndRemounts(bool tenant)
    {
        FakeTimeProvider time = new();
        Epic9UserContextAccessor scope = new();
        FreshRowAnnouncementLedger ledger = new(time, scope);
        NewItemIndicatorEntry entry = new("one", "row", "message", time.GetUtcNow());
        ledger.IsOriginatingScope(entry).ShouldBeTrue();
        ledger.IsOriginatingScope(entry with { ViewKey = "two" }).ShouldBeTrue();
        if (tenant) scope.TenantId = "other-tenant";
        else scope.UserId = "other-user";
        ledger.IsOriginatingScope(entry with { }).ShouldBeFalse();
        ledger.IsOriginatingScope(entry with { ViewKey = "two" }).ShouldBeFalse();
        scope.TenantId = "test-tenant";
        scope.UserId = "test-user";
        ledger.IsOriginatingScope(entry with { ViewKey = "three" }).ShouldBeFalse();
        // A new publication timestamp is a separate origin, while AM-21 claim identity stays entity/message.
        time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.IsOriginatingScope(entry with { CreatedAt = time.GetUtcNow() }).ShouldBeTrue();
    }

    [Fact]
    public void EquivalentOriginMetadataPrunesAfterTheWindowWithoutShorteningClaimLifetime()
    {
        FakeTimeProvider time = new();
        FreshRowAnnouncementLedger ledger = new(time, new Epic9UserContextAccessor());
        for (int index = 0; index < 100; index++)
            ledger.IsOriginatingScope(new("one", "row-" + index, "message", time.GetUtcNow())).ShouldBeTrue();
        var metadata = (System.Collections.IDictionary)typeof(FreshRowAnnouncementLedger)
            .GetField("_transitionOrigins", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ledger)!;
        metadata.Count.ShouldBe(100);
        time.Advance(TimeSpan.FromMilliseconds(9999));
        ledger.IsOriginatingScope(new("two", "extra", "message", time.GetUtcNow())).ShouldBeTrue();
        metadata.Count.ShouldBe(101);
        time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.IsOriginatingScope(new("three", "next", "message", time.GetUtcNow())).ShouldBeTrue();
        metadata.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(null, "user")]
    [InlineData("", "user")]
    [InlineData(" ", "user")]
    [InlineData("tenant", null)]
    [InlineData("tenant", "")]
    [InlineData("tenant", " ")]
    public void UnresolvedScopeDoesNotClaimAndClearsPriorClaims(string? tenant, string? user)
    {
        FakeTimeProvider time = new();
        Epic9UserContextAccessor scope = new();
        FreshRowAnnouncementLedger ledger = new(time, scope);
        NewItemIndicatorEntry entry = new("view", "row", "message", time.GetUtcNow());
        ledger.TryClaim(entry).ShouldBeTrue();
        scope.TenantId = tenant;
        scope.UserId = user;
        ledger.HasResolvedScope().ShouldBeFalse();
        ledger.TryClaim(entry).ShouldBeFalse();
        scope.TenantId = "test-tenant";
        scope.UserId = "test-user";
        time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.TryClaim(entry with { CreatedAt = time.GetUtcNow() }).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopeChangeClearsClaimsBeforeNewScopeIsUsed(bool changeTenant)
    {
        FakeTimeProvider time = new();
        Epic9UserContextAccessor scope = new();
        FreshRowAnnouncementLedger ledger = new(time, scope);
        NewItemIndicatorEntry entry = new("view", "row", "message", time.GetUtcNow());
        ledger.TryClaim(entry).ShouldBeTrue();
        if (changeTenant) scope.TenantId = "other-tenant";
        else scope.UserId = "other-user";
        ledger.HasResolvedScope().ShouldBeTrue();
        ledger.TryClaim(entry).ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        NewItemIndicatorEntry newScopeEntry = entry with { CreatedAt = time.GetUtcNow() };
        ledger.TryClaim(newScopeEntry).ShouldBeTrue();
        ledger.TryClaim(newScopeEntry).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FatalScopeAccessorFailurePropagatesUnchanged(bool tenant)
    {
        FakeTimeProvider time = new();
        IUserContextAccessor scope = Substitute.For<IUserContextAccessor>();
        scope.TenantId.Returns("test-tenant");
        scope.UserId.Returns("test-user");
        OutOfMemoryException failure = (OutOfMemoryException)RuntimeHelpers.GetUninitializedObject(typeof(OutOfMemoryException));
        if (tenant)
        {
            scope.TenantId.Returns(_ => throw failure);
        }
        else
        {
            scope.UserId.Returns(_ => throw failure);
        }
        FreshRowAnnouncementLedger ledger = new(time, scope);
        NewItemIndicatorEntry entry = new("view", "row", "message", time.GetUtcNow());

        Should.Throw<OutOfMemoryException>(() => ledger.IsOriginatingScope(entry)).ShouldBeSameAs(failure);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopeSwitchBetweenEligibilityAndClaimRejectsBoundEntry(bool changeTenant)
    {
        FakeTimeProvider time = new();
        Epic9UserContextAccessor scope = new();
        FreshRowAnnouncementLedger ledger = new(time, scope);
        NewItemIndicatorEntry entry = new("view", "row", "message", time.GetUtcNow());
        ledger.IsOriginatingScope(entry).ShouldBeTrue();

        if (changeTenant) scope.TenantId = "other-tenant";
        else scope.UserId = "other-user";

        ledger.TryClaim(entry).ShouldBeFalse();
        ledger.TryClaim(entry with { ViewKey = "other-view" }).ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.TryClaim(entry with { CreatedAt = time.GetUtcNow() }).ShouldBeTrue();
    }
}
