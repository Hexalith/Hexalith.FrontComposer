using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class MigrationCommandTests {
    [Fact]
    public void ProjectSelection_RejectsUnsupportedExplicitProjectFormats() {
        using var fixture = CliFixture.Create();
        string fsProject = Path.Combine(fixture.Root, "Acme.App", "Acme.App.fsproj");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(fsProject)!);
        File.WriteAllText(fsProject, "<Project />");

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--project", fsProject]),
            fixture.Root);

        selection.Success.ShouldBeFalse();
        selection.Error.ShouldContain(".fsproj is not supported");
        selection.Error.ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public void ProjectSelection_RejectsUnsupportedSolutionFormats() {
        using var fixture = CliFixture.Create();
        string slnx = Path.Combine(fixture.Root, "Acme.slnx");
        File.WriteAllText(slnx, "<Solution />");

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--solution", slnx]),
            fixture.Root);

        selection.Success.ShouldBeFalse();
        selection.Error.ShouldContain(".slnx is not supported");
        selection.Error.ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public void ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string solution = Path.Combine(fixture.Root, "Acme.sln");
        File.WriteAllText(
            solution,
            """"
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Acme ""App""", "Acme.App\Acme.App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            """");

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--solution", solution]),
            fixture.Root);

        selection.Success.ShouldBeTrue(selection.Error);
        selection.ProjectPath.ShouldBe(PathUtilities.Canonical(project));
    }

    [Fact]
    public void ProjectSelection_RejectsUnsupportedSolutionProjectTypes() {
        using var fixture = CliFixture.Create();
        string solution = Path.Combine(fixture.Root, "Acme.sln");
        File.WriteAllText(
            solution,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{F2A71F9B-5D33-465A-A702-920D77279786}") = "Acme.FSharp", "Acme.FSharp\Acme.FSharp.fsproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            """);

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--solution", solution]),
            fixture.Root);

        selection.Success.ShouldBeFalse();
        selection.Error.ShouldContain(".fsproj is not supported");
    }

    [Fact]
    public void ProjectSelection_RejectsNonFSharpUnsupportedSolutionProjectTypes() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string solution = Path.Combine(fixture.Root, "Acme.sln");
        File.WriteAllText(
            solution,
            $$"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Acme.App", "Acme.App\Acme.App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "Acme.Legacy", "Acme.Legacy\Acme.Legacy.vbproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--solution", solution]),
            fixture.Root);

        selection.Success.ShouldBeFalse();
        selection.Error.ShouldContain(".vbproj is not supported");
        selection.ProjectPath.ShouldBeNull();
        File.Exists(project).ShouldBeTrue();
    }

    [Fact]
    public void ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string solutionDirectory = Path.Combine(fixture.Root, "solution");
        _ = Directory.CreateDirectory(solutionDirectory);
        string solution = Path.Combine(solutionDirectory, "Acme.sln");
        File.WriteAllText(
            solution,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Acme.App", "..\Acme.App\Acme.App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            """);

        var selection = ProjectSelection.Resolve(
            CommandOptions.Parse(["--solution", solution]),
            fixture.Root);

        selection.Success.ShouldBeFalse();
        selection.Error.ShouldContain("outside the solution directory");
        selection.ProjectPath.ShouldBeNull();
        File.Exists(project).ShouldBeTrue();
    }

    [Fact]
    public async Task Migrate_DefaultsToDryRunAndDoesNotWriteSource() {
        using var fixture = CliFixture.Create();
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

        exitCode.ShouldBe(0, output + error.ToString());
        error.ToString().ShouldBeEmpty();
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");

        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("frontcomposer.cli.migrate.v1");
        document.RootElement.GetProperty("applied").GetBoolean().ShouldBeFalse();
        JsonElement summary = document.RootElement.GetProperty("summary");
        summary.GetProperty("changed").GetInt32().ShouldBe(1);
        summary.GetProperty("unchanged").GetInt32().ShouldBe(0);
        JsonElement entry = document.RootElement.GetProperty("entries").EnumerateArray().Single();
        entry.GetProperty("diagnosticId").GetString().ShouldBe("HFCM9001");
        entry.GetProperty("kind").GetString().ShouldBe("safe-fix");
        entry.GetProperty("path").GetString().ShouldBe("Program.cs");
        entry.GetProperty("docsLink").GetString().ShouldBe("docs/migrations/9.1-to-9.2.md");
        entry.GetProperty("diff").GetString()!.ShouldContain("AddFrontComposerDevMode");
        entry.GetProperty("formattingApplied").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void MigrationText_SummaryCountsMatchJsonForSharedContractFields() {
        MigrationEntry[] entries = [
            new("HFCM9001", "safe-fix", "Changed.cs", "changed", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", "diff"),
            new("HFCM0001", "unchanged", "Unchanged.cs", "unchanged", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", null),
            new("HFCM0000", "skipped", "Skipped.cs", "skipped", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", null),
            new("HFCM0004", "failed", "Failed.cs", "failed", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", null),
            new("HFCM9002", "manual-only", "Manual.cs", "manual", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", null),
            new("HFCM0004", "conflict", "Conflict.cs", "conflict", "expected", "got", "fix", "docs/migrations/9.1-to-9.2.md", null),
        ];
        MigrationResult result = new(false, entries, MigrationSummary.From(entries));

        string json = JsonSerializer.Serialize(MigrationJson.From(result), JsonOptions.Stable);
        using StringWriter writer = new();
        MigrationCommand.RenderText(result, writer);
        string text = writer.ToString();

        using var document = JsonDocument.Parse(json);
        JsonElement summary = document.RootElement.GetProperty("summary");
        int changed = summary.GetProperty("changed").GetInt32();
        int unchanged = summary.GetProperty("unchanged").GetInt32();
        int skipped = summary.GetProperty("skipped").GetInt32();
        int failed = summary.GetProperty("failed").GetInt32();
        int manualOnly = summary.GetProperty("manualOnly").GetInt32();
        int conflicts = summary.GetProperty("conflicts").GetInt32();

        text.ShouldContain($"Changed: {changed}; Unchanged: {unchanged}; Skipped: {skipped}; Failed: {failed}; Manual-only: {manualOnly}; Conflicts: {conflicts}");
        text.ShouldContain("- safe-fix HFCM9001 Changed.cs: changed");
        text.ShouldContain("- unchanged HFCM0001 Unchanged.cs: unchanged");
        text.ShouldContain("- skipped HFCM0000 Skipped.cs: skipped");
        text.ShouldContain("- failed HFCM0004 Failed.cs: failed");
        text.ShouldContain("- manual-only HFCM9002 Manual.cs: manual");
        text.ShouldContain("- conflict HFCM0004 Conflict.cs: conflict");
    }

    [Fact]
    public async Task MigrateApply_WritesOnlyImmediatelyPlannedSourceFilesAndIsIdempotent() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");
        _ = fixture.WriteGenerated("Acme.App", "Debug", "net10.0", "Acme.Generated.g.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        error.ToString().ShouldBeEmpty();
        File.ReadAllText(source).ShouldContain("AddFrontComposerDevMode");
        using (var firstDocument = JsonDocument.Parse(output.ToString())) {
            firstDocument.RootElement.GetProperty("applied").GetBoolean().ShouldBeTrue();
        }

        using StringWriter secondOutput = new();
        using StringWriter secondError = new();
        int secondExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            secondOutput,
            secondError,
            CancellationToken.None);

        secondExitCode.ShouldBe(0);
        secondError.ToString().ShouldBeEmpty();
        using var document = JsonDocument.Parse(secondOutput.ToString());
        document.RootElement.GetProperty("summary").GetProperty("unchanged").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Migrate_RefusesExcludedWriteTargetsAndReportsManualOnlyEntries() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        File.WriteAllText(
            project,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="Program.cs;bin\Debug\net10.0\Generated.cs" />
              </ItemGroup>
            </Project>
            """);
        _ = fixture.WriteSource(
            "Acme.App",
            "Program.cs",
            """
            services.AddFrontComposerDebugOverlay();
            """);
        _ = fixture.WriteGeneratedDiagnosticSidecar(
            "Acme.App",
            "Debug",
            "net10.0",
            "frontcomposer.migration.diagnostics.json",
            """
            {
              "diagnostics": [
                {
                  "id": "HFCM9002",
                  "severity": "Warning",
                  "path": "Program.cs",
                  "what": "Custom FrontComposer migration requires manual review"
                }
              ]
            }
            """);
        _ = fixture.WriteSource("Acme.App", "bin/Debug/net10.0/Generated.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        error.ToString().ShouldBeEmpty();

        using var document = JsonDocument.Parse(output.ToString());
        JsonElement summary = document.RootElement.GetProperty("summary");
        summary.GetProperty("manualOnly").GetInt32().ShouldBe(1);
        summary.GetProperty("skipped").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Theory]
    [InlineData("C:Program.cs")]
    [InlineData("../Program.cs")]
    [InlineData("file:///Program.cs")]
    public async Task Migrate_SidecarHostilePathsSurfaceManualOnlySentinel(string sidecarPath) {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");
        _ = fixture.WriteGeneratedDiagnosticSidecar(
            "Acme.App",
            "Debug",
            "net10.0",
            "frontcomposer.migration.diagnostics.json",
            $$"""
            {
              "diagnostics": [
                {
                  "id": "HFCM9002",
                  "severity": "Warning",
                  "path": "{{sidecarPath}}",
                  "what": "Unsafe sidecar path should not be trusted"
                }
              ]
            }
            """);

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        using var document = JsonDocument.Parse(output.ToString());
        JsonElement entry = document.RootElement.GetProperty("entries").EnumerateArray().Single();
        entry.GetProperty("kind").GetString().ShouldBe("manual-only");
        entry.GetProperty("path").GetString().ShouldStartWith("__sidecar__/");
        output.ToString().ShouldNotContain(sidecarPath, Case.Sensitive);
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task SourceFile_ReadAsyncRejectsExcessiveFileSizeBeforeDecoding() {
        using var fixture = CliFixture.Create();
        string largeSource = Path.Combine(fixture.Root, "TooLarge.cs");
        await using (FileStream stream = File.Create(largeSource)) {
            stream.SetLength(SourceFile.MaxSupportedBytes + 1);
        }

        IOException exception = await Should.ThrowAsync<IOException>(
            () => SourceFile.ReadAsync(largeSource, CancellationToken.None));

        exception.Message.ShouldContain("exceeds");
    }

    [Fact]
    public async Task SourceFile_ReadAsyncRejectsInvalidUtf8() {
        using var fixture = CliFixture.Create();
        string source = Path.Combine(fixture.Root, "InvalidUtf8.cs");
        await File.WriteAllBytesAsync(source, [0x63, 0x6C, 0x61, 0x73, 0x73, 0x20, 0xFF], CancellationToken.None);

        _ = await Should.ThrowAsync<DecoderFallbackException>(
            () => SourceFile.ReadAsync(source, CancellationToken.None));
    }

    [Fact]
    public async Task Migrate_DoesNotTreatDiagnosticIdInsideCommentAsManualOnly() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay(); // HFCM9002");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("manualOnly").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
    }

    [Fact]
    public void Hfcm9002Docs_KeepSyntheticOnlyBoundaryUnlessDecisionApprovesProductionEmission() {
        DirectoryInfo root = ProjectRoot();
        string decision = File.ReadAllText(
            Path.Combine(root.FullName, "_bmad-output", "contracts", "hfcm9002-production-emission-decision-2026-07-05.md"),
            Encoding.UTF8);

        decision.ShouldContain("Decision: production emission not approved");
        decision.ShouldContain("Owners: Architect + Product Owner");
        decision.ShouldContain("Date: 2026-07-05");
        decision.ShouldContain("Reviewed source documents");

        bool productionEmissionApproved = Regex.IsMatch(
            decision,
            @"(?im)^Decision:\s+production emission approved\s*$");
        productionEmissionApproved.ShouldBeFalse("Story 10.4 treats absent explicit Product + Architecture approval as not approved.");

        string cliReadme = ReadProjectFile(root, "src/Hexalith.FrontComposer.Cli/README.md");
        string migrateContract = ReadProjectFile(root, "_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md");
        cliReadme.ShouldContain("adopter builds do not yet produce production");
        cliReadme.ShouldContain("not a promise that normal builds");
        migrateContract.ShouldContain("There is no");
        migrateContract.ShouldContain("production SourceTools `HFCM9002` sidecar emitter");
        migrateContract.ShouldContain("No new production SourceTools `HFCM9002` sidecar emitter");

        foreach (string relativePath in new[] {
            "src/Hexalith.FrontComposer.Cli/README.md",
            "_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md",
            "_bmad-output/project-docs/api-contracts.md",
            "docs/migrations/9.1-to-9.2.md",
            "docs/diagnostics/migration-findings.json",
        }) {
            string content = ReadProjectFile(root, relativePath);
            content.ShouldNotMatch(
                @"(?is)\badopter builds\s+(?:now\s+)?(?:emit|generate|produce)\b.{0,160}\bHFCM9002\b",
                $"Do not let {relativePath} promise normal adopter-build HFCM9002 sidecars while production emission is not approved.");
            content.ShouldNotMatch(
                @"(?is)\bnormal builds\s+(?:now\s+)?(?:emit|generate|produce)\b.{0,160}\bHFCM9002\b",
                $"Do not let {relativePath} promise normal-build HFCM9002 sidecars while production emission is not approved.");
        }
    }

    [Fact]
    public async Task Migrate_DoesNotTreatNameofObsoleteApiAsSafeFix() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Program.cs", "var api = nameof(AddFrontComposerDebugOverlay);");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("summary").GetProperty("unchanged").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task MigrateApply_DetectsContentDriftBeforeWriting() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");
        MigrationEdge edge = MigrationCatalog.Resolve("9.1.0", "9.2.0")!;
        MigrationPlan plan = await MigrationPlanner.PlanAsync(project, edge, CancellationToken.None);

        File.WriteAllText(source, "services.AddFrontComposerDebugOverlay();\n// changed");

        MigrationResult result = await MigrationApplier.ApplyAsync(plan, CancellationToken.None);

        result.Summary.Failed.ShouldBe(1);
        result.Summary.Changed.ShouldBe(0);
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");
    }

    [Fact]
    public async Task Migrate_RefusesExplicitSubmoduleDocuments() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        File.WriteAllText(
            Path.Combine(fixture.Root, "Acme.App", ".gitmodules"),
            """
            [submodule "vendor/lib"]
              path = "vendor/lib"
              url = https://example.invalid/lib.git
            """);
        File.WriteAllText(
            project,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="vendor\lib\Code.cs" />
              </ItemGroup>
            </Project>
            """);
        string submoduleSource = fixture.WriteSource("Acme.App", "vendor/lib/Code.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
        File.ReadAllText(submoduleSource).ShouldContain("AddFrontComposerDebugOverlay");
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task Migrate_ParsesSingleQuotedGitmodulesSubmodulePaths() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        File.WriteAllText(
            Path.Combine(fixture.Root, "Acme.App", ".gitmodules"),
            """
            [submodule "vendor/lib"]
              path = 'vendor/lib'
              url = https://example.invalid/lib.git
            """);
        File.WriteAllText(
            project,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="vendor\lib\Code.cs" />
              </ItemGroup>
            </Project>
            """);
        string submoduleSource = fixture.WriteSource("Acme.App", "vendor/lib/Code.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        File.ReadAllText(submoduleSource).ShouldContain("AddFrontComposerDebugOverlay");
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData("obj/Debug/net10.0/Generated.cs")]
    [InlineData(".git/hooks/Generated.cs")]
    [InlineData("packages/cache/Generated.cs")]
    [InlineData(".nuget/packages/Generated.cs")]
    [InlineData("nupkgs/Generated.cs")]
    [InlineData("src/generated/Generated.cs")]
    public void WriteSafetyPolicy_RefusesExcludedSegments(string relativePath) {
        ArgumentNullException.ThrowIfNull(relativePath);

        using var fixture = CliFixture.Create();
        string projectDirectory = Path.Combine(fixture.Root, "Acme.App");
        string fullPath = Path.Combine(projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "services.AddFrontComposerDebugOverlay();");

        bool allowed = WriteSafetyPolicy.IsAllowed(projectDirectory, fullPath, []);

        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task Migrate_LargeFixtureUsesProjectDocumentsAndStaysWithinCiBudget() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        for (int i = 0; i < 240; i++) {
            _ = fixture.WriteSource("Acme.App", $"Features/F{i:000}.cs", "namespace Acme.App;");
        }

        _ = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        var stopwatch = Stopwatch.StartNew();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--dry-run", "--format", "json"],
            output,
            error,
            CancellationToken.None);
        stopwatch.Stop();

        exitCode.ShouldBe(0, output + error.ToString());
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30));
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("entries").EnumerateArray()
            .Select(x => x.GetProperty("path").GetString())
            .ShouldBe(["Program.cs"]);
    }

    [Fact]
    public async Task Migrate_UnsupportedVersionEdgeFailsClosedBeforePlanning() {
        using var fixture = CliFixture.Create();
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

    [Fact]
    public async Task Migrate_InvalidFormatAndConflictingApplyFlagsFailBeforeWriting() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter invalidFormatOutput = new();
        using StringWriter invalidFormatError = new();
        int invalidFormatExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "xml", "--apply"],
            invalidFormatOutput,
            invalidFormatError,
            CancellationToken.None);

        invalidFormatExitCode.ShouldBe(ExitCodes.InvalidArguments);
        invalidFormatOutput.ToString().ShouldBeEmpty();
        invalidFormatError.ToString().ShouldContain("--format");
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");

        using StringWriter conflictingFlagsOutput = new();
        using StringWriter conflictingFlagsError = new();
        int conflictingFlagsExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--dry-run", "--apply"],
            conflictingFlagsOutput,
            conflictingFlagsError,
            CancellationToken.None);

        conflictingFlagsExitCode.ShouldBe(ExitCodes.InvalidArguments);
        conflictingFlagsOutput.ToString().ShouldBeEmpty();
        conflictingFlagsError.ToString().ShouldContain("Choose either");
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");
    }

    [Fact]
    public async Task Migrate_FailOnFindingsReturnsOneOnlyForActionableFindings() {
        using var changedFixture = CliFixture.Create();
        string changedProject = changedFixture.WriteProject("Acme.App", "net10.0");
        _ = changedFixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter changedOutput = new();
        using StringWriter changedError = new();
        int changedExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", changedProject, "--from", "9.1.0", "--to", "9.2.0", "--fail-on-findings", "--format", "json"],
            changedOutput,
            changedError,
            CancellationToken.None);

        changedExitCode.ShouldBe(ExitCodes.ActionableFindings, changedOutput + changedError.ToString());

        using var manualOnlyFixture = CliFixture.Create();
        string manualOnlyProject = manualOnlyFixture.WriteProject("Acme.App", "net10.0");
        _ = manualOnlyFixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");
        _ = manualOnlyFixture.WriteGeneratedDiagnosticSidecar(
            "Acme.App",
            "Debug",
            "net10.0",
            "frontcomposer.migration.diagnostics.json",
            """
            {
              "diagnostics": [
                {
                  "id": "HFCM9002",
                  "severity": "Warning",
                  "path": "Program.cs",
                  "what": "Custom FrontComposer migration requires manual review"
                }
              ]
            }
            """);

        using StringWriter manualOnlyOutput = new();
        using StringWriter manualOnlyError = new();
        int manualOnlyExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", manualOnlyProject, "--from", "9.1.0", "--to", "9.2.0", "--fail-on-findings", "--format", "json"],
            manualOnlyOutput,
            manualOnlyError,
            CancellationToken.None);

        manualOnlyExitCode.ShouldBe(ExitCodes.ActionableFindings, manualOnlyOutput + manualOnlyError.ToString());
        using (var manualOnlyDocument = JsonDocument.Parse(manualOnlyOutput.ToString())) {
            manualOnlyDocument.RootElement.GetProperty("summary").GetProperty("manualOnly").GetInt32().ShouldBe(1);
        }

        using var unchangedFixture = CliFixture.Create();
        string unchangedProject = unchangedFixture.WriteProject("Acme.App", "net10.0");
        _ = unchangedFixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");

        using StringWriter unchangedOutput = new();
        using StringWriter unchangedError = new();
        int unchangedExitCode = await CliApplication.RunAsync(
            ["migrate", "--project", unchangedProject, "--from", "9.1.0", "--to", "9.2.0", "--fail-on-findings", "--format", "json"],
            unchangedOutput,
            unchangedError,
            CancellationToken.None);

        unchangedExitCode.ShouldBe(ExitCodes.Success, unchangedOutput + unchangedError.ToString());
    }

    [Fact]
    public async Task MigrateText_FailOnFindingsMatchesActionableSummaryKinds() {
        using var changedFixture = CliFixture.Create();
        string changedProject = changedFixture.WriteProject("Acme.App", "net10.0");
        _ = changedFixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        (int changedExitCode, string changedOutput, string changedError) = await RunMigrateCaptureAsync(
            changedProject,
            ["--fail-on-findings"]);

        changedExitCode.ShouldBe(ExitCodes.ActionableFindings, changedOutput + changedError);
        changedError.ShouldBeEmpty();
        changedOutput.ShouldContain("Migration dry-run completed.");
        changedOutput.ShouldContain("Changed: 1");
        changedOutput.ShouldContain("Manual-only: 0");
        changedOutput.ShouldContain("Conflicts: 0");

        using var manualOnlyFixture = CliFixture.Create();
        string manualOnlyProject = manualOnlyFixture.WriteProject("Acme.App", "net10.0");
        _ = manualOnlyFixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");
        _ = manualOnlyFixture.WriteGeneratedDiagnosticSidecar(
            "Acme.App",
            "Debug",
            "net10.0",
            "frontcomposer.migration.diagnostics.json",
            """
            {
              "diagnostics": [
                {
                  "id": "HFCM9002",
                  "severity": "Warning",
                  "path": "Program.cs",
                  "what": "Custom FrontComposer migration requires manual review"
                }
              ]
            }
            """);

        (int manualOnlyExitCode, string manualOnlyOutput, string manualOnlyError) = await RunMigrateCaptureAsync(
            manualOnlyProject,
            ["--fail-on-findings"]);

        manualOnlyExitCode.ShouldBe(ExitCodes.ActionableFindings, manualOnlyOutput + manualOnlyError);
        manualOnlyError.ShouldBeEmpty();
        manualOnlyOutput.ShouldContain("Changed: 0");
        manualOnlyOutput.ShouldContain("Manual-only: 1");
        manualOnlyOutput.ShouldContain("- manual-only HFCM9002 Program.cs:");
        manualOnlyOutput.ShouldNotContain(manualOnlyFixture.Root, Case.Sensitive);

        using var unchangedFixture = CliFixture.Create();
        string unchangedProject = unchangedFixture.WriteProject("Acme.App", "net10.0");
        _ = unchangedFixture.WriteSource("Acme.App", "Program.cs", "namespace Acme.App;");

        (int unchangedExitCode, string unchangedOutput, string unchangedError) = await RunMigrateCaptureAsync(
            unchangedProject,
            ["--fail-on-findings"]);

        unchangedExitCode.ShouldBe(ExitCodes.Success, unchangedOutput + unchangedError);
        unchangedError.ShouldBeEmpty();
        unchangedOutput.ShouldContain("Changed: 0");
        unchangedOutput.ShouldContain("Unchanged: 1");
        unchangedOutput.ShouldContain("Manual-only: 0");
        unchangedOutput.ShouldContain("Conflicts: 0");
    }

    [Fact]
    public async Task Migrate_ProjectWithTopLevelImportWarnsAboutUnevaluatedMsBuildImports() {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        File.WriteAllText(
            project,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <Import Project="SharedCompileItems.props" />
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="Program.cs" />
              </ItemGroup>
            </Project>
            """);
        _ = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--dry-run", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        error.ToString().ShouldContain("<Import>");
        error.ToString().ShouldContain("not evaluated");
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData("9.2.0", "9.1.0")]
    [InlineData("9.0.0", "9.2.0")]
    [InlineData("9.1.0", "10.0.0")]
    public async Task Migrate_UnsupportedVersionOrdersAndMissingEdgesFailClosed(string from, string to) {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", from, "--to", to, "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        error.ToString().ShouldContain("Supported edges");
        File.ReadAllText(source).ShouldContain("AddFrontComposerDebugOverlay");
        output.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void MigrationPlanner_DetectsOverlappingZeroLengthInsertions() {
        List<TextChange> changes = [
            new(new TextSpan(12, 0), "first"),
            new(new TextSpan(12, 0), "second"),
        ];

        MigrationPlanner.HasOverlappingChanges(changes).ShouldBeTrue();
    }

    [Fact]
    public void MigrationJson_CapsPerEntryAndAggregateDiffs() {
        string longDiff = new('x', 12_000);
        MigrationEntry[] entries = Enumerable.Range(0, 10)
            .Select(i => new MigrationEntry(
                MigrationDiagnostics.ObsoleteDevOverlay.Id,
                "safe-fix",
                "File" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".cs",
                "what",
                "expected",
                "got",
                "fix",
                "docs/migrations/9.1-to-9.2.md",
                longDiff))
            .ToArray();
        MigrationResult result = new(false, entries, MigrationSummary.From(entries));

        string json = JsonSerializer.Serialize(MigrationJson.From(result), JsonOptions.Stable);

        using var document = JsonDocument.Parse(json);
        JsonElement[] jsonEntries = document.RootElement.GetProperty("entries").EnumerateArray().ToArray();
        string firstDiff = jsonEntries[0].GetProperty("diff").GetString()!;
        firstDiff.Length.ShouldBeLessThan(8_100);
        firstDiff.ShouldContain("[truncated:");
        string lastDiff = jsonEntries[^1].GetProperty("diff").GetString()!;
        lastDiff.ShouldBe("[diff omitted: aggregate diff budget exceeded]");
    }

    [Fact]
    public void MigrationText_CapsPerEntryAndAggregateDiffs() {
        string longDiff = new('x', 12_000);
        MigrationEntry[] entries = Enumerable.Range(0, 10)
            .Select(i => new MigrationEntry(
                MigrationDiagnostics.ObsoleteDevOverlay.Id,
                "safe-fix",
                "File" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".cs",
                "what",
                "expected",
                "got",
                "fix",
                "docs/migrations/9.1-to-9.2.md",
                longDiff))
            .ToArray();
        MigrationResult result = new(false, entries, MigrationSummary.From(entries));

        using StringWriter writer = new();
        MigrationCommand.RenderText(result, writer);
        string text = writer.ToString();

        // Per-entry cap: at least one rendered diff is truncated to the 8,000-char budget.
        text.ShouldContain("[truncated:");
        // Aggregate cap: once 64,000 chars of diff are emitted, later entries are omitted in text mode too.
        text.ShouldContain("[diff omitted: aggregate diff budget exceeded]");
        // The last file's diff body must not leak past the aggregate budget.
        text.ShouldContain("- safe-fix HFCM9001 File9.cs:");
    }

    [Fact]
    public async Task MigrationPlanner_RejectsUnsupportedCodeActionOperations() {
        using AdhocWorkspace workspace = new();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        string projectDirectory = Path.GetTempPath();
        string approvedPath = Path.Combine(projectDirectory, "Program.cs");

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "Acme.App", "Acme.App", LanguageNames.CSharp)
            .AddDocument(documentId, "Program.cs", SourceText.From("class C {}"), filePath: approvedPath);

        SourceText? changedText = await MigrationPlanner.TryExtractDocumentChangesAsync(
            solution,
            [new UnsupportedCodeActionOperation()],
            documentId,
            projectDirectory,
            approvedPath,
            CancellationToken.None);

        changedText.ShouldBeNull();
    }

    [Fact]
    public async Task FrontComposerMigrationCodeFixProvider_ReplacesObsoleteDevOverlayApi() {
        using AdhocWorkspace workspace = new();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "Acme.App", "Acme.App", LanguageNames.CSharp)
            .AddDocument(documentId, "Program.cs", SourceText.From("services.AddFrontComposerDebugOverlay();"));
        _ = workspace.TryApplyChanges(solution);
        Document document = workspace.CurrentSolution.GetDocument(documentId)!;
        Diagnostic diagnostic = (await MigrationDiagnosticScanner.ScanAsync(document, CancellationToken.None)).Single();

        FrontComposerMigrationCodeFixProvider provider = new();
        List<CodeAction> actions = [];
        CodeFixContext context = new(
            document,
            diagnostic.Location.SourceSpan,
            [diagnostic],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(context);
        CodeAction action = actions.Single();
        var operation = (ApplyChangesOperation)(await action.GetOperationsAsync(CancellationToken.None)).Single();
        Document changedDocument = operation.ChangedSolution.GetDocument(documentId)!;
        string changedText = (await changedDocument.GetTextAsync(CancellationToken.None)).ToString();

        changedText.ShouldContain("AddFrontComposerDevMode");
        changedText.ShouldNotContain("AddFrontComposerDebugOverlay");
    }

    [Fact]
    public async Task MigrationPlanner_RejectsCodeActionsThatAddDocuments() {
        // Note: this test exercises the *any-added-document* rejection path
        // (`projectChange.GetAddedDocuments().Any()`); it is not specifically
        // about outside-project additions. See Known Gaps row "T8 code-action
        // safety tests" for the still-pending true outside-project test (which
        // requires a *different* project id and file path outside the
        // approved write set).
        using AdhocWorkspace workspace = new();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        string projectDirectory = Path.Combine(Path.GetTempPath(), "hfc-approved");
        string approvedPath = Path.Combine(projectDirectory, "Program.cs");

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "Acme.App", "Acme.App", LanguageNames.CSharp)
            .AddDocument(documentId, "Program.cs", SourceText.From("class C {}"), filePath: approvedPath);
        Solution changedSolution = solution.AddDocument(
            DocumentId.CreateNewId(projectId),
            "Outside.cs",
            SourceText.From("class Outside {}"),
            filePath: Path.Combine(Path.GetTempPath(), "Outside.cs"));

        SourceText? changedText = await MigrationPlanner.TryExtractDocumentChangesAsync(
            solution,
            [new ApplyChangesOperation(changedSolution)],
            documentId,
            projectDirectory,
            approvedPath,
            CancellationToken.None);

        changedText.ShouldBeNull();
    }

    [Fact]
    public void FrontComposerMigrationCodeFixProvider_DoesNotExposeUnsafeFixAll() {
        FrontComposerMigrationCodeFixProvider provider = new();

        provider.GetFixAllProvider().ShouldBeNull();
    }

    private sealed class UnsupportedCodeActionOperation : CodeActionOperation {
        public override void Apply(Workspace workspace, CancellationToken cancellationToken) {
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunMigrateCaptureAsync(string project, string[] additionalArgs) {
        using StringWriter output = new();
        using StringWriter error = new();
        string[] args = [
            "migrate",
            "--project",
            project,
            "--from",
            "9.1.0",
            "--to",
            "9.2.0",
            .. additionalArgs,
        ];

        int exitCode = await CliApplication.RunAsync(args, output, error, CancellationToken.None).ConfigureAwait(false);
        return (exitCode, output.ToString(), error.ToString());
    }

    private static string ReadProjectFile(DirectoryInfo root, string relativePath)
        => File.ReadAllText(Path.Combine(root.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar)), Encoding.UTF8);

    private static DirectoryInfo ProjectRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        int depth = 0;
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.FrontComposer.slnx"))) {
            current = current.Parent;
            if (++depth > 16) {
                break;
            }
        }

        _ = current.ShouldNotBeNull("Could not locate repository root.");
        return current;
    }
}
