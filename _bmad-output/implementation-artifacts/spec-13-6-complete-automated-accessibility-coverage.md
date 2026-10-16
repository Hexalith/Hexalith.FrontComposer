---
title: 'Story 13.6: Complete automated accessibility coverage'
type: 'feature'
created: '2026-10-16'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c3708690f8e1dbebfb49682c67a8aba88fd4a092'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 has passing focused checks but lacks real zero-field browser failures, generated-detail removal integration, complete observable state/theme coverage and cross-browser execution.

**Approach:** Complete those automated checks through existing gated Counter specimens and browser helpers, run available engines and the configured Shell lane, and refresh one candidate-bound result before human validation. Run the requested UX evidence review.

## Boundaries & Constraints

**Always:** Preserve the approved 13.6 decisions, Fluent V5 ownership, exact default density/typography/rail/measure contracts, tenant isolation and single-owner speech. Scenario controls use fixed safe data and existing Test/specimen gates; ordinary routes and six visual baselines keep their content. Record every state/AE row, executed result, reuse and external blocker precisely. Keep manual AT, actual zoom, tablet/phone and human approvals open. UX evidence review cannot supply those observations.

**Never:** Add backend workflows, public testing APIs, dependency upgrades, arbitrary failure text, independent palettes or axe exclusions. Infer heard speech, actual zoom or physical devices from automation. Infer separate OI-16/G-4/Fluent/Product/live-fresh-row approval. Auto-push or claim Windows execution on Linux.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
|---|---|---|---|
| Zero-field | Safe validation, returned/exception rejection, forbidden/unavailable/rate-limit/transport, admission/policy denial, acceptance | Visible safe feedback, retained trigger focus, one automation-observed owner, no pre-accept retry; next attempt clears, accepted uses pending summary | Fix actual generated path; retain failed proof |
| Detail removal | Two real generated rows; focused detail; matching completed result removes it | Focus non-live neutral explanation only after focus loss; external focus survives; repeated results silent; filter-only path distinct | Cancel stale scope/navigation recovery |
| AE state matrix | Each changed surface/state in light/dark, 320px plus spacing, forced colors/reduced motion | 4.5:1 normal text, 3:1 large/nontext/focus; targets/whole focus visible; semantics and immediate text/icon/current-state cues survive | No vacuous samples or unlisted omissions |
| Platforms | Chromium/Firefox/WebKit locally; existing Windows CI | Execute supported checks; Windows command includes new specs and artifacts | Exact blocker/owner/command; no fabricated pass |

</frozen-after-approval>

## Code Map

- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor` owns existing query-gated fixtures and real `CounterProjectionView`; `CounterProjectionTemplate.razor` exposes `Context.DefaultBody` under `generatedGrid=true`.
- `samples/Counter/Counter.Specimens.Domain/` generated specimen commands; `samples/Counter/Counter.Web/Program.cs`, `CounterSampleCommandService.cs` gate registries/policies and Test-only opt-in failures. Reuse derived fields and scoped fixed scenario state.
- `samples/Counter/Counter.Web/CounterProjectionEffects.cs`: matching LoadRequested/Loaded actions carry scope/generation; complete real results, never set banner flags directly.
- `tests/e2e/helpers/responsive-accessibility.ts`, `specs/responsive-accessibility.spec.ts`, `fixtures/lifecycle.fixture.ts`: reuse geometry/spacing/keyboard tools; measure actual composed-tree text, borders/icons/focus. Real wrapper states exceed the fixture's six-state list.
- `.github/workflows/quality.yml` already runs three Linux engines and Windows `npm run test:a11y`; extend that script in `tests/e2e/package.json` and existing artifact validation, preserve workflow names.

## Tasks & Acceptance

**Execution:**
- [ ] `samples/Counter/Counter.Specimens.Domain/`, `samples/Counter/Counter.Specimens/`, `samples/Counter/Counter.Web/` — expose real generated zero-field and projection-result scenarios through existing opt-in gates; cover production gate rejection and safe admission/policy seams.
- [ ] `tests/e2e/specs/responsive-accessibility.spec.ts`, new `tests/e2e/specs/accessibility-state-matrix.spec.ts`, `tests/e2e/helpers/responsive-accessibility.ts`, `tests/e2e/fixtures/lifecycle.fixture.ts` — cover shell/Home/nav, toolbar/tabs/popovers, projection/detail/states, generated forms/lifecycle, destructive/abandonment dialogs and palette/settings. Reuse existing journeys; assert text/nontext/focus contrast, actual mode cues, targets, clipping, keyboard and single-owner DOM sequences. Add negative fixtures for new measurement logic.
- [ ] `tests/e2e/package.json`, `tests/e2e/scripts/validate-a11y-artifacts.mjs` — include coverage in existing Windows lane and validate required artifacts without weakening gates.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, `evidence/story-13-6/`, original 13.6 spec — refresh surface/state/AE mapping, candidate hashes and honest platform/manual status. Preserve earlier evidence attribution; record UX review and finding dispositions.

**Acceptance Criteria:**
- Given gated real generated components, when the scenario matrix runs, then every listed outcome/surface has nonempty passing assertions or an explicit external execution blocker.
- Given available browser engines and configured Shell tests, when verification finishes, then exact current results and CI follow-ups bind the final candidate; manual and separate owner gates remain truthful.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Serialized Debug builds of Counter.Web and affected test projects; direct xUnit v3 affected classes and configured Shell lane excluding existing GovernanceBuild/Performance/e2e-palette/NightlyProperty/Quarantined traits. Record live dependency skips separately; do not run Release-spawning governance builds locally.
- `npm --prefix tests/e2e run typecheck`; new/affected specs on Chromium, Firefox, WebKit against rebuilt Debug Test host, existing visual comparisons and relevant journeys. Windows execution belongs to its existing hosted job.
- Existing artifact/governance validators, `git diff --check`, independent candidate hashes; scoped headless UX Validate accessibility/Fluent lenses, with all Critical/High findings dispositioned. Human checklist stays open.
