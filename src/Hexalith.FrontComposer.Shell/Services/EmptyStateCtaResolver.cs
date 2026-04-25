using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Routing;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Default registry-backed empty-state CTA resolver (Story 4-6 D4/D5).
/// </summary>
public sealed class EmptyStateCtaResolver : IEmptyStateCtaResolver {
    // English-only creation verbs covering common CRUD vocabularies; non-matching domain verbs
    // require explicit [ProjectionEmptyStateCta(nameof(...))] opt-in. See Story 4-6 revised D4.
    private static readonly string[] CreationVerbPrefixes = ["Create", "Add", "New", "Send", "Register", "Start", "Place", "Submit", "Issue", "Open", "Initiate"];

    private readonly IFrontComposerRegistry _registry;
    private readonly ILogger<EmptyStateCtaResolver> _logger;

    public EmptyStateCtaResolver(
        IFrontComposerRegistry registry,
        ILogger<EmptyStateCtaResolver> logger) {
        _registry = registry;
        _logger = logger;
    }

    public EmptyStateCta? Resolve(Type projectionType) {
        ArgumentNullException.ThrowIfNull(projectionType);

        IReadOnlyList<DomainManifest>? manifests = TryGetManifests();
        if (manifests is null || manifests.Count == 0) {
            return null;
        }

        string projectionFqn = projectionType.FullName ?? projectionType.Name;
        string? attributeCommandName = NormalizeCommandName(projectionType
            .GetCustomAttributes(typeof(ProjectionEmptyStateCtaAttribute), inherit: false)
            .OfType<ProjectionEmptyStateCtaAttribute>()
            .FirstOrDefault()
            ?.CommandTypeName);

        return attributeCommandName is not null
            ? ResolveExplicitInternal(projectionFqn, attributeCommandName, manifests)
            : ResolveByBoundedContext(projectionType, projectionFqn, manifests);
    }

    public EmptyStateCta? ResolveExplicit(Type projectionType, string commandName) {
        ArgumentNullException.ThrowIfNull(projectionType);
        string? normalized = NormalizeCommandName(commandName);
        if (normalized is null) {
            return null;
        }

        IReadOnlyList<DomainManifest>? manifests = TryGetManifests();
        if (manifests is null || manifests.Count == 0) {
            return null;
        }

        string projectionFqn = projectionType.FullName ?? projectionType.Name;
        return ResolveExplicitInternal(projectionFqn, normalized, manifests);
    }

    private IReadOnlyList<DomainManifest>? TryGetManifests() {
        try {
            return _registry.GetManifests();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to resolve empty-state CTA because the FrontComposer registry threw.");
            return null;
        }
    }

    private EmptyStateCta? ResolveExplicitInternal(
        string projectionFqn,
        string explicitCommandName,
        IReadOnlyList<DomainManifest> manifests) {
        // Iterate ALL matches (not FirstOrDefault per manifest) so a match later in the list
        // still wins when an earlier match lacks a route. Disambiguate cross-manifest collisions
        // by warning when more than one reachable match exists.
        var matches = manifests
            .SelectMany(m => m.Commands
                .Where(c => CommandMatches(c, explicitCommandName))
                .Select(c => (BoundedContext: m.BoundedContext, Command: c)))
            .Where(x => _registry.HasFullPageRoute(x.Command))
            .Where(x => _registry.IsCommandWritable(x.Command))
            .ToList();

        if (matches.Count == 0) {
            _logger.LogWarning(
                "Projection {ProjectionType} requested empty-state CTA command {CommandName}, but no reachable writable registered command matched.",
                projectionFqn,
                explicitCommandName);
            return null;
        }

        if (matches.Count > 1) {
            _logger.LogWarning(
                "Projection {ProjectionType} requested empty-state CTA command {CommandName}, which matched {Count} registered commands across bounded contexts. Picking the first ({BoundedContext}.{Command}); annotate with [ProjectionEmptyStateCta] using the fully qualified name to disambiguate.",
                projectionFqn,
                explicitCommandName,
                matches.Count,
                matches[0].BoundedContext,
                matches[0].Command);
        }

        return BuildCta(matches[0].BoundedContext, matches[0].Command);
    }

