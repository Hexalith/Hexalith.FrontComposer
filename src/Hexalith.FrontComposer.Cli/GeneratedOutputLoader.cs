using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Cli;

internal static class GeneratedOutputLoader {
    private const string GeneratedRoot = "generated";
    private const string FrontComposerGeneratedRoot = "HexalithFrontComposer";
    private static readonly SearchValues<char> InvalidConfigurationChars = SearchValues.Create("/\\:;,%=*?$<>|\"'");

    public static async Task<InspectLoadResult> LoadAsync(
        string projectPath,
        string configuration,
        string? framework,
        bool build,
        bool absolutePaths,
        TextWriter error,
        CancellationToken cancellationToken) {
        if (!IsValidConfigurationName(configuration)) {
            return InspectLoadResult.Fail("--configuration must be a single MSBuild configuration name.", ExitCodes.InvalidArguments);
        }

        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;

        if (build) {
            if (string.IsNullOrWhiteSpace(framework)) {
                // MSBuild does not expand $(TargetFramework) inside a command-line global property.
                var (evaluationExit, properties) = await RunDotnetAsync(
                    ["msbuild", projectFullPath, "-nologo", "-p:Configuration=" + configuration, "-getProperty:TargetFramework,TargetFrameworks"],
                    error, cancellationToken).ConfigureAwait(false);
                if (evaluationExit != 0) {
                    return InspectLoadResult.Fail("Could not evaluate target frameworks. Pass --framework to select one.", ExitCodes.GeneratedOutputUnavailable);
                }

                try {
                    using JsonDocument document = JsonDocument.Parse(properties);
                    JsonElement values = document.RootElement.GetProperty("Properties");
                    framework = values.GetProperty("TargetFramework").GetString();
                    if (string.IsNullOrWhiteSpace(framework)) {
                        string[] frameworks = (values.GetProperty("TargetFrameworks").GetString() ?? string.Empty)
                            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (frameworks.Length != 1) {
                            return InspectLoadResult.Fail("Pass --framework to select a target framework for this build.", ExitCodes.InvalidArguments);
                        }

                        framework = frameworks[0];
                    }
                }
                catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) {
                    return InspectLoadResult.Fail("Could not read the evaluated target frameworks. Pass --framework to select one.", ExitCodes.GeneratedOutputUnavailable);
                }
            }

            if (!IsValidFrameworkName(framework)) {
                return InspectLoadResult.Fail("--framework must be a target framework moniker, not a path.", ExitCodes.InvalidArguments);
            }

            IReadOnlyList<string> buildArguments = CreateBuildArguments(projectFullPath, configuration, framework);
            var (buildExit, _) = await RunDotnetAsync(buildArguments, error, cancellationToken).ConfigureAwait(false);
            if (buildExit != 0) {
                return InspectLoadResult.Fail(
                    "Build did not complete successfully; generated output may be stale. Re-run with a successful build before inspecting.",
                    ExitCodes.GeneratedOutputUnavailable);
            }
        }

