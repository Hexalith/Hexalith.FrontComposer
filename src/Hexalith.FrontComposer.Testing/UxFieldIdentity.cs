namespace Hexalith.FrontComposer.Testing;

/// <summary>Prior rendered control/error targets keyed by stable adopter field identity.</summary>
/// <param name="FieldKey">Stable declaration identity.</param>
/// <param name="ControlId">Prior rendered control target.</param>
/// <param name="ErrorId">Prior rendered error target.</param>
public sealed record UxFieldIdentity(string FieldKey, string ControlId, string ErrorId);
