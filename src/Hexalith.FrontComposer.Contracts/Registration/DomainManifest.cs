namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Describes a domain's projections and commands, emitted by the source generator
/// for runtime composition.
/// </summary>
/// <param name="Name">The domain display name.</param>
/// <param name="BoundedContext">The bounded context this domain belongs to.</param>
/// <param name="Projections">Fully qualified type names of projections in this domain.</param>
/// <param name="Commands">Fully qualified type names of commands in this domain.</param>
public record DomainManifest(
    string Name,
    string BoundedContext,
    IReadOnlyList<string> Projections,
    IReadOnlyList<string> Commands);
