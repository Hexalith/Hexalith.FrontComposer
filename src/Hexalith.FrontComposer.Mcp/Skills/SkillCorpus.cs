using System.Collections.Frozen;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Schema;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Skills;

public enum SkillCorpusDiagnosticCategory {
    MissingFrontMatter,
    InvalidFrontMatter,
    InvalidSectionMarker,
    DuplicateResource,
    MissingPublicApiReference,
    MissingSamplePath,
    UnsafeContent,
    MigrationGuideMissing,
}

public sealed record SkillCorpusDiagnostic(
    SkillCorpusDiagnosticCategory Category,
    string Source,
    string Message,
    string? Section = null);

public sealed record SkillCorpusSource(string Path, string Text);

public sealed record SkillCorpusResource(
    string Id,
    string Title,
    string Version,
    string Audience,
    bool Docfx,
    bool McpResource,
    string ResourceUri,
    int Order,
    string SourceDoc,
    bool Narrative,
    bool References,
    string? MigrationOwner,
    string Markdown,
    IReadOnlyList<string> PublicApiReferences,
    IReadOnlyList<string> SamplePaths,
    SchemaFingerprint? Fingerprint = null) {
    public string ContentType => McpResource ? "text/markdown" : "text/plain";
}

public sealed record SkillCorpusSnapshot(
    IReadOnlyList<SkillCorpusResource> Resources,
    IReadOnlyList<SkillCorpusDiagnostic> Diagnostics);

public sealed record SkillCorpusValidationResult(IReadOnlyList<SkillCorpusDiagnostic> Diagnostics) {
    public bool IsValid => Diagnostics.Count == 0;
}

