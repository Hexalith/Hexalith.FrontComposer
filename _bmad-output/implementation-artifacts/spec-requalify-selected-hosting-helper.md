---
title: Requalify the selected canonical hosting helper inputs
status: verifying
created: 2026-10-31
baseline_commit: 188d8b733090a83108bd006aaa9fa71cd9a5a2d8
---

# Repair the exact source projection after the authorized dependency update

CI41215250922job124580058593 and Quality41215250572job124578671464
refuse the same stale source-projection seal. Upstreama559 selected Builds
bb1b71ebc2b966b50b974e624e55cf540ad8a29c and changed root MSBuild selectors
and SDK metadata. Preserve those reviewed changes and Gate4 guard updates.
The existing exact guard remains authoritative: requalify the actual selected
helper and unchanged two canonical Platform sources before updating exactly
builds_commit, root_selectors_sha256 and consumer_configuration_sha256.

## File Scope

- `eng/dependency-graph-policy.json` - three exact source-projection expected values only
- `tests/eng/test_dependency_graph.py` - grounded separate-selector/helper/configuration seal controls
- `_bmad-output/implementation-artifacts/spec-requalify-selected-hosting-helper.md` - scope, exact evidence and transition record

No dependency graph guard implementation, global trust, resource bounds,
executable inventories, immutable operator/module receipts, workflows, release
protection or product behavior changes. Platform stays evidence-only; its full
repository remains non-executable and its raw nested inventory stays untrusted.
No nested submodule initialization or skills from references.

## Verification and transition

Read exact failed completed logs, derive input identities from the actual Git
tree, prove unchanged canonical source digests and full eight-selector graph
under the explicit requalified policy, compile the selected source helper with
the actual canonical Compile links, and run source-projection negative controls.
Then validate the exact candidate with pinned commitlint and real owner hooks.

The first repair push still has invalid188 as its immutable event base; preserve
that honest guard refusal. A genuine evidence-only follow-up after the corrected
policy lands uses that already-landed valid base and must pass exact current-head
CI and Quality before qualification is complete. Do not change event-base or
protection policy to fabricate the first transition green. Public4.7.4artifacts
remain unchanged; this build/test correction requires no new package release.

## Cross-Tenant Negative Evidence

**Not triggered:** this correction changes only exact test/source input seals.
No authorization, storage, tenant isolation, contract or product source changes.

## Local qualification evidence

- Actual root188 graph with the explicit requalified policy passes all8Builds
  selectors and the unchanged canonical Platform source/inventory contract.
- Approved root helper selection isbb1b71ebc2b966b50b974e624e55cf540ad8a29c.
  Root selectors SHA256:e32a4364aa59c24194fdf6ce309ce155e74031a7bd202ad9e30671a62c15257e.
  Full committed MSBuild configuration SHA256:b7b4f8827e37e1e133168f826eeeb2dd9b240122bcf5a827554fe1fd42287c26.
- Existing two canonical Compile items and actual selected helper ProjectReference
  compile in a bounded disposable consumer with zero warnings/errors, using the
  actual selected source snapshots and root package catalog. Initial whole
  AppHost probing correctly failed because unconsumed source resource trees were
  not materialized; no source paths or project references were fabricated to
  conceal that limitation. The focused source projection is the sealed two-file
  consumer, not a claim of a new distributed application runtime run.
- ConsumerProjectionTests:30/30pass, including separate selector/helper/config
  requalification controls, candidate self-authorization refusal, canonical
  source/inventory refusal, response/user-import controls, finite acquisition
  bounds and no nested initialization.
- Evidence:`/tmp/frontcomposer-188-seal-qualification.json`,
  `/tmp/frontcomposer-188-qualified-graph.json`,
  `/tmp/frontcomposer-188-canonical-helper-consumer-build.log`,
  `/tmp/frontcomposer-188-projection-tests.log`.
- No product/package source changes; all existing4.7.4public archives remain
  unchanged and no release bump is requested by this test-only correction.

## Landed seal and unchanged-base transition

Correctionbf87ab6224979d7ba8a297b23365ec99db861cbf is pushed. Pinned
commitlint21.2.4 and the real Husky committed-file/commit-message hooks passed.
The actual previously failing C# governance method now passes1/1against the
committed correction, with the genuine eight-selector dependency engine and
isolated exact metadata stores; its Shell test graph builds0warnings/errors.

The immutable188→bf87push replay still refuses the stale188active policy with
`consumer projection root source selectors changed` and exit1, as required.
Evidence:`/tmp/frontcomposer-invalid-base-transition.json`. This is recorded
without changing the active-base guard or historical results. First correction
hosted CI41223229013and Quality41223228512are pending and may be superseded by
this genuine evidence update. The current evidence-only follow-up must qualify
under already-landedbf87policy; final exact-head hosted CI/Quality is still
required. No publication or human verification receipt is fabricated.

Focused commands:

```sh
python3 -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests -v
```

Actual failing-method proof log:
`/tmp/frontcomposer-resealed-real-governance-case.log`.
Committed candidate graph proof:
`/tmp/frontcomposer-resealed-candidate-graph.json`.
Shell consumer test build:
`/tmp/frontcomposer-resealed-shell-build.log`.
