# Validation report — Hexalith.FrontComposer

**Fair** · 0 critical · 5 high · 7 medium · 1 low

FrontComposer main `aa016201c47e0cf226aed1418acb0f6c729fb0dc` · PRD dated 2026-11-16 with `prd-addendum-2026-09-08.md`.

PRD `_bmad-output/planning-artifacts/prd.md`\
Rubric `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`\
Reviewed 2026-11-20T11:48:24+00:00

## Overall verdict

The PRD gives teams testable operator journeys, clear evidence for adopter success, and an explicit decision to keep the readiness milestone open. Its approval table, public package contract, and CLI scope need reconciliation before Product re-approval or use as instructions for new stories. The most consequential discrepancies concern the release hold, the publisher that must qualify, and the framework consumers must target.

The November 19 receipt records HEXALITH_RELEASE_PUBLISH_ENABLED as false, stopping publication, and the architecture amendment places the protected publisher in FrontComposer. The release gate still requires owner acceptance. The README and migration plan also retain the local CLI and require the MCP host to retire with its Projects plug-in. Those distinctions affect which teams should act next.

## Dimension verdicts

- **Decision-readiness — thin.** Section 0.1 names the evidence, owner and decision for each of eight gates. G-5, G-6 and G-7 cite dated, bounded acceptance. G-1, G-2, G-3, G-4 and G-8 remain open. A Release Owner reading the opening summary would nevertheless start incident response from an obsolete observation. The November 19 architecture decision also changes which component Architecture and Release must accept.

- **Substance over theater — strong.** FR-4 defines exact field-count thresholds for three generated form densities. FR-15 and SM-10 distinguish acknowledgement from a confirmed outcome and bound the wait for confirmation. Addendum §§4.1–4.3 give observable examples of focus recovery and announcements. The roles in §2 correspond to real work, including release evidence and adopter testing.

- **Strategic coherence — strong.** The “describe their operational surface once” thesis in §1 links Nina's three-call bootstrap in UJ-1, Marc's row-freshness decision in UJ-2, and the named Tenants adoption proof in SM-1. SM-7 identifies a meaningful operator outcome. SM-C4 and SM-C5 prevent a green workflow or a ledger entry alone from counting as release success.

- **Done-ness clarity — adequate.** The consequences under the FRs make most behavioral requirements independently testable. Two statements would produce incorrect acceptance tests for supported inputs or a failure partway through a migration.

- **Scope honesty — thin.** Sections 8.3 and 13 distinguish deferred UX work, assumptions and approved scope. The public tooling boundary creates a consequential exception. The named local CLI exemption is documented in both the root-pinned AI.Tools revision and its refreshed main checkout.

- **Downstream usability — thin.** Frozen IDs, the §3 glossary and the explicit supersession of legacy epic IDs make individual requirements easy to extract. Some sections that identify themselves as current give implementers an outdated SDK, consumer target or compatibility baseline.

- **Shape fit — adequate.** A capability specification with named journeys, per-feature acceptance and an evidence register fits a brownfield framework serving both operators and downstream developers. The latest G-3 rule appears after the assumptions index, which makes a correct operational instruction too easy to miss when extracting the authoritative table alone.

## Findings by severity

### Critical (0)

None.

### High (5)

**F01 · Correct the recorded state of the release stop**

The PRD says “the halt is currently not in effect,” citing a November 16 observation of true. The November 19 receipt records literal false at 09:36:24 UTC and a second verification. Treating the earlier observation as current sends the Release Owner back to an action already evidenced.

**Suggested change.** Date both observations, cite the November 19 receipt as the latest recorded state and retain the earlier incidents in NC-35/36. Keep G-8 open pending conformance evidence and owner acceptance.

**Evidence.** PRD §0 line 21 and §0.1 G-8 · `docs/release-trusted-publishing.md:26–37` · GOV-1 nonconformance register, NC-35. Addendum §1 line 18.

**F02 · Qualify the publisher selected on November 19**

G-2 still requires an owner-accepted Builds split reusable workflow. The architecture amendment selects a protected workflow owned by FrontComposer that runs pinned Builds actions. The current gate would qualify the wrong artifact and leave the selected workflow’s identity unaccounted for.

