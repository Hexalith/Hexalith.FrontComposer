using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;

using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

[Trait("Category", "Contract")]
public sealed class McpRuntimePackageBoundaryTests {
    private const string McpAssemblyFileName = "Hexalith.FrontComposer.Mcp.dll";
    private const string PromptResourceName = "Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json";

    [Fact]
    public async Task PackagedRuntimeAssembly_MatchesInspectedReleaseBoundary() {
        string repositoryRoot = LocateRepositoryRoot();
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"frontcomposer-mcp-package-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(outputDirectory);
        try {
            ProcessResult result = await RunPackAsync(repositoryRoot, outputDirectory).ConfigureAwait(true);
            result.ExitCode.ShouldBe(
                0,
                $"MCP package validation against the configured 3.0.0 baseline must pass.\n{result.Output}");

            string packagePath = Directory.EnumerateFiles(outputDirectory, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase));
            string extractedAssemblyPath = Path.Combine(outputDirectory, McpAssemblyFileName);
            using (ZipArchive archive = ZipFile.OpenRead(packagePath)) {
                ZipArchiveEntry entry = archive.Entries.Single(item => string.Equals(
                    item.FullName,
                    $"lib/net10.0/{McpAssemblyFileName}",
                    StringComparison.Ordinal));
                entry.ExtractToFile(extractedAssemblyPath);
            }

            string inspectedAssemblyPath = Path.Combine(
                repositoryRoot,
                "src",
                "Hexalith.FrontComposer.Mcp",
                "bin",
                "Release",
                "net10.0",
                McpAssemblyFileName);
            File.Exists(inspectedAssemblyPath).ShouldBeTrue();
            File.ReadAllBytes(extractedAssemblyPath).ShouldBe(File.ReadAllBytes(inspectedAssemblyPath));

            var loadContext = new AssemblyLoadContext("McpRuntimePackageBoundary", isCollectible: true);
            loadContext.Resolving += ResolveFromCurrentProcess;
            try {
                Assembly packagedAssembly = loadContext.LoadFromAssemblyPath(extractedAssemblyPath);
                packagedAssembly.Location.ShouldBe(extractedAssemblyPath);
                string[] exportedSkillTypes = packagedAssembly.GetExportedTypes()
                    .Where(type => string.Equals(
                        type.Namespace,
                        "Hexalith.FrontComposer.Mcp.Skills",
                        StringComparison.Ordinal))
                    .Select(type => type.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

                exportedSkillTypes.ShouldBe(
                    SkillTypeOrganizationGovernanceTests.RuntimeTypeNames.OrderBy(name => name, StringComparer.Ordinal));
                exportedSkillTypes.ShouldNotContain(name => name.StartsWith("SkillBenchmark", StringComparison.Ordinal));
                packagedAssembly.GetManifestResourceNames().ShouldNotContain(PromptResourceName);
            }
            finally {
                loadContext.Resolving -= ResolveFromCurrentProcess;
                loadContext.Unload();
            }
        }
        finally {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static async Task<ProcessResult> RunPackAsync(string repositoryRoot, string outputDirectory) {
        var startInfo = new ProcessStartInfo("dotnet") {
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add("src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("-p:Version=4.0.0-review.1117c");
        startInfo.ArgumentList.Add("-p:EnableFrontComposerPackageValidation=true");
        startInfo.ArgumentList.Add("-p:NuGetAudit=false");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet pack.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.WhenAll(standardOutput, standardError).ConfigureAwait(true);
        return new ProcessResult(
            process.ExitCode,
            (await standardOutput.ConfigureAwait(true)) + (await standardError.ConfigureAwait(true)));
    }

    private static Assembly? ResolveFromCurrentProcess(AssemblyLoadContext context, AssemblyName assemblyName)
        => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.Ordinal));

    private static string LocateRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FrontComposer repository root.");
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
