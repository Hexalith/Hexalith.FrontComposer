using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Icons;

/// <summary>
/// FrontComposer-owned icon factory built on Fluent UI v5's core <see cref="Icon"/> abstraction.
/// </summary>
public static class FcFluentIcons {
    private const string AppsPath = "<path d=\"M 4 4 h 5 v 5 H 4 V 4 Z m 7 0 h 5 v 5 h -5 V 4 Z M 4 11 h 5 v 5 H 4 v -5 Z m 7 0 h 5 v 5 h -5 v -5 Z\"/>";
    private const string ChevronRightPath = "<path d=\"M7.2 4.3a1 1 0 0 1 1.4 0l4.3 4.3a1 1 0 0 1 0 1.4l-4.3 4.3a1 1 0 1 1-1.4-1.4L10.8 9 7.2 5.4a1 1 0 0 1 0-1.1Z\"/>";
    private const string DocumentSearchPath = "<path d=\"M8 4h10l6 6v7.2a7 7 0 0 0-2-1.1V11h-5V6H8v20h8.1a7 7 0 0 0 1.1 2H8a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Zm15.5 14a5.5 5.5 0 0 1 4.3 8.9l2 2a1 1 0 0 1-1.4 1.4l-2-2A5.5 5.5 0 1 1 23.5 18Zm0 2a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7Z\"/>";
    private const string NavigationPath = "<path d=\"M3 5.25h14a.75.75 0 0 1 0 1.5H3a.75.75 0 0 1 0-1.5Zm0 4h14a.75.75 0 0 1 0 1.5H3a.75.75 0 0 1 0-1.5Zm0 4h14a.75.75 0 0 1 0 1.5H3a.75.75 0 0 1 0-1.5Z\"/>";
    private const string DevModePath = "<path d=\"M4 5a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v5.5a5.5 5.5 0 0 0-2-.4V5H6v10h4.1c.1.7.3 1.4.7 2H6a2 2 0 0 1-2-2V5Zm4 2h4v2H8V7Zm0 4h3v2H8v-2Zm7.5 1a3.5 3.5 0 0 1 3.5 3.5c0 .7-.2 1.4-.6 2l1.3 1.3a1 1 0 0 1-1.4 1.4L17 18.4a3.5 3.5 0 1 1-1.5-6.4Zm0 2a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z\"/>";
    private const string PlayPath = "<path d=\"M5 3.8c0-.8.9-1.3 1.6-.8l7 4.2a1 1 0 0 1 0 1.7l-7 4.2A1 1 0 0 1 5 12.2V3.8Z\"/>";
    private const string SearchPath = "<path d=\"M8.5 4a4.5 4.5 0 1 0 2.8 8l3.4 3.4a1 1 0 0 0 1.4-1.4l-3.4-3.4A4.5 4.5 0 0 0 8.5 4Zm0 2a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5Z\"/>";
    private const string SettingsPath = "<path d=\"M10 6a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm0 2a2 2 0 1 1 0 4 2 2 0 0 1 0-4Zm0-6a1 1 0 0 1 1 1v1.1a6.8 6.8 0 0 1 1.5.6l.8-.8a1 1 0 0 1 1.4 1.4l-.8.8c.3.5.5 1 .6 1.5H16a1 1 0 1 1 0 2h-1.1a6.8 6.8 0 0 1-.6 1.5l.8.8a1 1 0 0 1-1.4 1.4l-.8-.8c-.5.3-1 .5-1.5.6V17a1 1 0 1 1-2 0v-1.1a6.8 6.8 0 0 1-1.5-.6l-.8.8a1 1 0 0 1-1.4-1.4l.8-.8c-.3-.5-.5-1-.6-1.5H4a1 1 0 1 1 0-2h1.1c.1-.5.3-1 .6-1.5l-.8-.8a1 1 0 0 1 1.4-1.4l.8.8c.5-.3 1-.5 1.5-.6V3a1 1 0 0 1 1-1Z\"/>";
    private const string PeoplePath = "<path d=\"M7 4a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5Zm6 0a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5ZM7 10c-2.2 0-4 1.3-4 3v.5c0 .83.67 1.5 1.5 1.5h5c.83 0 1.5-.67 1.5-1.5V13c0-1.7-1.8-3-4-3Zm6 0c-.5 0-.98.07-1.43.2.88.74 1.43 1.73 1.43 2.8v.5c0 .54-.14 1.05-.4 1.5H15.5c.83 0 1.5-.67 1.5-1.5V13c0-1.7-1.8-3-4-3Z\"/>";

