# Story 12.5: Accessibility and Stakeholder Acceptance Evidence Pack

Status: done

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

## Evidence Status Contract

Every release gate in this story must have exactly one explicit status before the final classification is written:

| Status | Meaning | Required fields |
| --- | --- | --- |
| `completed` | The gate was actually exercised or reviewed in the stated environment and has sanitized evidence. | `evidence_id`, `gate`, `task_id`, `ac_ids`, `environment`, `tester_or_reviewer`, `date`, `source_path`, `sanitization_status`, `approval_ref` |
| `not performed` | The gate has not been exercised and cannot satisfy a required release gate. | `owner`, `release_impact`, `reason`, `next_action`, `reopen_event` |
| `blocked` | The gate cannot be completed or accepted for the release without a blocking decision or fix. | `owner`, `release_impact`, `blocker_ref`, `reopen_event`, `decision_needed` |
| `accepted v1 constraint` | Product, Accessibility/Stakeholder, and Release Owner explicitly accept the release risk for v1. | `owner`, `release_impact`, `downstream_consumer_impact`, `adopter_communication_need`, `evidence_ref`, `expiry_or_revalidation_trigger`, `reopen_event`, `approval_refs` |
| `post-v1 roadmap` | The gate is intentionally outside v1 release readiness and is linked to a named owner/story/roadmap bucket. | `owner`, `story_or_roadmap_ref`, `target_release_or_nonplanning_rationale`, `release_impact`, `reopen_event` |

Manual AT, tablet, and phone gates can only be `completed` from dated manual evidence. Story 10.2 automation and Story 11.6 representative evidence can support the release decision, but they must not be used to infer manual NVDA, JAWS, VoiceOver, tablet, or phone completion.

The final release classification is fail-closed:

- `ready` is allowed only when every required gate is `completed`, every evidence reference is sanitized, and every stakeholder sign-off is present.
- `ready-with-accepted-constraints` is allowed only when all incomplete gates are either `accepted v1 constraint` or `post-v1 roadmap` with the required fields and approvals.
- `blocked` is required when a required gate is `not performed`, `blocked`, missing required fields, missing an owner, missing sanitization proof, or missing required stakeholder approval.

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
| AC21 | Any gate is entered in the evidence pack | The pack is reviewed | The gate has exactly one status from the Evidence Status Contract and all fields required for that status. |
| AC22 | A manual AT, tablet, or phone gate is marked `completed` | The final classification is prepared | The completion is backed by dated manual evidence; automated axe/specimen evidence or Story 11.6 representative evidence is not used as a substitute. |
| AC23 | A required gate is `not performed` or `blocked` | The final classification is prepared | The final classification is `blocked` unless Product, Accessibility/Stakeholder, and Release Owner approve an `accepted v1 constraint` or named post-v1 roadmap classification. |
| AC24 | A gate is accepted as a v1 constraint | The acceptance is reviewed | Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approvals are linked (aligns with AC28 four-role release-readiness sign-off), and adopter communication need is explicitly classified. |
| AC25 | A post-v1 roadmap item is recorded | The release owner reviews it | The item has a named owner, story or roadmap reference, target release or non-planning rationale, release impact, and reopen event. |
| AC26 | Evidence files, screenshots, logs, markdown, or attachments are added | Validation runs | A redaction review records scope, command or manual method, result, and any approved exception; unsanitized evidence blocks `ready` and `ready-with-accepted-constraints`. |
| AC27 | The pack references Story 10.2 or Story 11.6 evidence | The release classification is reviewed | The reference states whether the evidence is automated, representative, manual, blocked, accepted, or roadmap; no wording claims broad accessibility validation from representative evidence. |
| AC28 | Stakeholder acceptance contains any constraint, blocker, or roadmap item | The sign-off is reviewed | Product, Quality/Test, Release Owner, and Accessibility/Stakeholder sign-offs are recorded separately with date, scope, evidence path, and open conditions. |
| AC29 | A future automation or Playwright expansion is proposed during implementation | Scope is assessed | The expansion is split to a named follow-up unless it is required to verify changed executable behavior in this story. |
| AC30 | A gate appears in more than one evidence table, register, or stakeholder note | The final release classification is prepared | Exactly one canonical gate status wins, duplicate or contradictory statuses are listed as blockers, and the evidence pack cannot claim `ready` or `ready-with-accepted-constraints` until the contradiction is resolved. |
| AC31 | An accepted constraint or stakeholder approval references an external conversation, ticket, or artifact | The release owner reviews the evidence | The reference is repository-visible or otherwise auditable, names approver authority, date, scope, retention owner, and sanitized evidence pointer; private or stale references block the release classification. |
| AC32 | The release branch, tag, commit, browser/AT version, UX baseline, or responsive tier changes after evidence is collected | The evidence pack is finalized | The affected gate is revalidated, explicitly accepted as stale with owner and trigger, or marked blocked; old evidence is not silently reused for a changed release candidate. |
| AC33 | Evidence includes screenshots, recordings, exported logs, attachments, or links outside the repository | The redaction review runs | A bounded evidence manifest records artifact type, source, retention owner, checksum or immutable reference when available, sanitization result, and approved exceptions. |
| AC34 | A stakeholder signs off through a delegate or proxy | The sign-off is recorded | Delegation authority, scope, expiration, and approving role are explicit; otherwise the sign-off is treated as missing. |
| AC35 | The final classification is written in prose, markdown tables, or release notes | Validation runs | One machine-readable summary records the final enum, residual gates, accepted constraints, blockers, sign-off refs, and evidence pack version; conflicting prose cannot override it. |