    private EmptyStateCta? ResolveByBoundedContext(
        Type projectionType,
        string projectionFqn,
        IReadOnlyList<DomainManifest> manifests) {
        string? boundedContext = ResolveBoundedContext(projectionType, projectionFqn, manifests);
        if (string.IsNullOrWhiteSpace(boundedContext)) {
            return null;
        }

        DomainManifest[] matchingManifests = [.. manifests
            .Where(m => string.Equals(m.BoundedContext, boundedContext, StringComparison.OrdinalIgnoreCase))];
        if (matchingManifests.Length == 0) {
            return null;
        }

        // Spec D4: filter to writable + reachable commands, partition into creation-verb-prefix
        // matches vs non-matches, and order each partition alphabetically. The verb-prefix
        // partition wins (matches first); within each partition, alphabetical by simple type name.
        var writableReachable = matchingManifests
            .SelectMany(m => m.Commands.Select(c => (BoundedContext: m.BoundedContext, Command: c)))
            .Where(x => _registry.HasFullPageRoute(x.Command))
            .Where(x => _registry.IsCommandWritable(x.Command))
            .ToList();

        if (writableReachable.Count == 0) {
            return null;
        }

        var ordered = writableReachable
            .OrderBy(x => HasCreationVerbPrefix(TypeName(x.Command)) ? 0 : 1)
            .ThenBy(x => TypeName(x.Command), StringComparer.Ordinal)
            .ToList();

        return BuildCta(ordered[0].BoundedContext, ordered[0].Command);
    }

    private static string? ResolveBoundedContext(
        Type projectionType,
        string projectionFqn,
        IReadOnlyList<DomainManifest> manifests) {
        string? attrBoundedContext = projectionType
            .GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .OfType<BoundedContextAttribute>()
            .FirstOrDefault()
            ?.Name;
        if (!string.IsNullOrWhiteSpace(attrBoundedContext)) {
            return attrBoundedContext;
        }

        DomainManifest? owningManifest = manifests.FirstOrDefault(m => m.Projections.Contains(projectionFqn, StringComparer.Ordinal));
        return owningManifest?.BoundedContext;
    }

    private static EmptyStateCta BuildCta(string boundedContext, string commandFqn)
        => new(
            commandFqn,
            HumanizeCommandName(TypeName(commandFqn)),
            CommandRouteBuilder.BuildRoute(boundedContext, commandFqn),
            // AuthorizationPolicy is intentionally null in v1: the component wraps every CTA in
            // a default <AuthorizeView> (no Policy=) which delivers AC2.5 (anonymous users see
            // no CTA). Per-command policy discovery requires either a trim-safe registry
            // companion (Story 4-7+ follow-up — see Story 4-6 review findings) or AOT-friendly
            // source-generator emit; reflection-based discovery is incompatible with the
            // project's IsTrimmable=true posture.
            AuthorizationPolicy: null);

    private static string? NormalizeCommandName(string? commandName)
        => string.IsNullOrWhiteSpace(commandName) ? null : commandName.Trim();

    private static bool CommandMatches(string registeredCommand, string requestedCommand)
        => string.Equals(registeredCommand, requestedCommand, StringComparison.Ordinal)
            || string.Equals(TypeName(registeredCommand), requestedCommand, StringComparison.Ordinal)
            || string.Equals(TypeName(registeredCommand), TypeName(requestedCommand), StringComparison.Ordinal);

    private static bool HasCreationVerbPrefix(string commandTypeName) {
        for (int i = 0; i < CreationVerbPrefixes.Length; i++) {
            if (commandTypeName.StartsWith(CreationVerbPrefixes[i], StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static string TypeName(string typeName) {
        int idx = typeName.LastIndexOf('.');
        return idx >= 0 ? typeName[(idx + 1)..] : typeName;
    }

    private static string HumanizeCommandName(string commandTypeName) {
        string name = commandTypeName.EndsWith("Command", StringComparison.Ordinal)
            ? commandTypeName[..^"Command".Length]
            : commandTypeName;

        // Insert spaces before uppercase letters with two boundary rules:
        //   (a) lower → upper transition (CreateOrder → "Create Order")
        //   (b) upper → upper-followed-by-lower (URLImport → "URL Import")
        StringBuilder sb = new(name.Length + 4);
        for (int i = 0; i < name.Length; i++) {
            char c = name[i];
            if (i > 0 && char.IsUpper(c)) {
                bool prevIsLower = !char.IsUpper(name[i - 1]);
                bool nextIsLower = i + 1 < name.Length && !char.IsUpper(name[i + 1]) && char.IsLetter(name[i + 1]);
                if (prevIsLower || (char.IsUpper(name[i - 1]) && nextIsLower)) {
                    _ = sb.Append(' ');
                }
            }

            _ = sb.Append(c);
        }

        return sb.ToString();
    }
}
