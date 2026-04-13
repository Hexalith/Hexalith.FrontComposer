#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

/// <summary>
/// Diagnostic descriptors for the FrontComposer source generator (HFC1000-1999).
/// </summary>
public static class DiagnosticDescriptors
{
    // HFC1001 reserved for "No [Command] or [Projection] types found" (Story 1.5)

    /// <summary>
    /// HFC1002: Unsupported field type in [Projection].
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedFieldType = new DiagnosticDescriptor(
        id: "HFC1002",
        title: "Unsupported field type",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1003: Projection type should be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionShouldBePartial = new DiagnosticDescriptor(
        id: "HFC1003",
        title: "Projection type should be partial",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1004: Projection on unsupported type kind.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedTypeKind = new DiagnosticDescriptor(
        id: "HFC1004",
        title: "Projection on unsupported type kind",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1005: Invalid attribute argument.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAttributeArgument = new DiagnosticDescriptor(
        id: "HFC1005",
        title: "Invalid attribute argument",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
