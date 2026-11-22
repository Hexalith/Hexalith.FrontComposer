---
title: 'Story 17.1: Reconcile Publication-Governance Sources'
type: 'chore'
created: '2026-11-16'
status: 'done'
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
- [x] [Review][Patch] FC-DEP-1 Consequences still states "Production publication remains halted" as fact, and the G2 request says "During the current halt" — AA-1 (medium) [_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md:239]
- [x] [Review][Patch] v4.5.0 (2026-09-19) is missing from every "Observed nonconformance" paragraph, and PRD FR-24 and G-8 place it outside the halt. This contradicts NC-36 and PRD G-1 — ECH-6, BH-6, AA-2 (medium) [_bmad-output/planning-artifacts/prd.md:487]
- [x] [Review][Patch] Resolution diagnostics omit the actual value. The non-160000 branch drops the `ls-tree` output, and both `git show` failures drop git's stderr — ECH-7, BH-3 (low) [eng/release_contract.py:506]
- [x] [Review][Patch] The identity register omits the post-release execution pin: the `release-evidence.yml` `.hexalith/builds-execution` checkout `ref:`, authorized by the `post_release` rows. Row 2b also names no authorization authority (the `ci` rows) — BH-4 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [x] [Review][Patch] Row 2a, `architecture.md` and FC-DEP-1 say `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate` enforces the full equality set. It compares only `\b`-terminated exact 40-hex matches, so a non-SHA or `-suffix` member goes undetected there — BH-2 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [x] [Review][Patch] The policy-row values copied into row 2a's Authority cell carry no dated-snapshot label — AA-4 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:41]
- [x] [Review][Patch] Added text uses 12 abbreviated SHAs, and `3f0e3595…` never appears in full. This breaks the frozen "full lowercase 40-hex SHAs" rule — AA-3 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:47]
- [x] [Review][Patch] Stale caller wording sits beside the rewritten text: the addendum Activation bullet's "Flipping the current mode flag first", the §1 lead-in "after the 2026-09-09 GOV-1 validation", and PRD D-6 "Amended 2026-09-09 to current execution truth". Each credits 2026-10-04 and 2026-11-16 state to 2026-09-09 — AA-6 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:18]
- [x] [Review][Patch] The PRD G-2 pass condition hard-codes the legacy `prepare-domain-release@` field as a pin authority. It should defer to the identity register instead — AA-5 (low) [_bmad-output/planning-artifacts/prd.md:34]
- [x] [Review][Patch] The addendum frontmatter still reads `updated: 2026-09-09`, and the §8 change map has no 2026-11-16 Story 17.1 row — BH-11 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:4]
- [x] [Review][Patch] The REL-AI-1 `release_control` text still says the `HEXALITH_RELEASE_PUBLISH_ENABLED` path "is retired", which contradicts NC-35. Append a dated progress entry rather than rewriting the text — BH-10 (low) [_bmad-output/implementation-artifacts/sprint-status.yaml:427]
- [x] [Review][Patch] The rewritten docs prerequisites name "green exact-source push CI" but omit the successful push Quality run that `release.yml` authenticates — BH-8b (low) [docs/release-trusted-publishing.md:61]
- [x] [Review][Patch] `_PUBLISHER_DELEGATION_LINE` and the path constants are used only in diagnostics. The gate regex and the `git ls-tree`/`git show` arguments in `main()` repeat the literals, so a topology change can update one without the other — BH-15 (low) [eng/release_contract.py:358]
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

### Review Findings (pass 4)

Review pass 4, 2026-11-16. Layers: Blind Hunter (BH), Edge Case Hunter (ECH), Verification Gap (VG) and Acceptance Auditor (AA). They reviewed the fix delta `ba2edb3c1ddf3e6a0755ff02bf2275cf7c83cb49..f8c15a483257d74686bd1feabc8dc627d7d6648b`, which holds the pass-2 and pass-3 fixes (`f8c15a48` combines them). The delta excludes this spec, `deferred-work.md` and `epic-17-context.md`. At `f8c15a48` all nine Source Digest Record rows reproduce, by SHA-256 and by blob ID. `tests/eng/test_release_contract.py` runs 37 tests, all passing, and the live `builds` command exits 0.

- [x] [Review][Patch] The post-release authority citations omit open NC-7 and NC-33. `architecture.md`, FC-DEP-1, the G2 request and the GOV-1 story say the `post_release` rows authorize the post-release closure; addendum row 2c carries NC-33 but not NC-7. The `post_release` row matching the current `release-evidence.yml` blob (sha256 `5cc696fc168000e7d3995a2db3c415761c69e53be21497dffbb5989197a3f524`) names an unreachable Builds `domain-release.yml` reusable, while AD-12 requires `reusable: null` — BH-1, BH-2, AA-1 (medium) [_bmad-output/planning-artifacts/architecture.md:302]
- [x] [Review][Patch] The PRD FR-24 identity paragraph names only the Release and CI pins, and `architecture.md` says the gitlink "need not equal either CI or Release execution pin". G-2 and the identity register name three pins — ECH-7, BH-4, AA-2 (low) [_bmad-output/planning-artifacts/prd.md:489]
- [x] [Review][Patch] The PRD G-2 evidence column omits the identity register and every pin-holding workflow except `release.yml`. The pass condition now cites pins held in `release-publish.yml`, `ci.yml` and `release-evidence.yml` — BH-5 (low) [_bmad-output/planning-artifacts/prd.md:34]
- [x] [Review][Patch] The FC-DEP-1 Consequences correction ("The halt is currently not in effect") is undated text in approved decision content, outside the dated 2026-11-16 section. Date it as a 2026-11-16 observation — BH-8 (low) [_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md:244]
- [x] [Review][Patch] The REL-AI-1 progress entry marks only the variable-retirement clause historical. `release_control` still names successful push CI as the only run prerequisite and omits the successful push Quality run that `release.yml` authenticates — BH-10 (low) [_bmad-output/implementation-artifacts/sprint-status.yaml:483]
- [x] [Review][Patch] The pass-2 BH-15 de-duplication is incomplete. `_ACTION_PIN_RE` repeats the `Hexalith/Hexalith.Builds/Github/prepare-domain-release@` literal from `_PUBLISHER_ACTION_FIELD`, and `_INPUT_PIN_RE` repeats the `builds-execution-sha` field named in `members`, so a renamed field can drift from the pattern that reads it — BH-12, AA-4 (low) [eng/release_contract.py:362]
- [x] [Review][Patch] The rewritten gitlink CLI test no longer asserts the `cannot resolve the candidate Builds gitlink:` prefix that the replaced test asserted. A probe that mutated the prefix left the test green — ECH-5 (low) [tests/eng/test_release_contract.py:559]
- [x] [Review][Patch] Identity register row 2c omits the introducing commit that rows 2a, 2b and 3 carry. `release-evidence.yml` took `360a2b9c4e96809365a7de785be9a68152d5ac28` in FrontComposer commit `15577f42be42539b5e917cb7ae6716c2c11ac534` (2026-10-18) — ECH-6 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:43]
- [x] [Review][Patch] The history line dates `3f0e3595be693fce56a37648c0bd0f89390f5fd3` to 2026-08-11, which is the cited spec's `created:` date. The Builds commit and its FrontComposer pin `e787690fa6b48973ee83fd9c9b5127c424179dce` are both dated 2026-08-14 — AA-3 (low) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:48]
- [x] [Review][Patch] PRD D-6 says "reconciled 2026-11-16" twice in one cell — BH-15 (low) [_bmad-output/planning-artifacts/prd.md:696]

