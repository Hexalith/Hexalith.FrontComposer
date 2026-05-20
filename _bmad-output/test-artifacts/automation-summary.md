---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-identify-targets', 'step-03-generate-tests', 'step-04-validate-and-record']
lastStep: 'step-04-validate-and-record'
lastSaved: '2026-05-20'
author: 'Murat (TEA)'
storyId: '12.4'
storyKey: '12-4-trusted-release-evidence-dry-run'
scope: 'Apply F3 + F4 + F5 from the Story 12.4 test review (test-review-12-4.md)'
detectedStack: 'backend (xUnit v3 + Shouldly + Python shell-out)'
testFramework: 'xUnit v3 + Shouldly + NSubstitute (C#); shells out to eng/release_evidence.py (Python)'
executionMode: 'BMad-Integrated (sequential)'
filesModified:
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
helpersAdded:
  - 'FindStepBlockContaining(workflow, needle): scoped step-block search (F4 prerequisite)'
  - 'ExtractJobPermissionsBlock(workflow, jobId): job-scoped permissions extraction (F4 prerequisite)'
testsAffected:
  - 'Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow (refactored — F4)'
  - 'Story12_4_Def14_AttestationsWritePermission_IsRestored (refactored — F4)'
  - 'Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent (extended — F3 classifier round-trip)'
  - 'Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked (rebuilt — F5 single-axis manifest)'
inputDocuments:
  - _bmad-output/test-artifacts/test-reviews/test-review-12-4.md
  - _bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md
  - _bmad-output/test-artifacts/atdd-checklist-12-4-trusted-release-evidence-dry-run.md
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
  - eng/release_evidence.py
  - .github/workflows/release.yml
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md
---

# Automation Expansion — Story 12.4 Test-Review Findings F3 + F4 + F5

## Goal

Apply the three "Strongly Recommended" findings from [test-review-12-4.md](test-reviews/test-review-12-4.md):

- **F3** — Add classifier round-trip to Def22 so it pins both fixture shape AND classifier enforcement (mirror Def25's gold-standard pattern).
- **F4** — Refactor both Def14 tests from raw `Contains`/`IndexOf` on the full workflow text to structured step-body and job-permissions assertions.
- **F5** — Rebuild the Def23 manifest fixture so only `release_definition_fingerprints: {}` is the failing axis (eliminates seal-hash conflation).

## What changed

### Helpers added

Two new private static helpers next to the existing `ExtractNamedStep` in [CiGovernanceTests.cs](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs):

- **`FindStepBlockContaining(string workflow, string needle)`** — returns the workflow step block (between `      - name:`/`      - uses:` boundaries) whose body contains `needle`. Used by F4 to assert the attestation step exists as a real wired step, not just text in a comment or doc anchor.

- **`ExtractJobPermissionsBlock(string workflow, string jobId)`** — returns the contents of the named job's `permissions:` block (4-space-indented key, 6-space-indented entries), stopping at the next 2-space-indented job header so sibling jobs are never read. Used by F4 to scope `attestations: write` / `id-token: write` assertions to the release job specifically.

### F4 — Def14 tests refactored

**`Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`**
- Old: `workflow.Contains("actions/attest-build-provenance@v2")` + `IndexOf("Run semantic-release")` ordering
- New: `FindStepBlockContaining(workflow, "actions/attest-build-provenance@v2")` returns the actual step block; assert it is non-empty, has no `if: false`, no `continue-on-error: true`; ordering anchored to `ExtractNamedStep(workflow, "Run semantic-release (live publish)")` rather than the first textual match of "Run semantic-release"
- Closes three F4 blind spots: comments/doc anchors, step-level skip flags, ambiguous "Run semantic-release" positions

**`Story12_4_Def14_AttestationsWritePermission_IsRestored`**
- Old: `workflow.Contains("attestations: write")` + `workflow.Contains("id-token: write")` on the full file text
- New: `ExtractJobPermissionsBlock(workflow, "release")` scopes to the release job's permissions block; assertions distinguish workflow-level from job-level permission scope; also asserts the narrowed `attestations: read` from round-8 CR-12-4-P189 is REPLACED (not duplicated)
- Closes the F4 partial-green footgun where workflow-level `attestations: write` could shadow job-level `attestations: read`

### F3 — Def22 classifier round-trip

`Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent` gains a second phase after the existing fixture-shape assertions. The new block:
- Runs `python eng/release_evidence.py classify-fixtures --fixtures … --output <temp>` against the live fixtures file
- Asserts the result entry named `dry-run-with-side-effect-attempt` exists, classifies as `blocked`, and reports `publish_authorized=false`
- Wraps the temp output file in `try/finally` + `File.Delete` (F1/F2 cleanup pattern applied at the new call site)
- Mirrors the pattern that makes Def25 the gold-standard test in this file (fixture shape + classifier round-trip + typed diagnostic)

Today the new block is structurally unreachable because the existing first assertion (`matchingCase.ShouldNotBeNull`) already fails RED until Def22 adds the fixture. Once Def22 lands, the round-trip runs and pins the AC24 contract end-to-end.

### F5 — Def23 single-axis manifest

`Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked` rebuilt:

**Before (F5):** The manifest fixture had `seal.hash="0"` and `packages: []`. Today's exit-code assertion passed for the wrong reason — the seal check fired ("manifest seal with sha256 hash is required") and the empty-packages check fired ("package rows are required"). The fingerprints contract was never exercised on the exit-code axis.

**After (F5):** The fixture is an unsealed manifest with all top-level fields valid (concrete sha256 hashes for sbom/benchmark), a single fully-valid package row with all `REQUIRED_ROW_FIELDS` populated and `signing_status=verified`, `timestamp_status=verified`, `attestation_status=attested`, and a sha256-shaped checksum that doesn't start with `nupkgs/`. The test then shells out to `python eng/release_evidence.py seal-manifest --manifest <unsealed> --output <sealed>` so the helper itself computes the canonical seal hash (avoiding C#-side re-implementation of Python's `json.dumps(sort_keys=True, separators=(",",":"))` canonicalization).

