using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class ReturnPathValidatorTests {
    [Theory]
    [InlineData("/")]
    [InlineData("/orders")]
    [InlineData("/orders/123?tab=summary#items")]
    [InlineData("/files/report%20summary")]
    [InlineData("/.well-known/openid-configuration")]
    [InlineData("/var/data/report")]
    [InlineData("/orders/caf%C3%A9")]
    [InlineData("/schedule/12:30")]
    [InlineData("/orders?filter=a%2Bb")]
    public void IsSafeRelativePath_LocalRootRelativePath_ReturnsTrue(string returnPath)
        => ReturnPathValidator.IsSafeRelativePath(returnPath).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("orders")]
    [InlineData("https://evil.example/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("//evil.example/path")]
    [InlineData("/\\evil.example/path")]
    [InlineData("\\\\evil.example\\path")]
    [InlineData("\\/evil.example/path")]
    [InlineData("/%2f/evil.example/path")]
    [InlineData("/%252f%252fevil.example/path")]
    [InlineData("/%5c%5cevil.example/path")]
    [InlineData("/%255c%255cevil.example/path")]
    [InlineData("/redirect?next=https://evil.example")]
    [InlineData("/redirect?next=https%3a%2f%2fevil.example")]
    [InlineData("/orders/%0aLocation:%20//evil.example")]
    [InlineData("/orders/%C2%A0hidden")]
    [InlineData("/orders/%E2%80%AEevil")]
    [InlineData("/orders/\u202Eevil")]
    [InlineData("/orders/%E2%80%A8hidden")]
    [InlineData("/orders/%E2%80%8Bhidden")]
    [InlineData("/orders/\u200Bhidden")]
    [InlineData("/orders/%E2%81%A0hidden")]
    [InlineData("/orders/%zz")]
    [InlineData("/orders/<script>alert(1)</script>")]
    [InlineData("/orders/\"quoted\"")]
    [InlineData("/orders/{id}")]
    [InlineData("/orders/a|b")]
    [InlineData("/orders/a^b")]
    [InlineData("/orders/a`b")]
    [InlineData("/orders/%E2%80%89hidden")]
    [InlineData("/orders/%E3%80%80hidden")]
    [InlineData("/orders/\u00ADhidden")]
    [InlineData("/orders/%C2%ADhidden")]
    [InlineData("/orders/\u061Cevil")]
    [InlineData("/orders/%D8%9Cevil")]
    [InlineData("/orders/\u2066evil")]
    [InlineData("/redirect?next=https:/evil.example")]
    [InlineData("/foo//bar")]
    [InlineData("/a%2f%2fb")]
    [InlineData("/orders/%3Cscript%3E")]
    [InlineData("/orders/%22quoted%22")]
    [InlineData("/orders/%7Cbar")]
    [InlineData("/orders/%60tick")]
    [InlineData("/orders/%7Bid%7D")]
    [InlineData("/orders/a%5Eb")]
    [InlineData("/orders/\U000E0001evil")]
    [InlineData("/orders/%F3%A0%80%81evil")]
    [InlineData("/orders/\u2061hidden")]
    [InlineData("/orders/%E2%81%A1hidden")]
    public void IsSafeRelativePath_RedirectAttackClass_ReturnsFalse(string? returnPath)
        => ReturnPathValidator.IsSafeRelativePath(returnPath).ShouldBeFalse();

    [Theory]
    [InlineData("/../admin")]
    [InlineData("/..?next=/admin")]
    [InlineData("/app/../admin")]
    [InlineData("/app/..#admin")]
    [InlineData("/app/%2e%2e/admin")]
    [InlineData("/app/%2E%2E/admin")]
    [InlineData("/app/%252e%252e/admin")]
    [InlineData("/app/%25252e%25252e/admin")]
    public void IsSafeRelativePath_NonRootBaseTraversal_ReturnsFalse(string returnPath)
        => ReturnPathValidator.IsSafeRelativePath(returnPath).ShouldBeFalse();
}
