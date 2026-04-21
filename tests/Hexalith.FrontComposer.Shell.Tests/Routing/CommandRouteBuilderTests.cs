// Story 3-4 Task 10.2 (D21 — AC5) + Task 10.4d (D10 internal-route filter).
using Hexalith.FrontComposer.Shell.Routing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Routing;

public class CommandRouteBuilderTests
{
    [Theory]
    [InlineData("SubmitOrderCommand", "submit-order-command")]
    [InlineData("IncrementCommand", "increment-command")]
    [InlineData("XMLParser", "xml-parser")]
    [InlineData("Counter.Domain.IncrementCommand", "increment-command")]
    public void KebabCase_ProducesExpectedSlug(string input, string expected)
    {
        CommandRouteBuilder.KebabCase(input).ShouldBe(expected);
    }

    [Fact]
    public void BuildRoute_ProducesCanonicalDomainUrl()
    {
        CommandRouteBuilder.BuildRoute("Commerce", "SubmitOrderCommand")
            .ShouldBe("/domain/commerce/submit-order-command");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/counter")]
    [InlineData("/domain/commerce/submit-order")]
    [InlineData("/path?q=1")]
    [InlineData("/path#frag")]
    [InlineData("/calendar/10:30")]
    [InlineData("/path?at=10:30")]
    [InlineData("/path#section:2")]
    public void IsInternalRoute_AcceptsValidInternalUrls(string url)
    {
        CommandRouteBuilder.IsInternalRoute(url).ShouldBeTrue();
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("mailto:test@example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,evil")]
    [InlineData("/redirect?next=https://evil.com")]
    [InlineData("/page#javascript:alert(1)")]
    [InlineData("/proxy?payload=data:text/html,evil")]
    [InlineData("\\\\unc")]
    [InlineData("")]
    [InlineData(null)]
    public void IsInternalRoute_RejectsTamperedOrAbsoluteUrls(string? url)
    {
        CommandRouteBuilder.IsInternalRoute(url).ShouldBeFalse();
    }
}