Rejected:
- BH-2, second half (false): `architecture.md` and FC-DEP-1 already say `eng/release_contract.py builds` requires the input and the action pin to equal `--approved`. Leaving out the addendum's follow-on sentence creates no disagreement.
- BH-3 (low; carried rationale of pass-2 ECH-1/ECH-2/BH-1): a third-member decoy that `CiGovernanceTests` misses still needs a workflow edit, which changes a blob pinned by `evaluator_authorizations`. Row 2a already states that the C# check does not enforce every observed value.
- BH-4, D-11 part (false): D-11 cites the execution pins through the identity register and makes no complete-list claim.
- BH-6 (false): every paragraph lists v4.5.0 (2026-09-19) as published after the 2026-09-09 adoption, so none dates the deviation to October. NC-35 records the 2026-09-09 `true` read.
- BH-7 (false): no live paragraph attributes v4.5.0 to the NC-34 path or to a flag value. Each cites NC-36, which carries the "then-legacy Builds reusable" and "not observed" qualifiers.
- BH-9 (false): D-16 is the dated 2026-09-09 decision record of what the posture requires. G-8, which D-16 names, records the observed deviation. This spec's Code Map classifies D-16 as correct.
- BH-10, `due` part (low): the missed REL-AI-1 due date is incident and ledger scope, owned by Stories 17.2 and 17.14.
- BH-11 (false): row 2c makes no enforcement claim, and "held by" is accurate: the literal lives in `release-evidence.yml`.
- BH-13 (false): the `ls-tree` failure message already shows git's stderr. The missing `actual` keyword names no harm.
- ECH-1 and the empty-stderr part of BH-13 (low): `git show` writes a fatal message on every failure, so empty stderr needs a killed process, and the fix adds a branch.
- BH-14 (low/false). (a) The non-160000 branch rejects every mode other than 160000, and the regular-file case already covers it. (b) False: `git hash-object <path>` applies the same attributes and filters as `git add`. (c) False: `assertNotEqual(0, git_error.returncode)` fails loudly if the label selects a file that exists.
- BH-15, line wrap (false): Markdown rendering is unchanged, no lint gate covers `_bmad-output`, and `validate-docs.ps1` passes on the docs page.
- ECH-2 (low; carried class of pass-2 ECH-3 and pass-3 BH-3): a tree or symlink at the workflow path fails closed with a delegation message. An object-type guard is beyond a direct correction.
- ECH-3 (carried, pass 1 ECH-8 and pass 2 ECH-4): a non-UTF-8 blob raises a traceback.
- ECH-4 (carried, pass 2 ECH-3 and pass 3 BH-3): a 40-hex tree or tag ID passes `--commit`.
- ECH-5, check-order part (false): which of the gitlink and publisher errors appears first is not specified behavior, and both fail closed.
- VG-1 (low): GOV-SRC-1 `status: "review"` is outside the backlog header's documented list. `UX-E` sets the precedent, no code reads the field, and the fix would change either the shared vocabulary or this spec's recorded tracking state.
- AA-5 (rejected, because the fix edits this spec): the Line endings note says the addendum's four LF lines are untouched. L17 (Privilege boundary) was rewritten and kept LF. Blob digests are unaffected.

### Review Findings (pass 6)

Review pass 6, 2026-11-16. Layers: Blind Hunter (BH), Edge Case Hunter (ECH), Verification Gap (VG) and Acceptance Auditor (AA). They reviewed the fix delta `a3970e1ec6e8b0ce1c82505542d97ad4f386033c..9b93b9d7ac2838990d8c1efe5cc92978e876472e`, which holds the pass-4 fixes and the pass-5 BH-5 fix. The delta excludes this spec, `deferred-work.md` and `epic-17-context.md`. At `9b93b9d7` all nine Source Digest Record rows reproduce, by SHA-256 and by blob ID. `tests/eng/test_release_contract.py` runs 37 tests, all passing, and the live `builds` command exits 0. VG found no gaps.

- [x] [Review][Patch] The post-release authority citations omit open NC-6. Five sources say the post-release closure is "authorized by the policy's `post_release` rows", and the FR-24 bullet cites only NC-7 and NC-33. Under NC-6 the `release-evidence.yml` verify step runs `verify-release` with no policy reload and no `post_release` authorization. No code calls `require_evaluator_authorized(policy, "post_release", …)`, so no check consults those rows yet — BH-1 (medium) [_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md:43]
- [x] [Review][Patch] The NC-7 caveat copied into `architecture.md`, FC-DEP-1, the G2 request and the GOV-1 story says "the row matching the current caller blob". It names neither `release-evidence.yml`, the blob nor a date. In `architecture.md` and FC-DEP-1, "caller" has just meant the `release.yml`→`release-publish.yml` caller, whose `release` rows name a reachable local reusable. NC-7 (2026-09-08) says "Five rows … none matches the HEAD caller blob". Today all seven `post_release` rows name `domain-release.yml`, including the one for blob sha256 `5cc696fc168000e7d3995a2db3c415761c69e53be21497dffbb5989197a3f524`, and the undated caveat reads as contradicting NC-7 — ECH-1, ECH-2, AA-1, BH-2, BH-3 (low) [_bmad-output/planning-artifacts/architecture.md:303]
- [x] [Review][Patch] Halt-exit conditions disagree with the spine's Adoption and Release Eligibility. The docs page's new G-8 list omits the owner-accepted split Builds revision (EXT-BUILDS-1) entering AD-16 lineage. FC-DEP-1 Consequences names the incident runbook but not its response target, which the spine, D-16 and now the docs page require — ECH-6, AA-3, ECH-7, BH-7 (low) [docs/release-trusted-publishing.md:17]
- [x] [Review][Patch] The G2 request's Deny-Only Publication Stop says "the halt is currently not in effect" with no date and no NC-35/NC-36 citation. Every other copy is anchored by a citation, a same-bullet dated observation or "see below" — AA-2, BH-8 (low) [_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md:157]

Rejected:
- BH-4 (false): "unreachable" is fixed by the same sentence's "caller-reachable sources only", and it mirrors NC-7's own wording.
- BH-5 (false): `release-evidence.yml` downloads the served NuGet bytes (L426) and runs `dotnet nuget verify` (L552). "Independently" denotes the separate workflow, and the next sentence denies conformance. NC-9 concerns the CI-handoff copy, not NuGet bytes. The sentence predates this story (baseline L39).
- BH-6 (false): L11–12 introduce "the GOV-1 publication-safety boundary (gate G-8)" before L17 uses it. The diff makes no publication claim, and the page's absence from `docs/docfx.json` predates this story.
- BH-7, literal-`false` re-read part (carried, pass-5 BH-4 defer).
- BH-8, other copies (false): G2 L44 and `architecture.md` L359 cite NC-35/NC-36. The addendum L18 bullet carries the 2026-11-16 observation, and docs L21 points to "As of 2026-11-16". FC-DEP-1's NC-35/NC-36 name the read.
- BH-9 (false): the history paragraph makes no completeness claim. Row 2c's predecessor `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` is listed as the former Release pin, and the ancestry sentence covers "each listed commit".
- BH-10 (carried, pass-2 BH-10 and pass-4 BH-10): `release_control` is superseded by an appended dated entry rather than rewritten.
- BH-11 (false): `test_builds_identity_pin_drift_names_file_field_expected_and_actual` asserts `.github/workflows/release.yml builds-execution-sha` (L336), which is produced from `_BUILDS_INPUT_FIELD`. `_INPUT_PIN_RE`, built from the same constant, must read the fixture lines, so a broken constant fails existing tests.
- BH-12 (carried, pass-4 BH-14a and ECH-2): mode `160000` is the gitlink check, and tree or symlink entries fail closed.
- BH-13 (false): "Dated snapshot 2026-11-16: Release and post-release …, CI …" names the values it dates, so it cannot be read as dating NC-33. A mechanical alignment check would add a validator, which the Never list forbids.
- ECH-3 (false): the 2026-08-04 entry is a dated record of that date's path in an append-only progress log, and the 2026-11-16 entry supersedes it (pass-1 ECH-18 reasoning).
- ECH-4 (carried, pass-4 VG-1): `review` in the GOV-SRC-1 status.
- ECH-5 (rejected, because the fix edits this spec; carried pass-1 BH-17/ECH-12 and pass-2 BH-17/AA-8): spec `done` beside story `review` is the workflow's designed split.

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