    // 16x16 glyphs (viewBox 0 0 16 16) used by domain badges (status / role / freshness / category).
    private const string CheckmarkPath = "<path d=\"M6.3 12.3a1 1 0 0 1-.71-.3L2.8 9.2A1 1 0 0 1 4.2 7.8l2.1 2.1 5.5-5.5a1 1 0 0 1 1.4 1.42l-6.2 6.2a1 1 0 0 1-.7.3Z\"/>";
    private const string SubtractCirclePath = "<path fill-rule=\"evenodd\" d=\"M8 1.6a6.4 6.4 0 1 0 0 12.8A6.4 6.4 0 0 0 8 1.6ZM5 7.2a.8.8 0 0 0 0 1.6h6a.8.8 0 0 0 0-1.6Z\"/>";
    private const string QuestionCirclePath = "<path fill-rule=\"evenodd\" d=\"M8 1.6a6.4 6.4 0 1 0 0 12.8A6.4 6.4 0 0 0 8 1.6ZM8 4.3c-1.25 0-2.3.83-2.45 2a.8.8 0 1 0 1.59.2c.05-.37.4-.6.86-.6.5 0 .85.3.85.68 0 .26-.1.42-.62.8-.62.45-1.03.92-1.03 1.72a.8.8 0 0 0 1.6 0c0-.2.1-.32.55-.66.62-.46 1.1-.95 1.1-1.96 0-1.27-1.04-2.18-2.45-2.18ZM8 10.6a.95.95 0 1 0 0 1.9.95.95 0 0 0 0-1.9Z\"/>";
    private const string WarningPath = "<path fill-rule=\"evenodd\" d=\"M6.9 2.6a1.25 1.25 0 0 1 2.2 0l5.2 9.05a1.25 1.25 0 0 1-1.08 1.87H2.78A1.25 1.25 0 0 1 1.7 11.65L6.9 2.6ZM8 5.8a.78.78 0 0 0-.78.86l.25 2.7a.53.53 0 0 0 1.06 0l.25-2.7A.78.78 0 0 0 8 5.8Zm0 4.9a.82.82 0 1 0 0 1.64.82.82 0 0 0 0-1.64Z\"/>";
    private const string ArrowSyncPath = "<path d=\"M8 3.4c1.7 0 3.2 1 3.9 2.4l.45-1.3a.7.7 0 0 1 1.32.46l-1 2.9a.7.7 0 0 1-.9.43l-2.9-1a.7.7 0 1 1 .46-1.32l1.1.38A3.1 3.1 0 0 0 4.9 8a.75.75 0 0 1-1.5 0A4.6 4.6 0 0 1 8 3.4Zm-3.55 5.3 2.9 1a.7.7 0 0 1-.46 1.32l-1-.34A3.1 3.1 0 0 0 11.1 8a.75.75 0 0 1 1.5 0A4.6 4.6 0 0 1 8 12.6c-1.7 0-3.2-.92-3.9-2.32l-.45 1.28a.7.7 0 0 1-1.32-.46l1-2.9a.7.7 0 0 1 .9-.43Z\"/>";
    private const string StarPath = "<path d=\"M7.3 2.6a.8.8 0 0 1 1.4 0l1.45 2.95 3.25.47a.8.8 0 0 1 .44 1.36l-2.35 2.3.56 3.24a.8.8 0 0 1-1.16.84L8 12.66l-2.9 1.53a.8.8 0 0 1-1.16-.84l.55-3.24-2.35-2.3a.8.8 0 0 1 .44-1.36l3.25-.47Z\"/>";
    private const string EditPath = "<path fill-rule=\"evenodd\" d=\"M11.8 2.4a1.4 1.4 0 0 1 2 0l.8.8a1.4 1.4 0 0 1 0 2l-7.2 7.2c-.2.2-.45.34-.72.4l-2.9.66a.7.7 0 0 1-.84-.84l.66-2.9c.06-.27.2-.52.4-.72l6.8-6.6Zm-6.6 8.1-.4 1.75 1.75-.4 6.6-6.6-1.35-1.35-6.6 6.6Z\"/>";
    private const string EyePath = "<path fill-rule=\"evenodd\" d=\"M8 4.5c3 0 5.6 1.85 6.75 4.25a.6.6 0 0 1 0 .5C13.6 11.65 11 13.5 8 13.5S2.4 11.65 1.25 9.25a.6.6 0 0 1 0-.5C2.4 6.35 5 4.5 8 4.5Zm0 1.5a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm0 1.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Z\"/>";
    private const string KeyPath = "<path fill-rule=\"evenodd\" d=\"M9.5 2.5a4 4 0 0 0-3.8 5.25L2.3 11.15a1 1 0 0 0-.3.7v1.65c0 .27.23.5.5.5h1.65a.5.5 0 0 0 .5-.5v-1h1a.5.5 0 0 0 .5-.5v-1h1c.13 0 .26-.05.35-.15l.55-.55A4 4 0 1 0 9.5 2.5Zm1.5 2.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0Z\"/>";
    private const string CopyPath = "<path fill-rule=\"evenodd\" d=\"M4.5 4.5V3A1.5 1.5 0 0 1 6 1.5h5A1.5 1.5 0 0 1 12.5 3v5A1.5 1.5 0 0 1 11 9.5H9.5V11A1.5 1.5 0 0 1 8 12.5H3A1.5 1.5 0 0 1 1.5 11V6A1.5 1.5 0 0 1 3 4.5h1.5ZM6 4.5H8A1.5 1.5 0 0 1 9.5 6v2H11V3H6v1.5ZM3 6v5h5V6H3Z\"/>";