---

## Tasks / Subtasks

- [x] T1. Inventory current evidence and promises (AC1, AC2, AC7, AC9)
  - [x] Review `docs/accessibility-verification/README.md` and `manual-log-template.md`.
  - [x] Review Story 10.2 automated evidence scope and Story 11.6 accepted/split accessibility, localization, RTL, and sample evidence rows.
  - [x] Review `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md` for manual matrix, responsive tiers, and verification log requirements.
  - [x] Record which evidence is automated, manual, representative, missing, blocked, or accepted constraint before changing any templates.
  - [x] Record the exact required release gates before editing: NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari, tablet, phone fallback, cross-AT, localization, RTL, zoom, forced-colors, reduced-motion, Product acceptance, Quality/Test acceptance, Release Owner acceptance, and Accessibility/Stakeholder acceptance.

- [x] T2. Create the release evidence pack shape (AC3-AC5, AC14, AC15)
  - [x] Create a dated release-candidate log under `docs/accessibility-verification/` or a bounded repository artifact named for the release branch/tag candidate.
  - [x] Include separate sections for NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari, tablet, phone fallback, broader accessibility matrix, and stakeholder acceptance.
  - [x] Preserve required Story 10.2 fields and add release-certification classification fields only where needed.
  - [x] Add the Evidence Status Contract fields so missing checks cannot be misread as pass and every status can be audited back to AC/task IDs.
  - [x] Add an Accepted Constraints Register, Post-v1 Roadmap Register, Stakeholder Acceptance section, Adopter Communication section, and Release Classification section.
  - [x] Assign stable gate IDs for every manual AT, device, broader accessibility, stakeholder, constraint, roadmap, and final-classification row so duplicates can be reconciled deterministically.
  - [x] Add an evidence manifest section for any screenshots, recordings, exported logs, repository-external links, or retained artifacts, including sanitization status and retention owner.

- [x] T3. Classify manual screen-reader and real-device evidence (AC3-AC8)
  - [x] For each required screen-reader/browser pairing, record completed/blocked/accepted-constraint status.
  - [x] If a pairing is completed, record versions, route/flow, results, issue links, and sign-off owner.
  - [x] If a pairing is blocked or accepted, record release impact, owner, expiry/revalidation trigger, and reopen event.
  - [x] Record tablet and phone fallback checks using the responsive tier commitments from the UX spec.
  - [x] Reject any `completed` status that lacks dated manual evidence or tries to substitute automated axe/specimen evidence for manual AT/device proof.
  - [x] Bind each completed manual evidence row to release branch/tag/commit plus browser, OS, AT, responsive tier, and UX baseline versions; reclassify stale rows if any of those inputs change before release.

- [x] T4. Classify broader accessibility constraints (AC9-AC11, AC16, AC17)
  - [x] Classify cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion as v1 blocker, accepted v1 constraint, or post-v1 roadmap.
  - [x] Use Story 11.6 evidence for representative Shell/dev-mode/localization decisions, but do not inflate it into broad manual verification.
  - [x] If evidence reveals a release-blocking defect, record the blocker and split or fix only the narrow defect that is necessary for release honesty.
  - [x] Ensure accepted constraints include owner, likelihood, impact, downstream consumer impact, adopter communication need, evidence, and trigger.
  - [x] Ensure any post-v1 roadmap item has a named owner, story or roadmap reference, target release or non-planning rationale, release impact, and reopen event.
  - [x] Resolve duplicate or contradictory statuses across the gate matrix, accepted-constraint register, roadmap register, and stakeholder notes before writing the final classification.
  - [x] Treat stale, private, or unauditable accepted-constraint approval refs as blockers until a repository-visible or retained auditable reference is recorded.

