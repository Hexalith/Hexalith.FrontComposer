using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationCommand
{
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        bool apply = options.Has("apply");
        bool dryRun = options.Has("dry-run") || !apply;
        if (apply && options.Has("dry-run")) {
            await error.WriteLineAsync("Choose either --dry-run or --apply, not both.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        string? from = options.Get("from");
        string? to = options.Get("to");
        MigrationEdge? edge = MigrationCatalog.Resolve(from, to);
        if (edge is null) {
            await error.WriteLineAsync(MigrationCatalog.UnsupportedMessage(from, to)).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        ProjectSelection project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        MigrationPlan plan = await MigrationPlanner.PlanAsync(project.ProjectPath!, edge, cancellationToken).ConfigureAwait(false);
        MigrationResult result = apply
            ? await MigrationApplier.ApplyAsync(plan, cancellationToken).ConfigureAwait(false)
            : MigrationResult.FromPlan(plan, applied: false);

        if (format == "json") {
            string json = JsonSerializer.Serialize(MigrationJson.From(result), JsonOptions.Stable);
            await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        else {
            RenderText(result, output);
        }

        if (result.Summary.Failed > 0) {
            return ExitCodes.ApplyWriteFailure;
        }

        return options.Has("fail-on-findings") && result.Summary.Changed + result.Summary.ManualOnly + result.Summary.Conflicts > 0
            ? ExitCodes.ActionableFindings
            : ExitCodes.Success;
    }

    private static void RenderText(MigrationResult result, TextWriter output)
    {
        output.WriteLine(result.Applied ? "Migration apply completed." : "Migration dry-run completed.");
        output.WriteLine($"Changed: {result.Summary.Changed}; Unchanged: {result.Summary.Unchanged}; Skipped: {result.Summary.Skipped}; Failed: {result.Summary.Failed}; Manual-only: {result.Summary.ManualOnly}; Conflicts: {result.Summary.Conflicts}");
        foreach (MigrationEntry entry in result.Entries.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.DiagnosticId, StringComparer.Ordinal)) {
            output.WriteLine($"- {entry.Kind} {entry.DiagnosticId} {OutputSanitizer.Sanitize(entry.Path)}: {OutputSanitizer.Sanitize(entry.What)}");
            if (!string.IsNullOrWhiteSpace(entry.Diff)) {
                output.WriteLine(OutputSanitizer.Sanitize(entry.Diff, 2_000));
            }
        }
    }
}

internal sealed record MigrationEdge(string FromVersion, string ToVersion, string DocsLink);

internal static class MigrationCatalog
{
    private static readonly MigrationEdge[] Edges = [
        new("9.1.0", "9.2.0", "docs/migrations/9.1-to-9.2.md"),
    ];

    public static MigrationEdge? Resolve(string? from, string? to)
        => Edges.FirstOrDefault(edge => string.Equals(edge.FromVersion, from, StringComparison.Ordinal)
            && string.Equals(edge.ToVersion, to, StringComparison.Ordinal));

    public static string UnsupportedMessage(string? from, string? to)
        => "Unsupported FrontComposer migration edge '"
            + OutputSanitizer.Sanitize(from)
            + "' -> '"
            + OutputSanitizer.Sanitize(to)
            + "'. Supported edges: "
            + string.Join(", ", Edges.Select(edge => edge.FromVersion + " -> " + edge.ToVersion))
            + ". DocsLink: docs/migrations/index.md.";
}

internal sealed record MigrationPlan(
    string ProjectDirectory,
    MigrationEdge Edge,
    IReadOnlyList<PlannedFileEdit> FileEdits,
    IReadOnlyList<MigrationEntry> Entries)
{
    public MigrationSummary Summary => MigrationSummary.From(Entries);
}

internal sealed record PlannedFileEdit(
    string FullPath,
    string CanonicalPath,
    string RelativePath,
    SourceFileContent OriginalContent,
    string UpdatedText,
    string OriginalHash,
    IReadOnlyList<MigrationEntry> Entries);

internal sealed record SourceFileContent(string Text, Encoding Encoding);

internal sealed record MigrationEntry(
    string DiagnosticId,
    string Kind,
    string Path,
    string What,
    string Expected,
    string Got,
    string Fix,
    string DocsLink,
    string? Diff,
    bool FormattingApplied = false);

internal sealed record MigrationResult(bool Applied, IReadOnlyList<MigrationEntry> Entries, MigrationSummary Summary)
{
    public static MigrationResult FromPlan(MigrationPlan plan, bool applied)
        => new(applied, plan.Entries, MigrationSummary.From(plan.Entries));
}

internal sealed record MigrationSummary(
    int Changed,
    int Unchanged,
    int Skipped,
    int Failed,
    int ManualOnly,
    int Conflicts)
{
    public static MigrationSummary From(IReadOnlyList<MigrationEntry> entries)
        => new(
            entries.Count(x => x.Kind == "safe-fix"),
            entries.Count(x => x.Kind == "unchanged"),
            entries.Count(x => x.Kind == "skipped"),
            entries.Count(x => x.Kind == "failed"),
            entries.Count(x => x.Kind == "manual-only"),
            entries.Count(x => x.Kind == "conflict"));
}

internal static class MigrationPlanner
{
    private static readonly MetadataReference[] References = [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    ];

    public static async Task<MigrationPlan> PlanAsync(string projectPath, MigrationEdge edge, CancellationToken cancellationToken)
    {
        ProjectDocumentSet documentSet = ProjectDocumentLoader.Load(projectPath);
        HashSet<string> submoduleRoots = SubmoduleBoundaryReader.Read(documentSet.ProjectDirectory);
        List<MigrationEntry> entries = [];
        List<PlannedFileEdit> fileEdits = [];

        MefHostServices host = MefHostServices.Create(MefHostServices.DefaultAssemblies
            .Concat([typeof(CSharpCompilation).Assembly, typeof(CSharpFormattingOptions).Assembly])
            .Distinct());
        using AdhocWorkspace workspace = new(host);
        ProjectId projectId = ProjectId.CreateNewId(debugName: Path.GetFileNameWithoutExtension(projectPath));
        ProjectInfo projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            Path.GetFileNameWithoutExtension(projectPath),
            Path.GetFileNameWithoutExtension(projectPath),
            LanguageNames.CSharp,
            filePath: projectPath,
            parseOptions: CSharpParseOptions.Default,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: References);
        Solution solution = workspace.CurrentSolution.AddProject(projectInfo);
        Dictionary<DocumentId, ProjectDocument> documentsById = [];

        foreach (ProjectDocument document in documentSet.Documents.OrderBy(x => x.RelativePath, StringComparer.Ordinal)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!WriteSafetyPolicy.IsAllowed(documentSet.ProjectDirectory, document.FullPath, submoduleRoots)) {
                entries.Add(new MigrationEntry(
                    "HFCM0000",
                    "skipped",
                    document.RelativePath,
                    "Excluded project document was not scanned or written.",
                    "Only explicit project source documents outside generated, bin, obj, package cache, and submodule paths are eligible.",
                    "Excluded path.",
                    "Move source into the selected project or apply the migration manually.",
                    edge.DocsLink,
                    null));
                continue;
            }

            SourceFileContent content;
            try {
                content = await SourceFile.ReadAsync(document.FullPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                entries.Add(Failed(document.RelativePath, edge, "Source file could not be read during migration planning."));
                continue;
            }

            DocumentId documentId = DocumentId.CreateNewId(projectId, document.RelativePath);
            solution = solution.AddDocument(documentId, document.RelativePath, SourceText.From(content.Text, content.Encoding), filePath: document.FullPath);
            documentsById.Add(documentId, document with { Content = content });
        }

        if (!workspace.TryApplyChanges(solution)) {
            entries.Add(Failed(Path.GetFileName(projectPath), edge, "Migration workspace could not be initialized."));
            return new MigrationPlan(documentSet.ProjectDirectory, edge, [], entries);
        }

        FrontComposerMigrationCodeFixProvider provider = new();
        foreach ((DocumentId documentId, ProjectDocument projectDocument) in documentsById.OrderBy(x => x.Value.RelativePath, StringComparer.Ordinal)) {
            cancellationToken.ThrowIfCancellationRequested();
            Document document = workspace.CurrentSolution.GetDocument(documentId)!;
            SourceText originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            ImmutableArray<Diagnostic> diagnostics = await MigrationDiagnosticScanner.ScanAsync(document, cancellationToken).ConfigureAwait(false);
            List<MigrationEntry> fileEntries = [];

            foreach (Diagnostic manual in diagnostics.Where(x => x.Id == MigrationDiagnostics.ManualMigration.Id).OrderBy(x => x.Location.SourceSpan.Start)) {
                fileEntries.Add(new MigrationEntry(
                    manual.Id,
                    "manual-only",
                    projectDocument.RelativePath,
                    "Customization-sensitive FrontComposer API requires manual migration.",
                    "Developer reviews customization semantics before changing behavior.",
                    "A manual-only migration diagnostic was found.",
                    "Review the migration guide and update the affected call site manually.",
                    edge.DocsLink,
                    null));
            }

            List<TextChange> plannedChanges = [];
            List<MigrationEntry> fixEntries = [];
            bool unsupportedOperation = false;
            foreach (Diagnostic diagnostic in diagnostics.Where(x => provider.FixableDiagnosticIds.Contains(x.Id)).OrderBy(x => x.Location.SourceSpan.Start)) {
                List<CodeAction> actions = [];
                CodeFixContext context = new(
                    document,
                    diagnostic.Location.SourceSpan,
                    ImmutableArray.Create(diagnostic),
                    (action, _) => actions.Add(action),
                    cancellationToken);
                await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                CodeAction? action = actions.OrderBy(x => x.EquivalenceKey, StringComparer.Ordinal).FirstOrDefault();
                if (action is null) {
                    fileEntries.Add(ManualOnly(diagnostic, projectDocument.RelativePath, edge));
                    continue;
                }

                ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                if (!TryExtractDocumentChanges(workspace.CurrentSolution, operations, documentId, documentSet.ProjectDirectory, projectDocument.FullPath, out SourceText? changedText)) {
                    unsupportedOperation = true;
                    fileEntries.Add(ManualOnly(diagnostic, projectDocument.RelativePath, edge));
                    continue;
                }

                foreach (TextChange change in changedText!.GetTextChanges(originalText)) {
                    plannedChanges.Add(change);
                }
            }

            if (plannedChanges.Count == 0) {
                entries.AddRange(fileEntries);
                continue;
            }

            if (HasOverlappingChanges(plannedChanges)) {
                fileEntries.Add(new MigrationEntry(
                    "HFCM0004",
                    "conflict",
                    projectDocument.RelativePath,
                    "Multiple migration fixes produced overlapping edits.",
                    "Fixes compose through non-overlapping Roslyn document changes.",
                    "Overlapping edits were detected.",
                    "Apply one migration manually or simplify the file and rerun.",
                    edge.DocsLink,
                    null));
                entries.AddRange(fileEntries);
                continue;
            }

            SourceText updatedText = originalText.WithChanges(plannedChanges.OrderBy(x => x.Span.Start));
            string updated = updatedText.ToString();
            string diff = UnifiedDiff.Create(projectDocument.RelativePath, originalText.ToString(), updated);
            fixEntries.Add(new MigrationEntry(
                MigrationDiagnostics.ObsoleteDevOverlay.Id,
                "safe-fix",
                projectDocument.RelativePath,
                "Obsolete development overlay registration API was found.",
                FrontComposerMigrationCodeFixProvider.ReplacementApi + " is used for Story 6-5 dev-mode overlay registration.",
                FrontComposerMigrationCodeFixProvider.ObsoleteApi + " call.",
                "Replace " + FrontComposerMigrationCodeFixProvider.ObsoleteApi + " with " + FrontComposerMigrationCodeFixProvider.ReplacementApi + ".",
                edge.DocsLink,
                diff,
                FormattingApplied: false));

            fileEntries.AddRange(fixEntries);
            entries.AddRange(fileEntries);
            fileEdits.Add(new PlannedFileEdit(
                projectDocument.FullPath,
                PathUtilities.Canonical(projectDocument.FullPath),
                projectDocument.RelativePath,
                projectDocument.Content!,
                updated,
                Hash(projectDocument.Content!.Text),
                fixEntries));

            if (unsupportedOperation) {
                entries.Add(ManualOnly(MigrationDiagnostics.ObsoleteDevOverlay.Id, projectDocument.RelativePath, edge));
            }
        }

        if (!entries.Any(x => x.Kind == "safe-fix" || x.Kind == "manual-only" || x.Kind == "failed" || x.Kind == "conflict")) {
            entries.Add(new MigrationEntry(
                "HFCM0001",
                "unchanged",
                Path.GetFileName(Path.GetFullPath(projectPath)),
                "No fixable FrontComposer migration diagnostics were found.",
                "Only allowlisted HFC migration diagnostics are changed.",
                "No matching diagnostics.",
                "No source changes are required for this migration edge.",
                edge.DocsLink,
                null));
        }

        return new MigrationPlan(documentSet.ProjectDirectory, edge, fileEdits, entries);
    }

    public static string Hash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static bool TryExtractDocumentChanges(
        Solution originalSolution,
        ImmutableArray<CodeActionOperation> operations,
        DocumentId documentId,
        string projectDirectory,
        string approvedPath,
        out SourceText? changedText)
    {
        changedText = null;
        ApplyChangesOperation? applyOperation = null;
        foreach (CodeActionOperation operation in operations) {
            if (operation is not ApplyChangesOperation applyChangesOperation) {
                return false;
            }

            if (applyOperation is not null) {
                return false;
            }

            applyOperation = applyChangesOperation;
        }

        if (applyOperation is null) {
            return false;
        }

        Solution newSolution = applyOperation.ChangedSolution;
        SolutionChanges changes = newSolution.GetChanges(originalSolution);
        ProjectChanges[] projectChanges = changes.GetProjectChanges().ToArray();
        if (projectChanges.Length != 1) {
            return false;
        }

        ProjectChanges projectChange = projectChanges[0];
        if (projectChange.GetAddedDocuments().Any()
            || projectChange.GetRemovedDocuments().Any()
            || projectChange.GetAddedAdditionalDocuments().Any()
            || projectChange.GetRemovedAdditionalDocuments().Any()
            || projectChange.GetAddedAnalyzerReferences().Any()
            || projectChange.GetRemovedAnalyzerReferences().Any()
            || projectChange.GetAddedMetadataReferences().Any()
            || projectChange.GetRemovedMetadataReferences().Any()
            || projectChange.GetChangedAdditionalDocuments().Any()
            || projectChange.GetChangedAnalyzerConfigDocuments().Any()) {
            return false;
        }

        DocumentId[] changedDocuments = projectChange.GetChangedDocuments().ToArray();
        if (changedDocuments.Length != 1 || changedDocuments[0] != documentId) {
            return false;
        }

        Document changedDocument = newSolution.GetDocument(documentId)!;
        if (!string.Equals(PathUtilities.Canonical(changedDocument.FilePath!), PathUtilities.Canonical(approvedPath), PathUtilities.PathComparison)
            || PathUtilities.ToProjectRelative(projectDirectory, changedDocument.FilePath!) == "[redacted-path]") {
            return false;
        }

        changedText = changedDocument.GetTextAsync().GetAwaiter().GetResult();
        return true;
    }

    private static bool HasOverlappingChanges(List<TextChange> changes)
    {
        TextSpan? previous = null;
        foreach (TextSpan span in changes.Select(x => x.Span).OrderBy(x => x.Start)) {
            if (previous.HasValue && span.Start < previous.Value.End) {
                return true;
            }

            previous = span;
        }

        return false;
    }

    private static MigrationEntry ManualOnly(Diagnostic diagnostic, string relativePath, MigrationEdge edge)
        => ManualOnly(diagnostic.Id, relativePath, edge);

    private static MigrationEntry ManualOnly(string diagnosticId, string relativePath, MigrationEdge edge)
        => new(
            diagnosticId,
            "manual-only",
            relativePath,
            "Migration diagnostic was not eligible for an automated safe fix.",
            "Only deterministic FrontComposer-owned solution/document edits are applied.",
            "Unsupported or missing code-fix operation.",
            "Review the migration guide and update the affected call site manually.",
            edge.DocsLink,
            null);

    private static MigrationEntry Failed(string path, MigrationEdge edge, string reason)
        => new(
            "HFCM0004",
            "failed",
            path,
            reason,
            "Migration planning completes without unsafe source access.",
            "Planning failure.",
            "Review the file and rerun migration.",
            edge.DocsLink,
            null);
}

internal sealed record ProjectDocumentSet(string ProjectDirectory, IReadOnlyList<ProjectDocument> Documents);

internal sealed record ProjectDocument(string FullPath, string RelativePath, SourceFileContent? Content = null);

internal static class ProjectDocumentLoader
{
    public static ProjectDocumentSet Load(string projectPath)
    {
        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;
        XDocument project = XDocument.Load(projectFullPath);
        List<ProjectDocument> documents = [];
        List<(string Include, string Exclude)> compileItems = project
            .Descendants()
            .Where(x => string.Equals(x.Name.LocalName, "Compile", StringComparison.Ordinal))
            .Select(x => ((string?)x.Attribute("Include") ?? string.Empty, (string?)x.Attribute("Exclude") ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Item1))
            .ToList();

        if (compileItems.Count == 0) {
            documents.AddRange(EnumerateGlob(projectDirectory, "**/*.cs", "bin/**;obj/**"));
        }
        else {
            foreach ((string include, string exclude) in compileItems) {
                documents.AddRange(EnumerateGlob(projectDirectory, include, exclude));
            }
        }

        return new ProjectDocumentSet(
            projectDirectory,
            documents
                .GroupBy(x => PathUtilities.Canonical(x.FullPath), PathUtilities.PathComparer)
                .Select(x => x.First())
                .OrderBy(x => x.RelativePath, StringComparer.Ordinal)
                .ToArray());
    }

    private static IEnumerable<ProjectDocument> EnumerateGlob(string projectDirectory, string include, string exclude)
    {
        string[] excludes = exclude.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string pattern in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            foreach (string path in Expand(projectDirectory, pattern)) {
                string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
                if (relative == "[redacted-path]" || IsExcluded(relative, excludes)) {
                    continue;
                }

                yield return new ProjectDocument(path, relative);
            }
        }
    }

    private static IEnumerable<string> Expand(string projectDirectory, string pattern)
    {
        string normalized = pattern.Replace('\\', '/');
        if (!normalized.Contains('*', StringComparison.Ordinal)) {
            string path = Path.GetFullPath(normalized.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
            return File.Exists(path) ? [path] : [];
        }

        if (normalized is "**/*.cs" or "**.cs") {
            return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        }

        string filePattern = Path.GetFileName(normalized);
        string directoryPart = normalized[..^filePattern.Length].TrimEnd('/');
        SearchOption search = directoryPart.EndsWith("**", StringComparison.Ordinal) || normalized.Contains("/**/", StringComparison.Ordinal)
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        string root = directoryPart.Replace("/**", string.Empty).TrimEnd('/');
        string searchRoot = string.IsNullOrWhiteSpace(root)
            ? projectDirectory
            : Path.GetFullPath(root.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
        return Directory.Exists(searchRoot) ? Directory.EnumerateFiles(searchRoot, filePattern, search) : [];
    }

    private static bool IsExcluded(string relativePath, string[] excludes)
    {
        foreach (string exclude in excludes) {
            string normalized = exclude.Replace('\\', '/').Trim('/');
            if (normalized.EndsWith("/**", StringComparison.Ordinal)) {
                string prefix = normalized[..^3].TrimEnd('/') + "/";
                if (relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            else if (string.Equals(relativePath, normalized, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

internal static class SourceFile
{
    public static async Task<SourceFileContent> ReadAsync(string path, CancellationToken cancellationToken)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        Encoding encoding = DetectEncoding(bytes);
        string text = encoding.GetString(PreambleFree(bytes, encoding));
        return new SourceFileContent(text, encoding);
    }

    public static async Task WriteAsync(string path, string text, Encoding encoding, CancellationToken cancellationToken)
        => await File.WriteAllTextAsync(path, text, encoding, cancellationToken).ConfigureAwait(false);

    private static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode;
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static byte[] PreambleFree(byte[] bytes, Encoding encoding)
    {
        byte[] preamble = encoding.GetPreamble();
        return preamble.Length > 0 && bytes.AsSpan().StartsWith(preamble)
            ? bytes[preamble.Length..]
            : bytes;
    }
}

internal static class MigrationDiagnostics
{
#pragma warning disable RS2008 // The CLI reserves migration IDs in SourceTools AnalyzerReleases for Story 9-2 governance.
    public static readonly DiagnosticDescriptor ObsoleteDevOverlay = new(
        "HFCM9001",
        "Obsolete FrontComposer dev-mode registration",
        "Replace obsolete FrontComposer dev-mode registration",
        "HexalithFrontComposer.Migration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "docs/migrations/9.1-to-9.2.md");

    public static readonly DiagnosticDescriptor ManualMigration = new(
        "HFCM9002",
        "Manual FrontComposer migration required",
        "Manual FrontComposer migration required",
        "HexalithFrontComposer.Migration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "docs/migrations/9.1-to-9.2.md");
#pragma warning restore RS2008
}

internal static class MigrationDiagnosticScanner
{
    private const string ManualApi = "ConfigureFrontComposerCustomMigration";

    public static async Task<ImmutableArray<Diagnostic>> ScanAsync(Document document, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return [];
        }

        SyntaxTree tree = root.SyntaxTree;
        ImmutableArray<Diagnostic>.Builder builder = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            string name = identifier.Identifier.ValueText;
            if (name == FrontComposerMigrationCodeFixProvider.ObsoleteApi) {
                builder.Add(Diagnostic.Create(MigrationDiagnostics.ObsoleteDevOverlay, Location.Create(tree, identifier.Span)));
            }
            else if (name == ManualApi) {
                builder.Add(Diagnostic.Create(MigrationDiagnostics.ManualMigration, Location.Create(tree, identifier.Span)));
            }
        }

        return builder.ToImmutable();
    }
}

internal sealed class FrontComposerMigrationCodeFixProvider : CodeFixProvider
{
    public const string ObsoleteApi = "AddFrontComposerDebugOverlay";
    public const string ReplacementApi = "AddFrontComposerDevMode";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MigrationDiagnostics.ObsoleteDevOverlay.Id);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (Diagnostic diagnostic in context.Diagnostics.Where(x => x.Id == MigrationDiagnostics.ObsoleteDevOverlay.Id)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Replace obsolete FrontComposer dev-mode API",
                    cancellationToken => ReplaceAsync(context.Document, diagnostic, cancellationToken),
                    equivalenceKey: MigrationDiagnostics.ObsoleteDevOverlay.Id),
                diagnostic);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<Document> ReplaceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return document;
        }

        SyntaxToken token = root.FindToken(diagnostic.Location.SourceSpan.Start);
        if (token.ValueText != ObsoleteApi) {
            return document;
        }

        SyntaxToken replacement = SyntaxFactory.Identifier(token.LeadingTrivia, ReplacementApi, token.TrailingTrivia);
        return document.WithSyntaxRoot(root.ReplaceToken(token, replacement));
    }
}

internal static class MigrationApplier
{
    public static async Task<MigrationResult> ApplyAsync(MigrationPlan plan, CancellationToken cancellationToken)
    {
        List<MigrationEntry> entries = [.. plan.Entries.Where(x => x.Kind != "safe-fix")];
        List<MigrationEntry> changed = [];
        foreach (PlannedFileEdit edit in plan.FileEdits.OrderBy(x => x.RelativePath, StringComparer.Ordinal)) {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.Equals(PathUtilities.Canonical(edit.FullPath), edit.CanonicalPath, PathUtilities.PathComparison)
                    || !string.Equals(PathUtilities.Canonical(Path.Combine(plan.ProjectDirectory, edit.RelativePath)), edit.CanonicalPath, PathUtilities.PathComparison)
                    || !WriteSafetyPolicy.IsAllowed(plan.ProjectDirectory, edit.FullPath, SubmoduleBoundaryReader.Read(plan.ProjectDirectory))) {
                    entries.Add(Failed(edit, plan.Edge, "Resolved write target changed or became unsafe between planning and apply."));
                    continue;
                }

                SourceFileContent current = await SourceFile.ReadAsync(edit.FullPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(MigrationPlanner.Hash(current.Text), edit.OriginalHash, StringComparison.Ordinal)) {
                    entries.Add(Failed(edit, plan.Edge, "Source content changed between planning and apply."));
                    continue;
                }

                await SourceFile.WriteAsync(edit.FullPath, edit.UpdatedText, edit.OriginalContent.Encoding, cancellationToken).ConfigureAwait(false);
                changed.AddRange(edit.Entries);
            }
            catch (OperationCanceledException) {
                entries.Add(Failed(edit, plan.Edge, "Migration apply was cancelled after writing " + changed.Count + " file(s)."));
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                entries.Add(Failed(edit, plan.Edge, "Source file could not be written."));
            }
        }

        entries.AddRange(changed);
        return new MigrationResult(true, entries, MigrationSummary.From(entries));
    }

    private static MigrationEntry Failed(PlannedFileEdit edit, MigrationEdge edge, string reason)
        => new(
            "HFCM0004",
            "failed",
            edit.RelativePath,
            reason,
            "Plan path and source hash remain stable until write.",
            "Plan drift detected.",
            "Re-run migration after reviewing source changes.",
            edge.DocsLink,
            null);
}

internal static class WriteSafetyPolicy
{
    public static bool IsAllowed(string projectDirectory, string path, HashSet<string> submoduleRoots)
    {
        string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
        if (relative == "[redacted-path]" || PathUtilities.HasExcludedSegment(projectDirectory, path)) {
            return false;
        }

        string fullPath = PathUtilities.Canonical(path);
        string fullDirectory = Path.GetDirectoryName(fullPath) ?? fullPath;
        return !submoduleRoots.Any(root => IsSameOrUnder(fullDirectory, root) || string.Equals(fullPath, root, PathUtilities.PathComparison));
    }

    private static bool IsSameOrUnder(string path, string root)
    {
        string normalizedRoot = EnsureTrailingSeparator(PathUtilities.Canonical(root));
        string normalizedPath = EnsureTrailingSeparator(PathUtilities.Canonical(path));
        return normalizedPath.StartsWith(normalizedRoot, PathUtilities.PathComparison);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}

internal static class SubmoduleBoundaryReader
{
    public static HashSet<string> Read(string projectDirectory)
    {
        HashSet<string> roots = new(PathUtilities.PathComparer);
        string? repositoryRoot = FindRepositoryRoot(projectDirectory);
        if (repositoryRoot is null) {
            return roots;
        }

        string gitmodules = Path.Combine(repositoryRoot, ".gitmodules");
        if (!File.Exists(gitmodules)) {
            return roots;
        }

        bool inSubmoduleSection = false;
        foreach (string line in File.ReadLines(gitmodules)) {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal)) {
                inSubmoduleSection = trimmed.StartsWith("[submodule ", StringComparison.Ordinal);
                continue;
            }

            if (!inSubmoduleSection || !trimmed.StartsWith("path", StringComparison.Ordinal)) {
                continue;
            }

            int equals = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0) {
                continue;
            }

            string relative = trimmed[(equals + 1)..].Trim().Trim('"');
            if (relative.Length > 0) {
                roots.Add(PathUtilities.Canonical(Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))));
            }
        }

        return roots;
    }

    private static string? FindRepositoryRoot(string start)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(start));
        while (directory is not null) {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".gitmodules"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

internal static class UnifiedDiff
{
    public static string Create(string relativePath, string original, string updated)
    {
        string[] oldLines = SplitLines(original);
        string[] newLines = SplitLines(updated);
        StringBuilder builder = new();
        _ = builder.AppendLine("--- a/" + relativePath);
        _ = builder.AppendLine("+++ b/" + relativePath);
        _ = builder.AppendLine("@@ -1," + oldLines.Length + " +1," + newLines.Length + " @@");

        int commonPrefix = 0;
        while (commonPrefix < oldLines.Length
            && commonPrefix < newLines.Length
            && oldLines[commonPrefix] == newLines[commonPrefix]) {
            commonPrefix++;
        }

        int oldSuffix = oldLines.Length - 1;
        int newSuffix = newLines.Length - 1;
        while (oldSuffix >= commonPrefix
            && newSuffix >= commonPrefix
            && oldLines[oldSuffix] == newLines[newSuffix]) {
            oldSuffix--;
            newSuffix--;
        }

        AppendContext(builder, " ", oldLines.Take(Math.Min(commonPrefix, 3)));
        AppendContext(builder, "-", oldLines.Skip(commonPrefix).Take(oldSuffix - commonPrefix + 1));
        AppendContext(builder, "+", newLines.Skip(commonPrefix).Take(newSuffix - commonPrefix + 1));
        AppendContext(builder, " ", oldLines.Skip(Math.Max(oldSuffix + 1, oldLines.Length - 3)));
        return builder.ToString();
    }

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    private static void AppendContext(StringBuilder builder, string prefix, IEnumerable<string> lines)
    {
        foreach (string line in lines) {
            _ = builder.Append(prefix).AppendLine(OutputSanitizer.Sanitize(line, 400));
        }
    }
}

internal static class MigrationJson
{
    public static object From(MigrationResult result)
        => new {
            schemaVersion = "frontcomposer.cli.migrate.v1",
            applied = result.Applied,
            summary = new {
                changed = result.Summary.Changed,
                unchanged = result.Summary.Unchanged,
                skipped = result.Summary.Skipped,
                failed = result.Summary.Failed,
                manualOnly = result.Summary.ManualOnly,
                conflicts = result.Summary.Conflicts,
            },
            entries = result.Entries
                .OrderBy(x => x.Path, StringComparer.Ordinal)
                .ThenBy(x => x.DiagnosticId, StringComparer.Ordinal)
                .Select(x => new {
                    diagnosticId = x.DiagnosticId,
                    kind = x.Kind,
                    path = OutputSanitizer.Sanitize(x.Path),
                    what = OutputSanitizer.Sanitize(x.What),
                    expected = OutputSanitizer.Sanitize(x.Expected),
                    got = OutputSanitizer.Sanitize(x.Got),
                    fix = OutputSanitizer.Sanitize(x.Fix),
                    docsLink = OutputSanitizer.Sanitize(x.DocsLink),
                    diff = OutputSanitizer.Sanitize(x.Diff, 2_000),
                    formattingApplied = x.FormattingApplied,
                })
                .ToArray(),
        };
}
