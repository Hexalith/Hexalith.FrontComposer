using System.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests;

public sealed class ToolPackagingSmokeTests {
    // Generous per-process budget; CI cold-start pack/install can take ~60s on slow hosts.
    private static readonly TimeSpan PerProcessTimeout = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task DotnetToolPackage_CanInstallAndRunFromLocalManifest() {
        if (FindOnPath("dotnet") is null) {
            // Skip when the SDK is unavailable; this fact is environment-bound.
            return;
        }

        string repositoryRoot = LocateRepositoryRoot();
        string workDirectory = Path.Combine(Path.GetTempPath(), "hfc-cli-tool-smoke", Guid.NewGuid().ToString("N"));
        string packageDirectory = Path.Combine(workDirectory, "packages");
        _ = Directory.CreateDirectory(packageDirectory);

        try {
            // Note: pack runs against the live source tree's obj/bin. This races with concurrent
            // IDE/watch builds; serialize this test against the regression suite or run it
            // standalone. Full source-tree isolation via BaseIntermediateOutputPath conflicts with
            // the existing obj/.../AssemblyAttributes.cs and is intentionally not used here.
            _ = await RunAsync(
                "dotnet",
                [
                    "pack",
                    Path.Combine(repositoryRoot, "src", "Hexalith.FrontComposer.Cli", "Hexalith.FrontComposer.Cli.csproj"),
                    "--configuration",
                    "Release",
                    "--output",
                    packageDirectory,
                    "-p:PackageVersion=0.0.0-test-local",
                ],
                repositoryRoot);

            _ = await RunAsync("dotnet", ["new", "tool-manifest"], workDirectory);
            _ = await RunAsync(
                "dotnet",
                [
                    "tool",
                    "install",
                    "Hexalith.FrontComposer.Cli",
                    "--add-source",
                    packageDirectory,
                    "--version",
                    "0.0.0-test-local",
                ],
                workDirectory);

            ProcessResult help = await RunAsync("dotnet", ["frontcomposer", "--help"], workDirectory);
            help.Output.ShouldContain("frontcomposer inspect");
            help.Output.ShouldContain("frontcomposer migrate");

            string? dnx = FindOnPath("dnx");
            if (dnx is not null) {
                ProcessResult dnxHelp = await RunAsync(
                    dnx,
                    [
                        "Hexalith.FrontComposer.Cli",
                        "--version",
                        "0.0.0-test-local",
                        "--source",
                        packageDirectory,
                        "--",
                        "--help",
                    ],
                    workDirectory);
                dnxHelp.Output.ShouldContain("frontcomposer inspect");
            }
        }
        finally {
            TryDelete(workDirectory);
        }
    }

    private static async Task<ProcessResult> RunAsync(string fileName, string[] arguments, string workingDirectory) {
        ProcessStartInfo startInfo = new(fileName) {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in arguments) {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start " + fileName + ".");
        using CancellationTokenSource timeout = new(PerProcessTimeout);
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            try {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception) {
            }

            throw new TimeoutException(fileName + " did not exit within " + PerProcessTimeout + ".");
        }

        string output = await outputTask.ConfigureAwait(false);
        string error = await errorTask.ConfigureAwait(false);

        process.ExitCode.ShouldBe(0, output + error);
        return new ProcessResult(output, error);
    }

    private static string LocateRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string? FindOnPath(string command) {
        string[] extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';')
            : [string.Empty];
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator)) {
            if (string.IsNullOrWhiteSpace(directory)) {
                continue;
            }

            foreach (string extension in extensions) {
                // Preserve PATHEXT casing as-is; Windows filesystems are case-insensitive so an
                // uppercase `.EXE` resolves the same file as `.exe`.
                string candidate = Path.Combine(directory, command + extension);
                if (File.Exists(candidate)) {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static void TryDelete(string path) {
        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
    }

    private sealed record ProcessResult(string Output, string Error);
}
