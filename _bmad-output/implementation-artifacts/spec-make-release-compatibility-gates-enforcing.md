---
title: 'Make the release compatibility gates actually enforce'
type: 'bugfix'
created: '2026-09-07'
status: 'done'
route: 'dispatch'
review_loop_iteration: 2
baseline_commit: 'c55bfd06fbc06558dd8fda29fe70462a1439050b'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-current-release-compatibility-gates.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The compatibility-lifecycle policy introduced by
`spec-fix-current-release-compatibility-gates.md` is in place but does not enforce what it
claims. Its 2026-09-07 review confirmed four defects, all still live:

1. **The ApiCompat baseline cannot go stale loudly.** `eng/release_compatibility.py` pins
   `PUBLISHED_BASELINE_VERSION = "4.1.1"` and only checks the checked-in files against that
   literal — a constant compared with itself. The retired `eng/pack_release_packages.py` had a
   real advance rule (`MCP_SUPPRESSION_CLEANUP_LINE` / `MCP_POST_REMOVAL_BASELINE`) that was
   lost in the consolidation. Today `currentRelease` is `v4.3` and `v4.2.0`/`v4.3.0` are
   published, while the baseline is still `4.1.1`: candidates are diffed against a two-line-old
   surface, so any API added in 4.2.0 and removed since is invisible. `docs/diagnostics/README.md`
   still calls 4.1.1 "latest published", and the Python test asserts the constant equals itself.
2. **Quality Gate 2a proves nothing.** `.github/workflows/quality.yml` applies the three
   package-validation properties to a `dotnet pack` of `Hexalith.FrontComposer.Cli`, which is
   `PackAsTool=true` — the case the SDK disables package validation for. The `CiGovernanceTests`
   assertions pin that command as source text, so they report the gate as enforced either way.
3. **The policy sees 3 of 8 packages.** `DEFAULT_SUPPRESSION_FILES` covers Contracts, Mcp and
   Shell; `DEFAULT_BASELINE_PATHS` covers two files. A `CompatibilitySuppressions.xml` under any
   other packable project, or a per-project baseline override, passes parity unreviewed.
4. **Two divergent `reason` allowlists.** `DiagnosticRegistryTests` accepts
   `intentional-major-break`, `known-binary-compatibility-gap` and
   `temporary-release-candidate-exception`; `release_compatibility.py` accepts only the first, so
   a ledger row that passes every repository test aborts a live `prepare`.

**Approach:** Replace the self-referential baseline literal with a rule derived from the latest
published release line and advance the checked-in value to `4.3.0`; make Gate 2a pack a library
package so baseline validation actually executes; enumerate suppression and baseline sites from the
release inventory instead of hardcoded tuples; and reconcile the approved-reason set to one source
of truth.

## Boundaries & Constraints

**Always:** Keep pack-once/`--no-build`, the eight-package inventory, the unsigned sealed-candidate
flow, production approval, exact-source/evidence gates, the validation-aware restore that
`scripts/pack-release-packages.py` now performs, and the existing synthetic CI pack contract.
Governance checks must bind to behavior (for example the `--plan` JSON) rather than to Python or
YAML source substrings.

**Ask First:** Any new compatibility suppression or shim, public API change, real
dispatch/publication, or a release-topology change beyond the Gate 2a pack target.

**Never:** Reintroduce a second lifecycle implementation, weaken ApiCompat or expiry checks,
hand-edit `CHANGELOG.md`, rewrite tags/history, modify dependencies/submodules, or change triggers,
permissions, signing, evidence, or publishing commands. `ci.yml`, `release.yml` and
`release-evidence.yml` are pinned by `evaluator_authorizations` and must not be edited here.

## Human decisions (2026-09-07)

- **Baseline:** derive it from the latest published release line and fail closed when the
  checked-in value lags; bump the current value to `4.3.0`.
- **Gate 2a (amended 2026-09-07 after review pass 1):** keep the CLI pack and pack **every
  packable library** enumerated from `eng/release-package-inventory.json` (the 7 non-tool rows), so
  the acceptance criterion "a real ApiCompat break fails CI" holds for all of them, not just
  `Contracts`. The original "one library package" wording left five libraries unguarded.
- **Baseline rule (amended 2026-09-07 after review pass 1):** the baseline must be **no more than
  one release line behind the candidate** — its line may equal the candidate's line or be the one
  immediately before it. Packing `4.3.1` against published `4.3.0` is valid (the hotfix case);
  packing `4.4.0` requires `4.3.x`; a baseline two or more lines back fails closed. The original
  strict preceding-line rule was reverted because it made every candidate unpackable: `4.3.1`
  packed at `c55bfd06` and failed after, so the rule regressed the live release chain.
  The static repo-level check keeps the same tolerance against `currentRelease`.
