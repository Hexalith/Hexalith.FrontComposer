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

    [Fact]
    public async Task MigrationPublicationParityAcceptsTheCatalogAndIgnoresUnrelatedInitializersAndComments()
    {
        using var fixture = CreateMigrationParityFixture();
        string catalogPath = Path.Combine(fixture.Root, "MigrationCatalog.cs");
        string catalog = File.ReadAllText(catalogPath);
        catalog = catalog.Replace("private static readonly MigrationEdge[] Edges", """""
            // new("88.0.0", "89.0.0", "docs/migrations/comment.md"),
            /* private static readonly MigrationEdge[] Edges = BuildEdges([
                new("77.0.0", "78.0.0", "docs/migrations/block-comment.md"),
            ]); */
            private static readonly MigrationEdge[] Unrelated = [
                new("66.0.0", "67.0.0", "docs/migrations/unrelated.md"),
            ];
            private const char Quote = '"';
            private const char EscapedQuote = '\'';
            private const char Backslash = '\\';
            private const char UnicodeQuote = '\u0022';
            private const string Example = "private static readonly MigrationEdge[] Edges = BuildEdges([fake]);";
            private const string RawExample = """
                "quoted" // content
                #if false
                private static readonly MigrationEdge[] Edges = BuildEdges([fake]);
                """;
            private const string WideRawExample = """"
                """ and "quotes" are literal content
                """";
            private static readonly MigrationEdge[] Edges
            """"", StringComparison.Ordinal);
        catalog.ShouldContain("docs/migrations/comment.md");
        catalog.ShouldContain("docs/migrations/block-comment.md");
        catalog.ShouldContain("private static readonly MigrationEdge[] Unrelated");
        catalog.ShouldContain("private const char Quote = '\"';");
        catalog.ShouldContain("private const char EscapedQuote = '\\'';");
        catalog.ShouldContain("private const char Backslash = '\\\\';");
        catalog.ShouldContain("private const char UnicodeQuote = '\\u0022';");
        catalog.ShouldContain("private const string Example");
        catalog.ShouldContain("private const string RawExample");
        catalog.ShouldContain("private const string WideRawExample");
        File.WriteAllText(catalogPath, catalog);
        File.AppendAllText(Path.Combine(fixture.Root, "migrations", "index.md"), "\nSee [CLI reference](../reference/cli.md).\n");

        (await RunMigrationParityAsync(fixture.Root)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("missing-guide-mode", "migrationMode must be")]
    [InlineData("unknown-guide-mode", "migrationMode must be")]
    [InlineData("noncanonical-guide-mode-key", "canonical migrationMode key")]
    [InlineData("manual-non-exact-from-version", "fromVersion must be an exact semantic version")]
    [InlineData("manual-non-exact-to-version", "toVersion must be an exact semantic version")]
    [InlineData("false-executable", "complete catalog tuple")]
    [InlineData("false-manual", "falsely classifies")]
    [InlineData("manual-executable-alias", "falsely classifies")]
    [InlineData("conditional-catalog", "conditional catalog syntax is unsupported")]
    [InlineData("unsupported-catalog-syntax", "unsupported tuple syntax")]
    [InlineData("commented-index-row", "missing guide link")]
    [InlineData("fenced-index-row", "missing guide link")]
    [InlineData("tilde-fenced-index-row", "missing guide link")]
    [InlineData("header-prefix-duplicate-row", "duplicate guide link")]
    [InlineData("separator-prefix-duplicate-row", "duplicate guide link")]
    [InlineData("unclassified-fragment-link", "classified row")]
    [InlineData("unclassified-query-link", "classified row")]
    [InlineData("missing-index-link", "missing guide link")]
    [InlineData("duplicate-index-link", "duplicate guide link")]
    [InlineData("unclassified-index-link", "classified row")]
    [InlineData("stale-index-link", "stale guide link")]
    [InlineData("index-version-mismatch", "versions or migrationMode")]
    [InlineData("index-path-mismatch", "versions or migrationMode")]
    [InlineData("index-path-case-mismatch", "stale guide link")]
    [InlineData("index-unknown-mode", "migrationMode must be")]
    [InlineData("index-false-executable", "versions or migrationMode")]
    [InlineData("guide-version-mismatch", "must match catalog versions")]
    [InlineData("catalog-version-mismatch", "must match catalog versions")]
    [InlineData("catalog-path-mismatch", "missing catalog guidance")]
    [InlineData("missing-catalog-guidance", "missing catalog guidance")]
    [InlineData("duplicate-catalog-edge", "duplicate executable edge")]
    public async Task MigrationPublicationParityRejectsDriftWithDeterministicRelativeDiagnostics(string scenario, string expected)
    {
        using var fixture = CreateMigrationParityFixture();
        string indexPath = Path.Combine(fixture.Root, "migrations", "index.md");
        string guidePath = Path.Combine(fixture.Root, "migrations", "9.1-to-9.2.md");
        string manualPath = Path.Combine(fixture.Root, "migrations", "3.1-to-4.0.md");
        string catalogPath = Path.Combine(fixture.Root, "MigrationCatalog.cs");
        string index = File.ReadAllText(indexPath);
        string guide = File.ReadAllText(guidePath);
        string catalog = File.ReadAllText(catalogPath);
        string row = index.Split('\n').Single(line => line.Contains("(9.1-to-9.2.md)", StringComparison.Ordinal)).TrimEnd('\r');
        switch (scenario)
        {
            case "missing-guide-mode":
                File.WriteAllText(guidePath, guide.Replace("migrationMode: cli-supported", "", StringComparison.Ordinal));
                break;
            case "unknown-guide-mode":
                File.WriteAllText(guidePath, guide.Replace("migrationMode: cli-supported", "migrationMode: automatic", StringComparison.Ordinal));
                break;
            case "noncanonical-guide-mode-key":
                File.WriteAllText(guidePath, guide.Replace("migrationMode:", "MigrationMode:", StringComparison.Ordinal));
                break;
            case "manual-non-exact-from-version":
                File.WriteAllText(manualPath, File.ReadAllText(manualPath).Replace("fromVersion: \"3.1.1\"", "fromVersion: \"latest\"", StringComparison.Ordinal));
                File.WriteAllText(indexPath, index.Replace("| 3.1.1 |", "| latest |", StringComparison.Ordinal));
                break;
            case "manual-non-exact-to-version":
                File.WriteAllText(manualPath, File.ReadAllText(manualPath).Replace("toVersion: \"4.0.0\"", "toVersion: \"latest\"", StringComparison.Ordinal));
                File.WriteAllText(indexPath, index.Replace("| 4.0.0 |", "| latest |", StringComparison.Ordinal));
                break;
            case "false-executable":
                File.WriteAllText(manualPath, File.ReadAllText(manualPath).Replace("migrationMode: manual-only", "migrationMode: cli-supported", StringComparison.Ordinal));
                break;
            case "false-manual":
                File.WriteAllText(guidePath, guide.Replace("migrationMode: cli-supported", "migrationMode: manual-only", StringComparison.Ordinal));
                break;
            case "manual-executable-alias":
                File.WriteAllText(Path.Combine(fixture.Root, "migrations", "alias.md"), guide.Replace("migrationMode: cli-supported", "migrationMode: manual-only", StringComparison.Ordinal));
                File.AppendAllText(indexPath, row.Replace("| cli-supported |", "| manual-only |", StringComparison.Ordinal).Replace("(9.1-to-9.2.md)", "(alias.md)", StringComparison.Ordinal) + "\n");
                break;
            case "conditional-catalog":
                File.WriteAllText(catalogPath, catalog.Replace("    private static readonly MigrationEdge[] Edges = BuildEdges([", "#if false\n    private static readonly MigrationEdge[] Edges = BuildEdges([", StringComparison.Ordinal).Replace("    ]);", "    ]);\n#else\n    private static readonly MigrationEdge[] Edges = [];\n#endif", StringComparison.Ordinal));
                break;
            case "unsupported-catalog-syntax":
                string explicitEdge = "new MigrationEdge(\"4.5.0\", \"4.6.0\", \"docs/migrations/4.5-to-4.6.md\"),";
                string unsupportedCatalog = catalog.Replace("    ]);", "        " + explicitEdge + "\n    ]);", StringComparison.Ordinal);
                unsupportedCatalog.ShouldContain(explicitEdge);
                File.WriteAllText(catalogPath, unsupportedCatalog);
                break;
            case "commented-index-row":
                File.WriteAllText(indexPath, index.Replace(row, "<!--\n" + row + "\n-->", StringComparison.Ordinal));
                break;
            case "fenced-index-row":
                File.WriteAllText(indexPath, index.Replace(row, "```markdown\n" + row + "\n```", StringComparison.Ordinal));
                break;
            case "tilde-fenced-index-row":
                File.WriteAllText(indexPath, index.Replace(row, "~~~~markdown\n" + row + "\n~~~~", StringComparison.Ordinal));
                break;
            case "header-prefix-duplicate-row":
                File.AppendAllText(indexPath, row.Replace("| 9.1.0 |", "| fromVersion |", StringComparison.Ordinal) + "\n");
                break;
            case "separator-prefix-duplicate-row":
                File.AppendAllText(indexPath, row.Replace("| 9.1.0 |", "| --- |", StringComparison.Ordinal) + "\n");
                break;
            case "unclassified-fragment-link":
                File.AppendAllText(indexPath, "- [Unclassified guide](9.1-to-9.2.md#analyzer-and-code-fix)\n");
                break;
            case "unclassified-query-link":
                File.AppendAllText(indexPath, "- [Unclassified guide](9.1-to-9.2.md?view=manual)\n");
                break;
            case "missing-index-link":
                File.WriteAllText(indexPath, index.Replace(row, "", StringComparison.Ordinal));
                break;
            case "duplicate-index-link":
                File.AppendAllText(indexPath, row + "\n");
                break;
            case "unclassified-index-link":
                File.AppendAllText(indexPath, "- [Unclassified guide](9.1-to-9.2.md)\n");
                break;
            case "stale-index-link":
                File.WriteAllText(indexPath, index.Replace("(9.1-to-9.2.md)", "(stale.md)", StringComparison.Ordinal));
                break;
            case "index-version-mismatch":
                File.WriteAllText(indexPath, index.Replace("| 3.1.1 |", "| 3.1.0 |", StringComparison.Ordinal));
                break;
            case "index-path-mismatch":
                File.WriteAllText(indexPath, index.Replace("(9.1-to-9.2.md)", "(3.1-to-4.0.md)", StringComparison.Ordinal));
                break;
            case "index-path-case-mismatch":
                File.WriteAllText(indexPath, index.Replace("(9.1-to-9.2.md)", "(9.1-TO-9.2.md)", StringComparison.Ordinal));
                break;
            case "index-unknown-mode":
                File.WriteAllText(indexPath, index.Replace("| cli-supported |", "| automatic |", StringComparison.Ordinal));
                break;
            case "index-false-executable":
                File.WriteAllText(indexPath, index.Replace("| manual-only |", "| cli-supported |", StringComparison.Ordinal));
                break;
            case "guide-version-mismatch":
                File.WriteAllText(guidePath, guide.Replace("fromVersion: \"9.1.0\"", "fromVersion: \"9.0.0\"", StringComparison.Ordinal));
                break;
            case "catalog-version-mismatch":
                File.WriteAllText(catalogPath, catalog.Replace("\"9.1.0\"", "\"9.0.0\"", StringComparison.Ordinal));
                break;
            case "catalog-path-mismatch":
                File.WriteAllText(catalogPath, catalog.Replace("docs/migrations/9.1-to-9.2.md", "docs/migrations/stale.md", StringComparison.Ordinal));
                break;
            case "missing-catalog-guidance":
                File.Delete(guidePath);
                break;
            case "duplicate-catalog-edge":
                File.WriteAllText(catalogPath, catalog.Replace("new(\"9.1.0\"", "new(\"9.1.0\", \"9.2.0\", \"docs/migrations/9.1-to-9.2.md\"),\n        new(\"9.1.0\"", StringComparison.Ordinal));
                break;
            default:
                throw new InvalidOperationException("Unknown fixture.");
        }

        string[] failures = await RunMigrationParityAsync(fixture.Root);
        failures.ShouldNotBeEmpty();
        string.Join('\n', failures).ShouldContain(expected);
        string.Join('\n', failures).ShouldNotContain(fixture.Root);
        string.Join('\n', failures).ShouldNotContain(ProjectRoot().FullName);
        (await RunMigrationParityAsync(fixture.Root)).ShouldBe(failures);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MigrationRetainsOrdinarySourceWhenLinkedAliasSharesItsCanonicalTarget(bool linkFirst, bool apply)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Source.cs", "services.AddFrontComposerDebugOverlay();");
        string link = Path.Combine(Path.GetDirectoryName(project)!, "Linked.cs");
        FileSystemInfo linkInfo = File.CreateSymbolicLink(link, source);
        string? originalLinkTarget = linkInfo.LinkTarget;
        string first = linkFirst ? "Linked.cs" : "Source.cs";
        string second = linkFirst ? "Source.cs" : "Linked.cs";
        File.WriteAllText(project, $$"""
            <Project><ItemGroup>
              <Compile Include="{{first}}" Link="First.cs" />
              <Compile Include="{{second}}" Link="Second.cs" />
            </ItemGroup></Project>
            """);

        (int exitCode, string output, string error) = await RunMigrateCaptureAsync(project, [apply ? "--apply" : "--dry-run", "--format", "json"]);

        exitCode.ShouldBe(ExitCodes.Success, error + output);
        error.ShouldBeEmpty();
        using var document = JsonDocument.Parse(output);
        document.RootElement.GetProperty("applied").GetBoolean().ShouldBe(apply);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("failed").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("entries").EnumerateArray().Single(entry => entry.GetProperty("kind").GetString() == "skipped").GetProperty("path").GetString().ShouldBe("Linked.cs");
        File.ReadAllText(source).ShouldBe(apply ? "services.AddFrontComposerDevMode();" : "services.AddFrontComposerDebugOverlay();");
        linkInfo.Refresh();
        linkInfo.LinkTarget.ShouldBe(originalLinkTarget);
    }

    [Theory]
    [InlineData("project-root")]
    [InlineData("workspace-ancestor")]
    [InlineData("nested-workspace-ancestor")]
    public async Task MigrationApplyRefusesChangedResolvedProjectRootOrWorkspaceAncestor(string scenario)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "A.cs", "services.AddFrontComposerDebugOverlay();");
        _ = fixture.WriteSource("Acme.App", "Z.cs", "services.AddFrontComposerDebugOverlay();");
        string workspace = Path.Combine(fixture.Root, "Workspace");
        string relativeProject = scenario == "nested-workspace-ancestor" ? "Nested/Acme.App" : "Acme.App";
        string originalDirectory = Path.Combine(workspace, relativeProject);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(originalDirectory)!);
        Directory.Move(Path.GetDirectoryName(project)!, originalDirectory);
        string link = scenario == "project-root" ? originalDirectory : Path.Combine(fixture.Root, "LinkedWorkspace");
        if (scenario != "project-root")
        {
            _ = CreateDirectoryMigrationLink(link, workspace);
        }
        string selectedProject = Path.Combine(scenario == "project-root" ? originalDirectory : Path.Combine(link, relativeProject), "Acme.App.csproj");
        MigrationPlan plan = await MigrationPlanner.PlanAsync(selectedProject, MigrationCatalog.Resolve("9.1.0", "9.2.0")!, CancellationToken.None);
        plan.FileEdits.Count.ShouldBe(2);
        string[] sourceNames = ["A.cs", "Z.cs"];
        plan.FileEdits.Select(edit => edit.CanonicalPath).ShouldBe(sourceNames.Select(name => Path.Combine(originalDirectory, name)));
        string replacementWorkspace = Path.Combine(fixture.Root, "Replacement");
        string replacementDirectory = Path.Combine(replacementWorkspace, relativeProject);
        _ = Directory.CreateDirectory(replacementDirectory);
        byte[] originalBytes = File.ReadAllBytes(Path.Combine(originalDirectory, "Z.cs"));
        foreach (string name in sourceNames)
        {
            File.WriteAllBytes(Path.Combine(replacementDirectory, name), originalBytes);
        }
        if (scenario == "project-root")
        {
            string savedDirectory = Path.Combine(fixture.Root, "SavedProject");
            Directory.Move(originalDirectory, savedDirectory);
            originalDirectory = savedDirectory;
        }
        else
        {
            Directory.Delete(link);
        }
        FileSystemInfo linkInfo = CreateDirectoryMigrationLink(link, scenario == "project-root" ? replacementDirectory : replacementWorkspace);
        string? originalLinkTarget = linkInfo.LinkTarget;

        MigrationResult result = await MigrationApplier.ApplyAsync(plan, CancellationToken.None);

        result.Applied.ShouldBeFalse();
        result.Summary.Changed.ShouldBe(0);
        result.Summary.Failed.ShouldBe(2);
        result.Entries.ShouldAllBe(entry => entry.Kind == "failed" && entry.What == "Resolved write target changed or became unsafe between planning and apply.");
        foreach (string name in sourceNames)
        {
            File.ReadAllBytes(Path.Combine(originalDirectory, name)).ShouldBe(originalBytes);
            File.ReadAllBytes(Path.Combine(replacementDirectory, name)).ShouldBe(originalBytes);
        }
        linkInfo.Refresh();
        linkInfo.LinkTarget.ShouldBe(originalLinkTarget);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, true)]
    public async Task MigrationRefusesFileAndAncestorLinksBeforeReadingSource(bool directoryLink, bool external, bool apply, bool nested)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string projectDirectory = Path.GetDirectoryName(project)!;
        string targetDirectory = Path.Combine(external ? fixture.Root : projectDirectory, "Target");
        _ = Directory.CreateDirectory(targetDirectory);
        string target = Path.Combine(targetDirectory, nested ? "Nested/Regular/Source.cs" : "Source.cs");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        // Invalid UTF-8 would produce a failed read if the planner touched target bytes.
        byte[] targetBytes = [0xFF, 0x00, 0xFF];
        await File.WriteAllBytesAsync(target, targetBytes, CancellationToken.None);
        string link = Path.Combine(projectDirectory, directoryLink ? "LinkedDirectory" : "Linked.cs");
        FileSystemInfo linkInfo = directoryLink
            ? CreateDirectoryMigrationLink(link, targetDirectory)
            : File.CreateSymbolicLink(link, target);
        string? originalLinkTarget = linkInfo.LinkTarget;
        string include = directoryLink ? (nested ? "LinkedDirectory/Nested/Regular/Source.cs" : "LinkedDirectory/Source.cs") : "Linked.cs";
        File.WriteAllText(project, $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <Compile Include="{{include}}" Link="ReportedSource.cs" />
                <Compile Include="Ordinary.cs" />
              </ItemGroup>
            </Project>
            """);
        string ordinary = fixture.WriteSource("Acme.App", "Ordinary.cs", "services.AddFrontComposerDebugOverlay();");

        (int exitCode, string output, string error) = await RunMigrateCaptureAsync(project, [apply ? "--apply" : "--dry-run", "--format", "json"]);

        exitCode.ShouldBe(ExitCodes.Success, error + output);
        error.ShouldBeEmpty();
        using var document = JsonDocument.Parse(output);
        document.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("frontcomposer.cli.migrate.v1");
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("failed").GetInt32().ShouldBe(0);
        JsonElement skipped = document.RootElement.GetProperty("entries").EnumerateArray().Single(entry => entry.GetProperty("kind").GetString() == "skipped");
        skipped.GetProperty("expected").GetString()!.ShouldContain("without file links or linked directories below the project root");
        skipped.GetProperty("got").GetString().ShouldBe("Excluded, outside-root, or linked path.");
        (await File.ReadAllBytesAsync(target, CancellationToken.None)).ShouldBe(targetBytes);
        linkInfo.Refresh();
        linkInfo.LinkTarget.ShouldBe(originalLinkTarget);
        File.ReadAllText(ordinary).ShouldContain(apply ? "AddFrontComposerDevMode" : "AddFrontComposerDebugOverlay");
        output.ShouldNotContain(fixture.Root);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MigrationAllowsProjectReachedThroughLinkedWorkspaceAncestors(bool nestedProject, bool apply)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Source.cs", "services.AddFrontComposerDebugOverlay();");
        string workspace = Path.Combine(fixture.Root, "Workspace");
        _ = Directory.CreateDirectory(workspace);
        string projectDirectory = Path.Combine(workspace, nestedProject ? "Nested/Acme.App" : "Acme.App");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(projectDirectory)!);
        Directory.Move(Path.GetDirectoryName(project)!, projectDirectory);
        source = Path.Combine(projectDirectory, Path.GetFileName(source));
        string link = Path.Combine(fixture.Root, "LinkedWorkspace");
        FileSystemInfo linkInfo = CreateDirectoryMigrationLink(link, workspace);
        string? originalLinkTarget = linkInfo.LinkTarget;
        string selectedProject = Path.Combine(link, nestedProject ? "Nested/Acme.App" : "Acme.App", "Acme.App.csproj");

        (int exitCode, string output, string error) = await RunMigrateCaptureAsync(selectedProject, [apply ? "--apply" : "--dry-run", "--format", "json"]);

        exitCode.ShouldBe(ExitCodes.Success, error + output);
        error.ShouldBeEmpty();
        using var document = JsonDocument.Parse(output);
        document.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("frontcomposer.cli.migrate.v1");
        document.RootElement.GetProperty("applied").GetBoolean().ShouldBe(apply);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("summary").GetProperty("failed").GetInt32().ShouldBe(0);
        File.ReadAllText(source).ShouldBe(apply ? "services.AddFrontComposerDevMode();" : "services.AddFrontComposerDebugOverlay();");
        linkInfo.Refresh();
        linkInfo.LinkTarget.ShouldBe(originalLinkTarget);
        output.ShouldNotContain(fixture.Root);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task MigrationApplyRefusesLinksIntroducedAfterPlanningAndKeepsEarlierWrites(bool directoryLink, bool external, bool nested)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string projectDirectory = Path.GetDirectoryName(project)!;
        string earlier = fixture.WriteSource("Acme.App", "A.cs", "services.AddFrontComposerDebugOverlay();");
        string relativeSource = directoryLink ? (nested ? "Z/Nested/Regular/Source.cs" : "Z/Source.cs") : "Z.cs";
        string source = fixture.WriteSource("Acme.App", relativeSource, "services.AddFrontComposerDebugOverlay();");
        MigrationPlan plan = await MigrationPlanner.PlanAsync(project, MigrationCatalog.Resolve("9.1.0", "9.2.0")!, CancellationToken.None);
        plan.FileEdits.Count.ShouldBe(2);
        string targetDirectory = Path.Combine(external ? fixture.Root : projectDirectory, "Target");
        _ = Directory.CreateDirectory(targetDirectory);
        string target = Path.Combine(targetDirectory, directoryLink ? (nested ? "Nested/Regular/Source.cs" : "Source.cs") : "Target.cs");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        byte[] targetBytes = await File.ReadAllBytesAsync(source, CancellationToken.None);
        await File.WriteAllBytesAsync(target, targetBytes, CancellationToken.None);
        File.Delete(source);
        string link = directoryLink ? Path.Combine(projectDirectory, "Z") : source;
        if (directoryLink)
        {
            Directory.Delete(link, recursive: true);
        }
        FileSystemInfo linkInfo = directoryLink
            ? CreateDirectoryMigrationLink(link, targetDirectory)
            : File.CreateSymbolicLink(link, target);
        string? originalLinkTarget = linkInfo.LinkTarget;

        MigrationResult result = await MigrationApplier.ApplyAsync(plan, CancellationToken.None);

        result.Applied.ShouldBeFalse();
        result.Summary.Failed.ShouldBe(1);
        result.Summary.Changed.ShouldBe(1);
        File.ReadAllText(earlier).ShouldContain("AddFrontComposerDevMode");
        (await File.ReadAllBytesAsync(target, CancellationToken.None)).ShouldBe(targetBytes);
        linkInfo.Refresh();
        linkInfo.LinkTarget.ShouldBe(originalLinkTarget);
        result.Entries.Single(entry => entry.Kind == "failed").Path.ShouldBe(relativeSource);
    }

    [Fact]
    public async Task MigrationRefusesOutsideRootCompileLinkMetadata()
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string target = fixture.WriteSource("Outside", "Source.cs", "services.AddFrontComposerDebugOverlay();");
        byte[] original = await File.ReadAllBytesAsync(target, CancellationToken.None);
        File.WriteAllText(project, """
            <Project><ItemGroup><Compile Include="../Outside/Source.cs" Link="Source.cs" /></ItemGroup></Project>
            """);

        (int exitCode, string output, string error) = await RunMigrateCaptureAsync(project, ["--apply", "--format", "json"]);

        exitCode.ShouldBe(ExitCodes.Success, output + error);
        using var document = JsonDocument.Parse(output);
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
        (await File.ReadAllBytesAsync(target, CancellationToken.None)).ShouldBe(original);
        output.ShouldNotContain(fixture.Root);
    }

    [Fact]
    public async Task MigrationPublicationCommandRejectsCatalogOnlyMismatchInAnIsolatedCopy()
    {
        using var fixture = CliFixture.Create();
        foreach (string relative in new[]
        {
            "eng/validate-docs.ps1", "global.json", ".gitmodules", "docs",
            "src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs",
            "src/Hexalith.FrontComposer.Shell/Components",
            "src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs",
            "src/Hexalith.FrontComposer.Contracts", "src/Hexalith.FrontComposer.Contracts.UI/Rendering",
            "_bmad-output/project-docs/component-inventory.md",
            "_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md",
        })
        {
            CopyPublicationInput(Path.Combine(ProjectRoot().FullName, relative), Path.Combine(fixture.Root, relative));
        }
        string catalogPath = Path.Combine(fixture.Root, "src", "Hexalith.FrontComposer.Cli", "MigrationCatalog.cs");
        File.WriteAllText(catalogPath, File.ReadAllText(catalogPath).Replace("\"9.1.0\"", "\"9.0.0\"", StringComparison.Ordinal));
        ProcessStartInfo start = new("pwsh")
        {
            WorkingDirectory = fixture.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in new[] { "-NoProfile", "-File", "eng/validate-docs.ps1", "-SkipDocFx", "-SkipSnippetBuild" })
        {
            start.ArgumentList.Add(argument);
        }

        (int exitCode, string output, string error) = await RunProcessCaptureAsync(start, TimeSpan.FromSeconds(90));

        exitCode.ShouldNotBe(0, output + error);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixture.Root, "artifacts", "docs", "validation-manifest.json")));
        string[] failures = manifest.RootElement.GetProperty("blockingFailures").EnumerateArray().Select(value => value.GetString()!).ToArray();
        failures.ShouldNotBeEmpty();
        string.Join('\n', failures).ShouldContain("must match catalog versions and cli-supported classification");
        failures.ShouldAllBe(failure => failure.StartsWith("docs/migrations/9.1-to-9.2.md ", StringComparison.Ordinal));
        string.Join('\n', failures).ShouldNotContain(fixture.Root);
    }

    [Theory]
    [InlineData("--dry-run", "json")]
    [InlineData("--apply", "json")]
    [InlineData("--dry-run", "text")]
    [InlineData("--apply", "text")]
    public async Task MigrationPreCancelledBeforePlanningReturnsFourWithoutAReport(string mode, string format)
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        string source = fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");
        byte[] original = await File.ReadAllBytesAsync(source, CancellationToken.None);
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", mode, "--format", format],
            output, error, new CancellationToken(canceled: true));

        exitCode.ShouldBe(ExitCodes.ApplyWriteFailure);
        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Operation was cancelled");
        (await File.ReadAllBytesAsync(source, CancellationToken.None)).ShouldBe(original);
    }

    [Fact]
    public async Task MigrationSyntacticRenameIncludesSameNamedNonApiIdentifiersAndExcludesNameof()
    {
        using var fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        _ = fixture.WriteSource("Acme.App", "Program.cs", """
            var callback = AddFrontComposerDebugOverlay;
            var display = nameof(AddFrontComposerDebugOverlay);
            """);

        MigrationPlan plan = await MigrationPlanner.PlanAsync(project, MigrationCatalog.Resolve("9.1.0", "9.2.0")!, CancellationToken.None);

        plan.FileEdits.Single().UpdatedText.ShouldContain("var callback = AddFrontComposerDevMode;");
        plan.FileEdits.Single().UpdatedText.ShouldContain("nameof(AddFrontComposerDebugOverlay)");
    }

    private static void CopyPublicationInput(string source, string destination)
    {
        if (File.Exists(source))
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);
            return;
        }
        _ = Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            if (Path.GetFileName(directory) is "bin" or "obj" or "_site")
            {
                continue;
            }
            CopyPublicationInput(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static FileSystemInfo CreateDirectoryMigrationLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Directory.CreateSymbolicLink(link, target);
        }

        // Junctions are directory reparse points and do not require Windows symlink privileges.
        ProcessStartInfo start = new("cmd") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "/c", "mklink", "/J", link, target })
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        process.WaitForExit(10_000).ShouldBeTrue();
        process.ExitCode.ShouldBe(0, process.StandardError.ReadToEnd());
        return new DirectoryInfo(link);
    }

    private static CliFixture CreateMigrationParityFixture()
    {
        var fixture = CliFixture.Create();
        string migrations = Path.Combine(fixture.Root, "migrations");
        _ = Directory.CreateDirectory(migrations);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(ProjectRoot().FullName, "docs", "migrations"), "*.md"))
        {
            File.Copy(file, Path.Combine(migrations, Path.GetFileName(file)));
        }
        File.Copy(Path.Combine(ProjectRoot().FullName, "src", "Hexalith.FrontComposer.Cli", "MigrationCatalog.cs"), Path.Combine(fixture.Root, "MigrationCatalog.cs"));
        return fixture;
    }

    private static async Task<string[]> RunMigrationParityAsync(string fixtureRoot)
    {
        string runner = Path.Combine(fixtureRoot, "parity.ps1");
        await File.WriteAllTextAsync(runner, """
            param([string]$Validator, [string]$FixtureRoot)
            $ErrorActionPreference = 'Stop'
            Set-StrictMode -Version Latest
            $tokens = $null; $errors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseFile($Validator, [ref]$tokens, [ref]$errors)
            if ($errors.Count) { throw 'Publication validator has parse errors.' }
            $names = @('Add-Failure', 'Get-FrontMatter', 'Get-MigrationCatalogEdges', 'Assert-MigrationParity')
            foreach ($function in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $false)) {
                if ($function.Name -in $names) { . ([scriptblock]::Create($function.Extent.Text)) }
            }
            $failures = [System.Collections.Generic.List[string]]::new()
            Assert-MigrationParity (Join-Path $FixtureRoot 'migrations') (Join-Path $FixtureRoot 'MigrationCatalog.cs') $failures
            ConvertTo-Json -InputObject $failures.ToArray() -Compress
            """, CancellationToken.None).ConfigureAwait(false);
        ProcessStartInfo start = new("pwsh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in new[] { "-NoProfile", "-File", runner, Path.Combine(ProjectRoot().FullName, "eng", "validate-docs.ps1"), fixtureRoot })
        {
            start.ArgumentList.Add(argument);
        }
        (int exitCode, string output, string error) = await RunProcessCaptureAsync(start, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        exitCode.ShouldBe(0, error);
        return JsonSerializer.Deserialize<string[]>(output)!;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcessCaptureAsync(ProcessStartInfo start, TimeSpan limit)
    {
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(limit);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
            {
            }
            // Uncancelled reads drain both redirected streams after terminating the process tree.
            await Task.WhenAll(output, error).ConfigureAwait(false);
            throw new TimeoutException(start.FileName + " did not exit within " + limit + ".");
        }
        return (process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
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
