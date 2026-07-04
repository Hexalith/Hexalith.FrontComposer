using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Docs;

/// <summary>
/// Story 9.1 guard for the FC-NIP row-identity producer contract. The story is a
/// contract/documentation confirmation, so this test pins the decision artifact instead of
/// asserting generated output. Assertions read whitespace-normalized content so a benign markdown
/// reflow (line-wrapping, LF/CRLF) cannot silently break the governance guard.
/// </summary>
[Trait("Category", "Governance")]
public sealed class FcNipRowIdentityProducerContractTests {
    private const string ContractPath = "_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md";

    [Fact]
    public void FcNipContract_WhenAuthored_RecordsMinimumPayloadAndBlockingGap() {
        string contract = ReadNormalized(ContractPath);

        foreach (string candidate in new[] {
            "EventStore command status",
            "Submit result payload",
            "Projection nudge",
            "Projection detail nudge metadata",
            "Pending-command registration metadata",
            "Generated command metadata",
        }) {
            contract.ShouldContain(candidate);
        }

        foreach (string payloadField in new[] {
            "ViewKey",
            "EntityKey",
            "MessageId",
            "ProjectionTypeName",
            "ExpectedStatusSlot",
            "CreatedAt",
            "TenantId",
            "UserId",
            "first-wins",
        }) {
            contract.ShouldContain(payloadField);
        }

        contract.ShouldContain("Story 9.2 remains blocked");
        contract.ShouldContain("Owner:");
        contract.ShouldContain("Date:");
        contract.ShouldContain("Do not use EventStore ResultPayload");
        contract.ShouldContain("AggregateId is insufficient");
    }

    [Fact]
    public void FcNipContractReferences_WhenAuthored_NameEpicNineOwnershipInDocs() {
        string fcTbl = ReadNormalized("_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md");
        string fcCmd = ReadNormalized("_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md");
        string architecture = ReadNormalized("_bmad-output/project-docs/architecture.md");
        string dataGrid = ReadNormalized("docs/reference/components/datagrid.md");

        fcTbl.ShouldContain("Epic 9 / FC-NIP");
        fcTbl.ShouldContain("Story 9.1 confirms the row-identity payload");
        fcTbl.ShouldContain("Story 9.2 wires the producer");

        fcCmd.ShouldContain("Row-level `FcNewItemIndicator` producer wiring is out of scope for FC-CMD v1");
        fcCmd.ShouldContain("Epic 9 / FC-NIP owns");

        architecture.ShouldContain("Fresh-row indicators are not produced from the projection nudge seam");
        architecture.ShouldContain("FC-NIP owns the post-MVP command outcome payload and producer wiring");

        dataGrid.ShouldContain("Automatic row-level producer wiring");
        dataGrid.ShouldContain("Epic 9 / FC-NIP");
        dataGrid.ShouldContain("current projection nudge does not include row identity");
    }

    private static string ReadNormalized(string relative)
        => CollapseWhitespace(File.ReadAllText(Absolute(relative)));

    private static string CollapseWhitespace(string value)
        => Regex.Replace(value, @"\s+", " ");

    private static string Absolute(string relative)
        => Path.Combine(ProjectRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string ProjectRoot() {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent!;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root could not be found.");
    }
}
