using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Validates at host startup that every generated command policy is present in the
/// <see cref="FrontComposerAuthorizationOptions.KnownPolicies"/> catalog. Distinguishes
/// "no catalog configured" (Information when manifests still declare policies) from
/// "missing catalog entry" (Warning, or fatal under <c>StrictPolicyCatalogValidation</c>).
/// </summary>
public sealed class FrontComposerAuthorizationPolicyCatalogValidator(
    IFrontComposerRegistry registry,
    IOptions<FrontComposerAuthorizationOptions> options,
    ILogger<FrontComposerAuthorizationPolicyCatalogValidator> logger) : IHostedService {
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) {
        FrontComposerAuthorizationOptions value = options.Value;

        // Snapshot once and trim defensively; case-sensitive Ordinal comparer matches the registry.
        // Coalesce a null KnownPolicies (legitimate when "KnownPolicies": null in appsettings binds null)
        // to an empty enumerable so validator startup does not NRE.
        IEnumerable<string> knownPolicies = value.KnownPolicies ?? Enumerable.Empty<string>();
        var catalog = new HashSet<string>(
            knownPolicies
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Select(static p => p.Trim()),
            StringComparer.Ordinal);

        cancellationToken.ThrowIfCancellationRequested();

        // Snapshot manifests once: a custom IFrontComposerRegistry implementation that refreshes
        // manifests between calls would otherwise produce a declared-vs-missing race.
        DomainManifest[] manifests = [.. registry.GetManifests()];

        // Collect the set of policies any manifest declares so we can distinguish
        // "no catalog vs no protected commands" from "no catalog but commands declare policies".
        var declaredPolicies = new HashSet<string>(StringComparer.Ordinal);
        foreach (DomainManifest manifest in manifests) {
            foreach (KeyValuePair<string, string> policy in manifest.CommandPolicies) {
                if (string.IsNullOrWhiteSpace(policy.Value)) {
                    continue;
                }

                _ = declaredPolicies.Add(policy.Value.Trim());
            }
        }

        if (catalog.Count == 0) {
            // Promote to Warning when commands declare policies but no catalog is configured:
            // forgetting to populate KnownPolicies is a security-relevant configuration gap that
            // many production logging configs filter at Information level.
            if (declaredPolicies.Count > 0) {
                logger.LogWarning(
                    "FrontComposer command authorization policy catalog is empty but {DeclaredPolicyCount} command(s) declare policies. Configure FrontComposerAuthorizationOptions.KnownPolicies so missing-policy diagnostics surface at startup.",
                    declaredPolicies.Count);
            }

            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Deduplicate missing entries by policy NAME only (no command-FQN echo) to avoid leaking
        // command identifiers into orchestration logs. Adopters who embed PII in policy names are
        // already in violation of the documented contract; the surface area for the leak is bounded.
        var missing = new HashSet<string>(StringComparer.Ordinal);
        foreach (DomainManifest manifest in manifests) {
            foreach (KeyValuePair<string, string> policy in manifest.CommandPolicies) {
                if (string.IsNullOrWhiteSpace(policy.Value)) {
                    continue;
                }

                string trimmed = policy.Value.Trim();
                if (!catalog.Contains(trimmed)) {
                    _ = missing.Add(trimmed);
                }
            }
        }

        if (missing.Count == 0) {
            return Task.CompletedTask;
        }

        string payload = string.Join(", ", missing.OrderBy(static x => x, StringComparer.Ordinal));
        if (value.StrictPolicyCatalogValidation) {
            throw new InvalidOperationException(
                "FrontComposer command authorization policy catalog is missing entries for generated command policies: " + payload);
        }

        logger.LogWarning(
            "FrontComposer command authorization policy catalog is missing entries for generated command policies: {MissingPolicies}",
            payload);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