### Review pass 3 — 2026-11-16

All three layers reviewed the working-tree diff from `eddca353d566554af527064925798ec827e07c36`, excluding this spec and generated epic context. Findings are numbered in each layer's return order. Every finding has a verdict below before grouping; no intent or spec loopback is required.

| Pass | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 3 | BH-1 quoted/flow declarations can hide a drifting pin | low | reject | The quoted-key/action bypass is real and matches pass-2 ECH-1/ECH-2/BH-1's recorded rejection. The workflow blob must independently match an authorized policy row; supporting all YAML forms requires parser/guard complexity for an unlikely defense-in-depth case. Preserve the prior decision. |
| 3 | BH-2 a block-scalar action line can satisfy the regex beside a quoted mutable action | low | reject | The raw-text matcher sees the scalar line, but the demonstrated bypass also depends on the already-rejected quoted-action case. Correct YAML context handling requires a parser rather than a direct correction; independently authorized workflow blobs remain required. |
| 3 | BH-3 a root tree SHA satisfies the CLI's shape check | low | reject | This matches the explicit pass-2 ECH-3 rejection. The real caller supplies github.sha; adding an object-type guard for a non-caller tree/tag input is beyond a direct correction for an unlikely developer-only case. |
| 3 | BH-4 quoted uses keys are missing from the delegation diagnostic | low | reject | A quoted key is invisible to the reporting regex, while the gate still rejects the missing exact delegation. Extending syntax recognition adds handling for an unlikely unsupported form and changes no acceptance result. |
| 3 | BH-5 the privilege paragraph calls approval authorization | low | patch | The live addendum says approval is authorization beside an explicit statement that environment approval never authorizes these publications. Clarify job progression versus publication authority. |
| 3 | BH-6 the catalog gitlink selects more than catalog content | low | patch | Root Directory.Packages.props imports the selected Builds Props/Directory.Packages.props, which imports Logging.Build.targets and changes analyzer behavior. Name imported build content as well as the catalog without conflating workflow pins. |
| 3 | BH-7 the post-release row omits NC-33's execution caveat | low | patch | release-evidence.yml executes ambient dependency_handoff.py and release_disposition.py before the pinned checkout. Add the existing NC-33 caveat so the authority citation cannot imply exclusive authorized-closure execution. |
| 3 | BH-8 the containment deferral falsely says no source prohibits dispatch | low | patch | Spine Adoption and Release Eligibility already makes approval/dispatch ineligible before a literal-false read and server timestamp; the G2 stop section agrees. Append a dated correction to the deferred-work history and retain only the unresolved deadline/escalation/operator-guidance scope. |
| 3 | BH-9 NC-35's companion closure narrows the observer and omits the timestamp | low | patch | The NC-35 row permits a Release Owner or repository administrator; the spine additionally requires the authenticated read's server timestamp. Append a clarification, preserving all existing rows and companion history. |
| 3 | BH-10 the missing-source fixture assumes Git's English error text | low | patch | The assertion hard-codes an English fatal message while the implementation preserves Git stderr under its ambient locale. Compare with the actual Git diagnostic and keep the separate path/commit checks. |
| 3 | ECH-1 quoted input/action declarations bypass pin collection | low | reject | Same location and claim as BH-1 and the explicit pass-2 rejected quoted/case-variant finding. Preserve that decision; generalized YAML parsing is not a direct correction for this unlikely policy-guarded case. |
| 3 | VG-1 no automated committed-caller acceptance test | medium | defer (carried) | Carried from the pass-2 human-approved VG-1/BH-14 deferral. The gate fails closed before production. Revisit with the Epic 17 split caller switch; do not add a duplicate deferral or a workflow-YAML assertion test. |

### Review pass 4 — 2026-11-16

Four layers reviewed the fix delta `ba2edb3c1ddf3e6a0755ff02bf2275cf7c83cb49..f8c15a483257d74686bd1feabc8dc627d7d6648b`, excluding this spec, `deferred-work.md` and the epic context. Full evidence for each row is under Review Findings (pass 4).

| Pass | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 4 | BH-1, BH-2 (NC-33 part), AA-1: post-release authority citations omit NC-7 and NC-33 | medium | patch | The `post_release` row for blob `5cc696fc…` names a non-null `domain-release.yml` reusable. `grep -c NC-33` returns 0 in four sources. |
| 4 | BH-2, follow-on sentence | false | reject | Both sources already state the exact two-member comparison. |
| 4 | BH-3: the three C#-only members have no owner | low | reject (carried) | Pass-2 ECH-1/ECH-2/BH-1 rationale: the workflow blobs are pinned by policy. |
| 4 | ECH-7, BH-4, AA-2: post-release pin missing from FR-24 and `architecture.md` L281 | low | patch | G-2 and the register name three pins. The D-11 part is false. |
| 4 | BH-5: G-2 evidence column incomplete | low | patch | Lists only `release.yml` among the workflows. |
| 4 | BH-6: flag already `true` at adoption | false | reject | Every paragraph includes v4.5.0 (2026-09-19). |
| 4 | BH-7: v4.5.0 publication path | false | reject | No paragraph names its path; NC-36 qualifies it. |
| 4 | BH-8: FC-DEP-1 Consequences correction undated | low | patch | Approved decision text, outside the dated section. |
| 4 | BH-9: D-16 states the halt as fact | false | reject | Dated decision record; G-8 carries the deviation. |
| 4 | BH-10: REL-AI-1 entry partial | low | patch | `release_control` omits the Quality prerequisite. The `due` part is rejected. |
| 4 | BH-11: row 2c names no check | false | reject | Row 2c makes no enforcement claim. |
| 4 | BH-12, AA-4: field literals duplicated in the pin regexes | low | patch | `_ACTION_PIN_RE` and `_INPUT_PIN_RE` hard-code the field names. |
| 4 | BH-13: inconsistent resolution diagnostics | false | reject | The stderr is already shown. The empty-stderr part goes with ECH-1. |
| 4 | BH-14: test gaps (tree entry, hash-object oracle, label-derived path) | low/false | reject | Generic mode check, same filters, loud failure. |
| 4 | BH-15: wrap and D-6 duplicate | false/low | reject/patch | The wrap is not rendered or linted. The D-6 duplicate is real. |
| 4 | ECH-1: empty `git show` stderr renders `""` | low | reject | Needs a killed process; the fix adds a branch. |
| 4 | ECH-2: tree or symlink at a workflow path | low | reject (carried class) | Fails closed; would need an object-type guard. |
| 4 | ECH-3: non-UTF-8 traceback | low | reject (carried) | Pass 1 ECH-8, pass 2 ECH-4. |
| 4 | ECH-4: tree or tag `--commit` | low | reject (carried) | Pass 2 ECH-3, pass 3 BH-3. |
| 4 | ECH-5: prefix assertion lost | low | patch | Mutation probe survived. The order part is false. |
| 4 | ECH-6: row 2c lacks its introducing commit | low | patch | `15577f42…` (2026-10-18). |
| 4 | VG-1: `review` not in the backlog status vocabulary | low | reject | `UX-E` precedent; no reader. |
| 4 | AA-3: `3f0e3595…` dated by the spec, not the selection | low | patch | Builds commit and FrontComposer pin `e787690f…` are dated 2026-08-14. |
| 4 | AA-5: LF-line claim in Implementation Notes | low | reject | The fix edits this spec. |

