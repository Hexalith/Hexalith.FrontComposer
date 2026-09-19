---
title: 'Fix AppHost MSBuild Result Output Boundary'
type: 'bugfix'
created: '2026-09-19'
status: 'completed'
route: 'oneshot'
review_loop_iteration: 2
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The authenticated AppHost smoke requests a large, authoritative MSBuild evaluation document, but the generic command boundary retains only the final 1 MiB of stdout. Current GitHub-hosted output exceeds that bound, discards the JSON prefix, and fails Gate 2c as `apphost-msbuild-output-invalid` before any credential-bearing or host-start operation.

**Approach:** Redirect MSBuild `-getProperty`/`-getItem` results into an exclusive temporary result file, validate that file through a bounded duplicate-safe JSON read, and preserve bounded process output only for redacted diagnostics. Reject missing, malformed, symlinked, changed-during-read, or oversized result files and cover the live large-output regression without relaxing exact source-graph validation.

</frozen-after-approval>

## Implementation Notes

- Measured the exact AppHost `ResolveReferences` get-result document at 1,473,568 bytes with 352 `ReferencePath` items; the former 1,048,576-character stdout tail cannot contain the authoritative JSON object.
- Added an 8 MiB bounded, duplicate-safe MSBuild result-file boundary backed by an exclusive temporary directory. Process stdout remains bounded and is used only for redacted diagnostics.
- Updated injectable test runtimes to honor `-getResultOutputFile`, added the large-output regression and missing/malformed/symlinked/oversized rejection cases, and pinned the result-file/bound contract in governance source assertions.
- Verified the same patch against an isolated detached clone of current `origin/main` at `4b5a57cacc270ac28d334c659b1a36f87096fdce`; focused current-origin tests pass. Full capture remains unavailable locally because its runtime-input authority intentionally rejects the permitted sibling symlinks and nested dependency materialization is prohibited.
- Blind review found that live `origin/main` had independently raised retained MSBuild stdout to 4 MiB. The regression now exceeds that actual MSBuild-specific boundary while remaining beneath the independent 8 MiB result-file bound.
- Hardened the shared bounded reader with no-follow descriptor opening and before/after descriptor plus pathname metadata comparisons. Focused tests reject same-size in-place mutation and pathname replacement, accept exactly the byte limit, and reject limit plus one.
- Propagated sanitized, distinct MSBuild result failure categories for missing, malformed, duplicate-member, symlinked, oversized, and changed-during-read results instead of collapsing all failures into `apphost-msbuild-output-invalid`.
- The first merged live Quality run (`35447465188`, source `42ca0abbf1d6115a220f48493a6a24e44e57b299`) stopped in Gate 2b because its governance assertion expected a literal closing quote immediately after `-getResultOutputFile:`. The assertion now matches the actual interpolated option and also pins the no-follow/race/category controls.
- The follow-up live Quality run (`35448171676`, source `4d0042f8491d1544e16f4601b245d0af4a837689`) passed every gate through provider verification and the pre-build AppHost graph, then exposed a later `binding-changed-after-build` failure because ResolveReferences legitimately adds generated items during the explicit no-incremental build.
- Live source `a1d352992ace25e200df712ca14d6a5d08b8c164` made the independently recomputed post-build graph authoritative. The local correction aligns with that source-owned behavior instead of filtering `bin`/`obj`, which could hide evaluated repository inputs; final Gate 2c still independently recomputes the recorded binding after capture.
- Live Quality run `35448951466` at `a1d352992ace25e200df712ca14d6a5d08b8c164` passed both graph evaluations and exposed `apphost.runtime-output.not-closed`. Independent runtime-output/final validation still discovered projects only from root assets metadata, although MSBuild omits some conditional source references there. The validator now completes its graph from assets files bound to the invocation's exact fresh package root, with the same non-symlinked authority/project-path checks as smoke discovery, and smoke discovery reads every candidate through the bounded no-follow JSON boundary.
- Live Quality run `35449805021` at `560afbfcaf0257208ff33b07a8565a93e0715725` passed the requested MSBuild result, source-graph, and runtime-output closure boundaries. It reached Aspire resource health and failed later at the independent runtime boundary with `resource.security.not-healthy`, `apphost.capture.deadline-exceeded`, and `apphost.cleanup.incomplete`.
- Post-review focused Python validation passes (9/9), including oversized and symlinked root-assets regressions. The complete 69-test smoke module still reaches the known dependency-worktree baseline (`project-reference-path-invalid`) in 25 tests plus one dependent error when the nested source gitlinks are intentionally not materialized; the complete 175-test EventStore evidence module has the same eight pre-existing current-Pact/evidence drift failures introduced by the merged live-evidence changes.
