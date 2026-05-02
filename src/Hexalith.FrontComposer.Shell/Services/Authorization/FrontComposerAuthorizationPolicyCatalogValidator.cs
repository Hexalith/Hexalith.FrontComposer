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
        var catalog = new HashSet<string>(
            value.KnownPolicies
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Select(static p => p.Trim()),
            StringComparer.Ordinal);

        // Collect the set of policies any manifest declares so we can distinguish
        // "no catalog vs no protected commands" from "no catalog but commands declare policies".
        var declaredPolicies = new HashSet<string>(StringComparer.Ordinal);
        foreach (DomainManifest manifest in registry.GetManifests()) {
            foreach (KeyValuePair<string, string> policy in manifest.CommandPolicies) {
                if (string.IsNullOrWhiteSpace(policy.Value)) {
                    continue;
                }

                _ = declaredPolicies.Add(policy.Value.Trim());
            }
        }

        if (catalog.Count == 0) {
            if (declaredPolicies.Count > 0) {
                logger.LogInformation(
                    "FrontComposer command authorization policy catalog is empty but {DeclaredPolicyCount} command(s) declare policies. Configure FrontComposerAuthorizationOptions.KnownPolicies so missing-policy diagnostics surface at startup.",
                    declaredPolicies.Count);
            }

            return Task.CompletedTask;
        }

        // Deduplicate missing entries so the same policy missing from N manifests appears once.
        var missing = new HashSet<string>(StringComparer.Ordinal);
        foreach (DomainManifest manifest in registry.GetManifests()) {
            foreach (KeyValuePair<string, string> policy in manifest.CommandPolicies) {
                if (string.IsNullOrWhiteSpace(policy.Value)) {
                    continue;
                }

                string trimmed = policy.Value.Trim();
                if (!catalog.Contains(trimmed)) {
                    _ = missing.Add(policy.Key + ":" + trimmed);
                }
            }
        }

        if (missing.Count == 0) {
            return Task.CompletedTask;
        }

        string payload = string.Join("|", missing.OrderBy(static x => x, StringComparer.Ordinal));
        if (value.StrictPolicyCatalogValidation) {
            throw new InvalidOperationException(
                "FrontComposer command authorization policy catalog is missing generated command policy entries: " + payload);
        }

        logger.LogWarning(
            "FrontComposer command authorization policy catalog is missing generated command policy entries: {MissingPolicies}",
            payload);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
