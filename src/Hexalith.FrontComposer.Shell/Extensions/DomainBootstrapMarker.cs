namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>Marks that an <c>AddHexalithDomain&lt;TMarker&gt;()</c> call ran.</summary>
internal sealed record DomainBootstrapMarker : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.Domain;
}
