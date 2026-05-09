using System.Text.Json;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
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

        exitCode.ShouldBe(0, output + error.ToString());
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

        exitCode.ShouldBe(0, output + error.ToString());
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
        fixture.WriteSource(
            "Acme.App",
            "Program.cs",
            """
            services.AddFrontComposerDebugOverlay();
            """);
        fixture.WriteGeneratedDiagnosticSidecar(
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
        fixture.WriteSource("Acme.App", "bin/Debug/net10.0/Generated.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--apply", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        error.ToString().ShouldBeEmpty();

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement summary = document.RootElement.GetProperty("summary");
        summary.GetProperty("manualOnly").GetInt32().ShouldBe(1);
        summary.GetProperty("skipped").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task Migrate_DoesNotTreatDiagnosticIdInsideCommentAsManualOnly()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay(); // HFCM9002");

        using StringWriter output = new();
        using StringWriter error = new();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--format", "json"],
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0, output + error.ToString());
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("manualOnly").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task MigrateApply_DetectsContentDriftBeforeWriting()
    {
        using CliFixture fixture = CliFixture.Create();
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
    public async Task Migrate_RefusesExplicitSubmoduleDocuments()
    {
        using CliFixture fixture = CliFixture.Create();
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
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("skipped").GetInt32().ShouldBe(1);
        File.ReadAllText(submoduleSource).ShouldContain("AddFrontComposerDebugOverlay");
        output.ToString().ShouldNotContain(fixture.Root, Case.Sensitive);
    }

    [Fact]
    public async Task Migrate_LargeFixtureUsesProjectDocumentsAndStaysWithinCiBudget()
    {
        using CliFixture fixture = CliFixture.Create();
        string project = fixture.WriteProject("Acme.App", "net10.0");
        for (int i = 0; i < 240; i++) {
            fixture.WriteSource("Acme.App", $"Features/F{i:000}.cs", "namespace Acme.App;");
        }

        fixture.WriteSource("Acme.App", "Program.cs", "services.AddFrontComposerDebugOverlay();");

        using StringWriter output = new();
        using StringWriter error = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        int exitCode = await CliApplication.RunAsync(
            ["migrate", "--project", project, "--from", "9.1.0", "--to", "9.2.0", "--dry-run", "--format", "json"],
            output,
            error,
            CancellationToken.None);
        stopwatch.Stop();

        exitCode.ShouldBe(0, output + error.ToString());
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30));
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("summary").GetProperty("changed").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("entries").EnumerateArray()
            .Select(x => x.GetProperty("path").GetString())
            .ShouldBe(["Program.cs"]);
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

    [Theory]
    [InlineData("9.2.0", "9.1.0")]
    [InlineData("9.0.0", "9.2.0")]
    [InlineData("9.1.0", "10.0.0")]
    public async Task Migrate_UnsupportedVersionOrdersAndMissingEdgesFailClosed(string from, string to)
    {
        using CliFixture fixture = CliFixture.Create();
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
    public void MigrationPlanner_DetectsOverlappingZeroLengthInsertions()
    {
        List<TextChange> changes = [
            new(new TextSpan(12, 0), "first"),
            new(new TextSpan(12, 0), "second"),
        ];

        MigrationPlanner.HasOverlappingChanges(changes).ShouldBeTrue();
    }

    [Fact]
    public async Task MigrationPlanner_RejectsUnsupportedCodeActionOperations()
    {
        using AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId documentId = DocumentId.CreateNewId(projectId);
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
    public async Task FrontComposerMigrationCodeFixProvider_ReplacesObsoleteDevOverlayApi()
    {
        using AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId documentId = DocumentId.CreateNewId(projectId);
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
        ApplyChangesOperation operation = (ApplyChangesOperation)(await action.GetOperationsAsync(CancellationToken.None)).Single();
        Document changedDocument = operation.ChangedSolution.GetDocument(documentId)!;
        string changedText = (await changedDocument.GetTextAsync(CancellationToken.None)).ToString();

        changedText.ShouldContain("AddFrontComposerDevMode");
        changedText.ShouldNotContain("AddFrontComposerDebugOverlay");
    }

    [Fact]
    public async Task MigrationPlanner_RejectsCodeActionsThatAddDocuments()
    {
        // Note: this test exercises the *any-added-document* rejection path
        // (`projectChange.GetAddedDocuments().Any()`); it is not specifically
        // about outside-project additions. See Known Gaps row "T8 code-action
        // safety tests" for the still-pending true outside-project test (which
        // requires a *different* project id and file path outside the
        // approved write set).
        using AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId documentId = DocumentId.CreateNewId(projectId);
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
    public void FrontComposerMigrationCodeFixProvider_DoesNotExposeUnsafeFixAll()
    {
        FrontComposerMigrationCodeFixProvider provider = new();

        provider.GetFixAllProvider().ShouldBeNull();
    }

    private sealed class UnsupportedCodeActionOperation : CodeActionOperation
    {
        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
        }
    }
}