### Review pass 5 — 2026-11-16

Three independent context-free layers reviewed the cumulative implementation diff from `eddca353d566554af527064925798ec827e07c36`; the build spec was excluded from that diff and supplied as claims only to the Edge Case Hunter. The Blind Hunter independently found the spec through source references and was instructed to stop consulting it. No acceptance-auditor layer is configured in this workflow. Each finding is recorded separately before grouping.

| Pass | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 5 | BH-1: cached epic context says production stays halted | false | reject (carried) | Carried from pass-1 BH-16. Planning sources are newer than the compiled context, so step-01 rejects this cache and recompiles it before a subsequent epic story can use it. The alleged propagation into later story planning does not occur. |
| 5 | BH-2: cached context describes target split jobs and runbook in present tense | false | reject | Its Goal explicitly describes the epic replacing the current legacy caller, and Technical Decisions describes that target. The story list and dependency section retain the implementation/runbook work. It makes no claim of completed implementation; additionally the stale cache must be recompiled from current planning sources. |
| 5 | BH-3: cached context abbreviates the anchor and summarizes one execution identity | false | reject | The cached Technical Decisions section summarizes the future Release topology, not the complete live identity inventory or an accepted comparison value. Current source authority is the five-identity register with full SHAs; the outdated generated cache fails step-01 freshness checks. No authoritative identity is replaced by its abbreviation. |
| 5 | BH-4: public operator page omits authenticated literal-false approval/dispatch prerequisite | medium | defer (carried) | Carried from pass-1 BH-11 and the pass-3 BH-8 correction: the canonical spine and G2 request already prohibit approval/dispatch; explicit public operator guidance is retained in the existing containment deferral for Story 17.2/GOV-ACCEPT-1. Do not add another deferral or reopen the previous disposition. |
| 5 | BH-5: public halt sentence omits incident readiness, owner decisions and conformance closure | low | patch | The page names only acceptance and implementation of the split boundary as the halt endpoint, while the spine's Adoption and Release Eligibility also requires the incident runbook/response target and the implementation gate. Directly name G-8 closure and those existing requirements. |
| 5 | BH-6: NC-35 has no deadline, escalation or accountable contact | medium | defer (carried) | Carried from pass-1 BH-11 and pass-3 BH-8. The retained deferral expressly assigns containment deadline/escalation decisions to Story 17.2 and GOV-ACCEPT-1. No new deferral is added. |
| 5 | BH-7: register omits mutable Builds references feeding release prerequisites | medium | defer (carried) | Carried from pass-1 BH-7: the existing ledger records all eleven floating references and their release-prerequisite impact. Workflows and pin policy are explicitly outside the approved intent. Preserve the recorded disposition. |
| 5 | BH-8: flow mappings plus block-scalar decoys bypass raw pin patterns | low | reject | The block-scalar claim carries pass-3 BH-2's rejection; flow mappings are the same unsupported-YAML limitation. Authorized workflow blobs are checked independently. General YAML parsing or new unsupported-syntax guards add complexity for an unlikely policy-guarded case; no change to that prior decision. |
| 5 | BH-9: another remote workflow delegate can coexist with the expected local line | low | reject | The baseline already accepted other delegates: it counted the same exact local line. The new diagnostic accurately requires exactly one occurrence of that local line, not one job in the whole workflow. Enforcing another topology constraint would add a guard for independently policy-authorized workflow edits, an unlikely defense-in-depth case. |
| 5 | BH-10: root tree or annotated-tag object passes the commit-shape gate | low | reject (carried) | Carried from pass-2 ECH-3 and pass-3 BH-3. The actual caller supplies github.sha; adding object-type guards for unsupported local inputs was explicitly rejected. |
| 5 | BH-11: invalid UTF-8 workflow blobs raise a traceback | low | reject (carried) | Carried from pass-1 ECH-8/pass-2 ECH-4/pass-4 ECH-3. Decoding behavior predates this story; a new decode guard for an unlikely policy-guarded blob remains rejected. |
| 5 | ECH-1: quoted floating action beside a pin or block-scalar text bypasses the patterns | low | reject (carried) | Carried from pass-2 ECH-1/ECH-2/BH-1 and pass-3 BH-1/BH-2/ECH-1. The same quoted/unsupported-YAML limitation and independent authorized-blob guard remain. |
| 5 | VG-1: no committed-caller acceptance test for the hardened gate | medium | defer (carried) | Carried from the human-approved pass-2 VG-1/BH-14 deferral. The reviewer reran all 37 contract tests successfully. Revisit this fail-closed, pre-production gap with the Epic 17 caller switch; add no duplicate ledger entry. |

Only BH-5 survives as a new patch. There are no intent/spec loopbacks, new deferrals, or unresolved high findings.

### Review pass 6 — 2026-11-16

Four layers reviewed the fix delta `a3970e1ec6e8b0ce1c82505542d97ad4f386033c..9b93b9d7ac2838990d8c1efe5cc92978e876472e`, excluding this spec, `deferred-work.md` and the epic context. Full evidence for each row is under Review Findings (pass 6).

| Pass | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| 6 | BH-1: post-release citations omit NC-6 | medium | patch | No `require_evaluator_authorized(policy, "post_release", …)` call exists; `verify-release` loads no policy. |
| 6 | ECH-1, BH-2: NC-7 caveat vs register NC-7 row | low | patch | Seven rows today, one matching the current blob; NC-7 (2026-09-08) says five and none. |
| 6 | ECH-2, AA-1: "current caller blob" referent | low | patch | Row 2c names `release-evidence.yml`; the four copies do not. |
| 6 | BH-3: caveat undated, no blob identity | low | patch | Same sentence as ECH-2. |
| 6 | BH-4: "unreachable" ambiguous | false | reject | "caller-reachable sources only" in the same sentence. |
| 6 | BH-5: "independently verifies" uncaveated | false | reject | Bytes and signatures are verified; text predates the story. |
| 6 | BH-6: page unpublished, undefined ids | false | reject | L11–12 define G-8; no publication claim in the diff. |
| 6 | ECH-6, AA-3: docs halt list omits AD-16 lineage entry | low | patch | Spine L81–84 lists it; D-16 too. |
| 6 | ECH-7: FC-DEP-1 halt list omits response target | low | patch | Spine "runbook/response target"; D-16 "acknowledgement/containment target". |
| 6 | BH-7: halt-exit lists differ | low | patch / carried | Grouped with ECH-6/ECH-7; literal-`false` re-read part carried (pass-5 BH-4). |
| 6 | AA-2, BH-8: G2 L157 undated halt state | low | patch | Other copies cite NC-35/NC-36 or a dated observation (BH-8 remainder false). |
| 6 | BH-9: history paragraph incomplete | false | reject | No completeness claim; predecessor already listed. |
| 6 | BH-10: `release_control` present tense | low | reject (carried) | Pass-2 BH-10, pass-4 BH-10. |
| 6 | BH-11: summary prefix untested | false | reject | L336 fragment and regex both use `_BUILDS_INPUT_FIELD`. |
| 6 | BH-12: fixture entry types | low | reject (carried) | Pass-4 BH-14a, ECH-2. |
| 6 | BH-13: caveat placement, no alignment check | false | reject | Snapshot names its values; a check would be a new validator. |
| 6 | ECH-3: 2026-08-04 entry CI-only | false | reject | Dated append-only entry. |
| 6 | ECH-4: `review` status vocabulary | low | reject (carried) | Pass-4 VG-1. |
| 6 | ECH-5: spec/story status split | low | reject (carried) | The fix edits this spec. |
| 6 | VG: none | — | — | No verification gaps. |

