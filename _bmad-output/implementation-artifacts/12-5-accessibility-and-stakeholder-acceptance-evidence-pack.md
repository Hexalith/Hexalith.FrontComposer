# Story 12.5: Accessibility and Stakeholder Acceptance Evidence Pack

Status: ready-for-dev

> **Epic 12** - Release Certification and Evidence Alignment. This story captures the non-automated release gates for manual accessibility verification and stakeholder acceptance. It applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Stories 10.2 and 11.6 created strong automated accessibility/specimen evidence and bounded several Shell, dev-mode, localization, RTL, and visual follow-ups. They deliberately did not claim manual screen-reader, real-device, broad localization, or stakeholder acceptance completion.

Story 12.5 is the release-owner evidence pack for those remaining gates. It must turn the current manual accessibility templates, UX requirements, and stakeholder sign-off expectations into repository evidence that says one of three honest things for each gate: completed, blocked, or explicitly accepted as a v1 constraint with owner, release impact, evidence, and reopen trigger.

This story must not fake manual verification. An unperformed NVDA, JAWS, VoiceOver, real-device, localization, RTL, zoom, forced-colors, reduced-motion, or stakeholder acceptance check is a named release condition, not a passing result.

---

## Story

As a product and quality owner,
I want manual accessibility and stakeholder acceptance evidence captured,
so that release readiness includes the non-automated gates promised by the PRD and UX spec.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 12.5 evidence and know whether v1 is release-ready from a manual accessibility and stakeholder acceptance perspective, blocked by missing evidence, or ready only with named and approved constraints.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary evidence files | `docs/accessibility-verification/README.md`, `docs/accessibility-verification/manual-log-template.md`, new dated release evidence logs under `docs/accessibility-verification/`, and this story's Dev Agent Record. |
| Existing automated baseline | Story 10.2 owns Playwright/axe, keyboard, focus, forced-colors, reduced-motion, zoom/reflow, visual baseline, and specimen-manifest evidence for committed specimen surfaces. Do not duplicate that gate. |
| Manual screen-reader matrix | NVDA + Firefox, JAWS + Chrome, and VoiceOver + Safari must each be recorded as completed, blocked, or accepted constraint. Include OS, browser, assistive technology version, route/flow, result, issue links, and sign-off owner. |
| Real-device matrix | Tablet and phone fallback checks must be classified using the UX responsive tiers. Phone is functional fallback, not a v1 daily-use target. |
| Broader accessibility scope | Cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion must be classified as v1 blocker, accepted v1 constraint, or post-v1 roadmap with owner and evidence. |
| Stakeholder acceptance | Record product, quality/test, release-owner, and accessibility/stakeholder acceptance status with open feedback and release conditions. |
| Evidence integrity | Do not record pass results for unperformed audits. Do not paste raw screen-reader transcripts, personal data, tenant/user values, secrets, local absolute paths, full DOM dumps, cookies, or unbounded logs. |
| Scope guardrail | Do not redesign Shell UI, automate new Playwright coverage, rewrite accessibility architecture, implement broad localization/RTL support, or close unrelated ledger rows unless this evidence pack proves a release-blocking defect that must be split or fixed. |
| Validation | Run status-artifact consistency and `git diff --check`; run focused docs/e2e validation only if evidence docs, templates, or automation scripts change in a way that needs execution proof. |

