using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;

/// <summary>Builds bounded, deterministic signatures for fallback projection pages.</summary>
internal static class ProjectionFallbackRowSignature {
    private const int MaxCanonicalDepth = 32;
    private const int MaxRows = 128;
    private const int MaxSerializedBytesPerRow = 16 * 1024;

    [UnconditionalSuppressMessage(
        "Aot",
        "IL3050:RequiresDynamicCode",
        Justification = "Fallback rows are runtime adopter DTOs; the bounded serializer uses the Shell's existing reflection-enabled JSON profile and retains only a digest.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "Fallback rows are runtime adopter DTOs; the bounded serializer uses the Shell's existing reflection-enabled JSON profile and retains only a digest.")]
    internal static string Create(ProjectionPageResult result, out bool isReliable) {
        ArgumentNullException.ThrowIfNull(result);
        isReliable = true;

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendMarker(hash, 0x01);
        AppendInt32(hash, result.TotalCount);

        IReadOnlyList<object>? items = result.Items;
        int itemCount = items?.Count ?? 0;
        AppendInt32(hash, itemCount);

        byte[] serializationBuffer = GC.AllocateUninitializedArray<byte>(MaxSerializedBytesPerRow);
        int rowsToHash = Math.Min(itemCount, MaxRows);
        for (int index = 0; index < rowsToHash; index++) {
            AppendMarker(hash, 0x02);
            AppendInt32(hash, index);

            using MemoryStream stream = new(
                serializationBuffer,
                index: 0,
                count: serializationBuffer.Length,
                writable: true,
                publiclyVisible: true);
            stream.SetLength(0);

            try {
                object? row = items![index];
                JsonSerializer.Serialize(
                    stream,
                    row,
                    row?.GetType() ?? typeof(object),
                    FcJson.PlainWeb);

                using JsonDocument document = JsonDocument.Parse(
                    serializationBuffer.AsMemory(0, checked((int)stream.Position)),
                    new JsonDocumentOptions { MaxDepth = MaxCanonicalDepth });
                AppendElement(hash, document.RootElement, depth: 0, ref isReliable);
            }
            catch (JsonException) {
                isReliable = false;
                AppendMarker(hash, 0x03);
            }
            catch (NotSupportedException) {
                isReliable = false;
                AppendMarker(hash, 0x04);
            }
            catch (IOException) {
                isReliable = false;
                AppendMarker(hash, 0x04);
            }
            catch (Exception exception) when (!ExceptionGuard.IsFatal(exception)) {
                isReliable = false;
                AppendMarker(hash, 0x07);
            }
        }

        if (itemCount > rowsToHash) {
            isReliable = false;
            AppendMarker(hash, 0x05);
            AppendInt32(hash, itemCount - rowsToHash);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendElement(
        IncrementalHash hash,
        JsonElement element,
        int depth,
        ref bool isReliable) {
        if (depth >= MaxCanonicalDepth) {
            isReliable = false;
            AppendMarker(hash, 0x06);
            return;
        }

        switch (element.ValueKind) {
            case JsonValueKind.Object:
                AppendMarker(hash, 0x10);
                JsonProperty[] properties = element
                    .EnumerateObject()
                    .OrderBy(static property => property.Name, StringComparer.Ordinal)
                    .ToArray();
                AppendInt32(hash, properties.Length);
                foreach (JsonProperty property in properties) {
                    AppendUtf8(hash, property.Name);
                    AppendElement(hash, property.Value, depth + 1, ref isReliable);
                }

                break;
            case JsonValueKind.Array:
                AppendMarker(hash, 0x11);
                AppendInt32(hash, element.GetArrayLength());
                foreach (JsonElement item in element.EnumerateArray()) {
                    AppendElement(hash, item, depth + 1, ref isReliable);
                }

                break;
            case JsonValueKind.String:
                AppendMarker(hash, 0x12);
                AppendUtf8(hash, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                AppendMarker(hash, 0x13);
                AppendUtf8(hash, element.GetRawText());
                break;
            case JsonValueKind.True:
                AppendMarker(hash, 0x14);
                break;
            case JsonValueKind.False:
                AppendMarker(hash, 0x15);
                break;
            case JsonValueKind.Null:
                AppendMarker(hash, 0x16);
                break;
            default:
                AppendMarker(hash, 0x17);
                break;
        }
    }

    private static void AppendInt32(IncrementalHash hash, int value) {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static void AppendMarker(IncrementalHash hash, byte marker) {
        Span<byte> bytes = stackalloc byte[1];
        bytes[0] = marker;
        hash.AppendData(bytes);
    }

    private static void AppendUtf8(IncrementalHash hash, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        AppendInt32(hash, bytes.Length);
        hash.AppendData(bytes);
    }
}
