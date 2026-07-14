using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftDiagnosticFact(
    string id,
    DiagnosticSeverity severity,
    string message,
    string boundedContext,
    string declarationName,
    string memberName,
    string driftKind,
    string baselinePath,
    string declarationPath,
    string expectedShapeHash,
    string actualShapeHash,
    string schemaVersion,
    string algorithmVersion,
    string sortKey,
    string sourcePath,
    int sourceLine,
    int sourceColumn) {
    internal string Id { get; } = id;
    internal DiagnosticSeverity Severity { get; } = severity;
    internal string Message { get; } = message;
    internal string BoundedContext { get; } = boundedContext;
    internal string DeclarationName { get; } = declarationName;
    internal string MemberName { get; } = memberName;
    internal string DriftKind { get; } = driftKind;
    internal string BaselinePath { get; } = baselinePath;
    internal string DeclarationPath { get; } = declarationPath;
    internal string ExpectedShapeHash { get; } = expectedShapeHash;
    internal string ActualShapeHash { get; } = actualShapeHash;
    internal string SchemaVersion { get; } = schemaVersion;
    internal string AlgorithmVersion { get; } = algorithmVersion;
    internal string SortKey { get; } = sortKey;
    internal string SourcePath { get; } = sourcePath;
    internal int SourceLine { get; } = sourceLine;
    internal int SourceColumn { get; } = sourceColumn;

    public override string ToString()
        => Id + "|" + BoundedContext + "|" + DeclarationName + "|" + MemberName + "|" + DriftKind + "|" + Message;

    internal Diagnostic ToDiagnostic(Compilation? compilation = null) {
        DiagnosticDescriptor descriptor = DriftDiagnosticDescriptors.GetDescriptor(Id, Severity);
        Location location = ToLocation(compilation);
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add("BaselinePath", BaselinePath)
            .Add("DeclarationPath", DeclarationPath)
            .Add("DeclarationName", DeclarationName)
            .Add("MemberName", MemberName)
            .Add("DriftKind", DriftKind)
            .Add("ExpectedShapeHash", ExpectedShapeHash)
            .Add("ActualShapeHash", ActualShapeHash)
            .Add("SchemaVersion", SchemaVersion)
            .Add("AlgorithmVersion", AlgorithmVersion)
            .Add("BoundedContext", BoundedContext);

        return Diagnostic.Create(descriptor, location, properties, Message);
    }

    internal static DriftDiagnosticFact MissingBaseline()
        => Simple(
            DriftConstants.MissingBaselineId,
            DiagnosticSeverity.Warning,
            "What: drift detection is enabled but no trusted generated UI baseline was provided. Expected: a checked-in frontcomposer.drift-baseline.json AdditionalText. Got: first run or missing baseline. Fix: create or manually reconcile the baseline in Story 9-1; Story 9-2 owns the future CLI inspect/update workflow. DocsLink: " + Docs(DriftConstants.MissingBaselineId),
            "MissingBaseline");

    internal static DriftDiagnosticFact InvalidBaselinePath(string path)
        => Simple(
            DriftConstants.InvalidBaselinePathId,
            DiagnosticSeverity.Error,
            "What: configured drift baseline path did not resolve. Expected: an AdditionalText matching the configured baseline path. Got: " + DriftSanitizer.Safe(path) + ". Fix: include the checked-in baseline file or correct HfcDriftBaselinePath. DocsLink: " + Docs(DriftConstants.InvalidBaselinePathId),
            "InvalidBaselinePath");

    internal static DriftDiagnosticFact Configuration(string optionName, string expected, string got)
        => Simple(
            DriftConstants.InvalidOptionId,
            DiagnosticSeverity.Warning,
            "What: invalid drift detector analyzer-config option '" + DriftSanitizer.Safe(optionName) + "'. Expected: " + DriftSanitizer.Safe(expected) + ". Got: " + DriftSanitizer.Safe(got) + ". Fix: correct the MSBuild property; the generator falls back to documented safe defaults. DocsLink: " + Docs(DriftConstants.InvalidOptionId),
            "InvalidOption");

    internal static DriftDiagnosticFact TrustFailure(
        string id,
        string what,
        string expected,
        string got,
        string baselinePath,
        string schemaVersion = DriftConstants.SchemaVersion,
        string algorithmVersion = DriftConstants.Algorithm)
        => Simple(
            id,
            DiagnosticSeverity.Error,
            "What: " + DriftSanitizer.Safe(what) + ". Expected: " + DriftSanitizer.Safe(expected) + ". Got: " + DriftSanitizer.Safe(got) + ". Fix: repair or regenerate the checked-in generated UI baseline; unsafe partial drift comparison is suppressed. DocsLink: " + Docs(id),
            what,
            baselinePath: DriftSanitizer.NormalizePath(baselinePath),
            schemaVersion: DriftSanitizer.Safe(schemaVersion),
            algorithmVersion: DriftSanitizer.Safe(algorithmVersion));

    internal static DriftDiagnosticFact RedactionSuppressed()
        => Simple(
            DriftConstants.RedactionSuppressedId,
            DiagnosticSeverity.Error,
            "What: drift baseline contains structural values that could not be safely sanitized. Expected: runtime-data-free baseline metadata. Got: redaction-sensitive value. Fix: remove tenant/user/token/path/payload data from the checked-in baseline; original drift diagnostics are suppressed. DocsLink: " + Docs(DriftConstants.RedactionSuppressedId),
            "RedactionSuppressed");

    internal static DriftDiagnosticFact Structural(
        string driftKind,
        DriftBaselineContract? baseline,
        DriftCurrentContract? current,
        string? memberName,
        string message,
        DiagnosticSeverity severity) {
        string boundedContext = current?.BoundedContext ?? baseline?.BoundedContext ?? "<none>";
        string declarationName = current?.Type ?? baseline?.Type ?? "<none>";
        string baselinePath = baseline?.SourcePath ?? "<none>";
        string declarationPath = current?.SourcePath ?? "<none>";
        // Story 9-1 P19: include schema+algorithm in hash material AND emit explicit
        // <none> sentinels for null sides so an "added declaration" and a "removed
        // declaration" hashing the same memberName cannot collide.
        string expectedHash = Hash(ComposeHashInput(
            baseline?.Type ?? "<none>",
            baseline?.BoundedContext ?? "<none>",
            memberName ?? "<none>",
            driftKind,
            discriminator: "expected"));
        string actualHash = Hash(ComposeHashInput(
            current?.Type ?? "<none>",
            current?.BoundedContext ?? "<none>",
            memberName ?? "<none>",
            driftKind,
            discriminator: "actual"));
        string sortKey = boundedContext + "|" + (current?.Family ?? baseline?.Family ?? "<none>") + "|" + declarationName + "|" + (memberName ?? "<none>") + "|" + driftKind;
        return new DriftDiagnosticFact(
            DriftConstants.StructuralDriftId,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(boundedContext),
            DriftSanitizer.Safe(declarationName),
            DriftSanitizer.Safe(memberName ?? "<none>"),
            driftKind,
            DriftSanitizer.NormalizePath(baselinePath),
            DriftSanitizer.NormalizePath(declarationPath),
            expectedHash,
            actualHash,
            DriftConstants.SchemaVersion,
            DriftConstants.Algorithm,
            sortKey,
            current?.SourcePath ?? string.Empty,
            current?.SourceLine ?? -1,
            current?.SourceColumn ?? -1);
    }

    internal static DriftDiagnosticFact Metadata(
        string driftKind,
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        string? memberName,
        string message,
        DiagnosticSeverity severity) {
        string sortKey = current.BoundedContext + "|" + current.Family + "|" + current.Type + "|" + (memberName ?? "<none>") + "|" + driftKind;
        return new DriftDiagnosticFact(
            DriftConstants.MetadataDriftId,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(current.BoundedContext),
            DriftSanitizer.Safe(current.Type),
            DriftSanitizer.Safe(memberName ?? "<none>"),
            driftKind,
            DriftSanitizer.NormalizePath(baseline.SourcePath),
            DriftSanitizer.NormalizePath(current.SourcePath),
            // Story 9-1 P19: schema+algorithm in hash material; explicit <none> for null members.
            Hash(ComposeHashInput(baseline.Type, baseline.BoundedContext, memberName ?? "<none>", driftKind, "expected")),
            Hash(ComposeHashInput(current.Type, current.BoundedContext, memberName ?? "<none>", driftKind, "actual")),
            DriftConstants.SchemaVersion,
            DriftConstants.Algorithm,
            sortKey,
            current.SourcePath,
            current.SourceLine,
            current.SourceColumn);
    }

    internal static DriftDiagnosticFact Truncation(int omittedCount)
        => Simple(
            DriftConstants.TruncationId,
            DiagnosticSeverity.Warning,
            "What: drift diagnostics were truncated after the configured cap. Expected: full drift output. Got: " + omittedCount.ToString(CultureInfo.InvariantCulture) + " omitted diagnostics. Fix: address earlier diagnostics or increase HfcDriftMaxDiagnostics. DocsLink: " + Docs(DriftConstants.TruncationId),
            "Truncation",
            memberName: "<summary>",
            sortKey: "~~~~|Truncation");

    internal static DriftDiagnosticFact TrimAot()
        => Simple(
            DriftConstants.TrimAotReflectionCatalogId,
            DiagnosticSeverity.Warning,
            "What: PublishTrimmed/native AOT build uses projection metadata where the default ReflectionActionQueueProjectionCatalog path may be trim-incompatible. Expected: source-generated or adopter-supplied IActionQueueProjectionCatalog evidence. Got: default reflection catalog risk. Fix: register a source-generated IActionQueueProjectionCatalog override; runtime validators remain authoritative where build-time evidence is incomplete. DocsLink: " + Docs(DriftConstants.TrimAotReflectionCatalogId),
            "TrimAotReflectionCatalog");

    private static DriftDiagnosticFact Simple(
        string id,
        DiagnosticSeverity severity,
        string message,
        string driftKind,
        string boundedContext = "<none>",
        string declarationName = "<none>",
        string memberName = "<none>",
        string baselinePath = "<none>",
        string declarationPath = "<none>",
        string schemaVersion = DriftConstants.SchemaVersion,
        string algorithmVersion = DriftConstants.Algorithm,
        string sortKey = "") {
        string actualSortKey = string.IsNullOrEmpty(sortKey)
            ? boundedContext + "|<diagnostic>|" + declarationName + "|" + memberName + "|" + driftKind
            : sortKey;
        return new DriftDiagnosticFact(
            id,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(boundedContext),
            DriftSanitizer.Safe(declarationName),
            DriftSanitizer.Safe(memberName),
            driftKind,
            DriftSanitizer.NormalizePath(baselinePath),
            DriftSanitizer.NormalizePath(declarationPath),
            // Story 9-1 P19: schema+algorithm in hash material to prevent cross-version collisions.
            Hash(ComposeHashInput(declarationName, boundedContext, memberName, driftKind, id + "|expected")),
            Hash(ComposeHashInput(declarationName, boundedContext, memberName, driftKind, id + "|actual")),
            DriftSanitizer.Safe(schemaVersion),
            DriftSanitizer.Safe(algorithmVersion),
            actualSortKey,
            string.Empty,
            -1,
            -1);
    }

    private Location ToLocation(Compilation? compilation) {
        if (string.IsNullOrWhiteSpace(SourcePath) || SourceLine < 0 || SourceColumn < 0) {
            return Location.None;
        }

        if (compilation is not null) {
            // Story 9-1 P21: Windows file paths compare case-insensitively; using ordinal
            // here used to leak through to the LinePosition fallback when adopters' build
            // produced a mixed-case `tree.FilePath`, which then leaked an absolute path into
            // IDE diagnostics.
            StringComparison pathComparison = System.IO.Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            foreach (SyntaxTree tree in compilation.SyntaxTrees) {
                if (!string.Equals(tree.FilePath, SourcePath, pathComparison)) {
                    continue;
                }

                SourceText text = tree.GetText();
                if (SourceLine < text.Lines.Count) {
                    TextLine line = text.Lines[SourceLine];
                    int absolutePosition = Math.Min(line.End, line.Start + SourceColumn);
                    return Location.Create(tree, new TextSpan(absolutePosition, 0));
                }
            }
        }

        // Story 9-1 P21: sanitize the SourcePath before embedding it in Location.Create —
        // otherwise the absolute path leaks into IDE diagnostics, bypassing the message-level
        // sanitization. NormalizePath reduces absolute paths to filename / `<outside-project>`.
        string sanitizedPath = DriftSanitizer.NormalizePath(SourcePath);
        LinePosition position = new(SourceLine, SourceColumn);
        return Location.Create(sanitizedPath, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    /// <summary>
    /// Story 9-1 P18 / P19: previously created and disposed a <see cref="SHA256"/> instance
    /// per call (~500 hashes per drift pass × generator runs on every keystroke). Cache one
    /// instance per thread via <see cref="ThreadLocal{T}"/> — SHA256 instances are not
    /// thread-safe but generator pipelines do dispatch across the thread pool, so a
    /// thread-local pool gives allocation-free reuse without explicit locking.
    /// </summary>
    private static readonly ThreadLocal<SHA256> Sha256Pool = new(static () => SHA256.Create());

    private static string Hash(string? input) {
        SHA256 sha = Sha256Pool.Value!;
        byte[] bytes = Encoding.UTF8.GetBytes(input ?? "<none>");
        byte[] hash = sha.ComputeHash(bytes);
        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash) {
            _ = sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string ComposeHashInput(string declarationName, string boundedContext, string memberName, string driftKind, string discriminator)
        => DriftConstants.SchemaVersion + "|"
            + DriftConstants.Algorithm + "|"
            + (string.IsNullOrEmpty(declarationName) ? "<none>" : declarationName) + "|"
            + (string.IsNullOrEmpty(boundedContext) ? "<none>" : boundedContext) + "|"
            + (string.IsNullOrEmpty(memberName) ? "<none>" : memberName) + "|"
            + driftKind + "|"
            + discriminator;

    private static string Docs(string id) => "https://hexalith.github.io/FrontComposer/diagnostics/" + id;
}
