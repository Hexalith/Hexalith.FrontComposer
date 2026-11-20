# PRD Quality Review — Hexalith.FrontComposer

## Overall verdict

The PRD gives teams testable operator journeys, clear evidence for adopter success, and an explicit decision to keep the readiness milestone open. Its approval table, public package contract, and CLI scope need reconciliation before Product re-approval or use as instructions for new stories. The most consequential discrepancies concern the release hold, the publisher that must qualify, and the framework consumers must target.

## Decision-readiness — thin

Section 0.1 names the evidence, owner and decision for each of eight gates. G-5, G-6 and G-7 cite dated, bounded acceptance. G-1, G-2, G-3, G-4 and G-8 remain open. A Release Owner reading the opening summary would nevertheless start incident response from an obsolete observation. The November 19 architecture decision also changes which component Architecture and Release must accept.

### Findings

- **[high]** The summary misstates the latest recorded release stop (§0, PRD line 21 and §0.1 G-8) — “the halt is currently not in effect” projects the November 16 observation forward. The NC-35 entry in the GOV-1 nonconformance register, lines 85–100, records an authenticated literal-false readback on November 19 at 09:36:24 UTC and a second verification. It keeps G-8, incident handling and re-enable decisions open. *Fix:* Cite both observations with dates, identify November 19 as the latest recorded stop observation, and retain the historical incidents and open approvals. A present-tense assertion would require a fresh readback.
- **[high]** G-2 describes the superseded publisher topology as a pass condition (§0.1 G-2, §5.7 FR-24, §12 D-11, addendum §§1–2) — G-2 requires a “future owner-accepted split-reusable revision.” Section 4 and the November 19 amendment to the GOV-1 architecture spine, lines 103–153, select a FrontComposer-owned protected workflow executing owner-accepted Builds actions. The workflow signer and action revision have distinct identities. The addendum's identity register still asks owners to qualify the prior reusable. *Fix:* Update the gate, identity register and dependent requirement to identify the FrontComposer workflow blobs and signer, the accepted Builds action SHA, and the proof and approvals required for that topology.

## Substance over theater — strong

FR-4 defines exact field-count thresholds for three generated form densities. FR-15 and SM-10 distinguish acknowledgement from a confirmed outcome and bound the wait for confirmation. Addendum §§4.1–4.3 give observable examples of focus recovery and announcements. The roles in §2 correspond to real work, including release evidence and adopter testing.

## Strategic coherence — strong

The “describe their operational surface once” thesis in §1 links Nina's three-call bootstrap in UJ-1, Marc's row-freshness decision in UJ-2, and the named Tenants adoption proof in SM-1. SM-7 identifies a meaningful operator outcome. SM-C4 and SM-C5 prevent a green workflow or a ledger entry alone from counting as release success.

## Done-ness clarity — adequate

The consequences under the FRs make most behavioral requirements independently testable. Two statements would produce incorrect acceptance tests for supported inputs or a failure partway through a migration.

### Findings

- **[medium]** Migration atomicity lacks its unit (§5.6 FR-21, PRD line 446) — “Apply mode is atomic” can be read as an all-or-nothing plan. The migration guide, lines 65–69, and MigrationCommandTests.cs, line 1623, explicitly preserve earlier successful writes after a later refusal. This matters to an adopter deciding how to recover. *Fix:* Specify atomic replacement per file, the status of earlier writes on failure or cancellation, and how the completed report represents partial application.
- **[low]** The command-constructor rejection rule is too broad (§5.1 FR-2, PRD line 202) — “no public parameterless constructor fails with HFC1009” would reject a public constructor whose parameters all have explicit defaults. CommandParser.cs, lines 474–494, accepts this case because the generated form can construct the command with zero arguments. *Fix:* State the zero-argument construction rule and give one accepted all-default constructor alongside one rejected required-argument constructor.

## Scope honesty — thin