Start here: T1 inventory existing automated/manual evidence -> T2 create release evidence pack shape -> T3 classify manual screen-reader and device evidence -> T4 classify broader accessibility constraints -> T5 capture stakeholder acceptance -> T6 update release-readiness record and validate.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `docs/accessibility-verification/README.md` and `manual-log-template.md` already define release log expectations | Story 12.5 starts | The implementer records the current template fields, required manual matrix, automated specimen routes, and gaps before editing. |
| AC2 | Story 10.2 automated accessibility evidence exists | The evidence pack is prepared | The pack references automated Playwright/axe/specimen evidence without claiming it proves manual screen-reader or stakeholder acceptance. |
| AC3 | The UX spec requires manual screen-reader verification before release branches | The release evidence pack is created | NVDA + Firefox, JAWS + Chrome, and VoiceOver + Safari are each recorded as completed, blocked, or accepted v1 constraint. |
| AC4 | A manual screen-reader combination is marked completed | The log is reviewed | The log names release branch/tag, date, tester, OS version, browser version, screen reader version, specimen route or flow, result, issue links, resolution status, evidence path, and sign-off owner. |
| AC5 | A manual screen-reader combination was not performed | The release pack is reviewed | The combination is not marked pass; it is classified as blocked or accepted constraint with release impact, owner, expiry/revalidation trigger, and reopen event. |
| AC6 | Manual verification finds an accessibility defect | The release pack is finalized | The defect is linked to a repository issue/story or release blocker with severity, affected route/flow, assistive technology context, owner, and decision. |
| AC7 | Real-device verification is required | The release pack is prepared | Tablet and phone fallback evidence is recorded as completed, blocked, or accepted constraint using the UX responsive tier definitions. |
| AC8 | Phone behavior is reviewed | The release decision is recorded | Phone limitations are classified against the documented functional-fallback commitment, not silently upgraded to full v1 design support. |
| AC9 | Cross-AT, localization, RTL, zoom, forced-colors, or reduced-motion evidence is incomplete | The evidence pack is reviewed | Each area is classified as v1 blocker, accepted v1 constraint, or post-v1 roadmap with owner, evidence, release impact, and trigger. |
| AC10 | A broader accessibility area is accepted as a v1 constraint | The acceptance is recorded | The rationale names likelihood, impact, downstream consumer impact, adopter communication need, evidence, owner, expiry or revalidation trigger, and reopen event. |
| AC11 | A release blocker is identified | Sprint/release readiness is assessed | The blocker remains visible in this story's Dev Agent Record and any bounded release-readiness notes; it is not hidden as a completed task. |
| AC12 | Stakeholder acceptance is required before v1 readiness is claimed | Story 12.5 closes | Product, quality/test, release owner, and accessibility/stakeholder acceptance status is recorded with approver, date, scope, open feedback, and release conditions. |
| AC13 | Stakeholder feedback is open | The release pack is finalized | The feedback is classified as blocking, accepted constraint, post-v1 roadmap, or non-action decision with owner and rationale. |
| AC14 | Evidence paths or attachments are recorded | The story is reviewed | Paths are repository-relative or trusted artifact links, bounded, and free of local absolute paths, secrets, tenant/user values, raw payloads, cookies, full DOM dumps, and unbounded logs. |
| AC15 | Existing manual log templates are insufficient | The implementer updates them | Template changes preserve Story 10.2 fields and add only release-certification fields needed for Story 12.5. |
| AC16 | Accessibility evidence changes baseline or specimen expectations | The implementer proposes a change | The change includes rationale, before/after evidence, and a decision whether the fix belongs in this story, a split story, or release-blocker notes. |
| AC17 | The evidence pack names a post-v1 roadmap item | The item is recorded | It links to a named owner/story/roadmap bucket and includes the release impact that makes it non-blocking for v1. |
| AC18 | The story touches `docs/accessibility-verification/**` | Validation runs | Markdown/document checks or focused review are recorded; broader Playwright or Shell tests run only if docs/templates affect executable gates. |
| AC19 | The story closes | Validation runs | Status-artifact consistency, `git diff --check`, and any focused docs/evidence validation outcomes are recorded in the Dev Agent Record. |
| AC20 | A future release owner reads the story without surrounding context | The story is complete | The final record states one release classification: `ready`, `blocked`, or `ready-with-accepted-constraints`, and names residual gates. |

---

## Tasks / Subtasks

- [ ] T1. Inventory current evidence and promises (AC1, AC2, AC7, AC9)
  - [ ] Review `docs/accessibility-verification/README.md` and `manual-log-template.md`.
  - [ ] Review Story 10.2 automated evidence scope and Story 11.6 accepted/split accessibility, localization, RTL, and sample evidence rows.
  - [ ] Review `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md` for manual matrix, responsive tiers, and verification log requirements.
  - [ ] Record which evidence is automated, manual, representative, missing, blocked, or accepted constraint before changing any templates.

- [ ] T2. Create the release evidence pack shape (AC3-AC5, AC14, AC15)
  - [ ] Create a dated release-candidate log under `docs/accessibility-verification/` or a bounded repository artifact named for the release branch/tag candidate.
  - [ ] Include separate sections for NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari, tablet, phone fallback, broader accessibility matrix, and stakeholder acceptance.
  - [ ] Preserve required Story 10.2 fields and add release-certification classification fields only where needed.
  - [ ] Add an explicit "not performed" state so missing checks cannot be misread as pass.

- [ ] T3. Classify manual screen-reader and real-device evidence (AC3-AC8)
  - [ ] For each required screen-reader/browser pairing, record completed/blocked/accepted-constraint status.
  - [ ] If a pairing is completed, record versions, route/flow, results, issue links, and sign-off owner.
  - [ ] If a pairing is blocked or accepted, record release impact, owner, expiry/revalidation trigger, and reopen event.
  - [ ] Record tablet and phone fallback checks using the responsive tier commitments from the UX spec.