- **Reason set:** narrow to the single policy reason `intentional-major-break`; the C# tests adopt
  it. The ledger is empty, so no row breaks.
- **`nupkgs/` reset:** dropped. Verified already present at `scripts/pack-release-packages.py:180-184`
  (deletes `*.nupkg`/`*.snupkg` after restore, before the first pack); the retired
  `eng/pack_release_packages.py` never had one. Findings #10/#11 are refuted and DW-1860's
  "already resolved" closure was correct.
- **EventStore 3.103.0:** out of scope here; deferred as separate work owned by `Hexalith.Builds`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Lagging baseline | `currentRelease` ahead of the checked-in baseline | Policy fails before build with the expected baseline named | Actionable diagnostic and nonzero exit |
| New suppression site | `CompatibilitySuppressions.xml` under a packable project outside the current three | Parity check sees it | Unreviewed XML blocks the candidate |
| Ledger reason | Row using a reason the C# tests accept | One allowlist decides; tests and policy agree | Same diagnostic from both |
| Gate 2a | CI quality run | A library package packs with baseline validation in effect | A real ApiCompat break fails CI |

</frozen-after-approval>

## Carried Review Evidence

From the `## Review Triage Log` of `spec-fix-current-release-compatibility-gates.md`:
findings #2-#5 (baseline), #8/#9 (Gate 2a), #6 (allowlists), #7 (reason set), #10/#11
(`nupkgs/` reset). Note that `deferred-work.md` DW-1860 is closed as "already resolved" for the
`nupkgs/` cleanup; that closure is incorrect — no replacement cleanup exists in
`eng/release_prepublish.py` or `scripts/pack-release-packages.py`.

**Planning correction (2026-09-07):** findings #10/#11 and the DW-1860 note above are refuted by the
tree — the reset exists at `scripts/pack-release-packages.py:180-184` and the retired script never
had one. Human confirmed: no `nupkgs/` work in this spec.


## Code Map

- `eng/release_compatibility.py` -- the policy. `PUBLISHED_BASELINE_VERSION = "4.1.1"` (line 14) is
  the self-referential literal; the check at lines 160-167 compares checked-in XML against that
  constant. `APPROVED_SUPPRESSION_REASON` (line 27) is the single-reason allowlist, enforced at
  lines 236-240. `DEFAULT_BASELINE_PATHS` (29-32) and `DEFAULT_SUPPRESSION_FILES` (33-43, only
  Contracts/Mcp/Shell) are the hardcoded tuples to replace with inventory-driven enumeration.
  `validate_release_policy` (104-180) already accepts `baseline_paths` / `suppression_files`
  overrides — keep that seam for tests.
- `eng/release-package-inventory.json` -- canonical 8-package inventory; the enumeration source.
  Reuse the existing reader `packable_projects()` in `scripts/pack-release-packages.py:36` rather
  than adding a third copy (`eng/release_contract.py:16-25` is already a second hardcoded list).
- `Directory.Build.targets:5` and `src/Hexalith.FrontComposer.Contracts.UI/…csproj:8` -- the two
  checked-in `FrontComposerPackageValidationBaselineVersion` sites, both `4.1.1`.
- `.github/workflows/quality.yml:83-113` -- Gate 2a. Line 87 packs the `PackAsTool=true` CLI, so the
  SDK disables package validation; add a library pack here.
- `tests/…/Governance/CiGovernanceTests.cs` -- `PackageInventory_IsExplicitLockstepAndReviewable`
  (1597) pins `PUBLISHED_BASELINE_VERSION = "4.1.1"` (1627) and `…BaselineVersion=4.1.1` in the pack
  command (1632); `SemanticReleasePack_EvaluatesPublished411PackageValidationBaseline` (1676)
  MSBuild-evaluates the real property and asserts `4.1.1` (1701, 1707) — a behavior-bound check to
  keep and retarget. `ExtractNamedStep` (3329) is the helper for binding Gate 2a structurally.
- `tests/eng/test_pack_release_packages.py` -- line 117 asserts the constant equals itself; line 30
  and 189-195 carry `4.1.1` fixtures.
