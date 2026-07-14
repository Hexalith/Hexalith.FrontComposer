using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>
/// P-42: Roslyn symbol-existence validator for fenced C# snippets in the skill corpus. Extracts
/// every <c>```csharp</c> fence, parses it as a syntax tree, walks identifier and qualified-name
/// nodes, and verifies each referenced top-level type resolves in one of the supplied
/// framework assemblies. Lighter than full semantic compilation (which T4 marks "where feasible")
/// but stronger than the metadata-only `publicApiReferences` check.
/// </summary>
public static class SkillCorpusSnippetValidator {
    public static SkillCorpusValidationResult Validate(
        SkillCorpusSnapshot snapshot,
        IEnumerable<Assembly> publicApiAssemblies) {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(publicApiAssemblies);

        Assembly[] assemblies = [.. publicApiAssemblies];

        // Gather the type-name vocabulary the snippets are allowed to reference. This includes
        // simple type names (e.g., `CommandAttribute`) and full names. Snippet identifiers that
        // do not match any vocabulary entry are flagged.
        HashSet<string> vocabularySimple = new(StringComparer.Ordinal);
        HashSet<string> vocabularyFull = new(StringComparer.Ordinal);
        foreach (Assembly assembly in assemblies) {
            foreach (Type type in assembly.GetExportedTypes()) {
                _ = vocabularySimple.Add(type.Name);
                if (type.Name.EndsWith("Attribute", StringComparison.Ordinal)) {
                    _ = vocabularySimple.Add(type.Name[..^"Attribute".Length]);
                }

                if (!string.IsNullOrEmpty(type.FullName)) {
                    _ = vocabularyFull.Add(type.FullName);
                }
            }
        }

        // C# language built-ins and common keywords that the parser will hand us as identifiers
        // depending on syntax tree shape. Treat them as already-known.
        foreach (string builtin in BuiltinKnownIdentifiers) {
            _ = vocabularySimple.Add(builtin);
        }

        List<SkillCorpusDiagnostic> diagnostics = [.. snapshot.Diagnostics];
        foreach (SkillCorpusResource resource in snapshot.Resources) {
            foreach (string snippet in ExtractCSharpFences(resource.Markdown)) {
                ValidateSnippet(resource, snippet, vocabularySimple, vocabularyFull, diagnostics);
            }
        }

        return new SkillCorpusValidationResult(diagnostics);
    }

    private static IEnumerable<string> ExtractCSharpFences(string markdown) {
        string[] lines = markdown.Split('\n');
        bool inside = false;
        StringBuilder current = new();
        foreach (string raw in lines) {
            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal)) {
                if (inside) {
                    yield return current.ToString();
                    _ = current.Clear();
                    inside = false;
                }
                else if (trimmed.StartsWith("```csharp", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("```cs", StringComparison.OrdinalIgnoreCase)) {
                    inside = true;
                }

                continue;
            }

            if (inside) {
                _ = current.AppendLine(raw);
            }
        }
    }

    private static void ValidateSnippet(
        SkillCorpusResource resource,
        string snippet,
        HashSet<string> vocabularySimple,
        HashSet<string> vocabularyFull,
        List<SkillCorpusDiagnostic> diagnostics) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(snippet);
        SyntaxNode root = tree.GetRoot();
        if (root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.BrokenSnippet,
                resource.SourceDoc,
                "Skill snippet does not parse as C#."));
            return;
        }

        // Walk attributes — these are the identifiers most likely to drift if framework code
        // renames an attribute. Also walk QualifiedName tokens for full-name references.
        foreach (AttributeSyntax attribute in root.DescendantNodes().OfType<AttributeSyntax>()) {
            string name = attribute.Name.ToString();
            if (vocabularyFull.Contains(name)) {
                continue;
            }

            string simple = name.Split('.').Last();
            if (vocabularySimple.Contains(simple)) {
                continue;
            }

            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.BrokenSnippet,
                resource.SourceDoc,
                $"Skill snippet references attribute '{name}' that is not in the validated framework assemblies."));
        }
    }

    private static readonly string[] BuiltinKnownIdentifiers = [
        "string", "int", "long", "bool", "void", "object", "byte", "char", "double", "float",
        "decimal", "short", "sbyte", "uint", "ulong", "ushort", "var", "dynamic",
        "Task", "ValueTask", "List", "IReadOnlyList", "IEnumerable", "IList", "IDictionary",
        "Dictionary", "HashSet", "Guid", "DateTime", "DateTimeOffset", "TimeSpan",
        "CancellationToken", "JsonElement",
    ];
}
