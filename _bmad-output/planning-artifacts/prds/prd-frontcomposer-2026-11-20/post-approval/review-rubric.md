# PRD Quality Review — Hexalith.FrontComposer

## Overall verdict

The revised PRD gives Product an actionable gate register and gives adopters testable contracts for the shell, packages and compiler tools. Two details still affect planning and acceptance: OI-19 obscures documentation work already delivered, and the projection recovery metric promises fallback polling even when the adopter has disabled it. G-4 and the readiness milestone remain open on their own evidence and approval requirements.

## Decision-readiness — adequate

Section 0.1 distinguishes the release hold, the selected publication design, and the approvals needed to use it. For G-3 it identifies who stopped the previous attempt, why it stopped, and what a future candidate requires. G-4 identifies three remaining prerequisites. One of those rows gives maintainers too little information to decide whether to schedule implementation, check the existing evidence, or obtain a closure decision.

### Findings

- **[medium]** OI-19 omits the evidence for work already delivered (§5.6 FR-23 and §12.2 OI-19, PRD line 744) — The row classifies DOC-A and DOC-B as implementation and gives their state only as “Open.” Stories 15.2 and 15.3 are done in sprint-status.yaml lines 234–235. Story 15.2 records current component/source parity checks, including rejection of unclassified components, and Story 15.3 records executable versus manual migration-guide parity. E15R-AI-6 explicitly assigns reconciliation of the stale aliases. An owner cannot tell from the row which additional result or disposition G-4 still needs. *Fix:* Cite the two completion records and their tested scope, then identify any outstanding coverage, current-candidate evidence or owner disposition for OI-19. Preserve the open G-4 decision pending its prerequisites.

## Substance over theater — strong

The adopter, operator and release roles each drive a distinct decision. FR-4 changes the generated form according to the count of editable inputs, and FR-13 permits fresh-row indicators only when a command can identify a materially changed row. Those observable consequences give the framework and its user journeys a concrete purpose.

## Strategic coherence — strong

The promise in §1 that a domain team can describe its operational surface once is tested through UJ-1 and the named Tenants bootstrap proof in SM-1. UJ-3 and SM-10 extend the same thesis to operators by distinguishing a command acknowledgement from confirmation in the projection. SM-C4 and SM-C5 protect the release decision from two misleading signals, a successful workflow and an unverified ledger entry.

## Done-ness clarity — adequate

FR-2 describes the zero-argument command construction contract and FR-21 tells adopters exactly what persists after a partially successful migration. Projection and storage requirements also supply concrete numbers, but their acceptance conditions blur defaults and supported overrides.

### Findings

- **[medium]** Recovery lacks an acceptance case for disabled polling (§5.3 FR-12, §6 NFR-8 and §9 SM-8, PRD lines 317, 565 and 629) — SM-8 requires “FallbackPolling within one polling interval” and FR-12 promises a restart after closure. FcShellOptions.cs lines 237–241 explicitly permit interval zero. The cited Story 11.2 acceptance criteria distinguish that case: after a terminal hub close the shell records disconnection and avoids the automatic restart loop. ProjectionSubscriptionService.cs line 693 also checks for a positive interval. A valid adopter configuration would fail the metric as written. *Fix:* Scope the polling and closed-restart bounds to enabled polling. Give interval zero its own observable disconnection and no-poll acceptance case.
- **[low]** The storage default reads as a fixed ceiling (§5.2 FR-9 and §6 NFR-9, PRD lines 275 and 566) — “at most LocalStorageMaxEntries=500” presents 500 as universal. FcShellOptions.cs lines 138–139 set a default of 500 and a supported range of 50 to 10,000, and LocalStorageService.cs line 210 uses the configured value. *Fix:* Identify 500 as the default and specify eviction against the configured cap within the supported range.

## Scope honesty — strong

The opening and §§5.5–5.6 identify the supported local compiler commands, the maintained compatibility host, and the conditions for retiring the host with its consumer plug-in. Section 8.3 explicitly excludes fresh-row markers for server-allocated targets without the required identity proof. The limited local container use remains a disclosed A3 assumption assigned to OI-9. None of those boundaries silently expands milestone scope.

## Downstream usability — adequate

The status map preserves historical decisions beside the requirements they constrain. Named journeys, stable identifiers and relative source links support extraction into stories and tests. One glossary entry uses attractive UI descriptions where an adopter needs actual attribute values.

### Findings

- **[low]** The ProjectionRole examples are not all usable attribute values (§3 Glossary, PRD line 105) — The entry defines an attribute value with “for example list, timeline, card.” ProjectionRole.cs lines 7–21 defines ActionQueue, StatusOverview, DetailRecord, Timeline and Dashboard. An adopter copying List or Card into an annotation would get a compiler error. *Fix:* Use the declared enum names as examples and describe each resulting UI shape separately where useful.

## Shape fit — strong

A brownfield developer framework needs its public package surface and compatibility promises alongside operator journeys. Sections 2.4, 5.0 and 11 serve those different readers. The addendum houses the detailed release identities, interaction contracts and CLI recovery behavior, and §0.1 keeps the current milestone decision accessible without requiring a reader to reconstruct it from historical amendments.

## Mechanical notes

- Reviewed the working-tree PRD with SHA-256 656a1f6777d16f0af2a038071d24b47843cfe14a7de694d865d7022c436b51d6 and addendum with SHA-256 9604e2c70f1d4b63150b7fec7771ad2acebe8ca051b509a3b76d880aa00a0927 over FrontComposer aa016201c47e0cf226aed1418acb0f6c729fb0dc.
- FR-1 through FR-30, including the additional FR-19a, remain identifiable. FR-27 and FR-28 retain their retired status. The 13 NFRs, six named journeys and SM-1 through SM-10 plus SM-2a remain traceable. SM-C1 and SM-C3 have recorded retirement dispositions.
- A3 maps from the inline assumption to §13 and OI-9. A1 and A2 retain their retirement dispositions. G-1, G-2, G-3, G-4 and G-8 are open. G-5, G-6 and G-7 cite dated, bounded acceptances.
- All 59 relative Markdown link occurrences in the PRD and 18 in the addendum resolve to files and, where given, headings in the reviewed tree.
