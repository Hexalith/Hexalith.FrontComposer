---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-quality-evaluation', 'step-04-generate-report']
lastStep: 'step-04-generate-report'
lastSaved: '2026-05-20'
reviewer: 'Murat (TEA)'
storyId: '12.4'
storyKey: '12-4-trusted-release-evidence-dry-run'
storyStatus: 'review (red-phase ATDD scaffolds for open Def items)'
reviewScope: '5 new [Fact] methods added by commit 9b82ea9 in CiGovernanceTests.cs (lines 1639-1850), plus the supporting RunPython/ProcessResult helpers'
stackType: 'backend governance (xUnit v3 + Shouldly; shells out to Python release_evidence.py)'
executionMode: 'sequential'
inputDocuments:
  - _bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md
  - _bmad-output/test-artifacts/atdd-checklist-12-4-trusted-release-evidence-dry-run.md
  - _bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md
  - _bmad-output/project-context.md
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
  - tests/ci-governance/fixtures/release-readiness-cases.json
  - eng/release_evidence.py
  - .github/workflows/release.yml
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/selective-testing.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-healing-patterns.md
---

# Test Quality Review — Story 12.4 Red-Phase ATDD Scaffolds

**Reviewer:** Murat (TEA)
**Date:** 2026-05-20
**Review mode:** Create (sequential evaluation)
**Context:** Story 12.4 is `review` with 35/35 ACs implemented and 11 rounds of CR applied. The five new tests under review are **red-phase scaffolds** for genuinely-deferred CR items (`Def14`, `Def22`, `Def23`, `Def25`); they are quarantined and intentionally fail today. Quality criteria still apply — "test currently fails" is **not** a finding.

---

## Executive Summary

**Verdict:** Strong governance-test discipline overall, but two HIGH temp-file cleanup issues and one MEDIUM coverage gap need correction before the red phase can be trusted to flip cleanly to green. The tests carry the same positive patterns observed in earlier rounds (AC↔Def↔diagnostic traceability, consistent quarantine metadata, shared `RunPython` helper), and one of the five (Def25) is exemplary in how it pins both fixture shape AND classifier behavior.

**Risk signal:** The Def22 test pins fixture shape only, not classifier enforcement — symmetric to Def25 but with one weaker axis. The Def23 manifest fixture is over-poisoned: today's exit-code assertion is satisfied by the seal-hash check (`"0"` is not a valid sha256), not by the missing-fingerprints contract. When Def23 lands, the diagnostic-string assertion will still catch it, but the test conflates two failure modes — making the red→green transition harder to read.

**Counts in scope:**
- C# new: 1 file modified, 5 `[Fact]` methods, ~210 lines (incl. comments + metadata)
- All 5 carry `[Trait("Category", "Quarantined")]` + the required `frontcomposer-quarantine` metadata comment
- Net new fixtures, workflow edits, helper changes: zero (correct red-phase posture)

**Go/No-Go for green-phase work on Def14/Def22/Def23/Def25:** **Go**, with mandatory pre-green fixes **F1 + F2** (temp-file cleanup — both are 3-line patches). **F3** (Def22 classifier round-trip) is strongly recommended during green; **F5** (Def23 fixture conflation) is a 5-minute refactor that pays back in clearer failure modes. All other findings are LOW and can ride along with the green-phase patches for each Def item.

---

## Findings — Severity-Ordered

### ⚠️ HIGH

#### F1. `Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked` leaks the temp manifest
**File:** [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1740-1776](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1740)
**Violates:** `test-quality.md` DoD — "Self-Cleaning — Use fixtures with auto-cleanup or explicit teardown."

```csharp
string manifest = Path.Combine(Path.GetTempPath(), $"fc-manifest-no-fingerprints-{Guid.NewGuid():N}.json");
File.WriteAllText(manifest, """{ ... }""");

ProcessResult result = RunPython(root, [...]);

result.ExitCode.ShouldNotBe(0, ...);
// ❌ no File.Delete(manifest); the temp file is leaked
```

**Why it matters:** The Guid suffix prevents same-run collision, but every CI run + every local dev re-run accretes another `fc-manifest-no-fingerprints-*.json` file. On a hosted Windows agent this is bounded by sandbox lifetime; on developer machines and self-hosted runners it accumulates indefinitely. Per project context (`output must sanitize local absolute paths… and unbounded logs`) this also leaks a path with a predictable prefix into `%TEMP%`.

