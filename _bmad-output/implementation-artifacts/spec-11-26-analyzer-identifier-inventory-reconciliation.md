---
title: 'Story 11.26: Analyzer Identifier Inventory Reconciliation'
type: 'chore'
created: '2026-09-15'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 11 still records Story 11.26 and retrospective action E11R-AI-2 as open even though the two-identifier CA1707 delta was intentionally introduced and the analyzer inventory was resealed from `3325` / `36c57e1b…` to `3327` / `e33cb6e8…`. Without a dedicated story record, the declarations, owning commits, and scope rationale are not tied to the Epic 11 closure trail.

**Approach:** Verify the exact two declarations against their logging-governance purpose and approved `tests/**.cs` CA1707 scope, confirm the current full seal and analyzer-policy gates, then record the evidence and reconcile Story 11.26 plus E11R-AI-2 without changing source, analyzer configuration, suppression scope, or the already-correct ledger seal.

</frozen-after-approval>

## Implementation Notes

- The last matching seal before the delta was commit `4bee05cab77e3aa0e56c90af1bb172d17ac72098` at `3325` / `36c57e1bc4956c297e82ca9614e224be3a2971ae922813099207ac4ffe4f5dd5`.
- Commit `1be6b3778b04dd321d5db314c3b0bbcc6979a593` intentionally added `FrontComposerMcpLogTests.Wrapper_EmitsOnlyWhenItsDeclaredLevelIsEnabled` to detect wrappers guarded by the wrong `LogLevel` and `FrontComposerMcpLogTests.WrapperContracts_CoverEveryPublicWrapper` to keep every public logging wrapper covered. Both are public underscore-named test methods under the approved `tests/**.cs` CA1707 scope, so retaining them is correct.
- Commit `2cd0496a9bcd3c01e03aa0850a08d67d95999e77` resealed only the test inventory to `3327` / `e33cb6e8c92dea244db0cebde30721da208a5a569a7a7cf9c1e41b7d77ee270f`; the Contracts inventory remains `89` / `9b81beacad8c65e481b2239b6054ba483dd8e9b6e6af2a0e51ca7065deb3210a`.
- Current policy remains `AnalysisMode=Recommended` and `TreatWarningsAsErrors=true` (`Directory.Build.props:32-34`). The only CA1707 severity entries remain the approved `tests/**.cs` convention and the exact `FcDiagnosticIds.cs` compatibility scope (`.editorconfig:70-77`); no source, analyzer package, or warning-control change was needed, and the ledger **seal** (counts and hashes) was not changed. The 2026-09-15 code review did extend the `naming-ca1707-test-convention` disposition's `evidence` string in `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`, because that disposition's own `trigger` ("Revalidate if ... the test source inventory drifts") had fired and the ledger recorded no rationale for the reseal. No key was added or removed and `decisionDate` was left at `2026-07-17`, so the frozen constraint against changing the seal holds.
- The delta is exclusively those two declarations: `git diff 4bee05cab77e3aa0e56c90af1bb172d17ac72098 2cd0496a9bcd3c01e03aa0850a08d67d95999e77 -- 'tests/*.cs'`, filtered to public/protected underscore-bearing declarations, yields exactly the two methods above and **zero removals**. This matters because a net `+2` between `3325` and `3327` would otherwise be equally consistent with an add-plus-remove pair.
- The reseal preceded this review. Commit `2cd0496a` changed the seal on 2026-09-14 inside a CI-restoration commit that does not mention CA1707; the review recorded here is dated 2026-09-15. The epic's second acceptance criterion orders review before reseal, so this artifact ratifies the reseal after the fact. The guard that criterion exists to enforce still held: both declarations are intended, and the seal matches the source.
- Verification (revision `4c181ab8b94e610ac2dcd91e30ce4bf2a12ca364`, plus the 2026-09-15 code-review edits in the working tree; xUnit v3 in-process runner, Release):
  - `./Hexalith.FrontComposer.Shell.Tests -method "*AnalyzerPolicy_IdentifierInventory_MatchesSeal*"` -- **1/1 pass**. This is the seal-critical symbol (`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:462`); it recomputes the inventory from the live tree via `ValidateIdentifierInventory(root, LoadLedger(root))` and compares it to the ledger, so it is the check that proves `3327` / `e33cb6e8...` still matches the source rather than merely restating the sealed value.
  - `./Hexalith.FrontComposer.Shell.Tests -class "Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests"` -- **8/8 pass** (117s), re-run after the ledger `evidence` edit to confirm the governance contract still validates.
  - `./Hexalith.FrontComposer.Mcp.Tests -method "*FrontComposerMcpLogTests.Wrapper_EmitsOnlyWhenItsDeclaredLevelIsEnabled*" -method "*FrontComposerMcpLogTests.WrapperContracts_CoverEveryPublicWrapper*"` -- **10/10 pass**. The two subject declarations live in the **MCP** test project (`tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FrontComposerMcpLogTests.cs:140,163`), not the Shell project; the count is 9 `[Theory]` rows over `WrapperContracts` (9 wrappers, `:25-44`) plus 1 `[Fact]`, not "10 data rows".
