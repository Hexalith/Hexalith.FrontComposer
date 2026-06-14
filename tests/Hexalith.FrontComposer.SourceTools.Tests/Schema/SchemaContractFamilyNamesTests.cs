using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Schema;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Schema;

/// <summary>
/// P-46 (8-6a Group B): build-time exhaustiveness for <see cref="SchemaContractFamilyNames"/>.
/// The switch is fail-closed (throws on unknown enum values), but a missing arm only surfaces at
/// runtime when a baseline-loading code path hits the gap — and the throw is then masked by the
/// projection reader's outer catch as <c>DownstreamFailed</c>, hiding the real misconfiguration
/// behind a "temporarily unavailable, please retry" agent surface. These tests enumerate every
/// <see cref="SchemaContractFamily"/> value and assert each yields a distinct, non-empty,
/// kebab-case canonical name without throwing, so adding a new enum member without a matching
/// arm fails build immediately.
/// </summary>
public sealed class SchemaContractFamilyNamesTests {
    [Fact]
    public void Canonical_AllEnumValues_HaveExplicitMapping() {
        SchemaContractFamily[] all = Enum.GetValues<SchemaContractFamily>();

        all.ShouldNotBeEmpty("SchemaContractFamily must declare at least one member.");

        foreach (SchemaContractFamily family in all) {
            string canonical = SchemaContractFamilyNames.Canonical(family);
            canonical.ShouldNotBeNullOrWhiteSpace($"Canonical mapping for {family} must yield a non-empty name.");
            Regex.IsMatch(canonical, "^[a-z][a-z0-9]*(-[a-z0-9]+)*$").ShouldBeTrue(
                $"Canonical mapping for {family} must be kebab-case (got '{canonical}').");
        }
    }

    [Fact]
    public void Canonical_DistinctValuesPerFamily() {
        SchemaContractFamily[] all = Enum.GetValues<SchemaContractFamily>();
        HashSet<string> canonicalSet = new(StringComparer.Ordinal);

        foreach (SchemaContractFamily family in all) {
            string canonical = SchemaContractFamilyNames.Canonical(family);
            canonicalSet.Add(canonical).ShouldBeTrue(
                $"Canonical name '{canonical}' for {family} collides with another family's mapping.");
        }
    }
}