**Fix:**
```csharp
[Fact]
[Trait("Category", "Quarantined")]
public void Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked() {
    string root = RepositoryRoot();
    string manifest = Path.Combine(Path.GetTempPath(), $"fc-manifest-no-fingerprints-{Guid.NewGuid():N}.json");
    try {
        File.WriteAllText(manifest, """{ ... }""");
        ProcessResult result = RunPython(root, [...]);
        result.ExitCode.ShouldNotBe(0, "AC30/Def23: …");
        (result.Output + result.Error).Contains("release_definition_fingerprints", StringComparison.Ordinal).ShouldBeTrue("…");
    } finally {
        if (File.Exists(manifest)) File.Delete(manifest);
    }
}
```
Or — preferable for fleet of governance tests that all shell out to Python with temp artifacts — introduce a small `using TempFile { Path; … }` helper that `File.Delete`s on dispose. That makes the next leaked-temp regression impossible by construction.

---

#### F2. `Story12_4_Def25_PackagesEmptyArrayFixture_EmitsPackageRowsRequiredDiagnostic` leaks the temp `classify-fixtures` output
**File:** [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1820-1849](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1820)
**Violates:** Same as F1.

```csharp
string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-{Guid.NewGuid():N}.json");
ProcessResult result = RunPython(root, [
    "eng/release_evidence.py", "classify-fixtures",
    "--fixtures", fixturesPath,
    "--output", output,
]);
// ❌ no File.Delete(output); the output JSON is leaked
```

**Fix:** Identical pattern to F1 — wrap in `try { … } finally { File.Delete(output); }`, or use a shared `TempFile` helper. If the helper is introduced, retrofit Def23 and Def25 in the same patch.

---

### 📝 MEDIUM

#### F3. `Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent` proves fixture shape only — does not round-trip through `classify-fixtures`
**File:** [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1698-1735](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1698)

Def22 asserts:
- `cases[].name="dry-run-with-side-effect-attempt"` exists
- `override.context.dry_run=true` AND `override.checks.dry_run_side_effect_attempt=true`
- `expected_classification="blocked"` AND `expected_publish_authorized=false`

What's missing: a round-trip through `eng/release_evidence.py classify-fixtures` that proves the classifier **actually enforces** the rule. The Def25 test does this correctly (lines 1819-1849) — Def22 should mirror it.

**Why it matters:** A future regression in the classifier where `checks.dry_run_side_effect_attempt=true` is silently ignored leaves the fixture intact and the Def22 test passes — but AC24 (the contract being verified) is violated in production. The fixture is the *input*; the classifier behavior is the *contract*. Without the round-trip, the test guards only the input.

**Fix (mirror Def25):**
```csharp
[Fact]
[Trait("Category", "Quarantined")]
public void Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent() {
    // … existing fixture-shape assertions stay …

    // NEW: round-trip through classify-fixtures and assert the typed result.
    string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def22-{Guid.NewGuid():N}.json");
    try {
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py", "classify-fixtures",
            "--fixtures", fixturesPath,
            "--output", output,
        ]);
        result.ExitCode.ShouldBe(0, result.Error);

        using JsonDocument resultDoc = JsonDocument.Parse(File.ReadAllText(output));
        JsonElement dryRunSideEffect = resultDoc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "dry-run-with-side-effect-attempt");
        dryRunSideEffect.GetProperty("classification").GetString().ShouldBe(
            "blocked",
            "AC24/Def22: classify-fixtures must report blocked for the compound dry-run+side-effect case.");
        dryRunSideEffect.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse(
            "AC24/Def22: classify-fixtures must not authorize publishing for a dry-run that attempts a side effect.");
    } finally {
        if (File.Exists(output)) File.Delete(output);
    }
}
```
This also resolves F1/F2 for Def22 in the same patch (use the temp-file pattern from the outset).

---

#### F4. Def14 workflow checks use raw `IndexOf`/`Contains` on the full workflow string — YAML structure, comments, and step-skip flags are not enforced
**File:** [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1655-1693](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1655)

Both Def14 tests treat `release.yml` as a flat string:

```csharp
workflow.Contains("actions/attest-build-provenance@v2", StringComparison.Ordinal).ShouldBeTrue(...);
int attestStepIdx = workflow.IndexOf("actions/attest-build-provenance@v2", StringComparison.Ordinal);
int semanticReleaseIdx = workflow.IndexOf("Run semantic-release", StringComparison.Ordinal);
attestStepIdx.ShouldBeLessThan(semanticReleaseIdx, ...);
```

