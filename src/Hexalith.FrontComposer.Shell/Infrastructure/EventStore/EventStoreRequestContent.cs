using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class EventStoreRequestContent {
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore adapter serializes adopter command/query DTOs at runtime by design; AOT-specific contexts are deferred to Story 9-4.")]
    public static ByteArrayContent Create(object request, int maxBytes) {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
        if (body.Length > maxBytes) {
            throw new InvalidOperationException("Serialized EventStore request body exceeds the configured byte limit.");
        }

        ByteArrayContent content = new(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
