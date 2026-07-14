using System.Collections.Frozen;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed class FrontComposerSkillResourceProvider {
    private readonly IReadOnlyList<SkillCorpusResource> _resources;
    private readonly FrozenDictionary<string, SkillCorpusResource> _byUri;
    private readonly SkillResourceReadOptions _readOptions;
    private readonly string _aggregateMarkdown;

    public FrontComposerSkillResourceProvider(SkillCorpusSnapshot snapshot)
        : this(snapshot, SkillResourceReadOptions.Default) {
    }

    public FrontComposerSkillResourceProvider(SkillCorpusSnapshot snapshot, SkillResourceReadOptions readOptions) {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(readOptions);

        // P-45: fail fast at startup with a structured exception that carries the diagnostics
        // list, so an operator can triage which file failed without rebuilding under a debugger.
        if (snapshot.Diagnostics.Count > 0) {
            throw new InvalidSkillCorpusException(snapshot.Diagnostics);
        }

        _resources = snapshot.Resources;
        // P-37: URIs are canonicalized to lowercase at parse time, so all lookups use Ordinal.
        _byUri = snapshot.Resources.ToFrozenDictionary(r => r.ResourceUri, StringComparer.Ordinal);
        _readOptions = readOptions;
        AggregateManifest = SkillCorpusAggregateManifestBuilder.Build(snapshot);
        _aggregateMarkdown = SkillCorpusAggregateManifestBuilder.Render(AggregateManifest);
    }

    public IReadOnlyList<SkillResourceDescriptor> ListResources() {
        List<SkillResourceDescriptor> descriptors = [.. _resources.Select(ToDescriptor)];
        descriptors.Add(AggregateDescriptor);
        return descriptors;
    }

    public SkillCorpusAggregateManifest AggregateManifest { get; }

    public SkillResourceReadResult Read(string uri, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(uri);

        if (cancellationToken.IsCancellationRequested) {
            return SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.Canceled);
        }

        // P-43: aggregate manifest is served as a deterministic synthetic resource. Its size is
        // bounded by the per-resource cap so consumers cannot trigger an oversized payload via
        // the manifest URI either.
        if (string.Equals(uri, SkillCorpusAggregateManifestBuilder.ManifestResourceUri, StringComparison.Ordinal)) {
            return _aggregateMarkdown.Length > _readOptions.MaxCharacters
                ? SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.SkillResourceTooLarge)
                : SkillResourceReadResult.Success(_aggregateMarkdown);
        }

        if (!_byUri.TryGetValue(uri, out SkillCorpusResource? resource)) {
            return SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.UnknownResource);
        }

        if (cancellationToken.IsCancellationRequested) {
            return SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.Canceled);
        }

        return resource.Markdown.Length > _readOptions.MaxCharacters
            ? SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.SkillResourceTooLarge)
            : SkillResourceReadResult.Success(resource.Markdown);
    }

    public IReadOnlyList<FrontComposerSkillMcpResource> CreateMcpResources() {
        List<FrontComposerSkillMcpResource> result = [.. _resources.Select(r => new FrontComposerSkillMcpResource(ToDescriptor(r), this))];
        result.Add(new FrontComposerSkillMcpResource(AggregateDescriptor, this));
        return result;
    }

    /// <summary>
    /// P-24: callers must verify that skill resource URIs do not collide with manifest projection
    /// resource URIs at registration time. This method exposes the raw URI set for that check.
    /// </summary>
    public IReadOnlyCollection<string> ResourceUris {
        get {
            HashSet<string> set = new(StringComparer.Ordinal);
            foreach (SkillCorpusResource r in _resources) {
                _ = set.Add(r.ResourceUri);
            }

            _ = set.Add(SkillCorpusAggregateManifestBuilder.ManifestResourceUri);
            return set;
        }
    }

    private static SkillResourceDescriptor ToDescriptor(SkillCorpusResource resource)
        => new(
            resource.Id,
            resource.Title,
            "FrontComposer framework skill reference.",
            resource.ResourceUri,
            resource.ContentType,
            resource.Order,
            resource.Fingerprint);

    private static readonly SkillResourceDescriptor AggregateDescriptor = new(
        "skills-manifest",
        "FrontComposer skill corpus manifest",
        "Aggregate index of all FrontComposer skill resources with manifestSchemaVersion.",
        SkillCorpusAggregateManifestBuilder.ManifestResourceUri,
        "text/markdown",
        int.MaxValue,
        null);
}