```csharp
workflow.Contains("attestations: write", ...).ShouldBeTrue(...);
workflow.Contains("id-token: write", ...).ShouldBeTrue(...);
```

**Three concrete blind spots:**
1. **Comments and doc anchors:** A line such as `# TODO: rewire actions/attest-build-provenance@v2 (CR-12-4-Def14)` makes both `Contains` and `IndexOf` succeed, while no step is actually wired. The Def14 fix needs to land as a real YAML step, not a comment.
2. **Step-level skip:** A wired step with `if: false` or `continue-on-error: true` passes the substring test but is functionally absent. The companion test `Gate2bGovernanceStep_IsNotMarkedAdvisory` (line 31) already establishes the pattern for guarding against `continue-on-error: true` on a named step — replicate it for the attestation step.
3. **Permission scope conflation:** `attestations: write` could appear at the workflow-level `permissions:` while the release job still has `attestations: read` (the inverse is also possible). The substring assertion does not distinguish workflow-level vs job-level scope; a partial green is undetectable.

**Why it matters:** Story 12.4 is, by design, the gate against false release-ready records. Def14 is the CRITICAL item; the wire-up check is the last line of defense before AC9 is structurally reachable. Substring fidelity on the gate test is below par for the rest of this story's review surface.

**Fix (Def14 Test 1):** Use the existing `ExtractNamedStep` helper (lines 1867-1878) to scope the assertion to the actual step body, and assert the step is not advisory:
```csharp
string root = RepositoryRoot();
string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));

// Pin the step exists and is named (not just text).
string attestStep = ExtractNamedStep(workflow, "Generate build provenance attestation");
attestStep.ShouldContain("actions/attest-build-provenance@v2",
    "AC9/Def14: …");
attestStep.ShouldNotContain("if: false",
    "AC9/Def14: attestation step must not be conditionally disabled.");
attestStep.ShouldNotContain("continue-on-error: true",
    "AC9/Def14: attestation step must not be advisory.");

// Ordering by named-step position rather than raw substring.
int attestIdx = workflow.IndexOf("- name: Generate build provenance attestation", StringComparison.Ordinal);
int semanticReleaseIdx = workflow.IndexOf("- name: Run semantic-release", StringComparison.Ordinal);
attestIdx.ShouldBeGreaterThanOrEqualTo(0, "step is missing.");
attestIdx.ShouldBeLessThan(semanticReleaseIdx, "AC9/Def14: attest must run before semantic-release.");
```
(The exact step name is a Def14 decision; pick one and pin it.)

**Fix (Def14 Test 2):** Extract the release job's `permissions:` block (the workflow has the helper material; a one-off `ExtractJobPermissions` paralleling `ExtractOnBlock` is ~5 lines) and assert the job-scoped permissions, not the entire file:
```csharp
string jobPermissions = ExtractJobPermissionsBlock(workflow, jobId: "release");
jobPermissions.ShouldContain("attestations: write",
    "AC9/Def14: the release job — not just the workflow — must have attestations: write.");
jobPermissions.ShouldContain("id-token: write",
    "AC9/Def14: the release job must have id-token: write for OIDC.");
jobPermissions.ShouldNotContain("attestations: read",
    "AC9/Def14: the narrowed `attestations: read` from round-8 CR-12-4-P189 must be replaced, not duplicated.");
```

---

#### F5. The Def23 manifest fixture is over-poisoned — exit-code assertion can be satisfied by the seal check rather than the fingerprints contract
**File:** [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1748-1776](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1748)

The fixture manifest sets `"seal": { "algorithm": "sha256", "hash": "0", ... }`. In [eng/release_evidence.py:816-817](eng/release_evidence.py:816):

```python
if not isinstance(seal, dict) or seal.get("algorithm") != "sha256" or not looks_like_sha256(seal.get("hash")):
    diagnostics.append("manifest seal with sha256 hash is required")
```

`"0"` does not look like a sha256, so the seal check fails today. That alone produces a non-zero exit, which already satisfies `result.ExitCode.ShouldNotBe(0, ...)`. The test passes the first assertion **for the wrong reason** — the fingerprints contract is not being exercised on the exit-code axis. Only the diagnostic-string assertion (`Contains("release_definition_fingerprints")`) actually tests the Def23 contract today (and that assertion *correctly* fails red until Def23 lands).

**Why it matters:** After Def23 lands, the test should turn green ONLY when the new contract fires. If a future patch flips the helper's behavior to detect empty `release_definition_fingerprints` under `--no-root`, AND the seal check is independently weakened or removed, the test could silently pass on the wrong contract. The two assertions are not orthogonal: they both depend on the *same* fixture having *several* failure modes.