- [x] T5. Capture stakeholder acceptance (AC12, AC13, AC20)
  - [x] Record Product acceptance status, Quality/Test acceptance status, Release-owner acceptance status, and Accessibility/stakeholder acceptance status.
  - [x] Record approver, date, scope, evidence path, open feedback, release condition, and final decision for each stakeholder group.
  - [x] Classify every open feedback item as blocking, accepted constraint, post-v1 roadmap, or non-action decision.
  - [x] State the final release classification: `ready`, `blocked`, or `ready-with-accepted-constraints`.
  - [x] Require separate Product, Quality/Test, Release Owner, and Accessibility/Stakeholder approvals when a blocker, accepted constraint, or roadmap item affects release classification.
  - [x] Record delegation authority, scope, and expiration when any stakeholder sign-off is provided by a proxy; reject proxy approval that cannot prove authority.

- [x] T6. Validate and record closure (AC14, AC18-AC20)
  - [x] Run status-artifact consistency.
  - [x] Run `git diff --check`.
  - [x] Run or document a bounded redaction review over changed evidence artifacts for local absolute paths, secrets, cookies, tenant/user values, raw payloads, full DOM dumps, and unbounded logs.
  - [x] Check that every status references an evidence artifact or approved exception and that no `ready`/`ready-with-accepted-constraints` classification is produced with missing required fields.
  - [x] Check that the final release summary has one machine-readable classification enum, evidence pack version, residual gates, blockers, accepted constraints, roadmap refs, and sign-off refs.
  - [x] Check that no prose, table row, or release note contradicts the machine-readable classification summary.
  - [x] Run docs/evidence validation or focused Playwright/docs checks only if changed files require executable validation.
  - [x] Update this story's Dev Agent Record with changed files, validation, blockers, accepted constraints, final classification, and residual gates.

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
| D9 | Manual AT and device `completed` statuses require dated manual evidence. | Automated checks and representative evidence can support release decisions but cannot prove screen-reader or real-device execution. |
| D10 | `ready` and `ready-with-accepted-constraints` are fail-closed classifications. | Missing required fields, missing owners, missing approvals, or unsanitized evidence must block the release classification. |
| D11 | Accepted constraints require Product, Quality/Test, Accessibility/Stakeholder, and Release Owner approval when they affect release readiness. | A constraint is a release decision, not a documentation shortcut; aligned with AC28 four-role release-readiness sign-off per CR-12-5-D1 (2026-05-19 code review). |
| D12 | Post-v1 roadmap status is allowed only with a named owner and story/roadmap reference. | L10 requires story-specific ownership instead of vague future deferrals. |
| D13 | Redaction review is part of the evidence contract. | Release evidence can leak sensitive data even when the implementation is correct. |
| D14 | New automation belongs outside Story 12.5 unless executable behavior changes here. | Preserves L06/L07 budget and keeps this story focused on evidence alignment rather than broad test expansion. |
| D15 | Gate IDs are stable and final status is single-source. | Duplicate manual, stakeholder, constraint, and roadmap rows otherwise let contradictory release claims survive review. |
| D16 | Evidence freshness is bound to release candidate identity and environment versions. | Manual AT/device evidence can become stale when branch, tag, commit, browser, OS, AT, UX baseline, or responsive-tier assumptions change. |
| D17 | Constraint approvals and stakeholder sign-offs must be auditable outside private chat. | Release readiness evidence has to survive turnover, audit, and future reruns without relying on memory. |
| D18 | External evidence requires a manifest and retention owner. | Screenshots, recordings, logs, and external links are useful only if bounded, retained, sanitized, and traceable. |
| D19 | Proxy sign-off is valid only with explicit delegated authority. | A delegated approval without scope or expiration can accidentally bypass the real Product, Quality, Release Owner, or Accessibility decision maker. |
| D20 | The final classification must have one machine-readable summary. | Prose-only release decisions are easy to contradict and hard for later automation or release owners to verify. |

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

## Party-Mode Review

