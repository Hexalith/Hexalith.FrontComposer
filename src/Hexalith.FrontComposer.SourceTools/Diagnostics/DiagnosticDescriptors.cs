
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

    /// <summary>
    /// HFC1010: A customization metadata or descriptor edit requires a full rebuild/restart.
    /// </summary>
    public static readonly DiagnosticDescriptor FullRebuildRequired = new(
        id: "HFC1010",
        title: "Full rebuild or restart required for customization metadata change",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

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

    /// <summary>
    /// HFC1028: Two or more properties on a projection declare the same explicit
    /// <c>[ColumnPriority]</c> value (Story 4-4 D14 / D15). Information severity — the build
    /// succeeds; the deterministic fallback is declaration order within the tied priority.
    /// Fire once per colliding priority value per projection type (per-projection dedupe).
    /// </summary>
    public static readonly DiagnosticDescriptor ColumnPriorityCollision = new(
        id: "HFC1028",
        title: "[ColumnPriority] collision on projection",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1029: Projection exceeds the 15-column auto-generation limit (UX-DR63 / Story 4-4
    /// D15). Information severity — <c>FcColumnPrioritizer</c> wraps the grid at runtime
    /// showing the first 10 columns by priority; the remainder hide behind the "More columns"
    /// gear affordance. Fire once per projection type (per-projection dedupe). Annotate columns
    /// with <c>[ColumnPriority]</c> to control which 10 stay visible by default.
    /// </summary>
    public static readonly DiagnosticDescriptor ColumnPrioritizerActivated = new(
        id: "HFC1029",
        title: "Projection exceeds 15 columns — FcColumnPrioritizer activates",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1030: A <c>[ProjectionFieldGroup]</c> annotation declares a group name that
    /// case-insensitively collides with the reserved catch-all label "Additional details"
    /// (Story 4-5 D9 / D16). Information severity — fail-soft pass-through; the colliding
    /// group renders in the detail accordion alongside the catch-all bucket (visually
    /// unusual but not broken). Fire once per projection type with all colliding group
    /// names listed (per-projection dedupe). Fix: rename the group.
    /// </summary>
    public static readonly DiagnosticDescriptor FieldGroupNameCollidesWithCatchAll = new(
        id: "HFC1030",
        title: "[ProjectionFieldGroup] name collides with reserved catch-all label",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1031: A projection annotated with <c>[ProjectionRole(ProjectionRole.Timeline)]</c>
    /// also carries one or more <c>[ProjectionFieldGroup]</c> annotations (Story 4-5 D17).
    /// Information severity — Timeline renders a <c>FluentStack</c> chronological surface
    /// and has no detail body, so the grouping is silently unused. Fire once per projection
    /// type (per-projection dedupe). Fix: remove the annotations or change the projection
    /// role to Default / DetailRecord / ActionQueue / StatusOverview / Dashboard.
    /// </summary>
    public static readonly DiagnosticDescriptor FieldGroupIgnoredForNonDetailRole = new(
        id: "HFC1031",
        title: "[ProjectionFieldGroup] is ignored for the projection role",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1032: A Level 1 format annotation is incompatible with the field type or
    /// conflicts with another mutually-exclusive format annotation. Warning severity —
    /// generated code remains fail-soft and falls back to the existing default formatter.
    /// </summary>
    public static readonly DiagnosticDescriptor Level1FormatAnnotationInvalid = new(
        id: "HFC1032",
        title: "Invalid Level 1 format annotation",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1033: <c>[ProjectionTemplate]</c> marker references a projection type that is
    /// missing, unresolvable, generic, abstract, a struct, or is not annotated with
    /// <c>[Projection]</c> (Story 6-2 T3 / AC6). Error severity — the marker is excluded
    /// from the generated manifest so runtime selection cannot pick it.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionTemplateInvalidProjectionType = new(
        id: "HFC1033",
        title: "[ProjectionTemplate] references an invalid projection type",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1034: <c>[ProjectionTemplate]</c>-marked class is not a valid Blazor component or
    /// does not declare a public <c>[Parameter]</c> <c>Context</c> property of type
    /// <c>ProjectionTemplateContext&lt;TProjection&gt;</c> matching the marker's projection type
    /// (Story 6-2 T3 / AC1 / AC6). Warning severity — the invalid component is excluded from
    /// the generated manifest.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionTemplateContextParameterMissing = new(
        id: "HFC1034",
        title: "[ProjectionTemplate] component is not a valid typed template component",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1035: <c>[ProjectionTemplate]</c> marker's <c>ExpectedContractVersion</c> declares
    /// a major version different from the installed Level 2 contract (Story 6-2 T7 / AC5).
    /// Warning severity — selection is suppressed so the template never runs against an
    /// incompatible context shape.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionTemplateContractVersionMismatch = new(
        id: "HFC1035",
        title: "[ProjectionTemplate] contract version is incompatible",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1036: <c>[ProjectionTemplate]</c> marker's <c>ExpectedContractVersion</c> drifts
    /// in the minor digit from the installed Level 2 contract (Story 6-2 T7 / D6 / AC5).
    /// Warning severity — selection proceeds; build-only drift does not warn.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionTemplateContractVersionDrift = new(
        id: "HFC1036",
        title: "[ProjectionTemplate] contract version is out of date",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1037: Two or more <c>[ProjectionTemplate]</c> markers in the same compilation
    /// target the same projection-and-role tuple (Story 6-2 T3 / D10 / AC11 / AC12).
    /// Error severity — duplicates are excluded from the generated manifest so runtime
    /// selection cannot pick a non-deterministic winner. Fix: keep one template, or
    /// disambiguate via the <c>Role</c> named argument.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionTemplateDuplicate = new(
        id: "HFC1037",
        title: "Duplicate [ProjectionTemplate] for the same projection and role",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1038: A Level 3 slot selector is not a direct projection property access.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionSlotSelectorInvalid = new(
        id: "HFC1038",
        title: "Invalid Level 3 slot selector",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1039: A Level 3 slot component does not expose the required typed Context parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionSlotComponentInvalid = new(
        id: "HFC1039",
        title: "Invalid Level 3 slot component",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1040: Two or more Level 3 slot descriptors target the same projection, role, and field.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionSlotDuplicate = new(
        id: "HFC1040",
        title: "Duplicate Level 3 slot override",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1041: A Level 3 slot descriptor declares an incompatible contract version.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionSlotContractVersionMismatch = new(
        id: "HFC1041",
        title: "Level 3 slot contract version is incompatible",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1042: Reserved for Level 4 view override invalid projection type diagnostics.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionViewOverrideInvalidProjectionType = new(
        id: "HFC1042",
        title: "Invalid Level 4 view override projection type",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1043: A Level 4 view replacement component does not expose the required typed Context parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionViewOverrideComponentInvalid = new(
        id: "HFC1043",
        title: "Invalid Level 4 view override component",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1044: Two or more Level 4 view replacements target the same projection and role with
    /// different component types (Story 6-4 D6 / AC7). Error severity — registry construction
    /// fails hard at startup so duplicate registrations are surfaced immediately rather than
    /// silently fading into generated rendering. Idempotent re-registration of the same
    /// component type is treated as a no-op.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionViewOverrideDuplicate = new(
        id: "HFC1044",
        title: "Duplicate Level 4 view override",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1045: A Level 4 view replacement descriptor declares an incompatible contract version.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionViewOverrideContractVersionMismatch = new(
        id: "HFC1045",
        title: "Level 4 view override contract version is incompatible",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1046: Reserved for Level 4 custom-component accessibility warnings.
    /// </summary>
    public static readonly DiagnosticDescriptor ProjectionViewOverrideAccessibilityWarning = new(
        id: "HFC1046",
        title: "Level 4 view override accessibility contract warning",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1050: A statically inspectable custom override contains an interactive root without an accessible name.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationAccessibleNameMissing = new(
        id: "HFC1050",
        title: "Custom override interactive element is missing an accessible name",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1051: A statically inspectable custom override has an obvious keyboard reachability issue.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationKeyboardReachabilityIssue = new(
        id: "HFC1051",
        title: "Custom override has a keyboard reachability issue",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1052: A statically inspectable custom override suppresses focus visibility.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationFocusVisibilitySuppressed = new(
        id: "HFC1052",
        title: "Custom override suppresses focus visibility",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1053: A lifecycle/status custom override omits aria-live parity.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationAriaLiveParityMissing = new(
        id: "HFC1053",
        title: "Custom override is missing aria-live parity",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1054: A custom override uses motion without a reduced-motion fallback.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationReducedMotionMissing = new(
        id: "HFC1054",
        title: "Custom override motion has no reduced-motion fallback",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1055: A custom override uses custom colors without forced-colors or static contrast evidence.
    /// </summary>
    public static readonly DiagnosticDescriptor CustomizationForcedColorsMissing = new(
        id: "HFC1055",
        title: "Custom override color styling has no forced-colors fallback",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1056: A command declares an invalid <c>[RequiresPolicy]</c> value.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandAuthorizationPolicyInvalid = new(
        id: "HFC1056",
        title: "Command authorization policy is invalid",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1057: A command declares duplicate <c>[RequiresPolicy]</c> attributes.
    /// </summary>
    public static readonly DiagnosticDescriptor CommandAuthorizationPolicyDuplicate = new(
        id: "HFC1057",
        title: "Command declares duplicate authorization policies",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1047: Dev-mode annotation site lacks stable descriptor metadata.
    /// </summary>
    public static readonly DiagnosticDescriptor DevModeAnnotationSiteInvalid = new(
        id: "HFC1047",
        title: "Invalid dev-mode annotation site",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1048: Starter emission requested an unsupported customization level.
    /// </summary>
    public static readonly DiagnosticDescriptor DevModeUnsupportedEmissionLevel = new(
        id: "HFC1048",
        title: "Unsupported dev-mode starter emission level",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// HFC1049: Dev-mode starter output detected stale contract metadata.
    /// </summary>
    public static readonly DiagnosticDescriptor DevModeContractVersionDrift = new(
        id: "HFC1049",
        title: "Dev-mode starter contract metadata is stale",
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