**Fix:** Build a manifest that would otherwise pass under `--no-root` — valid seal hash (compute it inline with `sha256` over the canonical serialization), all required fields populated with `looks_like_sha256`-shaped values — so the ONLY failing axis is `release_definition_fingerprints: {}`:

```csharp
// Build a manifest whose only failing contract under --no-root is the empty fingerprints.
var manifestObj = new SortedDictionary<string, object?> {
    ["commit_sha"] = "abc123",
    ["tag"] = "v1.2.3",
    ["run_id"] = "42",
    ["workflow_ref"] = "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
    ["sbom_hash"] = new string('a', 64),
    ["benchmark_summary_hash"] = new string('c', 64),
    ["helper_version"] = new { version = "1.0.0", content_sha256 = new string('f', 64) },
    ["release_definition_fingerprints"] = new Dictionary<string, string>(),  // <-- the only failure axis
    ["packages"] = Array.Empty<object>(),
};
string canonical = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { /* sorted keys */ });
string sealHash = Sha256Text(canonical);  // existing helper at line 1930
manifestObj["seal"] = new { algorithm = "sha256", hash = sealHash, sealed_at = "2026-05-19T00:00:00+00:00" };

File.WriteAllText(manifest, JsonSerializer.Serialize(manifestObj, ...));
```
Now the exit-code assertion is meaningful: any non-zero exit demonstrates the fingerprints contract fired. Combined with the existing diagnostic-string check, the test pins the Def23 invariant without ambiguity.

