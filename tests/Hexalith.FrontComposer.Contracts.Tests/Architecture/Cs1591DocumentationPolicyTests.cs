using System.Diagnostics;
using System.Xml.Linq;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class Cs1591DocumentationPolicyTests
{
    private const string ContractsProject = "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj";

    private static readonly string[] _freezeScopes =
    {
        "Attributes",
        "Rendering",
        "Mcp",
        "Conformance",
    };

    [Theory]
    [InlineData("net10.0")]
    [InlineData("netstandard2.0")]
    public async Task ContractsDocumentationPolicy_EffectiveNoWarn_ExcludesCs1591(string targetFramework)
    {
        string root = FindRepoRoot();
        string noWarn = await RunDotnetAsync(
            root,
            TestContext.Current.CancellationToken,
            "msbuild",
            ContractsProject,
            "-p:Configuration=Release",
            $"-p:TargetFramework={targetFramework}",
            "-getProperty:NoWarn").ConfigureAwait(true);

        string[] diagnosticIds = SplitDiagnosticIds(noWarn);
        diagnosticIds.ShouldNotContain("1591");
        diagnosticIds.ShouldNotContain("CS1591");
        diagnosticIds.ShouldContain("1570");
        diagnosticIds.ShouldContain("1572");
        diagnosticIds.ShouldContain("1573");
        diagnosticIds.ShouldContain("1574");
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
            foreach (XElement noWarn in XDocument.Load(file).Descendants().Where(element => element.Name.LocalName == "NoWarn"))
            {
                bool suppressesCs1591 = SplitDiagnosticIds(noWarn.Value)
                    .Any(id => id.Equals("1591", StringComparison.OrdinalIgnoreCase)
                        || id.Equals("CS1591", StringComparison.OrdinalIgnoreCase));

                suppressesCs1591.ShouldBeFalse(
                    $"{Path.GetRelativePath(root, file)} must not suppress CS1591 through NoWarn.");
            }
        }
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
                string sourceDirectory = Path.Combine(temporaryContracts, scope, "Nested");
                _ = Directory.CreateDirectory(sourceDirectory);
                await WriteSpecimenAsync(sourceDirectory, scope, documented: false).ConfigureAwait(true);
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
                negativeOutput.ShouldContain($"'Undocumented{scope}'");
            }

            foreach (string scope in _freezeScopes)
            {
                string sourceDirectory = Path.Combine(temporaryContracts, scope, "Nested");
                await WriteSpecimenAsync(sourceDirectory, scope, documented: true).ConfigureAwait(true);
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

    private static string[] SplitDiagnosticIds(string value)
        => value.Split(
            new[] { ';', ',', ' ', '\r', '\n', '\t' },
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

    private static Task WriteSpecimenAsync(string directory, string scope, bool documented)
    {
        string documentation = documented
            ? $"/// <summary>Documented synthetic symbol for the {scope} freeze scope.</summary>{Environment.NewLine}"
            : string.Empty;
        string source = $"namespace Synthetic;{Environment.NewLine}{documentation}public sealed class Undocumented{scope};{Environment.NewLine}";
        return File.WriteAllTextAsync(
            Path.Combine(directory, $"Undocumented{scope}.cs"),
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
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);

        return (process.ExitCode, stdout + Environment.NewLine + stderr);
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
