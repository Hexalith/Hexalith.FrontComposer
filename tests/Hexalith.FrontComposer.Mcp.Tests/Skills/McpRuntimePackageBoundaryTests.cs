using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using Hexalith.FrontComposer.Mcp.Skills;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

[Trait("Category", "Contract")]
public sealed class McpRuntimePackageBoundaryTests {
    private const string McpAssemblyFileName = "Hexalith.FrontComposer.Mcp.dll";
    private const string MissingBaselineVersion = "9999.0.0-frontcomposer-missing-baseline-6f8d3be41a0e4d46";
    private static readonly TimeSpan PackTimeout = TimeSpan.FromMinutes(2);
    private const string PromptResourceName = "Hexalith.FrontComposer.Mcp.Skills.benchmark-prompts.v1.prompt-set.json";

    [Fact]
    public async Task PackageValidation_MissingBaseline_FailsWithActionableRestoreDiagnostics() {
        string repositoryRoot = LocateRepositoryRoot();
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"frontcomposer-mcp-package-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(outputDirectory);
        try {
            ProcessResult result = await RunPackAsync(
                repositoryRoot,
                outputDirectory,
                MissingBaselineVersion).ConfigureAwait(true);

            result.ExitCode.ShouldNotBe(0, "Package validation must fail closed when its baseline cannot be restored.");
            result.Output.ShouldContain("NU1102");
            result.Output.ShouldContain("Hexalith.FrontComposer.Mcp");
            result.Output.ShouldContain(MissingBaselineVersion);
        }
        finally {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

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
            string baselinePackagePath = Path.Combine(
                outputDirectory,
                "nuget-packages",
                "hexalith.frontcomposer.mcp",
                "3.0.0",
                "hexalith.frontcomposer.mcp.3.0.0.nupkg");
            File.Exists(baselinePackagePath).ShouldBeTrue(
                "the cold-cache pack must restore its configured 3.0.0 validation baseline.");

            ProcessResult warmCacheResult = await RunPackAsync(repositoryRoot, outputDirectory).ConfigureAwait(true);
            warmCacheResult.ExitCode.ShouldBe(
                0,
                $"MCP package validation must also pass when the isolated baseline cache is warm.\n{warmCacheResult.Output}");

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

            string inspectedAssemblyPath = Directory.EnumerateFiles(
                    Path.Combine(outputDirectory, "artifacts", "bin", "Hexalith.FrontComposer.Mcp"),
                    McpAssemblyFileName,
                    SearchOption.AllDirectories)
                .Single();
            File.Exists(inspectedAssemblyPath).ShouldBeTrue();
            File.ReadAllBytes(extractedAssemblyPath).ShouldBe(File.ReadAllBytes(inspectedAssemblyPath));

            WeakReference loadContextReference = InspectPackagedAssembly(extractedAssemblyPath);
            WaitForUnload(loadContextReference);
        }
        finally {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InspectPackagedAssembly(string extractedAssemblyPath) {
        var loadContext = new AssemblyLoadContext("McpRuntimePackageBoundary", isCollectible: true);
        var loadContextReference = new WeakReference(loadContext);
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
            packagedAssembly.GetTypes().ShouldNotContain(
                type => type.Name.StartsWith("SkillBenchmark", StringComparison.Ordinal),
                "The packaged MCP runtime must contain no public, internal, or nested benchmark-harness type.");
            packagedAssembly.GetManifestResourceNames().ShouldNotContain(PromptResourceName);
        }
        finally {
            loadContext.Resolving -= ResolveFromCurrentProcess;
            loadContext.Unload();
        }

        return loadContextReference;
    }

    private static void WaitForUnload(WeakReference loadContextReference) {
        for (int attempt = 0; loadContextReference.IsAlive && attempt < 10; attempt++) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        loadContextReference.IsAlive.ShouldBeFalse(
            "The packaged assembly load context must unload before its scratch directory is deleted.");
    }

    private static async Task<ProcessResult> RunPackAsync(
        string repositoryRoot,
        string outputDirectory,
        string? packageValidationBaselineVersion = null) {
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
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("-p:Version=4.0.0-review.1117c");
        startInfo.ArgumentList.Add("-p:EnableFrontComposerPackageValidation=true");
        startInfo.ArgumentList.Add("-p:NuGetAudit=false");
        startInfo.ArgumentList.Add($"-p:ArtifactsPath={Path.Combine(outputDirectory, "artifacts")}");
        if (packageValidationBaselineVersion is not null) {
            startInfo.ArgumentList.Add(
                $"-p:FrontComposerPackageValidationBaselineVersion={packageValidationBaselineVersion}");
        }
        string packagesDirectory = Path.Combine(outputDirectory, "nuget-packages");
        _ = Directory.CreateDirectory(packagesDirectory);
        startInfo.Environment["NUGET_PACKAGES"] = packagesDirectory;

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet pack.");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeoutSource.CancelAfter(PackTimeout);
        CancellationToken cancellationToken = timeoutSource.Token;
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        try {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(true);
            }

            throw;
        }

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