(`packages: []` will need attention too — the `manifest_diagnostics` flow checks `packages` separately. The intent is that EVERYTHING ELSE passes — so the fixture should mirror a real sealed manifest including a non-empty packages array, OR the test should explicitly note that packages is out of scope for `--no-root`. Today's `verify_manifest --no-root` path skips drift checks but still inspects packages → seal/etc. Check the `verify_manifest` flow under `--no-root` before finalizing the fixture.)

---

### 🔹 LOW

#### F6. `GetBoolean()` on Def22/Def25 fixture fields will throw `InvalidOperationException` if a fixture author writes `"true"` (string) instead of `true` (boolean)
**Files:** [CiGovernanceTests.cs:1724-1727, 1733](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1724), [1726-1727](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1726), [1815](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1815)

```csharp
override_.GetProperty("context").GetProperty("dry_run").GetBoolean().ShouldBeTrue(...);
```

If a future fixture author writes `"dry_run": "true"`, `GetBoolean()` throws and the test reports a raw `InvalidOperationException` instead of a typed Shouldly failure. Wrap with `ValueKind` check first for a cleaner diagnostic surface:

```csharp
JsonElement dryRunNode = override_.GetProperty("context").GetProperty("dry_run");
dryRunNode.ValueKind.ShouldBe(JsonValueKind.True,
    "AC24/Def22: context.dry_run must be a JSON boolean true (not the string \"true\").");
```

---

#### F7. Def23 diagnostic-string assertion is too loose
**File:** [CiGovernanceTests.cs:1774-1775](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1774)

```csharp
(result.Output + result.Error).Contains("release_definition_fingerprints", StringComparison.Ordinal).ShouldBeTrue(...);
```

A regression where the helper logs the field name in a *success* context (e.g., debug output `release_definition_fingerprints recomputed: 0 entries`) and exits non-zero on an unrelated axis would silently pass this test. Pin the actual diagnostic verbiage to match the helper's existing pattern at [release_evidence.py:828](eng/release_evidence.py:828):

```csharp
(result.Output + result.Error).Contains("manifest missing release_definition_fingerprints", StringComparison.Ordinal).ShouldBeTrue(...);
```
…or whatever diagnostic phrase Def23 ends up emitting under `--no-root`. The fix is "pick a contract, then pin it." Today the contract is described in prose only.

---

#### F8. `.Single(...)` in Def25 throws on missing element rather than producing a Shouldly assertion
**File:** [CiGovernanceTests.cs:1830-1831](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1830)

```csharp
JsonElement emptyPackagesResult = resultDoc.RootElement.GetProperty("results").EnumerateArray()
    .Single(r => r.GetProperty("name").GetString() == "packages-empty-array");
```

If the classifier output ever stops including the case (e.g., the fixture name is renamed), `.Single` throws `InvalidOperationException` with a generic message. Use `.SingleOrDefault(...)` and a Shouldly assertion for an actionable failure:

```csharp
JsonElement? emptyPackagesResult = resultDoc.RootElement.GetProperty("results").EnumerateArray()
    .Cast<JsonElement?>()
    .SingleOrDefault(r => r!.Value.GetProperty("name").GetString() == "packages-empty-array");
emptyPackagesResult.ShouldNotBeNull(
    "AC12/Def25: classify-fixtures must include results for the packages-empty-array case.");
```

---

#### F9. Def22 and Def25 redundantly re-read and re-parse `release-readiness-cases.json`
**File:** [CiGovernanceTests.cs:1704-1717, 1789-1801](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1704)

Both tests perform the same `File.ReadAllText` + `JsonDocument.Parse` + `EnumerateArray.foreach(name == "...")` pattern. The duplication is minor (≈10 lines × 2), but as more Def-test fixtures land, a `TryFindFixtureCase(string fixtureJson, string name, out JsonElement match)` helper amortizes well and keeps tests focused on their unique assertions.

Not blocking — flag as a candidate for the shared helper introduced for F1/F2 cleanup.

---

#### F10. `RunPython`'s 30-second timeout is generous but unscoped per test
**File:** [CiGovernanceTests.cs:1914](tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1914)

If Python cold-start on a hosted Windows agent under heavy load exceeds 30s (rare but possible), the test reports `governance script timed out` from a -1 exit. Mostly fine, but worth noting that Def23 and Def25 (the Python-shelling tests) inherit this timeout — and a future flaky-timeout incident is a candidate for the test-healing catalog (cf. `test-healing-patterns.md` — failure pattern "process timeout under load").

Pre-existing infrastructure; out of scope for this story's red phase but worth a future infra hardening pass.

---

## Positive Patterns (keep doing these)

1. **Quarantine trait + metadata comment, consistently applied.** All five tests carry `[Trait("Category", "Quarantined")]` plus the project-mandated `frontcomposer-quarantine: issue=… owner=… reason=… reintroduction=5-nightly-passes` comment in the correct format. The ATDD checklist explicitly verifies this validates via `python .github/scripts/ci_governance.py validate-quarantine-metadata`.

2. **AC↔Def↔diagnostic traceability in every failure message.** Every Shouldly assertion message names the AC and Def (`AC9/Def14: …`, `AC24/Def22: …`, `AC30/Def23: …`, `AC12/Def25: …`). When a test eventually fails in CI (post-green regression), the operator's first read tells them exactly which contract broke. This matches the project's broader style and the Story 3-2 review's positive pattern.

3. **Def25 is the gold-standard pattern for fixture+classifier coupling.** It pins:
   - The fixture exists with the expected override shape (`manifest.packages == []`)
   - Round-trips through `classify-fixtures` to prove the classifier actually treats this case as blocked
   - Asserts the typed diagnostic surfaces in `grouped_reasons.blocking`
   This is what AC-level governance tests should look like. Replicate in Def22 (see F3).

4. **Per-test inline comment explains *what* would make it green.** Each test's lead comment describes the deferred state and what the green-phase patch must do. New maintainers can pick up the file without consulting the ATDD checklist.

5. **Reuse of `RunPython` / `ProcessResult` / `RepositoryRoot` / `ExtractNamedStep` helpers.** No per-test reinvention of the process wrapper or workflow parser. When F1/F2 cleanup lands (or when an `ExtractJobPermissions` helper is added per F4), the new utilities slot into the same conventions.

6. **No hidden assertions in helpers.** All `Should*` calls are in the test body, with explicit messages. Matches `test-quality.md` Example 3.

7. **No hard waits, no conditional flow, no try/catch for control flow.** The tests are deterministic by construction. The only retry surface is `Process.WaitForExit(30_000)` and that is a hard upper bound, not a poll.

8. **Shared fixture (`release-readiness-cases.json`) is read but not mutated.** Multiple tests read the file; none write to it. Parallel-safe for xUnit's default collection-per-class parallelism.

---

## Must-fix Before Closing the Story (Blocks Story 12.4 Sign-off)

1. **F1** — Wrap Def23's temp manifest write in `try/finally` + `File.Delete` (or introduce a shared `TempFile` helper). Cheap, mandatory.
2. **F2** — Same treatment for Def25's `classify-fixtures` output file.

Both are 3-line patches per test. Pair them with a one-line helper if the pattern looks likely to recur (which it will when Def14/22/23/25 ship green-phase work that adds more Python-shelling tests).

---

## Strongly Recommended Before Def14/22/23/25 Green Phase

3. **F3** — Add classifier round-trip to Def22. The test cannot prove AC24 without it.
4. **F5** — Refactor the Def23 manifest fixture so only the fingerprints axis is failing. Untangles a 2-axis red into a 1-axis red and makes the green transition unambiguous.
5. **F4** — When wiring Def14, simultaneously upgrade both Def14 tests from raw `Contains/IndexOf` to `ExtractNamedStep` / a new `ExtractJobPermissions` helper. The Def14 green patch is the natural home for this work.

---

## Acceptable to Defer

- **F6** — `GetBoolean()` fragility (minor; type drift in fixture data is unlikely to slip past JSON-shape review).
- **F7** — Pin the exact diagnostic string in Def23 (depends on the verbiage the Def23 green-phase author picks; add when Def23 lands).
- **F8** — `.Single` → `.SingleOrDefault` + Shouldly (polish; not a correctness gap).
- **F9** — Shared fixture-lookup helper (polish; do it opportunistically).
- **F10** — Process timeout hardening (infra-level; future story).

---

## Not Covered (Out of Scope)

- **Coverage mapping / gate decision** — routed to `trace` workflow per `test-review` step-01 directive.
- **Quarantine policy enforcement** — the `reintroduction=5-nightly-passes` clause is metadata only. No test asserts a quarantined test cannot leave quarantine without satisfying the nightly-pass evidence. This is a workflow-governance concern, not a Story 12.4 red-phase concern; tracked here for visibility.
- **The other ~85 pre-existing tests in `CiGovernanceTests.cs`** — only the 5 new tests added by commit `9b82ea9` are in scope per the user's `12.4` argument and the ATDD checklist's "gap-fill" scope statement.
- **Python helper unit tests** — Story 12.4's tests exercise `release_evidence.py` end-to-end via subprocess. No Python-level unit tests are in scope here. (Consider for a future infra story.)

---

## Quality Dimension Scores

Sequential evaluation against `test-quality.md` DoD and the loaded knowledge fragments (`test-levels-framework`, `test-priorities-matrix`, `selective-testing`, `test-healing-patterns`):

| Dimension | Score (0-100) | Notes |
|---|---:|---|
| **Determinism** | 92 | No hard waits, no conditionals, no try/catch for flow. Process timeout is a hard bound. JSON `GetBoolean()` is the only deterministic-by-data-shape weakness (F6). |
| **Isolation** | 70 | Temp-file leaks in Def23 + Def25 (F1, F2) are the main hit. Shared fixture is read-only across tests (good). No global mutable state. |
| **Maintainability** | 85 | Strong AC traceability, consistent quarantine pattern, good helper reuse. Def22's missing classifier round-trip (F3) and Def23's fixture conflation (F5) lower the score; F4's YAML-string-as-truth pattern is borderline maintainable for governance code. |
| **Performance** | 90 | All five tests complete in seconds; Process timeout is 30s upper bound. Python cold-start dominates Def23/Def25 cost. No parallelism issues; xUnit class-level collection isolation suffices. |
| **Overall (weighted: 30/25/25/20)** | **84** | "Solid scaffold with two cleanup items and one missing classifier round-trip" — Go to green with mandatory F1+F2 fixes. |

Coverage is intentionally out of scope per workflow contract (route to `trace`).

---

## Recommended Next Steps

1. **Patch F1 + F2 in the same commit** (estimated 5 minutes). Either inline `try/finally` or introduce a shared `TempFile` helper — the second pays back across Def22's recommended F3 patch and any future Python-shelling test.
2. **Apply F3 + F5 alongside the Def22 and Def23 green-phase patches** (each owns ~15-30 minutes of refactor). These are best landed with the dev who's already touching the surrounding fixture/helper.
3. **When Def14 lands, apply F4 in the same PR.** The `ExtractJobPermissions` helper is a 5-line addition; the named-step rewrite is a 1:1 substitution for the existing substring checks.
4. **Re-run this review in Validate mode** (`/bmad-testarch-test-review` → V) after the green-phase patches land, to confirm fixes hold and no new quality regressions appeared.
5. **Route the coverage/gate decision through `trace`** after the green phase completes — `test-review` does not score coverage by design.

---

_Review completed 2026-05-20. Murat signing off — the scaffold is well-formed; F1+F2 are mandatory cleanup; F3 + F5 strengthen the red→green transition; everything else is polish._
