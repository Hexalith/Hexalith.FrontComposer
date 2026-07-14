using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationPlanner {
    private static readonly MetadataReference[] References = [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    ];

    public static async Task<MigrationPlan> PlanAsync(string projectPath, MigrationEdge edge, CancellationToken cancellationToken) {
        ProjectDocumentSet documentSet = ProjectDocumentLoader.Load(projectPath);
        IReadOnlyDictionary<string, ImmutableArray<Diagnostic>> generatedDiagnostics =
            MigrationDiagnosticSidecarReader.Read(documentSet.ProjectDirectory);
        HashSet<string> submoduleRoots = SubmoduleBoundaryReader.Read(documentSet.ProjectDirectory);
        List<MigrationEntry> entries = [];
        List<PlannedFileEdit> fileEdits = [];

        MefHostServices host;
        try {
            host = MefHostServices.Create(MefHostServices.DefaultAssemblies
                .Concat([typeof(CSharpCompilation).Assembly, typeof(CSharpFormattingOptions).Assembly])
                .Distinct());
        }
        catch (Exception ex) when (IsWorkspaceCompositionFailure(ex)) {
            entries.Add(Failed(Path.GetFileName(projectPath), edge, "Workspaces assemblies failed to load; verify the CLI package installation and rerun migration."));
            return new MigrationPlan(documentSet.ProjectDirectory, edge, [], entries);
        }

        using AdhocWorkspace workspace = new(host);
        var projectId = ProjectId.CreateNewId(debugName: Path.GetFileNameWithoutExtension(projectPath));
        var projectInfo = ProjectInfo.Create(
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

            var documentId = DocumentId.CreateNewId(projectId, document.RelativePath);
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
            if (generatedDiagnostics.TryGetValue(projectDocument.RelativePath, out ImmutableArray<Diagnostic> sourceToolDiagnostics)) {
                diagnostics = diagnostics.AddRange(sourceToolDiagnostics);
            }

            List<MigrationEntry> fileEntries = [];

            foreach (Diagnostic manual in diagnostics.Where(x => x.Id == MigrationDiagnostics.ManualMigration.Id).OrderBy(x => x.Location.SourceSpan.Start)) {
                // If the sidecar reader preserved the source-side `what`, surface it; otherwise fall back to the generic message.
                string sidecarWhat = manual.Properties.TryGetValue("what", out string? value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : "Customization-sensitive FrontComposer API requires manual migration.";
                fileEntries.Add(new MigrationEntry(
                    manual.Id,
                    "manual-only",
                    projectDocument.RelativePath,
                    sidecarWhat,
                    "Developer reviews customization semantics before changing behavior.",
                    "A manual-only migration diagnostic was found.",
                    "Review the migration guide and update the affected call site manually.",
                    edge.DocsLink,
                    null));
            }

            List<TextChange> plannedChanges = [];
            List<MigrationEntry> fixEntries = [];
            bool unsupportedOperation = false;
            // Track the *actual* failing diagnostic id for the AC28 strict-read ManualOnly entry,
            // not the hardcoded ObsoleteDevOverlay id. Defaults to ObsoleteDevOverlay because that
            // is currently the only fixable id, but will be correct when more fixers land.
            string failingDiagnosticId = MigrationDiagnostics.ObsoleteDevOverlay.Id;
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
                    failingDiagnosticId = diagnostic.Id;
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
                    failingDiagnosticId = diagnostic.Id;
                    continue;
                }

                foreach (TextChange change in changedText.GetTextChanges(originalText)) {
                    plannedChanges.Add(change);
                }
            }

            if (unsupportedOperation) {
                // AC28 strict read: any rejected/non-allowlisted CodeActionOperation in this file
                // discards every safe-fix planned for this file and emits a single ManualOnly entry
                // tagged with the diagnostic id that actually triggered the rejection.
                entries.Add(ManualOnly(failingDiagnosticId, projectDocument.RelativePath, edge));
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

        foreach ((string sidecarPath, ImmutableArray<Diagnostic> diagnostics) in generatedDiagnostics
            .Where(x => x.Key.StartsWith(MigrationDiagnosticSidecarReader.SentinelPrefix, StringComparison.Ordinal))
            .OrderBy(x => x.Key, StringComparer.Ordinal)) {
            foreach (Diagnostic diagnostic in diagnostics.OrderBy(x => x.Id, StringComparer.Ordinal)) {
                entries.Add(new MigrationEntry(
                    diagnostic.Id,
                    "manual-only",
                    sidecarPath,
                    "Migration sidecar path or payload was not trusted.",
                    "Sidecar diagnostics use project-relative source paths inside the selected project.",
                    "Untrusted, malformed, or unreadable sidecar data was found.",
                    "Regenerate FrontComposer diagnostics and rerun migration.",
                    edge.DocsLink,
                    null));
            }
        }

        if (!entries.Any(x => x.Kind is "safe-fix" or "manual-only" or "failed" or "conflict")) {
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

    private static bool IsWorkspaceCompositionFailure(Exception exception)
        => exception is FileLoadException or FileNotFoundException or TypeLoadException or System.Reflection.ReflectionTypeLoadException
            || exception.GetType().Name.Contains("Composition", StringComparison.Ordinal);

    public static string Hash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    internal static async Task<SourceText?> TryExtractDocumentChangesAsync(
        Solution originalSolution,
        ImmutableArray<CodeActionOperation> operations,
        DocumentId documentId,
        string projectDirectory,
        string approvedPath,
        CancellationToken cancellationToken) {
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

    internal static bool HasOverlappingChanges(List<TextChange> changes) {
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
