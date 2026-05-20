---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-generate-red-phase', 'step-04-save-and-handoff']
lastStep: 'step-04-save-and-handoff'
lastSaved: '2026-05-20'
storyId: '12.4'
storyKey: '12-4-trusted-release-evidence-dry-run'
storyFile: '_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md'
atddChecklistPath: '_bmad-output/test-artifacts/atdd-checklist-12-4-trusted-release-evidence-dry-run.md'
detectedStack: 'fullstack'
testFramework: 'xUnit v3 + Shouldly + NSubstitute (C#); pytest-style ad hoc via release_evidence.py CLI (Python)'
generationMode: 'gap-fill'
scope: 'open Def items only (Def14, Def22, Def23, Def25)'
generatedTestFiles:
  - 'tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs (5 new [Fact] methods, lines ~1639-1830)'
inputDocuments:
  - '_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md'
  - '_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md'
  - '_bmad-output/project-context.md'
  - 'tests/ci-governance/fixtures/release-readiness-cases.json'
  - '.github/workflows/release.yml'
  - 'tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs'
  - 'tests/README.md'
  - '_bmad/tea/config.yaml'
---

# Story 12.4 ATDD Checklist — Trusted Release Evidence Dry Run

## Mode

- **Generation mode:** gap-fill (red-phase scaffolds for open Def items only).
- **Reason for narrow scope:** Story 12.4 is at status `review` with 35/35 ACs implemented and 11 review rounds applied. Regenerating ATDD for already-green ACs is not red-phase. Only genuinely-deferred items can be true red.

## Detected Stack

- `fullstack` — .NET 10 (xUnit v3 + Shouldly + NSubstitute) is the primary surface; Python `eng/release_evidence.py` governance helper is exercised end-to-end through xUnit `ProcessResult` invocations.
- `test_stack_type` was `auto`; C# `.csproj` + `package.json` (Playwright E2E) detected → `fullstack`.
- Browser automation profile not relevant for Story 12.4 (workflow + helper governance only).

## Target Def Items

| Def ID | Severity | Source row | Test name | Expected color today |
| --- | --- | --- | --- | --- |
| CR-12-4-Def14 | CRITICAL | Story line 350; Def20 cross-link | `Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow` | RED |
| CR-12-4-Def14 | CRITICAL | Story line 350 | `Story12_4_Def14_AttestationsWritePermission_IsRestored` | RED |
| CR-12-4-Def22 | MEDIUM | Story line 512 | `Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent` | RED |
| CR-12-4-Def23 | MEDIUM | Story line 513 | `Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked` | RED |
| CR-12-4-Def25 | MEDIUM | Story line 515 | `Story12_4_Def25_PackagesEmptyArrayFixture_EmitsPackageRowsRequiredDiagnostic` | RED |

## Generated Tests

Location: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
(appended in a single block just before the `private static string ExtractOnBlock(...)` helper).

Each test carries:
- `[Trait("Category", "Quarantined")]` so the main blocking lane (`Category!=Quarantined`) skips them
- The required `// frontcomposer-quarantine: issue=… owner=… reason=… reintroduction=5-nightly-passes` metadata comment (validated by `python .github/scripts/ci_governance.py validate-quarantine-metadata`)
- An AC reference in every Shouldly failure message so a future failure points an operator back at the ACs

### Test 1 — `Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`
- Asserts `actions/attest-build-provenance@v2` appears in `.github/workflows/release.yml` AND runs before the `Run semantic-release` step.
- Today: `release.yml:64` carries the comment "attestation creation remains deferred to CR-12-4-Def14" and no provenance step exists → RED.

### Test 2 — `Story12_4_Def14_AttestationsWritePermission_IsRestored`
- Asserts the release job permissions include both `attestations: write` and `id-token: write`.
- Today: round-8 CR-12-4-P189 narrowed the scope to `attestations: read`, and `id-token: write` is also absent → RED.

### Test 3 — `Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent`
- Asserts a `cases[].name="dry-run-with-side-effect-attempt"` entry exists in `release-readiness-cases.json` with `override.context.dry_run=true` AND `override.checks.dry_run_side_effect_attempt=true`, and that it expects `blocked` + `publish_authorized=false`.
- Today: the existing `dry-run-from-dispatch` fixture covers only the single `context.dry_run` axis → compound case missing → RED.

