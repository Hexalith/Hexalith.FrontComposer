---
title: '13.9 [A · FLUENT-APP-1] Decide the Exact Fluent UI V5 Posture'
type: 'chore'
created: '2026-10-21'
status: 'draft'
story_id: '13.9'
baseline_commit: '530994afdaba96567752e1a0a264a4b13a5cf909'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FLUENT-APP-1/OI-4/D-13 has no decision. Planning still describes a Fluent RC posture while the root-selected catalog is GA, and UX-A..UX-F evidence was produced against three different catalog identities.

**Approach:** Record the owners' explicit, dated decision as a receipt in this spec, verified against the root gitlink catalog and the identity each evidence set actually bound to, then reconcile PRD, epics, sprint tracking and the deferred ledger to that outcome.

## Boundaries & Constraints

**Always:** Read identity via `git ls-tree HEAD references/Hexalith.Builds` + `git show <sha>:Props/Directory.Packages.props`, never the local checkout. The receipt names signers and roles, date, decision, package IDs/version, Builds revision, RC/GA, supported names (each verified in the pinned package), unsupported names, breaking-change rule, re-decision trigger, accessibility/visual revalidation trigger, accessibility evidence requirement, per-set evidence identity, rationale and consequences. Cite evidence; do not copy it. Acceptance closes OI-4/FLUENT-APP-1 only and accepts D-13. Non-acceptance leaves OI-4, FLUENT-APP-1, D-13 and G-4 open, with corrective work as bounded, owned DW residuals.

**Never:** Infer approval from this workflow, green tests or catalog selection. Project one identity's evidence onto another. Rewrite or relabel 13.2–13.8 evidence or statuses, including the stale UX-A..UX-F `proposed` aliases and the Story 13.6 row. Close or reopen OI-10, OI-16, OI-19, G-4, PRD-APP-1 or the reviewer gate. Change runtime code, tests, catalog, gitlinks, dependencies or published `docs/` (DOC-C/Story 15.4 owns doc correction).

</frozen-after-approval>

## Open Questions

