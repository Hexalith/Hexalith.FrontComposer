using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Defines the shared expected inventory for <c>FrontComposerDiagnosticLog</c> metadata observers.
/// </summary>
internal static class FrontComposerDiagnosticLogInventoryAssertions
{
    /// <summary>
    /// Gets the exact number of diagnostic events and public wrappers.
    /// </summary>
    public const int ExpectedEventCount = 73;

    /// <summary>
    /// Asserts the exact normalized diagnostic inventory contract.
    /// </summary>
    /// <param name="entries">Entries extracted by one independent observer.</param>
    public static void Assert(IEnumerable<FrontComposerDiagnosticLogInventoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        FrontComposerDiagnosticLogInventoryEntry[] actual = [.. entries.OrderBy(static entry => entry.EventId)];
        string locations = FormatLocations(actual);

        actual.Length.ShouldBe(ExpectedEventCount, $"Diagnostic event count drifted. {locations}");
        actual.Select(static entry => entry.EventId).ShouldBe(
            Enumerable.Range(6000, ExpectedEventCount),
            $"Diagnostic EventIds must be the exact contiguous 6000-6072 band. {locations}");
        actual.ShouldAllBe(
            static entry => !string.IsNullOrWhiteSpace(entry.EventName),
            $"Every diagnostic event must have a nonblank name. {locations}");
        actual.Select(static entry => entry.EventName).Distinct(StringComparer.Ordinal).Count().ShouldBe(
            ExpectedEventCount,
            $"Diagnostic event names must be unique. {locations}");
        actual.ShouldAllBe(
            static entry => entry.Level == LogLevel.Debug || entry.Level == LogLevel.Information,
            $"Diagnostic events may only use Debug or Information. {locations}");
        actual.Count(static entry => entry.Level == LogLevel.Information).ShouldBe(
            56,
            $"The Information inventory drifted. {locations}");
        actual.Count(static entry => entry.Level == LogLevel.Debug).ShouldBe(
            17,
            $"The Debug inventory drifted. {locations}");
        actual.Count(static entry => entry.HasExceptionParameter).ShouldBe(
            20,
            $"The exception-bearing inventory drifted. {locations}");
    }

    private static string FormatLocations(IEnumerable<FrontComposerDiagnosticLogInventoryEntry> entries)
        => "Observed entries: " + string.Join(
            "; ",
            entries.Select(static entry =>
                $"{entry.Location} => {entry.EventId}/{entry.EventName ?? "<null>"}/{entry.Level?.ToString() ?? "<null>"}/exception={entry.HasExceptionParameter}"));
}
