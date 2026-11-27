namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>Marks that the foundational <c>AddHexalithFrontComposer()</c> call ran.</summary>
internal sealed record QuickstartBootstrapMarker(Action? ValidateRegistrations = null) : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.Quickstart;
}
