using Microsoft.FluentUI.AspNetCore.Components;

using FluentIcons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Hexalith.FrontComposer.Shell.Components.Icons;

/// <summary>
/// FrontComposer-owned icon factory built on Fluent UI v5's typed icon catalog.
/// </summary>
public static class FcFluentIcons {
    /// <summary>Creates the default bounded-context rail icon.</summary>
    public static Icon Apps20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.Apps()
            : new FluentIcons.Regular.Size20.Apps();

    /// <summary>Creates a row-expansion chevron icon.</summary>
    public static Icon ChevronRight20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.ChevronRight()
            : new FluentIcons.Regular.Size20.ChevronRight();

    /// <summary>Creates the empty-projection placeholder icon (32px).</summary>
    public static Icon DocumentSearch32(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size32.DocumentSearch()
            : new FluentIcons.Regular.Size32.DocumentSearch();

    /// <summary>
    /// Creates the empty-projection placeholder icon through the largest official
    /// Fluent UI v5 <c>DocumentSearch</c> size available in the referenced package.
    /// </summary>
    public static Icon DocumentSearch48(IconVariant variant = IconVariant.Regular)
        => DocumentSearch32(variant);

    /// <summary>Creates the development overlay icon.</summary>
    public static Icon DevMode20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.DeveloperBoard()
            : new FluentIcons.Regular.Size20.DeveloperBoard();

    /// <summary>Creates the default command action icon.</summary>
    public static Icon Play16(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size16.Play()
            : new FluentIcons.Regular.Size16.Play();

    /// <summary>Creates the command-palette search icon.</summary>
    public static Icon Search20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.Search()
            : new FluentIcons.Regular.Size20.Search();

    /// <summary>Creates the settings icon.</summary>
    public static Icon Settings20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.Settings()
            : new FluentIcons.Regular.Size20.Settings();

    /// <summary>Creates the navigation (hamburger) icon used by the desktop sidebar toggle.</summary>
    public static Icon Navigation20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.Navigation()
            : new FluentIcons.Regular.Size20.Navigation();

    /// <summary>Creates a building with people bounded-context rail icon.</summary>
    public static Icon BuildingPeople20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.BuildingPeople()
            : new FluentIcons.Regular.Size20.BuildingPeople();

    /// <summary>Creates a people / group bounded-context rail icon.</summary>
    public static Icon People20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.People()
            : new FluentIcons.Regular.Size20.People();

    /// <summary>Creates a person board bounded-context rail icon.</summary>
    public static Icon PersonBoard20(IconVariant variant = IconVariant.Regular)
        => variant == IconVariant.Filled
            ? new FluentIcons.Filled.Size20.PersonBoard()
            : new FluentIcons.Regular.Size20.PersonBoard();

    /// <summary>Creates a 16px success / present checkmark glyph (domain status &amp; freshness badges).</summary>
    public static Icon Checkmark16() => new FluentIcons.Regular.Size16.Checkmark();

    /// <summary>Creates a 16px success status glyph.</summary>
    public static Icon CheckmarkCircle16() => new FluentIcons.Regular.Size16.CheckmarkCircle();

    /// <summary>Creates a 16px rejected / error status glyph.</summary>
    public static Icon DismissCircle16() => new FluentIcons.Regular.Size16.DismissCircle();

    /// <summary>Creates a 16px informational status glyph.</summary>
    public static Icon InfoCircle16() => new FluentIcons.Regular.Size16.Info();

    /// <summary>Creates a 16px disabled / blocked (circle-minus) glyph.</summary>
    public static Icon SubtractCircle16() => new FluentIcons.Regular.Size16.SubtractCircle();

    /// <summary>Creates a 16px unknown (circle-question) glyph.</summary>
    public static Icon QuestionCircle16() => new FluentIcons.Regular.Size16.QuestionCircle();

    /// <summary>Creates a 16px warning (triangle) glyph for aging / stale freshness.</summary>
    public static Icon Warning16() => new FluentIcons.Regular.Size16.Warning();

    /// <summary>Creates a 16px refresh (two-arrow sync) glyph.</summary>
    public static Icon ArrowSync16() => new FluentIcons.Regular.Size16.ArrowSync();

    /// <summary>Creates a 16px star glyph (tenant owner role).</summary>
    public static Icon Star16() => new FluentIcons.Regular.Size16.Star();

    /// <summary>Creates a 16px edit / pencil glyph (tenant contributor role).</summary>
    public static Icon Edit16() => new FluentIcons.Regular.Size16.Edit();

    /// <summary>Creates a 16px eye glyph (tenant reader role).</summary>
    public static Icon Eye16() => new FluentIcons.Regular.Size16.Eye();

    /// <summary>Creates a 16px key glyph (access audit category).</summary>
    public static Icon Key16() => new FluentIcons.Regular.Size16.Key();

    /// <summary>Creates a 16px copy (two-sheet) glyph for support-safe copy actions.</summary>
    public static Icon Copy16() => new FluentIcons.Regular.Size16.Copy();

    /// <summary>
    /// Attempts to create a supported FrontComposer icon from the existing
    /// <c>Variant.SizeNN.Name</c> contract.
    /// </summary>
    public static bool TryCreate(string? iconName, out Icon? icon)
        => TryCreate(iconName, IconVariant.Regular, out icon);

    /// <summary>
    /// Attempts to create a supported FrontComposer icon with the requested visual variant.
    /// </summary>
    public static bool TryCreate(string? iconName, IconVariant variant, out Icon? icon) {
        icon = iconName switch {
            "Regular.Size16.Play" => Play16(variant),
            "Regular.Size20.Apps" => Apps20(variant),
            "Regular.Size20.ChevronRight" => ChevronRight20(variant),
            "Regular.Size20.DevMode" => DevMode20(variant),
            "Regular.Size20.Search" => Search20(variant),
            "Regular.Size20.Settings" => Settings20(variant),
            "Regular.Size20.Navigation" => Navigation20(variant),
            "Regular.Size20.BuildingPeople" => BuildingPeople20(variant),
            "Regular.Size20.People" => People20(variant),
            "Regular.Size20.PersonBoard" => PersonBoard20(variant),
            "Regular.Size32.DocumentSearch" => DocumentSearch32(variant),
            "Regular.Size48.DocumentSearch" => DocumentSearch48(variant),
            _ => null,
        };

        return icon is not null;
    }
}
