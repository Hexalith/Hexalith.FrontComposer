using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Docs;

/// <summary>
/// Story 9.3 governance guard for the FC-NIP base and successor target-identity decisions.
/// Assertions use whitespace-normalized content so benign Markdown reflow cannot weaken the
/// decision contract.
/// <para>
/// Positive assertions run case-sensitively. Shouldly's <c>ShouldContain</c> defaults to
/// <see cref="Case.Insensitive"/>, which would let prose satisfy a pin with the wrong casing and
/// would silently disagree with the case-sensitive Playwright mirror in
/// <c>tests/e2e/specs/fc-nip-row-identity-contract.spec.ts</c>. Negative assertions deliberately
/// keep the case-insensitive default, because there the looser comparison rejects more.
/// </para>
/// <para>
/// Story 9.4 owns retiring the <see cref="ExistingSourceEvidence_WhenReviewed_StillShowsTheHistoricalRowCascade"/>
/// gap-evidence pins: that story replaces the historical row cascade these assertions require to
/// exist, so it must update them in the same change rather than working around them.
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class FcNipRowIdentityProducerContractTests {
    private const string BaseContractPath = "_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md";
    private const string SuccessorContractPath = "_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md";
    private const string FcTblContractPath = "_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md";
    private const string FcCmdContractPath = "_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md";
    private const string StoryNineTwoPath = "_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md";
    private const string IndicatorStateServicePath = "src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs";

    [Fact]
    public void BaseContract_WhenReviewed_PreservesHistoricalAuthorityAndLinksSuccessor() {
        string contract = ReadNormalized(BaseContractPath);

        AssertContainsAll(
            contract,
            "Status: approved base decision; delivery completion rejected 2026-08-11",
            "EventStore command status",
            "Submit result payload",
            "Projection nudge",
            "Projection detail nudge metadata",
            "Pending-command registration metadata",
            "Generated command metadata",
            "Approved Payload Source",
            "FrontComposer-owned pending-command row metadata",
            "Story 9.2 is unblocked",
            "Resolution date:",
            "Approved successor: `fc-nip-command-target-identity-contract-2026-08-12.md`",
            "where target identity or outcome disposition is concerned, the successor is authoritative",
            "must not infer row identity by diffing visible grid rows",
            "marking every row in a lane",
            "treating a projection nudge as row identity",
            "The nudge can refresh a lane, but it carries no row key",
            "FrontComposer deliberately treats metadata as opaque",
            "AggregateId is insufficient",
            "Do not use EventStore ResultPayload",
            "EventStore command status remains a lifecycle/status source by `MessageId`",
            "ViewKey",
            "EntityKey",
            "ProjectionTypeName",
            "MessageId",
            "ExpectedStatusSlot",
            "PriorStatusSlot",
            "CreatedAt",
            "TenantId",
            "UserId",
            "first-wins");
    }

    [Fact]
    public void SuccessorContract_WhenReviewed_PinsProviderSnapshotAndMateriality() {
        string contract = ReadNormalized(SuccessorContractPath);

        AssertContainsAll(
            contract,
            "Status: approved successor decision",
            "explicit command-to-projection declaration",
            "ICommandTargetIdentityProvider<TCommand>",
            "SameAsSource",
            "before invoking asynchronous command dispatch",
            "Only after accepted dispatch",
            "MessageId",
            "ObservedAt");

        AssertTableRows(
            ReadRaw(SuccessorContractPath),
            "## Immutable Target Snapshot",
            ["`ProjectionTypeName`", "Exact projection named by the generated command-target descriptor.", "Required; must resolve to that registered projection."],
            ["`ViewKey`", "Canonical generated view/lane identity selected by the descriptor and, when dynamic, returned through the typed provider then validated against the declared projection.", "Required and non-empty. A route or visible grid is not a view-key source."],
            ["`EntityKey`", "Exact target key returned by the typed provider, or copied from the generated projection key snapshot only in declared `SameAsSource` mode.", "Required and non-empty; EventStore `AggregateId` is not a substitute unless a later projection contract explicitly proves identity."],
            ["`ChangeKind`", "Declaration-fixed or typed-provider value: `Create`, `Update`, `StatusMove`, or `Delete`.", "Required and known. `NoOp` is terminal materiality, not a change kind."],
            ["`PriorStatus`", "Typed-provider value, or copied from the explicit source snapshot for `SameAsSource`.", "Required for `StatusMove`; otherwise optional."],
            ["`ExpectedStatus`", "Typed-provider destination value, or a declaration-fixed destination validated for the target view.", "Required for `StatusMove` and whenever lane eligibility depends on destination status; otherwise optional."],
            ["`TenantId`", "Framework-owned tenant accessor at target resolution.", "Required and non-empty. It is never read from command fields or tool input."],
            ["`UserId`", "Framework-owned user accessor at target resolution.", "Required and non-empty. It is never read from command fields or tool input."],
            ["`CapturedAt`", "FrontComposer `TimeProvider` at successful target resolution.", "Required. It is never supplied by command fields or overwritten by a terminal timestamp."]);

        AssertContainsAll(
            contract,
            "Provider failure, cancellation, missing registration",
            "Unknown identity or materiality always fails closed",
            "There is no ambient-source fallback",
            "There is no best-effort or source-row fallback",
            "server allocates the exact key only after dispatch, FC-NIP suppresses the indicator",
            "typed post-dispatch identity proof",
            "must be available and copied exactly once during target resolution immediately before dispatch",
            "never re-read from or revalidated against a mutable or virtualized row",
            "command dispatch, transport acceptance, and command lifecycle continue under their existing semantics",
            "`LaneKey` carries the canonical target view/lane and becomes `NewItemIndicatorEntry.ViewKey`",
            "`PriorStatus` | `PriorStatusSlot`",
            "`ExpectedStatus` | `ExpectedStatusSlot`",
            "`ObservedAt` | `NewItemIndicatorEntry.CreatedAt`",
            "`CapturedAt` | No historical field",
            "bounded early-observation buffer/replay path",
            "different snapshot is a conflict",
            "including after that indicator is dismissed or expires",
            "Any unlisted outcome suppresses indicators by default",
            "Pre-accept failure, cancellation, timeout, malformed-message, unsupported, and future lifecycle outcomes",
            "approved at the human Story 9.3 `bmad-build` plan checkpoint on 2026-08-12");

        // The declaration authoring surface is the sole legitimate target source, so its definition
        // and its exclusions must both stay pinned. Story 9.4 implements from exactly this.
        AssertContainsAll(
            contract,
            "Declaration Authoring Surface",
            "is an attribute applied to the command type",
            "Neither a DI registration, a configuration entry, a naming convention, nor a runtime call may act as a declaration",
            "are a duplicate registration: resolution fails closed");

        // Scope, precedence, and SameAsSource validity are the three rules Story 9.3 amended in.
        AssertContainsAll(
            contract,
            "Publication requires that the active tenant and user at eligible terminal observation equal the",
            "captured pair; any inequality suppresses FC-NIP publication",
            "A disagreement is not resolved by precedence: it fails closed",
            "A declared `SameAsSource` mode is valid only with `ChangeKind = Update`",
            "`SameAsSource` combined with `Create`, `StatusMove`, or `Delete` is an invalid declaration and fails closed",
            "| `TenantId` | `TenantId` |",
            "| `UserId` | `UserId` |");

        // Terminal materiality: assert the closed set as a phrase, plus each member's meaning.
        // A bare ShouldContain("Material") is satisfied by the heading "Terminal Materiality" alone.
        AssertContainsAll(
            contract,
            "`Material`, `NoOp`, or `Unknown`",
            "`Material` means the typed terminal adapter has affirmative evidence",
            "`NoOp` means the typed adapter has affirmative no-work evidence",
            "`Unknown` means evidence is absent, malformed, unsupported, contradictory",
            "Both `NoOp` and `Unknown` suppress the indicator",
            "Lifecycle text is never parsed to determine materiality");

        // Every forbidden identity/materiality source, pinned in the blocking lane rather than only
        // in the Playwright spec that no workflow executes.
        AssertContainsAll(
            contract,
            "ambient generated source-row placement or an undeclared cascading row context;",
            "command-property names such as `Id`, `EntityId`, `AggregateId`, or `Status`;",
            "current routes, query strings, selected tabs, visible rows, or virtualized-row instances;",
            "visible-row diffs, projection nudges, unrelated refreshes, or broad lane marking;",
            "EventStore `AggregateId` as universal projection `EntityKey`;",
            "opaque or domain-defined result payloads; or",
            "lifecycle/status text.");

        // The seven behavioural rules routed to Story 9.4, and the new preallocation owner.
        AssertContainsAll(
            contract,
            "a bounded provider-resolution deadline",
            "empty or non-ULID `MessageId`",
            "separates a duplicate re-observation from a conflict",
            "canonicalization plus comparison ordinality",
            "maximum `CapturedAt`-to-`ObservedAt` age and a clock-skew rule",
            "capacity, eviction policy, and overflow disposition",
            "invalidation events that discard a captured snapshot before terminal observation",
            "Story 9.9 (new, blocks Story 9.8)");

        // Opt-in migration: the live row-cascade regression and its explicit, adopter-visible
        // handling. An implicit SameAsSource promotion here would reintroduce ambient placement.
        AssertContainsAll(
            contract,
            "FC-NIP is **opt-in per command**",
            "publishes fresh-row indicators **with no declaration of any kind**",
            "No implicit or generated declaration closes that gap",
            "the historical cascade is not silently promoted into a `SameAsSource`",
            "build-time SourceTools diagnostic",
            "migrates this repository's own `[Command]` samples",
            "fresh-row indicators now require a declaration");

        // The three sentences that a later edit could reverse to manufacture completion.
        AssertContainsAll(
            contract,
            "FR-13, FR-26, and Epic 9 remain open through Story 9.8",
            "This records approved semantics, not Story 9.3 completion",
            "Story 9.3 does not add a public runtime API, change EventStore, or implement generated/runtime behavior");

        // Bind the contract's ten-second prose to the constant that actually implements it.
        contract.ShouldContain("ten-second TTL", Case.Sensitive);
        ReadNormalized(IndicatorStateServicePath)
            .ShouldContain("DefaultLifetime = TimeSpan.FromSeconds(10)", Case.Sensitive);
    }

    [Fact]
    public void SuccessorContract_WhenMatrixReviewed_PinsAllEightDispositions() {
        AssertTableRows(
            ReadRaw(SuccessorContractPath),
            "## Complete Outcome Disposition Matrix",
            ["Standalone create", "Typed provider resolves a valid `Create` snapshot before dispatch.", "Confirmed + `Material`.", "Publish only for the declared target view and entity. Missing or unknown target suppresses."],
            ["Same-row update", "Descriptor explicitly selects `SameAsSource`; the named pre-dispatch source snapshot is copied as an `Update` target.", "Confirmed + `Material`.", "Publish for that copied target. Never fall back to ambient source-row placement."],
            ["Cross-row update", "Typed provider resolves an `Update` target whose `EntityKey` may differ from the source.", "Confirmed + `Material`.", "Publish only for the provider-resolved target. Undeclared source reuse is invalid and suppresses."],
            ["Status move", "Typed provider resolves the target, `PriorStatus`, destination `ExpectedStatus`, and destination `ViewKey`.", "Confirmed + `Material`.", "Publish only in the destination lane and preserve both statuses. Missing destination status suppresses."],
            ["Delete", "Typed provider resolves a valid `Delete` target.", "Confirmed + `Material`.", "Preserve target metadata for lifecycle/audit; never publish a fresh-row indicator."],
            ["Idempotent confirmation", "A valid non-delete target was captured before dispatch.", "`IdempotentConfirmed` + `Material`.", "Apply the same eligibility and existing ten-second TTL disposition as material confirmation; duplicate observation handling does not extend TTL. `NoOp` or `Unknown` suppresses."],
            ["Rejected / needs review", "Any valid or invalid declared target.", "`Rejected` or `NeedsReview`.", "Never publish an indicator; preserve the lifecycle state."],
            ["No-op", "Any declared target.", "Typed `NoOp`, including `EventCount == 0`, or `Unknown`.", "Never publish an indicator. Status text and opaque payloads cannot upgrade it to `Material`."]);
    }

    [Fact]
    public void SynchronizedTruth_WhenReviewed_ResolvesDecisionAndKeepsCompositionOpen() {
        string prd = ReadNormalized("_bmad-output/planning-artifacts/prd.md");
        string planningArchitecture = ReadNormalized("_bmad-output/planning-artifacts/architecture.md");
        string publishedArchitecture = ReadNormalized("_bmad-output/project-docs/architecture.md");
        string dataGrid = ReadNormalized("docs/reference/components/datagrid.md");
        string fcTbl = ReadNormalized(FcTblContractPath);
        string fcCmd = ReadNormalized(FcCmdContractPath);

        AssertContainsAll(
            prd,
            "Resolved 2026-08-12",
            SuccessorContractPath,
            "D-4 is resolved; Stories 9.4-9.8 still block FR-13/FR-26 completion and Epic 9 closure");

        foreach (string architecture in new[] { planningArchitecture, publishedArchitecture }) {
            AssertContainsAll(
                architecture,
                SuccessorContractPath,
                "ICommandTargetIdentityProvider<TCommand>",
                "SameAsSource",
                "CapturedAt",
                "ObservedAt",
                "`Material`, `NoOp`, or `Unknown`");
        }

        // The published DocFX site must not leak internal planning paths and must describe the
        // explicit target producer boundary now shipped by Story 9.4.
        AssertContainsAll(
            dataGrid,
            "producer wiring uses an explicit command-to-projection `[CommandTarget]`",
            "`SameAsSource` is valid only for `Update`",
            "Only a confirmed or idempotent-confirmed `Material` terminal observation",
            "ambient row placement are never target");
        dataGrid.ShouldNotContain("_bmad-output");
        dataGrid.ShouldContain("ICommandTargetIdentityProvider<TCommand>");

        // Sibling FC-TBL / FC-CMD ownership wording must track the successor decision. These pins
        // were dropped when the pre-remediation guard was deleted; both documents went stale.
        foreach (string sibling in new[] { fcTbl, fcCmd }) {
            AssertContainsAll(
                sibling,
                "Epic 9 / FC-NIP",
                "fc-nip-command-target-identity-contract-2026-08-12.md",
                "Stories 9.4-9.8 own implementation and composed/live acceptance");
            sibling.ShouldNotContain("Story 9.1 confirms");
            sibling.ShouldNotContain("Story 9.2 wires");
        }

        fcCmd.ShouldContain("Row-level `FcNewItemIndicator` producer wiring is out of scope for FC-CMD v1", Case.Sensitive);
    }

    [Fact]
    public void ExistingSourceEvidence_WhenReviewed_ShowsTheConvergedProducerBoundary() {
        string rowIdentity = ReadNormalized("src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs");
        string eventStoreStatusQuery = ReadNormalized("src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs");
        string commandFormEmitter = ReadNormalized("src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs");
        string razorEmitter = ReadNormalized("src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs");
        string storyNineTwo = ReadNormalized(StoryNineTwoPath);

        AssertContainsAll(
            rowIdentity,
            "projection row identity cascaded to generated command forms",
            "It must not be populated from raw",
            "command payloads or user-editable form values");
        AssertContainsAll(
            eventStoreStatusQuery,
            "MessageId: pendingCommand.MessageId",
            "string? AggregateId",
            "int? EventCount");
        eventStoreStatusQuery.ShouldNotContain("EntityKey:");
        eventStoreStatusQuery.ShouldNotContain("ProjectionTypeName:");
        eventStoreStatusQuery.ShouldNotContain("LaneKey:");
        eventStoreStatusQuery.ShouldNotContain("ExpectedStatusSlot:");
        eventStoreStatusQuery.ShouldNotContain("PriorStatusSlot:");

        // The form emitter may read the row cascade only inside an explicit SameAsSource branch;
        // all terminal mutation and accepted association go through the resolver.
        AssertContainsAll(
            commandFormEmitter,
            "CascadingParameter",
            "CommandTypeName: typeof(",
            "form.CommandTarget?.ResolutionMode == CommandTargetResolutionMode.SameAsSource",
            "ResolveCommandTargetAsync(_model, cts.Token)",
            "var commandForDispatch = targetResolution.Command",
            "PendingCommandOutcomeResolver.AssociateAccepted",
            "PendingCommandOutcomeResolver.Resolve");
        commandFormEmitter.ShouldNotContain("PendingCommandState.ResolveTerminal");
        commandFormEmitter.ShouldNotContain("PendingCommandState.Register");
        commandFormEmitter.ShouldNotContain("EntityKey: status.AggregateId");
        commandFormEmitter.ShouldNotContain("ResultPayload");
        AssertContainsAll(
            razorEmitter,
            "PendingCommandRowIdentityFor(row)",
            "CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>");

        // Story 9.2's delivery record stays pinned: its no-smuggling prohibition is the reason the
        // emitter assertions above are worth making.
        AssertContainsAll(
            storyNineTwo,
            "Status: done",
            "FrontComposer-owned pending-command row metadata",
            "Source-level wiring was proven",
            "Do not hide FC-NIP row identity in optional EventStore/domain-defined `ResultPayload`");
    }

    private static void AssertContainsAll(string document, params string[] expectedFragments) {
        foreach (string fragment in expectedFragments) {
            document.ShouldContain(fragment, Case.Sensitive);
        }
    }

    private static void AssertTableRows(string document, string heading, params string[][] expectedRows) {
        string[][] actualRows = ParseTableRows(document, heading);
        actualRows.Length.ShouldBe(expectedRows.Length);
        for (int rowIndex = 0; rowIndex < expectedRows.Length; rowIndex++) {
            actualRows[rowIndex].Length.ShouldBe(expectedRows[rowIndex].Length);
            for (int columnIndex = 0; columnIndex < expectedRows[rowIndex].Length; columnIndex++) {
                actualRows[rowIndex][columnIndex].ShouldBe(expectedRows[rowIndex][columnIndex]);
            }
        }
    }

    /// <summary>
    /// Parses the first Markdown table that follows <paramref name="heading"/>, bounded to that
    /// heading's own section. The scan stops at the next Markdown heading so a deleted table fails
    /// instead of silently binding to a later section's table, and the separator row is verified
    /// rather than assumed.
    /// </summary>
    private static string[][] ParseTableRows(string document, string heading) {
        string[] lines = document.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int headingIndex = Array.FindIndex(lines, line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));
        headingIndex.ShouldBeGreaterThanOrEqualTo(0, $"'{heading}' heading is missing.");

        int sectionEnd = Array.FindIndex(lines, headingIndex + 1, line => line.TrimStart().StartsWith('#'));
        if (sectionEnd < 0) {
            sectionEnd = lines.Length;
        }

        int headerIndex = Array.FindIndex(lines, headingIndex + 1, sectionEnd - headingIndex - 1, line => line.TrimStart().StartsWith('|'));
        headerIndex.ShouldBeGreaterThanOrEqualTo(0, $"'{heading}' section contains no table.");
        (headerIndex + 2).ShouldBeLessThanOrEqualTo(sectionEnd, $"'{heading}' table is truncated.");
        Regex.IsMatch(lines[headerIndex + 1].Trim(), @"^\|[\s:|-]+\|$")
            .ShouldBeTrue($"'{heading}' table is missing its separator row.");

        List<string[]> rows = [];
        for (int index = headerIndex + 2; index < sectionEnd && lines[index].TrimStart().StartsWith('|'); index++) {
            rows.Add(StripEdgePipes(lines[index].Trim()).Split('|').Select(static cell => cell.Trim()).ToArray());
        }

        return [.. rows];
    }

    /// <summary>
    /// Strips exactly one leading and one trailing pipe, matching the Playwright mirror. Trimming
    /// every edge pipe would silently drop an empty first or last cell and make the two guards
    /// disagree on column counts.
    /// </summary>
    private static string StripEdgePipes(string line)
        => Regex.Replace(line, @"^\||\|$", string.Empty);

    private static string ReadNormalized(string relative)
        => CollapseWhitespace(File.ReadAllText(Absolute(relative)));

    private static string ReadRaw(string relative)
        => File.ReadAllText(Absolute(relative));

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