- Files changed by this story: `_bmad-output/implementation-artifacts/sprint-status.yaml` only, plus this spec. Transitions: `development_status['11-26-analyzer-identifier-inventory-reconciliation']` `backlog` -> `review`; retrospective action `E11R-AI-2` gained evidence and, after the 2026-09-15 code review, was held at `open` until Story 11.26 is accepted (see the triage log below).

## Review Triage Log

| Finding | Verdict | Evidence |
| --- | --- | --- |
| Missing `story_id` / `baseline_commit` | false | The one-shot template deliberately omits both fields. Story identity resolved unambiguously from the title, filename, Epic 11 context, and exact sprint key; the cited historical commits are provenance evidence rather than current commit-scope ownership. |
| Missing legacy `## File List` | false | Step 2 requires a one-shot spec to contain only frontmatter, frozen Intent, and Implementation Notes before review; adding the legacy section would violate the active workflow. The optional `validate-story-artifacts.py --story` command is therefore not an applicable gate for this preview-format artifact. |
| Verification lacks commands/date/revision | false | Story acceptance requires the current generated seal plus focused/default analyzer lanes. The notes record the exact test symbols/lanes, full seal, owning revisions, current dated artifact, and pass counts; no release or immutable candidate claim is made. |
| Analyzer Policy Owner reviewer is unnamed | false | The story requires the two declarations, owning changes, and in-scope rationale, all recorded with full commits and exact symbols. Neither the story nor the analyzer ledger requires a separate named reviewer receipt; sprint tracking retains the declared owner role. |
| Completion claim conflicts with `in-progress` | false | The reviewer inspected the required intermediate state before finalization. This finalization sets the spec to `done` and advances the sprint story to `review`, while E11R-AI-2 remains `done` because its implementation and verification are complete. |

### Code review pass, 2026-09-15

Four independent review layers plus triage. Rows below supersede the earlier verdicts they name.

| Finding | Verdict | Evidence |
| --- | --- | --- |
| `E11R-AI-2` closed ahead of its own acceptance gate | high | Corrected. It was the only `done` action item of the 15 carrying an `implementation_story`; `E11R-AI-1` sits four lines above, still `open`, though its Story 11.25 is `done`. `.claude/skills/bmad-retrospective/references/acceptance-verdict.md:23` records a follow-through line only for items "not already `done`", so closing it early removes it from the next retrospective's follow-through. Reverted to `open` with the closure rule recorded inline. Supersedes the "Completion claim conflicts with `in-progress`" row, whose claim that E11R-AI-2 "remains `done`" was wrong: the reviewed commit is what first set it. |
| Governed ledger carried no rationale for the `+2` reseal | medium | Corrected. The `naming-ca1707-test-convention` disposition's `trigger` is "Revalidate if the repository test naming convention changes or the test source inventory drifts"; the inventory drifted `3325` -> `3327` and this story is that revalidation, yet the disposition was untouched while the ledger annotates drift inline elsewhere (`refreshedCensus.driftFromBaseline`). Extended `evidence` only; no key added, `decisionDate` unchanged; `AnalyzerPolicyGovernanceTests` 8/8 after the edit. |
| `validate-story-artifacts.py` is not an applicable gate | false (supersedes the earlier `false`) | The earlier rows reached the right outcome on the wrong reasoning. `docs/reference/story-artifact-validation.md:29-32` (status: published) names "a freeform spec, a missing, `NO_VCS`, or unresolvable `baseline_commit`" as exactly what legacy mode is for, and scopes the requirement to numbered story artifacts, which this is; siblings 11.24, 11.25 and 11.2 all carry `baseline_commit` and record a run. The gate is in scope and it **fails**: legacy mode exits 1 (`story File List not found or empty`), and adding `story_id` + `baseline_commit` to a probe copy does **not** fix it -- strict mode then exits 1 on the File List plus `interleaved story commit ... touches unowned paths`. It cannot pass while `.claude/skills/bmad-build/step-02-plan.md:20` requires a one-shot spec to keep "only the frontmatter, `## Intent` ... and `## Implementation Notes`. Delete every other section." That is a structural collision between two repository contracts, not a defect in this story; recorded here and routed to E11R-AI-8 / Story 11.32 (Epic 11 artifact integrity enforcement). |
| Verification record was not reproducible | medium | Corrected in the Implementation Notes above: exact commands, revision, the seal-critical symbol `AnalyzerPolicy_IdentifierInventory_MatchesSeal`, the owning test project for each lane, the delta-exclusivity proof, and the files this story changed. Supersedes the "Verification lacks commands/date/revision" row, whose refutation claimed the notes "record the exact test symbols/lanes" when only the class name appeared. |
| `epics.md` story status stale; retro trail unreconciled | low, deferred | `epics.md:2392` says `**Status:** backlog` for Story 11.26 -- but so does every story from 11.20 on, while sprint-status records 11.20-11.25 as `done`. Pre-existing repo-wide planning-snapshot staleness this story neither introduced nor worsened. Deferred as DW-1958, owned by E11R-AI-8 / Story 11.32. |