- Date: 2026-05-13T22:34:15+02:00
- Selected story key: `12-5-accessibility-and-stakeholder-acceptance-evidence-pack`
- Command/skill invocation used: `/bmad-party-mode 12-5-accessibility-and-stakeholder-acceptance-evidence-pack; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: All reviewers agreed the story is the right evidence-alignment slice, but it needed stronger pre-dev guardrails before implementation. Main risks were false manual AT/device completion inferred from automation, weak `accepted v1 constraint` governance, ambiguous final release classification, underspecified stakeholder sign-off, broad accessibility/localization/RTL scope creep, and insufficient redaction proof for evidence artifacts.
- Changes applied: Added the Evidence Status Contract; added AC21-AC29; tightened T1-T6 with required gate inventory, status fields, accepted-constraint/register sections, manual-evidence-only completion rules, roadmap ownership, separate stakeholder approvals, redaction review, and fail-closed final classification checks; added Decisions D9-D14 for manual completion, fail-closed release classification, stakeholder approvals, roadmap ownership, redaction, and automation scope.
- Findings deferred: Exact future AT/device matrix expansion, broad localization/RTL coverage, Shell focus-management redesign, additional Playwright automation, official signature medium, and release acceptance thresholds remain Product/Accessibility/Release Owner decisions or split follow-up work unless the Story 12.5 evidence pack reveals a release blocker.
- Final recommendation: `ready-for-dev`

## Advanced Elicitation

- Date: 2026-05-14T12:03:31+02:00
- Selected story key: `12-5-accessibility-and-stakeholder-acceptance-evidence-pack`
- Command/skill invocation used: `/bmad-advanced-elicitation 12-5-accessibility-and-stakeholder-acceptance-evidence-pack`
- Batch 1 methods: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 methods: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- Findings summary: The story was already strong on manual-evidence honesty, but the elicitation found remaining release-readiness traps: duplicated gate statuses could produce conflicting classifications, accepted-constraint approvals could point to private or stale evidence, manual AT/device evidence could become stale when the release candidate or environment changes, external screenshots/logs need retention and redaction manifesting, proxy stakeholder sign-off needs authority proof, and prose-only final classification is too easy to contradict.
- Changes applied: Added AC30-AC35 for canonical gate status, auditable approval refs, release-candidate freshness, external evidence manifesting, proxy sign-off authority, and machine-readable final classification; tightened T2-T6 with stable gate IDs, evidence manifest, stale-evidence reclassification, duplicate-status reconciliation, approval auditability, delegation checks, final summary validation, and contradiction checks; added Decisions D15-D20 for gate identity, freshness, auditable sign-offs, external evidence retention, proxy authority, and machine-readable classification.
- Findings deferred: Exact evidence-pack schema filename, release-owner approval medium, retention duration, checksum format, and any automation that reads the machine-readable summary remain implementation or Product/Release Owner decisions unless existing repository conventions already define them.
- Final recommendation: `ready-for-dev`

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-5-accessibility-and-stakeholder-acceptance-evidence-pack` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 4, and selected the first backlog story `12-5-accessibility-and-stakeholder-acceptance-evidence-pack`.
- 2026-05-13: Starting evidence audit identified current manual evidence docs under `docs/accessibility-verification/`, automated specimen manifest `tests/e2e/specimens/frontcomposer-specimen-manifest.json`, Epic 12 Story 12.5 scope, Story 10.2 automated gate, Story 11.6 representative evidence, and Epic 11 retrospective gaps.
- 2026-05-13T22:34:15+02:00: Party-mode review applied via `/bmad-party-mode 12-5-accessibility-and-stakeholder-acceptance-evidence-pack; review;` with Winston, Amelia, John, and Murat. Added fail-closed evidence status, manual completion, stakeholder approval, roadmap ownership, redaction, and automation-scope guardrails.
- 2026-05-14T12:03:31+02:00: Advanced elicitation applied via `/bmad-advanced-elicitation 12-5-accessibility-and-stakeholder-acceptance-evidence-pack`. Added canonical gate identity, evidence freshness, approval auditability, external evidence manifest, proxy sign-off authority, and machine-readable final classification guardrails.
- 2026-05-15: Loaded `_bmad-output/project-context.md`, Story 12.5, sprint status, current accessibility verification docs, Story 10.2 automated specimen evidence, Story 11.6 representative accessibility/localization evidence, UX responsive/accessibility requirements, and `tests/e2e/specimens/frontcomposer-specimen-manifest.json`.
- 2026-05-15: Updated Story 12.5 and `sprint-status.yaml` from `ready-for-dev` to `in-progress` before implementation.
- 2026-05-15: Created `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md` with stable gate IDs, fail-closed manual AT/device/stakeholder blockers, post-v1 roadmap rows, evidence manifest, and machine-readable `final_classification: blocked`.
- 2026-05-15: Updated `docs/accessibility-verification/README.md` and `manual-log-template.md` with Story 12.5 release-certification fields while preserving Story 10.2 manual log expectations.
- 2026-05-15: `python jobs/preflight-code-review.py --repo . --out $env:TEMP\frontcomposer-12-5-preflight.json` reported YAML parse OK and status-artifact consistency pass (`no drift across 73 story keys`); overall preflight was fail only because the working tree had the intentional Story 12.5 edits.
- 2026-05-15: `git diff --check` passed after line-ending normalization.
- 2026-05-15: Redaction scan passed for the changed accessibility evidence docs, sprint status, and Story 12.5 record with no local absolute paths, bearer values, cookie headers, tenant/user assignments, secret assignments, or token assignments.
- 2026-05-15: Machine-readable classification check found exactly one YAML `final_classification: "blocked"` and matching prose final classification.
- 2026-05-15: Final validation after moving the story to review: `rg -n "\[ \]" _bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md` found no unchecked tasks; `python jobs/preflight-code-review.py --repo . --out $env:TEMP\frontcomposer-12-5-final-preflight.json` again reported YAML parse OK and status-artifact consistency pass (`no drift across 73 story keys`), with only the expected dirty-working-tree preflight failure from the current story edits.

