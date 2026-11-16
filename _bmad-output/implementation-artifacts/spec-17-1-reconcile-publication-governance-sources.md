---
title: 'Story 17.1: Reconcile Publication-Governance Sources'
type: 'chore'
created: '2026-11-16'
status: 'in-progress'
route: 'dispatch'
story_id: '17.1'
story_key: '17-1-i-gov-src-1-reconcile-publication-governance-sources'
baseline_commit: 'eddca353d566554af527064925798ec827e07c36'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-17-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The governance sources disagree with the repository and with each other. The PRD (G-2, D-11), the identity register and the G2 request still name execution pin `4eb33928…` and gitlinks `2fba3497…` or `a32cb422…`. In reality the caller-owned `release-publish.yml` runs Builds `360a2b9c…` and the root gitlink is `65fd22f3…`. No source names EXT-BUILDS-1 or the candidate identities separately. The sources also say production is halted, yet `HEXALITH_RELEASE_PUBLISH_ENABLED` has read `true` since 2026-10-05 and v4.6.0–v6.0.0 were published.

**Approach:** Reconcile every live source so it separates five identities. The lineage anchor appears literally. The execution pin and gitlink are cited by the authority that holds them, with dated snapshots. EXT-BUILDS-1 is stated as not yet existing. Candidates are bound to evidence. Record the halt rule and its observed violation truthfully, and make the existing exact-SHA check name the drifting source and field.

## Boundaries & Constraints

**Always:**
- Use full lowercase 40-hex SHAs and read gitlinks with `git ls-tree`.
- Label every copied value as a dated snapshot.
- Preserve CRLF in `_bmad-output`.
- Record each reconciled source's sha256 at the delivery commit in this spec, for GOV-ACCEPT-2.
- Keep G-2, G-8 and OI-7 to OI-14 open.

**Never:**
- Change workflows, `eng/dependency-graph-policy.json`, gitlinks, repository variables or release posture.
- Rewrite dated or historical artifacts: validation reports, `reviews/`, approved proposals, historical tuples, or existing register rows.
- Add a validator, catalog, mirror, schema, lane or evidence packet.
- Call the active caller G-8 conformant, or treat `true`, a green run or environment approval as authorization.

**Decisions (human, 2026-11-16):**
- *Halt: record a truthful nonconformance.* The sources state the rule: literal `false` is a deny-only stop, `true` never authorizes, and no exception is approved. They also record the observed deviation as new register rows routed to the Release Owner (incident handling in 17.2, ledger rows in 17.14). The flag is `true` (updated 2026-10-05), and v4.6.0–v6.0.0 were published without an approved exception. `docs/release-trusted-publishing.md` gets the same wording. No variable or workflow change is made.
- *Drift check: cite the authority, improve the existing check.* The anchor is stated literally. The pin and gitlink cite their authority plus a dated snapshot. `validate_builds_identity` names the file, field, and expected and actual values. Document wording is checked by the Verification grep. No new validator is added.
- *Size: keep the full spec (~1,900 tokens).*

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pin drift | `builds-execution-sha` or the `prepare-domain-release@` pin differs from `--approved` | `release_contract.py builds` fails | Message names the file and field, with expected and actual SHAs |
| Label as identity | `--approved` is a tag, branch or short SHA | Rejected | Exact 40-hex required; the field is named |
| Gitlink bump | Root Builds gitlink moves | Documents stay true: they cite the authority plus a dated snapshot | None |
| Stale copy | Retired SHA presented as current | Appears only in rows labelled dated or historical | Grep check in Verification |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/prd.md`
  - Stale pins and topology at L21, L34 (G-2), L481, L489, L575, L696 (D-6) and L701 (D-11).
  - D-16 at L706 is correct.
  - D-12 at L702 is history; keep it.
- `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` L36–43 is the identity register.
  - The pin and gitlink are stale.
  - The anchor row is conflated with EventStore `buildsCatalogGitlink`.
  - EXT-BUILDS-1 and CI pin `7d2019adf4591a2c320a9288f76fc5f0f6905f65` are missing.
- `_bmad-output/planning-artifacts/architecture.md`
  - Anchor and halt are correct at L279–283 and L321–324.
  - No pin or gitlink authority is named, and the caller topology is unstated.
  - L372–383 are immutable.
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` (FC-DEP-1), L152–210: it never names EXT-BUILDS-1 or calls the active caller legacy or non-conformant.
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`: L11 has the stale "current execution pin", and the gitlink is missing.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`: AD-13 at L534–540 assumes a `domain-release.yml@<sha>` caller, and the flag blocker is at L81–93.
- `.../nonconformance-register-2026-09-08.md`: append rows only.
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`, L40–55 and L161–166.
- `docs/release-trusted-publishing.md`, L34–36: setting the flag to `true` is presented as the enabling step.
- `eng/release_contract.py:355` `validate_builds_identity` and `tests/eng/test_release_contract.py:293–368`.
  - This is the existing exact-SHA check, but its message is generic.
  - The file is outside the sealed workflow closure, so editing it does not de-authorize releases.
- Facts:
  - Anchor `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`.
  - Execution pin `360a2b9c4e96809365a7de785be9a68152d5ac28`, set in FC commit `2670339e8b748a8264d433f49f8544e5e41cded2`.
  - Gitlink `65fd22f37b9fad64b595841923cdbe050563ab99`, set in FC commit `c6233bdaf445d6a87e465a1e67f9eb266b0e322f`.
  - Builds ancestry is linear: `a8a50859` → `4eb33928` → `2fba3497` → `360a2b9c` → `65fd22f3`.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_contract.py`, `tests/eng/test_release_contract.py`: name the file and field, with expected and actual values, on each mismatch. Add mismatch and label fixtures.