        FrameworkSelection selectedFramework;
        string generatedDirectory;
        List<GeneratedFileInfo> files;
        try {
            selectedFramework = SelectFramework(projectDirectory, configuration, framework);
            if (!selectedFramework.Success) {
                return InspectLoadResult.Fail(selectedFramework.Error, selectedFramework.ExitCode);
            }

            generatedDirectory = selectedFramework.GeneratedDirectory!;
            if (!Directory.Exists(generatedDirectory)) {
                string reason = ProjectLooksFrontComposerAnnotated(projectDirectory)
                    ? "Generated output is missing. Run 'dotnet build' or pass --build so FrontComposer can emit files."
                    : "Generated output is missing and no obvious FrontComposer annotations were found in project source. Run 'dotnet build' or pass --build if generated output is expected.";
                return InspectLoadResult.Fail(reason, ExitCodes.GeneratedOutputUnavailable);
            }

            if (build && !EnumerateGeneratedFiles(generatedDirectory).Any()) {
                // A warm SDK build can skip CoreCompile when disk emission is first requested.
                // References were built above, so only the selected project needs recompiling.
                var (rebuildExit, _) = await RunDotnetAsync(
                    [.. CreateBuildArguments(projectFullPath, configuration, selectedFramework.Framework!), "--no-incremental", "--no-dependencies", "--no-restore"],
                    error, cancellationToken).ConfigureAwait(false);
                if (rebuildExit != 0) {
                    return InspectLoadResult.Fail("Could not rebuild the selected project to emit generated files.", ExitCodes.GeneratedOutputUnavailable);
                }
            }

            files = EnumerateGeneratedFiles(generatedDirectory)
                .Select(path => GeneratedFileClassifier.Classify(projectDirectory, path, absolutePaths))
                .OrderBy(x => x.RelatedType ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(x => x.Family.ToString(), StringComparer.Ordinal)
                .ThenBy(x => x.RelativePath, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return InspectLoadResult.Fail(
                "Generated output became unavailable while inspecting it. Re-run with --build or retry after the build completes.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        if (files.Count == 0) {
            return InspectLoadResult.Fail(
                "Generated output directory exists but contains no FrontComposer generated files; the generation set is not reported as successful.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        List<InspectDiagnostic>? diagnostics = DiagnosticFileReader.Read(projectDirectory, generatedDirectory);
        if (diagnostics is null) {
            return InspectLoadResult.Fail(
                "Generated diagnostics directory became unavailable while inspecting it. Re-run with --build or retry after the build completes.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        string relativeProject = PathUtilities.ToProjectRelative(projectDirectory, projectFullPath);
        InspectReport report = new(
            Path.GetFileNameWithoutExtension(projectFullPath),
            relativeProject,
            configuration,
            selectedFramework.Framework!,
            files,
            diagnostics.OrderBy(x => x.Id, StringComparer.Ordinal)
                .ThenBy(x => x.RelativePath ?? string.Empty, StringComparer.Ordinal)
                .ToList());

        return InspectLoadResult.Ok(report);
    }

    private static IEnumerable<string> EnumerateGeneratedFiles(string outputRoot) {
        string compilerDirectory = Path.Combine(outputRoot, "Hexalith.FrontComposer.SourceTools", "Hexalith.FrontComposer.SourceTools.FrontComposerGenerator");
        return new[] { compilerDirectory, outputRoot }
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            .Where(path => path.EndsWith(".g.cs", StringComparison.Ordinal) || path.EndsWith(".g.razor.cs", StringComparison.Ordinal))
            .DistinctBy(Path.GetFileName, StringComparer.Ordinal);
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetAsync(
        IReadOnlyList<string> arguments,
        TextWriter error,
        CancellationToken cancellationToken) {
        ProcessStartInfo start = new("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in arguments) {
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

            string stdout = await standardOutput.ConfigureAwait(false);
            string stderr = await standardError.ConfigureAwait(false);
            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr)) {
                await error.WriteLineAsync(OutputSanitizer.Sanitize(stderr, 4_000), cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }

            return (process.ExitCode, stdout);
        }
        catch (Win32Exception) {
            return (ExitCodes.GeneratedOutputUnavailable, string.Empty);
        }
    }

    internal static IReadOnlyList<string> CreateBuildArguments(string projectPath, string configuration, string framework)
        => [
            "build", projectPath, "--configuration", configuration, "--framework", framework,
            "-p:EmitCompilerGeneratedFiles=true",
            "-p:CompilerGeneratedFilesOutputPath=" + string.Join("/", "obj", configuration, framework, GeneratedRoot, FrontComposerGeneratedRoot),
        ];

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
            .Where(name => name is not null && IsValidFrameworkName(name))
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

    private static bool IsValidConfigurationName(string configuration)
        => !string.IsNullOrWhiteSpace(configuration)
            && configuration == configuration.Trim()
            && !configuration.StartsWith('-')
            && !configuration.EndsWith('.')
            && !configuration.AsSpan().ContainsAny(InvalidConfigurationChars)
            && !configuration.Any(char.IsControl);

    private static readonly char[] InvalidFrameworkChars = ['/', '\\', ':', ';', '*', '?', '$', '<', '>', '|', '"', '\'', '\0'];

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
