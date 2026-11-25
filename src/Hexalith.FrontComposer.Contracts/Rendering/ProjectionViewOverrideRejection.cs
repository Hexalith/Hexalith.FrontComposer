namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>A Level 4 view override descriptor rejected during startup hydration.</summary>
/// <param name="Descriptor">The rejected registration.</param>
/// <param name="DiagnosticId">The stable diagnostic identifier.</param>
/// <param name="Reason">A developer-facing explanation of the rejection.</param>
public sealed record ProjectionViewOverrideRejection(
    ProjectionViewOverrideDescriptor Descriptor,
    string DiagnosticId,
    string Reason);