- [x] The identity register, `prd.md`, `architecture.md`, FC-DEP-1, the G2 request, the spine and the GOV-1 story: separate the five identities, apply the halt decision's wording, and add a non-conformance statement for the caller.
- [x] Nonconformance register: append rows for the caller-owned publisher, the flag reading `true`, and the post-2026-10-05 publications.
- [x] `docs/release-trusted-publishing.md`: use the deny-only wording, and state that the path is legacy and not conformant.
- [x] `sprint-status.yaml` and this spec: update the status, the GOV-SRC-1 action, and the digest record.

**Acceptance Criteria:**
- Given all reconciled sources, when they are compared, then every identity, halt and exception claim agrees, and none calls the caller G-8 conformant.
- Given the existing checks, when the story is verified, then they pass, and only this spec records the digests.

### Review Findings

Review pass 2, 2026-11-16. Layers: Blind Hunter (BH), Edge Case Hunter (ECH), Verification Gap (VG) and Acceptance Auditor (AA), over `eddca353d566554af527064925798ec827e07c36..5a7e2a2ad0de47ddb566b7d23ac58ccee666e834`, excluding this spec and `epic-17-context.md`. At `5a7e2a2a` all nine Source Digest Record rows reproduce, by SHA-256 and by blob ID. `tests/eng/test_release_contract.py` runs 37 tests, all passing, and the live `builds` command exits 0.

