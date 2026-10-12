namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>Effective exposure of a retained page panel, including its inactive ancestors.</summary>
/// <param name="IsVisible">Whether this panel and every containing panel are active.</param>
internal sealed record PagePanelVisibility(bool IsVisible);
