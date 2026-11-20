# Validation report — Hexalith.FrontComposer

**Excellent** · 0 critical · 0 high · 4 medium · 2 low

Working-tree PRD and addendum · FrontComposer main aa016201 · 2026-11-20T15:02:43+00:00

PRD `_bmad-output/planning-artifacts/prd.md`\
Rubric `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`

## Overall verdict

The revised PRD gives Product an actionable gate register and gives adopters testable contracts for the shell, packages and compiler tools. Two details still affect planning and acceptance: OI-19 obscures documentation work already delivered, and the projection recovery metric promises fallback polling even when the adopter has disabled it. G-4 and the readiness milestone remain open on their own evidence and approval requirements.

Source checks corroborated the OI-19 finding and identified two further discrepancies. The PRD still queues a validator repair completed by Story 15.1, and the addendum describes two planned release-policy inputs alongside fields already enforced. The dated release-stop receipt, runbook approval and stopped G-3 preflight agree with their source records.

Your November 20 approval receipt matches both reviewed files. Formal D-9 re-approval and G-4 still depend on OI-4 (Fluent 5.1.0), OI-16 (UX and accessibility) and OI-19 (documentation parity). G-1, G-2, G-3 and G-8 also remain open.

## Dimension verdicts

- **Decision-readiness — adequate.** Section 0.1 distinguishes the release hold, the selected publication design, and the approvals needed to use it. For G-3 it identifies who stopped the previous attempt, why it stopped, and what a future candidate requires. G-4 identifies three remaining prerequisites. One of those rows gives maintainers too little information to decide whether to schedule implementation, check the existing evidence, or obtain a closure decision.

- **Substance over theater — strong.** The adopter, operator and release roles each drive a distinct decision. FR-4 changes the generated form according to the count of editable inputs, and FR-13 permits fresh-row indicators only when a command can identify a materially changed row. Those observable consequences give the framework and its user journeys a concrete purpose.

- **Strategic coherence — strong.** The promise in §1 that a domain team can describe its operational surface once is tested through UJ-1 and the named Tenants bootstrap proof in SM-1. UJ-3 and SM-10 extend the same thesis to operators by distinguishing a command acknowledgement from confirmation in the projection. SM-C4 and SM-C5 protect the release decision from two misleading signals, a successful workflow and an unverified ledger entry.

- **Done-ness clarity — adequate.** FR-2 describes the zero-argument command construction contract and FR-21 tells adopters exactly what persists after a partially successful migration. Projection and storage requirements also supply concrete numbers, but their acceptance conditions blur defaults and supported overrides.

- **Scope honesty — strong.** The opening and §§5.5–5.6 identify the supported local compiler commands, the maintained compatibility host, and the conditions for retiring the host with its consumer plug-in. Section 8.3 explicitly excludes fresh-row markers for server-allocated targets without the required identity proof. The limited local container use remains a disclosed A3 assumption assigned to OI-9. None of those boundaries silently expands milestone scope.

- **Downstream usability — adequate.** The status map preserves historical decisions beside the requirements they constrain. Named journeys, stable identifiers and relative source links support extraction into stories and tests. One glossary entry uses attractive UI descriptions where an adopter needs actual attribute values.

- **Shape fit — strong.** A brownfield developer framework needs its public package surface and compatibility promises alongside operator journeys. Sections 2.4, 5.0 and 11 serve those different readers. The addendum houses the detailed release identities, interaction contracts and CLI recovery behavior, and §0.1 keeps the current milestone decision accessible without requiring a reader to reconstruct it from historical amendments.

## Findings by severity

### Critical (0)

None.

### High (0)

None.

### Medium (4)

**OI-19 omits the evidence for work already delivered** · §5.6 FR-23 and §12.2 OI-19, PRD lines 463–464 and 744

FR-23 describes page inclusion, four status-map areas and a CLI guide link, then assigns catalog/index parity to open OI-19. The implementation already checks current component sources against classified inventory and published contracts, rejects unclassified components, and detects migration catalog/index/guide drift in both directions. The November 1 retrospective reports 155 passing docs-parity cases and 127 passing migration cases on its reviewed build. Stories 15.2/15.3 are done. The remaining OI-19 handoff gives owners no way to identify which coverage or acceptance evidence is outstanding across the entire public surface. Evidence in `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcDocComponentDocumentationContractTests.cs:235,271,363`, `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs:1013,1098,1683`, `_bmad-output/implementation-artifacts/epic-15-retro-2026-11-01.md:238–239,295–297` and `sprint-status.yaml:233–236,1128–1137` in the same implementation directory.

**Suggested change.** Cite the delivered DOC-A/B tests, their bounded evidence and the stale tracker aliases. Identify the remaining surface, evidence identity or owner disposition required for OI-19 before changing its gate status.

