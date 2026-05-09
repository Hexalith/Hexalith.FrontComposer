using System.Text.Json;
using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class MigrationCommandTests
{
    [Fact]
    public async Task Migrate_DefaultsToDryRunAndDoesNotWriteSource()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource(
            "Acme.App",
            "Program.cs",
            """
            using Microsoft.Extensions.DependencyInjection;

            var services = new ServiceCollection();
            services.AddFrontComposerDebugOverlay();
            """);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement summary = document.RootElement.GetProperty("summary");
        summary.GetProperty("changed").GetInt32().ShouldBe(1);
        summary.GetProperty("unchanged").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("applied").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateApply_WritesOnlyImmediatelyPlannedSourceFilesAndIsIdempotent()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Generated.g.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();
        File.ReadAllText(source).ShouldContain("AddFrontComposerDevMode");

        using StringWriter secondOutput = new();
        using StringWriter secondError = new();
        int secondExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            secondOutput,
            secondError,
            CancellationToken.None);

        secondExitCode.ShouldBe(0);
        secondError.ToString().ShouldBeEmpty();
        using JsonDocument document = JsonDocument.Parse(secondOutput.ToString());
        document.RootElement.GetProperty("summary").GetProperty("unchanged").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Migrate_RefusesExcludedWriteTargetsAndReportsManualOnlyEntries()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay(); // HFCM9002");
        fixture.WriteSource("Acme.App", "bin/Debug/net10.0/Generated.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement summary = document.RootElement.GetProperty("summary");
        summary.GetProperty("manualOnly").GetInt32().ShouldBe(1);
        summary.GetProperty("skipped").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task Migrate_UnsupportedVersionEdgeFailsClosedBeforePlanning()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.2.0", "--to", "9.1.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain("Supported edges");
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");
        output.ToString().ShouldBeEmpty();
    }
}
