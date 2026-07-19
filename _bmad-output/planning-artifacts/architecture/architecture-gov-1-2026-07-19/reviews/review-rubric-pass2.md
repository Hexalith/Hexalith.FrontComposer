# Reviewer Gate — Rubric Pass 2

**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-07-19  
**Scope:** prior findings H1–H4 only, plus detection of any new Critical/High issue. AD-1 remains an
intentional ratification gate and is not re-scored here.

## Verdict

**CHANGES REQUIRED:** policy revision/bootstrap and manifest migration are now resolved, but affected-
module execution and semantic-profile contents still allow implementations that do not meet GOV-1's
compatibility proof. No new Critical/High issue was introduced.

## Prior-Finding Disposition

### Prior H1 — Policy revision and trust bootstrap: Resolved

AD-12 now selects one immutable active policy from the exact PR base or non-zero push-before commit,
uses it for both graphs, records its repository/commit/schema/raw digest, prevents a candidate policy
from authorizing itself, and defines a frozen, approved one-time bootstrap. AD-13 propagates those
coordinates into the exact release revision. This removes the prior base/candidate/ambient-policy
choice.

### Prior H2 — Affected-module command inventory: Partially resolved; High remains

The closed matrix now gives every governed identity exactly one `build` or `evidence-only` disposition,
pins solution names, rejects missing entries, and delays policy expansion. Those changes resolve the
coverage/default ambiguity.

The literal argv contract is nevertheless non-conforming with the GOV-1 story:

- restore omits `-p:Configuration=Release` and `-p:UseNuGetDeps=true`;
- build omits `-p:UseNuGetDeps=true`; and
- neither AD-8 nor the matrix requires materializing the exact selected Builds catalog into the
  isolated exact-commit checkout before restore/build.

Without those rules, the gate can evaluate source-project wiring or an absent/ambient catalog rather
than the selected shared catalog whose compatibility it is meant to prove.

**Required fix:** make the literal arrays match the story contract: Release/NuGet-mode restore and
Release `--no-restore` build, both with `UseNuGetDeps=true`; bind exact catalog materialization from the
selected committed object without moving a shared checkout. Retain the closed registry and fail-closed
missing-entry behavior.

### Prior H3 — Semantic-profile selection: Partially resolved; High remains

AD-6 and the Closed Policy Seed now provide a total owner-to-profile mapping, prohibit defaults, reject
missing/unknown mappings, and preserve every selector. That resolves profile selection.

The compatibility rules inside those profiles remain delegated to implementation: the seed says the
implementation may "refine package assertions" and neither the spine nor FC-DEP-1 fixes the minimum
rule set/profile schema that must exist before the historical C# SHA pins are removed. Two builders can
therefore create identically named profiles that validate materially different package/import
contracts.

**Required fix:** bind each profile to a versioned closed rule schema and require the migration to encode
all semantic assertions currently governed by `InfrastructureGovernanceTests.cs` and the GOV-1 story's
Semantic Catalog Contract. Add an explicit invariant that a rule may be strengthened/changed only by a
reviewed delayed policy revision, not chosen during implementation. Semantic-result caching, if added,
must include profile identity/version as well as Builds repository/commit.

### Prior H4 — Manifest migration and historical verification: Resolved

AD-14 makes v2 mandatory for new governed evidence, confines legacy manifests to explicit audit-only
diagnostics, prohibits publication/fallback/reseal/upgrade, preserves historical ledger bytes, and
migrates current fixtures atomically. This is a clear one-way brownfield compatibility decision.

## New Critical/High Findings

None. AD-13 materially tightens the existing release seam, but its exact-CI-revision propagation,
immutable reusable-workflow provenance, and continued publication freeze are internally coherent with
the fail-closed GOV-1 release model.

## Gate Closure

Apply the two High fixes above, then rerun the deterministic lint and reviewer lenses. Subject to the
separate AD-1 ratification gate, this rubric lens would then pass.

