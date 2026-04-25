using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T4.2 / D7 / ADR-053 — minimal empty placeholder emitted by every generated
/// projection view when <c>state.Items</c> is empty. Shows a muted 32px icon (generous
/// size: reads as "intentional calm", not "something failed to load") plus a
/// <c>"No {entities} yet."</c> message. Story 4.6 REPLACES the component body (CTA button,
/// secondary text) WITHOUT changing the parameter list OR the generator call-site.
/// </summary>
/// <remarks>
/// <para>Parameter surface is frozen + append-only (Story 4-1 ADR-053). Story 4.6 MAY add
/// parameters (e.g., SecondaryText, CtaCommand) but MUST NOT remove, rename, or reorder
/// <see cref="ProjectionType"/>, <see cref="EntityPluralOverride"/>, or <see cref="Role"/>.</para>
/// <para><see cref="Role"/> is emitted by the generator from 4-1 forward (H4) even though
/// this component's 4-1 body ignores it — Story 4.6 consumes it for role-differentiated CTA
/// copy without forcing generator re-emission.</para>
/// </remarks>
public partial class FcProjectionEmptyPlaceholder {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private IEmptyStateCtaResolver CtaResolver { get; set; } = default!;

    /// <summary>Gets or sets the projection type this placeholder represents.</summary>
    [Parameter, EditorRequired]
    public Type ProjectionType { get; set; } = default!;

    /// <summary>
    /// Gets or sets an optional override for the humanized plural label. When
    /// <see langword="null"/> the component resolves
    /// <see cref="ProjectionType"/><c>.Name</c> → humanized → pluralized (simple "s" suffix,
    /// English only; French parity deferred to Story 9-5 ICU — G2).
    /// </summary>
    [Parameter]
    public string? EntityPluralOverride { get; set; }

    /// <summary>
    /// Gets or sets the projection role emitted by the generator (H4). 4-1 ignores this
    /// value; Story 4.6 will consume it to pick the right role-differentiated CTA copy.
    /// </summary>
    [Parameter]
    public ProjectionRole? Role { get; set; }

    /// <summary>Gets or sets the optional command name used to render an empty-state CTA.</summary>
    [Parameter]
    public string? CtaCommandName { get; set; }

    /// <summary>Gets or sets optional secondary empty-state guidance text.</summary>
    [Parameter]
    public string? SecondaryText { get; set; }

    private string EntityPlural => EntityPluralOverride ?? PluralizeHumanized(ProjectionType?.Name ?? "items");

    private EmptyStateCta? ResolvedCta => ProjectionType is null
        ? null
        : CtaResolver.Resolve(ProjectionType, CtaCommandName);

    private string DisplayMessage => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        Localizer[Role == ProjectionRole.ActionQueue ? "EmptyStateActionQueueMessageTemplate" : "EmptyStateMessageTemplate"].Value,
        EntityPlural);

    private string ResolvedAriaLabel => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        Localizer["HomeEmptyPlaceholderAriaLabel"].Value,
        EntityPlural);

    private string CtaAriaLabel(EmptyStateCta cta)
        => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Localizer["EmptyStateCtaAriaLabelTemplate"].Value,
            EntityPlural,
            cta.CommandDisplayName);

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
