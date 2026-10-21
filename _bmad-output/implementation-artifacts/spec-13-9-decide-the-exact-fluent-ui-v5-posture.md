---
title: '13.9 [A · FLUENT-APP-1] Decide the Exact Fluent UI V5 Posture'
type: 'chore'
created: '2026-10-21'
status: 'ready-for-dev'
story_id: '13.9'
baseline_commit: '247410b8b35050abf3f06ec72d6bcf3f1552b150'
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

**Always:** Read identity via `git ls-tree HEAD references/Hexalith.Builds` + `git show <sha>:Props/Directory.Packages.props`, never the local checkout. The receipt names signers and roles, date, decision, package IDs/version, Builds revision, RC/GA, supported names (each verified in the pinned package), unsupported names, breaking-change rule, re-decision trigger, accessibility/visual revalidation trigger, accessibility evidence requirement, per-set evidence identity, rationale and consequences. Cite evidence; do not copy it.

**Never:** Infer approval from this workflow, green tests or catalog selection. Project one identity's evidence onto another. Rewrite or relabel 13.2–13.8 evidence or statuses, including the stale UX-A..UX-F `proposed` aliases and the Story 13.6 row. Close or reopen OI-10, OI-16, OI-19, G-4, PRD-APP-1 or the reviewer gate. Change runtime code, tests, catalog, gitlinks, dependencies or published `docs/` (DOC-C/Story 15.4 owns doc correction).

**Decisions (2026-10-21):** after the accept/reject/defer comparison, the requesting user, acting as both Product Owner and Architect, instructed "apply recommended":

1. **Not approved (AC3).** Target identity: `Microsoft.FluentUI.AspNetCore.Components` + `.Icons` `5.0.1` (GA), selected by Builds `5d03f1db9c38db34ecac7fe6048937478ffb82cc` at `530994af` and by `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` at `247410b8`. It is not approved because no single identity has green UX-A..UX-F evidence. OI-4, FLUENT-APP-1, D-13 and G-4 stay open; Story 13.9 completes on this recorded decision. Residuals: (a) one green hosted Quality run at the gitlink identity, with the 12 WebKit failures fixed or dispositioned (Gate 3a depends on the existing Gate 2b entries), followed by a Product Owner + Architect re-decision citing it; (b) the unresolvable `--fc-color-info` alias; (c) Story 13.6 and UX-A..UX-F alias tracker drift, owned but unchanged here.
2. **Signers:** the requesting user as Product Owner and Architect (session label Administrator; git author Jérôme Piquot). The 2026-09-22 proposal's Release Owner line (`:303`) is superseded by PRD OI-4/D-13 and the epics; no Release Owner co-signature.
3. **Accessibility evidence requirement:** one green hosted Quality run on a `main` commit whose gitlink selects the identity (Chromium, Firefox, WebKit, `accessibility-visual` and Gate 3a bUnit), cited by run URL. Human AT/zoom/device evidence remains under OI-16 and must name the same identity.
4. **Lifecycle:** the GA re-decision trigger fired on 2026-09-30 and is consumed. After acceptance, a patch bump needs a green Quality run on the bump commit plus a dated one-line Architect addendum to the receipt naming the exact version and run; a minor or major bump needs a new decision; a breaking change is a FrontComposer major with conformance and accessibility/visual evidence (D-13).

</frozen-after-approval>

## Code Map

