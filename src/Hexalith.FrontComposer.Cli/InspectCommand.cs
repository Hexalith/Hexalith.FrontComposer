using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Cli;

internal static class InspectCommand {
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken) {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        var project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        string configuration = options.Get("configuration", "Debug");
        string? framework = options.Get("framework");
        InspectLoadResult load = await GeneratedOutputLoader.LoadAsync(project.ProjectPath!, configuration, framework, options.Has("build"), options.Has("absolute-paths"), error, cancellationToken)
            .ConfigureAwait(false);
        if (!load.Success) {
            await error.WriteLineAsync(load.Error).ConfigureAwait(false);
            return load.ExitCode;
        }

        InspectReport report = load.Report!;
        string? severity = options.Get("severity");
        if (!string.IsNullOrWhiteSpace(severity)) {
            int minimumSeverity = NormalizeSeverityRank(severity);
            if (minimumSeverity < 0) {
                await error.WriteLineAsync("--severity must be one of hidden, info, warning, or error.").ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            // AC2: `hidden` is the "show everything" level and must include all diagnostics, even
            // sidecar entries whose severity is non-canonical (rank -1, e.g. a malformed sidecar).
            // Higher levels keep strict threshold semantics, so a non-canonical severity stays
            // visible only under `hidden` and never satisfies info/warning/error thresholds.
            report = report with {
                Diagnostics = report.Diagnostics
                    .Where(x => minimumSeverity == 0 || SeverityRank(x.Severity) >= minimumSeverity)
                    .ToArray(),
            };
        }

        string? requestedType = options.Get("type");
        if (!string.IsNullOrWhiteSpace(requestedType)) {
            TypeMatchResult match = TypeMatcher.Filter(report, requestedType);
            if (!match.Success) {
                await error.WriteLineAsync(match.Error).ConfigureAwait(false);
                return match.ExitCode;
            }

            report = match.Report!;
        }

        if (format == "json") {
            string json = JsonSerializer.Serialize(InspectJson.From(report), JsonOptions.Stable);
            await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        else {
            RenderText(report, output);
        }

        if (options.Has("fail-on-warning") && report.Diagnostics.Any(x => x.Severity is "Warning" or "Error")) {
            return ExitCodes.ActionableFindings;
        }

        if (options.Has("fail-on-error") && report.Diagnostics.Any(x => x.Severity is "Error")) {
            return ExitCodes.ActionableFindings;
        }

        return ExitCodes.Success;
    }

    private static int NormalizeSeverityRank(string severity)
        => severity.ToLowerInvariant() switch {
            "hidden" => 0,
            "info" or "information" => 1,
            "warning" => 2,
            "error" => 3,
            _ => -1,
        };

    private static int SeverityRank(string severity)
        => severity switch {
            "Hidden" => 0,
            "Info" => 1,
            "Warning" => 2,
            "Error" => 3,
            _ => -1,
        };

    private static void RenderText(InspectReport report, TextWriter output) {
        output.WriteLine($"Project: {OutputSanitizer.Sanitize(report.ProjectName)}");
        output.WriteLine($"Configuration: {OutputSanitizer.Sanitize(report.Configuration)}");
        output.WriteLine($"Framework: {OutputSanitizer.Sanitize(report.Framework)}");
        output.WriteLine($"Generated files: {report.Files.Count}");
        output.WriteLine($"Forms: {report.Summary.Forms}; Grids: {report.Summary.Grids}; Registrations: {report.Summary.Registrations}; MCP manifests: {report.Summary.McpManifestEntries}; Warnings: {report.Summary.Warnings}; Errors: {report.Summary.Errors}");
        foreach (GeneratedFileInfo file in report.Files) {
            output.WriteLine($"- {file.Family}: {OutputSanitizer.Sanitize(file.RelativePath)}");
        }

        foreach (InspectDiagnostic diagnostic in report.Diagnostics) {
            output.WriteLine($"! {diagnostic.Id} {diagnostic.Severity}: What: {OutputSanitizer.Sanitize(diagnostic.What)} Expected: {OutputSanitizer.Sanitize(diagnostic.Expected)} Got: {OutputSanitizer.Sanitize(diagnostic.Got)} Fix: {OutputSanitizer.Sanitize(diagnostic.Fix)} DocsLink: {OutputSanitizer.Sanitize(diagnostic.DocsLink)}");
        }
    }
}

internal sealed record InspectReport(
    string ProjectName,
    string ProjectRelativePath,
    string Configuration,
    string Framework,
    IReadOnlyList<GeneratedFileInfo> Files,
    IReadOnlyList<InspectDiagnostic> Diagnostics) {
    public InspectSummary Summary => InspectSummary.From(Files, Diagnostics);
}

internal sealed record InspectSummary(
    int GeneratedFiles,
    int Forms,
    int Grids,
    int Registrations,
    int McpManifestEntries,
    int Warnings,
    int Errors) {
    public static InspectSummary From(IReadOnlyList<GeneratedFileInfo> files, IReadOnlyList<InspectDiagnostic> diagnostics)
        => new(
            files.Count,
            files.Count(x => x.Family is GeneratedSourceFamily.CommandForm),
            files.Count(x => x.Family is GeneratedSourceFamily.ProjectionRazor),
            files.Count(x => x.Family is GeneratedSourceFamily.Registration),
            files.Count(x => x.Family is GeneratedSourceFamily.McpManifest),
            diagnostics.Count(x => x.Severity == "Warning"),
            diagnostics.Count(x => x.Severity == "Error"));
}

internal sealed record GeneratedFileInfo(
    string RelativePath,
    string FileName,
    GeneratedSourceFamily Family,
    string? RelatedType);

internal enum GeneratedSourceFamily {
    ProjectionRazor,
    FluxorFeature,
    FluxorActions,
    FluxorReducers,
    Registration,
    CommandForm,
    CommandRenderer,
    CommandLifecycleBridge,
    CommandLastUsedSubscriber,
    CommandPage,
    McpManifest,
    TemplateManifest,
    Unknown,
}

internal sealed record InspectDiagnostic(
    string Id,
    string Severity,
    string? RelatedType,
    string? RelativePath,
    string What,
    string Expected,
    string Got,
    string Fix,
    string DocsLink);

internal sealed record InspectLoadResult(bool Success, InspectReport? Report, string Error, int ExitCode) {
    public static InspectLoadResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static InspectLoadResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}

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

internal sealed record FrameworkSelection(bool Success, string? Framework, string? GeneratedDirectory, string Error, int ExitCode) {
    public static FrameworkSelection Ok(string framework, string generatedDirectory) => new(true, framework, generatedDirectory, string.Empty, ExitCodes.Success);

    public static FrameworkSelection Fail(string error, int exitCode) => new(false, null, null, error, exitCode);
}

internal static class GeneratedFileClassifier {
    private static readonly (string Suffix, GeneratedSourceFamily Family)[] Suffixes = [
        (".CommandForm.g.razor.cs", GeneratedSourceFamily.CommandForm),
        ("CommandForm.g.razor.cs", GeneratedSourceFamily.CommandForm),
        (".CommandRenderer.g.razor.cs", GeneratedSourceFamily.CommandRenderer),
        ("Renderer.g.razor.cs", GeneratedSourceFamily.CommandRenderer),
        ("LifecycleBridge.g.cs", GeneratedSourceFamily.CommandLifecycleBridge),
        ("LastUsedSubscriber.g.cs", GeneratedSourceFamily.CommandLastUsedSubscriber),
        ("Page.g.razor.cs", GeneratedSourceFamily.CommandPage),
        ("LifecycleFeature.g.cs", GeneratedSourceFamily.FluxorFeature),
        ("Feature.g.cs", GeneratedSourceFamily.FluxorFeature),
        ("Actions.g.cs", GeneratedSourceFamily.FluxorActions),
        ("Reducers.g.cs", GeneratedSourceFamily.FluxorReducers),
        ("Registration.g.cs", GeneratedSourceFamily.Registration),
        (".g.razor.cs", GeneratedSourceFamily.ProjectionRazor),
    ];

    public static GeneratedFileInfo Classify(string projectDirectory, string path, bool absolutePaths = false) {
        string fileName = Path.GetFileName(path);
        if (fileName == "FrontComposerMcpManifest.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.McpManifest, null, absolutePaths);
        }

        if (fileName == "__FrontComposerProjectionTemplatesRegistration.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.TemplateManifest, null, absolutePaths);
        }

        foreach ((string suffix, GeneratedSourceFamily family) in Suffixes) {
            if (fileName.EndsWith(suffix, StringComparison.Ordinal)) {
                string related = fileName[..^suffix.Length];
                related = related.TrimEnd('.');
                if (related.EndsWith(".Command", StringComparison.Ordinal)) {
                    related = related[..^".Command".Length];
                }

                return New(path, projectDirectory, fileName, family, string.IsNullOrWhiteSpace(related) ? null : related, absolutePaths);
            }
        }

        return New(path, projectDirectory, fileName, GeneratedSourceFamily.Unknown, null, absolutePaths);
    }

    private static GeneratedFileInfo New(string path, string projectDirectory, string fileName, GeneratedSourceFamily family, string? relatedType, bool absolutePaths)
        => new(absolutePaths ? Path.GetFullPath(path) : PathUtilities.ToProjectRelative(projectDirectory, path), fileName, family, relatedType);
}

internal static class TypeMatcher {
    public static TypeMatchResult Filter(InspectReport report, string requestedType) {
        string sanitized = OutputSanitizer.Sanitize(requestedType);
        string[] known = report.Files
            .Select(x => x.RelatedType)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] exact = known.Where(x => string.Equals(x, requestedType, StringComparison.Ordinal)).ToArray();
        string[] simple = exact.Length == 0
            ? known.Where(x => string.Equals(SimpleName(x), requestedType, StringComparison.Ordinal)).ToArray()
            : [];

        string[] matches = exact.Length > 0 ? exact : simple;
        if (matches.Length == 0) {
            string closest = string.Join(", ", known.OrderBy(x => Distance(x, requestedType)).ThenBy(x => x, StringComparer.Ordinal).Take(5).Select(x => OutputSanitizer.Sanitize(x)));
            return TypeMatchResult.Fail(
                $"Generated output for type '{sanitized}' was not found. Closest known generated type names: {closest}.",
                ExitCodes.InvalidArguments);
        }

        if (matches.Length > 1) {
            return TypeMatchResult.Fail(
                $"Type name '{sanitized}' is ambiguous. Use one of: {string.Join(", ", matches.Select(x => OutputSanitizer.Sanitize(x)))}.",
                ExitCodes.InvalidArguments);
        }

        string match = matches[0];
        InspectReport filtered = report with {
            Files = report.Files.Where(x => x.RelatedType == match || (x.RelatedType is null && x.Family is GeneratedSourceFamily.McpManifest or GeneratedSourceFamily.TemplateManifest)).ToArray(),
            Diagnostics = report.Diagnostics.Where(x => x.RelatedType == match || x.RelatedType is null).ToArray(),
        };
        return TypeMatchResult.Ok(filtered);
    }

    private static string SimpleName(string metadataName) {
        int index = metadataName.LastIndexOf('.');
        return index < 0 ? metadataName : metadataName[(index + 1)..];
    }

    private const int MaxDistanceInputLength = 256;

    private static int Distance(string left, string right) {
        // Bound closest-type suggestions so hostile generated metadata names cannot force
        // quadratic work across unbounded strings. The output remains a hint, not an exact scorer.
        ReadOnlySpan<char> l = left.AsSpan(0, Math.Min(left.Length, MaxDistanceInputLength));
        ReadOnlySpan<char> r = right.AsSpan(0, Math.Min(right.Length, MaxDistanceInputLength));
        int[,] costs = new int[l.Length + 1, r.Length + 1];
        for (int i = 0; i <= l.Length; i++) {
            costs[i, 0] = i;
        }

        for (int j = 0; j <= r.Length; j++) {
            costs[0, j] = j;
        }

        for (int i = 1; i <= l.Length; i++) {
            for (int j = 1; j <= r.Length; j++) {
                int substitution = l[i - 1] == r[j - 1] ? 0 : 1;
                costs[i, j] = Math.Min(
                    Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1),
                    costs[i - 1, j - 1] + substitution);
            }
        }

        return costs[l.Length, r.Length];
    }
}

