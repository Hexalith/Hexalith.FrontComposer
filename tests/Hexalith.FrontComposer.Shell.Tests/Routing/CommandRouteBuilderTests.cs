// Story 3-4 Task 10.2 (D21 — AC5) + Task 10.4d (D10 internal-route filter).
using Hexalith.FrontComposer.Shell.Routing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Routing;

public class CommandRouteBuilderTests {
    [Theory]
    [InlineData("SubmitOrderCommand", "submit-order-command")]
    [InlineData("IncrementCommand", "increment-command")]
    [InlineData("XMLParser", "xml-parser")]
    [InlineData("GenerateXML", "generate-xml")]
    [InlineData("Counter.Domain.IncrementCommand", "increment-command")]
    [InlineData("Domaine.CaféCommande", "café-commande")]
    [InlineData("Domaine.Café\U00010400View", "café-\U00010428-view")]
    public void KebabCase_ProducesExpectedSlug(string input, string expected) => CommandRouteBuilder.KebabCase(input).ShouldBe(expected);

    [Theory]
    [InlineData("Orders.Checkout Command")]
    [InlineData("Orders\t.CheckoutCommand")]
    [InlineData("Checkout\u00a0Command")]
    [InlineData(".CheckoutCommand")]
    [InlineData("Orders..CheckoutCommand")]
    [InlineData("Orders.")]
    public void KebabCase_RejectsWhitespaceOrEmptySegments(string input)
        => Should.Throw<ArgumentException>(() => CommandRouteBuilder.KebabCase(input))
            .ParamName.ShouldBe("pascalCase");

    [Fact]
    public void BuildRoute_ProducesCanonicalGeneratedCommandUrl() => CommandRouteBuilder.BuildRoute("Commerce", "Commerce.Commands.SubmitOrderCommand")
            .ShouldBe("/commands/Commerce/SubmitOrderCommand");

    [Fact]
    public void BuildRoute_MissingBoundedContext_UsesDefault() => CommandRouteBuilder.BuildRoute(null, "SubmitOrderCommand")
            .ShouldBe("/commands/Default/SubmitOrderCommand");

    [Fact]
    public void BuildRoute_UnsafeCharacters_UsesGeneratorSanitization() => CommandRouteBuilder.BuildRoute("Sales / West", "Commerce.Commands.Submit/Order Command")
            .ShouldBe("/commands/Sales---West/Submit-Order-Command");

    [Theory]
    [InlineData("/")]
    [InlineData("/counter")]
    [InlineData("/domain/commerce/submit-order")]
    [InlineData("/path?q=1")]
    [InlineData("/path#frag")]
    [InlineData("/calendar/10:30")]
    [InlineData("/path?at=10:30")]
    [InlineData("/path#section:2")]
    [InlineData("/caf%C3%A9")]
    [InlineData("/search?q=1\u20442")]
    [InlineData("/path#1%E2%81%842")]
    public void IsInternalRoute_AcceptsValidInternalUrls(string url) => CommandRouteBuilder.IsInternalRoute(url).ShouldBeTrue();

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("/%2Fexternal.example")]
    [InlineData("/counter\u2044detail")]
    [InlineData("/counter\u2215detail")]
    [InlineData("/counter\uFF0Fdetail")]
    [InlineData("/counter\uFF3Cdetail")]
    [InlineData("/counter\uFE68detail")]
    [InlineData("/counter%E2%81%84detail")]
    [InlineData("/counter%EF%BC%8Fdetail?q=1")]
    [InlineData("/counter%3Fpart%EF%BC%BCdetail#section")]
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
    public void IsInternalRoute_RejectsTamperedOrAbsoluteUrls(string? url) => CommandRouteBuilder.IsInternalRoute(url).ShouldBeFalse();
}