- Catalog: Components + Icons `5.0.1` at gitlink `5d03f1db9c38db34ecac7fe6048937478ffb82cc` (`530994af`) and at `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` (HEAD `247410b8`, moved by the 09:15 `/pushall` sync). The identity is package IDs + exact version; the receipt records the selecting Builds revisions as provenance, and a Builds move that leaves the Fluent version unchanged is not a posture change. History: `5.0.0-rc.5-26219.1` from `e017948a` (2026-08-22); `5.0.0` at `38a59499` (2026-09-30, Builds `212583e0`); `5.0.1` at `b102b85b` (2026-10-17, Builds `4f53d9f3`).
- Evidence identity (local runs): 13.2 rc.5; 13.3 rc.5→5.0.0, local checkout ahead of gitlink on 09-30 (`spec-13-3-…:583`); 13.4 5.0.0 (`Builds@608c164c`, `spec-13-4-…:751`); 13.5 5.0.0; 13.6 5.0.0→5.0.1, final three-engine run proven 5.0.1 by DLL SHA-256 `82718d81…` (`evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/candidate.json:76`) with 18 visual failures, Shell .NET 3709/7 failed (`spec-13-6-rerun-browser-dotnet-gates.md:101-102`), primary spec still `in-review`; 13.7 5.0.1, .NET/docs only.
- Hosted Quality run `39554582583` on HEAD `530994af` binds to the gitlink identity (5.0.1) by checkout: `accessibility-visual`, Chromium and Firefox all-spec jobs pass. WebKit has 417 passed / 12 failed / 22 skipped, all in `accessibility-state-matrix.spec.ts` (`:241,:265,:647,:664,:689`, light and dark), with no ledger entry. `build-and-test` is red at Gate 2b governance (tracked: DW-1800, DW-1965), so the Gate 3a bUnit lane is skipped.
- Manual AT/zoom/device: every MAN-136-* gate `not performed` (`13-6-accessibility-result.md:228-234,306-310`).
- Names resolving in 5.0.1 package (`lib.module.js`, assembly types): accent role = brand ramp from `IThemeService.SetThemeAsync(new ThemeSettings(AccentColor, 0, 0, mode, false))` (`FrontComposerShell.razor.cs:749`), consumed as `--colorBrandForeground1` (`FrontComposerShell.razor.css:10`); `--colorBrandBackground`, `--colorNeutralBackground1..3`, `--colorNeutralStroke2`, `--colorNeutralForeground1..3`, `--colorStatus{Success,Warning,Danger}Foreground1`; `FluentProviders`, `FluentAccordion`, `FluentBadge`, `FluentTooltip`, `FluentStack`, `FluentGrid`, `FluentLayout`, `ThemeSettings`, `ThemeMode`. Unresolvable: `--colorStatusInfoForeground1` (status tokens exist only for success/warning/danger), so `--fc-color-info` (`FrontComposerShell.razor.css:15`) is dead; it has no consumers and is pinned by `SlotMappingRegressionTests.cs:18,39`.
- Guards already at 5.0.1, unchanged: `FluentConformanceTests.cs:27`, `PackageBoundaryTests.cs:16,112`.
- Planning: epics `:3729-3750` (ACs), `:1742` stale rc.4 label; PRD D-13 `:694`, OI-4 `:721`, G-4 `:44`, RC wording `:568,:647,:705`. Sprint: `sprint-status.yaml:209` (13.9), `:978` FLUENT-APP-1 alias, E9-APP-1 `action_items` `:722-746` (shape precedent). Ledger: last ID DW-1984 (format precedent).
- Precedent: `spec-13-8-accept-the-completed-fresh-row-live-proof.md` § Product Decision Receipt.

## Tasks & Acceptance

**Execution:**
- [ ] This spec -- append `## Product Decision Receipt` with every Always field, the four decisions, a per-set evidence identity table and the consequences -- the receipt is the decision of record.
- [ ] `_bmad-output/planning-artifacts/prd.md` -- dated 2026-10-21 inline updates to D-13, OI-4 and the `:568/:647/:705` posture lines: GA trigger fired, 5.0.1 target not approved, receipt linked, both still open -- G-4, OI-10, OI-16, OI-19 and Table A stay unchanged.
- [ ] `_bmad-output/planning-artifacts/epics.md` -- dated Story 13.9 disposition and a dated note at `:1742`; set `updated` -- preserve history while removing the misleading current label.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- keep the `:978` alias `open` and add the receipt ref; add an open FLUENT-APP-1 `action_items` entry shaped like E9-APP-1 with `decision: "not-approved"`; move the 13.9 row and this spec to done together after review.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md` -- append DW-1985 (residual a; Quality/Test role, then Product Owner + Architect re-decision), DW-1986 (residual b; FrontComposer Shell maintainer) and DW-1987 (residual c; sprint tracking owner), each with status, origin, source_spec, owner, reason and closure -- cite DW-1800/DW-1965 instead of duplicating Gate 2b.

**Acceptance Criteria:**
- Given the gitlink catalog and per-set evidence identities, when the receipt is recorded, then it is named and dated, states "not approved" for the exact target identity with every Always field, and every supported name resolves in the 5.0.1 package while every unsupported name does not.
- Given the not-approved decision, when tracking reconciles, then OI-4, FLUENT-APP-1, D-13 and G-4 read open with a dated receipt link, and OI-10, OI-16, OI-19 and PRD-APP-1 are unchanged.
- Given the three residuals, when appended, then each has an owner and closure condition and none duplicates the existing Gate 2b entries.
- Given Stories 13.2–13.8, when 13.9 completes, then their evidence and statuses are unchanged from baseline.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md --candidate HEAD` -- expected: exit 0 with a `## File List`.
- `git diff --check` and a YAML parse of `sprint-status.yaml` -- expected: clean.
- Re-run the identity commands; grep the 5.0.1 package for each supported name (non-zero) and each unsupported name (zero).
- `git diff --stat 247410b8 -- src tests docs references` -- expected: empty.
