using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Provides the canonical, immutable JSON serialization profiles used by the Shell.
/// </summary>
internal static class FcJson {
    /// <summary>
    /// Gets the read-only System.Text.Json web-default profile used for EventStore payloads.
    /// </summary>
    internal static JsonSerializerOptions PlainWeb { get; } = GetPlainWeb();

    /// <summary>
    /// Gets the read-only web-default profile that omits default values for compact browser storage.
    /// </summary>
    internal static JsonSerializerOptions StorageCompact { get; } = CreateStorageCompact();

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "The canonical web profile preserves the Shell's existing runtime DTO serialization contract; reflective call sites carry their own trimming authorization.")]
    private static JsonSerializerOptions GetPlainWeb() => JsonSerializerOptions.Web;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "Browser storage persists the Shell state records authorized by IStorageService's generic runtime contract.")]
    private static JsonSerializerOptions CreateStorageCompact() {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web) {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
