using System;
using System.Collections.Generic;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Storage;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 3-6 AC8 / ADR-050 — schema-lock for <see cref="GridViewPersistenceBlob"/>. Serializes
/// through the production <see cref="LocalStorageService.SchemaLockJsonOptions"/> so the pinned
/// shape matches what LocalStorage actually sees.
/// </summary>
public sealed class GridViewPersistenceBlobSchemaLockedTests
{
    [Fact]
    public void BlobSerializesToExpectedJsonShape()
    {
        GridViewPersistenceBlob blob = new(
            ScrollTop: 128.0,
            Filters: new Dictionary<string, string> { ["Name"] = "ACME" },
            SortColumn: "CreatedAt",
            SortDescending: true,
            ExpandedRowId: "01J",
            SelectedRowId: "01K",
            CapturedAt: new DateTimeOffset(2026, 4, 22, 12, 0, 0, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(blob, LocalStorageService.SchemaLockJsonOptions);

        json.ShouldContain("\"scrollTop\":128");
        json.ShouldContain("\"filters\":{\"Name\":\"ACME\"}");
        json.ShouldContain("\"sortColumn\":\"CreatedAt\"");
        json.ShouldContain("\"sortDescending\":true");
        json.ShouldContain("\"expandedRowId\":\"01J\"");
        json.ShouldContain("\"selectedRowId\":\"01K\"");
        json.ShouldContain("\"capturedAt\":");
    }

    [Fact]
    public void BlobRoundTripsThroughProductionJsonOptions()
    {
        GridViewPersistenceBlob original = new(
            ScrollTop: 42.5,
            Filters: new Dictionary<string, string> { ["Status"] = "Active" },
            SortColumn: "Name",
            SortDescending: false,
            ExpandedRowId: null,
            SelectedRowId: null,
            CapturedAt: new DateTimeOffset(2026, 4, 22, 9, 30, 0, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(original, LocalStorageService.SchemaLockJsonOptions);
        GridViewPersistenceBlob? roundTripped = JsonSerializer.Deserialize<GridViewPersistenceBlob>(
            json, LocalStorageService.SchemaLockJsonOptions);

        roundTripped.ShouldNotBeNull();
        roundTripped!.ScrollTop.ShouldBe(42.5);
        roundTripped.Filters.ShouldNotBeNull();
        roundTripped.Filters["Status"].ShouldBe("Active");
        roundTripped.SortColumn.ShouldBe("Name");
        roundTripped.SortDescending.ShouldBeFalse();
        roundTripped.ExpandedRowId.ShouldBeNull();
        roundTripped.SelectedRowId.ShouldBeNull();
        roundTripped.CapturedAt.ShouldBe(original.CapturedAt);
    }

    [Fact]
    public void ToSnapshotNormalisesCapturedAtToUtc()
    {
        // Review Finding F-EH-008 — non-zero offset must not throw on snapshot conversion.
        GridViewPersistenceBlob blob = new(
            ScrollTop: 0,
            Filters: new Dictionary<string, string>(),
            SortColumn: null,
            SortDescending: false,
            ExpandedRowId: null,
            SelectedRowId: null,
            CapturedAt: new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.FromHours(2)));

        GridViewSnapshot snapshot = blob.ToSnapshot();

        snapshot.CapturedAt.Offset.ShouldBe(TimeSpan.Zero);
        snapshot.CapturedAt.UtcDateTime.ShouldBe(blob.CapturedAt.UtcDateTime);
    }

    [Fact]
    public void ToSnapshotClampsScrollTopToNonNegativeFinite()
    {
        GridViewPersistenceBlob blob = new(
            ScrollTop: double.NaN,
            Filters: new Dictionary<string, string>(),
            SortColumn: null,
            SortDescending: false,
            ExpandedRowId: null,
            SelectedRowId: null,
            CapturedAt: new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero));

        GridViewSnapshot snapshot = blob.ToSnapshot();

        snapshot.ScrollTop.ShouldBe(0);
    }
}