- [x] [Review][Defer] No test runs the hardened `builds` gate against the committed `release.yml`/`release-publish.yml` pair — VG-1, BH-14 (medium) [tests/eng/test_release_contract.py] — deferred: it guards a path that must stay halted. Add the committed-caller acceptance test with the Epic 17 caller switch (GOV-B to GOV-H). Human decision, 2026-11-16 (option 1 of 3). A break today fails closed in `verify-source`, before `production` approval or any side effect. A committed-caller test also departs from the Hexalith baseline "Do not write tests that assert workflow YAML".
- [ ] [Review][Patch] FC-DEP-1 Consequences still states "Production publication remains halted" as fact, and the G2 request says "During the current halt" — AA-1 (medium) [_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md:239]
- [ ] [Review][Patch] v4.5.0 (2026-09-19) is missing from every "Observed nonconformance" paragraph, and PRD FR-24 and G-8 place it outside the halt. This contradicts NC-36 and PRD G-1 — ECH-6, BH-6, AA-2 (medium) [_bmad-output/planning-artifacts/prd.md:487]
- [ ] [Review][Patch] Resolution diagnostics omit the actual value. The non-160000 branch drops the `ls-tree` output, and both `git show` failures drop git's stderr — ECH-7, BH-3 (low) [eng/release_contract.py:506]
- [ ] [Review][Patch] The identity register omits the post-release execution pin: the `release-evidence.yml` `.hexalith/builds-execution` checkout `ref:`, authorized by the `post_release` rows. Row 2b also names no authorization authority (the `ci` rows) — BH-4 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [ ] [Review][Patch] Row 2a, `architecture.md` and FC-DEP-1 say `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate` enforces the full equality set. It compares only `\b`-terminated exact 40-hex matches, so a non-SHA or `-suffix` member goes undetected there — BH-2 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [ ] [Review][Patch] The policy-row values copied into row 2a's Authority cell carry no dated-snapshot label — AA-4 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [ ] [Review][Patch] Added text uses 12 abbreviated SHAs, and `3f0e3595…` never appears in full. This breaks the frozen "full lowercase 40-hex SHAs" rule — AA-3 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:47]
- [ ] [Review][Patch] Stale caller wording sits beside the rewritten text: the addendum Activation bullet's "Flipping the current mode flag first", the §1 lead-in "after the 2026-09-09 GOV-1 validation", and PRD D-6 "Amended 2026-09-09 to current execution truth". Each credits 2026-10-04 and 2026-11-16 state to 2026-09-09 — AA-6 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:18]
- [ ] [Review][Patch] The PRD G-2 pass condition hard-codes the legacy `prepare-domain-release@` field as a pin authority. It should defer to the identity register instead — AA-5 (low) [_bmad-output/planning-artifacts/prd.md:34]
- [ ] [Review][Patch] The addendum frontmatter still reads `updated: 2026-09-09`, and the §8 change map has no 2026-11-16 Story 17.1 row — BH-11 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:4]
- [ ] [Review][Patch] The REL-AI-1 `release_control` text still says the `HEXALITH_RELEASE_PUBLISH_ENABLED` path "is retired", which contradicts NC-35. Append a dated progress entry rather than rewriting the text — BH-10 (low) [_bmad-output/implementation-artifacts/sprint-status.yaml:427]
- [ ] [Review][Patch] The rewritten docs prerequisites name "green exact-source push CI" but omit the successful push Quality run that `release.yml` authenticates — BH-8b (low) [docs/release-trusted-publishing.md:61]
- [ ] [Review][Patch] `_PUBLISHER_DELEGATION_LINE` and the path constants are used only in diagnostics. The gate regex and the `git ls-tree`/`git show` arguments in `main()` repeat the literals, so a topology change can update one without the other — BH-15 (low) [eng/release_contract.py:358]
- [x] [Review][Defer] Eight `evaluator_authorizations.release` rows still authorize older `release.yml` blobs that delegate to the Builds `domain-release.yml` reusable, at `4eb33928…`, `0a3508b3…`, `99d5a46c…`, `a8a50859…` and `3f0e3595…`. NC-34 and row 2a cite only the `release-publish.yml` rows — BH-5 [eng/dependency-graph-policy.json] — deferred: pre-existing policy state (NC-16 covers only the `0a3508b3…` rows). Pruning or recording these rows needs a Release Owner decision and a policy change, which Story 17.1 may not make.

Rejected:
- ECH-1, ECH-2, BH-1 (low): case-variant or quoted `prepare-domain-release@`/`builds-execution-sha` decoys pass the gate; a lowercase `@main` decoy reproduced this. The gate is defense in depth: any edit to either workflow changes a blob pinned by `evaluator_authorizations`, so it needs a separate reviewed policy change. `CiGovernanceTests` is equally case-sensitive. The fix needs quote and case handling across both patterns and `_pin_value`.
- ECH-3 (low): a 40-hex tree or annotated-tag id passes `--commit`. `release.yml` passes `${{ github.sha }}`, and the fix would add a `cat-file -t` guard.
- ECH-4 (carried, pass 1 ECH-8): a non-UTF-8 blob raises a traceback.
- ECH-5 (low): fixtures inherit `GIT_DIR` and `GIT_INDEX_FILE`. The pattern predates this story (baseline L333–353), no hook runs this suite, and the fix would add environment scrubbing.
- BH-7 (false/low): the companion bullet already names each new row's closure path, and the gate's closed-register requirement covers them. Retitling the file or editing the bundle table would break "append rows only".
- BH-8a (carried, pass 1 BH-11 defer): there is no "do not dispatch" containment instruction.
- BH-9 (low): the published docs page cannot link into `_bmad-output`, and its snapshot is already dated.
- BH-12 (low): the raw API read is recorded in Implementation Notes. Who changed the variable is incident investigation, owned by Story 17.2, and this spec forbids new evidence packets.
- BH-13 (false): the PRD frontmatter already reads `product_approval: pending-reapproval`, G-2 states that Story 17.1 is not acceptance, and GOV-ACCEPT-2 (Story 17.12) is the acceptance step.
- BH-11, FC-DEP-1 part (false): its new section "amends none of the decisions above", so `amended: 2026-10-28` stays accurate.
- BH-16 (false): the ledger format has no owner field, and the bmad-loop sweep triages every open entry.
- BH-17, AA-8 (rejected, because the fix edits this spec): the frontmatter and File List statuses disagree with `sprint-status.yaml`. This is carried from pass 1 BH-17/ECH-12, and the review's status sync resolves it.
- AA-7 (false): the appended companion bullet rewrites no existing row. The frozen rule forbids rewriting rows, not adding a bullet, and pass 1 BH-15 already patched it.

