using System.Collections.Frozen;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Icons;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>
/// Exhaustive mapping from semantic <see cref="BadgeSlot"/> values to generated status icon glyphs and colors.
/// </summary>
internal static class StatusIconTable {
#pragma warning disable CS0618 // Story 8.7 explicitly requires Color.Neutral and Color.Accent on the pinned Fluent v5 API.
    /// <summary>Gets the frozen lookup of slot → icon factory and color.</summary>
    public static FrozenDictionary<BadgeSlot, (Func<Icon> IconFactory, Color Color)> Mapping { get; } =
        new KeyValuePair<BadgeSlot, (Func<Icon>, Color)>[] {
            new(BadgeSlot.Neutral, (FcFluentIcons.QuestionCircle16, Color.Neutral)),
            new(BadgeSlot.Info, (FcFluentIcons.InfoCircle16, Color.Info)),
            new(BadgeSlot.Success, (FcFluentIcons.CheckmarkCircle16, Color.Success)),
            new(BadgeSlot.Warning, (FcFluentIcons.Warning16, Color.Warning)),
            new(BadgeSlot.Danger, (FcFluentIcons.DismissCircle16, Color.Error)),
            new(BadgeSlot.Accent, (FcFluentIcons.Star16, Color.Accent)),
        }.ToFrozenDictionary();
#pragma warning restore CS0618

    /// <summary>Resolves a slot to a fresh icon instance and Fluent icon color.</summary>
    public static (Icon Icon, Color Color) Resolve(BadgeSlot slot) {
        (Func<Icon> iconFactory, Color color) = Mapping[slot];
        return (iconFactory(), color);
    }
}