public static partial class SkillCorpusParser {
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
            SkillCorpusResource? resource = ParseOne(source, diagnostics);
            if (resource is not null) {
                resources.Add(resource);
            }
        }

        HashSet<string> resourceUris = new(StringComparer.OrdinalIgnoreCase);
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

    private static SkillCorpusResource? ParseOne(SkillCorpusSource source, List<SkillCorpusDiagnostic> diagnostics) {
        string[] lines = source.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
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

            frontMatter[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        ValidateFrontMatterKeys(source.Path, frontMatter, diagnostics);
        string body = string.Join('\n', lines.Skip(end + 1));
        string? markdown = ExtractAgentReference(source.Path, body, diagnostics);
        if (diagnostics.Count > 0 || markdown is null) {
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

        if (!LowerIdPattern().IsMatch(id)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                "Skill id must be lowercase kebab-case."));
        }

        if (!resourceUri.StartsWith("frontcomposer://skills/", StringComparison.OrdinalIgnoreCase)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.InvalidFrontMatter,
                source.Path,
                "Skill resourceUri must be a frontcomposer://skills/... URI."));
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

        if (diagnostics.Count > 0) {
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
            ReadArray(frontMatter.GetValueOrDefault("publicApiReferences")),
            ReadArray(frontMatter.GetValueOrDefault("samplePaths")));
    }

    private static SkillCorpusResource WithFingerprint(SkillCorpusResource resource) {
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
                ["contentType"] = resource.ContentType,
                ["docfx"] = resource.Docfx ? "true" : "false",
                ["mcpResource"] = resource.McpResource ? "true" : "false",
                ["order"] = resource.Order.ToString(System.Globalization.CultureInfo.InvariantCulture),
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

        foreach (string rawLine in body.Split('\n')) {
            string line = rawLine.Trim();
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
                current.Clear();
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
                current.Clear();
                continue;
            }

            if (active is not null) {
                current.AppendLine(rawLine);
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
        if (int.TryParse(value, out int parsed) && parsed >= 0) {
            return parsed;
        }

        diagnostics.Add(new SkillCorpusDiagnostic(
            SkillCorpusDiagnosticCategory.InvalidFrontMatter,
            source,
            $"Front matter field '{key}' must be a non-negative integer."));
        return 0;
    }

    private static IReadOnlyList<string> ReadArray(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return [];
        }

        string trimmed = value.Trim();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal) || !trimmed.EndsWith("]", StringComparison.Ordinal)) {
            return [trimmed];
        }

        return [.. trimmed[1..^1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    private static bool ContainsUnsafeContent(string markdown)
        => markdown.Contains("bypass authorization", StringComparison.OrdinalIgnoreCase)
            || markdown.Contains("impersonate system", StringComparison.OrdinalIgnoreCase)
            || markdown.Contains("impersonate developer", StringComparison.OrdinalIgnoreCase)
            || markdown.Contains("impersonate tool", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^<!--\\s*frontcomposer:section\\s+(?<kind>[a-z-]+)\\s*-->$", RegexOptions.CultureInvariant)]
    private static partial Regex StartSectionRegex();

    [GeneratedRegex("^<!--\\s*/frontcomposer:section\\s*-->$", RegexOptions.CultureInvariant)]
    private static partial Regex EndSectionRegex();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex LowerIdPattern();
}

public static class SkillCorpusLoader {
    private const string SkillResourcePrefix = "Hexalith.FrontComposer.Mcp.Skills.";

    public static SkillCorpusSnapshot LoadEmbedded() {
        Assembly assembly = typeof(SkillCorpusLoader).Assembly;
        SkillCorpusSource[] sources = [.. assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(SkillResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".md", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new SkillCorpusSource(ToPath(name), ReadResource(assembly, name)))];

        return SkillCorpusParser.Parse(sources);
    }

    private static string ToPath(string resourceName)
        => "docs/skills/frontcomposer/" + resourceName[SkillResourcePrefix.Length..].Replace('.', '/');

    private static string ReadResource(Assembly assembly, string resourceName) {
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) {
            return string.Empty;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

public static class SkillCorpusReferenceValidator {
    public static SkillCorpusValidationResult Validate(
        SkillCorpusSnapshot snapshot,
        IEnumerable<Assembly> publicApiAssemblies,
        string projectRoot = "") {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(publicApiAssemblies);

        List<SkillCorpusDiagnostic> diagnostics = [.. snapshot.Diagnostics];
        Assembly[] assemblies = [.. publicApiAssemblies];

        foreach (SkillCorpusResource resource in snapshot.Resources) {
            foreach (string reference in resource.PublicApiReferences) {
                if (FindType(reference, assemblies) is null) {
                    diagnostics.Add(new SkillCorpusDiagnostic(
                        SkillCorpusDiagnosticCategory.MissingPublicApiReference,
                        resource.SourceDoc,
                        $"Public API reference '{reference}' was not found."));
                }
            }

            if (!string.IsNullOrWhiteSpace(projectRoot)) {
                foreach (string samplePath in resource.SamplePaths) {
                    string fullPath = Path.Combine(projectRoot, samplePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!Directory.Exists(fullPath) && !File.Exists(fullPath)) {
                        diagnostics.Add(new SkillCorpusDiagnostic(
                            SkillCorpusDiagnosticCategory.MissingSamplePath,
                            resource.SourceDoc,
                            $"Sample path '{samplePath}' was not found."));
                    }
                }
            }
        }

        return new SkillCorpusValidationResult(diagnostics);
    }

    private static Type? FindType(string fullName, IEnumerable<Assembly> assemblies) {
        foreach (Assembly assembly in assemblies) {
            Type? type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (type is not null) {
                return type;
            }
        }

        return Type.GetType(fullName, throwOnError: false, ignoreCase: false);
    }
}

public sealed record SkillResourceDescriptor(
    string Id,
    string Title,
    string Description,
    string ResourceUri,
    string ContentType,
    int Order,
    SchemaFingerprint? Fingerprint = null);

public sealed record SkillResourceReadResult(
    bool IsSuccess,
    FrontComposerMcpFailureCategory Category,
    string ContentType,
    string Markdown) {
    public static SkillResourceReadResult Success(string markdown)
        => new(true, FrontComposerMcpFailureCategory.None, "text/markdown", markdown);

    public static SkillResourceReadResult Failure(FrontComposerMcpFailureCategory category)
        => new(false, category, "text/plain", category.ToString());
}

public sealed class FrontComposerSkillResourceProvider {
    private readonly IReadOnlyList<SkillCorpusResource> _resources;
    private readonly FrozenDictionary<string, SkillCorpusResource> _byUri;

    public FrontComposerSkillResourceProvider(SkillCorpusSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Diagnostics.Count > 0) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MissingManifest);
        }

        _resources = snapshot.Resources;
        _byUri = snapshot.Resources.ToFrozenDictionary(r => r.ResourceUri, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SkillResourceDescriptor> ListResources()
        => [.. _resources.Select(ToDescriptor)];

    public SkillResourceReadResult Read(string uri, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.Canceled);
        }

        return _byUri.TryGetValue(uri, out SkillCorpusResource? resource)
            ? SkillResourceReadResult.Success(resource.Markdown)
            : SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.UnknownResource);
    }

    public IReadOnlyList<FrontComposerSkillMcpResource> CreateMcpResources()
        => [.. _resources.Select(r => new FrontComposerSkillMcpResource(ToDescriptor(r), this))];

    private static SkillResourceDescriptor ToDescriptor(SkillCorpusResource resource)
        => new(
            resource.Id,
            resource.Title,
            "FrontComposer framework skill reference.",
            resource.ResourceUri,
            resource.ContentType,
            resource.Order,
            resource.Fingerprint);
}