## Spec Change Log

## Review Triage Log

Pass 1, 2026-11-16. Layers: Blind Hunter (BH), Edge Case Hunter (ECH) and Verification Gap (VG), over the diff from baseline `eddca353d566554af527064925798ec827e07c36`.

| Pass | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 1 | VG-1 `_shown` JSON escaping of candidate-controlled values is untested | low | patch | The gap was pre-verified. Replacing `json.dumps` with plain quoting left all 9 builds tests green, so control characters could reach the release log unescaped. |
| 1 | VG-2 CLI resolution errors (workflow or publisher missing, commit in the gitlink error, malformed-gitlink actual) are untested | low | patch | The gap was pre-verified. Three mutation probes survived. |
| 1 | VG-3, ECH-5, ECH-6, BH-13a: the delegation diagnostic reports `actual none` or a value identical to the expected one | low | patch | The reporting regex captures only an unquoted local path with nothing after it. A trailing comment, a remote `domain-release.yml@` reference, quoting, wrong indentation or CRLF are all misreported. |
| 1 | ECH-1, ECH-2, ECH-3, BH-14a: the decision ignores observed values | medium | patch | The narrow regexes decide alone. A second non-SHA `builds-execution-sha`, a commented `# uses: …@<sha>` beside a live `@main`, or `@<sha>-suffix` all pass. That violates the matrix row "pin differs → fails". The `\s+` anchor also crosses newlines. |
| 1 | BH-14b `@(\S+)` truncates `${{ … }}` to `"${{"` | low | reject | Diagnostic text only. The value is still rejected, and the case is unlikely. |
| 1 | ECH-9, BH-13b: `--commit` accepts a label or a `-`-prefixed value | low | patch | Probe: `--commit HEAD` exits 0. The epic requires that a label cannot satisfy a commit comparison. |
| 1 | ECH-7, BH-13c: an unknown commit is reported as a missing 160000 gitlink, and git's stderr is dropped | low | patch | The new message names a cause that did not occur when `git ls-tree` exits non-zero. |
| 1 | ECH-8 non-UTF-8 blob raises a traceback | low | reject | Pre-existing decode path. Unlikely, and the fix adds a guard. |
| 1 | ECH-10 new CLI fixture commits without `-c commit.gpgsign=false` | low | patch | Direct correction. Other fixtures in the file already pass the flag. |
| 1 | ECH-14 early raises stop at the first failure | low | reject | Unlikely, and accumulating every error means restructuring. A malformed `--approved` makes later comparisons meaningless anyway. |
| 1 | BH-3, ECH-15: register row 2a says `release_contract.py builds` fails if `env.BUILDS_EXECUTION_SHA` or `HEXALITH_BUILDS_EXECUTION_SHA` differ | medium | patch | False claim. The script checks only the input and the action pin, with `--approved` taken from the caller's own env. The full equality set is enforced by `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate`. |
| 1 | BH-4, BH-5: the sources omit that the `evaluator_authorizations` release rows authorize execution of the non-conformant caller | medium | patch | Verified: the policy's `release` rows name `release-publish.yml` at `c97156de…`, `2670339e…` and `31c6ecd0…`. Neither register row 2a nor NC-34 records this. |
| 1 | BH-6 NC-34 omits `prepare-candidate` under `environment: production` | false | reject | Already covered: SCB-3 (NC-13, NC-32) requires removing `production` from candidate and build jobs. |
| 1 | BH-7 floating `@main` Builds refs (quality, codeql, commitlint, dependency-review, nightlies) | medium | defer | Real (verified by grep) but pre-existing, and not caused by this story. |
| 1 | BH-8, ECH-16: the ancestry line says "linear", omits `7d2019ad…`, and row 2b lacks the NC-30 caveat | low | patch | Verified: `4eb33928..65fd22f3` includes merge commits. The CI pin sits between `360a2b9c…` and `65fd22f3…`. The spec Code Map half is rejected because it would edit this spec. |
| 1 | BH-9 "Production releases remain halted" still reads as fact in the G2 request, GOV-1 story, architecture and docs | medium | patch | It contradicts the adjacent observed-nonconformance text. It must read as the requirement, with the halt not in effect. |
| 1 | BH-10, ECH-11, ECH-13: the docs say setting `true` is not an enabling step, omit the literal-`false` branch, and dropped "dispatch from the current `main` commit" | medium | patch | Verified: the Builds action publishes only on literal `true`. `true` turns publication on mechanically but never authorizes it. |
| 1 | BH-11 no interim containment instruction, and NC-35 has no deadline or escalation | medium | defer | Pre-existing gap (the old page instructed `true`). Containment and deadlines belong to the Release Owner and Story 17.2. |
| 1 | BH-1 v4.5.0 (2026-09-19) and attempts from 2026-09-09 to 2026-10-05 are omitted, and NC-36 narrows G-1's ledger scope | medium | patch | v4.5.0 was published after the 2026-09-09 halt adoption. G-1 requires every publication-capable attempt, not only those since 2026-10-05. |
| 1 | BH-2 NC-36 overstates its verification: it claims all ten went through the NC-34 path and "NuGet.org" for eight packages | low | patch | Only the `hexalith.frontcomposer.contracts` index was read. Nine successful Release runs descend from `2670339e…`, and v4.6.0's run is unverified. |
| 1 | BH-12 PRD G-1 state says "four later tags have no rows" | low | patch | This contradicts the changed FR-24 text, since fourteen tags now lack rows. The D-9 sub-claim is false: D-9 already says re-approval is pending. |
| 1 | BH-15 the companion bullet says NC-34 to NC-36 sit outside every bundle, but NC-34's fix is the SCB-2/SCB-3 switch | low | patch | No closure path is named for the new rows. |
| 1 | ECH-17 the spine never names EXT-BUILDS-1 or the five identities | low | patch | Verified: `grep -c EXT-BUILDS-1` on the spine returns 0. |
| 1 | ECH-18 addendum change-map row "recording current `a32cb422…`" | false | reject | The row is the dated 2026-09-09 change-map entry, describing state at that date. |
| 1 | BH-16 `epic-17-context.md` hides the incident from later stories | false | reject | Six planning artifacts are now newer than the context file, so step-01's validity rule recompiles it from the reconciled sources on the next run. |
| 1 | BH-17, ECH-12: spec, story row and GOV-SRC-1 statuses disagree | low | patch | Transient within the workflow. Align GOV-SRC-1 with the story row at the step-05 status sync. |
| 1 | BH-18 the digest record omits workflows and gitlink, and the grep cannot catch future stale copies | low | reject | The fix edits this build's spec. Grep-based document checking was the human's 2026-11-16 decision. |

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_release_contract.py`: passes.
- `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 360a2b9c4e96809365a7de785be9a68152d5ac28`: passes.
- `pwsh eng/validate-docs.ps1` (run twice): passes.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` and `python3 eng/validate-story-artifacts.py --story <this spec>`: pass.
- `grep -rn -E '4eb33928|2fba3497|a32cb422'` over the live sources: every hit is labelled dated or historical.

