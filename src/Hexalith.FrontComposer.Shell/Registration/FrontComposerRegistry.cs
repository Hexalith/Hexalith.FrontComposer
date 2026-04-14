namespace Hexalith.FrontComposer.Shell.Registration;

using System.Linq;

using Hexalith.FrontComposer.Contracts.Registration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="IFrontComposerRegistry"/>.
/// Stores registered domain manifests and navigation groups for runtime composition.
/// Domain registrations from <see cref="DomainRegistrationAction"/> are applied on construction.
/// </summary>
internal sealed class FrontComposerRegistry : IFrontComposerRegistry
{
    private readonly List<DomainManifest> _manifests = [];
    private readonly List<(string Name, string BoundedContext)> _navGroups = [];

    public FrontComposerRegistry(
        IEnumerable<DomainRegistrationAction> registrationActions,
        IEnumerable<DomainRegistrationWarning> warnings,
        ILogger<FrontComposerRegistry> logger)
    {
        foreach (DomainRegistrationWarning warning in warnings)
        {
            logger.LogWarning(
                "Skipping registration type {RegistrationType}: expected a static Manifest member and RegisterDomain(IFrontComposerRegistry) method. Found Manifest={HasManifest}, RegisterDomain={HasRegisterMethod}.",
                warning.RegistrationType,
                warning.HasManifest,
                warning.HasRegisterMethod);
        }

        foreach (DomainRegistrationAction action in registrationActions)
        {
            action.Apply(this);
        }
    }

    /// <inheritdoc />
    public void AddNavGroup(string name, string boundedContext)
    {
        _navGroups.Add((name, boundedContext));
    }

    /// <inheritdoc />
    public IReadOnlyList<DomainManifest> GetManifests() => _manifests;

    /// <inheritdoc />
    public void RegisterDomain(DomainManifest manifest)
    {
        int existingIndex = _manifests.FindIndex(m => string.Equals(m.BoundedContext, manifest.BoundedContext, StringComparison.Ordinal));
        if (existingIndex < 0)
        {
            _manifests.Add(Clone(manifest));
            return;
        }

        DomainManifest existing = _manifests[existingIndex];
        string name = ChooseName(existing.Name, manifest.Name, existing.BoundedContext);
        _manifests[existingIndex] = new DomainManifest(
            name,
            existing.BoundedContext,
            [.. existing.Projections.Concat(manifest.Projections).Distinct(StringComparer.Ordinal)],
            [.. existing.Commands.Concat(manifest.Commands).Distinct(StringComparer.Ordinal)]);
    }

    private static DomainManifest Clone(DomainManifest manifest)
        => new(
            manifest.Name,
            manifest.BoundedContext,
            [.. manifest.Projections],
            [.. manifest.Commands]);

    private static string ChooseName(string currentName, string candidateName, string boundedContext)
    {
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return candidateName;
        }

        if (string.Equals(currentName, boundedContext, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(candidateName)
            && !string.Equals(candidateName, boundedContext, StringComparison.Ordinal))
        {
            return candidateName;
        }

        return currentName;
    }
}