### Implementation Plan

- Preserve Story 10.2 automated accessibility evidence as supporting evidence only, not a substitute for manual screen-reader or real-device completion.
- Create repository-visible release evidence that names every required manual/stakeholder gate and blocks release readiness when evidence or approvals are missing.
- Keep Story 12.5 scoped to documentation/evidence artifacts; do not add Playwright automation or Shell UI changes because no executable behavior changed.

### Completion Notes List

- 2026-05-13: Created the Story 12.5 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.
- 2026-05-13: Party-mode review hardened the story and left it `ready-for-dev` for later advanced elicitation.
- 2026-05-14: Advanced elicitation hardened the story and left it `ready-for-dev` for development.
- 2026-05-15: Implemented the Story 12.5 accessibility and stakeholder acceptance evidence pack. Manual NVDA + Firefox, JAWS + Chrome, VoiceOver + Safari, tablet, phone fallback, and stakeholder sign-off gates are blocked because no dated manual evidence or auditable approvals exist in the repository.
- 2026-05-15: Classified broad cross-AT, localization, RTL, and extra zoom/forced-colors/reduced-motion matrices as post-v1 roadmap items with named owner roles and reopen events, while preserving Story 10.2 automated specimen evidence as supporting evidence only.
- 2026-05-15: Final release classification is `blocked`; residual gates are `AT-NVDA-FIREFOX`, `AT-JAWS-CHROME`, `AT-VOICEOVER-SAFARI`, `DEVICE-TABLET`, `DEVICE-PHONE-FALLBACK`, `STAKEHOLDER-PRODUCT`, `STAKEHOLDER-QUALITY`, `STAKEHOLDER-RELEASE-OWNER`, and `STAKEHOLDER-ACCESSIBILITY`.

### Change Log

- 2026-05-13: Created Story 12.5 and marked ready-for-dev.
- 2026-05-13: Applied party-mode review hardening for evidence status, fail-closed release classification, stakeholder approvals, redaction proof, and scope guardrails.
- 2026-05-14: Applied advanced elicitation hardening for gate identity, stale evidence, auditable approvals, external evidence manifests, proxy sign-off authority, and machine-readable final classification.
- 2026-05-15: Added the dated release-candidate evidence pack, tightened manual accessibility evidence docs/templates, recorded fail-closed blocked classification, and moved the story to review.
- 2026-05-19: Code review via `/bmad-code-review 12.5`. 135 raw findings (50 Blind Hunter + 10 Acceptance Auditor + 75 Edge Case Hunter); 2 decisions resolved (CR-12-5-D1, CR-12-5-D2), 22 patches applied (P1-P22), 16 deferred to `deferred-work.md`, ~97 dismissed. CR-12-5-D1 tightened AC24 and D11 to four-role constraint approval (added Quality/Test). CR-12-5-D2 documented per-row `post-v1 roadmap` provisional rule in pack and README. P3 added within-file gate-id precedence rule. P6 split ROADMAP-BROAD-MEDIA-MATRICES into three rows (ROADMAP-ZOOM-MATRIX, ROADMAP-FORCED-COLORS-MATRIX, ROADMAP-REDUCED-MOTION-MATRIX). P5 added per-status required-fields matrix to the manual log template; P4 added delegation/proxy fields. Final release classification remains `blocked`; residual gates unchanged.

### File List

- `_bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/accessibility-verification/README.md`
- `docs/accessibility-verification/manual-log-template.md`
- `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md`

### Review Findings

