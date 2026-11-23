using System.Collections;

namespace Hexalith.FrontComposer.Cli.Tests;

internal sealed class CancelAfterReadingMigrationEntries(
    IReadOnlyList<MigrationEntry> entries,
    CancellationTokenSource cancellation) : IReadOnlyList<MigrationEntry>
{
    public int Count => entries.Count;

    public MigrationEntry this[int index] => entries[index];

    // Apply enumerates these entries only after it has successfully written the file.
    public IEnumerator<MigrationEntry> GetEnumerator()
    {
        foreach (MigrationEntry entry in entries)
        {
            yield return entry;
        }

        cancellation.Cancel();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
