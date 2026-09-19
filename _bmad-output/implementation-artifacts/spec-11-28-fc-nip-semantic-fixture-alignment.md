---
title: 'Story 11.28: FC-NIP Semantic Fixture Alignment'
type: 'bugfix'
created: '2026-09-19'
status: 'done'
baseline_commit: 'cffb1eb72e150f70d7e3ba26ee527ee622195415'
baseline_revision: 'cffb1eb72e150f70d7e3ba26ee527ee622195415'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
warnings: []
deferred:
  - summary: >-
      Reject duplicate exact table headings before parsing FC-NIP semantic tables.
    evidence: |-
      Both shared-manifest consumers locate only the first exact heading and validate that table. A later conflicting duplicate heading would be ignored, allowing documentation contradiction to escape the governance guard. This behavior predates Story 11.28 and is not caused by its fixture alignment.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipCommandTargetIdentityContractTests.cs:209; tests/e2e/specs/fc-nip-command-target-identity-contract.spec.ts:243
    severity: medium
---

<intent-contract>

## Intent

**Problem:** The shared FC-NIP semantic fixture formerly required obsolete current-PRD prose even though the canonical PRD records the D-4 successor decision, completed Stories 9.3-9.8, and the passing Story 9.8 live proof. Shared commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77` already repaired the fixture, but Story 11.28 lacks a dedicated attribution and verification record.

**Approach:** Attribute the existing language-neutral fixture correction to Story 11.28 and freshly verify both consumers. Preserve the canonical PRD and dated contracts as read-only sources rather than manufacturing a duplicate semantic edit.

## Boundaries & Constraints

**Always:** Keep one case-sensitive, whitespace-normalized JSON manifest as the shared C# and TypeScript source; require current D-4/delivery truth, immutable pre-dispatch command-target identity, independent typed materiality, the server-allocated-key non-goal, and explicit rejection of the obsolete current-PRD blocker sentence. Preserve manifest schema/path safety and exact-table assertions.

**Never:** Rewrite the canonical PRD or historical FC-NIP contracts to satisfy fixture literals; reopen runtime/design behavior; claim G-5 administrative approval; close DW-679; alter `sprint-status.yaml`; or duplicate the already-landed fixture change without demonstrated drift.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Current delivery truth | Canonical PRD records D-4 delivery and Stories 9.3-9.8 live proof | Both consumers find every current positive fragment | Missing or changed truth fails both guards |
| Obsolete blocker prose | Current PRD contains the old `Stories 9.4-9.8 still block` sentence | Both consumers reject the document | Any reintroduction fails closed |
| Server-allocated key | Target key exists only after dispatch | Fixture retains the v1.0 non-goal and no-marker outcome | Do not infer or publish a target |

</intent-contract>

## Code Map

- `tests/contract-fixtures/fc-nip-command-target-identity-contract.json:143` -- shared language-neutral fixture; lines 144-156 pin current PRD truth and reject the obsolete blocker, while lines 253-281 preserve exact semantic tables.
- `_bmad-output/planning-artifacts/prd.md:310` -- read-only canonical FR-13 truth; FR-26 completion is at line 487 and D-4 at line 678.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipCommandTargetIdentityContractTests.cs:17` -- Governance-tagged C# consumer and manifest structural validator.
- `tests/e2e/specs/fc-nip-command-target-identity-contract.spec.ts:18` -- browserless TypeScript consumer of the same manifest.
- `tests/e2e/package.json:31` -- focused `test:fc-nip` entry point.
- `.github/workflows/quality.yml:708` -- blocking browserless FC-NIP workflow step; the C# Governance lane is at lines 126-137.
- `_bmad-output/implementation-artifacts/spec-actions-34817507610-fix-cicd-release.md:46` -- prior shared implementation/evidence record for commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77`.

## Tasks & Acceptance

**Execution:**
- [x] `tests/contract-fixtures/fc-nip-command-target-identity-contract.json` -- verify the shared commit's positive and negative fragments still match canonical PRD truth; edit only if fresh verification proves drift.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipCommandTargetIdentityContractTests.cs` -- run the focused Governance class against the shared fixture.
- [x] `tests/e2e/specs/fc-nip-command-target-identity-contract.spec.ts` -- run the browserless Playwright guard against that same fixture.
- [x] `_bmad-output/implementation-artifacts/spec-11-28-fc-nip-semantic-fixture-alignment.md` -- record shared-commit attribution and current verification without touching orchestrator bookkeeping.