Sections 8.3 and 13 distinguish deferred UX work, assumptions and approved scope. The public tooling boundary creates a consequential exception. The named local CLI exemption is documented in both the root-pinned AI.Tools revision and its refreshed main checkout.

### Findings

- **[high]** The PRD routes supported local compiler tools toward retirement (opening course correction, §5.6 FR-20/21 and §11 CLI row) — The opening calls inspect and migrate “obsolete migration sources.” AI.Tools hexalith-mcpcli-migration-instructions.md, named local-tooling exemptions, explicitly retains both as local developer tools. It reserves the proprietary MCP surface for its separate migration and owner decisions. Following the PRD would create replacement or withdrawal work for exempt commands. *Fix:* Carry the local exemption and its safety and output contracts through FR-20, FR-21 and the package table. Keep agent-facing MCP migration and any inventory amendment separately owned.

## Downstream usability — thin

Frozen IDs, the §3 glossary and the explicit supersession of legacy epic IDs make individual requirements easy to extract. Some sections that identify themselves as current give implementers an outdated SDK, consumer target or compatibility baseline.

### Findings

- **[high]** The package table promises the wrong consumer runtime (§6 NFR-1/2, §7 and §11) — NFR-1 pins SDK “10.0.400” and §11 lists net10.0 for Shell and Testing. At FrontComposer aa016201, global.json selects 11.0.100, the project files target net11.0 for both libraries, and docs/migrations/5.0-to-6.0.md documents the breaking consumer upgrade. A net10.0 adopter cannot reference these net11.0 assets. *Fix:* Reconcile the SDK and every TFM against the selected project files. Retain the netstandard2.0 faces of Contracts, Schema and SourceTools and point adopters to the 6.0 migration guide.
- **[medium]** The open ApiCompat task could lower the implemented baseline (§5.7 FR-25 and §12.2 OI-11) — The register directs Release to advance “from 4.4.0” to “currently v4.5.0,” while eng/release_compatibility.py, line 17, already sets PUBLISHED_BASELINE_VERSION to 5.0.0. Story extraction could reintroduce the earlier comparison target. *Fix:* Record the actual constant and the dated evidence or owner decision for the next target. Keep any remaining lag explicit.

## Shape fit — adequate

A capability specification with named journeys, per-feature acceptance and an evidence register fits a brownfield framework serving both operators and downstream developers. The latest G-3 rule appears after the assumptions index, which makes a correct operational instruction too easy to miss when extracting the authoritative table alone.

### Findings

- **[medium]** The G-3 row omits the adopted location for future candidate evidence (§0.1 G-3, §12 D-12 and the November 16 amendment after §13) — The row is labelled the “only authority” yet speaks of a story record and an unresolved lineage decision. The approved amendment requires a separately authorized same-repository record ref while main remains at the tested candidate. A maintainer following just the gate can put a receipt on main and move it off the tested commit. *Fix:* Put the record-ref rule and remaining execution prerequisites in G-3, link to the amendment from D-12 and FR-29.7, and label the prior candidate-specific history. Keep G-3 open.

## Mechanical notes

- The numbered definitions cover FR-1 through FR-30 plus FR-19a, NFR-1 through NFR-13, UJ-1 through UJ-6, and SM-1 through SM-10 plus SM-2a. FR-27 and FR-28 are retained as retired. SM-C1 and SM-C3 have explicit retirement notes.
- All six journeys name a protagonist. Module and Bounded Context have an explicit operator/developer mapping in §3.
- A3 is the sole active inline assumption and maps to §13 and OI-9. A1 and A2 have retirement dispositions. The register has 19 OI rows, five marked closed and one conditional transfer entry. The November 16 G-3 amendment records that the transfer was not invoked for the earlier attempt.
- Relative Markdown file links and heading fragments checked in both reviewed documents resolved against the checkout. Each document's updated field reads November 16 although both contain a November 19 credential-design amendment.