- [ ] T4. Classify broader accessibility constraints (AC9-AC11, AC16, AC17)
  - [ ] Classify cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion as v1 blocker, accepted v1 constraint, or post-v1 roadmap.
  - [ ] Use Story 11.6 evidence for representative Shell/dev-mode/localization decisions, but do not inflate it into broad manual verification.
  - [ ] If evidence reveals a release-blocking defect, record the blocker and split or fix only the narrow defect that is necessary for release honesty.
  - [ ] Ensure accepted constraints include owner, likelihood, impact, downstream consumer impact, adopter communication need, evidence, and trigger.

- [ ] T5. Capture stakeholder acceptance (AC12, AC13, AC20)
  - [ ] Record Product acceptance status, Quality/Test acceptance status, Release-owner acceptance status, and Accessibility/stakeholder acceptance status.
  - [ ] Record approver, date, scope, evidence path, open feedback, release condition, and final decision for each stakeholder group.
  - [ ] Classify every open feedback item as blocking, accepted constraint, post-v1 roadmap, or non-action decision.
  - [ ] State the final release classification: `ready`, `blocked`, or `ready-with-accepted-constraints`.

- [ ] T6. Validate and record closure (AC14, AC18-AC20)
  - [ ] Run status-artifact consistency.
  - [ ] Run `git diff --check`.
  - [ ] Run docs/evidence validation or focused Playwright/docs checks only if changed files require executable validation.
  - [ ] Update this story's Dev Agent Record with changed files, validation, blockers, accepted constraints, final classification, and residual gates.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Automated Story 10.2 evidence and manual release evidence are separate gates. | Axe, keyboard, visual, and specimen checks do not prove screen-reader announcement quality or stakeholder acceptance. |
| D2 | Unperformed manual checks must be explicit `not performed`, `blocked`, or `accepted constraint`, never pass. | False accessibility evidence is more dangerous than a visible release blocker. |
| D3 | Required screen-reader pairings are NVDA + Firefox, JAWS + Chrome, and VoiceOver + Safari unless Product/Quality accepts a named constraint. | The UX spec made these pairings reproducible release evidence, not optional nice-to-have checks. |
| D4 | Phone is a functional fallback for v1, while desktop/compact/tablet carry stronger commitments. | The responsive strategy explicitly distinguishes daily-use targets from fallback usability. |
| D5 | Accepted accessibility constraints require owner, impact, downstream consumer impact, evidence, and reopen trigger. | L10 requires story-specific ownership; L06/L07 require budgeted decisions instead of vague deferrals. |
| D6 | Stakeholder acceptance is repository evidence, not a private conversation. | Release readiness must be inspectable without relying on memory or chat history. |
| D7 | Evidence artifacts are hostile input. | Logs, transcripts, screenshots, markdown, and test artifacts can leak secrets, personal data, tenant/user values, local paths, or unbounded output. |
| D8 | Story 12.5 may split or block on defects, but it should not become a broad accessibility rewrite. | The story's purpose is release certification evidence alignment, not reopening completed UI implementation stories by default. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `docs/accessibility-verification/README.md` | Update possible | Only if release-certification classification fields or evidence-pack instructions are missing. |
| `docs/accessibility-verification/manual-log-template.md` | Update possible | Preserve Story 10.2 fields; add `not performed`, blocker, accepted constraint, and sign-off fields if needed. |
| `docs/accessibility-verification/*` | Create likely | Dated release-candidate evidence pack/logs. Keep bounded and sanitized. |
| `_bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md` | Update | Dev Agent Record, validation evidence, final classification, blockers, accepted constraints, file list. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update possible | Only if Story 12.5 closes or routes current accessibility/stakeholder evidence rows. |
| `tests/e2e/specimens/frontcomposer-specimen-manifest.json` | Read likely, update unlikely | Use as current automated specimen evidence; do not change unless evidence proves the manifest is wrong. |
| `tests/e2e/specs/specimen-accessibility.spec.ts` | Read likely, update unlikely | Reference existing automated checks; do not broaden tests by default. |

No unrelated Shell redesign, Fluent UI upgrade, broad localization/RTL implementation, release workflow rewrite, EventStore provider work, MCP contract work, or nested submodule initialization should be made by default.

---

## Project Structure Notes

