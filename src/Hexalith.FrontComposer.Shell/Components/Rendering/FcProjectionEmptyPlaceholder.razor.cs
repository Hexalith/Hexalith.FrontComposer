using System.Globalization;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T4.2 / D7 / ADR-053 — minimal empty placeholder emitted by every generated
/// projection view when <c>state.Items</c> is empty. Story 4-6 ships the full anatomy: muted
/// 48px icon (UX-DR4), Body1 primary message, optional CTA <see cref="FluentAnchor"/> wrapped in
/// <c>&lt;AuthorizeView&gt;</c> (per AC2.5 — anonymous users see the empty-state without the
/// CTA), and optional Body2 secondary text resolved through the
/// <c>{ProjectionFqn}_EmptyStateSecondaryText</c> convention key (D17).
/// </summary>
/// <remarks>
/// Parameter surface is frozen + append-only (Story 4-1 ADR-053). Story 4-6 added
/// <see cref="CtaCommandName"/> and <see cref="SecondaryText"/> per the contract.
/// </remarks>
public partial class FcProjectionEmptyPlaceholder : ComponentBase {
    private EmptyStateCta? _resolvedCta;
    private string? _resolvedSecondaryText;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private IEmptyStateCtaResolver CtaResolver { get; set; } = default!;

    /// <summary>Gets or sets the projection type this placeholder represents.</summary>
    [Parameter, EditorRequired]
    public Type ProjectionType { get; set; } = default!;

    /// <summary>
    /// Gets or sets an optional override for the humanized plural label. When
    /// <see langword="null"/> the component resolves <see cref="ProjectionType"/><c>.Name</c>
    /// → humanized → pluralized (simple "s" suffix, English only; French parity deferred to
    /// Story 9-5 ICU).
    /// </summary>
    [Parameter]
    public string? EntityPluralOverride { get; set; }

    /// <summary>
    /// Gets or sets the projection role emitted by the generator (H4) — drives role-differentiated
    /// CTA copy.
    /// </summary>
    [Parameter]
    public ProjectionRole? Role { get; set; }

    /// <summary>
    /// Gets or sets an optional explicit command-name override for the CTA. When non-null bypasses
    /// <c>[ProjectionEmptyStateCta]</c> attribute discovery; when null the resolver discovers the
    /// CTA via attribute or bounded-context fallback per D5.
    /// </summary>
    [Parameter]
    public string? CtaCommandName { get; set; }

    /// <summary>
    /// Gets or sets optional secondary empty-state guidance text. When null the component falls
    /// back to the <c>{ProjectionFqn}_EmptyStateSecondaryText</c> resource convention (D17); when
    /// the convention key is absent, no secondary line renders.
    /// </summary>
    [Parameter]
    public string? SecondaryText { get; set; }

    private string EntityPlural => EntityPluralOverride ?? PluralizeHumanized(ProjectionType?.Name ?? "items");

    private string DisplayMessage {
        get {
            string templateKey = Role == ProjectionRole.ActionQueue
                ? "EmptyStateActionQueueMessageTemplate"
                : "EmptyStateMessageTemplate";
            LocalizedString template = Localizer[templateKey];
            return template.ResourceNotFound
                ? string.Format(CultureInfo.CurrentCulture, "No {0} yet.", EntityPlural)
                : string.Format(CultureInfo.CurrentCulture, template.Value, EntityPlural);
        }
    }

    private string ResolvedAriaLabel {
        get {
            LocalizedString template = Localizer["EmptyStateAriaLabelTemplate"];
            return template.ResourceNotFound
                ? string.Format(CultureInfo.CurrentCulture, "No {0} found", EntityPlural)
                : string.Format(CultureInfo.CurrentCulture, template.Value, EntityPlural);
        }
    }

    private string? ResolvedSecondaryText => _resolvedSecondaryText;

    private string FormatCtaLabel(EmptyStateCta cta) {
        LocalizedString template = Localizer["EmptyStateCtaTemplate"];
        return template.ResourceNotFound
            ? cta.CommandDisplayName
            : string.Format(CultureInfo.CurrentCulture, template.Value, cta.CommandDisplayName);
    }

    private string CtaAriaLabel(EmptyStateCta cta) => FormatCtaLabel(cta);

    /// <inheritdoc/>
    protected override void OnParametersSet() {
        // Cache CTA + secondary-text resolution once per parameter change so the razor doesn't
        // re-walk the registry on every re-render and so the rendered slot stays consistent
        // within a single render pass.
        _resolvedCta = ProjectionType is null
            ? null
            : (CtaCommandName is { Length: > 0 } explicitName
                ? CtaResolver.ResolveExplicit(ProjectionType, explicitName)
                : CtaResolver.Resolve(ProjectionType));
        _resolvedSecondaryText = ResolveSecondaryText();
    }

    private string? ResolveSecondaryText() {
        if (!string.IsNullOrWhiteSpace(SecondaryText)) {
            return SecondaryText;
        }

        if (ProjectionType is null) {
            return null;
        }

        string projectionFqn = ProjectionType.FullName ?? ProjectionType.Name;
        string conventionKey = projectionFqn + "_EmptyStateSecondaryText";
        LocalizedString secondary = Localizer[conventionKey];
        return secondary.ResourceNotFound ? null : secondary.Value;
    }

    private RenderFragment RenderCta(EmptyStateCta cta)
        => builder => {
            // FluentUI v5 collapses the v3 "FluentAnchor Appearance=Accent" pattern into
            // FluentAnchorButton + ButtonAppearance.Primary. Semantically still a navigation
            // link (Href set, no @onclick).
            builder.OpenComponent<FluentAnchorButton>(0);
            builder.AddAttribute(1, "Href", cta.CommandRoute);
            builder.AddAttribute(2, "Appearance", ButtonAppearance.Primary);
            builder.AddAttribute(3, "Class", "fc-empty-state-body__cta fc-projection-empty-placeholder-cta");
            builder.AddAttribute(4, "aria-label", CtaAriaLabel(cta));
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(childBuilder => childBuilder.AddContent(0, FormatCtaLabel(cta))));
            builder.CloseComponent();
        };

    private static string PluralizeHumanized(string typeName) {
        string humanized = HumanizeAndStripProjectionSuffix(typeName).ToLowerInvariant();
        if (humanized.EndsWith("s", StringComparison.Ordinal)) {
            return humanized;
        }

        return humanized + "s";
    }

    private static string HumanizeAndStripProjectionSuffix(string typeName) {
        const string projectionSuffix = "Projection";
        string baseName = typeName.EndsWith(projectionSuffix, StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - projectionSuffix.Length)
            : typeName;

        // Simple CamelCase → "Camel Case" humanizer (pluralization rules are trivial for v1).
        System.Text.StringBuilder sb = new(baseName.Length + 4);
        for (int i = 0; i < baseName.Length; i++) {
            char c = baseName[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(baseName[i - 1])) {
                _ = sb.Append(' ');
            }

            _ = sb.Append(c);
        }

        return sb.ToString();
    }
}
