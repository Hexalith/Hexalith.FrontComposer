using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.EventStore;

public sealed class FcPendingCommandSummaryTests : LayoutComponentTestBase {
    public FcPendingCommandSummaryTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersBoundedCountsBeforeDetails() {
        PendingCommandEntry[] entries = [
            Entry("m1", PendingCommandStatus.Confirmed),
            Entry("m2", PendingCommandStatus.Rejected, "Save failed", "No data changed."),
            Entry("m3", PendingCommandStatus.IdempotentConfirmed),
            Entry("m4", PendingCommandStatus.NeedsReview),
        ];

        IRenderedComponent<FcPendingCommandSummary> cut = Render<FcPendingCommandSummary>(parameters => parameters
            .Add(p => p.Entries, entries)
            .Add(p => p.MaxDetails, 2));

        cut.Markup.ShouldContain("2 confirmed, 1 rejected, 1 unresolved");
        cut.Markup.ShouldContain("additional resolved commands");
        cut.Markup.ShouldContain("aria-live=\"polite\"");
    }

    [Fact]
    public void RejectedOutcomeUsesDangerMessageBarAndDoesNotAutoDismiss() {
        PendingCommandEntry[] entries = [
            Entry("m2", PendingCommandStatus.Rejected, "Save failed", "No data changed."),
        ];

        IRenderedComponent<FcPendingCommandSummary> cut = Render<FcPendingCommandSummary>(parameters => parameters
            .Add(p => p.Entries, entries));

        cut.Markup.ShouldContain("intent=\"error\"", Case.Insensitive);
        cut.Markup.ShouldContain("data-allow-dismiss=\"false\"", Case.Insensitive);
        cut.Markup.ShouldContain("Save failed: No data changed.");
    }

    private static PendingCommandEntry Entry(
        string messageId,
        PendingCommandStatus status,
        string? title = null,
        string? detail = null) =>
        new(
            CorrelationId: string.Concat("corr-", messageId),
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft",
            SubmittedAt: new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero),
            Status: status,
            RejectionTitle: title,
            RejectionDetail: detail,
            TerminalAt: new DateTimeOffset(2026, 4, 26, 12, 0, 1, TimeSpan.Zero));
}
