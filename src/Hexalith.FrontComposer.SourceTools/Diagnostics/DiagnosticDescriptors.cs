
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
    /// HFC1008: A <c>[Flags]</c> enum cannot be expressed by a single-value UI surface. Two call sites:
    /// (1) on a <see cref="Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute"/>-annotated
    /// type the field renders as <c>FcFieldPlaceholder</c> so adopters supply a multi-select
    /// renderer via the customization gradient;
    /// (2) on a <see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute"/>-annotated
    /// type any member-level <c>[ProjectionBadge]</c> annotations are ignored (Story 4-2 RF5 / D10)
    /// because a bitmask value can set multiple members simultaneously and the 6-slot palette
    /// cannot render compound state. The column falls through to the Story 1-5 plain-text path;
    /// adopters needing compound rendering take the Epic 6 Slot-level customization path.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandFlagsEnumProperty = new(
        id: "HFC1008",
        title: "[Flags] enum in a single-value UI context",
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

    /// <summary>
    /// HFC1020: Command whose name matches a destructive verb pattern
    /// (<c>Delete*</c> / <c>Remove*</c> / <c>Purge*</c> / <c>Erase*</c> / <c>Drop*</c> /
    /// <c>Truncate*</c> / <c>Wipe*</c>) is missing the <c>[Destructive]</c> attribute.
    /// Story 2-5 Decision D20 / ADR-026. Info severity in v0.1 to prevent Day-1 adoption
    /// blockers for codebases with pre-existing non-destructive <c>Remove*</c> / <c>Delete*</c>
    /// commands; promotion to Warning + suppression escape hatch ship in Story 9-4.
    /// </summary>
    public static readonly DiagnosticDescriptor DestructiveNamePatternMissingAttribute = new(
        id: "HFC1020",
        title: "Command appears destructive by name but is missing [Destructive] attribute",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1021: <c>[Destructive]</c> command has zero non-derivable properties, so it would
    /// render as a 0-field Inline button. Destructive commands must require at least one
    /// non-derivable field (UX-DR36 — danger never inline on DataGrid rows). Story 2-5
    /// Decision D1 / AC4.
    /// </summary>
    public static readonly DiagnosticDescriptor DestructiveCommandHasZeroFields = new(
        id: "HFC1021",
        title: "Destructive command must have at least one non-derivable property",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1022: <c>[ProjectionRole(..., WhenState = "...")]</c> references an enum member
    /// that does not exist on the projection's status-enum type (Story 4-1 D3 / D17 / AC9).
    /// Warning severity — member is passed through to the IR and will silently never match
    /// at runtime. Fix: correct the typo or remove the invalid member from the CSV.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionWhenStateMemberUnknown = new(
        id: "HFC1022",
        title: "ProjectionRole.WhenState references unknown enum member",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1023: <c>[ProjectionRole(ProjectionRole.Dashboard)]</c> falls back to Default
    /// DataGrid rendering in v1 (Story 4-1 D16 / D17 / AC10). Information severity — feature
    /// reserved, not broken; full Dashboard rendering deferred to Story 6-3.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionRoleDashboardFallback = new(
        id: "HFC1023",
        title: "Dashboard projection rendering is deferred",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1024: <c>[ProjectionRole]</c> attribute carries a numeric role value outside the
    /// declared <see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole"/>
    /// enum (Story 4-1 D15 / D17 / AC7). Warning severity — renderer falls back to Default
    /// rendering. Fix: correct the unsafe cast at the attribute call site.
    /// </summary>
    public static readonly DiagnosticDescriptor UnknownProjectionRoleValue = new(
        id: "HFC1024",
        title: "Unknown ProjectionRole value",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1025: An enum column on a projection carries <c>[ProjectionBadge]</c> on SOME of
    /// its members but not ALL (Story 4-2 D6 / AC3). Information severity — the build
    /// succeeds, annotated members render as semantic badges, and unannotated members
    /// fall back to humanized text. Fix: annotate every member or none for visual
    /// consistency. Per-view deduped: one diagnostic per (projection, enum) pair.
    /// </summary>
    public static readonly DiagnosticDescriptor BadgeSlotFallbackApplied = new(
        id: "HFC1025",
        title: "Projection enum has partial [ProjectionBadge] coverage",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1026: Reserved (Story 4-2 D7). Color-only badge rendered without a visible text
    /// label — unreachable from generated code because <c>FcStatusBadge.Label</c> is
    /// <c>EditorRequired</c>. Reserved now so Story 10-2's specimen checker can emit the
    /// diagnostic for adopter-authored custom badges (Epic 6 override path) without
    /// re-opening the diagnostic table. No call sites in Story 4-2.
    /// </summary>
    public static readonly DiagnosticDescriptor ColorOnlyBadgeDetected = new(
        id: "HFC1026",
        title: "Color-only badge detected (reserved)",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1027: Projection has a Collection-typed column which does not support automatic
    /// column-header filtering (Story 4-3 D14 / D20). Information severity — filter affordance
    /// is omitted on the column; adopters needing collection-aware filtering override via the
    /// Epic 6 Slot-level customization path. Per-projection deduped: one diagnostic per
    /// projection type regardless of how many Collection columns it carries.
    /// </summary>
    public static readonly DiagnosticDescriptor CollectionColumnNotFilterable = new(
        id: "HFC1027",
        title: "Collection column does not support automatic filtering",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
