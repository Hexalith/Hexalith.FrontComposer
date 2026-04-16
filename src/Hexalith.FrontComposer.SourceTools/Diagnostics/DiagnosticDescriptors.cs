
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

    /// <summary>
    /// HFC1011: [Command] type exceeds the hard 200-property limit (DoS mitigation for Story 2-2).
    /// Total public-property count (derivable + non-derivable) greater than 200 is rejected.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandTooManyTotalProperties = new(
        id: "HFC1011",
        title: "Command exceeds 200-property hard limit",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1012: <c>[DefaultValue(x)]</c> argument type is not assignable to the decorated property's type.
    /// </summary>
    public static readonly DiagnosticDescriptor DefaultValueTypeMismatch = new(
        id: "HFC1012",
        title: "[DefaultValue] value type does not match property type",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // HFC1013 reserved — previously proposed "BaseName collision" diagnostic was cut (Story 2-2 R2 Trim) after Decision D22 reverted to full {CommandTypeName} naming.

    /// <summary>
    /// HFC1014: Nested <c>[Command]</c> types are unsupported. Command types must be top-level within a namespace.
    /// </summary>
    public static readonly DiagnosticDescriptor NestedCommandUnsupported = new(
        id: "HFC1014",
        title: "Nested [Command] type is unsupported",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1015: <c>RenderMode</c> parameter is incompatible with the command density
    /// (e.g. <c>CommandRenderMode.Inline</c> on a 5-field command). Runtime warning log
    /// in Story 2-2; analyzer emission deferred to Epic 9. (Renumbered from originally-proposed
    /// HFC1008 to avoid collision with <see cref="CommandFlagsEnumProperty"/>.)
    /// </summary>
    public static readonly DiagnosticDescriptor RenderModeIncompatibleWithDensity = new(
        id: "HFC1015",
        title: "RenderMode incompatible with command density",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1016: A non-derivable property on a <c>[Command]</c> type is read-only or init-only.
    /// The generated form binds input controls via <c>_model.Property = value</c>, which requires
    /// a writable instance setter. Records with positional parameters (which become init-only)
    /// or hand-authored <c>{ get; init; }</c> / <c>{ get; }</c> properties fail to compile.
    /// Change to <c>{ get; set; }</c> or mark the property with <c>[DerivedFrom]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandPropertyNotWritable = new(
        id: "HFC1016",
        title: "Command non-derivable property is read-only or init-only",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1017: Generic <c>[Command]</c> type is unsupported (Story 2-3 Hindsight H9).
    /// The per-command lifecycle bridge emitter generates one hint per command type; a generic
    /// command would collide on hint-name across specialisations and cannot be uniformly
    /// subscribed via <c>IActionSubscriber.SubscribeToAction&lt;T&gt;</c>. Specialise the type or
    /// remove the type parameters.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandTypeIsGeneric = new(
        id: "HFC1017",
        title: "Command type is generic",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
