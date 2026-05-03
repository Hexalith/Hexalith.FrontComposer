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
        result.Document.Role.ShouldBe("Default");
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
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: "ActionQueue", entityPluralLabel: "Invoices");
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
        result.Document!.Role.ShouldBe("ActionQueue");
        result.Document.Text.ShouldContain("- Role: ActionQueue");
        result.Document.Text.ShouldContain("| INV-1 | 42 | - | Warning: Pending |");
        result.Document.Text.ShouldNotContain("INV-2");
        result.Document.Text.ShouldContain("Output truncated by FrontComposer agent rendering limits.");
    }

    [Fact]
    public void Render_StatusOverviewProjection_GroupsBySemanticBadgeSlot() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: "StatusOverview", title: "Invoice status");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("INV-1", 42, null, BillingStatus.Pending),
                new InvoiceProjection("INV-2", 100, null, BillingStatus.Pending),
                new InvoiceProjection("INV-3", 50, null, BillingStatus.Blocked),
            ],
            TotalCount: 3), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Role.ShouldBe("StatusOverview");
        result.Document.Text.ShouldBe("""
            ## Invoice status

            - Total: 3
            - Warning: 2 Pending
            - Danger: 1 Blocked

            """, StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public void Render_TimelineProjection_SortsNewestFirstWithStableNullTail() {
        McpResourceDescriptor descriptor = Descriptor(renderStrategy: "Timeline", title: "Invoice timeline");

        McpProjectionRenderResult result = McpMarkdownProjectionRenderer.Render(new McpProjectionRenderRequest(
            descriptor,
            [
                new InvoiceProjection("Old", 10, DateTimeOffset.Parse("2026-05-01T08:00:00Z"), BillingStatus.Pending),
                new InvoiceProjection("No date", 30, null, BillingStatus.Blocked),
                new InvoiceProjection("New", 20, DateTimeOffset.Parse("2026-05-03T08:00:00Z"), BillingStatus.Blocked),
            ],
            TotalCount: 3), new FrontComposerMcpOptions(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Role.ShouldBe("Timeline");
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
            SafeCommandSuggestions: ["Create invoice", "[hidden](frontcomposer://x)", "run /danger"]),
            new FrontComposerMcpOptions { MaxProjectionSuggestions = 2 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Document!.Text.ShouldContain("No invoices found.");
        result.Document.Text.ShouldContain("Suggestions:");
        result.Document.Text.ShouldContain("- Create invoice");
        result.Document.Text.ShouldContain("- \\[hidden\\]\\(frontcomposer://x\\)");
        result.Document.Text.ShouldNotContain("run /danger");
        result.Document.Text.ShouldNotContain("[hidden](frontcomposer://x)");
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

    private static McpResourceDescriptor Descriptor(
        string title = "Invoices",
        string renderStrategy = "Default",
        string? entityPluralLabel = "Invoices")
        => new(
            "frontcomposer://Billing/projections/InvoiceProjection",
            "InvoiceProjection",
            typeof(InvoiceProjection).FullName!,
            "Billing",
            title,
            null,
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
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

    public sealed record InvoiceProjection(string Number, int Amount, DateTimeOffset? LastPaid, BillingStatus Status);

    public enum BillingStatus {
        Pending,
        Blocked,
    }

    public sealed class BrokenProjection {
        private readonly string _message = "raw tenant-a exception text";

        public string Explodes => throw new InvalidOperationException(_message);
    }
}