**Suggested change.** Reconcile G-2, FR-24, the decision register and addendum around the selected workflow, signer, and Builds action revisions. Preserve the separate requirements for secretless building, authenticated evidence and formal owner acceptance.

**Evidence.** PRD §0.1 G-2, §5.7 FR-24, §12 D-11 · addendum §§1–2 · `docs/release-trusted-publishing.md:11–24` · GOV-1 architecture spine, November 19 amendment, lines 103–153.

**F03 · Publish the correct consumer framework requirements**

NFR-1 specifies SDK 10.0.400. The package table promises net10.0 for Shell and Testing. The 6.0 consumer migration requires .NET 11 and the repository pins SDK 11.0.100. An adopter following the table can select a framework that cannot load the runtime packages.

**Suggested change.** Refresh each package target and the SDK requirement from its project file. Identify the retained netstandard2.0 assets and link the 5.0-to-6.0 migration instructions.

**Evidence.** PRD §6 NFR-1/2, §7, §11 · `global.json` · `docs/migrations/5.0-to-6.0.md:24–26` · Shell and Testing project files.

**F04 · Retain inspect and migrate as supported local developer tools**

FR-20 and FR-21 make a generic McpCli replacement or withdrawal a prerequisite for retiring the FrontComposer CLI. The merged migration plan explicitly retains both commands under its local compiler exemption. A team following the PRD could schedule a replacement that the plan excludes.

**Suggested change.** Record the exemption in the vision, FRs and package table. Preserve local build, freshness, write-safety and privacy behavior and keep the commands out of agent-facing transports.

**Evidence.** PRD opening, §5.6 FR-20/21, §11 · `README.md:30` · `references/Hexalith.AI.Tools/hexalith-mcpcli-migration-instructions.md:33`.

**F05 · Require coordinated retirement of the MCP host and Projects plug-in**

The PRD permits removal after FrontComposer capabilities have replacement or withdrawal evidence. The approved plan couples the host to the Projects plug-in. A host-only cutover could strand a consumer.

**Suggested change.** Add the complete consumer inventory, coordinated cutover and live, release and rollback evidence to the host retirement condition.

**Evidence.** PRD §5.5 line 372 and §11 line 673 · `README.md:31` · AI.Tools McpCli migration instructions, lines 117–125.

### Medium (7)

**F06 · Specify the unit of atomicity for migration writes**

“Apply mode is atomic” can be read as a promise to roll back the entire run. Each file is replaced atomically. Earlier writes survive a later refusal, and cancellation can interrupt the report.

**Suggested change.** Describe partial application, path and hash revalidation, and the exit/report outcomes. Use a two-file case in which the second file fails after the first succeeds.

**Evidence.** PRD §5.6 FR-21 line 446 · `docs/reference/cli.md:54–59` · `src/Hexalith.FrontComposer.Cli/MigrationApplier.cs:28–46`.

**F07 · Require real compiler output in packaged inspection acceptance**

Formatting and filtering checks can pass when inspect returns an empty result. The November 20 audit found this failure on the Counter sample with the installed CLI even though the fixture passed.

**Suggested change.** Require a known command and projection from the packaged tool. Cover compiler and flat layouts, exclude unrelated generators and cite the existing regression evidence.

**Evidence.** PRD §5.6 FR-20 lines 433–438 · November 20 sprint change proposal, lines 19 and 41 · `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs:213`.

**F08 · Update the API compatibility baseline**

OI-11 instructs Release to advance the comparison target from 4.4.0 to 4.5.0. The implemented published-package baseline is already 5.0.0. Turning the open item into a story could move the baseline backward. The ledger records the November 1 owner decision closing REL-BASE-1 and carries a bounded residual.

**Suggested change.** Replace the 4.4.0-to-4.5.0 instruction with the November 1 disposition and actual baseline. Identify the remaining precursor-acceptance work separately from the open G-1 attempt-ledger backfill.

**Evidence.** PRD §5.7 FR-25 and §12.2 OI-11 · `eng/release_compatibility.py:17`. Release evidence ledger, lines 1086–1105 and 1429–1446.

**F09 · Put the approved G-3 evidence location in the gate table**

A maintainer extracting the gate table could place the receipt on main, advancing it past the tested commit. The November 16 amendment requires a separately authorized record ref in the same repository and keeps main at the tested candidate.