- `tests/…/Diagnostics/DiagnosticRegistryTests.cs:2219-2223` -- the three-reason `HashSet`.
- `docs/diagnostics/README.md:7` -- still calls `4.1.1` "latest published".
- Do not change: `ci.yml`, `release.yml`, `release-evidence.yml` (pinned by
  `evaluator_authorizations`), signing, evidence and publishing commands.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_compatibility.py` -- replace the self-referential baseline check with the
      preceding-line rule, naming both found and expected baseline in the diagnostic -- makes a
      lagging baseline fail closed.
- [x] `eng/release_compatibility.py` -- derive suppression and baseline sites from the release
      inventory instead of `DEFAULT_SUPPRESSION_FILES` / `DEFAULT_BASELINE_PATHS` -- closes the
      3-of-8 blind spot.
- [x] `eng/release_compatibility.py` + `tests/…/DiagnosticRegistryTests.cs` -- narrow the reason set
      to `intentional-major-break` in both, leaving one source of truth.
- [x] `Directory.Build.targets`, `src/…Contracts.UI/….csproj` -- advance the baseline to `4.3.0`.
- [x] `.github/workflows/quality.yml` -- ~~add a `dotnet pack` of `Hexalith.FrontComposer.Contracts`
      carrying the three validation properties, keeping the existing CLI pack -- gives Gate 2a a
      target the SDK does not disable validation for.~~ **SUPERSEDED by the pass-2 amendment
      (see `## Human decisions` and `**Pass 3 (2026-09-07) -- review remediation.**

- **A declared baseline override is required again.** `eng/release-package-inventory.json` gained
  `baseline_override: true` on Contracts.UI, and `inventory_required_baseline_paths` folds the
  flagged projects in beside `Directory.Build.targets`. Reproduced and fixed: deleting the
  Contracts.UI `<FrontComposerPackageValidationBaselineVersion>` pin exits 1 with
  `required FrontComposerPackageValidationBaselineVersion declaration is missing`.
- **`exception` collision removed.** The PackAsTool rationale moved off `exception` (which means
  "why this project is not packable" and is consumed at `eng/release_evidence.py:3396` as the
  `symbol_artifact` stand-in) onto `pack_as_tool_reason`. The governance test now requires that
  field and forbids `exception` on a packable row.
- **Inventory flag types validated.** `compatibility_suppressions`, `baseline_override` and
  `pack_as_tool` must be the JSON boolean `true` when present, and a `pack_as_tool` row must carry
  a non-empty `pack_as_tool_reason`. A string `"true"` now fails closed instead of silently
  dropping a reviewed site.
- **Gate 4 output isolation is per project.** Each pack gets
  `artifacts/library-package-validation/<project name>/bin/`, so seven sequential packs and their
  ProjectReference graphs cannot intermingle in one `bin/Release/<tfm>/` and ApiCompat cannot diff
  a stale sibling assembly.
- **One definition of the gate predicate.** The step calls
  `release_compatibility.packable_library_projects` instead of re-implementing "packable and not
  pack_as_tool" as inline YAML Python.
- **Ordering assertion scoped to its job.** `ExtractJobBlock(quality, "build-and-test")` bounds the
  `--no-build` search (proven by asserting the block stops before `accessibility-visual`), and the
  Gate 4 comment no longer contains the literal the assertion searches for -- previously the
  comment itself satisfied the check.
- **Real-script release-policy coverage.** `test_real_script_packs_the_checked_in_release_line_under_release_policy`
  runs `scripts/pack-release-packages.py 4.3.1 --release-policy --plan` against the real repository
  root, so a tree that cannot pack any version fails a test.
- **`v0.0` diagnostic.** `preceding_release_line` returns `None` instead of raising, so the failure
  message on the `v0.0` line still names the found and the accepted baseline.
- **Stale message** at `DiagnosticRegistryTests.cs:700` updated to `4.3.0`.

**Observed 2026-09-07 (pass 3):**

- `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish` -- 60/60 pass.
- `CiGovernanceTests` 81/81; `DiagnosticRegistryTests` 98/98.
- Deleting the Contracts.UI baseline pin -> exit 1 with the named diagnostic; restored and re-verified.
- The seven-library Gate 4 loop with per-project `BaseOutputPath` packs all 7 with zero ApiCompat
  errors, and `md5sum` of every `src/*/bin/Release/net10.0/*.dll` is unchanged before/after.
- `python3 eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json`
  -- `valid`. `actionlint .github/workflows/quality.yml` -- exit 0.