**Recovery lacks an acceptance case for disabled polling** · §5.3 FR-12, §6 NFR-8 and §9 SM-8, PRD lines 317, 565 and 629

SM-8 requires “FallbackPolling within one polling interval” and FR-12 promises a restart after closure. FcShellOptions.cs lines 237–241 explicitly permit interval zero. The cited Story 11.2 acceptance criteria distinguish that case: after a terminal hub close the shell records disconnection and avoids the automatic restart loop. ProjectionSubscriptionService.cs line 693 also checks for a positive interval. A valid adopter configuration would fail the metric as written.

**Suggested change.** Scope the polling and closed-restart bounds to enabled polling. Give interval zero its own observable disconnection and no-poll acceptance case.

**The planning status still queues the original validator repair** · §8.2, PRD line 601

“The current Story 11.32 validator red state is new PLAN-INT-2 work” appeared under a September snapshot heading. Story 15.1 subsequently repaired the bounded 11.32 issue. The retrospective records a passing default validator run and separate strict-mode attribution failures involving interleaved commits across five Epic 15 story records. A reader using §8.2 to plan remediation can reopen completed work and miss the later defect. Evidence in `_bmad-output/implementation-artifacts/sprint-status.yaml:233,1128–1137` and `_bmad-output/implementation-artifacts/epic-15-retro-2026-11-01.md:236–237,295`.

**Suggested change.** Date the original failure and reference the Story 15.1 closure. Route strict-attribution, deferred coverage and stale-alias work through their recorded retrospective actions.

**The addendum leaves the implementation state of two release-policy controls ambiguous** · addendum §2, line 36

It identifies the policy JSON as the executable authority for “Release Owner logins, and attestation capability.” At FrontComposer `aa016201`, its JSON contains neither `release_owner_logins` nor `attestation_capability`. The GOV-1 spine specifies those inputs for the target, and the November 19 credential spec explicitly requires a policy/parser change before Release Owner logins can activate. A reviewer looking for current evaluator enforcement needs to know the fields are outstanding. Evidence in `eng/dependency-graph-policy.json`, `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md:415–420,1156–1157` and `_bmad-output/implementation-artifacts/spec-g8-split-publisher-credential-contract.md:34`.

**Suggested change.** Identify the implemented policy fields and label owner logins and attestation capability as target policy inputs pending implementation and active-policy evidence. Retain G-8’s existing acceptance obligations.

### Low (2)

**The storage default reads as a fixed ceiling** · §5.2 FR-9 and §6 NFR-9, PRD lines 275 and 566

“at most LocalStorageMaxEntries=500” presents 500 as universal. FcShellOptions.cs lines 138–139 set a default of 500 and a supported range of 50 to 10,000, and LocalStorageService.cs line 210 uses the configured value.

**Suggested change.** Identify 500 as the default and specify eviction against the configured cap within the supported range.

**The ProjectionRole examples are not all usable attribute values** · §3 Glossary, PRD line 105

The entry defines an attribute value with “for example list, timeline, card.” ProjectionRole.cs lines 7–21 defines ActionQueue, StatusOverview, DetailRecord, Timeline and Dashboard. An adopter copying List or Card into an annotation would get a compiler error.

**Suggested change.** Use the declared enum names as examples and describe each resulting UI shape separately where useful.

## Mechanical notes

- FR-1 through FR-30, including the additional FR-19a, remain identifiable. FR-27 and FR-28 retain their retired status. The 13 NFRs, six named journeys and SM-1 through SM-10 plus SM-2a remain traceable. SM-C1 and SM-C3 have recorded retirement dispositions.
- A3 maps from the inline assumption to §13 and OI-9. A1 and A2 retain their retirement dispositions. G-1, G-2, G-3, G-4 and G-8 are open. G-5, G-6 and G-7 cite dated, bounded acceptances.
- The earlier review’s 13 findings and the subsequent correction to the G-6/G-7 status table were resolved on this revision. The checks covered 31 stable functional requirement IDs, eight package target rows and eight gate states. The repository story-artifact validator passed.

## Reviewed file identities

- `prd.md` · SHA-256 `656a1f6777d16f0af2a038071d24b47843cfe14a7de694d865d7022c436b51d6`
- `prd-addendum-2026-09-08.md` · SHA-256 `9604e2c70f1d4b63150b7fec7771ad2acebe8ca051b509a3b76d880aa00a0927`

## Review and decision records

- [Approval receipt](../update/review-update.md)
- [Prior findings and dispositions](../update/reconcile-validation.md)
- [Rubric review](review-rubric.md)
- [Source consistency review](review-source-consistency.md)
