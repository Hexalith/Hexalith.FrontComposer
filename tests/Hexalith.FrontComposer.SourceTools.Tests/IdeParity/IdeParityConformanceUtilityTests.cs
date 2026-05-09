using Hexalith.FrontComposer.SourceTools.Conformance;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

public sealed class IdeParityConformanceUtilityTests
{
    [Theory]
    [InlineData("Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjection.g.razor.cs")]
    [InlineData("Release", "netstandard2.0", "Acme.Shipping.SubmitOrderCommand.CommandForm.g.razor.cs", "obj/Release/netstandard2.0/generated/HexalithFrontComposer/Acme.Shipping.SubmitOrderCommand.CommandForm.g.razor.cs")]
    public void GeneratedOutputPathContract_BuildsPublicForwardSlashPath(string configuration, string framework, string fileName, string expected)
    {
        GeneratedOutputPathContract.BuildProjectRelativePath(configuration, framework, fileName).ShouldBe(expected);
    }

    [Fact]
    public void IdeParityCounterFixture_GeneratesDeterministicSymbolsForMatrixRows()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(IdeParityCounterFixtureSource);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjection.g.razor.cs");
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjectionFeature.g.cs");
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjectionRegistration.g.cs");
        fileNames.ShouldContain("IdeParity.Counter.ConfigureCounterCommand.CommandRenderer.g.razor.cs");
        fileNames.ShouldContain("IdeParity.Counter.ConfigureCounterCommand.CommandLifecycleBridge.g.cs");

        GeneratedOutputPathContract
            .BuildProjectRelativePath("Debug", "net10.0", "IdeParity.Counter.IdeParityCounterProjection.g.razor.cs")
            .ShouldBe("obj/Debug/net10.0/generated/HexalithFrontComposer/IdeParity.Counter.IdeParityCounterProjection.g.razor.cs");
    }

    [Fact]
    public void EvidencePathNormalization_RejectsTraversalAbsoluteUserAndUnsupportedUriPaths()
    {
        string root = Path.Combine(Path.GetTempPath(), "frontcomposer-ide-parity-root");

        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "artifacts/ide-parity/evidence/IDE-MUST-001.json", caseSensitive: false, out string normalized)
            .ShouldBeTrue();
        normalized.ShouldBe("artifacts/ide-parity/evidence/IDE-MUST-001.json");

        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "../outside.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "C:/Users/Ada/AppData/Local/Temp/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "file:///C:/Users/Ada/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "https://example.test/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
    }

    [Fact]
    public void ReportSanitizer_RedactsControlsSecretsAbsolutePathsAndSpreadsheetFormulas()
    {
        string unsafeValue = "=HYPERLINK(\"file:///C:/Users/Ada/AppData/Local/Temp/log.txt\")\u001b[31m token=ghp_abcdefghijklmnopqrstuvwxyz1234567890 machine=BUILD-01";

        string markdown = IdeParityReportSanitizer.Sanitize(unsafeValue, IdeParityReportFormat.Markdown);
        markdown.ShouldNotContain("\u001b");
        markdown.ShouldNotContain("C:/Users/Ada");
        markdown.ShouldNotContain("ghp_abcdefghijklmnopqrstuvwxyz1234567890");
        markdown.ShouldNotContain("BUILD-01");
        markdown.ShouldContain("[redacted-path]");
        markdown.ShouldContain("[redacted-secret]");
        markdown.ShouldContain("[redacted-machine]");
        markdown.ShouldContain("=HYPERLINK");

        string csv = IdeParityReportSanitizer.Sanitize(unsafeValue, IdeParityReportFormat.Csv);
        csv.ShouldContain("'=HYPERLINK");
        csv.ShouldNotContain("file:///");

        string terminal = IdeParityReportSanitizer.Sanitize(unsafeValue, IdeParityReportFormat.Terminal);
        terminal.ShouldNotContain("\u001b");
        terminal.ShouldNotContain("ghp_");
    }

    [Fact]
    public void VersionRevalidation_ProducesBlockingDryRunIssueWhenGithubIsUnavailable()
    {
        IdeParityVersionPin supported = new(
            Product: "Visual Studio 2022",
            MinimumInclusive: "17.13",
            MaximumExclusive: "17.14",
            Owner: "SourceTools");

        IdeParityDetectedVersion detected = new(
            Product: "Visual Studio 2022",
            Version: "17.14",
            Os: "Windows",
            Fixture: "IdeParityCounterFixture",
            MatrixRows: ["IDE-MUST-001", "IDE-MUST-002"],
            ExpectedBehavior: "Generated source navigation remains on the public path contract.",
            ObservedBehavior: "Vendor minor version moved outside the pinned range.");

        IdeParityRevalidationIssue issue = IdeParityVersionRevalidator.CreateDryRunIssue(supported, detected, githubAvailable: false);

        issue.IsBlocking.ShouldBeTrue();
        issue.DryRun.ShouldBeTrue();
        issue.Title.ShouldContain("Visual Studio 2022 17.14");
        issue.Labels.ShouldContain("ide-parity");
        issue.Labels.ShouldContain("conformance-revalidation");
        issue.Body.ShouldContain("IDE-MUST-001");
        issue.Body.ShouldContain("current pin: 17.13 <= version < 17.14");
        issue.Body.ShouldContain("GitHub issue creation unavailable");
        issue.Body.ShouldNotContain("C:/Users/");
    }

    private const string IdeParityCounterFixtureSource = """
        using Hexalith.FrontComposer.Contracts.Attributes;

        namespace IdeParity.Counter;

        [BoundedContext("Counter", DisplayLabel = "Counter")]
        [Projection]
        public partial class IdeParityCounterProjection
        {
            public string Id { get; set; } = string.Empty;
            public int Count { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        [Command]
        public sealed class ConfigureCounterCommand
        {
            public string MessageId { get; set; } = string.Empty;
            public string CounterId { get; set; } = string.Empty;
            public int Step { get; set; }
        }
        """;
}
