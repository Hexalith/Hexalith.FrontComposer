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
                output.Write(OutputSanitizer.SanitizeMultiLine(entry.Diff, 8_000));
            }
        }
    }
}

internal sealed record MigrationEdge(string FromVersion, string ToVersion, string DocsLink);

internal static class MigrationCatalog
{
    private static readonly MigrationEdge[] Edges = BuildEdges([
        new("9.1.0", "9.2.0", "docs/migrations/9.1-to-9.2.md"),
    ]);

    public static MigrationEdge? Resolve(string? from, string? to)
        => Edges.FirstOrDefault(edge => string.Equals(edge.FromVersion, from, StringComparison.Ordinal)
            && string.Equals(edge.ToVersion, to, StringComparison.Ordinal));

    private static MigrationEdge[] BuildEdges(MigrationEdge[] edges)
    {
        IGrouping<(string From, string To), MigrationEdge>[] duplicates = edges
            .GroupBy(edge => (edge.FromVersion, edge.ToVersion))
            .Where(g => g.Count() > 1)
            .ToArray();
        if (duplicates.Length > 0) {
            throw new InvalidOperationException(
                "Migration catalog contains duplicate edge(s): "
                    + string.Join(", ", duplicates.Select(g => g.Key.From + "->" + g.Key.To)));
        }

        return edges;
    }

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
            catch (System.Text.DecoderFallbackException) {
                entries.Add(Failed(document.RelativePath, edge, "Source file could not be decoded as UTF-8 / UTF-16 / UTF-32; refusing to migrate unknown encoding."));
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

                ImmutableArray<CodeActionOperation> operations;
                try {
                    operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    unsupportedOperation = true;
                    continue;
                }

                SourceText? changedText = await TryExtractDocumentChangesAsync(
                    workspace.CurrentSolution,
                    operations,
                    documentId,
                    documentSet.ProjectDirectory,
                    projectDocument.FullPath,
                    cancellationToken).ConfigureAwait(false);
                if (changedText is null) {
                    unsupportedOperation = true;
                    continue;
                }

                foreach (TextChange change in changedText.GetTextChanges(originalText)) {
                    plannedChanges.Add(change);
                }
            }

