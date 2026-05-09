using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class CliHelpTests
{
    [Fact]
    public async Task Help_ListsInspectAndMigrateCommands()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(["--help"], output, error, CancellationToken.None);

        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("frontcomposer inspect");
        output.ToString().ShouldContain("frontcomposer migrate");
        error.ToString().ShouldBeEmpty();
    }
}
