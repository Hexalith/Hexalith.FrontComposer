using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class OutputSanitizerTests {
    [Fact]
    public void Sanitize_BoundsControlCharactersAnsiAndLongValues() {
        string unsafeValue = "tenant\u001b[31m\r\n{\"token\":\"secret\"}" + new string('x', 500);

        string safe = OutputSanitizer.Sanitize(unsafeValue, 40);

        safe.ShouldNotContain("\u001b");
        safe.ShouldNotContain("\r");
        safe.ShouldNotContain("\n");
        safe.ShouldContain("\\u001B");
        safe.ShouldContain("[truncated:");
        safe.Length.ShouldBeLessThanOrEqualTo(80);
    }
}
