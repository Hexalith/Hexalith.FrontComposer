using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>Exposes actual application component health to Aspire's bounded readiness wait.</summary>
internal sealed class LocalReadinessHealthCheck(Func<string> endpoint, bool maintenance) : IHealthCheck
{
    private static readonly string[] _requiredComponents =
    [
        "self", "dapr-sidecar", "dapr-actor-placement", "dapr-statestore", "dapr-pubsub", "dapr-configstore",
        "dapr-app-channel-token", "projection-delivery-writer-protocol", "projection-delivery-unresolved-conflict",
    ];

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpClient client = new(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(8) };
            using HttpResponseMessage response = await client.GetAsync(endpoint() + "/health", cancellationToken).ConfigureAwait(false);
            using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            JsonProperty[] entries = body.RootElement.GetProperty("results").EnumerateObject().ToArray();
            string[] unhealthy = entries.Where(entry => entry.Value.GetProperty("status").GetString() != "Healthy")
                .Select(entry => entry.Name).ToArray();
            if (!_requiredComponents.All(name => entries.Any(entry => entry.Name == name)) || (maintenance
                ? body.RootElement.GetProperty("status").GetString() != "Unhealthy" || response.StatusCode != HttpStatusCode.ServiceUnavailable || unhealthy.Length != 1 || unhealthy[0] != "projection-delivery-writer-protocol"
                : response.StatusCode != HttpStatusCode.OK || unhealthy.Length != 0 || body.RootElement.GetProperty("status").GetString() != "Healthy"))
            {
                return HealthCheckResult.Unhealthy("Disposable application component health has not reached the required phase.");
            }
            if (!maintenance)
            {
                using HttpResponseMessage ready = await client.GetAsync(endpoint() + "/ready", cancellationToken).ConfigureAwait(false);
                using JsonDocument readiness = JsonDocument.Parse(await ready.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                if (ready.StatusCode != HttpStatusCode.OK || readiness.RootElement.GetProperty("status").GetString() != "Healthy")
                {
                    return HealthCheckResult.Unhealthy("Disposable application readiness is not healthy.");
                }
            }
            return HealthCheckResult.Healthy(maintenance ? "Maintenance prerequisites ready; writer marker remains inactive." : "Application health and readiness are Healthy.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return HealthCheckResult.Unhealthy("Disposable application health could not be verified.");
        }
    }
}
