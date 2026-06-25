using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Describes one optional tab rendered by <see cref="FcPageToolbar"/>.
/// </summary>
/// <param name="Id">Stable tab id used by <c>FluentTabs.ActiveTabId</c>.</param>
/// <param name="Header">Visible tab label.</param>
/// <param name="Disabled">Whether the tab is disabled.</param>
/// <param name="IconStart">Optional caller-supplied Fluent icon rendered before the label.</param>
public sealed record FcPageToolbarTab(string Id, string Header, bool Disabled = false, Icon? IconStart = null);
