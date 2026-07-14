using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static partial class SkillCorpusParser {
    private const int MaxIdLength = 128;
    private const string SkillsUriPrefix = "frontcomposer://skills/";

    private static readonly FrozenSet<string> RequiredKeys = new[] {
        "id",
        "title",
        "version",
        "audience",
        "docfx",
        "mcpResource",
        "resourceUri",
        "order",
        "sourceDoc",
        "narrative",
        "references",
    }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> OptionalKeys = new[] {
        "migrationOwner",
        "owningStory",
        "publicApiReferences",
        "samplePaths",
    }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> SectionNames = new[] {
        "narrative",
        "agent-reference",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static SkillCorpusSnapshot Parse(IEnumerable<SkillCorpusSource> sources) {
        ArgumentNullException.ThrowIfNull(sources);

        List<SkillCorpusResource> resources = [];
        List<SkillCorpusDiagnostic> diagnostics = [];

        foreach (SkillCorpusSource source in sources.OrderBy(s => s.Path, StringComparer.Ordinal)) {
            // P-2: track per-file diagnostics so a prior file's failure does not silently nuke
            // every subsequent valid file. ParseOne consults the delta against this baseline.
            int initialDiagnosticCount = diagnostics.Count;
            SkillCorpusResource? resource = ParseOne(source, diagnostics, initialDiagnosticCount);
            if (resource is not null) {
                resources.Add(resource);
            }
        }

        // P-37: URIs are canonicalized to lowercase at parse time so lookup, dedupe, and
        // dispatch all use Ordinal comparisons consistently.
        HashSet<string> resourceUris = new(StringComparer.Ordinal);
        foreach (SkillCorpusResource resource in resources) {
            if (!resourceUris.Add(resource.ResourceUri)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.DuplicateResource,
                    resource.SourceDoc,
                    $"Duplicate skill resource URI '{resource.ResourceUri}'."));
            }
        }

        if (diagnostics.Count > 0) {
            return new SkillCorpusSnapshot([], diagnostics);
        }

        return new SkillCorpusSnapshot(
            [.. resources
                .OrderBy(r => r.Order)
                .ThenBy(r => r.ResourceUri, StringComparer.Ordinal)
                .Select(WithFingerprint)],
            diagnostics);
    }

    private static SkillCorpusResource? ParseOne(
        SkillCorpusSource source,
        List<SkillCorpusDiagnostic> diagnostics,
        int initialDiagnosticCount) {
        // P-19: strip BOM and normalize lone CR before line-splitting.
        string text = source.Text;
        if (text.Length > 0 && text[0] == '﻿') {
            text = text[1..];
        }

        text = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string[] lines = text.Split('\n');
        if (lines.Length < 3 || !string.Equals(lines[0], "---", StringComparison.Ordinal)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.MissingFrontMatter,
                source.Path,
                "Skill source must start with front matter."));
            return null;
        }

        int end = Array.IndexOf(lines, "---", 1);
        if (end < 0) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.MissingFrontMatter,
                source.Path,
                "Skill source front matter is not terminated."));
            return null;
        }

        Dictionary<string, string> frontMatter = new(StringComparer.Ordinal);
        for (int i = 1; i < end; i++) {
            string line = lines[i];
            int separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                    source.Path,
                    $"Invalid front matter line '{line}'."));
                continue;
            }

            string key = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();

            // P-17: emit a diagnostic when a duplicate key is encountered instead of silently
            // overwriting the prior value.
            if (frontMatter.ContainsKey(key)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                    source.Path,
                    $"Duplicate front matter field '{key}'."));
                continue;
            }

            frontMatter[key] = value;
        }

        ValidateFrontMatterKeys(source.Path, frontMatter, diagnostics);
        string body = string.Join('\n', lines.Skip(end + 1));
        string? markdown = ExtractAgentReference(source.Path, body, diagnostics);
        if (diagnostics.Count > initialDiagnosticCount || markdown is null) {
            return null;
        }

        string id = ReadString(source.Path, frontMatter, "id", diagnostics);
        string title = ReadString(source.Path, frontMatter, "title", diagnostics);
        string version = ReadString(source.Path, frontMatter, "version", diagnostics);
        string audience = ReadString(source.Path, frontMatter, "audience", diagnostics);
        string resourceUri = ReadString(source.Path, frontMatter, "resourceUri", diagnostics);
        string sourceDoc = ReadString(source.Path, frontMatter, "sourceDoc", diagnostics);
        bool docfx = ReadBool(source.Path, frontMatter, "docfx", diagnostics);
        bool mcpResource = ReadBool(source.Path, frontMatter, "mcpResource", diagnostics);
        bool narrative = ReadBool(source.Path, frontMatter, "narrative", diagnostics);
        bool references = ReadBool(source.Path, frontMatter, "references", diagnostics);
        int order = ReadInt(source.Path, frontMatter, "order", diagnostics);

        if (!LowerIdPattern().IsMatch(id) || id.Length > MaxIdLength) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                $"Skill id must be lowercase kebab-case starting with a letter and at most {MaxIdLength} characters."));
        }

        // P-37: canonicalize resourceUri to lowercase at parse time.
        if (!resourceUri.StartsWith(SkillsUriPrefix, StringComparison.OrdinalIgnoreCase)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                "Skill resourceUri must be a frontcomposer://skills/... URI."));
        }
        else {
            resourceUri = resourceUri.ToLowerInvariant();
        }

        if (!string.Equals(audience, "agent", StringComparison.Ordinal)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                "Skill audience must be 'agent'."));
        }

        if (!mcpResource) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                "Skill source must opt into mcpResource."));
        }

        if (ContainsUnsafeContent(markdown)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.UnsafeContent,
                source.Path,
                "Agent reference content contains unsafe instruction language."));
        }

        if (diagnostics.Count > initialDiagnosticCount) {
            return null;
        }

        IReadOnlyList<string> publicApiReferences = ReadArray(source.Path, frontMatter.GetValueOrDefault("publicApiReferences"), "publicApiReferences", diagnostics);
        IReadOnlyList<string> samplePaths = ReadArray(source.Path, frontMatter.GetValueOrDefault("samplePaths"), "samplePaths", diagnostics);

        if (diagnostics.Count > initialDiagnosticCount) {
            return null;
        }

        return new SkillCorpusResource(
            id,
            title,
            version,
            audience,
            docfx,
            mcpResource,
            resourceUri,
            order,
            sourceDoc,
            narrative,
            references,
            frontMatter.GetValueOrDefault("migrationOwner"),
            markdown.Trim(),
            publicApiReferences,
            samplePaths,
            OwningStory: frontMatter.GetValueOrDefault("owningStory"));
    }

    private static SkillCorpusResource WithFingerprint(SkillCorpusResource resource) {
        // P-39: include a digest of the markdown body in the fingerprint payload so any content
        // change produces a fingerprint delta. Without this, two resources with identical metadata
        // and different bodies would share a fingerprint, defeating AC8 drift detection.
        string bodyDigest = Sha256Hex(resource.Markdown);

        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.SkillCorpusResource,
            resource.Id,
            "frontcomposer.skill-corpus.v1",
            null,
            null,
            resource.ResourceUri,
            [],
            [
                new SchemaCollectionContract("publicApiReferences", SchemaCollectionOrder.NonStructuralSorted, "value"),
                new SchemaCollectionContract("samplePaths", SchemaCollectionOrder.NonStructuralSorted, "value"),
            ],
            new SortedDictionary<string, string>(StringComparer.Ordinal) {
                ["bodyDigest"] = bodyDigest,
                ["contentType"] = resource.ContentType,
                ["docfx"] = resource.Docfx ? "true" : "false",
                ["mcpResource"] = resource.McpResource ? "true" : "false",
                ["order"] = resource.Order.ToString(CultureInfo.InvariantCulture),
                ["publicApiReferences"] = string.Join("|", resource.PublicApiReferences.OrderBy(v => v, StringComparer.Ordinal)),
                ["samplePaths"] = string.Join("|", resource.SamplePaths.OrderBy(v => v, StringComparer.Ordinal)),
                ["title"] = resource.Title,
                ["version"] = resource.Version,
            }));

        return resource with { Fingerprint = payload.Fingerprint };
    }

    private static void ValidateFrontMatterKeys(
        string source,
        Dictionary<string, string> frontMatter,
        List<SkillCorpusDiagnostic> diagnostics) {
        foreach (string key in RequiredKeys) {
            if (!frontMatter.ContainsKey(key)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                    source,
                    $"Missing required front matter field '{key}'."));
            }
        }

        foreach (string key in frontMatter.Keys) {
            if (!RequiredKeys.Contains(key) && !OptionalKeys.Contains(key)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                    source,
                    $"Unknown front matter field '{key}'."));
            }
        }
    }

    private static string? ExtractAgentReference(
        string source,
        string body,
        List<SkillCorpusDiagnostic> diagnostics) {
        List<string> agentSections = [];
        string? active = null;
        StringBuilder current = new();
        HashSet<string> seenKinds = new(StringComparer.Ordinal);

        // P-20: track triple-backtick fenced code-block state so markers inside fences do not
        // toggle section state. The marker grammar is reserved for narrative-vs-agent boundaries
        // outside code samples; corpus authors documenting marker syntax should not have their
        // examples interpreted as live markers.
        bool insideFence = false;

        foreach (string rawLine in body.Split('\n')) {
            string line = rawLine.Trim();

            if (line.StartsWith("```", StringComparison.Ordinal)) {
                insideFence = !insideFence;
                if (active is not null) {
                    _ = current.AppendLine(rawLine);
                }
                continue;
            }

            if (insideFence) {
                if (active is not null) {
                    _ = current.AppendLine(rawLine);
                }
                continue;
            }

            Match start = StartSectionRegex().Match(line);
            Match end = EndSectionRegex().Match(line);

            if (start.Success) {
                string kind = start.Groups["kind"].Value;
                if (!SectionNames.Contains(kind)) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                        source,
                        $"Unknown section marker '{kind}'.",
                        kind));
                    return null;
                }

                if (active is not null) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                        source,
                        "Nested section markers are not allowed.",
                        kind));
                    return null;
                }

                if (!seenKinds.Add(kind)) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                        source,
                        $"Duplicate section marker block '{kind}'.",
                        kind));
                    return null;
                }

                active = kind;
                _ = current.Clear();
                continue;
            }

            if (end.Success) {
                if (active is null) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                        source,
                        "Closing section marker has no matching opening marker."));
                    return null;
                }

                if (string.Equals(active, "agent-reference", StringComparison.Ordinal)) {
                    agentSections.Add(current.ToString());
                }

                active = null;
                _ = current.Clear();
                continue;
            }

            if (active is not null) {
                _ = current.AppendLine(rawLine);
            }
        }

        if (active is not null) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                source,
                $"Section marker '{active}' is not terminated.",
                active));
            return null;
        }

        if (agentSections.Count == 0) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidSectionMarker,
                source,
                "Skill source must contain one agent-reference section."));
            return null;
        }

        return string.Join("\n\n", agentSections);
    }

    private static string ReadString(string source, Dictionary<string, string> frontMatter, string key, List<SkillCorpusDiagnostic> diagnostics) {
        if (!frontMatter.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source,
                $"Front matter field '{key}' must be non-empty."));
            return string.Empty;
        }

        return value;
    }

    private static bool ReadBool(string source, Dictionary<string, string> frontMatter, string key, List<SkillCorpusDiagnostic> diagnostics) {
        string value = ReadString(source, frontMatter, key, diagnostics);
        if (bool.TryParse(value, out bool parsed)) {
            return parsed;
        }

        diagnostics.Add(new SkillCorpusDiagnostic(
            SkillCorpusDiagnosticCategory.InvalidFrontMatter,
            source,
            $"Front matter field '{key}' must be a boolean."));
        return false;
    }

    private static int ReadInt(string source, Dictionary<string, string> frontMatter, string key, List<SkillCorpusDiagnostic> diagnostics) {
        string value = ReadString(source, frontMatter, key, diagnostics);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed >= 0) {
            return parsed;
        }

        diagnostics.Add(new SkillCorpusDiagnostic(
            SkillCorpusDiagnosticCategory.InvalidFrontMatter,
            source,
            $"Front matter field '{key}' must be a non-negative integer."));
        return 0;
    }

    private static IReadOnlyList<string> ReadArray(
        string source,
        string? value,
        string key,
        List<SkillCorpusDiagnostic> diagnostics) {
        if (string.IsNullOrWhiteSpace(value)) {
            return [];
        }

        string trimmed = value.Trim();

        // P-6 (parser-side companion): require explicit bracket syntax for array values.
        // Bare comma-containing values are an authoring error rather than a single-element list.
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']')) {
            if (trimmed.Contains(',', StringComparison.Ordinal)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                    source,
                    $"Front matter array '{key}' must use bracket syntax '[a, b]'."));
                return [];
            }

            return [trimmed];
        }

        // Embedded commas inside array elements are not supported by this minimal parser; the
        // corpus does not currently contain generic-arg references, so we reject rather than
        // silently mis-parse.
        string inner = trimmed[1..^1];
        if (inner.Contains('<', StringComparison.Ordinal) || inner.Contains('>', StringComparison.Ordinal)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source,
                $"Front matter array '{key}' must not contain generic argument syntax; declare types via simple full names."));
            return [];
        }

        return [.. inner
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    // P-16: detect imperative-shaped bypass/impersonation instructions while NOT flagging
    // negation-shaped warnings (e.g., "do not bypass validation"). Defense-in-depth check, not
    // a security boundary — corpus authors can still write skill content that teaches agents
    // about boundaries without being misclassified as instructions to break them.
    private static bool ContainsUnsafeContent(string markdown)
        => ImperativeBypassRegex().IsMatch(markdown)
            || ImpersonationRegex().IsMatch(markdown);

    [GeneratedRegex(
        @"\b(?:must|should|may|let(?:\s+\w+)?|to|please|you\s+can)\s+bypass\s+(?:authorization|authentication|validation|tenant|policy|generated[\w-]*|team\s+policy)\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex ImperativeBypassRegex();

    [GeneratedRegex(
        @"\bimpersonate\s+(?:system|developer|tool)\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex ImpersonationRegex();

    internal static string Sha256Hex(string value) {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    [GeneratedRegex("^<!--\\s*frontcomposer:section\\s+(?<kind>[a-z-]+)\\s*-->$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex StartSectionRegex();

    [GeneratedRegex("^<!--\\s*/frontcomposer:section\\s*-->$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex EndSectionRegex();

    // P-? (DEF-5 hardening): IDs must start with a letter; numeric-only IDs are rejected.
    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex LowerIdPattern();
}
