using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class AppHostNuGetAuditPolicyTests {
    private static readonly string[] ForbiddenAuditWarningCodes = ["NU1901", "NU1902", "NU1903", "NU1904"];

    private static readonly string[] RequiredRationaleMetadata = [
        "AffectedPackage",
        "AffectedVersion",
        "Applicability",
        "Owner",
        "DecisionDate",
        "ReviewDate",
        "RemediationUrl",
    ];

    private static readonly Regex AdvisoryUrlPattern = new(
        @"\Ahttps://github\.com/advisories/GHSA-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{4}\z",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    [Fact]
    public void AppHostAuditPolicy_EffectiveProjectAndImports_HaveNoFamilySuppressionAndNonEmptyGraph() {
        string root = RepositoryRoot();
        string project = AppHostProject(root);
        string evaluation = RunProcess(root, "dotnet", [
            "msbuild",
            project,
            "-p:Configuration=Release",
            "-getProperty:NoWarn,WarningsNotAsErrors,NuGetAudit,NuGetAuditMode,TreatWarningsAsErrors",
            "-getItem:PackageReference,ProjectReference",
            "-nologo",
        ]);

        using JsonDocument evaluated = JsonDocument.Parse(evaluation);
        JsonElement properties = evaluated.RootElement.GetProperty("Properties");
        foreach (string propertyName in new[] { "NoWarn", "WarningsNotAsErrors" }) {
            IReadOnlyCollection<string> warningCodes = SplitWarningCodes(properties.GetProperty(propertyName).GetString());
            foreach (string forbiddenCode in ForbiddenAuditWarningCodes) {
                warningCodes.ShouldNotContain(
                    forbiddenCode,
                    $"AppHost effective {propertyName} must not hide the {forbiddenCode} vulnerability family.");
            }
        }

        properties.GetProperty("NuGetAudit").GetString().ShouldBe("true");
        properties.GetProperty("NuGetAuditMode").GetString().ShouldBe("all");
        properties.GetProperty("TreatWarningsAsErrors").GetString().ShouldBe("true");

        JsonElement items = evaluated.RootElement.GetProperty("Items");
        int graphItems = items.GetProperty("PackageReference").GetArrayLength()
            + items.GetProperty("ProjectReference").GetArrayLength();
        graphItems.ShouldBeGreaterThan(
            0,
            "the effective audit-policy check must cover a non-empty AppHost package/project graph.");

        string preprocessedProject = PreprocessProject(root, project);
        try {
            XDocument importedGraph = XDocument.Load(preprocessedProject);
            foreach (XElement property in importedGraph
                .Descendants()
                .Where(static element =>
                    element.Name.LocalName is "NoWarn" or "WarningsNotAsErrors")) {
                IReadOnlyCollection<string> warningCodes = SplitWarningCodes(property.Value);
                foreach (string forbiddenCode in ForbiddenAuditWarningCodes) {
                    warningCodes.ShouldNotContain(
                        forbiddenCode,
                        $"AppHost or an imported props/targets file must not declare {forbiddenCode} in {property.Name.LocalName}.");
                }
            }
        }
        finally {
            File.Delete(preprocessedProject);
        }
    }

    [Fact]
    public void AppHostAuditPolicy_ActualAdvisorySuppressions_AreUniqueExactAndReviewable() {
        string root = RepositoryRoot();
        string evaluation = RunProcess(root, "dotnet", [
            "msbuild",
            AppHostProject(root),
            "-p:Configuration=Release",
            "-getItem:NuGetAuditSuppress",
            "-nologo",
        ]);

        using JsonDocument evaluated = JsonDocument.Parse(evaluation);
        XElement[] suppressions = evaluated.RootElement
            .GetProperty("Items")
            .GetProperty("NuGetAuditSuppress")
            .EnumerateArray()
            .Select(static item => new XElement(
                "NuGetAuditSuppress",
                new XAttribute("Include", item.GetProperty("Identity").GetString() ?? string.Empty),
                RequiredRationaleMetadata.Select(metadataName => new XElement(
                    metadataName,
                    item.TryGetProperty(metadataName, out JsonElement value)
                        ? value.GetString()
                        : string.Empty))))
            .ToArray();

        ValidateSuppressionPolicy(suppressions).ShouldBeEmpty();
    }

    [Fact]
    public void AppHostAuditPolicy_SyntheticInvalidSuppressions_FailClosed() {
        (string Xml, string ExpectedFailure)[] invalidCases = [
            (
                ReviewedSuppressionXml("https://example.test/GHSA-2345-6789-cfgh"),
                "exact GitHub advisory URL"),
            (
                $"<Project><ItemGroup>{ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh")}{ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh")}</ItemGroup></Project>",
                "duplicate"),
            (
                ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh")
                    .Replace("<Owner>Security/Release Owner</Owner>", "", StringComparison.Ordinal),
                "Owner"),
            (
                ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh")
                    .Replace("<ReviewDate>2026-08-16</ReviewDate>", "<ReviewDate>2026-06-16</ReviewDate>", StringComparison.Ordinal),
                "before its decision date"),
            (
                ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh")
                    .Replace("https://github.com/example/remediation/issues/42", "not-a-link", StringComparison.Ordinal),
                "RemediationUrl"),
        ];

        foreach ((string xml, string expectedFailure) in invalidCases) {
            XDocument document = XDocument.Parse(xml);
            string[] violations = ValidateSuppressionPolicy(
                document.Descendants().Where(static element => element.Name.LocalName == "NuGetAuditSuppress"));
            violations.ShouldNotBeEmpty($"the synthetic invalid suppression must fail closed: {expectedFailure}");
            string.Join(Environment.NewLine, violations).ShouldContain(expectedFailure);
        }
    }

    [Fact]
    public void AppHostAuditPolicy_SyntheticReviewedAdvisory_Passes() {
        XDocument document = XDocument.Parse(
            ReviewedSuppressionXml("https://github.com/advisories/GHSA-2345-6789-cfgh"));

        ValidateSuppressionPolicy(
            document.Descendants().Where(static element => element.Name.LocalName == "NuGetAuditSuppress"))
            .ShouldBeEmpty();
    }

    private static string AppHostProject(string root) => Path.Combine(
        root,
        "src",
        "Hexalith.FrontComposer.AppHost",
        "Hexalith.FrontComposer.AppHost.csproj");

    private static string PreprocessProject(string root, string project) {
        string preprocessedProject = Path.Combine(
            Path.GetTempPath(),
            $"frontcomposer-apphost-audit-{Guid.NewGuid():N}.xml");
        RunProcess(root, "dotnet", [
            "msbuild",
            project,
            $"-preprocess:{preprocessedProject}",
            "-p:Configuration=Release",
            "-nologo",
        ]);
        File.Exists(preprocessedProject).ShouldBeTrue("MSBuild must emit the preprocessed AppHost import graph.");
        return preprocessedProject;
    }

    private static string[] ValidateSuppressionPolicy(IEnumerable<XElement> suppressionItems) {
        XElement[] suppressions = suppressionItems.ToArray();
        List<string> violations = [];
        HashSet<string> advisoryUrls = new(StringComparer.OrdinalIgnoreCase);

        foreach (XElement suppression in suppressions) {
            string advisoryUrl = suppression.Attribute("Include")?.Value.Trim() ?? string.Empty;
            if (!AdvisoryUrlPattern.IsMatch(advisoryUrl)) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' must use an exact GitHub advisory URL.");
            }

            if (!advisoryUrls.Add(advisoryUrl)) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' is a duplicate advisory exception.");
            }

            foreach (string metadataName in RequiredRationaleMetadata) {
                string value = suppression
                    .Elements()
                    .SingleOrDefault(element => element.Name.LocalName == metadataName)
                    ?.Value.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value)) {
                    violations.Add($"NuGetAuditSuppress '{advisoryUrl}' requires {metadataName} rationale metadata.");
                }
            }

            string decisionDateText = Metadata(suppression, "DecisionDate");
            string reviewDateText = Metadata(suppression, "ReviewDate");
            bool hasDecisionDate = DateOnly.TryParseExact(
                decisionDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly decisionDate);
            bool hasReviewDate = DateOnly.TryParseExact(
                reviewDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly reviewDate);
            if (!hasDecisionDate) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' DecisionDate must use yyyy-MM-dd.");
            }

            if (!hasReviewDate) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' ReviewDate must use yyyy-MM-dd.");
            }
            else if (hasDecisionDate && reviewDate < decisionDate) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' ReviewDate is before its decision date.");
            }

            string remediationUrl = Metadata(suppression, "RemediationUrl");
            if (!Uri.TryCreate(remediationUrl, UriKind.Absolute, out Uri? remediation)
                || remediation.Scheme is not ("http" or "https")) {
                violations.Add($"NuGetAuditSuppress '{advisoryUrl}' RemediationUrl must be an absolute HTTP(S) link.");
            }
        }

        return [.. violations];
    }

    private static string Metadata(XElement suppression, string metadataName) => suppression
        .Elements()
        .SingleOrDefault(element => element.Name.LocalName == metadataName)
        ?.Value.Trim() ?? string.Empty;

    private static IReadOnlyCollection<string> SplitWarningCodes(string? value) => (value ?? string.Empty)
        .Split([';', ',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ReviewedSuppressionXml(string advisoryUrl) => $$"""
        <NuGetAuditSuppress Include="{{advisoryUrl}}">
          <AffectedPackage>Example.Package</AffectedPackage>
          <AffectedVersion>1.2.3</AffectedVersion>
          <Applicability>Reviewed and temporarily applicable to the AppHost dependency graph.</Applicability>
          <Owner>Security/Release Owner</Owner>
          <DecisionDate>2026-07-16</DecisionDate>
          <ReviewDate>2026-08-16</ReviewDate>
          <RemediationUrl>https://github.com/example/remediation/issues/42</RemediationUrl>
        </NuGetAuditSuppress>
        """;

    private static string RunProcess(string workingDirectory, string fileName, IReadOnlyList<string> arguments) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start().ShouldBeTrue($"failed to start {fileName}.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, $"{fileName} {string.Join(' ', arguments)} failed. stdout={output} stderr={error}");
        return output;
    }

    private static string RepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