**Acceptance Criteria:**
- Given the canonical PRD records D-4 delivery, Stories 9.3-9.8 done, and the Story 9.8 live proof, when the shared FC-NIP fixture is evaluated, then its positive and negative fragments pin those outcomes together with explicit command-target identity, typed materiality, and the server-allocated-key non-goal.
- Given obsolete current-state prose such as `Resolved 2026-08-12` or `Stories 9.4-9.8 still block`, when the fixture is reviewed, then it does not require that prose and the canonical PRD remains unchanged.
- Given the aligned language-neutral fixture, when the focused C# Governance class and browserless `test:fc-nip` suite run, then both pass without suppression.

## Implementation Notes

- Confirmed `2cd0496a9bcd3c01e03aa0850a08d67d95999e77` is an ancestor of baseline `cffb1eb72e150f70d7e3ba26ee527ee622195415` and contains the complete fixture correction attributed to Story 11.28.
- Compared the shared fixture with the canonical PRD through both case-sensitive, whitespace-normalized consumers. The current D-4 delivery, Stories 9.3–9.8 completion/live proof, immutable pre-dispatch command-target identity, independent typed materiality, server-allocated-key non-goal, and obsolete-blocker rejection all remain aligned.
- No fixture, consumer, canonical PRD, historical contract, or sprint-status edit was necessary; fresh verification demonstrated no semantic drift.

## Spec Change Log

- 2026-09-19: Attributed the existing FC-NIP fixture correction to shared commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77`, confirmed no drift at baseline `cffb1eb72e150f70d7e3ba26ee527ee622195415`, and recorded fresh C# and TypeScript verification.

## Review Triage Log

### 2026-09-19 — Review pass
- verdicts: 14 findings — high 0, medium 4, low 3, false 7, maybe-false 0
- findings:
  - `[false]` `[reject]` The intent's substantive expectation lives in the shared manifest while the reviewed diff adds only the story artifact — the aligned manifest is already present in ancestor commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77`; duplicating that semantic edit is not required.
  - `[false]` `[reject]` The story's named consumer surface is unchanged by this diff — this is intentional because the existing C# and TypeScript consumers already exercise the one corrected shared manifest and passed 7/7 and 3/3.
  - `[false]` `[reject]` The reviewed change surface is only the implementation-spec Markdown file — the invocation permits state-aware completion, and repository history proves the substantive fixture delta predates this attribution pass.
  - `[false]` `[reject]` The focused tests do not validate the new story artifact or its commit attribution — those tests validate the story's semantic acceptance surface; the full ancestor SHA and `git merge-base --is-ancestor`/`git show` evidence establish attribution separately.
  - `[false]` `[reject]` The substantive alignment is outside the current baseline diff — the cited ancestor commit contains the exact required fixture correction, so a second content-identical edit would manufacture drift rather than fulfill intent.
  - `[false]` `[reject]` Workflow execution, subagent use, the final commit, and Auto Run Result are not evidenced by the pre-finalization diff — these are execution/finalization obligations, not semantic fixture surfaces, and are completed outside the reviewed implementation delta.
  - `[medium]` `[reject]` The Commit Scope Dispositions row is mechanically malformed and its ancestor SHA is outside `baseline..candidate` — the focused validator reproduces the failure, but the review rule rejects findings whose fix edits this build's spec; the historical attribution remains independently recorded in Implementation Notes.
  - `[medium]` `[reject]` The numbered story artifact has no File List — `eng/validate-story-artifacts.py` reproduces the unreconciled story-file failure, but the review rule rejects findings whose fix edits this build's spec.
  - `[medium]` `[reject]` Three checked verification tasks name unchanged paths and fail task-evidence reconciliation — the focused validator reproduces all three failures, but the review rule rejects findings whose fix edits this build's spec; the commands and results remain explicit under Verification.
  - `[low]` `[reject]` The listed `git diff --check` command alone does not reproduce the recorded staged/no-index whitespace check — this is a spec-only reproducibility wording issue, while parent verification ran both `git diff --check` and `git diff --cached --check`; the review rule rejects a spec edit.
  - `[false]` `[reject]` Story completion conflicts with orchestrator backlog/open bookkeeping — the user explicitly declared orchestrator row state neither a defect nor verification evidence, and `sprint-status.yaml` is intentionally untouched.
  - `[low]` `[reject]` .NET and JavaScript whitespace normalization differ for rare characters such as U+0085 — the current ASCII-authored governed documents do not trigger the mismatch, and adding cross-runtime Unicode normalization machinery is disproportionate to this uncommon pre-existing case.
  - `[medium]` `[defer]` Both consumers accept the first of duplicate exact table headings and can ignore a conflicting later table — verified at the first-match searches in the C# and TypeScript parsers; this pre-existing governance weakness is recorded in frontmatter for separate ownership.
  - `[low]` `[reject]` The focused build uses `--no-restore` and can fail in a checkout without restored assets — active development verification has restored assets, and making every focused command bootstrap its toolchain is disproportionate to this precondition-only risk.