_Source: `/bmad-code-review 12.5` run on 2026-05-19. Raw input: 135 findings (50 Blind Hunter + 10 Acceptance Auditor cross-cutting + 75 Edge Case Hunter). After dedup/triage: 2 decisions, 20 patches, 16 deferred, ~97 dismissed/duplicated. Acceptance Auditor verdict: 21 ACs MET, 2 PARTIAL (AC10, AC31 — schema gaps masked by `blocked` vacancy), 12 N/A (conditional triggers not fired because pack honestly classifies as `blocked`), 0 MISSED. Decisions: 17 MET, 3 PARTIAL (D5/D17/by-vacancy)._

#### Decisions

- [x] [Review][Decision] **CR-12-5-D1 — Stakeholder approver count for accepted constraints (AC24 vs AC28).** AC24 names 3 approvers (Product, Accessibility/Stakeholder, Release Owner) for an `accepted v1 constraint`. AC28 names 4 sign-offs (Product, Quality/Test, Release Owner, Accessibility/Stakeholder) when classification is affected. The pack's Manual Screen-Reader Matrix `Decision needed` cells use the 3-role text from AC24 while the Stakeholder Acceptance table tracks 4 roles. Decide: (a) align AC24 to 4 approvers (require Quality/Test for constraint approvals too), (b) keep 3-role rule for constraint approval but 4-role for release-readiness sign-offs and document the distinction, or (c) collapse AC28 to 3 roles. Spec ref: AC24, AC28, D11. Source: EC-54.
- [x] [Review][Decision] **CR-12-5-D2 — Per-row approvals for `post-v1 roadmap` classifications (AC23).** Required gates per pack line 59 (cross-AT, localization, RTL, zoom, forced-colors, reduced-motion) are classified `post-v1 roadmap` without recorded Product/Accessibility/Release Owner per-row approvals. The overall pack final classification is `blocked` so AC23 is satisfied transitively. Decide: (a) accept that the overall `blocked` classification subsumes per-row roadmap approvals, (b) reclassify each ROADMAP-* row to `blocked` until per-row approvals exist, or (c) record per-row approval refs before any non-blocked classification can be claimed. Spec ref: AC23, AC25, D12. Source: EC-19, EC-44, EC-66.

#### Patches

