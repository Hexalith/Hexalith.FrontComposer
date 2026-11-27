namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>Defines the published FrontComposer diagnostic documentation URL format.</summary>
public static class FcDiagnosticLinks
{
    internal const string Prefix = "https://hexalith.github.io/FrontComposer/diagnostics/";

    /// <summary>Gets the canonical URL format with the diagnostic ID at position zero.</summary>
    public const string Format = Prefix + "{0}";
}
