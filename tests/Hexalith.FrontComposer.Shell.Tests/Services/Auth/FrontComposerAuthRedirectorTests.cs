using Hexalith.FrontComposer.Shell.Services.Auth;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAuthRedirectorTests {
    [Theory]
    [InlineData("/orders/1", "/orders/1")]
    [InlineData("~/orders/1", "/orders/1")]
    [InlineData("?tab=summary", "/?tab=summary")]
    [InlineData("/orders/1#details", "/orders/1#details")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void SanitizeReturnUrl_PreservesLocalReturnPaths(string? input, string expected)
        => FrontComposerReturnUrl.Sanitize(input).ShouldBe(expected);

    [Theory]
    [InlineData("https://evil.test/callback")]
    [InlineData("//evil.test/callback")]
    [InlineData("/\\evil")]
    [InlineData("/%5cevil")]
    [InlineData("/%250d%250aHeader:%20value")]
    [InlineData("/%2f%2fevil.test")]
    public void SanitizeReturnUrl_DropsUnsafeReturnPaths(string input)
        => FrontComposerReturnUrl.Sanitize(input).ShouldBe("/");
}