internal sealed record TypeMatchResult(bool Success, InspectReport? Report, string Error, int ExitCode) {
    public static TypeMatchResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static TypeMatchResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}

internal static class DiagnosticFileReader {
    public static IEnumerable<InspectDiagnostic> Read(string projectDirectory, string generatedDirectory) {
        List<InspectDiagnostic> result = [];
        foreach (string path in Directory.EnumerateFiles(generatedDirectory, "*.diagnostics.json", SearchOption.TopDirectoryOnly)
                     .Order(StringComparer.Ordinal)) {
            try {
                using FileStream stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
                JsonElement root = document.RootElement;
                IEnumerable<JsonElement> entries = root.ValueKind == JsonValueKind.Array
                    ? root.EnumerateArray()
                    : root.TryGetProperty("diagnostics", out JsonElement diagnostics) && diagnostics.ValueKind == JsonValueKind.Array
                        ? diagnostics.EnumerateArray()
                        : [];

                foreach (JsonElement entry in entries) {
                    string id = Get(entry, "id");
                    if (!id.StartsWith("HFC", StringComparison.Ordinal)) {
                        continue;
                    }

                    result.Add(new InspectDiagnostic(
                        OutputSanitizer.Sanitize(id, 32),
                        OutputSanitizer.Sanitize(Get(entry, "severity"), 16),
                        OutputSanitizer.Sanitize(Get(entry, "relatedType"), 160),
                        OutputSanitizer.Sanitize(NormalizePath(Get(entry, "path"), projectDirectory), 240),
                        OutputSanitizer.Sanitize(Get(entry, "what")),
                        OutputSanitizer.Sanitize(Get(entry, "expected")),
                        OutputSanitizer.Sanitize(Get(entry, "got")),
                        OutputSanitizer.Sanitize(Get(entry, "fix")),
                        OutputSanitizer.Sanitize(Get(entry, "docsLink"))));
                }
            }
            catch (JsonException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar JSON could not be parsed."));
            }
            catch (IOException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar could not be read."));
            }
            catch (UnauthorizedAccessException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar could not be read."));
            }
        }

        return result;
    }

