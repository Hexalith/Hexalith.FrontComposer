// Story 3-2 Task 10.7 (D21 / AC2 / ADR-037) — evolved by Story 3-6 Review Finding F-BH-003 / F-AA-001
// to serialise through LocalStorageService's production JsonSerializerOptions (camelCase +
// WhenWritingDefault) so the verified snapshot pins the actual wire format, not the default.

using System.Text.Json;

using Hexalith.FrontComposer.Shell.Infrastructure.Storage;
using Hexalith.FrontComposer.Shell.State.Navigation;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Persistence blob wire-format lock (D21, ADR-037). Asserts JSON property order and that
/// <c>CurrentViewport</c>/<c>viewport</c>/<c>tier</c> fields are ABSENT from the serialised form
/// (viewport is derived at runtime, never persisted).
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

        string serialised = JsonSerializer.Serialize(blob, LocalStorageService.SchemaLockJsonOptions);
        return Verify(serialised);
    }
}