## Commit Scope Dispositions

- `2cd0496a9bcd3c01e03aa0850a08d67d95999e77` | shared | This ancestor CI-repair commit contains the complete Story 11.28 fixture delta and its prior evidence; the dedicated story pass verifies and attributes it instead of duplicating the edit.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false -p:NuGetAudit=false` -- expected: focused test assembly builds with zero warnings and errors.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipCommandTargetIdentityContractTests` -- expected: 7/7 pass.
- `npm --prefix tests/e2e run test:fc-nip` -- expected: 3/3 pass against the shared manifest.
- `git diff --check` -- expected: no whitespace errors.

**Results:**
- Focused Debug build succeeded with 0 warnings and 0 errors.
- Focused C# Governance class passed 7/7 with no failures, skips, or suppressed tests.
- Browserless Playwright FC-NIP suite passed 3/3 with no failures or skips.
- `git diff --check` and the equivalent no-index check for the untracked story spec reported no whitespace errors.

## Auto Run Result

Status: done

**Summary:** Attributed the already-landed FC-NIP semantic-fixture correction in shared ancestor commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77` to Story 11.28 and freshly proved the current fixture against both language-neutral consumers. No duplicate fixture, PRD, contract, runtime, test, or orchestrator-bookkeeping edit was made.

**Files changed:**
- `_bmad-output/implementation-artifacts/spec-11-28-fc-nip-semantic-fixture-alignment.md` -- dedicated Story 11.28 intent, provenance, execution evidence, review triage, verification results, and completion result.

**Review findings:**
- Patches applied: 0 (high 0, medium 0, low 0).
- Deferred: 1 medium pre-existing item -- reject duplicate exact table headings before either shared-manifest consumer parses the first table.
- Rejected: the six intent-audit surface distinctions because state-aware completion is valid and the substantive ancestor delta is independently evidenced; the malformed disposition, missing File List, unchanged-path task-evidence, and whitespace-command wording findings because their remedies edit this build's spec and the review rule requires rejection; the sprint-status mismatch because orchestrator bookkeeping is explicitly non-evidence and read-only; the U+0085 normalization mismatch because it is an uncommon pre-existing low-risk case whose repair is disproportionate; and clean-checkout restore bootstrapping because restored assets are an ordinary focused-verification precondition.

**Follow-up review recommendation:** false -- this pass applied no patches; patched counts are high 0, medium 0, low 0.

**Verification performed:**
- Focused SourceTools test-project Debug build: passed with 0 warnings and 0 errors.
- `FcNipCommandTargetIdentityContractTests`: 7/7 passed with 0 skipped.
- Browserless `test:fc-nip`: 3/3 passed with 0 skipped.
- Matrix audit: all three rows were exercised by both shared-manifest consumers.
- `git diff --check` and `git diff --cached --check`: passed.
- Frontmatter YAML parse: passed with one preserved deferred entry.

**Residual risks:** Duplicate exact table headings remain a pre-existing fail-open documentation-governance case. The focused story-artifact validator also rejects the spec-only disposition/File List/task-evidence shape; review policy barred spec-only repair findings, so those results are retained transparently in the triage log rather than hidden or misrepresented.