public sealed class FrontComposerSkillMcpResource(
    SkillResourceDescriptor descriptor,
    FrontComposerSkillResourceProvider provider) : McpServerResource {
    private readonly Resource _resource = new() {
        Uri = descriptor.ResourceUri,
        Name = descriptor.Id,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = descriptor.ContentType,
    };

    public SkillResourceDescriptor Descriptor => descriptor;

    public override Resource ProtocolResource => _resource;

    public override ResourceTemplate ProtocolResourceTemplate
        => throw new NotSupportedException("FrontComposer skill resources do not expose URI templates in v1.");

    public override IReadOnlyList<object> Metadata { get; } = [descriptor];

    public override bool IsMatch(string uri)
        => string.Equals(uri, descriptor.ResourceUri, StringComparison.Ordinal);

    public override ValueTask<ReadResourceResult> ReadAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string? uri = request.Params?.Uri;
        SkillResourceReadResult result = string.IsNullOrWhiteSpace(uri)
            ? SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest)
            : provider.Read(uri, cancellationToken);

        return ValueTask.FromResult(new ReadResourceResult {
            Contents = [
                new TextResourceContents {
                    Uri = descriptor.ResourceUri,
                    MimeType = result.ContentType,
                    Text = result.Markdown,
                },
            ],
        });
    }
}

public enum GeneratedCodeFailureCategory {
    None,
    Compile,
    PackageBoundary,
    MissingRegistration,
    InvalidAttribute,
    ValidationShape,
    TenantSpoofing,
    GeneratedFileEdit,
    TestScaffold,
    SourceToolsManifest,
    Unknown,
}

public sealed record GeneratedCodeFile(string Path, string Content);

public sealed record GeneratedCodeDiagnostic(
    GeneratedCodeFailureCategory Category,
    string Path,
    string Message);

public sealed record GeneratedCodeValidationResult(IReadOnlyList<GeneratedCodeDiagnostic> Diagnostics) {
    public bool IsValid => Diagnostics.Count == 0;
}

public static partial class GeneratedBoundedContextValidator {
    private static readonly FrozenSet<string> ApprovedPackages = new[] {
        "Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.SourceTools",
        "Microsoft.NET.Test.Sdk",
        "xunit.v3",
        "xunit.v3.assert",
        "Shouldly",
    }.ToFrozenSet(StringComparer.Ordinal);

    public static GeneratedCodeValidationResult Validate(IEnumerable<GeneratedCodeFile> files) {
        ArgumentNullException.ThrowIfNull(files);

        GeneratedCodeFile[] input = [.. files];
        List<GeneratedCodeDiagnostic> diagnostics = [];

        foreach (GeneratedCodeFile file in input) {
            ValidateFile(file, diagnostics);
        }

        if (diagnostics.Any(d => d.Category == GeneratedCodeFailureCategory.PackageBoundary)) {
            return new GeneratedCodeValidationResult(diagnostics);
        }

        bool hasCommand = input.Any(f => f.Content.Contains("[Command]", StringComparison.Ordinal));
        bool hasProjection = input.Any(f => f.Content.Contains("[Projection]", StringComparison.Ordinal));
        bool hasRegistration = input.Any(f => f.Content.Contains("Add", StringComparison.Ordinal) && f.Content.Contains("FrontComposer", StringComparison.Ordinal));
        bool hasValidator = input.Any(f => f.Path.Contains("Validator", StringComparison.OrdinalIgnoreCase));
        bool hasTests = input.Any(f => f.Path.Contains(".Tests", StringComparison.OrdinalIgnoreCase) || f.Path.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase));
        bool hasSourceToolsManifest = input.Any(f => f.Path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            && f.Path.Contains("obj", StringComparison.OrdinalIgnoreCase)
            && f.Content.Contains("manifest", StringComparison.OrdinalIgnoreCase));