Net effect: today, `verify-manifest --no-root` on the sealed fixture exits 0 (all checks pass; the fingerprints check is gated by `root is not None`). The test fails RED on `result.ExitCode.ShouldNotBe(0)` — the precise contract Def23 closes. After Def23 lands, the fingerprints check fires under `--no-root`, exit becomes non-zero, the diagnostic contains `release_definition_fingerprints`, and the test transitions cleanly RED → GREEN.

Both temp files (unsealed + sealed) cleaned up via `try/finally`.

## Validation

### Build

```
cd D:\Hexalith.FrontComposer
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release
# Build succeeded. 0 Warning(s) 0 Error(s) — Time Elapsed 00:00:19.61
```

### RED-phase verification

```
dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj `
  --configuration Release --no-build `
  --filter "FullyQualifiedName~Story12_4_Def"
# Failed: 5, Passed: 0, Skipped: 0, Total: 5
```

Each test fails on the precise axis pinned by its Def item:

| Test | Fail axis (post-refactor) |
|---|---|
| Def14 T1 | `attestStep.ShouldNotBeNullOrEmpty` — step missing |
| Def14 T2 | `jobPermissions.Contains("attestations: write")` is False — job-scoped, not file-wide |
| Def22 | `matchingCase.ShouldNotBeNull` — fixture missing (F3 round-trip is unreachable until fixture lands, by design) |
| Def23 | `result.ExitCode.ShouldNotBe(0)` — manifest is otherwise valid; only fingerprints gate fires |
| Def25 | `matchingCase.ShouldNotBeNull` — fixture missing (unchanged from prior; F1/F2 cleanup only) |

### Main-lane exclusion

```
dotnet test ... --filter "FullyQualifiedName~Story12_4_Def&Category!=Quarantined"
# (No test matches the given run settings.)
```

All 5 tests remain correctly quarantined.

## Scope guardrails honored

- No changes to `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, `eng/release-package-inventory.json`, or `tests/ci-governance/fixtures/release-readiness-cases.json` (red-phase invariant: tests describe the contract, green-phase patches change the implementation)
- No new packages, no analyzer changes, no DI changes
- Same convention as the rest of the file: `string.Contains(value, StringComparison.Ordinal).Should[True|False](message)` rather than `string.ShouldContain(value, message)` (Shouldly version doesn't have the 2-arg customMessage overload for those extension methods on string)
- CRLF, UTF-8, 4-space indent, file-scoped namespace, existing brace/spacing style preserved
- No submodule (`Hexalith.EventStore/**`, `Hexalith.Tenants/**`) edits

## Findings left open (deferred per review)

- **F6** — `GetBoolean()` fragility on Def22/Def25 (low; fixture-author drift unlikely)
- **F7** — Pin exact diagnostic string for Def23 (depends on the verbiage the Def23 author picks; add when Def23 lands)
- **F8** — `.Single` → `.SingleOrDefault` + Shouldly on Def25 (polish)
- **F9** — Shared fixture-lookup helper across Def22/25 (polish; opportunistic)
- **F10** — Process timeout hardening (infra-level; future story)
- **Shared `TempFile` helper** — first considered post-F1/F2; would now retrofit four call sites (Def22 output, Def23 unsealed, Def23 sealed, Def25 output). Reasonable next consolidation if this pattern grows further.

## Recommended next steps

1. **Commit** as one focused change — F3+F4+F5 are conceptually a single "tighten the red-phase contracts" PR. (F1+F2 already applied earlier and untouched here.)
2. **Apply F-Def14 green-phase**: wire the `actions/attest-build-provenance@v2` step into `release.yml` before "Run semantic-release (live publish)", restore `attestations: write` + `id-token: write` to the release job permissions. Both Def14 tests should turn GREEN together; remove the `[Trait("Category", "Quarantined")]` and metadata comment in the same change.
3. **Apply F-Def22 green-phase**: add the `dry-run-with-side-effect-attempt` fixture case to `tests/ci-governance/fixtures/release-readiness-cases.json`. The F3 round-trip will then exercise the classifier contract.
4. **Apply F-Def23 green-phase**: extend `verify-manifest --no-root` in `eng/release_evidence.py` to enforce the `release_definition_fingerprints` contract (either by lifting the `if root is not None and root_exists:` gate for the fingerprints check, or by adding a dedicated `--require-fingerprints` flag).
5. **Apply F-Def25 green-phase**: add the `packages-empty-array` fixture; ensure `manifest_diagnostics` queues "package rows are required" for the empty-array path identical to the null path.
6. **Re-run** `/bmad-testarch-test-review` in Validate mode after green-phase work to confirm fixes hold.

---

_Automation expansion completed 2026-05-20. Murat signing off — 3 review findings closed, all 5 red-phase tests still RED on the correct contract, 0 build warnings, 0 build errors._
