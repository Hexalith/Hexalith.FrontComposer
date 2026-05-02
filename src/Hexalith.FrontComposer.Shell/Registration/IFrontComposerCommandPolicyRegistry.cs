using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Registration;

/// <summary>
/// Registry companion that resolves the effective command authorization policy and owning bounded
/// context in a single canonical lookup.
/// </summary>
/// <remarks>
/// The default registry uses last-write-wins semantics symmetric with manifest merge. Adopters
/// with custom <see cref="IFrontComposerRegistry"/> implementations get a default interface-method
/// fallback that walks <see cref="IFrontComposerRegistry.GetManifests"/>; overriding the method
/// lets registries plug in indexed, lock-snapshot, or hot-reload-aware lookups.
/// Story 7-3 Pass 4 DN-7-3-4-4 (b) — single canonical lookup returns both policy and bounded
/// context so dispatch, palette, empty-state CTA, and home capability surfaces share identical
/// resource shape (eliminates AA-08 / AA-23 drift).
/// </remarks>
public interface IFrontComposerCommandPolicyRegistry : IFrontComposerRegistry {
    /// <summary>
    /// Attempts to resolve the effective policy and owning bounded context for a command
    /// fully qualified type name in a single lookup.
    /// </summary>
    /// <param name="commandTypeName">The command fully qualified type name.</param>
    /// <param name="policyName">The resolved non-empty policy name when found.</param>
    /// <param name="boundedContext">
    /// The bounded context of the manifest that supplied the effective policy when found, or
    /// <see langword="null"/> when the manifest does not advertise a bounded context.
    /// </param>
    /// <returns><see langword="true"/> when a policy is registered for the command.</returns>
    bool TryGetCommandPolicy(string commandTypeName, out string policyName, out string? boundedContext) {
        policyName = string.Empty;
        boundedContext = null;
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        string trimmedKey = commandTypeName.Trim();
        foreach (DomainManifest manifest in GetManifests()) {
            if (!manifest.CommandPolicies.TryGetValue(trimmedKey, out string? candidate)
                || string.IsNullOrWhiteSpace(candidate)) {
                continue;
            }

            policyName = candidate.Trim();
            boundedContext = manifest.BoundedContext;
        }

        return policyName.Length > 0;
    }
}
