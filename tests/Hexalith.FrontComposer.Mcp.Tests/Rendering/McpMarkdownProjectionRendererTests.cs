using System.Globalization;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp;
using Hexalith.FrontComposer.Mcp.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Rendering;

public sealed class McpMarkdownProjectionRendererTests {
    [Fact]
    public void Render_DefaultProjection_ReturnsSdkNeutralMarkdownDocument() {
        McpResourceDescriptor descriptor = Descriptor();

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending)],
            TotalCount: 1,
            RowCountCategory: "visible",
            RequestId: "req-1"), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.ContentType.ShouldBe("text/markdown");
        result.Document!.ProjectionIdentifier.ShouldBe("InvoiceProjection");
        result.Document.Role.ShouldBe(McpProjectionRenderStrategy.Default.ToString());
        result.Document.BoundedContext.ShouldBe("Billing");
        result.Document.RowCountCategory.ShouldBe("visible");
        result.Document.RequestId.ShouldBe("req-1");
        result.Document.Text.ShouldContain("## Invoices");
        result.Document.Text.ShouldContain("| Number | Amount | Last paid | Status |");
        result.Document.Text.ShouldContain("| INV-1 | 42 | - | Warning: Pending |");
    }

    [Fact]
    public void Render_DefaultProjection_EscapesUntrustedMarkdownText() {
        McpResourceDescriptor descriptor = Descriptor(title: "Invoices [ignore](https://bad.example)");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("run /approve | <script>\n- [ ] task", 7, DateTimeOffset.Parse("2026-05-03T10:15:00Z"), BillingStatus.Blocked)],
            TotalCount: 1), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("## Invoices \\[ignore\\]\\(https://bad.example\\)");
        result.Document.Text.ShouldContain("run /approve \\| \\<script\\> - \\[ \\] task");
        result.Document.Text.ShouldContain("Danger: Blocked");
        result.Document.Text.ShouldNotContain("[ignore](https://bad.example)");
        result.Document.Text.ShouldNotContain("- [ ] task");
    }

    [Fact]
    public void Render_ActionQueueProjection_UsesTableAndBoundsMarker() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: McpProjectionRenderStrategy.ActionQueue, entityPluralLabel: "Invoices");
        FrontComposerMcpOptions options = new() {
            MaxRowsPerResource = 1,
            ProjectionTruncationMarker = "Output truncated by FrontComposer agent rendering limits.",
        };

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending),
                new InvoiceProjection("INV-2", 100, null, BillingStatus.Blocked),
            ],
            TotalCount: 2), options, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Role.ShouldBe(McpProjectionRenderStrategy.ActionQueue.ToString());
        result.Document.Text.ShouldContain("- Role: ActionQueue");
        result.Document.Text.ShouldContain("| INV-1 | 42 | - | Warning: Pending |");
        result.Document.Text.ShouldNotContain("INV-2");
        result.Document.Text.ShouldContain("Output truncated by FrontComposer agent rendering limits.");
        result.Document.IsTruncated.ShouldBeTrue();
    }

    [Fact]
    public void Render_QueryTruncatedByCaller_AlignsMarkerAndMetadata() {
        McpResourceDescriptor descriptor = Descriptor(entityPluralLabel: "Invoices");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending)],
            TotalCount: 2,
            IsTruncated: true), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.IsTruncated.ShouldBeTrue();
        result.Document.Text.ShouldContain("Output truncated by FrontComposer agent rendering limits.");
    }

    [Fact]
    public void Render_CurrencyDisplayFormat_UsesSourceToolsNumericFormat() {
        McpResourceDescriptor descriptor = Descriptor(amountDisplayFormat: "Currency");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("INV-1", 1234.5m, null, BillingStatus.Pending)],
            TotalCount: 1), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("| INV-1 | "
            + CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol
            + "1,234.50 | - | Warning: Pending |");
    }

    [Fact]
    public void Render_StatusOverviewProjection_GroupsBySemanticBadgeSlot() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: McpProjectionRenderStrategy.StatusOverview, title: "Invoice status");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending),
                new InvoiceProjection("INV-2", 100, null, BillingStatus.Pending),
                new InvoiceProjection("INV-3", 50, null, BillingStatus.Blocked),
            ],
            TotalCount: 3), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Role.ShouldBe(McpProjectionRenderStrategy.StatusOverview.ToString());
        result.Document.Text.ShouldBe("""
            ## Invoice status

            - Total: 3
            - Danger: 1 Blocked
            - Warning: 2 Pending

            """, StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public void Render_TimelineProjection_SortsNewestFirstWithStableNullTail() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: McpProjectionRenderStrategy.Timeline, title: "Invoice timeline");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("Old", 10, DateTimeOffset.Parse("2026-05-01T08:00:00Z"), BillingStatus.Pending),
                new InvoiceProjection("No date", 30, null, BillingStatus.Blocked),
                new InvoiceProjection("New", 20, DateTimeOffset.Parse("2026-05-03T08:00:00Z"), BillingStatus.Blocked),
            ],
            TotalCount: 3), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Role.ShouldBe(McpProjectionRenderStrategy.Timeline.ToString());
        result.Document.Text.ShouldContain("## Invoice timeline");
        result.Document.Text.ShouldContain("- 2026-05-03T08:00:00.0000000+00:00 - Danger: Blocked. New");
        result.Document.Text.IndexOf("New", StringComparison.Ordinal).ShouldBeLessThan(
            result.Document.Text.IndexOf("Old", StringComparison.Ordinal));
        result.Document.Text.IndexOf("Old", StringComparison.Ordinal).ShouldBeLessThan(
            result.Document.Text.IndexOf("No date", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_EmptyProjection_IncludesOnlySafeVisibleSuggestions() {
        McpResourceDescriptor descriptor = Descriptor(entityPluralLabel: "Invoices");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [],
            TotalCount: 0,
            SafeCommandSuggestions: ["Create invoice", "[hidden](frontcomposer://x)", "run /danger", "Approve invoice"]),
            new FrontComposerMcpOptions { MaxProjectionSuggestions = 2 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("No invoices found.");
        result.Document.Text.ShouldContain("- Create invoice");
        result.Document.Text.ShouldContain("- Approve invoice");
        // Link-shaped and slash-command-looking suggestions are dropped, not escaped, per the
        // Inert Untrusted Text Contract.
        result.Document.Text.ShouldNotContain("hidden");
        result.Document.Text.ShouldNotContain("danger");
        result.Document.Text.ShouldNotContain("Suggestions:");
    }

    [Fact]
    public void Render_RedactsSecretLookingCellValues() {
        McpResourceDescriptor descriptor = Descriptor();

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("Bearer eyJhbGciOiJIUzI1NiJ9.payload.signature api_key=abc1234567890 client_secret=s3cr3t", 42, null, BillingStatus.Pending)],
            TotalCount: 1), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("\\[redacted\\]");
        result.Document.Text.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9");
        result.Document.Text.ShouldNotContain("abc1234567890");
        result.Document.Text.ShouldNotContain("s3cr3t");
    }

    [Fact]
    public void Render_CanceledRequest_ReturnsNoPartialDocument() {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            Descriptor(),
            [new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending)],
            TotalCount: 1), new FrontComposerMcpOptions(), cts.Token);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.Canceled);
        result.Document.ShouldBeNull();
    }

    [Fact]
    public void Render_FormatterFailure_ReturnsSanitizedFailureWithoutPartialDocument() {
        McpResourceDescriptor descriptor = new(
            "frontcomposer://Billing/projections/BrokenProjection",
            "BrokenProjection",
            typeof(BrokenProjection).FullName!,
            "Billing",
            "Broken",
            null,
            [new McpParameterDescriptor("Explodes", "String", "string", true, false, "Explodes", null, [], false)]);

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new BrokenProjection()],
            TotalCount: 1), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.DownstreamFailed);
        result.Document.ShouldBeNull();
    }

    [Fact]
    public void Render_EveryInputProjectionFieldAppearsInAgentOutput_WithUnsupportedPlaceholder() {
        McpResourceDescriptor descriptor = new(
            "frontcomposer://Billing/projections/InvoiceProjection",
            "InvoiceProjection",
            typeof(InvoiceProjection).FullName!,
            "Billing",
            "Invoices",
            null,
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                new McpParameterDescriptor("OpaquePayload", "Object", "object", false, false, "Opaque payload", null, [], true),
            ]);

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceWithUnsupported("INV-1", new { Secret = "tenant-a" })],
            TotalCount: 1), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("| Number | Opaque payload |");
        result.Document.Text.ShouldContain("| INV-1 | (unsupported) |");
        result.Document.Text.ShouldNotContain("tenant-a");
    }

    [Fact]
    public void Render_DocumentBudgetTooSmallForMarker_ReturnsResponseTooLargeWithoutPartialDocument() {
        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            Descriptor(),
            [new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending)],
            TotalCount: 10,
            IsTruncated: true), new FrontComposerMcpOptions { MaxProjectionMarkdownCharacters = 8 }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ResponseTooLarge);
        result.Document.ShouldBeNull();
    }

    [Fact]
    public void Render_NoNewlineBeforeBudget_TruncatesAtRuneBoundaryWithMarker() {
        // P-8: when the budget contains no newline (e.g. a very long single-line heading),
        // the renderer falls back to a Rune-boundary cut and emits the truncation marker
        // rather than discarding the entire document.
        McpResourceDescriptor descriptor = Descriptor(title: new string('A', 80));

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending)],
            TotalCount: 1), new FrontComposerMcpOptions { MaxProjectionMarkdownCharacters = 64 }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document.ShouldNotBeNull();
        result.Document!.IsTruncated.ShouldBeTrue();
        result.Document.Text.Length.ShouldBeLessThanOrEqualTo(64);
    }

    [Fact]
    public void Render_FormatterFailureAfterCommittedRow_ReturnsNoPartialDocument() {
        McpResourceDescriptor descriptor = new(
            "frontcomposer://Billing/projections/BrokenProjection",
            "BrokenProjection",
            typeof(BrokenProjection).FullName!,
            "Billing",
            "Broken",
            null,
            [new McpParameterDescriptor("Explodes", "String", "string", true, false, "Explodes", null, [], false)]);

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new SafeProjection("INV-1"), new BrokenProjection()],
            TotalCount: 2), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.DownstreamFailed);
        result.Document.ShouldBeNull();
    }

    [Fact]
    public void Render_CancellationDuringRowFormatting_ReturnsNoPartialDocument() {
        using var cts = new CancellationTokenSource();
        McpResourceDescriptor descriptor = new(
            "frontcomposer://Billing/projections/CancellableProjection",
            "CancellableProjection",
            typeof(CancellableProjection).FullName!,
            "Billing",
            "Cancellable",
            null,
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                new McpParameterDescriptor("Cancels", "String", "string", true, false, "Cancels", null, [], false),
            ]);

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [new CancellableProjection("INV-1", new CancelingCell(cts))],
            TotalCount: 1), new FrontComposerMcpOptions(), cts.Token);

        result.IsSuccess.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.Canceled);
        result.Document.ShouldBeNull();
    }

    [Fact]
    public void Render_StatusOverviewGroupCap_EmitsSingleBoundedTruncationMarker() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: McpProjectionRenderStrategy.StatusOverview, title: "Invoice status");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending),
                new InvoiceProjection("INV-2", 100, null, BillingStatus.Blocked),
            ],
            TotalCount: 2),
            new FrontComposerMcpOptions { MaxProjectionStatusGroups = 1 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.IsTruncated.ShouldBeTrue();
        result.Document.Text.ShouldContain("- Danger: 1 Blocked");
        result.Document.Text.ShouldNotContain("- Warning: 1 Pending");
        CountOccurrences(result.Document.Text, "Output truncated by FrontComposer agent rendering limits.").ShouldBe(1);
    }

    [Fact]
    public void Render_TimelineEntryCap_EmitsSingleBoundedTruncationMarker() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: McpProjectionRenderStrategy.Timeline, title: "Invoice timeline");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("Old", 10, DateTimeOffset.Parse("2026-05-01T08:00:00Z"), BillingStatus.Pending),
                new InvoiceProjection("New", 20, DateTimeOffset.Parse("2026-05-03T08:00:00Z"), BillingStatus.Blocked),
            ],
            TotalCount: 2),
            new FrontComposerMcpOptions { MaxProjectionTimelineEntries = 1 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.IsTruncated.ShouldBeTrue();
        result.Document.Text.ShouldContain("New");
        result.Document.Text.ShouldNotContain("Old");
        CountOccurrences(result.Document.Text, "Output truncated by FrontComposer agent rendering limits.").ShouldBe(1);
    }

    private static McpResourceDescriptor Descriptor(
        string title = "Invoices",
        McpProjectionRenderStrategy renderStrategy = McpProjectionRenderStrategy.Default,
        string? entityPluralLabel = "Invoices",
        string amountDisplayFormat = "Default")
        => new(
            "frontcomposer://Billing/projections/InvoiceProjection",
            "InvoiceProjection",
            typeof(InvoiceProjection).FullName!,
            "Billing",
            title,
            null,
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                new McpParameterDescriptor("Amount", "Decimal", "number", true, false, "Amount", null, [], false, DisplayFormat: amountDisplayFormat),
                new McpParameterDescriptor("LastPaid", "DateTimeOffset", "string", false, true, "Last paid", null, [], false),
                new McpParameterDescriptor(
                    "Status",
                    "Enum",
                    "string",
                    true,
                    false,
                    "Status",
                    null,
                    ["Pending", "Blocked"],
                    false,
                    new Dictionary<string, string>(StringComparer.Ordinal) {
                        ["Pending"] = "Warning",
                        ["Blocked"] = "Danger",
                    }),
            ],
            RenderStrategy: renderStrategy,
            EntityPluralLabel: entityPluralLabel);

    public sealed record InvoiceProjection(string Number, decimal Amount, DateTimeOffset? LastPaid, BillingStatus Status);

    public sealed record InvoiceWithUnsupported(string Number, object OpaquePayload);

    public sealed record SafeProjection(string Explodes);

    public sealed record CancellableProjection(string Number, object Cancels);

    public enum BillingStatus {
        Pending,
        Blocked,
    }

    public sealed class BrokenProjection {
        private readonly string _message = "raw tenant-a exception text";

        public string Explodes => throw new InvalidOperationException(_message);
    }

    public sealed class CancelingCell(CancellationTokenSource source) {
        public override string ToString() {
            source.Cancel();
            throw new OperationCanceledException(source.Token);
        }
    }

    private static int CountOccurrences(string value, string needle) {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