            if (unsupportedOperation) {
                // AC28 strict read: any rejected/non-allowlisted CodeActionOperation in this file
                // discards every safe-fix planned for this file and emits a single ManualOnly entry.
                entries.Add(ManualOnly(MigrationDiagnostics.ObsoleteDevOverlay.Id, projectDocument.RelativePath, edge));
                continue;
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

            SourceText updatedText = originalText.WithChanges(plannedChanges.OrderBy(x => x.Span.Start).ThenBy(x => x.Span.Length).ThenBy(x => x.NewText, StringComparer.Ordinal));
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

    private static async Task<SourceText?> TryExtractDocumentChangesAsync(
        Solution originalSolution,
        ImmutableArray<CodeActionOperation> operations,
        DocumentId documentId,
        string projectDirectory,
        string approvedPath,
        CancellationToken cancellationToken)
    {
        ApplyChangesOperation? applyOperation = null;
        foreach (CodeActionOperation operation in operations) {
            if (operation is not ApplyChangesOperation applyChangesOperation) {
                return null;
            }

            if (applyOperation is not null) {
                return null;
            }

            applyOperation = applyChangesOperation;
        }

        if (applyOperation is null) {
            return null;
        }

        Solution newSolution = applyOperation.ChangedSolution;
        SolutionChanges changes = newSolution.GetChanges(originalSolution);
        ProjectChanges[] projectChanges = changes.GetProjectChanges().ToArray();
        if (projectChanges.Length != 1) {
            return null;
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
            return null;
        }

        DocumentId[] changedDocuments = projectChange.GetChangedDocuments().ToArray();
        if (changedDocuments.Length != 1 || changedDocuments[0] != documentId) {
            return null;
        }

        Document changedDocument = newSolution.GetDocument(documentId)!;
        if (changedDocument.FilePath is null
            || !string.Equals(PathUtilities.Canonical(changedDocument.FilePath), PathUtilities.Canonical(approvedPath), PathUtilities.PathComparison)
            || PathUtilities.ToProjectRelative(projectDirectory, changedDocument.FilePath) == PathUtilities.RedactedPathSentinel) {
            return null;
        }

        return await changedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool HasOverlappingChanges(List<TextChange> changes)
    {
        TextChange[] sorted = [.. changes.OrderBy(x => x.Span.Start).ThenBy(x => x.Span.Length).ThenBy(x => x.NewText, StringComparer.Ordinal)];
        for (int i = 1; i < sorted.Length; i++) {
            TextSpan previous = sorted[i - 1].Span;
            TextSpan current = sorted[i].Span;
            if (current.Start < previous.End) {
                return true;
            }

            if (current.Start == previous.End && previous.IsEmpty && current.IsEmpty) {
                return true;
            }
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
        XDocument project;
        try {
            project = XDocument.Load(projectFullPath);
        }
        catch (System.Xml.XmlException) {
            return new ProjectDocumentSet(projectDirectory, []);
        }
        catch (IOException) {
            return new ProjectDocumentSet(projectDirectory, []);
        }

        List<ProjectDocument> documents = [];
        List<(string Include, string Exclude, string? Link)> compileItems = project
            .Descendants()
            .Where(x => string.Equals(x.Name.LocalName, "Compile", StringComparison.Ordinal))
            .Select(x => (
                (string?)x.Attribute("Include") ?? string.Empty,
                (string?)x.Attribute("Exclude") ?? string.Empty,
                (string?)x.Attribute("Link")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Item1))
            .ToList();

        if (compileItems.Count == 0) {
            documents.AddRange(EnumerateGlob(projectDirectory, "**/*.cs", "bin/**;obj/**", link: null));
        }
        else {
            foreach ((string include, string exclude, string? link) in compileItems) {
                documents.AddRange(EnumerateGlob(projectDirectory, include, exclude, link));
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

    private static IEnumerable<ProjectDocument> EnumerateGlob(string projectDirectory, string include, string exclude, string? link)
    {
        string[] excludes = exclude.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string projectRoot = PathUtilities.Canonical(projectDirectory);
        string projectRootWithSep = projectRoot.EndsWith(Path.DirectorySeparatorChar)
            ? projectRoot
            : projectRoot + Path.DirectorySeparatorChar;

        foreach (string pattern in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            foreach (string path in Expand(projectDirectory, pattern)) {
                string canonical = PathUtilities.Canonical(path);
                bool insideProject = string.Equals(canonical, projectRoot, PathUtilities.PathComparison)
                    || canonical.StartsWith(projectRootWithSep, PathUtilities.PathComparison);

                if (!insideProject) {
                    // P-D5 / AC23: a Compile Include that resolves outside the project root is allowed only when the element
                    // declares a Link attribute. The link target is then treated as project-relative for reporting.
                    if (string.IsNullOrWhiteSpace(link)) {
                        continue;
                    }

                    string linkRelative = link.Replace('\\', '/').TrimStart('/');
                    yield return new ProjectDocument(path, linkRelative);
                    continue;
                }

                string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
                if (relative == PathUtilities.RedactedPathSentinel || IsExcluded(relative, excludes)) {
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
            string path;
            try {
                path = Path.GetFullPath(normalized.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
                return [];
            }

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
        string searchRoot;
        try {
            searchRoot = string.IsNullOrWhiteSpace(root)
                ? projectDirectory
                : Path.GetFullPath(root.Replace('/', Path.DirectorySeparatorChar), projectDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
            return [];
        }

        return Directory.Exists(searchRoot) ? Directory.EnumerateFiles(searchRoot, filePattern, search) : [];
    }

    private static bool IsExcluded(string relativePath, string[] excludes)
    {
        foreach (string exclude in excludes) {
            string normalized = exclude.Replace('\\', '/').Trim('/');
            if (normalized.EndsWith("/**", StringComparison.Ordinal)) {
                string prefix = normalized[..^3].TrimEnd('/') + "/";
                if (relativePath.StartsWith(prefix, PathUtilities.PathComparison)) {
                    return true;
                }
            }
            else if (string.Equals(relativePath, normalized, PathUtilities.PathComparison)) {
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
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) {
            return new UTF32Encoding(bigEndian: true, byteOrderMark: true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) {
            return new UTF32Encoding(bigEndian: false, byteOrderMark: true);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode;
        }

        // Strict UTF-8: fail closed on invalid bytes rather than silently replacing with U+FFFD.
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
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
            if (IsInsideNameOf(identifier)) {
                continue;
            }

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

    private static bool IsInsideNameOf(IdentifierNameSyntax identifier)
    {
        for (SyntaxNode? parent = identifier.Parent; parent is not null; parent = parent.Parent) {
            if (parent is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax target
                && target.Identifier.ValueText == "nameof") {
                return true;
            }
        }

        return false;
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
        if (token.ValueText != ObsoleteApi || token.Parent is not IdentifierNameSyntax) {
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
        HashSet<string> submoduleSnapshot = SubmoduleBoundaryReader.Read(plan.ProjectDirectory);
        bool cancelled = false;
        foreach (PlannedFileEdit edit in plan.FileEdits.OrderBy(x => x.RelativePath, StringComparer.Ordinal)) {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                string canonicalNow = PathUtilities.Canonical(edit.FullPath);
                if (!string.Equals(canonicalNow, edit.CanonicalPath, PathUtilities.PathComparison)
                    || !WriteSafetyPolicy.IsAllowed(plan.ProjectDirectory, edit.FullPath, submoduleSnapshot)) {
                    entries.Add(Failed(edit, plan.Edge, "Resolved write target changed or became unsafe between planning and apply."));
                    continue;
                }

                SourceFileContent current;
                try {
                    current = await SourceFile.ReadAsync(edit.FullPath, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.DecoderFallbackException) {
                    entries.Add(Failed(edit, plan.Edge, "Source file could not be re-read during apply."));
                    continue;
                }

                if (!string.Equals(MigrationPlanner.Hash(current.Text), edit.OriginalHash, StringComparison.Ordinal)) {
                    entries.Add(Failed(edit, plan.Edge, "Source content changed between planning and apply."));
                    continue;
                }

                await SourceFile.WriteAsync(edit.FullPath, edit.UpdatedText, edit.OriginalContent.Encoding, cancellationToken).ConfigureAwait(false);
                changed.AddRange(edit.Entries);
            }
            catch (OperationCanceledException) {
                cancelled = true;
                entries.Add(Failed(edit, plan.Edge, "Migration apply was cancelled after writing " + changed.Count + " file(s)."));
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                entries.Add(Failed(edit, plan.Edge, "Source file could not be written."));
            }
        }

        List<MigrationEntry> final = [.. entries, .. changed];
        final = [.. final.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.DiagnosticId, StringComparer.Ordinal).ThenBy(x => x.Kind, StringComparer.Ordinal)];
        return new MigrationResult(!cancelled, final, MigrationSummary.From(final));
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
        if (relative == PathUtilities.RedactedPathSentinel || PathUtilities.HasExcludedSegment(projectDirectory, path)) {
            return false;
        }

        string fullPath = PathUtilities.Canonical(path);
        return !submoduleRoots.Any(root => IsSameOrUnder(fullPath, root));
    }

    private static bool IsSameOrUnder(string path, string root)
    {
        string normalizedRoot = PathUtilities.Canonical(root);
        string normalizedPath = PathUtilities.Canonical(path);
        if (string.Equals(normalizedPath, normalizedRoot, PathUtilities.PathComparison)) {
            return true;
        }

        string rootWithSep = EnsureTrailingSeparator(normalizedRoot);
        return normalizedPath.StartsWith(rootWithSep, PathUtilities.PathComparison);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}

internal static class SubmoduleBoundaryReader
{
    private const int MaxAncestorWalk = 32;

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
        int walked = 0;
        while (directory is not null && walked < MaxAncestorWalk) {
            string gitMarker = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitMarker) || File.Exists(gitMarker) || File.Exists(Path.Combine(directory.FullName, ".gitmodules"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
            walked++;
        }

        return null;
    }
}

internal static class UnifiedDiff
{
    private const int ContextLines = 3;

    public static string Create(string relativePath, string original, string updated)
    {
        string[] oldLines = SplitLines(original);
        string[] newLines = SplitLines(updated);
        StringBuilder builder = new();
        _ = builder.Append("--- a/").Append(relativePath).Append('\n');
        _ = builder.Append("+++ b/").Append(relativePath).Append('\n');

        // Compute LCS-based hunks; each hunk has its own @@ -L1,N1 +L2,N2 @@ header.
        List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char Prefix, string Line)> Lines)> hunks =
            ComputeHunks(oldLines, newLines, ContextLines);
        if (hunks.Count == 0) {
            return builder.ToString();
        }

        foreach (var hunk in hunks) {
            _ = builder.Append("@@ -")
                .Append(hunk.OldCount == 0 ? hunk.OldStart : hunk.OldStart + 1)
                .Append(',').Append(hunk.OldCount)
                .Append(" +")
                .Append(hunk.NewCount == 0 ? hunk.NewStart : hunk.NewStart + 1)
                .Append(',').Append(hunk.NewCount)
                .Append(" @@\n");
            foreach ((char prefix, string line) in hunk.Lines) {
                _ = builder.Append(prefix).Append(line).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char, string)> Lines)> ComputeHunks(string[] oldLines, string[] newLines, int context)
    {
        // Identify diff segments via simple two-pointer walk — emit insert/delete groups with surrounding context.
        List<(char Op, int OldIdx, int NewIdx, string Line)> ops = DiffOps(oldLines, newLines);
        List<(int OldStart, int OldCount, int NewStart, int NewCount, List<(char, string)> Lines)> hunks = [];
        if (ops.Count == 0) {
            return hunks;
        }

        int i = 0;
        while (i < ops.Count) {
            // skip equals
            while (i < ops.Count && ops[i].Op == '=') {
                i++;
            }

            if (i >= ops.Count) {
                break;
            }

            int hunkStart = Math.Max(0, i - context);
            int j = i;
            while (j < ops.Count) {
                while (j < ops.Count && ops[j].Op != '=') {
                    j++;
                }

                int trailingContext = 0;
                while (j < ops.Count && ops[j].Op == '=' && trailingContext < context * 2) {
                    j++;
                    trailingContext++;
                }

                if (j >= ops.Count || ops[j].Op == '=') {
                    break;
                }
            }

            int hunkEnd = Math.Min(ops.Count, j);
            // trim equals beyond context lines
            int trailing = 0;
            while (hunkEnd > 0 && ops[hunkEnd - 1].Op == '=' && trailing < context) {
                hunkEnd--;
                trailing++;
                if (hunkEnd == 0) {
                    break;
                }
            }

            int oldStart = -1;
            int newStart = -1;
            int oldCount = 0;
            int newCount = 0;
            List<(char, string)> lines = [];
            for (int k = hunkStart; k < hunkEnd; k++) {
                (char op, int oldIdx, int newIdx, string line) = ops[k];
                if (oldStart < 0 && oldIdx >= 0) {
                    oldStart = oldIdx;
                }

                if (newStart < 0 && newIdx >= 0) {
                    newStart = newIdx;
                }

                switch (op) {
                    case '=':
                        lines.Add((' ', line));
                        oldCount++;
                        newCount++;
                        break;
                    case '-':
                        lines.Add(('-', line));
                        oldCount++;
                        break;
                    case '+':
                        lines.Add(('+', line));
                        newCount++;
                        break;
                }
            }

            if (oldStart < 0) {
                oldStart = 0;
            }

            if (newStart < 0) {
                newStart = 0;
            }

            hunks.Add((oldStart, oldCount, newStart, newCount, lines));
            i = hunkEnd;
            // consume any equals between hunks
            while (i < ops.Count && ops[i].Op == '=') {
                i++;
            }
        }

        return hunks;
    }

    private static List<(char Op, int OldIdx, int NewIdx, string Line)> DiffOps(string[] oldLines, string[] newLines)
    {
        // Simple O(N+M) walk anchored on line equality; conservative — emits all old as deletes and all new as inserts
        // when the lines diverge, then re-syncs at the next match. Sufficient for the small migration diffs this CLI
        // produces; not a full Myers diff.
        List<(char, int, int, string)> ops = [];
        int i = 0;
        int j = 0;
        while (i < oldLines.Length && j < newLines.Length) {
            if (string.Equals(oldLines[i], newLines[j], StringComparison.Ordinal)) {
                ops.Add(('=', i, j, oldLines[i]));
                i++;
                j++;
                continue;
            }

            int nextMatchOld = -1;
            int nextMatchNew = -1;
            for (int k = 1; k <= 32 && (i + k < oldLines.Length || j + k < newLines.Length); k++) {
                if (i + k < oldLines.Length && string.Equals(oldLines[i + k], newLines[j], StringComparison.Ordinal)) {
                    nextMatchOld = i + k;
                    break;
                }

                if (j + k < newLines.Length && string.Equals(oldLines[i], newLines[j + k], StringComparison.Ordinal)) {
                    nextMatchNew = j + k;
                    break;
                }
            }

            if (nextMatchOld > 0) {
                while (i < nextMatchOld) {
                    ops.Add(('-', i, -1, oldLines[i]));
                    i++;
                }
            }
            else if (nextMatchNew > 0) {
                while (j < nextMatchNew) {
                    ops.Add(('+', -1, j, newLines[j]));
                    j++;
                }
            }
            else {
                ops.Add(('-', i, -1, oldLines[i]));
                ops.Add(('+', -1, j, newLines[j]));
                i++;
                j++;
            }
        }

        while (i < oldLines.Length) {
            ops.Add(('-', i, -1, oldLines[i]));
            i++;
        }

        while (j < newLines.Length) {
            ops.Add(('+', -1, j, newLines[j]));
            j++;
        }

        return ops;
    }

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
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
                    diff = OutputSanitizer.SanitizeMultiLine(x.Diff, 8_000),
                    formattingApplied = x.FormattingApplied,
                })
                .ToArray(),
        };
}
