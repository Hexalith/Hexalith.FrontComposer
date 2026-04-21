using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

public sealed class BoundedContextRouteParserTests
{
    [Theory]
    [InlineData("/", null)]
    [InlineData("https://localhost/", null)]
    [InlineData("/home", null)]
    [InlineData("/settings", null)]
    [InlineData("/counter/counter-view", "counter")]
    [InlineData("https://localhost/counter/counter-view?tab=1#recent", "counter")]
    [InlineData("/domain/commerce/submit-order-command", "commerce")]
    [InlineData("https://localhost/domain/commerce/submit-order-command?from=palette", "commerce")]
    [InlineData("/domain/commerce", null)]
    public void Parse_ReturnsExpectedBoundedContext(string uriOrPath, string? expected)
        => BoundedContextRouteParser.Parse(uriOrPath).ShouldBe(expected);
}