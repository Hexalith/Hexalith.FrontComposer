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
    // P21 (2026-04-21 pass-4, C3-D2(a) ratified): lenient first-segment fallback is the documented
    // behaviour per D28 addendum — any non-/domain/ 2+-segment path resolves to its first segment.
    // These cases lock the contract against accidental tightening.
    [InlineData("/admin/users", "admin")]
    [InlineData("/help/topic", "help")]
    [InlineData("/Counter/Counter-View", "counter")] // case normalisation (ToLowerInvariant)
    [InlineData("/DOMAIN/Commerce/Submit-Order-Command", "commerce")] // /domain/ match is OrdinalIgnoreCase
    public void Parse_ReturnsExpectedBoundedContext(string uriOrPath, string? expected)
        => BoundedContextRouteParser.Parse(uriOrPath).ShouldBe(expected);
}