using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Routing;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Default registry-backed empty-state CTA resolver.
/// </summary>
public sealed class EmptyStateCtaResolver : IEmptyStateCtaResolver {
    private static readonly string[] CreationVerbPrefixes = ["Create", "Add", "New", "Register"];

    private readonly IFrontComposerRegistry _registry;
    private readonly ILogger<EmptyStateCtaResolver> _logger;

    public EmptyStateCtaResolver(
        IFrontComposerRegistry registry,
        ILogger<EmptyStateCtaResolver> logger) {
        _registry = registry;
        _logger = logger;
    }

    public EmptyStateCta? Resolve(Type projectionType, string? explicitCommandName = null) {
        ArgumentNullException.ThrowIfNull(projectionType);

        IReadOnlyList<DomainManifest> manifests;
        try {
            manifests = _registry.GetManifests();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to resolve empty-state CTA because the FrontComposer registry threw.");
            return null;
        }

        if (manifests.Count == 0) {
            return null;
        }

        string projectionFqn = projectionType.FullName ?? projectionType.Name;
        string? commandName = NormalizeCommandName(explicitCommandName)
            ?? NormalizeCommandName(projectionType.GetCustomAttributes(typeof(ProjectionEmptyStateCtaAttribute), inherit: false)
                .OfType<ProjectionEmptyStateCtaAttribute>()
                .FirstOrDefault()
                ?.CommandTypeName);

        if (commandName is not null) {
            foreach (DomainManifest manifest in manifests) {
                string? command = manifest.Commands.FirstOrDefault(c => CommandMatches(c, commandName));
                if (command is not null && _registry.HasFullPageRoute(command)) {
                    return BuildCta(manifest.BoundedContext, command);
                }
            }

            _logger.LogWarning(
                "Projection {ProjectionType} requested empty-state CTA command {CommandName}, but no reachable registered command matched.",
                projectionFqn,
                commandName);
            return null;
        }

        string? boundedContext = ResolveBoundedContext(projectionType, projectionFqn, manifests);
        if (string.IsNullOrWhiteSpace(boundedContext)) {
            return null;
        }

        DomainManifest[] matchingManifests = [.. manifests
            .Where(m => string.Equals(m.BoundedContext, boundedContext, StringComparison.OrdinalIgnoreCase))];
        if (matchingManifests.Length == 0) {
            return null;
        }

        string[] reachableCommands = [.. matchingManifests
            .SelectMany(m => m.Commands.Select(c => new { Manifest = m, Command = c }))
            .Where(x => _registry.HasFullPageRoute(x.Command))
            .OrderBy(x => CreationVerbRank(TypeName(x.Command)))
            .ThenBy(x => TypeName(x.Command), StringComparer.Ordinal)
            .Select(x => x.Manifest.BoundedContext + "\n" + x.Command)];

        if (reachableCommands.Length == 0) {
            return null;
        }

        string[] parts = reachableCommands[0].Split('\n');
        return BuildCta(parts[0], parts[1]);
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

    private static EmptyStateCta BuildCta(string boundedContext, string commandTypeName)
        => new(
            commandTypeName,
            HumanizeCommandName(TypeName(commandTypeName)),
            CommandRouteBuilder.BuildRoute(boundedContext, commandTypeName));

    private static string? NormalizeCommandName(string? commandName)
        => string.IsNullOrWhiteSpace(commandName) ? null : commandName.Trim();

    private static bool CommandMatches(string registeredCommand, string requestedCommand)
        => string.Equals(registeredCommand, requestedCommand, StringComparison.Ordinal)
            || string.Equals(TypeName(registeredCommand), requestedCommand, StringComparison.Ordinal)
            || string.Equals(TypeName(registeredCommand), TypeName(requestedCommand), StringComparison.Ordinal);

    private static int CreationVerbRank(string commandTypeName) {
        for (int i = 0; i < CreationVerbPrefixes.Length; i++) {
            if (commandTypeName.StartsWith(CreationVerbPrefixes[i], StringComparison.Ordinal)) {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static string TypeName(string typeName) {
        int idx = typeName.LastIndexOf('.');
        return idx >= 0 ? typeName[(idx + 1)..] : typeName;
    }

    private static string HumanizeCommandName(string commandTypeName) {
        string name = commandTypeName.EndsWith("Command", StringComparison.Ordinal)
            ? commandTypeName[..^"Command".Length]
            : commandTypeName;

        System.Text.StringBuilder sb = new(name.Length + 4);
        for (int i = 0; i < name.Length; i++) {
            char c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1])) {
                _ = sb.Append(' ');
            }

            _ = sb.Append(c);
        }

        return sb.ToString();
    }
}