        if (!hasCommand || !hasProjection) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.InvalidAttribute,
                "",
                "Generated bounded context must include command and projection attributes."));
        }

        if (!hasRegistration) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.MissingRegistration,
                "",
                "Generated bounded context must include FrontComposer registration."));
        }

        if (!hasValidator) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.ValidationShape,
                "",
                "Generated bounded context must include validation shape."));
        }

        if (!hasTests) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.TestScaffold,
                "",
                "Generated bounded context must include tests."));
        }

        if (!hasSourceToolsManifest) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.SourceToolsManifest,
                "",
                "Generated bounded context must include SourceTools manifest output."));
        }

        if (input.Any(f => f.Content.Contains("COMPILE_ERROR", StringComparison.Ordinal))) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.Compile,
                "",
                "Generated bounded context did not compile."));
        }

        return new GeneratedCodeValidationResult(diagnostics);
    }

    private static void ValidateFile(GeneratedCodeFile file, List<GeneratedCodeDiagnostic> diagnostics) {
        string path = file.Path.Replace('\\', '/');

        if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) && !path.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.GeneratedFileEdit,
                file.Path,
                "Generated files must not be hand-edited."));
        }

        if (CommandClassRegex().IsMatch(file.Content)
            && (file.Content.Contains("TenantId", StringComparison.Ordinal) || file.Content.Contains("UserId", StringComparison.Ordinal))) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.TenantSpoofing,
                file.Path,
                "Agent-authored command inputs must not contain tenant/user spoofing fields."));
        }

        if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        if (file.Content.Contains("<Target", StringComparison.OrdinalIgnoreCase)
            || file.Content.Contains("<Exec", StringComparison.OrdinalIgnoreCase)
            || file.Content.Contains("<Import", StringComparison.OrdinalIgnoreCase)
            || file.Content.Contains("<PackageSource", StringComparison.OrdinalIgnoreCase)
            || file.Content.Contains("<RestoreSources", StringComparison.OrdinalIgnoreCase)
            || file.Content.Contains("PostBuildEvent", StringComparison.OrdinalIgnoreCase)) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.PackageBoundary,
                file.Path,
                "Unsafe MSBuild project shape is not allowed."));
        }

        foreach (Match match in PackageReferenceRegex().Matches(file.Content)) {
            string packageName = match.Groups["name"].Value;
            if (!ApprovedPackages.Contains(packageName)) {
                diagnostics.Add(new GeneratedCodeDiagnostic(
                    GeneratedCodeFailureCategory.PackageBoundary,
                    file.Path,
                    $"PackageReference '{packageName}' is not approved."));
            }
        }
    }

    [GeneratedRegex("\\[Command\\][\\s\\S]*?class\\s+\\w+Command", RegexOptions.CultureInvariant)]
    private static partial Regex CommandClassRegex();

    [GeneratedRegex("<PackageReference\\s+Include=\"(?<name>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex PackageReferenceRegex();
}

public sealed record SkillBenchmarkPrompt(
    string Id,
    string Text,
    IReadOnlyList<string> ExpectedShape);

public sealed record SkillBenchmarkPromptSet(
    string Version,
    IReadOnlyList<SkillBenchmarkPrompt> Prompts) {
    public static SkillBenchmarkPromptSet LoadEmbeddedV1() {
        Assembly assembly = typeof(SkillBenchmarkPromptSet).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.EndsWith("benchmark-prompts.v1.prompt-set.json", StringComparison.Ordinal));
        if (resourceName is null) {
            return new SkillBenchmarkPromptSet("1.0.0", []);
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded benchmark prompt set is missing.");
        var dto = JsonSerializer.Deserialize(stream, SkillBenchmarkJsonContext.Default.SkillBenchmarkPromptSetDto)
            ?? throw new InvalidOperationException("Embedded benchmark prompt set is invalid.");

        return new SkillBenchmarkPromptSet(
            dto.Version,
            [.. dto.Prompts
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .Select(p => new SkillBenchmarkPrompt(p.Id, p.Text, p.ExpectedShape))]);
    }
}

public sealed record SkillBenchmarkModelConfig(
    string ProviderId,
    string ModelId,
    double Temperature,
    int? Seed,
    int TimeoutSeconds,
    int RetryCount);

public sealed record SkillBenchmarkCacheKey(string Value) {
    public static SkillBenchmarkCacheKey Create(
        SkillBenchmarkPrompt prompt,
        string frameworkVersion,
        string corpusVersion,
        SkillBenchmarkModelConfig config,
        string scorerVersion,
        string validatorVersion,
        string redactionPolicyVersion) {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(config);

        string json = JsonSerializer.Serialize(new {
            prompt.Id,
            prompt.Text,
            frameworkVersion,
            corpusVersion,
            config,
            scorerVersion,
            validatorVersion,
            redactionPolicyVersion,
        });
        return new SkillBenchmarkCacheKey(Sha256(json));
    }

    private static string Sha256(string value) {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class SkillBenchmarkCachePolicy {
    public static bool CanReuse(SkillBenchmarkCacheKey expected, SkillBenchmarkCacheKey actual) {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        return string.Equals(expected.Value, actual.Value, StringComparison.Ordinal);
    }

    public static string CacheMissReason(SkillBenchmarkCacheKey expected, SkillBenchmarkCacheKey actual)
        => CanReuse(expected, actual) ? string.Empty : "contract-input-changed";
}

public enum SkillBenchmarkRedactionStatus {
    Passed,
    Failed,
}

public sealed record SkillBenchmarkResult(
    string PromptId,
    string FrameworkVersion,
    string CorpusVersion,
    string ModelId,
    string ProviderConfigHash,
    string ScorerVersion,
    string ValidatorVersion,
    bool CompileSucceeded,
    bool ValidatorSucceeded,
    GeneratedCodeFailureCategory FailureCategory,
    SkillBenchmarkRedactionStatus RedactionStatus,
    string GeneratedArtifactToken,
    IReadOnlyList<string> SanitizedDiagnostics);

public sealed record SkillBenchmarkArtifactBuildResult(
    bool CanPersist,
    IReadOnlyList<string> Diagnostics,
    string? ArtifactJson);

public static class SkillBenchmarkArtifactWriter {
    public static bool CanPersist(SkillBenchmarkResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return result.RedactionStatus == SkillBenchmarkRedactionStatus.Passed;
    }

    public static SkillBenchmarkArtifactBuildResult TryBuildArtifact(SkillBenchmarkResult result) {
        if (!CanPersist(result)) {
            return new SkillBenchmarkArtifactBuildResult(false, ["redaction-not-passed"], null);
        }

        return new SkillBenchmarkArtifactBuildResult(
            true,
            [],
            JsonSerializer.Serialize(result));
    }
}

public sealed record SkillBenchmarkScore(
    bool Passed,
    GeneratedCodeFailureCategory FailureCategory);

public static class SkillBenchmarkOfflineScorer {
    public static SkillBenchmarkScore Score(SkillBenchmarkPrompt prompt, IEnumerable<GeneratedCodeFile> generatedFiles) {
        ArgumentNullException.ThrowIfNull(prompt);
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate(generatedFiles);
        return result.IsValid
            ? new SkillBenchmarkScore(true, GeneratedCodeFailureCategory.None)
            : new SkillBenchmarkScore(false, result.Diagnostics[0].Category);
    }
}

public static class SkillCorpusReleaseGuard {
    public static SkillCorpusValidationResult ValidateBreakingChangesRequireMigration(IEnumerable<SkillCorpusResource> changedResources) {
        ArgumentNullException.ThrowIfNull(changedResources);

        List<SkillCorpusDiagnostic> diagnostics = [];
        foreach (SkillCorpusResource resource in changedResources) {
            if (string.IsNullOrWhiteSpace(resource.MigrationOwner)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.MigrationGuideMissing,
                    resource.SourceDoc,
                    "Breaking skill corpus changes require migrationOwner metadata."));
            }
        }

        return new SkillCorpusValidationResult(diagnostics);
    }
}

internal sealed record SkillBenchmarkPromptSetDto(
    string Version,
    IReadOnlyList<SkillBenchmarkPromptDto> Prompts);

internal sealed record SkillBenchmarkPromptDto(
    string Id,
    string Text,
    IReadOnlyList<string> ExpectedShape);

[JsonSerializable(typeof(SkillBenchmarkPromptSetDto))]
[JsonSerializable(typeof(SkillBenchmarkResult))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class SkillBenchmarkJsonContext : JsonSerializerContext;
