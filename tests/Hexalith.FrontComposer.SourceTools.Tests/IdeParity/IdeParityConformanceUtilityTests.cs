using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Conformance;
using Hexalith.FrontComposer.SourceTools.Conformance;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

[Trait("MatrixRowId", "IDE-MUST-001")]
[Trait("MatrixRowId", "IDE-MUST-003")]
[Trait("MatrixRowId", "IDE-MUST-005")]
public sealed class IdeParityConformanceUtilityTests
{
    [Theory]
    [InlineData("Debug", "net10.0", "Acme.Shipping.ShipmentProjection.g.razor.cs", "obj/Debug/net10.0/generated/HexalithFrontComposer/Acme.Shipping.ShipmentProjection.g.razor.cs")]
    [InlineData("Release", "netstandard2.0", "Acme.Shipping.SubmitOrderCommand.CommandForm.g.razor.cs", "obj/Release/netstandard2.0/generated/HexalithFrontComposer/Acme.Shipping.SubmitOrderCommand.CommandForm.g.razor.cs")]
    [Trait("MatrixRowId", "IDE-MUST-003")]
    public void GeneratedOutputPathContract_BuildsPublicForwardSlashPath(string configuration, string framework, string fileName, string expected)
        => GeneratedOutputPathContract.BuildProjectRelativePath(configuration, framework, fileName).ShouldBe(expected);

    [Theory]
    [InlineData("..", "generatedFileName must not contain traversal segments.")]
    [InlineData("Foo..g.cs", "generatedFileName must not contain traversal segments.")]
    [InlineData("Foo:bar.g.cs", "generatedFileName must not contain path separators, NUL, NTFS ADS colons, or wildcard characters.")]
    [InlineData("CON.g.cs", "generatedFileName must not begin with a Windows reserved device name ('CON').")]
    [InlineData("nul.g.razor.cs", "generatedFileName must not begin with a Windows reserved device name ('nul').")]
    [Trait("MatrixRowId", "IDE-MUST-003")]
    public void GeneratedOutputPathContract_RejectsHostilePathSegments(string fileName, string messageStart)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(()
            => GeneratedOutputPathContract.BuildProjectRelativePath("Debug", "net10.0", fileName));
        ex.Message.ShouldStartWith(messageStart);
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-001")]
    [Trait("MatrixRowId", "IDE-MUST-003")]
    public void IdeParityCounterFixture_GeneratesDeterministicSymbolsForMatrixRows()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(LoadIdeParityCounterFixtureSource());
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjection.g.razor.cs");
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjectionFeature.g.cs");
        fileNames.ShouldContain("IdeParity.Counter.IdeParityCounterProjectionRegistration.g.cs");
        // Command-derived hint names duplicate the namespace prefix; this is the existing
        // generator behavior (Story 1-x) and must remain stable as part of the IDE parity
        // contract that adopters/IDEs index against.
        fileNames.ShouldContain("IdeParity.Counter.IdeParity.Counter.ConfigureCounterCommand.CommandRenderer.g.razor.cs");
        fileNames.ShouldContain("IdeParity.Counter.IdeParity.Counter.ConfigureCounterCommand.CommandLifecycleBridge.g.cs");

