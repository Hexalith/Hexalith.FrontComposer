namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>Marks that an <c>AddHexalithEventStore(...)</c> call ran.</summary>
internal sealed record EventStoreBootstrapMarker : IFrontComposerBootstrapMarker {
    /// <inheritdoc />
    public FrontComposerBootstrapStage Stage => FrontComposerBootstrapStage.EventStore;
}