Four patches survive (one medium, three low). There are no decisions, no new deferrals and no unresolved high findings.

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

### Results after review pass 2 fixes — 2026-11-16

Verified on the patched working tree over `ba2edb3c1ddf3e6a0755ff02bf2275cf7c83cb49`:
- `run-with-temp python3 -m unittest tests/eng/test_release_contract.py`: 37 tests pass. The existing missing-gitlink fixture now also covers a regular file at the gitlink path and checks the observed mode/blob/path. The missing-workflow and missing-publisher fixtures check Git's exact stderr.
- `run-with-temp python3 -m unittest tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`: 32 tests pass.
- `run-with-temp python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 360a2b9c4e96809365a7de785be9a68152d5ac28`: exit 0, `{"builds_catalog_sha": "65fd22f37b9fad64b595841923cdbe050563ab99", "builds_execution_sha": "360a2b9c4e96809365a7de785be9a68152d5ac28", "ok": true}`.
- `run-with-temp python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests run, 2 skipped; result `OK`. Both `run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md` and `run-with-temp python3 eng/validate-story-artifacts.py` pass. `git diff --check` is clean.
- `pwsh eng/validate-docs.ps1`, twice in a private repository copy beneath the `run-with-temp` workspace: both exit 0 and print "Docs validation passed." The copy used the patched source and the exact selected root-declared submodules, with no nested submodule initialization; `dotnet tool restore` also passed. All disposable build and documentation outputs stayed in that workspace. All nine Source Digest Record SHA-256/blob pairs reproduce.
- Source inspection: every live `Observed nonconformance` paragraph includes v4.5.0 (2026-09-19), which was published after the 2026-09-09 halt adoption. No added live-source text abbreviates a SHA. The stale-SHA search over the nine digest sources plus `sprint-status.yaml` finds only labelled dated or historical values. CRLF and the pre-existing LF exceptions are preserved.

### Final orchestrator verification — 2026-11-16

The six direct corrections from review pass 3 are complete. The previous quoted/unsupported-YAML and object-type rejections are preserved, and the committed-caller test remains deferred by the prior human decision. No new deferral was added.

Verified on the final working tree over `ba2edb3c1ddf3e6a0755ff02bf2275cf7c83cb49`:
- `run-with-temp python3 -m unittest tests/eng/test_release_contract.py`: 37 tests pass.
- `run-with-temp python3 -m unittest tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`: 32 tests pass.
- `run-with-temp python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests run, 2 skipped, result `OK`.
- Both targeted and repository-wide `eng/validate-story-artifacts.py` invocations pass. The live `release_contract.py builds` command accepts the exact HEAD above, with distinct catalog and execution identities. `git diff --check` passes.
- All nine normalized source SHA-256/Git-blob pairs reproduce. All previous register lines remain unchanged and in order. All 11 retired-SHA hits across the nine sources and sprint status are explicitly dated or historical in their surrounding text.
- An additional full `pwsh eng/validate-docs.ps1` run initially exited 1 on `docs/how-to/customization-gradient-cookbook.md` compiled snippet 1: `dotnet build Snippet.csproj --nologo -v:minimal -p:UseSharedCompilation=false` stopped after restore with no compiler diagnostic. In a fresh private copy, the exact snippet passed with `-m:1 /nr:false`, zero warnings and zero errors. Both subsequent full docs runs passed with `MSBUILDDISABLENODEREUSE=1` and `DOTNET_CLI_USE_MSBUILD_SERVER=0`. No source, audit, test or documentation gate was weakened. The failed run and successful retries retain separate useful logs outside the disposable workspace.
- Every disposable source copy and build output used `run-with-temp`, existing selected root-declared submodule commits and no nested initialization. No remote operation or publication occurred.

Workflow completion sets this spec to `done`, the sprint story and GOV-SRC-1 implementation action to `review`. G-2, G-8, OI-7 through OI-14 and GOV-ACCEPT-2 remain open; implementation completion supplies no publication authorization.

### Results after review pass 4 fixes — 2026-11-16

Verified on the patched working tree over `a3970e1ec6e8b0ce1c82505542d97ad4f386033c`:
- `run-with-temp python3 -m unittest tests/eng/test_release_contract.py`: 37 tests pass.
- `run-with-temp python3 -m unittest tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`: 32 tests pass.
- `run-with-temp python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests run, 2 skipped; result `OK`.
- Both `run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md` and `run-with-temp python3 eng/validate-story-artifacts.py` pass. `git diff --check` is clean.
- The live `run-with-temp python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 360a2b9c4e96809365a7de785be9a68152d5ac28` exits 0 with distinct catalog and execution identities.
- An in-memory mutation of the non-gitlink diagnostic prefix makes both missing-gitlink and regular-file subtests fail, confirming the restored assertion covers that prefix.
- All nine normalized source SHA-256/Git-blob pairs reproduce. The frozen intent remains unchanged. Retired-SHA hits across the nine sources and sprint status remain explicitly dated or historical. Existing CRLF files, the G2 request's LF and the addendum's four pre-existing LF lines are preserved.

### Orchestrator verification after review pass 4 fixes — 2026-11-16

Verified on the working tree over `a3970e1ec6e8b0ce1c82505542d97ad4f386033c`:
- Both full `pwsh -NoProfile -File eng/validate-docs.ps1` runs pass (exit 0), including compiled snippets, the Release solution build, DocFX metadata and the documentation site. The first took 356.9 seconds and the second 205.1 seconds. `dotnet tool restore` passed. Runs used `MSBUILDDISABLENODEREUSE=1`, `DOTNET_CLI_USE_MSBUILD_SERVER=0` and `DOTNET_PROCESSOR_COUNT=2`; no validation gate was skipped or changed.
- Each copy, selected root submodule checkout and build output stayed beneath the private `run-with-temp` workspace, which was removed after completion. No nested submodule was initialized. Useful logs and both documentation manifests are retained outside the disposable workspace at `/home/administrator/.local/state/codex/frontcomposer-17-1-20261116T201500Z`.
- Independent inspection reproduces all nine source SHA-256/Git-blob pairs, confirms the frozen intent is unchanged and every pre-existing nonconformance row remains intact, and finds only dated/historical retired-SHA references. `git diff --check` passes. The matrix remains covered by the passing pin-drift, label and divergent-gitlink tests plus the approved stale-copy grep.

### Final verification after review pass 5 patch — 2026-11-16

Review pass 5 BH-5 is fixed: the public halt endpoint now explicitly requires G-8 closure, the accepted and implemented split boundary, approved incident runbook/response target, required owner decisions and passing conformance evidence. The public page's digest/blob row was refreshed. Prior deferrals and rejections remain unchanged; no new deferral was added.

Verified on the final working tree over `a3970e1ec6e8b0ce1c82505542d97ad4f386033c`:
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/eng/test_release_contract.py tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`: 69 tests pass.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests run, 2 existing skips, result `OK`. Targeted and repository-wide story validators both pass. The exact-HEAD live Builds command passes with catalog `65fd22f37b9fad64b595841923cdbe050563ab99` and execution pin `360a2b9c4e96809365a7de785be9a68152d5ac28`.
- Both full `pwsh -NoProfile -File eng/validate-docs.ps1` reruns pass (161.5 and 75.3 seconds), using the same environment pins as above and fresh disposable source/submodule copies beneath `run-with-temp`. Tool restore passes. All temporary copies and outputs were removed after completion. Final logs and manifests are retained outside the workspace at `/home/administrator/.local/state/codex/frontcomposer-17-1-20261116T201500Z/final`.
- All nine source SHA-256/Git-blob pairs reproduce. All eleven retired-SHA hits across the nine live sources and sprint status have explicitly dated or historical context. Frozen intent and historical register rows remain unchanged; `git diff --check` passes.
- The spec is `done`; the sprint story and GOV-SRC-1 implementation action are `review`. G-2, G-8, OI-7 through OI-14 and GOV-ACCEPT-2 remain open. Implementation completion grants no publication authorization.

