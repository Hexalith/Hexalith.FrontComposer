---
title: 'Make the release compatibility gates actually enforce'
type: 'bugfix'
created: '2026-09-07'
status: 'draft'
review_loop_iteration: 0
baseline_commit: 'bd4d57ded939faff198e0f408ffdb5bb4f445b0a'
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
of truth. Restore an explicit `nupkgs/` reset before sealing.

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
- **Gate 2a:** keep the CLI pack and add a pack of one library package so the validation
  properties have effect.

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