**Suggested change.** State the record-ref rule in G-3, link D-12 and FR-29.7 to the amendment, and retain the outstanding qualification and owner decisions.

**Evidence.** PRD §0.1 G-3 line 35, §12 D-12 and November 16 amendment, lines 775–785.

**F11 · Record why the most recent G-3 attempt stopped**

The appendix refers to a future candidate T. Story 16.3 records a later attempted candidate, ffb75718, whose preflight ended when the pinned CI configuration did not match. Scheduling that attempt again would bypass the outstanding CiCd decision.

**Suggested change.** Link the cancelled slot and its owner disposition from G-3. Distinguish a future qualification from the November 20 dependency seal and the ended attempt.

**Evidence.** PRD §0.1 G-3, November 16 appendix · Story 16.3 spec, lines 1112–1125 · `eng/dependency-governance.md:3–21`.

**F12 · Distinguish completed governance work from open G-8 approvals**

The gate text leaves all implementation and evidence slices open. The tracker marks GOV-SRC-1 done. Story 17.2 records a tabletop, approval of the revised runbook digest and prospective response targets, and GOV-I in review.

**Suggested change.** Credit the completed source check and runbook decision. Keep remaining review, preservation, live incident-readiness and conformance decisions explicit.

**Evidence.** PRD §0.1 G-2/G-8, D-16 · sprint status, lines 1279–1286 · Story 17.2 spec, lines 145–151 · `docs/release-incident-response.md:41–44`.

**F13 · Replace the obsolete Fluent documentation task**

OI-10 describes an rc.2-versus-rc.5 mismatch. The contingency page now names Components and Icons 5.1.0 GA. Story 15.4 is marked done while its DOC-C alias still says proposed.

**Suggested change.** Record the named catalog revision and completed documentation work. Identify the outstanding Product posture or approval decision separately.

**Evidence.** PRD FR-23 line 472 and §12.2 OI-10 line 738 · `docs/fluent-ui-v5-contingency.md:18–40` · sprint status, lines 236 and 1266.

### Low (1)

**F10 · Clarify which command constructors are accepted**

FR-2 says a missing public parameterless constructor triggers HFC1009. The parser also accepts a public constructor when every argument has an explicit default.

**Suggested change.** Describe zero-argument construction and include an all-default constructor that passes alongside one required-argument constructor that fails.

**Evidence.** PRD §5.1 FR-2 line 202 · `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:474–494`.

## Mechanical notes

- The numbered definitions cover FR-1 through FR-30 plus FR-19a, NFR-1 through NFR-13, UJ-1 through UJ-6, and SM-1 through SM-10 plus SM-2a. FR-27 and FR-28 are retained as retired. SM-C1 and SM-C3 have explicit retirement notes.
- All six journeys name a protagonist. Module and Bounded Context have an explicit operator/developer mapping in §3.
- A3 is the sole active inline assumption and maps to §13 and OI-9. A1 and A2 have retirement dispositions. The register has 19 OI rows, five marked closed and one conditional transfer entry. The November 16 G-3 amendment records that the transfer was not invoked for the earlier attempt.
- Relative Markdown file links and heading fragments checked in both reviewed documents resolved against the checkout. Each document's updated field reads November 16 although both contain a November 19 credential-design amendment.
- The PRD records G-1, G-2, G-3, G-4 and G-8 as open. G-5, G-6 and G-7 have dated decisions bounded to their evidence. Product re-approval remains pending.
- Review scope was the checked-in requirements and corroborating evidence. The November 19 release-stop state comes from the tracked readback. The November 20 checkout has eight direct submodule revisions ahead of the parent gitlinks. Their branch tips cannot establish qualification of a sealed release candidate.
- The reviewed Git blobs (LF) have SHA-256 digests 5523074e2c12e111e35f92c83505bda467fafe063585de3e2c93372c81260e4a for the PRD and 4103b551bfa1747e3385addfcbf8ce4af6009f15dcae5b9bc4a0d8d78a6c723b for the addendum.

## Review files

- [review-rubric.md](review-rubric.md)
- [review-source-consistency.md](review-source-consistency.md)
- [review-transport-ownership.md](review-transport-ownership.md)