    /// <summary>Creates the default bounded-context rail icon.</summary>
    public static Icon Apps20() => Create("Apps", IconSize.Size20, AppsPath);

    /// <summary>Creates a row-expansion chevron icon.</summary>
    public static Icon ChevronRight20() => Create("ChevronRight", IconSize.Size20, ChevronRightPath);

    /// <summary>Creates the empty-projection placeholder icon (32px — kept for legacy callers).</summary>
    public static Icon DocumentSearch32() => Create("DocumentSearch", IconSize.Size32, DocumentSearchPath);

    /// <summary>Creates the empty-projection placeholder icon at the UX-DR4 mandated 48px size.</summary>
    public static Icon DocumentSearch48() => Create("DocumentSearch", IconSize.Size48, DocumentSearchPath);

    /// <summary>Creates the development overlay icon.</summary>
    public static Icon DevMode20() => Create("DevMode", IconSize.Size20, DevModePath);

    /// <summary>Creates the default command action icon.</summary>
    public static Icon Play16() => Create("Play", IconSize.Size16, PlayPath);

    /// <summary>Creates the command-palette search icon.</summary>
    public static Icon Search20() => Create("Search", IconSize.Size20, SearchPath);

    /// <summary>Creates the settings icon.</summary>
    public static Icon Settings20() => Create("Settings", IconSize.Size20, SettingsPath);

    /// <summary>Creates the navigation (hamburger) icon used by the desktop sidebar toggle.</summary>
    public static Icon Navigation20() => Create("Navigation", IconSize.Size20, NavigationPath);

    /// <summary>Creates a people / group bounded-context rail icon.</summary>
    public static Icon People20() => Create("People", IconSize.Size20, PeoplePath);

    /// <summary>Creates a 16px success / present checkmark glyph (domain status &amp; freshness badges).</summary>
    public static Icon Checkmark16() => Create("Checkmark", IconSize.Size16, CheckmarkPath);

    /// <summary>Creates a 16px disabled / blocked (circle-minus) glyph.</summary>
    public static Icon SubtractCircle16() => Create("SubtractCircle", IconSize.Size16, SubtractCirclePath);

    /// <summary>Creates a 16px unknown (circle-question) glyph.</summary>
    public static Icon QuestionCircle16() => Create("QuestionCircle", IconSize.Size16, QuestionCirclePath);

    /// <summary>Creates a 16px warning (triangle) glyph for aging / stale freshness.</summary>
    public static Icon Warning16() => Create("Warning", IconSize.Size16, WarningPath);

    /// <summary>Creates a 16px refresh (two-arrow sync) glyph.</summary>
    public static Icon ArrowSync16() => Create("ArrowSync", IconSize.Size16, ArrowSyncPath);

    /// <summary>Creates a 16px star glyph (tenant owner role).</summary>
    public static Icon Star16() => Create("Star", IconSize.Size16, StarPath);

    /// <summary>Creates a 16px edit / pencil glyph (tenant contributor role).</summary>
    public static Icon Edit16() => Create("Edit", IconSize.Size16, EditPath);

    /// <summary>Creates a 16px eye glyph (tenant reader role).</summary>
    public static Icon Eye16() => Create("Eye", IconSize.Size16, EyePath);

    /// <summary>Creates a 16px key glyph (access audit category).</summary>
    public static Icon Key16() => Create("Key", IconSize.Size16, KeyPath);

    /// <summary>Creates a 16px copy (two-sheet) glyph for support-safe copy actions.</summary>
    public static Icon Copy16() => Create("Copy", IconSize.Size16, CopyPath);

    /// <summary>
    /// Attempts to create a supported FrontComposer icon from the existing
    /// <c>Variant.SizeNN.Name</c> contract.
    /// </summary>
    public static bool TryCreate(string? iconName, out Icon? icon) {
        icon = iconName switch {
            "Regular.Size16.Play" => Play16(),
            "Regular.Size20.Apps" => Apps20(),
            "Regular.Size20.ChevronRight" => ChevronRight20(),
            "Regular.Size20.DevMode" => DevMode20(),
            "Regular.Size20.Search" => Search20(),
            "Regular.Size20.Settings" => Settings20(),
            "Regular.Size20.Navigation" => Navigation20(),
            "Regular.Size20.People" => People20(),
            "Regular.Size32.DocumentSearch" => DocumentSearch32(),
            "Regular.Size48.DocumentSearch" => DocumentSearch48(),
            _ => null,
        };

        return icon is not null;
    }

    private static Icon Create(string name, IconSize size, string content)
        => new(name, IconVariant.Regular, size, content);
}
