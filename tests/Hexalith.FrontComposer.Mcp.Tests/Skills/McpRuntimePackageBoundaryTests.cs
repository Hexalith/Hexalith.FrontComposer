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

    [Fact]
    public async Task PackageValidation_MissingBaseline_FailsWithActionableRestoreDiagnostics() {
        string repositoryRoot = LocateRepositoryRoot();
        ProcessResult buildResult = await RunVersionedReleaseBuildAsync(repositoryRoot).ConfigureAwait(true);
        buildResult.ExitCode.ShouldBe(
            0,
            $"The offline package-validation test requires a deterministic release build.\n{buildResult.Output}");
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"frontcomposer-mcp-package-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(outputDirectory);
        try {
            ProcessResult result = await RunPackAsync(
                repositoryRoot,
                outputDirectory,
                "artifacts-missing-baseline",
                MissingBaselineVersion).ConfigureAwait(true);

            result.ExitCode.ShouldNotBe(0, "Package validation must fail closed when its baseline cannot be restored.");
            result.Output.ShouldContain("baseline", Case.Insensitive);
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
        ProcessResult buildResult = await RunVersionedReleaseBuildAsync(repositoryRoot).ConfigureAwait(true);
        buildResult.ExitCode.ShouldBe(
            0,
            $"The offline package-validation test requires a deterministic release build.\n{buildResult.Output}");
        string restoredPackagesDirectory = LocateRestoredPackagesDirectory();
        string restoredBaselinePackagePath = GetPackagePath(
            restoredPackagesDirectory,
            "Hexalith.FrontComposer.Mcp",
            "3.0.0");
        File.Exists(restoredBaselinePackagePath).ShouldBeTrue(
            "the repository restore must cache the MCP 3.0.0 package-validation baseline before the offline test lane runs.");
        string outputDirectory = Path.Combine(Path.GetTempPath(), $"frontcomposer-mcp-package-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(outputDirectory);
        try {
            ProcessResult result = await RunPackAsync(
                repositoryRoot,
                outputDirectory,
                "artifacts-cold").ConfigureAwait(true);
            result.ExitCode.ShouldBe(
                0,
                $"MCP package validation against the configured 3.0.0 baseline must pass.\n{result.Output}");
            ProcessResult warmCacheResult = await RunPackAsync(
                repositoryRoot,
                outputDirectory,
                "artifacts-warm").ConfigureAwait(true);
            warmCacheResult.ExitCode.ShouldBe(
                0,
                $"MCP package validation must also pass when the isolated baseline cache is warm.\n{warmCacheResult.Output}");

            string packagePath = Directory.EnumerateFiles(outputDirectory, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase));
            string extractedAssemblyPath = Path.Combine(outputDirectory, McpAssemblyFileName);
            using (ZipArchive archive = ZipFile.OpenRead(packagePath)) {
                archive.Entries.ShouldNotContain(
                    item => IsBenchmarkPayloadName(item.FullName),
                    "The MCP package must not ship benchmark prompts under any package-entry name.");
                ZipArchiveEntry entry = archive.Entries.Single(item => string.Equals(
                    item.FullName,
                    $"lib/net10.0/{McpAssemblyFileName}",
                    StringComparison.Ordinal));
                entry.ExtractToFile(extractedAssemblyPath);
            }

            string inspectedAssemblyPath = Directory.EnumerateFiles(
                    Path.Combine(
                        repositoryRoot,
                        "src",
                        "Hexalith.FrontComposer.Mcp",
                        "bin",
                        "Release",
                        "net10.0"),
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
            string[] resourceNames = packagedAssembly.GetManifestResourceNames();
            resourceNames.ShouldNotContain(
                name => IsBenchmarkPayloadName(name),
                "The packaged MCP runtime must contain no benchmark-prompt resource under any logical name.");
            resourceNames.ShouldAllBe(
                name => name.StartsWith("Hexalith.FrontComposer.Mcp.Skills.", StringComparison.Ordinal)
                    && name.EndsWith(".md", StringComparison.Ordinal),
                "Only the approved MCP skill-corpus markdown resources may be embedded in the runtime assembly.");
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
        string artifactsDirectoryName,
        string? packageValidationBaselineVersion = null) {
        string validationIntermediatePath = Path.Combine(outputDirectory, artifactsDirectoryName);
        CopyPackageValidationProjectReferences(repositoryRoot, validationIntermediatePath);
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
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("-p:Version=4.0.0-review.1117c");
        startInfo.ArgumentList.Add("-p:EnableFrontComposerPackageValidation=true");
        startInfo.ArgumentList.Add("-p:NuGetAudit=false");
        startInfo.ArgumentList.Add(
            $"-p:IntermediateOutputPath={validationIntermediatePath}{Path.DirectorySeparatorChar}");
        if (packageValidationBaselineVersion is not null) {
            startInfo.ArgumentList.Add(
                $"-p:FrontComposerPackageValidationBaselineVersion={packageValidationBaselineVersion}");
        }

        return await RunProcessAsync(startInfo).ConfigureAwait(false);
    }

    private static Task<ProcessResult> RunVersionedReleaseBuildAsync(string repositoryRoot) {
        var startInfo = new ProcessStartInfo("dotnet") {
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add("src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("-m:1");
        startInfo.ArgumentList.Add("-p:Version=4.0.0-review.1117c");
        startInfo.ArgumentList.Add("-p:MinVerVersionOverride=4.0.0");
        startInfo.ArgumentList.Add("-p:NuGetAudit=false");
        return RunProcessAsync(startInfo);
    }

    private static async Task<ProcessResult> RunProcessAsync(ProcessStartInfo startInfo) {
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeoutSource.CancelAfter(PackTimeout);
        CancellationToken cancellationToken = timeoutSource.Token;
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        try {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }

        await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
        return new ProcessResult(
            process.ExitCode,
            (await standardOutput.ConfigureAwait(false)) + (await standardError.ConfigureAwait(false)));
    }

    private static Assembly? ResolveFromCurrentProcess(AssemblyLoadContext context, AssemblyName assemblyName)
        => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.Ordinal));

    private static bool IsBenchmarkPayloadName(string name)
        => name.Contains("benchmark", StringComparison.OrdinalIgnoreCase)
            || name.Contains("prompt-set", StringComparison.OrdinalIgnoreCase);

    private static string LocateRestoredPackagesDirectory() {
        string? configuredPackagesDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        string packagesDirectory = string.IsNullOrWhiteSpace(configuredPackagesDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            : configuredPackagesDirectory;
        Directory.Exists(packagesDirectory).ShouldBeTrue(
            $"The restored NuGet package directory '{packagesDirectory}' must exist before package-boundary tests run.");
        return Path.GetFullPath(packagesDirectory);
    }

    private static string GetPackagePath(string packagesDirectory, string packageId, string version)
        => Path.Combine(
            packagesDirectory,
            packageId.ToLowerInvariant(),
            version,
            $"{packageId.ToLowerInvariant()}.{version}.nupkg");

    private static void CopyPackageValidationProjectReferences(
        string repositoryRoot,
        string validationIntermediatePath) {
        string referenceDirectory = Path.Combine(validationIntermediatePath, "ref");
        _ = Directory.CreateDirectory(referenceDirectory);
        foreach (string projectName in new[] {
            "Hexalith.FrontComposer.Contracts",
            "Hexalith.FrontComposer.Schema",
        }) {
            string referenceAssembly = Path.Combine(
                repositoryRoot,
                "src",
                projectName,
                "obj",
                "Release",
                "net10.0",
                "ref",
                $"{projectName}.dll");
            File.Exists(referenceAssembly).ShouldBeTrue(
                $"Build {projectName} in Release before running package-boundary validation.");
            File.Copy(
                referenceAssembly,
                Path.Combine(referenceDirectory, Path.GetFileName(referenceAssembly)),
                overwrite: true);
        }
    }

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
}