### Results — 2026-11-16

- `python3 -m unittest tests/eng/test_release_contract.py`: 33 tests pass (28 at baseline plus 5 new).
- `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 360a2b9c4e96809365a7de785be9a68152d5ac28` at `eddca353d566554af527064925798ec827e07c36`: exit 0, `{"builds_catalog_sha": "65fd22f37b9fad64b595841923cdbe050563ab99", "builds_execution_sha": "360a2b9c4e96809365a7de785be9a68152d5ac28", "ok": true}`. No workflow changed, so the delivery commit gives the same result.
- Negative probes of the same command: `--approved 4eb33928a1d8c7775f97221cf9edc171db0cb5f8` exits 1. Its message names both `.github/workflows/release.yml builds-execution-sha` and `.github/workflows/release-publish.yml uses: Hexalith/Hexalith.Builds/Github/prepare-domain-release@`, each with expected and actual SHAs. `--approved v6.0.0` exits 1 with `--approved (Builds execution SHA): expected an exact lowercase 40-hex commit; actual "v6.0.0"`.
- Neighbouring suites: `python3 -m unittest tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py` runs 32 tests, all passing.
- `pwsh eng/validate-docs.ps1`, run twice: both runs print "Docs validation passed."
- `python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests pass (2 skipped). `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md` and the repository-integrity run `python3 eng/validate-story-artifacts.py` both print "Story artifact validation passed." `git diff --check` is clean.
- Stale-SHA grep over the nine sources in the digest record plus `sprint-status.yaml`. Every remaining hit is dated or historical: PRD G-3 (2026-09-22 inspection point), FR-24 and §7 (marked historical), D-11 ("2026-09-22 values … are historical") and D-12 (retained history). Also the addendum identity-register history line, the dated 2026-09-09 change-map row, the G2 request ("named until 2026-11-16 … historical"), `architecture.md` EventStore history (immutable; "completed history" and "2026-09-22 reconciliation point"), and the `sprint-status.yaml` E11R-AI-1 evidence ("At reconciliation HEAD b61e51e1…"). The FC-DEP-1, spine, register, GOV-1 story and `docs/release-trusted-publishing.md` have no hits.
- Matrix coverage. Pin drift: `test_builds_identity_pin_drift_names_file_field_expected_and_actual`, `test_builds_identity_duplicate_or_missing_pin_reports_every_observed_value` and `test_builds_cli_rejects_drift_and_labels_naming_source_and_field`. Label as identity: `test_builds_identity_rejects_labels_and_names_the_field` and the CLI test's tag, branch and short-SHA cases. Gitlink bump: `test_builds_identity_accepts_diverged_gitlink_and_rejects_pin_mismatch` (unchanged), and each document cites the authority plus a dated snapshot. Stale copy: the grep above.

### Results after review pass 1 fixes — 2026-11-16 (supersede the counts above)

Re-run by the orchestrating session on the patched working tree, at baseline `eddca353d566554af527064925798ec827e07c36`:
- `python3 -m unittest tests/eng/test_release_contract.py`: 37 tests pass.
- Neighbouring release suites (`tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`): 32 tests pass.
- Live `release_contract.py builds` with `--approved 360a2b9c4e96809365a7de785be9a68152d5ac28`: exit 0, with the same JSON as above. `--commit HEAD` now exits 1: `--commit (FrontComposer candidate commit): expected an exact lowercase 40-hex commit; actual "HEAD"`.
- `python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests pass (2 skipped). `--story` on this spec and the repository-integrity run both pass.
- `pwsh eng/validate-docs.ps1`, run twice: exit 0 both times, "Docs validation passed."
- `git diff --check` is clean.
- Stale-SHA grep over the nine sources: every hit is dated or historical. That covers PRD G-3, FR-24, §7, D-11 and D-12; the addendum history and ancestor line; the dated 2026-09-09 change-map row; the G2 request's "named until 2026-11-16"; and `architecture.md` EventStore history. "Production releases … halted" appears only in requirement form ("must stay halted").