        GeneratedOutputPathContract
            .BuildProjectRelativePath("Debug", "net10.0", "IdeParity.Counter.IdeParityCounterProjection.g.razor.cs")
            .ShouldBe("obj/Debug/net10.0/generated/HexalithFrontComposer/IdeParity.Counter.IdeParityCounterProjection.g.razor.cs");
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-VERSION-001")]
    public void IdeParityCounterFixture_HashIsStableAcrossRuns()
    {
        string hash = ComputeIdeParityCounterFixtureHash();
        hash.ShouldStartWith("sha256:");
        hash.Length.ShouldBe("sha256:".Length + 64);
        ComputeIdeParityCounterFixtureHash().ShouldBe(hash);
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-003")]
    public void EvidencePathNormalization_RejectsTraversalAbsoluteUserAndUnsupportedUriPaths()
    {
        string root = Path.Combine(Path.GetTempPath(), "frontcomposer-ide-parity-root");

        IdeParityEvidencePath
            .TryNormalizeProjectRelativePath(root, "artifacts/ide-parity/evidence/IDE-MUST-001.json", caseSensitive: false, out string normalized)
            .ShouldBeTrue();
        normalized.ShouldBe("artifacts/ide-parity/evidence/IDE-MUST-001.json");

        // Traversal: leading, embedded, trailing.
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "../outside.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "artifacts/../escaped.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "artifacts/foo/..", caseSensitive: false, out _).ShouldBeFalse();

        // Absolute / drive-rooted.
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "C:/Users/Ada/AppData/Local/Temp/evidence.json", caseSensitive: false, out _).ShouldBeFalse();

        // Scheme-prefixed (not just file:/http:/https:).
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "file:///C:/Users/Ada/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "https://example.test/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "javascript:alert(1)", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "data:application/json;base64,e30=", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "mailto:owner@example.test", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "vbscript:msgbox", caseSensitive: false, out _).ShouldBeFalse();

        // UNC-style (forward and back).
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "//attacker/share/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "\\\\attacker\\share\\evidence.json", caseSensitive: false, out _).ShouldBeFalse();

