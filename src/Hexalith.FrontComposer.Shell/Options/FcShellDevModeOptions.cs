using System.ComponentModel.DataAnnotations;

namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Development-only overlay limits for bounded starter-template generation.
/// </summary>
public sealed class FcShellDevModeOptions {
    /// <summary>Maximum component-tree depth walked while emitting starter metadata.</summary>
    [Range(8, 512, ErrorMessage = "DevMode.MaxNodeDepth must be between 8 and 512.")]
    public int MaxNodeDepth { get; set; } = 64;

    /// <summary>Maximum children emitted for a single node while walking starter metadata.</summary>
    [Range(8, 4_096, ErrorMessage = "DevMode.MaxFanOut must be between 8 and 4096.")]
    public int MaxFanOut { get; set; } = 512;

    /// <summary>Maximum time a clipboard copy operation may take before timing out.</summary>
    [Range(100, 30_000, ErrorMessage = "DevMode.CopyTimeoutMilliseconds must be between 100 and 30000.")]
    public int CopyTimeoutMilliseconds { get; set; } = 2_000;

    /// <summary>Maximum size (bytes) of a clipboard payload accepted by the dev-mode overlay.</summary>
    [Range(1_024, 1_048_576, ErrorMessage = "DevMode.MaxClipboardPayloadBytes must be between 1024 and 1048576.")]
    public int MaxClipboardPayloadBytes { get; set; } = 65_536;
}