## Implementation Notes

- **Check.** `validate_builds_identity` keeps its exact-SHA decision patterns unchanged. A second, broader pattern reports what each file actually holds, such as a tag, branch, short SHA or expression. Every mismatch now reads `<file> <field>: expected exactly one "<approved>"; actual <values or none>`. Both members are reported when both drift. The `--approved` label rejection names that field. The gitlink, workflow and publisher resolution errors in `main` name the path and commit. They keep their original prefixes, so existing assertions on those prefixes still match.
- **Fallback binding.** `eng/release_evidence.py` lists `eng/release_contract.py` in `RELEASE_DEFINITION_FILES` and `FALLBACK_INVALIDATION_FILES`. Its new bytes therefore invalidate any `fallback-approved` state bound to the old bytes. The last recorded fallback window expired at 2026-11-14T18:56:16Z. The file is not part of any `evaluator_authorizations` workflow blob closure, so no evaluator row changes.
- **Observations used for the halt wording.** An authenticated `gh api` read of `HEXALITH_RELEASE_PUBLISH_ENABLED` returned `"value":"true"` and `"updated_at":"2026-10-05T06:13:40Z"`, with server `Date: Mon, 16 Nov 2026 15:55:29 GMT`. `gh release list` shows v4.6.0 (2026-10-05T07:55:31Z) through v6.0.0 (2026-11-14T10:55:25Z): ten releases. The NuGet.org flat-container index for `hexalith.frontcomposer.contracts` lists each of those versions. Many failed and cancelled `release.yml` dispatches since 2026-10-05 are also publication-capable attempts. NC-36 leaves their ledger rows to Story 17.14. v4.5.0 (2026-09-19) was also published after the 2026-09-09 halt; it is outside the post-2026-10-05 scope of these rows.
- **Identity facts.** `git ls-tree HEAD references/Hexalith.Builds` returns `65fd22f37b9fad64b595841923cdbe050563ab99`. `git log -S` attributes the Release pin to `2670339e8b748a8264d433f49f8544e5e41cded2` and the CI pin to `8e96d8c3ff85d0e9b171cfe5d87fde0a6a9980de`. `git merge-base --is-ancestor` in Hexalith.Builds confirms `a8a50859…` → `4eb33928…` → `2fba3497…` → `360a2b9c…` → `65fd22f3…`. No check verifies the AD-16 policy-history predicate yet (NC-30), so no source claims lineage membership for the current pins.
- **Scope kept.** No workflow, `eng/dependency-graph-policy.json`, gitlink, repository variable or release posture changed. No validator, catalog, mirror, schema, lane or evidence packet was added. The following are untouched: validation reports, `reviews/`, approved proposals, historical tuples (`architecture.md` EventStore history, PRD D-12), D-16 and every existing register row and bundle. The 2026-09-09 identity table is superseded in place by the dated 2026-11-16 register. Its values survive as labelled historical provenance. G-2, G-8 and OI-7 to OI-14 stay open.
- **Tracking.** The GOV-SRC-1 approved-change item is `review`, with this record as evidence. The workflow's step transitions own the `17-1` row. GOV-ACCEPT-2 stays `open`.
- **Review fixes, 2026-11-16 (review iteration 1).** `validate_builds_identity` now decides on every observed line-anchored value. A member passes only when its observed list is exactly `[approved]`, and a pin value must be 40 lowercase hex ended by the line end or by whitespace and a comment. A second expression input, a live `@main` beside a commented pin, a `-suffix`, or a value on the next line now fail. The delegation diagnostic shows the raw job-level `uses:` lines, including remote, commented, quoted, mis-indented or CR-terminated lines. `builds` rejects a non-40-hex `--commit` before any git call. A failing `git ls-tree` is reported with git's stderr. Four new tests bring the suite to 37, all passing; the live command still exits 0 for `360a2b9c4e96809365a7de785be9a68152d5ac28`. In the sources, the register row 2a states which check covers which members: `eng/release_contract.py` covers the input and the action pin, and `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate` covers the full set. It names the policy `release` rows (`c97156de…`, `2670339e…`, `31c6ecd0…`, actions at `360a2b9c…`) as the authorization of a non-conformant caller. The "linear history" claim is now an ancestor chain that includes `7d2019ad…`, and row 2b carries the NC-30 caveat. NC-34 names the policy rows. NC-36 says "after the NC-34 caller switch", names the one NuGet index read, adds v4.5.0 and widens Story 17.14 to every attempt G-1 requires; this supersedes the earlier note above that v4.5.0 was out of scope. The companion bullet names each row's closure path. Halt sentences in the G2 request, the GOV-1 story, `architecture.md` and the docs page now read as requirements; each also states that the halt is currently not in effect. The docs page states the `true`/other-value mechanics and restores the current-`main` dispatch rule. PRD G-1 counts fourteen tags without rows. The spine names the five identities, including EXT-BUILDS-1. The digest record below was recomputed.
- **Line endings.** Every CRLF file remains CRLF with no bare LF. The G2 request and register working copies were LF before this change and remain LF. The addendum's four pre-existing LF lines are untouched. Git normalizes every file to LF blobs, which is what the digests below hash.

