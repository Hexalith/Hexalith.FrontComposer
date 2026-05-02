using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Registration;

/// <summary>
/// Shared command-policy lookup for registries that do not implement the optional indexed
/// companion interface.
/// </summary>
internal static class FrontComposerCommandPolicyLookup {
    public static bool TryGetCommandPolicy(
        IFrontComposerRegistry registry,
        string commandTypeName,
        out string policyName,
        out string? boundedContext) {
        ArgumentNullException.ThrowIfNull(registry);

        policyName = string.Empty;
        boundedContext = null;
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        string trimmedKey = commandTypeName.Trim();
        if (registry is IFrontComposerCommandPolicyRegistry policyRegistry
            && policyRegistry.TryGetCommandPolicy(trimmedKey, out policyName, out boundedContext)) {
            policyName = policyName.Trim();
            return policyName.Length > 0;
        }

        IReadOnlyList<DomainManifest> manifests = registry.GetManifests();
        foreach (DomainManifest manifest in manifests) {
            if (manifest.CommandPolicies is null) {
                continue;
            }

            foreach (KeyValuePair<string, string> pair in manifest.CommandPolicies) {
                if (string.IsNullOrWhiteSpace(pair.Key)
                    || string.IsNullOrWhiteSpace(pair.Value)
                    || !string.Equals(pair.Key.Trim(), trimmedKey, StringComparison.Ordinal)) {
                    continue;
                }

                policyName = pair.Value.Trim();
                boundedContext = manifest.BoundedContext;
            }
        }

        return policyName.Length > 0;
    }
}
