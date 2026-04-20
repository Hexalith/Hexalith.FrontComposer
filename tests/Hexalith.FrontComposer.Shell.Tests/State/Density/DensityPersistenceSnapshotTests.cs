// ATDD RED PHASE — Story 3-3 Task 10.5a (D8, D18; AC3, AC5)
// The serialiser invocations work today (DensityLevel? is a valid System.Text.Json target),
// but the Verify baselines in *.verified.txt lock the wire format so any future schema change
// (e.g., enum-as-int converter, wrapper record) produces a CI hard-fail.

using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.5a — persistence wire-format lock for <c>DensityLevel?</c> stored
/// under <c>{tenantId}:{userId}:density</c>. Both the non-null (UserPreferenceChanged) and
/// the null (UserPreferenceCleared) paths are pinned. Mirrors Story 3-2's
/// <c>NavigationPersistenceSnapshotTests</c> baseline-lock pattern.
/// </summary>
public sealed class DensityPersistenceSnapshotTests
{
    [Fact]
    public Task SerialisedNonNull_LocksSchema()
    {
        DensityLevel? value = DensityLevel.Roomy;
        string serialised = JsonSerializer.Serialize(value);
        return Verify(serialised);
    }

    [Fact]
    public Task SerialisedNull_LocksSchema()
    {
        DensityLevel? value = null;
        string serialised = JsonSerializer.Serialize(value);
        return Verify(serialised);
    }
}