### Results after review pass 6 fixes — 2026-11-16

All four pass-6 patches are applied. Seven of the nine digest-recorded sources changed (the spine and the nonconformance register did not), and their Source Digest Record rows were refreshed. Prior deferrals and rejections remain unchanged; no new deferral was added.

Verified on the patched working tree over `9b93b9d7ac2838990d8c1efe5cc92978e876472e`:
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/eng/test_release_contract.py tests/eng/test_release_prepublish.py tests/eng/test_release_evidence_v2.py`: 69 tests pass. No code or test changed in this pass.
- The live `run-with-temp python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 360a2b9c4e96809365a7de785be9a68152d5ac28` exits 0 with catalog `65fd22f37b9fad64b595841923cdbe050563ab99` and execution pin `360a2b9c4e96809365a7de785be9a68152d5ac28`.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest eng.tests.test_validate_story_artifacts`: 222 tests run, 2 existing skips, result `OK`. Targeted (`--story` on this spec) and repository-wide `eng/validate-story-artifacts.py` runs both print "Story artifact validation passed."
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` exits 0 ("Docs validation passed."), with `MSBUILDDISABLENODEREUSE=1` and `DOTNET_CLI_USE_MSBUILD_SERVER=0`. `Get-ContentFiles` (`eng/validate-docs.ps1` L59–86) does not select `docs/release-trusted-publishing.md`, so the full DocFX and snippet runs cannot observe this page's change; they were not re-run. The log is retained at `/home/administrator/.local/state/frontcomposer/review-17-1-pass6/validate-docs-quick.log`.
- All nine Source Digest Record rows reproduce from the working tree: each blob ID equals `git hash-object <path>`, and each SHA-256 equals the digest of the LF-normalized bytes. `git diff --check` is clean.
- Added source lines contain no abbreviated SHA. The only retired-SHA hit in them is the rewritten PRD FR-24 line, whose unchanged text already labels `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` and `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab` historical.
- The review sync sets the `17-1` sprint row and the GOV-SRC-1 action to `done`, the action with `closed: "2026-11-16"` and this record as its receipt. G-2, G-8, OI-7 through OI-14 and GOV-ACCEPT-2 remain open; implementation completion grants no publication authorization.

## Implementation Notes

- **Check.** `validate_builds_identity` keeps its exact-SHA decision patterns unchanged. A second, broader pattern reports what each file actually holds, such as a tag, branch, short SHA or expression. Every mismatch now reads `<file> <field>: expected exactly one "<approved>"; actual <values or none>`. Both members are reported when both drift. The `--approved` label rejection names that field. The gitlink, workflow and publisher resolution errors in `main` name the path and commit. They keep their original prefixes, so existing assertions on those prefixes still match.
- **Fallback binding.** `eng/release_evidence.py` lists `eng/release_contract.py` in `RELEASE_DEFINITION_FILES` and `FALLBACK_INVALIDATION_FILES`. Its new bytes therefore invalidate any `fallback-approved` state bound to the old bytes. The last recorded fallback window expired at 2026-11-14T18:56:16Z. The file is not part of any `evaluator_authorizations` workflow blob closure, so no evaluator row changes.
- **Observations used for the halt wording.** An authenticated `gh api` read of `HEXALITH_RELEASE_PUBLISH_ENABLED` returned `"value":"true"` and `"updated_at":"2026-10-05T06:13:40Z"`, with server `Date: Mon, 16 Nov 2026 15:55:29 GMT`. `gh release list` shows v4.6.0 (2026-10-05T07:55:31Z) through v6.0.0 (2026-11-14T10:55:25Z): ten releases. The NuGet.org flat-container index for `hexalith.frontcomposer.contracts` lists each of those versions. Many failed and cancelled `release.yml` dispatches since 2026-10-05 are also publication-capable attempts. NC-36 leaves their ledger rows to Story 17.14. v4.5.0 (2026-09-19) was also published after the 2026-09-09 halt; it is outside the post-2026-10-05 scope of these rows.
- **Identity facts.** `git ls-tree HEAD references/Hexalith.Builds` returns `65fd22f37b9fad64b595841923cdbe050563ab99`. `git log -S` attributes the Release pin to `2670339e8b748a8264d433f49f8544e5e41cded2` and the CI pin to `8e96d8c3ff85d0e9b171cfe5d87fde0a6a9980de`. `git merge-base --is-ancestor` in Hexalith.Builds confirms `a8a50859…` → `4eb33928…` → `2fba3497…` → `360a2b9c…` → `65fd22f3…`. No check verifies the AD-16 policy-history predicate yet (NC-30), so no source claims lineage membership for the current pins.
- **Scope kept.** No workflow, `eng/dependency-graph-policy.json`, gitlink, repository variable or release posture changed. No validator, catalog, mirror, schema, lane or evidence packet was added. The following are untouched: validation reports, `reviews/`, approved proposals, historical tuples (`architecture.md` EventStore history, PRD D-12), D-16 and every existing register row and bundle. The 2026-09-09 identity table is superseded in place by the dated 2026-11-16 register. Its values survive as labelled historical provenance. G-2, G-8 and OI-7 to OI-14 stay open.
- **Tracking.** The GOV-SRC-1 approved-change item is `review`, with this record as evidence. The workflow's step transitions own the `17-1` row. GOV-ACCEPT-2 stays `open`.
- **Review fixes, 2026-11-16 (review iteration 1).** `validate_builds_identity` now decides on every observed line-anchored value. A member passes only when its observed list is exactly `[approved]`, and a pin value must be 40 lowercase hex ended by the line end or by whitespace and a comment. A second expression input, a live `@main` beside a commented pin, a `-suffix`, or a value on the next line now fail. The delegation diagnostic shows the raw job-level `uses:` lines, including remote, commented, quoted, mis-indented or CR-terminated lines. `builds` rejects a non-40-hex `--commit` before any git call. A failing `git ls-tree` is reported with git's stderr. Four new tests bring the suite to 37, all passing; the live command still exits 0 for `360a2b9c4e96809365a7de785be9a68152d5ac28`. In the sources, the register row 2a states which check covers which members: `eng/release_contract.py` covers the input and the action pin, and `CiGovernanceTests.ReleaseWorkflow_DelegatesToCallerOwnedPublisherAfterCiGate` covers the full set. It names the policy `release` rows (`c97156de…`, `2670339e…`, `31c6ecd0…`, actions at `360a2b9c…`) as the authorization of a non-conformant caller. The "linear history" claim is now an ancestor chain that includes `7d2019ad…`, and row 2b carries the NC-30 caveat. NC-34 names the policy rows. NC-36 says "after the NC-34 caller switch", names the one NuGet index read, adds v4.5.0 and widens Story 17.14 to every attempt G-1 requires; this supersedes the earlier note above that v4.5.0 was out of scope. The companion bullet names each row's closure path. Halt sentences in the G2 request, the GOV-1 story, `architecture.md` and the docs page now read as requirements; each also states that the halt is currently not in effect. The docs page states the `true`/other-value mechanics and restores the current-`main` dispatch rule. PRD G-1 counts fourteen tags without rows. The spine names the five identities, including EXT-BUILDS-1. The digest record below was recomputed.
- **Review fixes, 2026-11-16 (review iteration 2).** All thirteen pass-2 patch findings are addressed. FC-DEP-1 Consequences and the G2 stop section now distinguish the halt requirement from its observed absence. All live nonconformance paragraphs, PRD G-8 and FR-24 include v4.5.0 after the 2026-09-09 halt adoption. The identity register adds the post-release checkout pin and the `post_release` authorization authority, names the CI `ci` rows, dates the copied `release` policy-row values, expands introduced abbreviated SHAs (including historical `3f0e3595be693fce56a37648c0bd0f89390f5fd3`), and records the actual limits of the existing C# regex comparison. Architecture and FC-DEP-1 carry the same limit; the pin projections also name the post-release authority. PRD G-2 cites the identity register, and D-6/addendum wording distinguishes the 2026-09-09 adoption from later current-state reconciliation. The addendum update date and change map record Story 17.1. REL-AI-1 receives a dated progress entry that supersedes its historical variable-retirement claim without rewriting that claim. The public publishing page includes the successful exact-source push Quality prerequisite. Resolution failures now show the observed non-gitlink entry or escaped Git stderr, and delegation matching plus Git reads share the path constants. Existing CLI fixtures cover the additional diagnostic facts. The two pass-2 deferrals remain deferred; no workflow, policy, gitlink, repository variable or release posture changes.
- **Review follow-up, 2026-11-16.** The addendum now distinguishes environment job progression from publication authorization, includes imported build props/targets in the gitlink description, and carries the open NC-33 ambient/candidate-helper caveat beside the post-release checkout pin. Appended corrections preserve the containment deferral and register history while identifying the existing approval/dispatch prohibition, its remaining deadline/escalation/public-guidance scope, and the Release Owner or repository administrator's authenticated literal-false read with server timestamp. The missing-workflow/publisher fixture compares captured Git stderr under the ambient locale while retaining the source-path and candidate-commit assertion. The two affected source digest rows were refreshed.
- **Review fixes, 2026-11-16 (review pass 4).** All ten patch findings are addressed. The identity register, architecture, FC-DEP-1, G2 request and GOV-1 story now retain both NC-7 (the matching post-release row's unreachable reusable, contrary to AD-12 `reusable: null`) and NC-33 (ambient/candidate-helper execution) beside the post-release authority. FR-24 lists the post-release pin; architecture separates the gitlink from all three execution pins; G-2 lists the identity register and all four pin-holding workflows. FC-DEP-1 dates its halt observation. REL-AI-1 also supersedes the historical CI-only prerequisite with exact-source successful push CI and Quality. Pin patterns and diagnostics share field/reference constants, and the missing/non-gitlink fixture again requires its original diagnostic prefix. Row 2c names introducing commit `15577f42be42539b5e917cb7ae6716c2c11ac534` (2026-10-18); the historical `3f0e3595be693fce56a37648c0bd0f89390f5fd3` selection is dated 2026-08-14 at FrontComposer `e787690fa6b48973ee83fd9c9b5127c424179dce`. D-6's duplicate reconciliation date is removed. Prior deferrals and rejected findings remain unchanged; no workflow, policy, gitlink, variable or release posture changed.
- **Review fixes, 2026-11-16 (review pass 6).** All four patch findings are addressed. The post-release authority text in the identity register (row 2c), `architecture.md`, FC-DEP-1, the G2 request and the GOV-1 story now carries open NC-6: the `release-evidence.yml` verify step reloads no policy and performs no `post_release` authorization, so no check consults those rows yet. The PRD FR-24 bullet cites NC-6, NC-7 and NC-33. In the four copied caveats, NC-7 is dated to its 2026-09-08 record, followed by a dated 2026-11-16 policy snapshot: all seven `post_release` rows name a Builds `domain-release.yml` reusable, including the row matching the current `release-evidence.yml` blob (sha256 `5cc696fc168000e7d3995a2db3c415761c69e53be21497dffbb5989197a3f524`), whose reusable that workflow never calls. The docs page's G-8 list adds the owner-accepted Hexalith.Builds split revision (EXT-BUILDS-1) entering the AD-16 lineage through delayed activation. FC-DEP-1 Consequences adds the incident runbook's response target. The G2 Deny-Only Publication Stop dates its halt observation and cites NC-35 and NC-36. No workflow, policy, gitlink, variable, register row or release posture changed. While applying these edits, the editor normalized the addendum's four pre-existing LF working-copy lines to CRLF. Git stores LF blobs for both forms, so no digest or blob ID depends on it, and `git diff` shows only the row 2c change. The G2 request working copy remains LF.
- **Line endings.** Every CRLF file remains CRLF with no bare LF. The G2 request and register working copies were LF before this change and remain LF. The addendum's four pre-existing LF lines are untouched. Git normalizes every file to LF blobs, which is what the digests below hash.

## Source Digest Record

These are SHA-256 digests of each reconciled source's Git blob bytes (LF-normalized under `.gitattributes` `* text=auto eol=crlf`), computed 2026-11-16 from the delivery working tree. Each blob ID equals `git hash-object <path>`. At the delivery commit, `git show <commit>:<path> | sha256sum` must reproduce every value. Any difference means this record is stale for GOV-ACCEPT-2. A later edit to any listed source needs a fresh record.

| Source | SHA-256 | Git blob |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/prd.md` | `acb0d692ef19fe5c5fc1e3dfca9797ade474b56b06add4fbbd497ae846455b6a` | `fb13deb5c5fb1104dc9a1a9cff75fc4a87120826` |
| `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` | `706f99d1c4b1eb553eca1788c6339b718c7913da5ede43affd637d1f6e961c53` | `b2b4ea46a97b45e58e7b4c91ec8b1f72200ee260` |
| `_bmad-output/planning-artifacts/architecture.md` | `f7a388f00410597972cf45cda3aae697428e22fe48c9cca665d5e662546e76a9` | `237327abd5ca91b5d3349375a3896021f41b327a` |
| `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` | `d7a7e0cbd8125596247f0ff4ce4e2074e23ce0f87d78c246039d1a10aab89d57` | `f13b25c9be0d3e3d8fc802a465869e7bb6e5e402` |
| `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` | `e383b16810ef45335a3a13702c72795d715f52bf16b1b0dc9877ff2458c87e07` | `e6767172be58469918484406de9b6b46808315f2` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` | `bc2c8caa6accca45851e9fc76455bfb4fd70ca214ba2410be4a6bca712307aaa` | `08c0d00aff79d445e2f95794fd1ef0d33b5782f5` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` | `0c4f81686e40814a97f7985ba0d253516c93f3087d1fbbda742312e42b8ba1d6` | `2189d74541e274787920ebee88e11655fbe5fecf` |
| `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` | `2b40c0f287cddbde375e094483f70289dd5457996817ac13f7daa4b3f2075d13` | `2faf794367e7113f99691d9632595f87316608ea` |
| `docs/release-trusted-publishing.md` | `b39fd982bf375b9cb9f0b1f05b0c37b84062b6d2c628c66d6d33b9ac06b715cf` | `8aab053796fb780099b0472c8816c23a812c56a8` |

