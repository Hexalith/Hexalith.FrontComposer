using System.Diagnostics;
using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class InspectCommand
{
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        ProjectSelection project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        string configuration = options.Get("configuration", "Debug");
        string? framework = options.Get("framework");
        InspectLoadResult load = await GeneratedOutputLoader.LoadAsync(project.ProjectPath!, configuration, framework, options.Has("build"), cancellationToken)
            .ConfigureAwait(false);
        if (!load.Success) {
            await error.WriteLineAsync(load.Error).ConfigureAwait(false);
            return load.ExitCode;
        }

        InspectReport report = load.Report!;
        string? severity = options.Get("severity");
        if (!string.IsNullOrWhiteSpace(severity)) {
            string normalizedSeverity = NormalizeSeverity(severity);
            if (normalizedSeverity.Length == 0) {
                await error.WriteLineAsync("--severity must be one of hidden, info, warning, or error.").ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            report = report with {
                Diagnostics = report.Diagnostics
                    .Where(x => string.Equals(x.Severity, normalizedSeverity, StringComparison.OrdinalIgnoreCase))
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

    private static string NormalizeSeverity(string severity)
        => severity.ToLowerInvariant() switch {
            "hidden" => "Hidden",
            "info" or "information" => "Info",
            "warning" => "Warning",
            "error" => "Error",
            _ => string.Empty,
        };

    private static void RenderText(InspectReport report, TextWriter output)
    {
        output.WriteLine($"Project: {OutputSanitizer.Sanitize(report.ProjectName)}");
        output.WriteLine($"Configuration: {OutputSanitizer.Sanitize(report.Configuration)}");
        output.WriteLine($"Framework: {OutputSanitizer.Sanitize(report.Framework)}");
        output.WriteLine($"Generated files: {report.Files.Count}");
        output.WriteLine($"Forms: {report.Summary.Forms}; Grids: {report.Summary.Grids}; Registrations: {report.Summary.Registrations}; MCP manifests: {report.Summary.McpManifestEntries}");
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
    IReadOnlyList<InspectDiagnostic> Diagnostics)
{
    public InspectSummary Summary => InspectSummary.From(Files, Diagnostics);
}

internal sealed record InspectSummary(
    int GeneratedFiles,
    int Forms,
    int Grids,
    int Registrations,
    int McpManifestEntries,
    int Warnings,
    int Errors)
{
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

internal enum GeneratedSourceFamily
{
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

internal sealed record InspectLoadResult(bool Success, InspectReport? Report, string Error, int ExitCode)
{
    public static InspectLoadResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static InspectLoadResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}

internal static class GeneratedOutputLoader
{
    private const string GeneratedRoot = "generated";
    private const string FrontComposerGeneratedRoot = "HexalithFrontComposer";

    public static async Task<InspectLoadResult> LoadAsync(
        string projectPath,
        string configuration,
        string? framework,
        bool build,
        CancellationToken cancellationToken)
    {
        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;

        if (build) {
            int buildExit = await RunBuildAsync(projectFullPath, configuration, framework, cancellationToken).ConfigureAwait(false);
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
                : "Generated output is missing and no obvious FrontComposer annotations were found in project source.";
            return InspectLoadResult.Fail(reason, ExitCodes.GeneratedOutputUnavailable);
        }

        List<GeneratedFileInfo> files = Directory.EnumerateFiles(generatedDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".g.cs", StringComparison.Ordinal) || path.EndsWith(".g.razor.cs", StringComparison.Ordinal))
            .Select(path => GeneratedFileClassifier.Classify(projectDirectory, path))
            .OrderBy(x => x.RelatedType ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(x => x.Family.ToString(), StringComparer.Ordinal)
            .ThenBy(x => x.RelativePath, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0) {
            return InspectLoadResult.Fail(
                "Generated output directory exists but contains no FrontComposer generated files; the generation set is not reported as successful.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        List<InspectDiagnostic> diagnostics = DiagnosticFileReader.Read(projectDirectory, generatedDirectory)
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
        CancellationToken cancellationToken)
    {
        List<string> args = ["build", projectPath, "--configuration", configuration];
        if (!string.IsNullOrWhiteSpace(framework)) {
            args.Add("--framework");
            args.Add(framework);
        }

        string configuredOutput = Path.Combine("obj", configuration, framework ?? "$(TargetFramework)", GeneratedRoot, FrontComposerGeneratedRoot);
        args.Add("-p:EmitCompilerGeneratedFiles=true");
        args.Add("-p:CompilerGeneratedFilesOutputPath=" + configuredOutput);

        ProcessStartInfo start = new("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in args) {
            start.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(start)!;
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static FrameworkSelection SelectFramework(string projectDirectory, string configuration, string? framework)
    {
        string configurationDirectory = Path.Combine(projectDirectory, "obj", configuration);
        if (!Directory.Exists(configurationDirectory)) {
            return FrameworkSelection.Fail(
                $"Generated output for configuration '{OutputSanitizer.Sanitize(configuration)}' is missing. Run with --build or choose a valid --configuration.",
                ExitCodes.GeneratedOutputUnavailable);
        }

        if (!string.IsNullOrWhiteSpace(framework)) {
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

    private static bool ProjectLooksFrontComposerAnnotated(string projectDirectory)
        => Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !PathUtilities.HasExcludedSegment(projectDirectory, path))
            .Take(500)
            .Any(path => {
                string text = File.ReadAllText(path);
                return text.Contains("ProjectionAttribute", StringComparison.Ordinal)
                    || text.Contains("[Projection", StringComparison.Ordinal)
                    || text.Contains("CommandAttribute", StringComparison.Ordinal)
                    || text.Contains("[Command", StringComparison.Ordinal);
            });
}

internal sealed record FrameworkSelection(bool Success, string? Framework, string? GeneratedDirectory, string Error, int ExitCode)
{
    public static FrameworkSelection Ok(string framework, string generatedDirectory) => new(true, framework, generatedDirectory, string.Empty, ExitCodes.Success);

    public static FrameworkSelection Fail(string error, int exitCode) => new(false, null, null, error, exitCode);
}

internal static class GeneratedFileClassifier
{
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

    public static GeneratedFileInfo Classify(string projectDirectory, string path)
    {
        string fileName = Path.GetFileName(path);
        if (fileName == "FrontComposerMcpManifest.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.McpManifest, null);
        }

        if (fileName == "__FrontComposerProjectionTemplatesRegistration.g.cs") {
            return New(path, projectDirectory, fileName, GeneratedSourceFamily.TemplateManifest, null);
        }

        foreach ((string suffix, GeneratedSourceFamily family) in Suffixes) {
            if (fileName.EndsWith(suffix, StringComparison.Ordinal)) {
                string related = fileName[..^suffix.Length];
                related = related.TrimEnd('.');
                if (related.EndsWith(".Command", StringComparison.Ordinal)) {
                    related = related[..^".Command".Length];
                }

                return New(path, projectDirectory, fileName, family, string.IsNullOrWhiteSpace(related) ? null : related);
            }
        }

        return New(path, projectDirectory, fileName, GeneratedSourceFamily.Unknown, null);
    }

    private static GeneratedFileInfo New(string path, string projectDirectory, string fileName, GeneratedSourceFamily family, string? relatedType)
        => new(PathUtilities.ToProjectRelative(projectDirectory, path), fileName, family, relatedType);
}

internal static class TypeMatcher
{
    public static TypeMatchResult Filter(InspectReport report, string requestedType)
    {
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
                ExitCodes.GeneratedOutputUnavailable);
        }

        if (matches.Length > 1) {
            return TypeMatchResult.Fail(
                $"Type name '{sanitized}' is ambiguous. Use one of: {string.Join(", ", matches.Select(x => OutputSanitizer.Sanitize(x)))}.",
                ExitCodes.InvalidArguments);
        }

        string match = matches[0];
        InspectReport filtered = report with {
            Files = report.Files.Where(x => x.RelatedType == match || x.RelatedType is null && x.Family is GeneratedSourceFamily.McpManifest or GeneratedSourceFamily.TemplateManifest).ToArray(),
            Diagnostics = report.Diagnostics.Where(x => x.RelatedType == match || x.RelatedType is null).ToArray(),
        };
        return TypeMatchResult.Ok(filtered);
    }

    private static string SimpleName(string metadataName)
    {
        int index = metadataName.LastIndexOf('.');
        return index < 0 ? metadataName : metadataName[(index + 1)..];
    }

    private static int Distance(string left, string right)
    {
        int length = Math.Min(left.Length, right.Length);
        int distance = Math.Abs(left.Length - right.Length);
        for (int i = 0; i < length; i++) {
            if (left[i] != right[i]) {
                distance++;
            }
        }

        return distance;
    }
}

internal sealed record TypeMatchResult(bool Success, InspectReport? Report, string Error, int ExitCode)
{
    public static TypeMatchResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static TypeMatchResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}

internal static class DiagnosticFileReader
{
    public static IEnumerable<InspectDiagnostic> Read(string projectDirectory, string generatedDirectory)
    {
        foreach (string path in Directory.EnumerateFiles(generatedDirectory, "*.diagnostics.json", SearchOption.TopDirectoryOnly)
                     .Order(StringComparer.Ordinal)) {
            using FileStream stream = File.OpenRead(path);
            using JsonDocument document = JsonDocument.Parse(stream);
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

                yield return new InspectDiagnostic(
                    OutputSanitizer.Sanitize(id, 32),
                    OutputSanitizer.Sanitize(Get(entry, "severity"), 16),
                    OutputSanitizer.Sanitize(Get(entry, "relatedType"), 160),
                    OutputSanitizer.Sanitize(NormalizePath(Get(entry, "path"), projectDirectory), 240),
                    OutputSanitizer.Sanitize(Get(entry, "what")),
                    OutputSanitizer.Sanitize(Get(entry, "expected")),
                    OutputSanitizer.Sanitize(Get(entry, "got")),
                    OutputSanitizer.Sanitize(Get(entry, "fix")),
                    OutputSanitizer.Sanitize(Get(entry, "docsLink")));
            }
        }
    }

    private static string Get(JsonElement entry, string property)
        => entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string NormalizePath(string path, string projectDirectory)
        => string.IsNullOrWhiteSpace(path) ? string.Empty : PathUtilities.ToProjectRelative(projectDirectory, path);
}

internal static class InspectJson
{
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
            generatedFiles = report.Files
                .OrderBy(x => x.RelativePath, StringComparer.Ordinal)
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
