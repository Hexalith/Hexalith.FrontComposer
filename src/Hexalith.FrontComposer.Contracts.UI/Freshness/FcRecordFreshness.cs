namespace Hexalith.FrontComposer.Contracts.Freshness;

/// <summary>A transport-neutral presentation snapshot for one record.</summary>
/// <param name="Status">Normalized presentation state.</param>
/// <param name="AsOf">Optional caller-owned observation time.</param>
public sealed record FcRecordFreshness(FcFreshnessStatus Status, DateTimeOffset? AsOf = null);