### Test 4 — `Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked`
- Writes a temp manifest with empty `release_definition_fingerprints` and invokes `eng/release_evidence.py verify-manifest --manifest <temp> --no-root`. Asserts non-zero exit and that the diagnostic names `release_definition_fingerprints`.
- Today: `verify-manifest --no-root` does not enforce the fingerprints contract → exit 0 → RED.

### Test 5 — `Story12_4_Def25_PackagesEmptyArrayFixture_EmitsPackageRowsRequiredDiagnostic`
- Asserts a `cases[].name="packages-empty-array"` fixture exists with `override.manifest.packages = []`; runs `classify-fixtures` and asserts the case classifies as `blocked` AND the `grouped_reasons.blocking` array contains `"package rows are required"`.
- Today: fixture missing → RED.

## Red-Phase Verification

```
dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj `
  --configuration Release --no-build `
  --filter "FullyQualifiedName~Story12_4_Def"
# Result: Failed: 5, Passed: 0, Skipped: 0, Total: 5 (as of 2026-05-20)
```

Main-lane exclusion verified:
```
dotnet test ... --filter "FullyQualifiedName~Story12_4_Def&Category!=Quarantined"
# Result: No test matches (the Quarantined trait correctly excludes them from the blocking lane)
```

Quarantine metadata validator:
```
python .github/scripts/ci_governance.py validate-quarantine-metadata --root .
# Exit 0
```

Build status:
```
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release
# Build succeeded. 0 Warning(s) 0 Error(s)
```

## Green-Phase Owner Action (per test)

When the corresponding Def item lands, the test should go green and the Quarantined trait + frontcomposer-quarantine comment must be removed in the same change.

- **Def14** — Wire `actions/attest-build-provenance@v2` into `.github/workflows/release.yml` (likely outside `prepareCmd` because of the semantic-release `${nextRelease.version}` timing issue noted in CR-12-4-P74). Restore `attestations: write` and add `id-token: write` to job permissions. Bind the attestation bundle into the sealed manifest via `prepare-manifest --attestation-bundle …`. Both Def14 tests then go green together.
- **Def22** — Add a new `cases[]` entry `dry-run-with-side-effect-attempt` to `tests/ci-governance/fixtures/release-readiness-cases.json` with `override.context.dry_run=true`, `override.checks.dry_run_side_effect_attempt=true`, `expected_classification="blocked"`, `expected_publish_authorized=false`.
- **Def23** — Either extend `verify-manifest --no-root` to call `manifest_diagnostics` on the missing fingerprints contract, OR add a dedicated `--require-fingerprints` flag emitted in the diagnostic name. Update `_RELEASE_DEFINITION_FILES` enforcement to apply under `--no-root` as well.
- **Def25** — Add `cases[]` entry `packages-empty-array` with `override.manifest.packages = []`, `expected_classification="blocked"`. The existing `manifest_diagnostics` already queues `"package rows are required"` when `packages is None` — ensure the same diagnostic fires for `packages == []` (likely a one-line predicate change).

## Dev-Story Handoff

For BMM `dev-story`, the artifact bundle is:

- **Story file:** `_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md`
- **Story id / key:** `12.4` / `12-4-trusted-release-evidence-dry-run`
- **Red tests:** 5 new `[Fact]` methods named `Story12_4_DefN_*` in `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- **Quarantine state:** all 5 currently `Category=Quarantined`; trait + metadata comment must be removed when the matching Def item lands
- **Reintroduction policy:** standard `5-nightly-passes` (per `tests/README.md`) — five consecutive `Category=Quarantined`-lane nightly passes on a protected branch before a test can leave quarantine
- **No fixture/workflow edits performed yet** — this is pure ATDD red-phase. Green-phase work belongs in a follow-up dev story or directly in a Def14/Def22/Def23/Def25 patch.

## Scope Guardrails (per project context)

- Did not change `.github/workflows/release.yml`, `eng/release_evidence.py`, `.releaserc.json`, or `eng/release-package-inventory.json`.
- Did not add new fixture cases (the red-phase tests are the contract; the green-phase patch adds them).
- Did not touch `Hexalith.EventStore/**` or `Hexalith.Tenants/**` (root-level submodules out of scope).
- Did not introduce new packages or analyzers; relied on existing xUnit + Shouldly + System.Text.Json imports already in the test file.
- All comments and assertions follow CRLF, UTF-8, 4-space indent, and the existing project file style.
