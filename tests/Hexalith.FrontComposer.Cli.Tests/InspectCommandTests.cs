using System.Text.Json;
using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class InspectCommandTests
{
    [Fact]
    public async Task InspectJson_ReportsGeneratedFilesWithDeterministicRelativePaths()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "Acme.Shipping.ShipmentProjection.g.razor.cs",
            "namespace Acme.Shipping { }");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionActions.g.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionReducers.g.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionRegistration.g.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.CreateShipment.CommandForm.g.razor.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "FrontComposerMcpManifest.g.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        root.GetProperty("schemaVersion").GetString().ShouldBe("frontcomposer.cli.inspect.v1");
        root.GetProperty("summary").GetProperty("generatedFiles").GetInt32().ShouldBe(6);
        root.GetProperty("summary").GetProperty("forms").GetInt32().ShouldBe(1);
        root.GetProperty("summary").GetProperty("registrations").GetInt32().ShouldBe(1);
        root.GetProperty("summary").GetProperty("mcpManifestEntries").GetInt32().ShouldBe(1);

        string[] paths = root.GetProperty("generatedFiles")
            .EnumerateArray()
            .Select(x => x.GetProperty("path").GetString()!)
            .ToArray();

        paths.ShouldBe(paths.Order(StringComparer.Ordinal).ToArray());
        paths.ShouldAllBe(path => path.Contains('/', StringComparison.Ordinal));
        paths.ShouldAllBe(path => !Path.IsPathRooted(path));
        paths.ShouldContain("obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjection.g.razor.cs");
    }

    [Fact]
    public async Task InspectType_NotFound_ReturnsBoundedClosestKnownTypeNames()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Billing.InvoiceProjection.g.razor.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--type", "PackageProjection"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.GeneratedOutputUnavailable);
        error.ToString().ShouldContain("PackageProjection");
        error.ToString().ShouldContain("Acme.Shipping.ShipmentProjection");
        error.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Inspect_RequiresFrameworkWhenGeneratedOutputIsAmbiguous()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0;netstandard2.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "netstandard2.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain("--framework");
        error.ToString().ShouldContain("net10.0");
        error.ToString().ShouldContain("netstandard2.0");
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Inspect_MissingGeneratedOutput_ReturnsStableUnavailableExitCode()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteSource("Acme.App", "Projection.cs", "[Projection] public partial class ShipmentProjection { }");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.GeneratedOutputUnavailable);
        error.ToString().ShouldContain("Generated output");
        error.ToString().ShouldContain("--build");
        error.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task InspectSeverity_FiltersHfcDiagnostics()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn", "docsLink": "https://example.invalid/warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err", "docsLink": "https://example.invalid/error" }
              ]
            }
            """);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--severity", "error", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldBe(0);
        JsonElement diagnostic = document.RootElement.GetProperty("diagnostics").EnumerateArray().Single();
        diagnostic.GetProperty("id").GetString().ShouldBe("HFC1003");
    }
}
