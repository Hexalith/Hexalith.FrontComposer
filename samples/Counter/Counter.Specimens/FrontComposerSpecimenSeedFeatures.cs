using Counter.Domain;
using Counter.Specimens.Domain;

using Fluxor;

using System.Globalization;

namespace Counter.Specimens;

/// <summary>
/// Specimen-only Fluxor features with deterministic initial rows for accessibility evidence.
/// </summary>
public sealed class SpecimenStatusProjectionSeedFeature : Feature<SpecimenStatusProjectionState> {
    public override string GetName() => nameof(SpecimenStatusProjectionState);

    protected override SpecimenStatusProjectionState GetInitialState()
        => new(IsLoading: false, Items: FrontComposerSpecimenSeedData.StatusRows, Error: null);
}

/// <summary>
/// Specimen-only Fluxor feature for the generated Counter projection grid evidence.
/// </summary>
public sealed class CounterProjectionSeedFeature : Feature<CounterProjectionState> {
    public override string GetName() => nameof(CounterProjectionState);

    protected override CounterProjectionState GetInitialState()
        => new(IsLoading: false, Items: FrontComposerSpecimenSeedData.CounterRows, Error: null);
}

/// <summary>
/// Specimen-only Fluxor feature for generated formatting field evidence.
/// </summary>
public sealed class SpecimenFormattingProjectionSeedFeature : Feature<SpecimenFormattingProjectionState> {
    public override string GetName() => nameof(SpecimenFormattingProjectionState);

    protected override SpecimenFormattingProjectionState GetInitialState()
        => new(IsLoading: false, Items: FrontComposerSpecimenSeedData.FormattingRows, Error: null);
}

internal static class FrontComposerSpecimenSeedData {
    internal static readonly SpecimenStatusProjection[] StatusRows = [
        new() { Id = "FC-1000", Name = "Neutral projection", Status = SpecimenBadgeState.Neutral, Owner = "Shell" },
        new() { Id = "FC-1001", Name = "Info projection", Status = SpecimenBadgeState.Info, Owner = "SourceTools" },
        new() { Id = "FC-1002", Name = "Success projection", Status = SpecimenBadgeState.Success, Owner = "Contracts" },
        new() { Id = "FC-1003", Name = "Warning projection", Status = SpecimenBadgeState.Warning, Owner = "Testing" },
        new() { Id = "FC-1004", Name = "Danger projection", Status = SpecimenBadgeState.Danger, Owner = "CI" },
        new() { Id = "FC-1005", Name = "Accent projection", Status = SpecimenBadgeState.Accent, Owner = "Docs" },
    ];

    internal static readonly CounterProjection[] CounterRows = [
        new() {
            Id = "fc-correlation-0002",
            Count = 42,
            LastUpdated = DateTimeOffset.Parse("2026-01-15T13:45:00Z", CultureInfo.InvariantCulture),
            Metadata = new Dictionary<string, string> { ["source"] = "specimen" },
        },
    ];

    internal static readonly SpecimenFormattingProjection[] FormattingRows = [
        new() {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TotalOrders = 12345.67m,
            SubmittedAt = DateTimeOffset.Parse("2026-01-15T13:45:00Z", CultureInfo.InvariantCulture),
            LastSync = DateTimeOffset.UtcNow.AddHours(-3),
            OptionalComment = null,
            Approvers = ["Ada", "Grace", "Katherine"],
            Budget = 1234.50m,
            IsActive = true,
            LifecycleState = SpecimenLongLifecycleState.AwaitingManualReviewAndExtendedGovernanceApproval,
            OpaquePayload = new Dictionary<string, string> { ["kind"] = "unsupported" },
        },
    ];
}
