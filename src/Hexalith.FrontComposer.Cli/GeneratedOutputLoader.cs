using System.ComponentModel;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Cli;

internal static class GeneratedOutputLoader {
    private const string GeneratedRoot = "generated";
    private const string FrontComposerGeneratedRoot = "HexalithFrontComposer";

    public static async Task<InspectLoadResult> LoadAsync(
        string projectPath,
        string configuration,
        string? framework,
        bool build,
        bool absolutePaths,
        TextWriter error,
        CancellationToken cancellationToken) {
        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;

        if (build) {
            int buildExit = await RunBuildAsync(projectFullPath, configuration, framework, error, cancellationToken).ConfigureAwait(false);
            if (buildExit != 0) {
                return InspectLoadResult.Fail(
                    "Build did not complete successfully; generated output may be stale. Re-run with a successful build before inspecting.",
                    ExitCodes.GeneratedOutputUnavailable);
            }
        }

        FrameworkSelection selectedFramework = SelectFramework(projectDirectory, configuration, framework);
        if (!selectedFramework.Success) {
            return InspectLoadResult.Fail(selectedFramework.Error, selectedFramework.ExitCode);
        }

        string generatedDirectory = selectedFramework.GeneratedDirectory!;
        if (!Directory.Exists(generatedDirectory)) {
            string reason = ProjectLooksFrontComposerAnnotated(projectDirectory)
                ? "Generated output is missing. Run 'dotnet build' or pass --build so FrontComposer can emit files."
                : "Generated output is missing and no obvious FrontComposer annotations were found in project source. Run 'dotnet build' or pass --build if generated output is expected.";
            return InspectLoadResult.Fail(reason, ExitCodes.GeneratedOutputUnavailable);
        }

        List<GeneratedFileInfo> files;
        try {
            files = Directory.EnumerateFiles(generatedDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => path.EndsWith(".g.cs", StringComparison.Ordinal) || path.EndsWith(".g.razor.cs", StringComparison.Ordinal))
                .Select(path => GeneratedFileClassifier.Classify(projectDirectory, path, absolutePaths))
                .OrderBy(x => x.RelatedType ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => x.Family.ToString(), StringComparer.Ordinal)
                .ThenBy(x => x.RelativePath, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException) {
            return InspectLoadResult.Fail(
                "Generated output became unavailable while inspecting it. Re-run with --build or retry after the build completes.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        if (files.Count == 0) {
            return InspectLoadResult.Fail(
                "Generated output directory exists but contains no FrontComposer generated files; the generation set is not reported as successful.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        var diagnostics = DiagnosticFileReader.Read(projectDirectory, generatedDirectory)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ThenBy(x => x.RelativePath ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        string relativeProject = PathUtilities.ToProjectRelative(projectDirectory, projectFullPath);
        InspectReport report = new(
            Path.GetFileNameWithoutExtension(projectFullPath),
            relativeProject,
            configuration,
            selectedFramework.Framework!,
            files,
            diagnostics);

        return InspectLoadResult.Ok(report);
    }

    private static async Task<int> RunBuildAsync(
        string projectPath,
        string configuration,
        string? framework,
        TextWriter error,
        CancellationToken cancellationToken) {
        ProcessStartInfo start = new("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in CreateBuildArguments(projectPath, configuration, framework)) {
            start.ArgumentList.Add(arg);
        }

        try {
            using Process process = Process.Start(start)!;
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            try {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                try {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception) {
                }

                throw;
            }

            _ = await standardOutput.ConfigureAwait(false);
            string stderr = await standardError.ConfigureAwait(false);
            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr)) {
                await error.WriteLineAsync(OutputSanitizer.Sanitize(stderr, 4_000)).ConfigureAwait(false);
            }

            return process.ExitCode;
        }
        catch (Win32Exception) {
            return ExitCodes.GeneratedOutputUnavailable;
        }
    }

    internal static IReadOnlyList<string> CreateBuildArguments(string projectPath, string configuration, string? framework) {
        List<string> args = ["build", projectPath, "--configuration", configuration];
        if (!string.IsNullOrWhiteSpace(framework)) {
            args.Add("--framework");
            args.Add(framework);
        }

        args.Add("-p:EmitCompilerGeneratedFiles=true");
        args.Add("-p:CompilerGeneratedFilesOutputPath=" + BuildCompilerGeneratedFilesOutputPath(configuration, framework));
        return args;
    }

    private static string BuildCompilerGeneratedFilesOutputPath(string configuration, string? framework)
        => string.Join(
            "/",
            "obj",
            configuration,
            string.IsNullOrWhiteSpace(framework) ? "$(TargetFramework)" : framework,
            GeneratedRoot,
            FrontComposerGeneratedRoot);

    private static FrameworkSelection SelectFramework(string projectDirectory, string configuration, string? framework) {
        string configurationDirectory = Path.Combine(projectDirectory, "obj", configuration);
        if (!Directory.Exists(configurationDirectory)) {
            return FrameworkSelection.Fail(
                $"Generated output for configuration '{OutputSanitizer.Sanitize(configuration)}' is missing. Run with --build or choose a valid --configuration.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        if (!string.IsNullOrWhiteSpace(framework)) {
            if (!IsValidFrameworkName(framework)) {
                return FrameworkSelection.Fail("--framework must be a target framework moniker, not a path.", ExitCodes.InvalidArguments);
            }

            string generatedDirectory = Path.Combine(configurationDirectory, framework, GeneratedRoot, FrontComposerGeneratedRoot);
            return Directory.Exists(generatedDirectory)
                ? FrameworkSelection.Ok(framework, generatedDirectory)
                : FrameworkSelection.Fail(
                    $"Generated output for framework '{OutputSanitizer.Sanitize(framework)}' is missing. Valid choices: {string.Join(", ", EnumerateFrameworks(configurationDirectory).Select(x => OutputSanitizer.Sanitize(x)))}.",
                    ExitCodes.GeneratedOutputUnavailable);
        }

        string[] frameworks = EnumerateFrameworks(configurationDirectory).ToArray();
        if (frameworks.Length == 0) {
            return FrameworkSelection.Fail("No target framework generated-output folders were found.", ExitCodes.GeneratedOutputUnavailable);
        }

        if (frameworks.Length > 1) {
            return FrameworkSelection.Fail(
                "Generated output is ambiguous; pass --framework. Valid choices: " + string.Join(", ", frameworks.Select(x => OutputSanitizer.Sanitize(x))),
                ExitCodes.InvalidArguments);
        }

        return FrameworkSelection.Ok(frameworks[0], Path.Combine(configurationDirectory, frameworks[0], GeneratedRoot, FrontComposerGeneratedRoot));
    }

    private static IEnumerable<string> EnumerateFrameworks(string configurationDirectory)
        => Directory.EnumerateDirectories(configurationDirectory)
            .Where(path => Directory.Exists(Path.Combine(path, GeneratedRoot, FrontComposerGeneratedRoot)))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Order(StringComparer.Ordinal)!;

    private static bool ProjectLooksFrontComposerAnnotated(string projectDirectory) {
        try {
            return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !PathUtilities.HasExcludedSegment(projectDirectory, path))
                .Take(500)
                .Any(ContainsFrontComposerAttribute);
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    private static bool ContainsFrontComposerAttribute(string path) {
        try {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
            return tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Any(attribute => {
                string name = attribute.Name.ToString();
                return name is "Projection" or "ProjectionAttribute" or "Command" or "CommandAttribute"
                    || name.EndsWith(".Projection", StringComparison.Ordinal)
                    || name.EndsWith(".ProjectionAttribute", StringComparison.Ordinal)
                    || name.EndsWith(".Command", StringComparison.Ordinal)
                    || name.EndsWith(".CommandAttribute", StringComparison.Ordinal);
            });
        }
        catch (IOException) {
            return false;
        }
        catch (UnauthorizedAccessException) {
            return false;
        }
    }

    private static readonly char[] InvalidFrameworkChars = ['/', '\\', ':', ';', '*', '?', '<', '>', '|', '"', '\'', '\0'];

    private static bool IsValidFrameworkName(string framework) {
        if (string.IsNullOrWhiteSpace(framework) || framework.Length > 80 || framework != framework.Trim()) {
            return false;
        }

        if (framework.Contains("..", StringComparison.Ordinal)) {
            return false;
        }

        foreach (char c in framework) {
            if (char.IsControl(c) || Array.IndexOf(InvalidFrameworkChars, c) >= 0) {
                return false;
            }
        }

        return true;
    }
}
