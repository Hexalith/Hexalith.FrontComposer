// ATDD RED PHASE — Story 3-2 Task 10.7 (D21 / AC2 / ADR-037)
// Fails at compile until Task 3.2 (NavigationPersistenceBlob) lands.
// Once it compiles, the Verify snapshot baseline locks the wire format — the .verified.txt is
// the cross-story contract consumed by Story 3-6 session-resume.

using System.Text.Json;

using Hexalith.FrontComposer.Shell.State.Navigation;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-2 Task 10.7 — persistence blob wire-format lock (D21, ADR-037). Asserts JSON property
/// order and that <c>CurrentViewport</c>/<c>viewport</c>/<c>tier</c> fields are ABSENT from the
/// serialised form (viewport is derived at runtime, never persisted).
/// </summary>
public sealed class NavigationPersistenceSnapshotTests
{
    [Fact]
    public Task BlobSchemaLocked()
    {
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: true,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["Counter"] = true,
                ["Orders"] = false,
            });

        string serialised = JsonSerializer.Serialize(blob);
        return Verify(serialised);
    }
}