## Source Digest Record

These are SHA-256 digests of each reconciled source's Git blob bytes (LF-normalized under `.gitattributes` `* text=auto eol=crlf`), computed 2026-11-16 from the delivery working tree. Each blob ID equals `git hash-object <path>`. At the delivery commit, `git show <commit>:<path> | sha256sum` must reproduce every value. Any difference means this record is stale for GOV-ACCEPT-2. A later edit to any listed source needs a fresh record.

| Source | SHA-256 | Git blob |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/prd.md` | `15bcbc1952cf5061a025a0cfe2c295842b542f296bbbcdf419bb1b35689416c3` | `6110c7fd2b1d884e75c4283f14ec65408818fd55` |
| `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` | `0a91ca07df6e4792e77feb7bdfb9686fc7dce6be8439bf2d2f78e2c3c194266b` | `3f89458e3d5366985762427fcdfd431ee81347e1` |
| `_bmad-output/planning-artifacts/architecture.md` | `74e815d4c68121de724927b04bc4b7e15618d4e82f7622f7337a1598eec38726` | `62a734707ccda3d299625de4bf6a0d3765dd2e06` |
| `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` | `59f70188627900023caf73bde5015d72cc39f99ca32765cc486916e61a1db84a` | `9dffb6078ccfbf320c75980c470f1df87ddd0580` |
| `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` | `c63bc39796a86bfd2cef959403902e68da024f733fa7a4a469a16d869f60f059` | `4efc18221fc466929db39d9d3e1584daa701b2aa` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` | `5235cb3d55fe5670469a4e871fb2cc66b10d4ca564d899a7444471b57452884b` | `e5c44ba0b895989f884db2ad2165e3613602e2ad` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` | `a22acd83aff1a1af9bf0c2f9f8f27f9b81b23862c081321ddc9ab356c46b7168` | `83b955485d1620aa8df7b74257b8a250e562da7d` |
| `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` | `a9495b1ab5faae2b8f4aac1e1e464977581adda789b4fd500d5e71665bdf20d1` | `58e4dff9bcbfafda543a10fe364e2eaa377f934e` |
| `docs/release-trusted-publishing.md` | `35e363d3a7023b94ff020dffd5c15e0cb9659ae84234a8fa7a1422a34652a800` | `205419dc34cdfc6f81095cd188787d431167b08a` |

## File List

- `eng/release_contract.py` — names the file, field, expected and actual values on every Builds identity mismatch and resolution failure.
- `tests/eng/test_release_contract.py` — adds the pin-drift, duplicate/missing-pin, label and delegation fixtures plus a CLI drift and label fixture.
- `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` — replaces the 2026-09-09 identity table with the dated five-identity register and corrects the current-caller and halt bullets.
- `_bmad-output/planning-artifacts/prd.md` — separates the five identities in §0, G-2, FR-24, §7, D-6 and D-11, applies the halt and caller wording, and dates OI-7.
- `_bmad-output/planning-artifacts/architecture.md` — names each identity's authority, states the caller-owned topology and its nonconformance, and records the observed halt deviation.
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` — adds the current identity and caller section, names EXT-BUILDS-1 and records the observed halt deviation.
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — replaces the stale execution pin with the dated five identities and records the caller and halt deviation.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` — records the 2026-11-16 flag observation and the AD-13 caller deviation.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` — appends NC-34 to NC-36 and one companion-state bullet.
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` — adds the identity, caller and halt-deviation state and updates two owner-decision rows.
- `docs/release-trusted-publishing.md` — uses the deny-only wording, states that the path is legacy and not conformant, and removes "set it to `true`" as an enabling step.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — sets the GOV-SRC-1 action to `review` with this record as evidence; the workflow moved `epic-17` and the `17-1` row to `in-progress` and refreshed `last_updated`.
- `_bmad-output/implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md` — this spec: verification results, implementation notes, digest record and file list.
- `_bmad-output/implementation-artifacts/epic-17-context.md` — Epic 17 context compiled for this story by the workflow; read as context and not edited by the implementation.
- `_bmad-output/implementation-artifacts/deferred-work.md` — the Story 17.1 review appended two deferrals (floating `@main` Builds references; no interim containment instruction or deadline while the flag reads `true`).