## Spec Change Log`):** delivered as `Gate 4: Library Package
      Validation (ApiCompat baseline)` over the inventory's 7 non-tool rows, not one hardcoded
      project, and with its own gate id so `Gate 2a` no longer names two steps.
- [x] `tests/eng/test_pack_release_packages.py` -- replace the equals-itself assertion with cases
      that fail a lagging baseline and pass a current one.
- [x] `tests/…/CiGovernanceTests.cs` -- retarget the `4.1.1` pins to `4.3.0`, rename the `411` test,
      and bind the gate to the packed projects' validation behavior rather than only its step name.
      Delivered as `QualityWorkflow_Gate4ValidatesEveryPackableLibrary` under the pass-2 amendment.
- [x] `docs/diagnostics/README.md` -- correct the "latest published" baseline text.

**Acceptance Criteria:**
- Given the checked-in baseline lags the rule's expected value, when the policy runs before build,
  then it exits nonzero naming both the found and the expected baseline.
- Given a `CompatibilitySuppressions.xml` is added under a packable project outside
  Contracts/Mcp/Shell, when parity runs, then the unreviewed file blocks the candidate.
- Given a ledger row uses a reason accepted by the C# tests, when the policy validates it, then both
  produce the same verdict.
- Given Gate 2a runs in CI, when a real ApiCompat break exists in a packed library, then the job
  fails.

## Implementation Notes

**Implemented 2026-09-07.**

- **Baseline rule (`eng/release_compatibility.py`).** `_validate_baseline` replaces the
  constant-compared-with-itself check. At pack time (`match_candidate_release=True`) the checked-in
  baseline line must be exactly the line before the candidate's line; the static repository check
  (shared CI / `--plan` without `--release-policy`) accepts the ledger's `currentRelease` line or
  the one before it and fails closed below that. Both diagnostics name the found value and the
  expected line. `preceding_release_line` / `is_preceding_release_line` treat a major bump
  (`M.0`) as "any minor of major `M-1`", because the previous major's last minor cannot be derived
  from the candidate version alone.
- **Inventory-driven enumeration.** `packable_packages` / `packable_projects` moved from
  `scripts/pack-release-packages.py` into `eng/release_compatibility.py`; the packer keeps a thin
  wrapper so its `REPO_ROOT` / `INVENTORY_PATH` test seam still works. `validate_release_policy` now
  derives baseline override sites (every packable `.csproj`) and suppression sites (each packable
  project's `CompatibilitySuppressions.xml`) from `eng/release-package-inventory.json`, requires the
  shared `Directory.Build.targets` default, requires every declaring site to agree, and rejects any
  `CompatibilitySuppressions.xml` under `src/` that belongs to no packable project. A packable
  project with no suppression file contributes no rows, exactly like the checked-in empty files.
- **Reason set.** `DiagnosticRegistryTests` now accepts only `intentional-major-break`, matching
  `APPROVED_SUPPRESSION_REASON`. The ledger is empty, so no row changed verdict.
- **Gate 2a.** A new `Gate 2a: Library Package Validation` step packs
  `Hexalith.FrontComposer.Contracts` with the three validation properties; the CLI smoke pack is
  unchanged apart from its baseline literal. The library pack does **not** use `--no-build`: the
  solution build stamps assembly version `1.0.0.0`, and ApiCompat's CP0003 identity rule rejects
  that against a 4.x baseline, so the step builds with `-p:Version=4.3.0-ci`. Verified locally that
  the `--no-build` form fails with CP0003 and the built form passes, which also proves the baseline
  package is opened and diffed (the old CLI-only gate produced no ApiCompat output at all).
- **Governance binding.** `PublishedPackageValidationBaseline` reads the baseline out of the live
  packer's `--plan` JSON instead of pinning a Python source substring; both the inventory lockstep
  test and the renamed `SemanticReleasePack_EvaluatesThePublishedPackageValidationBaseline` compare
  the MSBuild-evaluated property to that value. `QualityWorkflow_Gate2aPacksALibraryPackageWithValidationInEffect`
  MSBuild-evaluates every project Gate 2a packs and requires at least one to evaluate
  `EnablePackageValidation=true`, while asserting the CLI still evaluates `false`.
- **Collateral baseline sites not named in the Code Map.** `tests/…/PackageBoundaryTests.cs`,
  `tests/…/McpRuntimePackageBoundaryTests.cs`, `tests/…/DiagnosticRegistryTests.cs` and
  `tests/eng/test_release_prepublish.py` also pinned `4.1.1`; they were retargeted (deriving the
  value from `Directory.Build.targets` or the policy constant where practical). The MCP candidate
  fixture moved to `4.4.0-review.compat` because a `4.2.0` candidate now trips CP0003 against the
  `4.3.0` baseline.
- **Ledger left at `v4.3`.** `docs/diagnostics/compatibility-suppressions.json` is unchanged.
  ~~Consequence: a live `--release-policy` prepare on the v4.3 line is now blocked until the ledger
  advances to `v4.4`.~~ **SUPERSEDED by the pass-2 amendment:** the one-line tolerance accepts a
  baseline on the candidate's own line, so `4.3.1` packs against published `4.3.0` with the ledger
  still on `v4.3`; nothing is blocked. `tests/eng/test_pack_release_packages.py::
  test_real_script_packs_the_checked_in_release_line_under_release_policy` runs the real script
  against the real tree to keep that true. The production `--plan` test still drives an
  inventory-shaped fixture repository so it does not go transient as the ledger advances.

**Pass 2 (2026-09-07) -- review remediation.**

- **Baseline rule amended to "no more than one line behind".** `is_within_one_release_line`
  replaces the strict preceding-line check in both modes: the baseline may sit on the line under
  validation or the one immediately before it. `4.3.1` against published `4.3.0` now validates
  against the checked-in tree (the regression that made every candidate unpackable is gone), and
  `4.4.0` still requires `4.3.x`. The expected label is computed only on failure so the `v0.0`
  same-line case is not blocked by "no preceding line".
- **`is_preceding_release_line` major-bump limitation documented, not tightened.** The ledger
  records only the planned `currentRelease`, never published history, so the previous major's last
  minor has no in-repository source; the docstring states the limitation explicitly and tests pin
  the accepted (`5.0.0` vs `4.0.x`/`4.9.x`) and rejected (`3.9.x`) cases plus the `v0.0` paths.
- **Deleted reviewed suppression file now fails closed.** `eng/release-package-inventory.json`
  gained `compatibility_suppressions: true` on Contracts/Mcp/Shell; a flagged site (or any package
  with ledger rows) must have its file, while a packable project that never carried one still
  contributes no rows. Reproduced and fixed: deleting `src/…Contracts/CompatibilitySuppressions.xml`
  exits 1 with `required compatibility policy XML file is missing`.
- **Gate 2a -> Gate 4, covering every packable library.** The library pack is now driven from the
  inventory's 7 non-tool rows (`pack_as_tool: true` marks the documented CLI exception) rather than
  one hardcoded project, so a CP0002 break in Shell, Schema, SourceTools, Testing or Contracts.UI
  fails CI. The duplicate `Gate 2a` name is gone.
- **Shared build outputs preserved.** The gate runs after every `--no-build` consumer and writes to
  an isolated `-p:BaseOutputPath`; verified locally that `src/*/bin/Release/net10.0/*.dll` is
  byte-identical across all seven packs. `-p:BaseIntermediateOutputPath` is deliberately NOT
  redirected: `DefaultItemExcludes` is derived from it, so moving it un-excludes the real `obj/`
  tree and the checked-in source-generator fixtures collide (`CS0102`/`CS0111` duplicate members),
  and `project.assets.json` collides across a project graph. Both failure modes were reproduced.
- **Reason set bound to one source.** `DiagnosticRegistryTests` parses
  `APPROVED_SUPPRESSION_REASON` out of `eng/release_compatibility.py`; a new `[Theory]` pins that
  `known-binary-compatibility-gap` and `temporary-release-candidate-exception` are rejected.
- **Docs/ignore/docstring:** the README sentence now states the one-line tolerance;
  `nupkgs-validation/` joins `nupkgs/` and `nupkgs-signed/` in `.gitignore`;
  `eng/release_prepublish.py::phase_build` no longer says the cached baseline is 4.1.1.

**Observed 2026-09-07 (pass 2):**

- `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish` -- 55/55 pass.
- `validate_release_policy(ROOT, "4.3.1")` -> `v4.3` (hotfix unblocked); the seven-library Gate 4
  loop packs all 7 with package validation on against published `4.3.0` and zero ApiCompat findings;
  `md5sum` of every `src/*/bin/Release/net10.0/*.dll` unchanged before/after.
- `CiGovernanceTests` 81/81 (the EventStore gitlink failure reported in pass 1 was fixed upstream by
  `914375d2`/`2fd1d11e`); `DiagnosticRegistryTests` 98/98.
- `python3 eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json`
  -- `valid` with the two new inventory fields. `actionlint .github/workflows/quality.yml` -- exit 0.

## Spec Change Log

**Pass 1 -> 2 (2026-09-07).**
- **Triggering findings:** triage #1 (preceding-line rule blocks every candidate; `4.3.1` packed at
  `c55bfd06` and fails after) and #2 (Gate 2a validates 1 of 7 packable libraries while the frozen
  AC requires any real ApiCompat break to fail CI).
- **Amended:** both root causes sit inside `<frozen-after-approval>`, so the human renegotiated two
  decisions — the baseline rule became "no more than one line behind the candidate" (same line or
  preceding), and Gate 2a became "every packable library from the inventory".
- **Known-bad state avoided:** a release chain that cannot pack any version, and a CI gate that
  misses binary breaks in five of the eight release packages.
- **Human chose fix-forward** over revert-and-re-derive: the change is already committed as
  `7e0e6cc6`, and only the rule and the gate scope were wrong.
- **KEEP (must survive re-derivation):** inventory-driven enumeration of baseline and suppression
  sites via `packable_packages`/`packable_projects` in `eng/release_compatibility.py` (with the
  packer's `REPO_ROOT`/`INVENTORY_PATH` wrapper seam); the narrowed single reason set; the
  behaviour-bound governance test that reads the baseline from the packer's `--plan` JSON instead
  of pinning Python source text; the full new Python test suite; and the `4.3.0` baseline advance
  across `Directory.Build.targets`, `Contracts.UI.csproj` and the docs.


## Review Triage Log

**Pass 1 (2026-09-07)** — layers: blind-hunter, edge-case-hunter, verification-gap.

| # | Finding | Verdict | Evidence |
|---|---------|---------|----------|
| 1 | Preceding-line rule blocks every candidate; no version can pack | **high** | Reproduced. `4.3.1` -> "baseline must be on the published release line immediately preceding --version v4.3, expected v4.2; found '4.3.0'"; `4.4.0` -> "release line v4.4 does not match currentRelease v4.3". A worktree at `c55bfd06` shows `4.3.1` PASSED before this change, so the change regressed the live chain. Root cause is the frozen preceding-line decision interacting with the pre-existing `currentRelease` equality check. |
| 2 | Gate 2a validates 1 of 7 packable libraries | **high** | Only `Contracts` is packed with validation. A CP0002 break in Shell/Schema/SourceTools/Testing/Contracts.UI passes CI. The frozen decision says "one library package", but the frozen AC says "a real ApiCompat break fails CI" — the frozen block contradicts itself. |
| 3 | Deleting a reviewed `CompatibilitySuppressions.xml` no longer fails closed | **medium** | Reproduced on a repo copy: removing `src/Hexalith.FrontComposer.Contracts/CompatibilitySuppressions.xml` now PASSES. `_suppression_xml_rows` gained `if not path.is_file(): continue`, retiring the "required compatibility policy XML file is missing" error. No test covers deletion. |
| 4 | Gate 2a library pack omits `--no-build`, restamping shared Release output | **medium** | Runs between Gate 2's solution build and ~10 downstream `--no-build` steps, rewriting `Contracts.dll` to `4.3.0.0` in shared `bin`/`obj`. Matches the known Gate 3a missing-assembly flake shape. Also deviates from the frozen "Always: keep pack-once/`--no-build`". |
| 5 | Reason set is two unbound literals; retired reasons not pinned as rejected | **medium** | `APPROVED_SUPPRESSION_REASON` (Python) and the C# `allowedReasons` agree only by coincidence; nothing binds them. Re-adding `known-binary-compatibility-gap` to the C# set breaks no test. |
| 6 | Major-bump branch accepts any minor of the previous major; untested | **medium** | `is_preceding_release_line` returns true when `baseline_line[0] == major - 1`, so `5.0.0` validates against `4.0.x` even when `4.9.x` is published. No test covers the major-bump or `v0.0` paths. |
| 7 | `docs/diagnostics/README.md` overstates the static check | **low** | Text says it "fails closed as soon as the checked-in baseline falls behind the ledger's `currentRelease`", but `_validate_baseline` accepts the same line *or* the preceding one, so a one-line lag passes. |
| 8 | `nupkgs-validation/` is not gitignored | **low** | `.gitignore` has `nupkgs/` (:440) and `nupkgs-signed/` (:443); the new output directory matches neither and is never cleaned. |
| 9 | Two workflow steps both named "Gate 2a" | **low** | `quality.yml:88` and `:94`. Gate ids are this repo's log vocabulary. |
| 10 | Unrelated submodule gitlink bumps in the commit | **medium** | Confirmed: `Hexalith.EventStore` `059f6a89->d45206f7`, `Hexalith.Memories` `7e9c2c38->e5168bd6`, swept in by the workspace auto-commit actor. Pre-existing workspace behavior, not caused by this change. Deferred (already logged). |
| 11 | `PublishedPackageValidationBaseline` still self-referential | **medium** | The `--plan` baseline comes from `PUBLISHED_BASELINE_VERSION`, and the test asserts it equals `4.3.0` — the old pin routed through a subprocess. The only external anchor is the hand-edited `currentRelease`; nothing checks it against what is actually published. |
| 12 | Gate 2a packs `4.3.0-ci` against baseline `4.3.0` — same line the policy forbids | **medium** | CI never exercises the shape a real release pack takes, and the new governance test locks the inconsistency in. |
| 13 | `eng/release_prepublish.py:193` docstring still says "currently 4.1.1" | **low** | Missed in the baseline sweep. |
| 14 | `release_contract.EXPECTED_PACKAGES` not retired | **false** | Checked `eng/release_contract.py:220-227`: it already validates against `release-package-inventory.json` and fails on drift, so it is bound, not an unverified second copy. |


**Pass 1 resolutions (2026-09-07).** #1 and #2 were intent gaps; the human renegotiated both frozen
decisions and chose fix-forward over revert. #1 fixed (`is_within_one_release_line`; `4.3.1` and
`4.3.0` pack again, a two-line-back baseline still fails closed). #2 fixed (Gate 4 enumerates all 7
packable libraries from the inventory). #3-#9, #11-#13 patched. #12 was dissolved by the amended
rule — Gate 4 packing `4.3.0-ci` against baseline `4.3.0` is now a legal same-line check. #10
deferred (logged). #14 rejected as false. Verified after patching: Python 55/55, CiGovernanceTests
81/81, DiagnosticRegistryTests 98/98, actionlint clean, Gate 4 placed after the last `--no-build`.

**Pass 2 (2026-09-07)** — same three layers, re-run on the patched tree.

*Scoping note:* `baseline_commit` `c55bfd06` now spans four commits from concurrent sessions
(`914375d2`, `2fd1d11e`, `9f09416c`, `978175a8`), so the pass-2 diff carried changes this spec does
not own. Findings were triaged by ownership.

| # | Finding | Verdict | Evidence |
|---|---------|---------|----------|
| 15 | Deleting the `Contracts.UI` baseline pin no longer fails the policy | **medium** | Reproduced on a repo copy: removing the property PASSES, where the retired `DEFAULT_BASELINE_PATHS` made it fail as `<missing>`. Fail-closed regression caused by this change. Patched. |
| 16 | Gate 4 shares one `BaseOutputPath` across 7 packs and their P2P graph | **medium** | Seven sequential packs emit into one `bin/Release/<tfm>/`; ApiCompat can diff a stale sibling. Patched. |
| 17 | Gate 4 ordering assertion is file-wide, not job-scoped | **medium** | `quality.LastIndexOf("--no-build")` searches the whole file; passes today only because the last literal sits in Gate 4's own comment. Patched. |
| 18 | Packable-library predicate now has three copies | **medium** | Inline Python in YAML duplicates `packable_library_projects` (called only from tests) and the C# recomputation. The hardcoded `-ne 7` makes drift loud, not silent. Patched. |
| 19 | Inventory `exception` field overloaded on the packable Cli row | **medium** | `exception` already means "why not packable" and is consumed by `eng/release_evidence.py:3396` as the `symbol_artifact` stand-in. Patched to a distinct field. |
| 20 | New inventory flags not type-validated | **low** | `compatibility_suppressions: "true"` (string) would silently stop requiring a reviewed site. Patched. |
| 21 | No test proves the real script packs against the real repo | **medium** | The production-plan test moved to a synthetic fixture, so the pass-1 "nothing can pack" state would not have failed any test. Patched. |
| 22 | `v0.0` failure path names neither found nor expected baseline | **low** | `preceding_release_line` raises before the diagnostic is built, breaking the stated AC on that path. Patched. |
| 23 | Stale `4.1.1` assertion message at `DiagnosticRegistryTests.cs:700` | **low** | Missed by the baseline sweep that retargeted the rest of the file. Patched. (`:967` `ShouldNotContain` is intentional.) |
| 24 | Spec contradicts itself after the amendment | **low** | Pass-1 notes still claim a blocked prepare; Tasks still say "pack Contracts" / "Gate 2a". Annotated as superseded. |
| 25 | Synthetic-CI pack rebuilds shared `bin/` with no isolation | **medium** | Real shared-CI flake risk, and inconsistent with the isolation rule Gate 4 now enforces — but introduced by `9f09416c` (concurrent session), not this change. **Deferred**, logged. |
| 26 | Candidate at or below the published baseline is accepted | **medium** | Verified: `4.3.0` and `4.3.0-rc.1` pass. Confirmed **pre-existing** — `4.3.0` also passed at `c55bfd06`. Already an open `deferred-work.md` entry; the same-line amendment does not close it. **Deferred**. |
| 27 | Analyzer ledger re-seal / `buildsCatalogSha` / EventStore gitlink / spec-template scaffold ride along | **low** | All from concurrent-session commits in the baseline span; analyzer-policy ledger drift is explicitly out of scope for this spec. **Deferred**. |
| 28 | Baseline still anchored to a constant and a hand-edited ledger | **medium** | Accurate: nothing verifies `4.3.0` or `currentRelease` against what is actually published. Inherent to the "CI must not call live package feeds" constraint. **Deferred** — recorded as a known limit, not fixed here. |


**Accepted deviation:** `BaseIntermediateOutputPath` was NOT redirected for Gate 4. Redirecting it
un-excludes the real `obj/` tree via `DefaultItemExcludes`, colliding checked-in source-generator
fixtures (`CS0102`/`CS0111`) and `project.assets.json` across the graph; both reproduced. Shared
`bin/Release` output was confirmed byte-identical (`md5sum`) before and after all 7 packs, and the
gate runs after every `--no-build` consumer, so the flake risk is contained by ordering instead.


## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py` -- expected: all pass.
- `python3 scripts/pack-release-packages.py ./nupkgs 4.4.0 --plan` -- expected: exits 0 and the plan
  reports the advanced baseline; a stale checked-in baseline exits nonzero with the expected value named.
- `dotnet msbuild src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -getProperty:EnablePackageValidation,PackageValidationBaselineVersion -p:EnableFrontComposerPackageValidation=true` -- expected: `true` and `4.3.0`.
- Shell governance tests via the direct xUnit runner (VSTest sockets are blocked):
  `-class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` -- expected: pass.

**Observed 2026-09-07:**

- `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish` -- 49/49 pass
  (36 in the packer/policy suite, including the new preceding-line, static-lag, site-disagreement,
  inventory-enumeration and stray-suppression-file cases).
- `python3 scripts/pack-release-packages.py ./nupkgs 4.4.0 --plan` -- exit 0, plan reports
  `-p:FrontComposerPackageValidationBaselineVersion=4.3.0` on the restore and all eight packs.
  With `Directory.Build.targets` temporarily reverted to `4.1.1` the same command exits 1 with
  `Directory.Build.targets: package-validation baseline must be on the v4.2 or v4.3 release line
  for currentRelease v4.3; found '4.1.1'`; reverting only one of the two sites exits 1 with
  `package-validation baseline sites disagree`.
- `dotnet msbuild src/Hexalith.FrontComposer.Contracts/… -getProperty:EnablePackageValidation,PackageValidationBaselineVersion -p:EnableFrontComposerPackageValidation=true`
  -- `true` / `4.3.0`. The CLI evaluates `EnablePackageValidation=false` (PackAsTool), confirming
  the pre-change gate was inert.
- Gate 2a command run locally: `dotnet pack …Contracts.csproj -c Release --output … -p:Version=4.3.0-ci
  -p:PackageVersion=4.3.0-ci` plus the three validation properties -- succeeds with no ApiCompat
  findings against published `4.3.0`. The same command with `--no-build` fails with
  `CP0003 … assembly version '1.0.0.0' should be equal to or higher than [Baseline] … '4.3.0.0'`,
  proving baseline validation executes.
- Direct xUnit v3 runner (VSTest sockets blocked): `CiGovernanceTests` 80/81 pass; the single
  failure is the pre-existing `EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`
  gitlink pin (`references/Hexalith.EventStore` moved to `059f6a8` in commit `7c57aee0`), unrelated
  to this change. `DiagnosticRegistryTests` 95/95, `McpRuntimePackageBoundaryTests` 2/2 (real pack
  against the published `4.3.0` MCP baseline), `PackageBoundaryTests.PackageValidation_PinsTheSharedPublishedBaseline` 1/1.
- `actionlint .github/workflows/quality.yml` -- exit 0. `git diff --check` -- clean.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` was already modified in the working tree
  by the orchestrator sweep before this change began; it was neither edited nor reverted here.
