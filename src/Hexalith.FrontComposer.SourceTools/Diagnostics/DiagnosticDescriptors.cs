
using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Diagnostics;
/// <summary>
/// Diagnostic descriptors for the FrontComposer source generator (HFC1000-1999).
/// </summary>
public static class DiagnosticDescriptors {
    /// <summary>
    /// HFC1001: No [Command] or [Projection] types found in compilation.
    /// </summary>
    public static readonly DiagnosticDescriptor NoAnnotatedTypesFound = new(
        id: "HFC1001",
        title: "No [Command] or [Projection] types found",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1002: Unsupported field type in [Projection].
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedFieldType = new(
        id: "HFC1002",
        title: "Unsupported field type",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1003: Projection type should be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionShouldBePartial = new(
        id: "HFC1003",
        title: "Projection type should be partial",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1004: Projection on unsupported type kind.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedTypeKind = new(
        id: "HFC1004",
        title: "Projection on unsupported type kind",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1005: Invalid attribute argument.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAttributeArgument = new(
        id: "HFC1005",
        title: "Invalid attribute argument",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // HFC1010 reserved — "Full restart required for this change type" (not yet implemented; requires analyzer, not generator — see docs/hot-reload-guide.md)
}
