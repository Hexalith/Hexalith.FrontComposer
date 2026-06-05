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

        // AC21 (third-pass fix): JSON `generatedFiles` ordering matches the canonical load-order
        // tri-key sort (RelatedType -> Family -> RelativePath). Files without a RelatedType
        // (e.g., the MCP manifest) sort first because their RelatedType compares as the empty
        // string ordinally. Text and JSON now share this exact ordering.
        var entries = root.GetProperty("generatedFiles")
            .EnumerateArray()
            .Select(x => new {
                Path = x.GetProperty("path").GetString()!,
                Family = x.GetProperty("family").GetString()!,
                RelatedType = x.GetProperty("relatedType").GetString() ?? string.Empty,
            })
            .ToArray();

        var triKeyExpected = entries
            .OrderBy(x => x.RelatedType, StringComparer.Ordinal)
            .ThenBy(x => x.Family, StringComparer.Ordinal)
            .ThenBy(x => x.Path, StringComparer.Ordinal)
            .Select(x => x.Path)
            .ToArray();

        string[] paths = entries.Select(x => x.Path).ToArray();
        paths.ShouldBe(triKeyExpected);
        paths.ShouldAllBe(path => path.Contains('/', StringComparison.Ordinal));
        paths.ShouldAllBe(path => !Path.IsPathRooted(path));
        paths.ShouldContain("obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjection.g.razor.cs");
    }

    [Fact]
    public async Task InspectJson_ClassifiesGeneratedFamiliesAndCountsMcpManifestFiles()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string[] generatedFiles = [
            "Acme.Shipping.ShipmentProjection.g.razor.cs",
            "Acme.Shipping.ShipmentProjectionFeature.g.cs",
            "Acme.Shipping.ShipmentProjectionActions.g.cs",
            "Acme.Shipping.ShipmentProjectionReducers.g.cs",
            "Acme.Shipping.ShipmentProjectionRegistration.g.cs",
            "Acme.Shipping.CreateShipment.CommandForm.g.razor.cs",
            "Acme.Shipping.CreateShipment.CommandRenderer.g.razor.cs",
            "Acme.Shipping.CreateShipment.CommandLifecycleBridge.g.cs",
            "Acme.Shipping.CreateShipment.CommandLastUsedSubscriber.g.cs",
            "Acme.Shipping.CreateShipment.CommandPage.g.razor.cs",
            "FrontComposerMcpManifest.g.cs",
            "__FrontComposerProjectionTemplatesRegistration.g.cs",
        ];
        foreach (string fileName in generatedFiles) {
            fixture.WriteGenerated("Acme.App", "Debug", "net10.0", fileName, "");
        }

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
        root.GetProperty("summary").GetProperty("generatedFiles").GetInt32().ShouldBe(generatedFiles.Length);
        root.GetProperty("summary").GetProperty("forms").GetInt32().ShouldBe(1);
        root.GetProperty("summary").GetProperty("grids").GetInt32().ShouldBe(1);
        root.GetProperty("summary").GetProperty("registrations").GetInt32().ShouldBe(1);
        root.GetProperty("summary").GetProperty("mcpManifestEntries").GetInt32().ShouldBe(1);

        string[] families = root.GetProperty("generatedFiles")
            .EnumerateArray()
            .Select(x => x.GetProperty("family").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        families.ShouldBe([
            "CommandForm",
            "CommandLastUsedSubscriber",
            "CommandLifecycleBridge",
            "CommandPage",
            "CommandRenderer",
            "FluxorActions",
            "FluxorFeature",
            "FluxorReducers",
            "McpManifest",
            "ProjectionRazor",
            "Registration",
            "TemplateManifest",
        ]);
    }

    [Fact]
    public void InspectBuildArguments_UsePublicGeneratedOutputPathContractShape()
    {
        IReadOnlyList<string> explicitFramework = GeneratedOutputLoader.CreateBuildArguments(
            "Acme.App.csproj",
            "Release",
            "net10.0");
        explicitFramework.ShouldContain("-p:EmitCompilerGeneratedFiles=true");
        explicitFramework.ShouldContain("-p:CompilerGeneratedFilesOutputPath=obj/Release/net10.0/generated/HexalithFrontComposer");
        explicitFramework.ShouldContain("--framework");
        explicitFramework.ShouldContain("net10.0");

        IReadOnlyList<string> inferredFramework = GeneratedOutputLoader.CreateBuildArguments(
            "Acme.App.csproj",
            "Debug",
            null);
        inferredFramework.ShouldContain("-p:CompilerGeneratedFilesOutputPath=obj/Debug/$(TargetFramework)/generated/HexalithFrontComposer");
        inferredFramework.ShouldNotContain("--framework");
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

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
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
    public async Task Inspect_MissingGeneratedOutputWithoutAnnotations_StillSuggestsBuild()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.GeneratedOutputUnavailable);
        error.ToString().ShouldContain("--build");
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

    [Fact]
    public async Task InspectText_SummaryLineReportsWarningAndErrorTotals()
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
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err" }
              ]
            }
            """);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();

        // Default format is text. The summary line must surface warning/error totals at parity with
        // the JSON `summary.warnings`/`summary.errors` fields, not only the per-diagnostic lines.
        string text = output.ToString();
        text.ShouldContain("Warnings: 1");
        text.ShouldContain("Errors: 1");
    }

    [Fact]
    public async Task InspectFailFlags_UseExpectedSeverityThresholds()
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
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err" }
              ]
            }
            """);

        int warningExit = await RunInspectAsync(project, ["--fail-on-warning"]);
        int errorExit = await RunInspectAsync(project, ["--fail-on-error"]);

        warningExit.ShouldBe(ExitCodes.ActionableFindings);
        errorExit.ShouldBe(ExitCodes.ActionableFindings);
    }

    [Fact]
    public async Task InspectFailOnError_DoesNotFailForWarningOnlyDiagnostics()
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
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" }
              ]
            }
            """);

        int warningExit = await RunInspectAsync(project, ["--fail-on-warning"]);
        int errorExit = await RunInspectAsync(project, ["--fail-on-error"]);

        warningExit.ShouldBe(ExitCodes.ActionableFindings);
        errorExit.ShouldBe(ExitCodes.Success);
    }

    [Fact]
    public async Task InspectFailFlags_AreEvaluatedAfterSeverityAndTypeFiltering()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Billing.InvoiceProjection.g.razor.cs", "");
        fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Billing.InvoiceProjection", "what": "err" }
              ]
            }
            """);

        int severityFiltered = await RunInspectAsync(project, ["--severity", "warning", "--fail-on-error"]);
        int typeFiltered = await RunInspectAsync(project, ["--type", "Acme.Shipping.ShipmentProjection", "--fail-on-error"]);
        int warningFiltered = await RunInspectAsync(project, ["--type", "Acme.Shipping.ShipmentProjection", "--fail-on-warning"]);

        severityFiltered.ShouldBe(0);
        typeFiltered.ShouldBe(0);
        warningFiltered.ShouldBe(ExitCodes.ActionableFindings);
    }

    [Fact]
    public async Task InspectDiagnosticsSidecars_DefaultMissingOptionalFieldsIgnoreNonHfcAndRedactHostilePaths()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteSource("Acme.App", "Projection.cs", "namespace Acme.App;");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "CS1001", "severity": "Error", "path": "Projection.cs", "what": "ignored" },
                { "id": "HFC1002", "severity": "Warning", "path": "Projection.cs" },
                { "id": "HFC1003", "severity": "Error", "path": "file:///tmp/secret.cs", "what": "bad\u0001path" }
              ]
            }
            """);

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
        JsonElement[] diagnostics = document.RootElement.GetProperty("diagnostics").EnumerateArray().ToArray();
        diagnostics.Length.ShouldBe(2);
        diagnostics.Select(x => x.GetProperty("id").GetString()).ShouldNotContain("CS1001");

        JsonElement missingOptional = diagnostics.Single(x => x.GetProperty("id").GetString() == "HFC1002");
        missingOptional.GetProperty("path").GetString().ShouldBe("Projection.cs");
        missingOptional.GetProperty("expected").GetString().ShouldBeEmpty();
        missingOptional.GetProperty("got").GetString().ShouldBeEmpty();
        missingOptional.GetProperty("fix").GetString().ShouldBeEmpty();
        missingOptional.GetProperty("docsLink").GetString().ShouldBeEmpty();

        JsonElement hostile = diagnostics.Single(x => x.GetProperty("id").GetString() == "HFC1003");
        hostile.GetProperty("path").GetString().ShouldBe(PathUtilities.RedactedPathSentinel);
        hostile.GetProperty("what").GetString().ShouldNotBeNull().ShouldContain("\\u0001");
        output.ToString().ShouldNotContain("/tmp/secret.cs", Case.Sensitive);
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task Inspect_RejectsFrameworkPathTraversal()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "../../etc"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain("--framework");
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task Inspect_EmitsSentinelForMalformedDiagnosticsSidecars()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Broken.diagnostics.json", "{not-json");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        // P-12: Corrupted sidecars must surface a deterministic HFCM0002 sentinel rather than silently
        // dropping diagnostics. The sentinel itself is a Warning (not Error), so default exit stays 0.
        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement diagnostics = document.RootElement.GetProperty("diagnostics");
        diagnostics.GetArrayLength().ShouldBe(1);
        diagnostics[0].GetProperty("id").GetString().ShouldBe("HFCM0002");
        diagnostics[0].GetProperty("severity").GetString().ShouldBe("Warning");
    }

    [Fact]
    public async Task Inspect_RejectsProjectDirectoryArgument()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", Path.GetDirectoryName(project)!],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain(".csproj");
        output.ToString().ShouldBeEmpty();
    }

    private static async Task<int> RunInspectAsync(string project, string[] additionalArgs)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        string[] args = [
            "inspect",
            "--project",
            project,
            "--configuration",
            "Debug",
            "--framework",
            "net10.0",
            .. additionalArgs,
        ];

        int exitCode = await CliApplication.RunAsync(args, output, error, CancellationToken.None).ConfigureAwait(false);
        error.ToString().ShouldBeEmpty();
        return exitCode;
    }
}
