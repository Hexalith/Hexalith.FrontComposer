using Hexalith.FrontComposer.Shell.State.ETagCache;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.State.ETagCache;

/// <summary>
/// Story 5-2 T3 / AC6 — discriminator allowlist. Anything that resembles user-entered text,
/// PII, hashes, or path-traversal characters MUST be rejected so the bounded ETag cache
/// can never grow a key derived from untrusted data.
/// </summary>
public class ETagCacheDiscriminatorTests {
    [Theory]
    [InlineData("Counter.Domain.OrderProjection", 0, 25, "projection-page:Counter.Domain.OrderProjection:s0-t25")]
    [InlineData("Counter.Domain.OrderProjection", 25, 25, "projection-page:Counter.Domain.OrderProjection:s25-t25")]
    public void ForProjectionPage_BuildsAllowlistedDiscriminator(string typeFqn, int skip, int take, string expected) {
        ETagCacheDiscriminator.ForProjectionPage(typeFqn, skip, take).ShouldBe(expected);
    }

    [Fact]
    public void ForActionQueueCount_BuildsAllowlistedDiscriminator() {
        ETagCacheDiscriminator.ForActionQueueCount("Counter.Domain.ApprovalQueue")
            .ShouldBe("action-queue-count:Counter.Domain.ApprovalQueue");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not allowed because of spaces")]
    [InlineData("path/with/slash")]
    [InlineData("path\\with\\back")]
    [InlineData("contains:colon")]
    [InlineData("contains?query")]
    [InlineData("contains#fragment")]
    public void ForProjectionPage_RejectsUnsafeTypeFqn(string? typeFqn) {
        ETagCacheDiscriminator.ForProjectionPage(typeFqn, 0, 25).ShouldBeNull();
    }

    [Theory]
    [InlineData("Some.Type", -1, 10)]
    [InlineData("Some.Type", 0, 0)]
    [InlineData("Some.Type", 0, -1)]
    public void ForProjectionPage_RejectsInvalidPaging(string typeFqn, int skip, int take) {
        ETagCacheDiscriminator.ForProjectionPage(typeFqn, skip, take).ShouldBeNull();
    }

    [Theory]
    [InlineData("projection-page:Foo:s0-t25", true)]
    [InlineData("action-queue-count:Foo", true)]
    [InlineData("foo", false)]
    [InlineData("user-input:hostile", false)]
    [InlineData("projection-page:Foo:", false)]
    [InlineData("projection-page::Foo", false)]
    [InlineData("projection-page:Foo", false)]
    [InlineData("projection-page:Foo:s0", false)]
    [InlineData("projection-page:Foo:s0-t0", false)]
    [InlineData("projection-page:Foo/Bar:s0-t25", false)]
    [InlineData("action-queue-count:Foo:Bar", false)]
    [InlineData("projection-page:Foo s0-t25", false)]
    public void IsAllowlisted_AcceptsOnlyKnownLanePrefixesAndCleanShape(string discriminator, bool expected) {
        ETagCacheDiscriminator.IsAllowlisted(discriminator).ShouldBe(expected);
    }
}