1. **Decision on `Microsoft.FluentUI.AspNetCore.Components` + `.Icons` `5.0.1` (GA) at Builds `5d03f1db…`** — Accept (closes OI-4/FLUENT-APP-1; waives AC1's single-identity precondition and projects rc.5/5.0.0 browser evidence onto 5.0.1) / Not approved, recorded per AC3 (receipt fixes target, names and rules; gates stay open; revalidation + later re-decision become residuals; 13.9 closes) / Defer (no receipt; 13.9 stays backlog until single-identity evidence exists).
2. **Signers** — the requesting user as both Product Owner and Architect, with the 2026-09-22 proposal's "Release Owner" recorded as superseded by PRD OI-4/D-13 and epics / also require a Release Owner co-signature (story blocks until obtained).
3. **Accessibility evidence requirement for acceptance** — automated single-identity UX-A..UX-F evidence (.NET/bUnit + three-engine Playwright with axe WCAG 2.2 AA); human AT/zoom/device stays OI-16's and must name the same identity / automated plus all MAN-136-* human gates on that identity.
4. **Revalidation granularity** — any catalog Fluent version change, patch included, triggers automated revalidation and minor/major triggers re-decision / only minor/major triggers revalidation; patches rely on governance tests.

## Code Map

- Catalog at root gitlink `5d03f1db9c38db34ecac7fe6048937478ffb82cc` `Props/Directory.Packages.props:260-261`: Components + Icons `5.0.1`. History: `5.0.0-rc.5-26219.1` from `e017948a` (2026-08-22); `5.0.0` at `38a59499` (2026-09-30, Builds `212583e0`); `5.0.1` at `b102b85b` (2026-10-17, Builds `4f53d9f3`). GA re-decision trigger has fired.
- Evidence identity (all local; no CI runs recorded): 13.2 rc.5; 13.3 rc.5→5.0.0, local checkout ahead of gitlink on 09-30 (`spec-13-3-…:583`); 13.4 5.0.0 (`Builds@608c164c`, `spec-13-4-…:751`); 13.5 5.0.0; 13.6 5.0.0→5.0.1, final three-engine run proven 5.0.1 by DLL SHA-256 `82718d81…` (`evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/candidate.json:76`) with 18 visual failures, Shell .NET 3709/7 failed (`spec-13-6-rerun-browser-dotnet-gates.md:101-102`), primary spec still `in-review`; 13.7 5.0.1, .NET/docs only. 13.2–13.5 browser specs never re-ran at 5.0.1.
- Hosted Quality run `39554582583` on HEAD `530994af` binds to the gitlink identity (5.0.1) by checkout: `accessibility-visual`, Chromium and Firefox all-spec jobs pass. WebKit has 417 passed / 12 failed / 22 skipped, all in `accessibility-state-matrix.spec.ts` (`:241,:265,:647,:664,:689`, light and dark), with no ledger entry. `build-and-test` is red at Gate 2b governance (tracked: DW-1800, DW-1965), so the Gate 3a bUnit lane is skipped.
- Manual AT/zoom/device: every MAN-136-* gate `not performed` (`tests/13-6-accessibility-result.md:228-234,306-310`).
- Names resolving in 5.0.1 package (`lib.module.js`, assembly types): accent role = brand ramp from `IThemeService.SetThemeAsync(new ThemeSettings(AccentColor, 0, 0, mode, false))` (`FrontComposerShell.razor.cs:749`), consumed as `--colorBrandForeground1` (`FrontComposerShell.razor.css:10`); `--colorBrandBackground`, `--colorNeutralBackground1..3`, `--colorNeutralStroke2`, `--colorNeutralForeground1..3`, `--colorStatus{Success,Warning,Danger}Foreground1`; `FluentProviders`, `FluentAccordion`, `FluentBadge`, `FluentTooltip`, `FluentStack`, `FluentGrid`, `FluentLayout`, `ThemeSettings`, `ThemeMode`. Unresolvable: `--colorStatusInfoForeground1` (status tokens exist only for success/warning/danger), so `--fc-color-info` (`FrontComposerShell.razor.css:15`) is dead; it has no consumers and is pinned by `SlotMappingRegressionTests.cs:18,39`. The rc.5 package is absent locally.
- Guards already at 5.0.1, unchanged: `FluentConformanceTests.cs:27`, `PackageBoundaryTests.cs:16,112`.
- Planning: epics `:3729-3750` (ACs), `:1742` stale rc.4 label; PRD D-13 `:694`, OI-4 `:721`, G-4 `:44`, RC wording `:568,:647,:705`; owner conflict `sprint-change-proposal-2026-09-22.md:303`. Sprint: `sprint-status.yaml:209` (13.9), `:978` FLUENT-APP-1 alias, E9-APP-1 `action_items` `:722-746` (shape precedent). Ledger: last ID DW-1984 (format precedent).
- Precedent: `spec-13-8-accept-the-completed-fresh-row-live-proof.md` § Product Decision Receipt.

## Tasks & Acceptance

**Execution:**
- [ ] This spec -- append `## Product Decision Receipt` with every Always field and a per-set evidence identity table -- the receipt is the decision of record.
- [ ] `_bmad-output/planning-artifacts/prd.md` -- dated 2026-10-21 inline updates to D-13, OI-4 and the `:568/:647/:705` posture lines, linking the receipt -- record that the GA trigger fired and the outcome; G-4, OI-10, OI-16, OI-19 and Table A stay unchanged.
- [ ] `_bmad-output/planning-artifacts/epics.md` -- dated Story 13.9 disposition and a dated note at `:1742`; set `updated` -- preserve history while removing the misleading current label.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- annotate the `:978` alias with decision and ref (`done` only on acceptance); add a FLUENT-APP-1 `action_items` entry mirroring E9-APP-1; move the 13.9 row and this spec together after review.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md` -- append owned residuals: the unresolvable `--fc-color-info` alias; the Story 13.6/UX alias tracker drift; on non-acceptance, single-identity UX-A..UX-F revalidation followed by an A-story re-decision.

**Acceptance Criteria:**
- Given the gitlink catalog and per-set evidence identities, when the owners decide, then a named, dated receipt records the decision with every Always field, and every listed supported name resolves in the pinned package.
- Given acceptance, when tracking reconciles, then OI-4, FLUENT-APP-1 and D-13 close and G-4 stays open.
- Given non-acceptance, when recorded, then OI-4, FLUENT-APP-1, D-13 and G-4 stay open, each correction is a bounded DW residual with owner and closure, and 13.2–13.8 evidence and statuses are unchanged.
- Given no recorded decision, when status is checked, then nothing closes and this spec stays in draft.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md --candidate HEAD` -- expected: exit 0 with a `## File List`.
- `git diff --check` and a YAML parse of `sprint-status.yaml` -- expected: clean.
- Re-run the identity commands; grep the 5.0.1 package for each supported name (non-zero) and each unsupported name (zero).
- `git diff --stat 530994af -- src tests docs references` -- expected: empty.
