namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Projection group health observed during the latest reconnect rejoin pass.</summary>
public readonly record struct ProjectionFallbackGroupKey(string ProjectionType, string TenantId);
