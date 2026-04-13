namespace Hexalith.FrontComposer.Contracts.Telemetry;

/// <summary>
/// Shared telemetry source name and version constants for the FrontComposer framework.
/// The actual ActivitySource instance is created in Shell (net10.0).
/// </summary>
public static class FrontComposerActivitySource
{
    /// <summary>
    /// The telemetry source name.
    /// </summary>
    public const string Name = "Hexalith.FrontComposer";

    /// <summary>
    /// The telemetry source version.
    /// </summary>
    public const string Version = "0.1.0";
}
