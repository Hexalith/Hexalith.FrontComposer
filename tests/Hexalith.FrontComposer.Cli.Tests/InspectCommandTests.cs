using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class InspectCommandTests {
    [Fact]
    public async Task InspectJson_ReportsGeneratedFilesWithDeterministicRelativePaths() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "Acme.Shipping.ShipmentProjection.g.razor.cs",
            "namespace Acme.Shipping { }");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionActions.g.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionReducers.g.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjectionRegistration.g.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.CreateShipment.CommandForm.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "FrontComposerMcpManifest.g.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();

        using var document = JsonDocument.Parse(output.ToString());
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

        string[] triKeyExpected = entries
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
    public async Task InspectJson_ClassifiesGeneratedFamiliesAndCountsMcpManifestFiles() {
        using var fixture = CliFixture.Create();
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
            _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", fileName, "");
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

        using var document = JsonDocument.Parse(output.ToString());
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
    public async Task InspectText_SummaryCountsMatchJsonForSharedContractFields() {
        using var fixture = CliFixture.Create();
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
            _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", fileName, "");
        }

        _ = fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Shipping.CreateShipment", "what": "err" }
              ]
            }
            """);

        (int jsonExitCode, string jsonOutput, string jsonError) = await RunInspectCaptureAsync(project, ["--format", "json"]);
        (int textExitCode, string text, string textError) = await RunInspectCaptureAsync(project, []);

        jsonExitCode.ShouldBe(0);
        jsonError.ShouldBeEmpty();
        textExitCode.ShouldBe(0);
        textError.ShouldBeEmpty();

        using var document = JsonDocument.Parse(jsonOutput);
        JsonElement summary = document.RootElement.GetProperty("summary");
        int generatedFileCount = summary.GetProperty("generatedFiles").GetInt32();
        int forms = summary.GetProperty("forms").GetInt32();
        int grids = summary.GetProperty("grids").GetInt32();
        int registrations = summary.GetProperty("registrations").GetInt32();
        int mcpManifestEntries = summary.GetProperty("mcpManifestEntries").GetInt32();
        int warnings = summary.GetProperty("warnings").GetInt32();
        int errors = summary.GetProperty("errors").GetInt32();

        text.ShouldContain($"Generated files: {generatedFileCount}");
        text.ShouldContain($"Forms: {forms}; Grids: {grids}; Registrations: {registrations}; MCP manifests: {mcpManifestEntries}; Warnings: {warnings}; Errors: {errors}");
        text.ShouldContain("- ProjectionRazor: obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjection.g.razor.cs");
        text.ShouldContain("- CommandForm: obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.CreateShipment.CommandForm.g.razor.cs");
        text.ShouldContain("- Registration: obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjectionRegistration.g.cs");
        text.ShouldContain("- McpManifest: obj/Debug/net10.0/generated/HexalithFrontComposer/FrontComposerMcpManifest.g.cs");
        text.ShouldContain("! HFC1002 Warning");
        text.ShouldContain("! HFC1003 Error");
        text.ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public void InspectBuildArguments_UsePublicGeneratedOutputPathContractShape() {
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
    public async Task InspectType_NotFound_ReturnsBoundedClosestKnownTypeNames() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Billing.InvoiceProjection.g.razor.cs", "");

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
    public async Task Inspect_RequiresFrameworkWhenGeneratedOutputIsAmbiguous() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0;netstandard2.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "netstandard2.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");

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
    public async Task Inspect_MissingGeneratedOutput_ReturnsStableUnavailableExitCode() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Projection.cs", "[Projection] public partial class ShipmentProjection { }");

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
    public async Task Inspect_MissingGeneratedOutputWithoutAnnotations_StillSuggestsBuild() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");

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
    public async Task InspectSeverity_UsesThresholdSemanticsInJsonOutput() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "CS1001", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "ignored" },
                { "id": "HFC1001", "severity": "Hidden", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "hidden" },
                { "id": "HFC1002", "severity": "Info", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "info" },
                { "id": "HFC1003", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1004", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err" }
              ]
            }
            """);

        (await ReadDiagnosticIdsAsync("hidden")).ShouldBe(["HFC1001", "HFC1002", "HFC1003", "HFC1004"], ignoreOrder: false);
        (await ReadDiagnosticIdsAsync("info")).ShouldBe(["HFC1002", "HFC1003", "HFC1004"], ignoreOrder: false);
        (await ReadDiagnosticIdsAsync("warning")).ShouldBe(["HFC1003", "HFC1004"], ignoreOrder: false);
        (await ReadDiagnosticIdsAsync("error")).ShouldBe(["HFC1004"], ignoreOrder: false);

        async Task<string[]> ReadDiagnosticIdsAsync(string severity) {
            using StringWriter output = new();
            using StringWriter error = new();
            int exitCode = await CliApplication.RunAsync(
                ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--severity", severity, "--format", "json"],
                output,
                error,
                CancellationToken.None).ConfigureAwait(false);

            exitCode.ShouldBe(0);
            error.ToString().ShouldBeEmpty();

            using var document = JsonDocument.Parse(output.ToString());
            return document.RootElement.GetProperty("diagnostics")
                .EnumerateArray()
                .Select(x => x.GetProperty("id").GetString()!)
                .ToArray();
        }
    }

    [Fact]
    public async Task InspectSeverity_Hidden_IncludesNonCanonicalSeverities() {
        // AC2: `hidden` must include all diagnostics. A malformed sidecar can carry a severity
        // outside Hidden/Info/Warning/Error; such an entry is shown unfiltered, so `--severity hidden`
        // must remain a superset of the unfiltered output while strict levels still exclude it.
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "HFC1001", "severity": "Critical", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "noncanonical" },
                { "id": "HFC1002", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err" }
              ]
            }
            """);

        (await ReadDiagnosticIdsAsync("hidden")).ShouldBe(["HFC1001", "HFC1002"], ignoreOrder: false);
        (await ReadDiagnosticIdsAsync("error")).ShouldBe(["HFC1002"], ignoreOrder: false);

        async Task<string[]> ReadDiagnosticIdsAsync(string severity) {
            using StringWriter output = new();
            using StringWriter error = new();
            int exitCode = await CliApplication.RunAsync(
                ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--severity", severity, "--format", "json"],
                output,
                error,
                CancellationToken.None).ConfigureAwait(false);

            exitCode.ShouldBe(0);
            error.ToString().ShouldBeEmpty();

            using var document = JsonDocument.Parse(output.ToString());
            return document.RootElement.GetProperty("diagnostics")
                .EnumerateArray()
                .Select(x => x.GetProperty("id").GetString()!)
                .ToArray();
        }
    }

    [Fact]
    public async Task InspectSeverity_UsesThresholdSemanticsInTextSummary() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
            "Acme.App",
            "Debug",
            "net10.0",
            "FrontComposer.diagnostics.json",
            """
            {
              "diagnostics": [
                { "id": "HFC1001", "severity": "Info", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "info" },
                { "id": "HFC1002", "severity": "Warning", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "warn" },
                { "id": "HFC1003", "severity": "Error", "relatedType": "Acme.Shipping.ShipmentProjection", "what": "err" }
              ]
            }
            """);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--severity", "warning"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        error.ToString().ShouldBeEmpty();
        string text = output.ToString();
        text.ShouldNotContain("HFC1001");
        text.ShouldContain("HFC1002 Warning");
        text.ShouldContain("HFC1003 Error");
        text.ShouldContain("Warnings: 1");
        text.ShouldContain("Errors: 1");
    }

    [Fact]
    public async Task InspectSeverity_InvalidValue_ReturnsInvalidArguments() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["inspect", "--project", project, "--configuration", "Debug", "--framework", "net10.0", "--severity", "verbose"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain("--severity");
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task InspectText_SummaryLineReportsWarningAndErrorTotals() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
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
    public async Task InspectFailFlags_UseExpectedSeverityThresholds() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
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
    public async Task InspectFailOnError_DoesNotFailForWarningOnlyDiagnostics() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
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
    public async Task InspectFailFlags_AreEvaluatedAfterSeverityAndTypeFiltering() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Billing.InvoiceProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
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

        severityFiltered.ShouldBe(ExitCodes.ActionableFindings);
        typeFiltered.ShouldBe(0);
        warningFiltered.ShouldBe(ExitCodes.ActionableFindings);
    }

    [Fact]
    public async Task InspectText_FilteringAffectsRowsBeforeFailFlags() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Billing.InvoiceProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "FrontComposerMcpManifest.g.cs", "");
        _ = fixture.WriteGenerated(
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

        (int typeExitCode, string typeText, string typeError) = await RunInspectCaptureAsync(
            project,
            ["--type", "Acme.Shipping.ShipmentProjection", "--fail-on-error"]);
        (int warningExitCode, string warningText, string warningError) = await RunInspectCaptureAsync(
            project,
            ["--type", "Acme.Shipping.ShipmentProjection", "--fail-on-warning"]);
        (int emptySeverityExitCode, string emptySeverityText, string emptySeverityError) = await RunInspectCaptureAsync(
            project,
            ["--type", "Acme.Shipping.ShipmentProjection", "--severity", "error", "--fail-on-warning"]);

        typeExitCode.ShouldBe(0);
        typeError.ShouldBeEmpty();
        typeText.ShouldContain("Acme.Shipping.ShipmentProjection.g.razor.cs");
        typeText.ShouldContain("FrontComposerMcpManifest.g.cs");
        typeText.ShouldContain("! HFC1002 Warning");
        typeText.ShouldContain("Warnings: 1");
        typeText.ShouldContain("Errors: 0");
        typeText.ShouldNotContain("Acme.Billing.InvoiceProjection");
        typeText.ShouldNotContain("HFC1003");

        warningExitCode.ShouldBe(ExitCodes.ActionableFindings);
        warningError.ShouldBeEmpty();
        warningText.ShouldContain("! HFC1002 Warning");

        emptySeverityExitCode.ShouldBe(0);
        emptySeverityError.ShouldBeEmpty();
        emptySeverityText.ShouldContain("Warnings: 0");
        emptySeverityText.ShouldContain("Errors: 0");
        emptySeverityText.ShouldNotContain("HFC1002");
        emptySeverityText.ShouldNotContain("HFC1003");
    }

    [Fact]
    public async Task InspectDiagnosticsSidecars_DefaultMissingOptionalFieldsIgnoreNonHfcAndRedactHostilePaths() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Projection.cs", "namespace Acme.App;");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated(
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

        using var document = JsonDocument.Parse(output.ToString());
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
    public async Task Inspect_RejectsFrameworkPathTraversal() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");

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
    public async Task Inspect_EmitsSentinelForMalformedDiagnosticsSidecars() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Broken.diagnostics.json", "{not-json");

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
        using var document = JsonDocument.Parse(output.ToString());
        JsonElement diagnostics = document.RootElement.GetProperty("diagnostics");
        diagnostics.GetArrayLength().ShouldBe(1);
        diagnostics[0].GetProperty("id").GetString().ShouldBe("HFCM0002");
        diagnostics[0].GetProperty("severity").GetString().ShouldBe("Warning");
    }

    [Fact]
    public async Task Inspect_RejectsProjectDirectoryArgument() {
        using var fixture = CliFixture.Create();
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

    private static async Task<int> RunInspectAsync(string project, string[] additionalArgs) {
        (int exitCode, _, string error) = await RunInspectCaptureAsync(project, additionalArgs).ConfigureAwait(false);
        error.ShouldBeEmpty();
        return exitCode;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunInspectCaptureAsync(string project, string[] additionalArgs) {
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
        return (exitCode, output.ToString(), error.ToString());
    }
}
