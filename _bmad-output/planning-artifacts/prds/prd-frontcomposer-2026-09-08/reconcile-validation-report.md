# Input Reconciliation — validation-report.md vs rewritten PRD + addendum

- **Input:** `validation-report.md` (+ `review-rubric.md`, `review-adversarial.md`, `review-source-reconciliation.md`)
- **Targets:** `_bmad-output/planning-artifacts/prd.md` (status `draft`, updated 2026-09-08) and `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`
- **Run at:** 2026-09-08
- **Method:** every finding in the report (6 critical, 15 high, 22 medium, 8 low) was enumerated from the reviewer files, then checked against the new PRD/addendum text. Cited artifact paths named by the new PRD were existence-checked (all present: HFCM9002 contract, 9.8 live acceptance, REL-AI-1 ledger, EventStore identity JSON, `prd-addendum-2026-07-05.md`).

**Totals (51 enumerable findings):** addressed 43 · partially addressed 6 · not addressed 1 · not a distinct finding 1 (the report's "plus related medium notes" placeholder).

## Findings table

| # | Finding | Severity | Verdict | Note |
| --- | --- | --- | --- | --- |
| C1 | Status launders unfinished release as v1.0-ready (frontmatter, §0, D-9/D-6/D-10/D-11) | critical | addressed | Frontmatter split into `status: draft`, `product_approval: pending-reapproval`, `v1_publication_authorized: false`; §0.1 blocker table G-1…G-6 is sole authority; D-9 now "Blocks: G-4"; rule that no D-row may say "Blocks: None" while §0.1 has open rows. |
| C2 | REL-AI-1 circular with the release it forbids (FR-24, D-6) | critical | addressed | FR-24 redefines REL-AI-1 as owner sign-off of already-published v4.1.1 plus ledgering v4.2.0–v4.4.0; "circular reading … withdrawn"; addendum §6 records rejected alternatives (gated evidence release / dropping REL-AI-1). |
| C3 | MCP fail-closed omits oracles, skill bypass, allow-all, additive-schema side effects; SM-4 measures coverage (UJ-4, FR-17–19, NFR-5, SM-4) | critical | partially addressed | Request-class → public-shape table in FR-19; empty `tools/list` named; skill bypass in FR-18; `AllowAll*` production ban (D-14, enforcement OI-3); `CompatibleAdditive`/`CompatibleWarning` approved in D-14; dual fingerprint algorithms in §11; FR-30 gives the Shell a fail-closed rule; SM-4 rewritten as leak/oracle audit. **Missing:** timing-channel oracle is not in SM-4; the suite has no named owner ("owned by security" dropped); SM-4 "State" still cites the pre-existing coverage tests, so the independent suite is not yet an artifact or story. |
| C4 | Stories 11.20–11.23 are a publication gate on a failed schedule (FR-29, §8.2, D-10) | critical | addressed | §8.2 records 11.20–11.23 done (11.23 on 2026-08-08); D-10 "delivered"; due dates removed; FR-25 states `AnalysisMode=Recommended` as baseline. |
| C5 | Epic 9 / FR-13 / FR-26 / D-4 still treat 9.4–9.8 as open (vs sprint-status, specs, 9.8 proof) | critical | partially addressed | FR-13/FR-26 marked complete with 9.8 live proof; D-4 amended; G-5/OI-1 carry the bookkeeping lag. **Missing:** reviewer asked residuals DW-672 and DW-678 be listed; PRD/addendum name only DW-679 and the 9.8 deferred AppHost-fallback items (DW-672/678 exist in `deferred-work.md`). |
| C6 | FR-24 / NFR-12 / SM-2 require signed `release-evidence.v2` (vs architecture, REL-5, ledger) | critical | addressed | FR-24, NFR-12, SM-2, D-6, §4, §8.3 restated to unsigned author / NuGet.org repository signature / v3 / `workflow_dispatch`; "published" vs "REL-AI-1 closed" distinguished; ledger state and `fallback-approved` classification recorded in addendum §1. |
| H1 | §12.1 open-question closure hides live tensions | high | addressed | §12.1 split into Closed / Routed-with-owner-and-date / Accepted residuals; §12.2 open-items table (OI-1…OI-8) with owner, unblock condition, blocks. |
| H2 | Success metrics do not test remaining bets (FC-NIP, projection recovery, H-series) | high | addressed | SM-7 (freshness), SM-8 (projection recovery), SM-9 (architecture closure) added; SM-1 names Tenants. Minor: SM-8 "met by Story 11.2 evidence" names no artifact path. |
| H3 | FR-29 is a program envelope, not a requirement | high | addressed | FR-29.1–FR-29.7 rows with outcome, review IDs, story, state; all H1–H12 mapped. Minor: M-series lumped as "M-series" in FR-29.6 (all done, so no open outcome is lost). |
| H4 | Projection reliability has no PRD-carried bounds (FR-12, NFR-8/9/11) | high | partially addressed | FR-12 and NFR-8 inline reconnect/fallback/notice/restart numbers; NFR-11 lists v1.0 mandatory lanes. **Missing:** NFR-9 still says "checked-in benchmark thresholds and cache caps" without naming benchmark IDs or caps other than `MaxProjectionFallbackPollingLanes=8`. |
| H5 | Story 11.24 / EventStore identity invisible | high | addressed | FR-29.7, D-12, G-3, §8.2, NFR-11 live Pact lane, §7 external systems. |
| H6 | FR-24/FR-26–29/REL/GOV bundling hides the ship set | high | addressed | §5.0 Tables A (ship blockers) / B (regression baselines) / C (closed records); §0.1 gate IDs with one owner, one evidence artifact, one pass/fail sentence; FR-27/28 demoted. |
| H7 | No consumer-visible public-surface inventory (§11, D-5) | high | addressed | §11 package table (8 packable + UI/AppHost non-packable) with TFM, baseline file, breaking-change policy; MCP tool/resource identifiers, HFC bands, fingerprint algorithms, manifest names listed; UI container contradiction resolved by A3. Minor: A3 "confirm with Release Owner" has no OI row. |
| H8 | Operator jobs are the vision; CI activity is the scoreboard | high | partially addressed | §1 explicitly commits to both theses; SM-7/SM-8 are operator-outcome metrics that fail the release. **Missing:** no SM covers command-lifecycle trust (transport vs projection-confirmed, Degraded after budget — UJ-3/FR-15), which the fix named alongside freshness. |
| H9 | D-4 "Resolved" while 9.4–9.8 block freshness | high | addressed | D-4: "decision recorded … composition delivered … live proof 2026-08-27"; Blocks G-5 only. |
| H10 | A1/A2 treated as approved law; "preferably Tenants" | high | addressed | A1 retired into FR-22 text (Story 11.6); A2 retired into §4 with Tenants named as obligated adopter; G-6/OI-5 require a dated evidence artifact; D-9 no longer cites assumption dispositions. |
| H11 | D-1 BMad run copy path is archived | high | addressed | §0 and D-1: single canonical copy, archive path named as immutable, run folder holds reviews/memlog only. |
| H12 | D-6/D-11 say issue 17 closed; G2 recorded reopen + SHA | high | addressed | D-11 corrected to reopen 2026-08-08 and revision `a8a50859…`; FR-24 cites it; OI-7 for contract frontmatter/spine/GOV-1 status collapse. |
| H13 | FR-29 / §8.2 frozen on 2026-07-17 table | high | addressed | §8.2 restated from sprint-status 2026-08-29; 11.24 named; OI-6 syncs `epics.md`/`architecture.md`. |
| H14 | D-6 REL-4 freeze model contradicts REL-4 story | high | addressed | FR-24 and addendum §1: no caller freeze guard, Builds-hosted `HEXALITH_RELEASE_PUBLISH_ENABLED`, exact-SHA dispatch; default-frozen publisher vs already-published 4.1.x+ separated. |
| H15 | EventStore identity / Pact expanded NFR-11 without a requirement | high | addressed | D-12 owner-approved vs current-compatibility tuple; FR-29.7; NFR-11 live Pact provider evidence as distinct gate; G-3. |
| M1 | D-9 approval does not cover later rows | medium | addressed | D-9 "Re-approval pending for the 2026-09-08 register"; G-4. |
| M2 | HFCM9002 has no register row | medium | addressed | D-15 with contract path (file exists); FR-27 and Table C cite it. |
| M3 | Prioritization replaced by story-status narration | medium | partially addressed | Mechanism moved to addendum; §8.2 is one snapshot; §5.0 is structured. **Residual:** FR-24 still carries REL-AI-1 / BUILD-REL-1 / issue-17 choreography bullets and FR-26 carries "Stories 9.3–9.8 are done" in consequence lists. |
| M4 | FR-13/FR-26 done-ness is a story checklist | medium | addressed | FR-13 consequences now state one producer boundary, scope-safe state, atomic first-wins, materiality independence, 5912/5913 telemetry, evidence artifact. |
| M5 | Catalog FRs defer testable part to "documented" (FR-1/3/9/11) | medium | addressed | FR-1 names the five files; FR-9 names `75rem` and `32px`; FR-11 names the >15-column threshold and the full state set incl. Stale; FR-3 cites the HFC1001–1070 catalog as the single canonical source. |
| M6 | Open-item/assumption density too clean | medium | addressed | OI-1…OI-8; A3/A4 added; due dates removed; blocker table with blocks-what column. |
| M7 | Operator IA nouns not identical across UJ/glossary/routes | medium | addressed | UJ-2 uses Module/Module Tab/flyout; glossary Bounded Context → Module mapping; `{BoundedContext}` route segment called technical. |
| M8 | Canonical copy vs run copy stale | medium | addressed | Same change as H11. |
| M9 | Shape overloaded with release mechanism, underloaded on operator UX | medium | addressed | Manifest/evaluator/ceiling mechanics moved to addendum §1–2; UX rules lifted into FR-8–FR-11; UJ-2 enumerates visual states; §10 risk rewritten. |
| M10 | Dual canonical PRD copies, one path gone | medium | addressed | Same change as H11; addendum §6 records the rejected dual-copy model. |
| M11 | FR-19 labeled baseline while text says v1.0-blocking | medium | addressed | Split into FR-19 (admission) and FR-19a (cross-request lifecycle, Story 11.3 done); Table B lists FR-14–FR-19a. |
| M12 | §12.1 claims everything resolved | medium | addressed | Three-list disposition; blockers appear only in §0.1/§12.2. |
| M13 | SM-4/SM-6/NFR-11 measure activity, not adopter safety | medium | addressed | SM-C4 "green workflow is not release success"; SM-2 bound to classified manifest + ledger row; SM-4 bound to leak/oracle audit. |
| M14 | Fluent UI Blazor RC pinned as runtime constraint | medium | addressed | D-13 dated exception with `FluentConformanceTests` + e2e lanes as compatibility pack; OI-4 confirmation. |
| M15 | Shell / operator tenant isolation not a requirement | medium | addressed | FR-30 (queries, SignalR groups, storage keys, counts, fail-closed operator state); UJ-2 edge case; NFR-5 references FR-30. |
| M16 | Success metrics lack the evidence treated as resolved | medium | addressed | SM-1 "unmet"; SM-2 restated to v3/fallback-approved/owner sign-off; SM-5 ticked to 11.6; SM-4 lists existing suites. Minor: SM-3 and SM-6 have no "State" line despite §9's "each metric names its evidence" rule. |
| M17 | FR structure dropped IA and qualitative UX rules | medium | partially addressed | Default tab, module-root alias, flyout target, 32px, hamburger, non-noisy live regions, `/` search, Aspire chrome, status slots all lifted (FR-8–FR-11, glossary, addendum §4). **Missing:** the Parties "Search" tab journey vs Tenants-as-first-reference distinction named in the reviewer's dropped list is absent from PRD and addendum. |
| M18 | `api-contracts.md` is a stale 2026-06-02 scan | medium | addressed | §0 and §11 mark it provenance-only, superseded by `contracts/` + `architecture.md`. |
| M19 | Successor FC-NIP diagnostic ID never allocated | medium | addressed | FR-2 records HFC1005 reuse; FR-13/NFR-10 record EventIds 5912/5913; addendum §3 and §6 record the contract-vs-shipped note. |
| M20 | Fluent UI pin stale (rc.4 vs rc.5) | medium | addressed | §7: catalog-owned, currently `5.0.0-rc.5-26219.1`. |
| M21 | H1–H12 rationale stale vs 11.6/11.8/11.11–11.14 | medium | addressed | FR-29 rows show Done per H-id; SM-9; FR-28 records closed decisions. |
| M22 | "plus related medium notes in review-source-reconciliation.md" | medium | n/a | Placeholder line in the report; every medium in the reviewer file is already covered by M16–M21. |
| L1 | Addendum depth was dropped (rubric) | low | addressed | `prd-addendum-2026-09-08.md` created; D-1/D-2 name it and the overflow homes. |
| L2 | Command-lifecycle vocabulary does not roundtrip | low | addressed | Glossary lists the FR-15 state path incl. Warning/Degraded; UJ-3 includes the full landing set and Degraded-after-budget. |
| L3 | Generated-output path is a public contract with no compatibility window | low | addressed | §11: path changes only with a major version and a `frontcomposer migrate` edge; JSON, not folder shape, is the supported surface; glossary points to §11. |
| L4 | "Sensitive cases" / "internals" undefined | low | addressed | Words removed from UJ-4/FR-19; FR-19 table and last bullet enumerate what may not be echoed. |
| L5 | LEGACY-FR-* can still confuse a naive grep | low | not addressed | No PRD/addendum sentence keeps the LEGACY prefix mandatory in new traces. Low impact: the artifact concerned is `epics.md`, but the reviewer's fix was a rule the PRD could carry in §0. |
| L6 | No addendum for qualitative/mechanism overflow (source recon) | low | addressed | Same as L1; D-2 also adopts `architecture.md` + `ux-design.md` as overflow homes. |
| L7 | Sprint-status Epic 9 action items open after `done` | low | addressed | G-5 / OI-1 ("Default: accepted"). |
| L8 | Logging governance continued after 11.18 froze | low | addressed | FR-29 consequence: post-11.18 hardening is NFR-10 follow-through, not a reopened row. |

## Mechanical-note follow-through

| Mechanical note (report §"Mechanical notes") | Status |
| --- | --- |
| Glossary missing EventStore, Level-2/3/4, REL-*/GOV-1/BUILD-REL-1, release-evidence manifest | Added (EventStore, Customization Levels, REL/GOV/BUILD-REL-1, Governed Release, Publication Authorization, Opaque Failure Token, Command Target Identity). **`ProjectionRole` still used in FR-1 with no glossary entry.** |
| Command Lifecycle glossary incomplete | Fixed. |
| Operator "bounded context" vs Module | Fixed. |
| A1/A2 wording drift across UJ-6 / FR-22 / SM-5 | Aligned (UJ-6 and FR-22 use the same list; SM-5 adds "redacted evidence" only). |
| SM-2a numbering wart | Unchanged (SM-2a retained; new SMs numbered SM-7…SM-9 out of §9 order — primary list reads SM-1, 2, 2a, 3, 4, 7, 8, 9 then secondary SM-5, 6). Cosmetic. |
| Story 11.24 unresolved cross-ref | Fixed. |
| No addendum | Fixed. |
| Missing v1.0 blocker table | Fixed (§0.1). |

## Gaps (content from the original inputs still silently dropped or thin)

1. **DW-672 / DW-678 FC-NIP residuals** named by the source-reconciliation fix are absent; only DW-679 and the 9.8 AppHost-fallback items are carried (C5).
2. **SM-4 independence**: no timing-channel oracle, no named owner for the leak/oracle suite, and the "State" line still points at the pre-existing coverage tests rather than a suite artifact or story; C3's core is fixed, the audit is not yet a deliverable.
3. **No command-lifecycle operator SM**: SM-7/SM-8 cover freshness and recovery, but nothing fails the release if the UI overstates command confirmation or a stalled confirm does not become Degraded (H8's second half; UJ-3/FR-15).
4. **NFR-9 benchmark IDs and cache caps** remain unnamed beyond `MaxProjectionFallbackPollingLanes=8` (H4).
5. **Parties "Search" tab journey / Tenants-as-first-reference** distinction from `ux-experience-2026-07-05.md` is not carried anywhere (M17).
6. **`ProjectionRole`** is still a load-bearing undefined glossary term (mechanical note).
7. **A3** (UI sample host container) asks for Release Owner confirmation but has no OI row; **SM-3/SM-6** have no "State" line despite §9's rule (H7, M16).
8. **LEGACY-FR-* prefix rule** is not stated (L5).
9. **Residual choreography in FR consequences**: FR-24 (REL-AI-1, BUILD-REL-1, issue 17) and FR-26 ("Stories 9.3–9.8 are done") still narrate program status inside requirement text (M3).
