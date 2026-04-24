using System.Collections.Frozen;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>
/// Story 4-2 D2 / D3 / AC2 — frozen mapping from semantic <see cref="BadgeSlot"/> values
/// to their Fluent UI v5 <see cref="BadgeColor"/> + <see cref="BadgeAppearance"/> rendering pair.
/// </summary>
/// <remarks>
/// <para>The mapping is the single source of truth for the six-slot semantic palette:
/// changing a row requires a MAJOR version bump (UX-DR24 living-specification discipline).</para>
/// <para>Accent uses <see cref="BadgeAppearance.Filled"/> so it picks up the deployment-overridable
/// brand accent token and reads as an emphatic "Active/Running/Highlighted" chip. Every other
/// slot uses <see cref="BadgeAppearance.Tint"/> for the muted status-report appearance.</para>
/// <para>Exhaustiveness is enforced by
/// <c>SlotAppearanceTableTests.TableIsExhaustive</c>: any new member added to
/// <see cref="BadgeSlot"/> without a matching entry here fails the test at build time.</para>
/// </remarks>
internal static class SlotAppearanceTable {
    private static readonly FrozenDictionary<BadgeSlot, (BadgeColor Color, BadgeAppearance Appearance)> _mapping =
        new KeyValuePair<BadgeSlot, (BadgeColor, BadgeAppearance)>[] {
            new(BadgeSlot.Neutral, (BadgeColor.Subtle, BadgeAppearance.Tint)),
            new(BadgeSlot.Info, (BadgeColor.Informative, BadgeAppearance.Tint)),
            new(BadgeSlot.Success, (BadgeColor.Success, BadgeAppearance.Tint)),
            new(BadgeSlot.Warning, (BadgeColor.Warning, BadgeAppearance.Tint)),
            new(BadgeSlot.Danger, (BadgeColor.Danger, BadgeAppearance.Tint)),
            new(BadgeSlot.Accent, (BadgeColor.Brand, BadgeAppearance.Filled)),
        }.ToFrozenDictionary();

    /// <summary>Gets the frozen lookup of slot → (color, appearance).</summary>
    public static FrozenDictionary<BadgeSlot, (BadgeColor Color, BadgeAppearance Appearance)> Mapping => _mapping;

    /// <summary>
    /// Resolves the Fluent UI v5 visual pair for a given slot. Throws
    /// <see cref="KeyNotFoundException"/> when the caller passes a slot that is not declared
    /// on <see cref="BadgeSlot"/> (for example the result of an unsafe cast). Callers should
    /// treat that as a programmer error — the component API makes it unreachable in practice.
    /// </summary>
    public static (BadgeColor Color, BadgeAppearance Appearance) Resolve(BadgeSlot slot) => _mapping[slot];
}
