using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Icons;

/// <summary>
/// FrontComposer-owned icon factory built on Fluent UI v5's core <see cref="Icon"/> abstraction.
/// </summary>
public static class FcFluentIcons {
    private const string AppsPath = "<path d=\"M 4 4 h 5 v 5 H 4 V 4 Z m 7 0 h 5 v 5 h -5 V 4 Z M 4 11 h 5 v 5 H 4 v -5 Z m 7 0 h 5 v 5 h -5 v -5 Z\"/>";
    private const string ChevronRightPath = "<path d=\"M7.2 4.3a1 1 0 0 1 1.4 0l4.3 4.3a1 1 0 0 1 0 1.4l-4.3 4.3a1 1 0 1 1-1.4-1.4L10.8 9 7.2 5.4a1 1 0 0 1 0-1.1Z\"/>";
    private const string DocumentSearchPath = "<path d=\"M8 4h10l6 6v7.2a7 7 0 0 0-2-1.1V11h-5V6H8v20h8.1a7 7 0 0 0 1.1 2H8a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Zm15.5 14a5.5 5.5 0 0 1 4.3 8.9l2 2a1 1 0 0 1-1.4 1.4l-2-2A5.5 5.5 0 1 1 23.5 18Zm0 2a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7Z\"/>";
    private const string PlayPath = "<path d=\"M5 3.8c0-.8.9-1.3 1.6-.8l7 4.2a1 1 0 0 1 0 1.7l-7 4.2A1 1 0 0 1 5 12.2V3.8Z\"/>";
    private const string SearchPath = "<path d=\"M8.5 4a4.5 4.5 0 1 0 2.8 8l3.4 3.4a1 1 0 0 0 1.4-1.4l-3.4-3.4A4.5 4.5 0 0 0 8.5 4Zm0 2a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5Z\"/>";
    private const string SettingsPath = "<path d=\"M10 6a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm0 2a2 2 0 1 1 0 4 2 2 0 0 1 0-4Zm0-6a1 1 0 0 1 1 1v1.1a6.8 6.8 0 0 1 1.5.6l.8-.8a1 1 0 0 1 1.4 1.4l-.8.8c.3.5.5 1 .6 1.5H16a1 1 0 1 1 0 2h-1.1a6.8 6.8 0 0 1-.6 1.5l.8.8a1 1 0 0 1-1.4 1.4l-.8-.8c-.5.3-1 .5-1.5.6V17a1 1 0 1 1-2 0v-1.1a6.8 6.8 0 0 1-1.5-.6l-.8.8a1 1 0 0 1-1.4-1.4l.8-.8c-.3-.5-.5-1-.6-1.5H4a1 1 0 1 1 0-2h1.1c.1-.5.3-1 .6-1.5l-.8-.8a1 1 0 0 1 1.4-1.4l.8.8c.5-.3 1-.5 1.5-.6V3a1 1 0 0 1 1-1Z\"/>";

    /// <summary>Creates the default bounded-context rail icon.</summary>
    public static Icon Apps20() => Create("Apps", IconSize.Size20, AppsPath);

    /// <summary>Creates a row-expansion chevron icon.</summary>
    public static Icon ChevronRight20() => Create("ChevronRight", IconSize.Size20, ChevronRightPath);

    /// <summary>Creates the empty-projection placeholder icon (32px — kept for legacy callers).</summary>
    public static Icon DocumentSearch32() => Create("DocumentSearch", IconSize.Size32, DocumentSearchPath);

    /// <summary>Creates the empty-projection placeholder icon at the UX-DR4 mandated 48px size.</summary>
    public static Icon DocumentSearch48() => Create("DocumentSearch", IconSize.Size48, DocumentSearchPath);

    /// <summary>Creates the default command action icon.</summary>
    public static Icon Play16() => Create("Play", IconSize.Size16, PlayPath);

    /// <summary>Creates the command-palette search icon.</summary>
    public static Icon Search20() => Create("Search", IconSize.Size20, SearchPath);

    /// <summary>Creates the settings icon.</summary>
    public static Icon Settings20() => Create("Settings", IconSize.Size20, SettingsPath);

    /// <summary>
    /// Attempts to create a supported FrontComposer icon from the existing
    /// <c>Variant.SizeNN.Name</c> contract.
    /// </summary>
    public static bool TryCreate(string? iconName, out Icon? icon) {
        icon = iconName switch {
            "Regular.Size16.Play" => Play16(),
            "Regular.Size20.Apps" => Apps20(),
            "Regular.Size20.ChevronRight" => ChevronRight20(),
            "Regular.Size20.Search" => Search20(),
            "Regular.Size20.Settings" => Settings20(),
            "Regular.Size32.DocumentSearch" => DocumentSearch32(),
            "Regular.Size48.DocumentSearch" => DocumentSearch48(),
            _ => null,
        };

        return icon is not null;
    }

    private static Icon Create(string name, IconSize size, string content)
        => new(name, IconVariant.Regular, size, content);
}
