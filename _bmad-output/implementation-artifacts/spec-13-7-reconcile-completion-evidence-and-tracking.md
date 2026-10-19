---
title: 'Story 13.7: Reconcile completion evidence and tracking'
type: 'chore'
created: '2026-10-19'
status: 'done'
route: 'oneshot'
artifact_role: 'supporting-completion-record'
primary_story: '_bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md'
story_id: '13.7'
story_key: '13-7-i-ux-f-provide-reusable-ux-assertions-for-adopters'
baseline_commit: 'ee9762e6c74914ccd33ec1d2caf322bdc21a10b4'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.7's existing implementation spec says done while sprint tracking says review. The required strict artifact validator rejects its missing File List, abbreviated task paths and historical commit attribution, so the recorded completion is not supported by the repository's gate.

**Approach:** Revalidate the existing Testing implementation, correct the original artifact's evidence paths and inventory, and reconcile its status with the actual gate result. Preserve the original frozen intent and historical evidence. Keep the story in review whenever strict reconciliation fails; record exact failures without changing commit history, claiming unrelated files as story output, changing validators or closing independently owned approvals.

</frozen-after-approval>

## Implementation Notes

- Investigation: the existing helpers, API baseline, consumer tests and adopter documentation are delivered. No missing implementation was identified. This follow-up changes only completion records; it introduces no runtime behavior or public API.
- Intent gaps: none. Irreversible actions: none. Footprint: this follow-up spec, the existing Story 13.7 spec and one appended deferred-work entry. The sprint row already says review and needs no edit unless the strict gate passes.
- Reuse the full baseline `b9e1490cfe1c51406d3c8d4dc213971d06911a24` in the original artifact. Do not replace it to omit intervening commits. Current candidate is `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4`.
- Prior implementation delivery is commit `7f57da411c3abf8e1d459d0611c49632c07393a7`; its recorded final tests were 185/185. Treat that as historical evidence, and report fresh test counts separately.
- Fresh Debug build of `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` passed with 0 warnings and 0 errors. Full build command and log: `/tmp/fc137-build-20261019/build-debug.log`.
- Required artifact command: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md --candidate HEAD`. Initial result: exit 1, missing inventory, eight task-evidence failures and interleaved commit `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4`; log `/tmp/fc137-build-20261019/artifact-initial.log`.

- Corrected the original spec's status (`in-review`), eight task entries and the 46-path delivery/completion inventory, including the maintainer handoff. The sprint row remains `review`; sync to `in-progress` is a no-op because the row is already later in the lifecycle.
- Fresh verification passed: focused Debug and Release builds, each 0 warnings/0 errors; native MTP Testing suite 185/185 with 0 skipped; direct Debug failure matrix 139/139. No test count delta from prior 185-case completion evidence.
- After correcting the inventory, the original strict gate still exits 1 on two historical commits: original delivery `7f57da411c3abf8e1d459d0611c49632c07393a7` is unmapped because its subject lacks the story ID; current candidate `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4` is interleaved because it includes 11 unrelated CI/release paths. All missing-inventory and task-path failures are resolved. Log: `/tmp/fc137-build-20261019/artifact-reconciled.log`.
- Completing this bounded evidence reconciliation does not complete Story 13.7. Its original implementation spec and sprint remain in review pending the strict history gate. Tests and scope/frozen intent are preserved; no source/dependency/validator/submodule change is required or made.

- Blind Hunter review checked the documentation changes; no additional layers were configured. Five findings were individually triaged below. The only deferred work is the pre-existing historical-attribution repair, now owned by the FrontComposer repository maintainer with a tracked retry condition.
- Finalization requires rerunning both strict artifact commands after the local reconciliation commit, obtaining the full canonical candidate with `git rev-parse HEAD`, and reporting their outcomes. The follow-up validator must pass. The original validator is expected to retain the two documented historical failures; that keeps Story 13.7 in review. Preserve the final logs under `/tmp/fc137-build-20261019` and record the canonical candidate/outcomes in the final handoff.

## Review Triage Log

| Finding | Verdict | Route | Evidence / disposition |
| --- | --- | --- | --- |
| BH1: another authoritative story record | low | patch | The Intent and notes already limit this to a bounded follow-up, while the original remains in review. Added explicit supporting-record and primary-story metadata to make that distinction visible in frontmatter as well. |
| BH2: attribution repair lacks an owner/action | medium | defer | Both historical commit errors predate this change and require maintainer authority outside this documentation scope. Appended one deferred-work entry naming the FrontComposer repository maintainer, the two exact commits, the preserved baseline, the repair decision and strict retry/closure condition. No published-history rewrite or policy change is authorized. |
| BH3: no durable evidence summary | false | reject | The tracked original spec's Current-candidate verification table already retains every required command, outcomes, exact 185/139 counts and zero test-count delta; its completion section retains both historical blocker identities. Temporary raw logs are additional local diagnostics, not claimed portable or immutable evidence. |
| BH4: missing Tasks/Acceptance invalidates verification | false | reject | The rendered Step 2 explicitly deletes Tasks and Acceptance for the oneshot route. Its passing validator does not claim feature completion; scope, original gate failure, inventory and fresh test evidence are explicitly recorded and checked separately. |
| BH5: post-commit validation not carried forward | low | patch | Added the mandatory two-validator post-commit check and canonical-candidate handoff before reporting this bounded follow-up complete. The original gate's failures remain visible and cannot be replaced by the follow-up's success. |

## File List

- `_bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md`
- `_bmad-output/implementation-artifacts/spec-13-7-reconcile-completion-evidence-and-tracking.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

The bounded reconciliation is finalized after review. Story 13.7 itself remains in review for the deferred maintainer-owned attribution repair. The final post-commit checks must confirm this distinction before the completion handoff.