        // Bidi/zero-width chars and BOM.
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "artifacts‮/evidence.json", caseSensitive: false, out _).ShouldBeFalse();
        IdeParityEvidencePath.TryNormalizeProjectRelativePath(root, "﻿artifacts/ide-parity/evidence/IDE-MUST-001.json", caseSensitive: false, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-REMOTE-001")]
    public void EvidencePathNormalization_DefaultCaseSensitivityFollowsRuntime()
    {
        bool expected = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        IdeParityEvidencePath.DefaultCaseSensitivityForFilesystem().ShouldBe(expected);
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-REMOTE-001")]
    public void EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        string root = Path.Combine(Path.GetTempPath(), "frontcomposer-ide-parity-case");
        Directory.CreateDirectory(Path.Combine(root, "artifacts", "ide-parity", "evidence"));

        IdeParityEvidencePath
            .TryNormalizeProjectRelativePath(root, "artifacts/ide-parity/evidence/IDE.json", caseSensitive: true, out string lower)
            .ShouldBeTrue();
        lower.ShouldBe("artifacts/ide-parity/evidence/IDE.json");

        IdeParityEvidencePath
            .TryNormalizeProjectRelativePath(root, "Artifacts/IDE-Parity/Evidence/IDE.json", caseSensitive: true, out _)
            .ShouldBeFalse();
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    [Trait("MatrixRowId", "IDE-MUST-006")]
    public void ReportSanitizer_RedactsControlsSecretsAbsolutePathsAndSpreadsheetFormulas()
    {
        string unsafeValue =
            "=HYPERLINK(\"file:///C:/Users/Ada/AppData/Local/Temp/log.txt\")[31m" +
            " token=ghp_abcdefghijklmnopqrstuvwxyz1234567890" +
            " AKIA0123456789ABCDEF" +
            " bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U" +
            " /home/jpiquot/.ssh/id_rsa /etc/passwd \\\\fileserver\\share\\secret.txt" +
            " machine=BUILD-01";

        string markdown = IdeParityReportSanitizer.Sanitize(unsafeValue, IdeParityReportFormat.Markdown);
        markdown.ShouldNotContain("");
        markdown.ShouldNotContain("C:/Users/Ada");
        markdown.ShouldNotContain("ghp_abcdefghijklmnopqrstuvwxyz1234567890");
        markdown.ShouldNotContain("AKIA0123456789ABCDEF");
        markdown.ShouldNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        markdown.ShouldNotContain("/home/jpiquot");
        markdown.ShouldNotContain("/etc/passwd");
        markdown.ShouldNotContain("\\\\fileserver\\share");
        markdown.ShouldNotContain("BUILD-01");
        markdown.ShouldContain("[redacted-path]");
        markdown.ShouldContain("[redacted-secret]");
        markdown.ShouldContain("[redacted-machine]");

        string csv = IdeParityReportSanitizer.Sanitize(unsafeValue, IdeParityReportFormat.Csv);
        csv.ShouldStartWith("\"'=HYPERLINK");
        csv.ShouldNotContain("file:///");
    }

    [Theory]
    [InlineData("=cmd|/c calc")]
    [InlineData("\t=cmd|/c calc")]
    [InlineData("\r=cmd|/c calc")]
    [InlineData("\n=cmd|/c calc")]
    [InlineData(" =cmd|/c calc")]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void ReportSanitizer_CsvBlocksFormulaInjectionEvenWithLeadingWhitespace(string payload)
    {
        string csv = IdeParityReportSanitizer.Sanitize(payload, IdeParityReportFormat.Csv);
        // The leading-formula guard prepends a single quote. CSV-quoting (enclosing double
        // quotes) only fires when the value contains comma, double-quote, or newline; in
        // that case the final form is `"'<original>"`. Either way the cell content visible
        // to a spreadsheet starts with `'` and not with a formula trigger.
        bool startsWithGuard = csv.StartsWith("'", StringComparison.Ordinal)
            || csv.StartsWith("\"'", StringComparison.Ordinal);
        startsWithGuard.ShouldBeTrue($"Sanitized CSV cell '{csv}' must lead with a single-quote formula guard.");
        csv.ShouldNotMatch("^[=+\\-@\\s]");
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void ReportSanitizer_StripsOsc8HyperlinkRuns()
    {
        const string osc8 = "]8;;https://attacker.testlabel]8;;";
        string sanitized = IdeParityReportSanitizer.Sanitize(osc8, IdeParityReportFormat.Terminal);
        sanitized.ShouldNotContain("");
        sanitized.ShouldNotContain("https://attacker.test");
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void ReportSanitizer_MarkdownEscapesAllHtmlNotJustScript()
    {
        const string payload = "<iframe src=\"x\"></iframe><img src=x onerror=alert(1)>line1\nline2|cell";
        string markdown = IdeParityReportSanitizer.Sanitize(payload, IdeParityReportFormat.Markdown);
        markdown.ShouldNotContain("<iframe", Case.Insensitive);
        markdown.ShouldNotContain("<img", Case.Insensitive);
        markdown.ShouldNotContain("\n");
        markdown.ShouldContain("&lt;iframe");
        markdown.ShouldContain("&lt;img");
        markdown.ShouldContain("\\|");
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void ReportSanitizer_GuardsMaxLengthAndSurrogatePairs()
    {
        Should.Throw<ArgumentOutOfRangeException>(()
            => IdeParityReportSanitizer.Sanitize("anything", IdeParityReportFormat.Terminal, maxLength: 0));

        // 33 chars: 32 ASCII + one surrogate pair (U+1F600 'GRINNING FACE').
        string mixed = new string('A', 32) + "😀";
        string sanitized = IdeParityReportSanitizer.Sanitize(mixed, IdeParityReportFormat.Terminal, maxLength: 33);
        sanitized.ShouldStartWith(new string('A', 32));
        // Either we kept the full pair or we cut before the high surrogate; never an unpaired high surrogate.
        if (sanitized.Length > 32)
        {
            char lastBeforeMarker = sanitized[31];
            char.IsHighSurrogate(lastBeforeMarker).ShouldBeFalse();
        }
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-VERSION-001")]
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
            Fixture: "samples/IdeParityCounter",
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
        issue.Body.ShouldContain("OS/container:");
        issue.Body.ShouldContain("release owner:");
        issue.Body.ShouldContain("Visual Studio calibration");
        issue.Body.ShouldContain("GitHub issue creation unavailable");
        issue.Body.ShouldNotContain("C:/Users/");
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-VERSION-001")]
    public void VersionRevalidation_ProbeUnavailableReturnsBlockingDryRun()
    {
        IdeParityVersionPin supported = new("VS", "17.13", "17.14", "SourceTools");
        IdeParityDetectedVersion detected = new("VS", "17.13.1", "Windows", "samples/IdeParityCounter", ["IDE-MUST-001"], "in range", "in range");

        IdeParityRevalidationIssue offline = IdeParityVersionRevalidator.Resolve(supported, detected, probe: null);
        offline.DryRun.ShouldBeTrue();
        offline.IsBlocking.ShouldBeTrue();

        IdeParityRevalidationIssue authedNoLabels = IdeParityVersionRevalidator.Resolve(supported, detected, new FakeProbe(authenticated: true, hasLabels: false));
        authedNoLabels.DryRun.ShouldBeTrue();

        IdeParityRevalidationIssue live = IdeParityVersionRevalidator.Resolve(supported, detected, new FakeProbe(authenticated: true, hasLabels: true));
        live.DryRun.ShouldBeFalse();
        live.IsBlocking.ShouldBeFalse();
    }

    [Theory]
    [InlineData("v17.13", "Version segment 'v17' must start with a digit.")]
    [InlineData("17.preview", "Version segment 'preview' must start with a digit.")]
    [InlineData("99999999999.0", "Version segment '99999999999' overflows Int32.")]
    [Trait("MatrixRowId", "IDE-VERSION-001")]
    public void VersionRevalidation_FailsClosedOnInvalidVersionSegments(string raw, string expectedMessage)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(()
            => IdeParityVersionRevalidator.CompareVersions(raw, "17.13"));
        ex.Message.ShouldStartWith(expectedMessage);
    }

    private sealed class FakeProbe : IGithubAvailabilityProbe
    {
        private readonly bool _authenticated;
        private readonly bool _hasLabels;

        public FakeProbe(bool authenticated, bool hasLabels)
        {
            _authenticated = authenticated;
            _hasLabels = hasLabels;
        }

        public bool IsAuthenticated() => _authenticated;

        public bool LabelsAccessible(IReadOnlyList<string> labels) => _hasLabels;
    }

    internal static string LoadIdeParityCounterFixtureSource()
    {
        StringBuilder builder = new();
        foreach (string file in IdeParityCounterFixtureSourceFiles())
        {
            _ = builder.Append(File.ReadAllText(file));
            _ = builder.AppendLine();
        }

        return builder.ToString();
    }

    internal static string ComputeIdeParityCounterFixtureHash()
    {
        // Strip CR bytes before hashing so the digest is stable across CRLF/LF checkouts.
        // The repo's .gitattributes uses `* text=auto eol=crlf`; without normalization the
        // manifest hash would diverge between Windows-checkout and Linux-checkout runners.
        using SHA256 sha = SHA256.Create();
        foreach (string file in IdeParityCounterFixtureSourceFiles())
        {
            byte[] raw = File.ReadAllBytes(file);
            int writeIndex = 0;
            byte[] normalized = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] != (byte)'\r')
                {
                    normalized[writeIndex++] = raw[i];
                }
            }

            _ = sha.TransformBlock(normalized, 0, writeIndex, null, 0);
        }

        _ = sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] digest = sha.Hash ?? Array.Empty<byte>();
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static IEnumerable<string> IdeParityCounterFixtureSourceFiles()
    {
        string root = Path.Combine(IdeParityRepositoryRoot.Value, "samples", "IdeParityCounter");
        yield return Path.Combine(root, "IdeParityCounterProjection.cs");
        yield return Path.Combine(root, "ConfigureCounterCommand.cs");
    }
}

internal static class IdeParityRepositoryRoot
{
    public static string Value
    {
        get
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.sln")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                throw new InvalidOperationException("Tests must run under the repository checkout (Hexalith.FrontComposer.sln not found in any ancestor).");
            }

            return directory.FullName;
        }
    }
}
