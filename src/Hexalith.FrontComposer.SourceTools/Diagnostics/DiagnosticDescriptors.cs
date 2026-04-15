
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

    /// <summary>
    /// HFC1006: [Command] type is missing a <c>MessageId</c> property (required for correlation).
    /// </summary>
    public static readonly DiagnosticDescriptor CommandMissingMessageId = new(
        id: "HFC1006",
        title: "Command missing MessageId property",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1007: [Command] type has an excessive number of non-derivable properties (DoS mitigation).
    /// Warning at &gt; 30 properties; error at &gt; 100.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandTooManyProperties = new(
        id: "HFC1007",
        title: "Command has too many non-derivable properties",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1008: [Command] property uses a <c>[Flags]</c> enum. Single-select controls cannot express composite values;
    /// the field renders as <c>FcFieldPlaceholder</c> so adopters supply a multi-select renderer via the customization gradient.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandFlagsEnumProperty = new(
        id: "HFC1008",
        title: "Command property is a [Flags] enum (renders as placeholder)",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1009: [Command] type has no public parameterless constructor. The generated form initialises
    /// <c>_model = new()</c>, which fails to compile for positional records and for classes without a default ctor.
    /// Add a parameterless ctor, or provide defaults on every positional parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandMissingParameterlessCtor = new(
        id: "HFC1009",
        title: "Command type has no parameterless constructor",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // HFC1010 reserved — "Full restart required for this change type" (not yet implemented; requires analyzer, not generator — see docs/hot-reload-guide.md)
}
