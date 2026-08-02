using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class Cs1591DocumentationPolicyTests
{
    private const string ContractsProject = "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj";
    private static readonly TimeSpan DotnetTimeout = TimeSpan.FromMinutes(2);

    private static readonly string[] _freezeScopes =
    {
        "Attributes",
        "Rendering",
        "Mcp",
        "Conformance",
    };

    private static readonly Regex _pragmaDisableCs1591 = new(
        @"^\s*#pragma\s+warning\s+disable\s+(?<ids>[^/\r\n]+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex _cliCs1591Suppression = new(
        @"(?:/nowarn:|/p:NoWarn=|-p:NoWarn=|--property:NoWarn=)[^\s""']*1591",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Theory]
    [InlineData("net10.0")]
    [InlineData("netstandard2.0")]
    public async Task ContractsDocumentationPolicy_EffectiveNoWarn_ExcludesCs1591(string targetFramework)
    {
        string root = FindRepoRoot();
        string evaluation = await RunDotnetAsync(
            root,
            TestContext.Current.CancellationToken,
            "msbuild",
            ContractsProject,
            "-p:Configuration=Release",
            $"-p:TargetFramework={targetFramework}",
            "-getProperty:NoWarn,WarningsNotAsErrors",
            "-nologo").ConfigureAwait(true);

        (string noWarn, string warningsNotAsErrors) = ParseMsBuildProperties(evaluation, "NoWarn", "WarningsNotAsErrors");

        string[] noWarnIds = SplitDiagnosticIds(noWarn);
        noWarnIds.ShouldNotContain("1591");
        noWarnIds.ShouldNotContain("CS1591");
        noWarnIds.ShouldContain("1570");
        noWarnIds.ShouldContain("1572");
        noWarnIds.ShouldContain("1573");
        noWarnIds.ShouldContain("1574");

        string[] warningsNotAsErrorsIds = SplitDiagnosticIds(warningsNotAsErrors);
        warningsNotAsErrorsIds.ShouldNotContain("1591");
        warningsNotAsErrorsIds.ShouldNotContain("CS1591");
    }

    [Fact]
    public void ContractsDocumentationPolicy_DefaultAndFreezeScopes_AreExplicit()
    {
        string root = FindRepoRoot();
        string editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        GetEditorConfigSection(editorConfig, "[*.cs]")
            .ShouldContain("dotnet_diagnostic.CS1591.severity = none");

        foreach (string scope in _freezeScopes)
        {
            string header = $"[src/Hexalith.FrontComposer.Contracts/{scope}/**.cs]";
            GetEditorConfigSection(editorConfig, header)
                .ShouldContain("dotnet_diagnostic.CS1591.severity = warning");
        }
    }

    [Fact]
    public void ContractsDocumentationPolicy_RootOwnedNoWarnEntries_ExcludeCs1591()
    {
        string root = FindRepoRoot();
        string[] extensions = { ".csproj", ".props", ".targets" };
        string[] excludedSegments = { ".git", "bin", "obj", "node_modules", "references" };

        IEnumerable<string> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)));

        foreach (string file in files)
        {
            foreach (XElement property in XDocument.Load(file).Descendants()
                .Where(element => element.Name.LocalName is "NoWarn" or "WarningsNotAsErrors"))
            {
                bool suppressesCs1591 = SplitDiagnosticIds(property.Value)
                    .Any(id => id.Equals("1591", StringComparison.OrdinalIgnoreCase)
                        || id.Equals("CS1591", StringComparison.OrdinalIgnoreCase));

                suppressesCs1591.ShouldBeFalse(
                    $"{Path.GetRelativePath(root, file)} must not suppress CS1591 through {property.Name.LocalName}.");
            }
        }

        AssertNoCs1591PragmaDisables(root, excludedSegments);
        AssertNoCliCs1591Suppressions(root, excludedSegments);
    }

    [Fact]
    public void ContractsDocumentationPolicy_FreezeScopes_MatchRealRecursiveSourceSets()
    {
        string root = FindRepoRoot();
        string contractsRoot = Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts");

        foreach (string scope in _freezeScopes)
        {
            string scopeDirectory = Path.Combine(contractsRoot, scope);
            Directory.Exists(scopeDirectory).ShouldBeTrue($"Missing freeze-scope directory {scope}.");
            Directory.EnumerateFiles(scopeDirectory, "*.cs", SearchOption.AllDirectories)
                .ShouldNotBeEmpty($"Freeze scope {scope} must match real C# source recursively.");
        }
    }

    [Fact]
    public async Task ContractsDocumentationPolicy_SyntheticBuilds_EnforceEveryScopeOnly()
    {
        string root = FindRepoRoot();
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "fc-doc-policy-" + Guid.NewGuid().ToString("N"));
        string temporarySrc = Path.Combine(temporaryRoot, "src");
        string temporaryContracts = Path.Combine(temporarySrc, "Hexalith.FrontComposer.Contracts");
        string projectPath = Path.Combine(temporaryContracts, "SyntheticContracts.csproj");

        try
        {
            _ = Directory.CreateDirectory(temporaryContracts);
            File.Copy(Path.Combine(root, ".editorconfig"), Path.Combine(temporaryRoot, ".editorconfig"));
            File.Copy(Path.Combine(root, "Directory.Build.props"), Path.Combine(temporaryRoot, "Directory.Build.props"));
            File.Copy(Path.Combine(root, "src", "Directory.Build.props"), Path.Combine(temporarySrc, "Directory.Build.props"));
            await File.WriteAllTextAsync(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                </Project>
                """,
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            foreach (string scope in _freezeScopes)
            {
                string scopeRoot = Path.Combine(temporaryContracts, scope);
                string nestedDirectory = Path.Combine(scopeRoot, "Nested");
                _ = Directory.CreateDirectory(nestedDirectory);
                await WriteSpecimenAsync(scopeRoot, $"{scope}Root", documented: false).ConfigureAwait(true);
                await WriteSpecimenAsync(nestedDirectory, scope, documented: false).ConfigureAwait(true);
            }

            string outsideDirectory = Path.Combine(temporaryContracts, "OutsideFreeze", "Nested");
            _ = Directory.CreateDirectory(outsideDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(outsideDirectory, "UndocumentedOutsideFreeze.cs"),
                "namespace Synthetic; public sealed class UndocumentedOutsideFreeze;",
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            (int negativeExitCode, string negativeOutput) = await RunDotnetResultAsync(
                temporaryRoot,
                TestContext.Current.CancellationToken,
                "build",
                projectPath,
                "-c",
                "Release",
                "-m:1",
                "/nr:false",
                "-p:NuGetAudit=false").ConfigureAwait(true);

            negativeExitCode.ShouldNotBe(0, "Undocumented public symbols in freeze scopes must fail the build.");
            negativeOutput.ShouldContain("error CS1591");
            negativeOutput.ShouldNotContain("'UndocumentedOutsideFreeze'");
            foreach (string scope in _freezeScopes)
            {
                negativeOutput.ShouldContain($"'Undocumented{scope}Root'");
                negativeOutput.ShouldContain($"'Undocumented{scope}'");
            }

            foreach (string scope in _freezeScopes)
            {
                string scopeRoot = Path.Combine(temporaryContracts, scope);
                string nestedDirectory = Path.Combine(scopeRoot, "Nested");
                await WriteSpecimenAsync(scopeRoot, $"{scope}Root", documented: true).ConfigureAwait(true);
                await WriteSpecimenAsync(nestedDirectory, scope, documented: true).ConfigureAwait(true);
            }

            (int positiveExitCode, string positiveOutput) = await RunDotnetResultAsync(
                temporaryRoot,
                TestContext.Current.CancellationToken,
                "build",
                projectPath,
                "-c",
                "Release",
                "--no-restore",
                "--no-incremental",
                "-m:1",
                "/nr:false",
                "-p:NuGetAudit=false").ConfigureAwait(true);

            positiveExitCode.ShouldBe(
                0,
                $"Documented freeze-scope symbols and undocumented out-of-scope symbols must compile.{Environment.NewLine}{positiveOutput}");
            positiveOutput.ShouldNotContain("error CS1591");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static void AssertNoCs1591PragmaDisables(string root, string[] excludedSegments)
    {
        IEnumerable<string> sourceFiles = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)));

        foreach (string file in sourceFiles)
        {
            foreach (Match match in _pragmaDisableCs1591.Matches(File.ReadAllText(file)))
            {
                bool disablesCs1591 = SplitDiagnosticIds(match.Groups["ids"].Value)
                    .Any(id => id.Equals("1591", StringComparison.OrdinalIgnoreCase)
                        || id.Equals("CS1591", StringComparison.OrdinalIgnoreCase));

                disablesCs1591.ShouldBeFalse(
                    $"{Path.GetRelativePath(root, file)} must not disable CS1591 with #pragma warning disable.");
            }
        }
    }

    private static void AssertNoCliCs1591Suppressions(string root, string[] excludedSegments)
    {
        string[] extensions = { ".yml", ".yaml", ".ps1", ".sh", ".py", ".md", ".props", ".targets", ".csproj" };
        IEnumerable<string> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !Path.GetRelativePath(root, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)));

        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(root, file);
            if (relativePath.StartsWith("_bmad-output", StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith("docs", StringComparison.OrdinalIgnoreCase))
            {
                // Story/docs prose may cite the forbidden forms; only executable/config surfaces are sealed.
                continue;
            }

            _cliCs1591Suppression.IsMatch(File.ReadAllText(file)).ShouldBeFalse(
                $"{relativePath} must not suppress CS1591 through a command-line NoWarn/nowarn argument.");
        }
    }

    private static (string NoWarn, string WarningsNotAsErrors) ParseMsBuildProperties(
        string evaluation,
        string noWarnName,
        string warningsNotAsErrorsName)
    {
        // -getProperty:A,B emits either JSON (SDK 8+) or bare values; accept both shapes.
        string trimmed = evaluation.Trim();
        if (trimmed.StartsWith('{'))
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(trimmed);
            System.Text.Json.JsonElement properties = document.RootElement.TryGetProperty("Properties", out System.Text.Json.JsonElement nested)
                ? nested
                : document.RootElement;
            return (
                properties.GetProperty(noWarnName).GetString() ?? string.Empty,
                properties.GetProperty(warningsNotAsErrorsName).GetString() ?? string.Empty);
        }

        // Fallback: sequential -getProperty without JSON returns newline-separated values in request order.
        string[] lines = trimmed.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        lines.Length.ShouldBeGreaterThanOrEqualTo(
            2,
            $"Expected NoWarn and WarningsNotAsErrors values from msbuild evaluation.{Environment.NewLine}{evaluation}");
        return (lines[0], lines[1]);
    }

    private static string[] SplitDiagnosticIds(string value)
        => value.Split(
            [';', ',', ' ', '\r', '\n', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string GetEditorConfigSection(string editorConfig, string header)
    {
        int sectionStart = editorConfig.IndexOf(header, StringComparison.Ordinal);
        sectionStart.ShouldBeGreaterThanOrEqualTo(0, $"Missing EditorConfig section {header}.");

        int nextSection = editorConfig.IndexOf("\n[", sectionStart + header.Length, StringComparison.Ordinal);
        return nextSection < 0
            ? editorConfig[sectionStart..]
            : editorConfig[sectionStart..nextSection];
    }

    private static Task WriteSpecimenAsync(string directory, string symbolSuffix, bool documented)
    {
        string documentation = documented
            ? $"/// <summary>Documented synthetic symbol for the {symbolSuffix} freeze specimen.</summary>{Environment.NewLine}"
            : string.Empty;
        string source = $"namespace Synthetic;{Environment.NewLine}{documentation}public sealed class Undocumented{symbolSuffix};{Environment.NewLine}";
        return File.WriteAllTextAsync(
            Path.Combine(directory, $"Undocumented{symbolSuffix}.cs"),
            source,
            TestContext.Current.CancellationToken);
    }

    private static async Task<string> RunDotnetAsync(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        (int exitCode, string output) = await RunDotnetResultAsync(
            workingDirectory,
            cancellationToken,
            arguments).ConfigureAwait(false);

        exitCode.ShouldBe(
            0,
            $"dotnet {string.Join(' ', arguments)} failed.{Environment.NewLine}{output}");
        return output;
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetResultAsync(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet.");

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DotnetTimeout);
        CancellationToken effectiveToken = timeout.Token;

        try
        {
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(effectiveToken);
            Task<string> stderrTask = process.StandardError.ReadToEndAsync(effectiveToken);
            await process.WaitForExitAsync(effectiveToken).ConfigureAwait(false);
            string stdout = await stdoutTask.ConfigureAwait(false);
            string stderr = await stderrTask.ConfigureAwait(false);

            return (process.ExitCode, stdout + Environment.NewLine + stderr);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            throw;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the HasExited check and Kill.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Process already gone or not killable in this environment.
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