## File List

- `eng/release_contract.py` — names the file, field, expected and actual values on every Builds identity mismatch and resolution failure.
- `tests/eng/test_release_contract.py` — adds the pin-drift, duplicate/missing-pin, label and delegation fixtures plus CLI drift, label, observed-gitlink and Git-stderr fixtures.
- `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` — replaces the 2026-09-09 identity table with the dated five-identity register and corrects the current-caller and halt bullets.
- `_bmad-output/planning-artifacts/prd.md` — separates the five identities in §0, G-2, FR-24, §7, D-6 and D-11, applies the halt and caller wording, and dates OI-7.
- `_bmad-output/planning-artifacts/architecture.md` — names each identity's authority, states the caller-owned topology and its nonconformance, and records the observed halt deviation.
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` — adds the current identity and caller section, names EXT-BUILDS-1 and records the observed halt deviation.
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — replaces the stale execution pin with the dated five identities and records the caller and halt deviation.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` — records the 2026-11-16 flag observation and the AD-13 caller deviation.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` — appends NC-34 to NC-36 and one companion-state bullet.
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` — adds the identity, caller and halt-deviation state and updates two owner-decision rows.
- `docs/release-trusted-publishing.md` — uses the deny-only wording, states that the path is legacy and not conformant, and removes "set it to `true`" as an enabling step.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — sets the GOV-SRC-1 action to `review` with this record as evidence; the workflow keeps `epic-17` in progress, sets the `17-1` row to `review`, and refreshes `last_updated`.
- `_bmad-output/implementation-artifacts/spec-17-1-reconcile-publication-governance-sources.md` — this spec: verification results, implementation notes, digest record and file list.
- `_bmad-output/implementation-artifacts/epic-17-context.md` — Epic 17 context compiled for this story by the workflow; read as context and not edited by the implementation.
- `_bmad-output/implementation-artifacts/deferred-work.md` — the Story 17.1 review appended two deferrals (floating `@main` Builds references; no interim containment instruction or deadline while the flag reads `true`).