- Manual accessibility evidence belongs under `docs/accessibility-verification/`.
- Automated accessibility evidence is generated by the existing `tests/e2e` Playwright workspace and specimen manifest.
- The specimen routes are `/__frontcomposer/specimens/type` and `/__frontcomposer/specimens/data-formatting`.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.
- Use repository-relative paths in evidence. Do not paste local absolute paths, screen-reader raw transcripts containing personal data, full DOM dumps, cookies, secrets, tenant/user values, command payloads, or unbounded logs.

---

## Testing Strategy

- Start with document/evidence review: existing accessibility verification docs, UX responsive/accessibility spec, Story 10.2, Story 11.6, and Epic 11 retrospective.
- Run status-artifact consistency and `git diff --check` for all changes.
- Run focused docs checks if markdown templates or evidence logs change and a local checker exists.
- Run Playwright accessibility/specimen commands only if this story changes executable e2e behavior, specimen manifest entries, or route assumptions.
- Do not fake manual screen-reader or stakeholder acceptance results to satisfy tests.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 10.2 | Story 12.5 | Automated Playwright/axe/specimen evidence proves committed specimen surfaces, not manual AT or stakeholder acceptance. |
| Story 11.6 | Story 12.5 | Representative Shell/dev-mode/accessibility/localization evidence and accepted constraints inform broader release classification. |
| UX responsive/accessibility spec | Story 12.5 | Manual matrix, responsive tiers, screen-reader/browser pairings, and verification log fields define the release evidence shape. |
| `docs/accessibility-verification/**` | Release owner | Manual logs and release-candidate evidence pack are the repository evidence for accessibility sign-off. |
| Story 12.5 | v1 release decision | Final classification must say `ready`, `blocked`, or `ready-with-accepted-constraints` with residual gates. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Any required screen-reader pairing that cannot be completed before v1. | Product/Quality owner via accepted constraint or release blocker decision |
| Full RTL visual baseline matrix and direction-specific keyboard assertions if not completed here. | Post-v1 accessibility roadmap unless Product marks v1 blocking |
| Broad localization layout verification beyond representative evidence. | Product/UX localization roadmap |
| Expanded browser/device matrix beyond current release-candidate evidence. | Product/Quality release policy |
| Stakeholder feedback that changes product scope or architecture policy. | Product/Architecture decision story |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.5`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`] - release-readiness blockers and manual accessibility/stakeholder acceptance gap.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`] - approved Epic 12 correction and Story 12.5 scope.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md`] - manual matrix, screen-reader pairings, responsive tiers, and verification log fields.
- [Source: `_bmad-output/implementation-artifacts/10-2-accessibility-ci-gates-and-visual-specimen-verification.md`] - automated specimen gate and manual log template origin.
- [Source: `_bmad-output/implementation-artifacts/11-6-shell-ux-accessibility-and-sample-coverage-follow-ups.md`] - representative accessibility/localization/RTL evidence and accepted constraints.
- [Source: `docs/accessibility-verification/README.md`] - current manual release log rules.
- [Source: `docs/accessibility-verification/manual-log-template.md`] - current release log template fields.
- [Source: `tests/e2e/specimens/frontcomposer-specimen-manifest.json`] - current automated specimen route and artifact contract.
- [Source: `_bmad-output/project-context.md`] - project rules for accessibility, evidence redaction, testing, and submodules.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope/decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.
- [Official: W3C WAI, WCAG overview](https://www.w3.org/WAI/standards-guidelines/wcag/) - current WCAG guidance and version context.
- [Official: W3C, WCAG 2.2 Recommendation](https://www.w3.org/TR/wcag/) - current normative WCAG 2.2 text.
- [Official: Playwright accessibility testing](https://playwright.dev/docs/accessibility-testing) - current Playwright + axe guidance.
- [Official: Playwright emulation](https://playwright.dev/docs/emulation) - current browser/media/device emulation guidance.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-5-accessibility-and-stakeholder-acceptance-evidence-pack` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 4, and selected the first backlog story `12-5-accessibility-and-stakeholder-acceptance-evidence-pack`.
- 2026-05-13: Starting evidence audit identified current manual evidence docs under `docs/accessibility-verification/`, automated specimen manifest `tests/e2e/specimens/frontcomposer-specimen-manifest.json`, Epic 12 Story 12.5 scope, Story 10.2 automated gate, Story 11.6 representative evidence, and Epic 11 retrospective gaps.

### Completion Notes List

- 2026-05-13: Created the Story 12.5 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.

### Change Log

- 2026-05-13: Created Story 12.5 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