    private static InspectDiagnostic SidecarUnreadable(string path, string projectDirectory, string what)
        => new(
            "HFCM0002",
            "Warning",
            null,
            OutputSanitizer.Sanitize(NormalizePath(path, projectDirectory), 240),
            what,
            "Diagnostic sidecars must be valid JSON arrays or { diagnostics: [] } documents.",
            "Sidecar parsing failed.",
            "Re-run the build, or delete the corrupt sidecar.",
            "docs/migrations/index.md");

    private static string Get(JsonElement entry, string property)
        => entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string NormalizePath(string path, string projectDirectory) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        string trimmed = path.Trim();
        if (trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':') {
            return PathUtilities.RedactedPathSentinel;
        }

        if (trimmed.Contains("://", StringComparison.Ordinal)) {
            return PathUtilities.RedactedPathSentinel;
        }

        string fullPath;
        try {
            fullPath = Path.IsPathRooted(trimmed)
                ? trimmed
                : Path.GetFullPath(trimmed, projectDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            return PathUtilities.RedactedPathSentinel;
        }

        return PathUtilities.ToProjectRelative(projectDirectory, fullPath);
    }
}

internal static class InspectJson {
    public static object From(InspectReport report)
        => new {
            schemaVersion = "frontcomposer.cli.inspect.v1",
            project = new {
                name = OutputSanitizer.Sanitize(report.ProjectName),
                path = OutputSanitizer.Sanitize(report.ProjectRelativePath),
                configuration = OutputSanitizer.Sanitize(report.Configuration),
                framework = OutputSanitizer.Sanitize(report.Framework),
            },
            summary = new {
                generatedFiles = report.Summary.GeneratedFiles,
                forms = report.Summary.Forms,
                grids = report.Summary.Grids,
                registrations = report.Summary.Registrations,
                mcpManifestEntries = report.Summary.McpManifestEntries,
                warnings = report.Summary.Warnings,
                errors = report.Summary.Errors,
            },
            // AC21: text and JSON share the same iteration order. `report.Files` is already
            // sorted tri-key (RelatedType -> Family -> RelativePath) at load time, which the
            // text renderer relies on; do not re-sort here.
            generatedFiles = report.Files
                .Select(x => new {
                    path = OutputSanitizer.Sanitize(x.RelativePath),
                    family = x.Family.ToString(),
                    relatedType = OutputSanitizer.Sanitize(x.RelatedType),
                })
                .ToArray(),
            diagnostics = report.Diagnostics.Select(x => new {
                id = x.Id,
                severity = x.Severity,
                relatedType = x.RelatedType,
                path = x.RelativePath,
                what = x.What,
                expected = x.Expected,
                got = x.Got,
                fix = x.Fix,
                docsLink = x.DocsLink,
            }).ToArray(),
        };
}