- [x] [Review][Patch] **CR-12-5-P1 [HIGH] Status enum casing drift in template** — template lists Title Case ("Completed / Not performed / Blocked / Accepted v1 constraint / Post-v1 roadmap"), contract uses lowercase. Operators will produce values that fail string-equality with canonical YAML enum. [docs/accessibility-verification/manual-log-template.md:15] (EC-01)
- [x] [Review][Patch] **CR-12-5-P2 [HIGH] README "Required fields" list mis-applies fields across statuses** — "Pass or fail" listed as required for every log but only applies to `completed`; Owner/Release impact/Reopen event are required for non-completed but currently presented as Story-12.5-specific optionals. Restructure into per-status required-fields. [docs/accessibility-verification/README.md:16-40] (EC-04, EC-05, EC-65)
- [x] [Review][Patch] **CR-12-5-P3 [HIGH] Duplicate gate ID within same file with no precedence rule** — `ROADMAP-CROSS-AT-EXPANSION` (and 3 siblings) appears in both Broader Accessibility Classification (line 84) and Post-v1 Roadmap Register (line 103). Duplicate Status Reconciliation section only addresses cross-file duplicates. Add within-file precedence rule OR consolidate to one canonical table. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:84,103,145-147] (EC-17, EC-50)
- [x] [Review][Patch] **CR-12-5-P4 [HIGH] Template lacks AC34/D19 delegation/proxy fields** — no `delegation_authority`, `delegation_scope`, `expiration`, `approving_role` fields. A proxy sign-off cannot be captured per AC34 by following the template alone. [docs/accessibility-verification/manual-log-template.md] (EC-30)
- [x] [Review][Patch] **CR-12-5-P5 [HIGH] Canonical-status template doesn't enforce per-status required fields** — selecting `not performed` has no `reason` slot; selecting `accepted v1 constraint` has no `downstream_consumer_impact` / `adopter_communication_need` slots; selecting `post-v1 roadmap` has no `story_or_roadmap_ref` / `target_release_or_nonplanning_rationale` slots. Add per-status required-fields matrix at template top OR split into per-status templates. [docs/accessibility-verification/manual-log-template.md] (EC-02, EC-06, EC-07, EC-08)
- [x] [Review][Patch] **CR-12-5-P6 [HIGH] zoom / forced-colors / reduced-motion collapsed into one roadmap row** — line 59 names them as 3 separate required gates; line 87 collapses to one ROADMAP-BROAD-MEDIA-MATRICES row. AC9 requires "each area" classified. Split into ROADMAP-ZOOM-MATRIX, ROADMAP-FORCED-COLORS-MATRIX, ROADMAP-REDUCED-MOTION-MATRIX. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:84-87] (EC-18, EC-44)
- [x] [Review][Patch] **CR-12-5-P7 [MEDIUM] Stakeholder Acceptance: duplicate Status / Final decision columns** — `Status: blocked` and `Final decision: Blocked` for every row — same dimension twice, mis-cased. Remove "Final decision" column. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:110-115] (Blind#9, EC-59)
- [x] [Review][Patch] **CR-12-5-P8 [MEDIUM] Feedback enum drift "blocking" vs canonical `blocked`** — Open Feedback Register uses "blocking"; gate Evidence Status Contract uses `blocked`. Per AC13 the open-feedback enum is intentionally separate ("blocking, accepted constraint, post-v1 roadmap, non-action decision"). Document the dual-enum convention prominently in README/contract or align. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:119-126] (EC-39)
- [x] [Review][Patch] **CR-12-5-P9 [MEDIUM] Stakeholder Acceptance "Open feedback" column doesn't classify per AC13** — free-form text ("Missing product acceptance") without an AC13 enum classification. Add classification column or reference Open Feedback Register IDs. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:110-115] (EC-60)
- [x] [Review][Patch] **CR-12-5-P10 [MEDIUM] YAML key plural drift `post_v1_roadmap_refs` vs `story_or_roadmap_ref`** — YAML key is plural, contract per-row field is singular; schema validator cannot reconcile. Document or align. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:37] (EC-46)
- [x] [Review][Patch] **CR-12-5-P11 [MEDIUM] Accepted Constraints Register missing "likelihood" column** — AC10 and D5 require accepted-constraint records to include "likelihood, impact, downstream consumer impact". Pack columns omit "Likelihood" and subsume "impact" into "Release impact". Add Likelihood column. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:93] (A-02)
- [x] [Review][Patch] **CR-12-5-P12 [MEDIUM] Real-Device Matrix vs Screen-Reader Matrix schema drift** — Real-Device has 11 columns including "UX commitment"; Screen-Reader has 10 columns (no UX baseline) although template at line 264 requires `UX baseline or responsive tier:`. Add UX baseline column to Screen-Reader Matrix. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:351-357] (Blind#8, Blind#32, EC-69)
- [x] [Review][Patch] **CR-12-5-P13 [MEDIUM] Blocker_ref tautology** — Manual Screen-Reader Matrix rows use "Story 12.5" as blocker_ref but Story 12.5 IS the pack being reviewed. Per AC23 blocker_ref must point to a decision/fix. Replace with a meaningful pointer (e.g., "No Quality team manual-AT session scheduled"). [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:355-357] (EC-55, EC-73)
- [x] [Review][Patch] **CR-12-5-P14 [MEDIUM] "Missing" used as approver/date value, neither in enum** — Stakeholder rows write `Approver: Missing` / `Date: Missing` — not canonical sentinels; future parsers expecting ISO dates will choke. Document and use a canonical sentinel (e.g., `unassigned` for approver, omit date or use `pending`). [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:112-115] (EC-15)
- [x] [Review][Patch] **CR-12-5-P15 [MEDIUM] Accepted Constraints Register "None"-row contradicts empty YAML list** — line 95 has "None / None / None approved" placeholder row while YAML `accepted_constraints: []` is correctly empty. Two empty-state idioms in same pack. Remove placeholder row or document the dual convention. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:36,93-95] (Blind#11, Blind#46, EC-16, EC-49)
- [x] [Review][Patch] **CR-12-5-P16 [LOW] Template "Decision needed:" field unconditional** — contract requires it only for `blocked`; template lists it for every status. Annotate as conditional. [docs/accessibility-verification/manual-log-template.md:21] (Blind#25, EC-36)
- [x] [Review][Patch] **CR-12-5-P17 [LOW] Template "Approval reference" singular masks plural case** — contract uses `approval_ref` (singular) for `completed` and `approval_refs` (plural) for `accepted v1 constraint`. Template's one singular field hides the 3-ref requirement for accepted constraints. Pluralize and annotate. [docs/accessibility-verification/manual-log-template.md:24] (Blind#5, EC-03)
- [x] [Review][Patch] **CR-12-5-P18 [LOW] AI agent identity allowed as tester** — `Prepared by: GPT-5 Codex` is fine for the pack but the README/template doesn't prohibit `tester_or_reviewer: <AI agent>` on a manual AT/device `completed` row. D9 requires manual evidence. Add explicit note: AI-agent identifiers are valid as `Prepared by` only, never as `tester_or_reviewer` for manual `completed` rows. [docs/accessibility-verification/README.md, docs/accessibility-verification/manual-log-template.md] (Blind#7, Blind#31, EC-48)
- [x] [Review][Patch] **CR-12-5-P19 [LOW] Adopter Communication lacks audit metadata** — bullets at lines 419-423 enumerate what to communicate but no owner / channel / target date / evidence. AC24 requires explicit classification. Restructure as table with Owner, Channel, Target date, Evidence ref. [docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:419-423] (Blind#12, EC-62)
- [x] [Review][Patch] **CR-12-5-P20 [LOW] Sprint-status.yaml `last_updated` carries release-classification prose** — line 178 says "final release classification is blocked pending..." — a fourth location for the classification claim, unprotected by AC35 single-source rule. Trim the classification claim from sprint-status prose; the evidence-pack YAML is canonical. [_bmad-output/implementation-artifacts/sprint-status.yaml:178] (Blind#40, EC-41)

#### Deferred

_Routed to `_bmad-output/implementation-artifacts/deferred-work.md` under "Deferred from: code review of 12-5-accessibility-and-stakeholder-acceptance-evidence-pack (2026-05-19)"._

- [x] [Review][Defer] **CR-12-5-Def01** Submodule pointer change for Hexalith.EventStore in commit 3f707c2 is outside story scope; can't be unmade and subsequent commits also touched submodules — deferred, pre-existing process pattern (A-01)
- [x] [Review][Defer] **CR-12-5-Def02** Per-artifact `sanitization_status` (vs single bundled string) in YAML — deferred, pick up when first external evidence artifact is added (EC-23)
- [x] [Review][Defer] **CR-12-5-Def03** `evidence_id` distinct from `gate_id` in template — deferred, pick up alongside first `completed` row (EC-68)
- [x] [Review][Defer] **CR-12-5-Def04** `environment` object aggregation in manual evidence rows — deferred, schema improvement for structured data (EC-69)
- [x] [Review][Defer] **CR-12-5-Def05** Browser/OS/AT version and date format constraints — deferred, pick up when first manual evidence row is recorded (EC-27, EC-28)
- [x] [Review][Defer] **CR-12-5-Def06** External-link rot detection / trusted-domain rule for evidence refs — deferred, pick up when first external link is recorded (EC-31)
- [x] [Review][Defer] **CR-12-5-Def07** Staleness check enforcement mechanism — deferred, AC32 is rule-only without automation; pick up when first `completed` row is bound to a real release candidate (EC-26)
- [x] [Review][Defer] **CR-12-5-Def08** Cross-pack precedence rule when multiple `release-candidate-*` packs exist — deferred, pick up when second pack is created (EC-10, EC-67)
- [x] [Review][Defer] **CR-12-5-Def09** Schema example for `ready` / `ready-with-accepted-constraints` packs — deferred, only `blocked` shape is exemplified today (EC-53)
- [x] [Review][Defer] **CR-12-5-Def10** `sanitization_status` free-form value validator — deferred, AC26 fail-closes on missing proof but accepts any string (EC-38)
- [x] [Review][Defer] **CR-12-5-Def11** Automated check that prose final classification matches YAML — deferred, currently one-time manual check (EC-33)
- [x] [Review][Defer] **CR-12-5-Def12** YAML schema validation for the machine-readable block — deferred, only `rg` keyword count today (EC-32)
- [x] [Review][Defer] **CR-12-5-Def13** Vague reopen events for ROADMAP-* rows ("New supported AT/browser policy, adopter request, or release-owner decision") aren't observable conditions — deferred, design reopen-event automation later (EC-21)
- [x] [Review][Defer] **CR-12-5-Def14** Stakeholder YAML `sign_off_refs: []` is opaque; can't distinguish 0-of-4 from 3-of-4 partial sign-off — deferred, pick up alongside first stakeholder sign-off (EC-13, EC-14, EC-51)
- [x] [Review][Defer] **CR-12-5-Def15** Dev Agent Record claims `ready-for-dev → in-progress → review` but diff only shows `ready-for-dev → review` — deferred, audit-trail loss minor (EC-70)
- [x] [Review][Defer] **CR-12-5-Def16** Internal markdown manifested as if external evidence; future automation may treat them as external artifacts — deferred, pick up when first external artifact is added (EC-57)
