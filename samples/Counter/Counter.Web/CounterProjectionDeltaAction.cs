namespace Counter.Web;

/// <summary>Applies a confirmed sample command to the Counter projection.</summary>
/// <param name="CorrelationId">The confirmed command's correlation ID.</param>
/// <param name="Delta">The change to the counter.</param>
/// <param name="TenantId">The submitting tenant.</param>
/// <param name="UserId">The submitting user.</param>
/// <param name="ScopeGeneration">The projection scope at submission.</param>
/// <param name="Timestamp">The time the sample received confirmation.</param>
public sealed record CounterProjectionDeltaAction(
    string CorrelationId,
    int Delta,
    string TenantId,
    string UserId,
    long ScopeGeneration,
    DateTimeOffset Timestamp);