## NC-35 observation delta on 2026-11-19

The append-only stop observation changes the nonconformance-register entry in
the Story 17.1 source set. The original Source Digest Record above remains
the historical snapshot. Use the following updated register identity when
assembling the next GOV-ACCEPT-2 receipt and revalidate the committed source
set before approval.

| Source | SHA-256 of Git blob bytes | Git blob |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` | `8a708d40f465580dcd1de0d34c9403bc421b326fa81b608af35427a248b0e30a` | `07db58dbc934fb57f0e0c53fc328eb664ff98a4c` |

## Source digests for the 2026-11-19 Trusted Publishing amendment

The following working-tree source identities supersede the earlier register
delta for the selected same-repository OIDC design. They are review material
for GOV-ACCEPT-2. Reproduce them from the reviewed commit and record the
Product Owner and Release Owner decision against that commit after the
source reconciliation is complete.

| Source | SHA-256 of Git blob bytes | Git blob |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/prd.md` | `5523074e2c12e111e35f92c83505bda467fafe063585de3e2c93372c81260e4a` | `6c61073886d18ef2604d56ae9494a8abe5901730` |
| `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` | `4103b551bfa1747e3385addfcbf8ce4af6009f15dcae5b9bc4a0d8d78a6c723b` | `327e607d6c16ba704bc2bb1a8a953619a0af827e` |
| `_bmad-output/planning-artifacts/architecture.md` | `303741999cd5ff2c62109051e427fc21056f38ed68d88ab397830eaad08f209d` | `898c7ad4aacab67f6be96e9e690e4981d7bd43f5` |
| `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` | `04ddf0474e5bdbf45848cd799898432a87e8aaa7db75cd6da8aca1d0dab68547` | `a831059dfb5c9d2cbaaeb08a5e249c25ca7c0a19` |
| `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` | `efe5aa167cacaf5844be18a74db82d29f378ce874f41f54bf910b3e1ac2876ee` | `45cf4660c376b7e99d0001d7fe7b9fd945b24360` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` | `4e12f40925962c2706052c3b03c1db314af7fa46eefb5ca841b0e8135aec5a73` | `9ac7e6d997883b223785817afa3bc6f05a566869` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` | `8a708d40f465580dcd1de0d34c9403bc421b326fa81b608af35427a248b0e30a` | `07db58dbc934fb57f0e0c53fc328eb664ff98a4c` |
| `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` | `a48333cc43b14957e992d5ed20264fe8fc90b91a6d68d21e8db925526d43f821` | `e38512ec20b1a14888ab8100387ff871691326e0` |
| `docs/release-trusted-publishing.md` | `f78e8d3a7efa4f1a9514663098a693e1aa212a0bd4010be4657aad3305753d7c` | `56efbc8bbfdf2156676fb861ff667c52e5b355f7` |

## Source digests for the 2026-11-21 sprint-planning decision

The operator selected the reviewed Platform transition and CI retention for G-8. These working-tree identities allow GOV-ACCEPT-2 reviewers to reproduce the sources from the eventual reviewed commit. The earlier digest sets retain their dated scope. Current-candidate acceptance and recovery of the drifted Platform base remain open. [Decision record](../planning-artifacts/sprint-change-proposal-2026-11-21.md)

| Source | SHA-256 of Git blob bytes | Git blob |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/prd.md` | `16441d68271633b24dd6444cefc4dbccff3a1d7c2d246e70736afac4093f8683` | `602ee4f39b02837272f1d354975e6a2881bac907` |
| `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` | `761e5e9ce367e5ef6529c588e07059b587108e867407f0fb328a715e43b39ac5` | `4e156a2b98a956f8beec4f45d1d515460aaa848a` |
| `_bmad-output/planning-artifacts/architecture.md` | `5e3e6c592276b96e82a55a8932756a8ebe2d6c62672abd5d065e989a3afb9631` | `ee05a373f76ebc7ca93651d1f9f62537acaba722` |
| `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` | `04ddf0474e5bdbf45848cd799898432a87e8aaa7db75cd6da8aca1d0dab68547` | `a831059dfb5c9d2cbaaeb08a5e249c25ca7c0a19` |
| `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` | `af68d3b557b00b7b9e4b3d72e16e5e1d19953f85772b030212788fa8f5150a49` | `6e7932f21cbb5bae44ae8571a0948ee7bfca0601` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` | `3d65dea8fe1e5ab0bb38c1dabb7819090aba24ca7a412166de8c10a8284f97f0` | `e9e2e228d0491e21d721c20f93f1a45a0cad58e5` |
| `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md` | `8a708d40f465580dcd1de0d34c9403bc421b326fa81b608af35427a248b0e30a` | `07db58dbc934fb57f0e0c53fc328eb664ff98a4c` |
| `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` | `6745a02be0c2894a8cba751cbfcfbf16c57e25f3fcb728eb6366189e7dd059a2` | `87e1d566cfcfc95451b142f7de60243b2c0c5f1d` |
| `docs/release-trusted-publishing.md` | `f78e8d3a7efa4f1a9514663098a693e1aa212a0bd4010be4657aad3305753d7c` | `56efbc8bbfdf2156676fb861ff667c52e5b355f7` |
