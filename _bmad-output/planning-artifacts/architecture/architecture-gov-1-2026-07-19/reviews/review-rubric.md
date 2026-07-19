# Reviewer Gate — Good-Spine Rubric

**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-07-19  
**Lens:** real divergence points, enforceable rules, safe deferral, brownfield fit, capability coverage,
source/inherited conflicts, and epic-level operational completeness

## Gate Verdict

**CHANGES REQUIRED:** the bounded committed-object paradigm is coherent and the draft is unusually
strong on deterministic evidence and fail-closed behavior, but it cannot be finalized while its graph
boundary contradicts the approved PRD/Epic contract, and three policy/migration seams still permit
incompatible implementations.

## Critical Findings

### C1 — The load-bearing boundary still contradicts the approved capability contract

- **Evidence:** AD-1 is explicitly `[ASSUMPTION]` and limits v1 to depth 1–2, while `prd.md` SM-2a and
  D-11, `epics.md` GOV-1 AC1/AC5, and the approved portion of FC-DEP-1 still require the "complete
  reachable" graph. The spine's Deferred section acknowledges reconciliation but leaves the
  contradiction in force. This changes both the set of evidence sealed and the meaning of successful
  GOV-1 completion; it is not an implementation detail.
- **Why it matters:** one builder following the spine would deliberately omit deeper edges while one
  following the current acceptance criteria would traverse them. Either can claim compliance against a
  different authoritative source.
- **Disposition:** **Discuss, then autofix.** Architect and Release Owner must explicitly ratify or reject
  the bounded v1 decision. If ratified, promote AD-1 from assumption to adopted and atomically reconcile
  FC-DEP-1, PRD, epics, proposal, canonical architecture, and GOV-1 acceptance criteria before
  development. If rejected, replace AD-1 with an implementable complete-reachable boundary and resolve
  the known legacy-identity/object and traversal-budget problems before handoff. Do not finalize a spine
  whose central rule remains an assumption.

## High Findings

### H1 — Policy revision and trust bootstrap are unspecified

- **Evidence:** AD-3 validates base and candidate graphs against "the versioned ... policy in AD-12";
  AD-10 acquires objects from policy-approved remotes; AD-12 introduces
  `eng/dependency-graph-policy.json`. None says which commit's policy is authoritative for base
  collection, candidate collection, pull-request acquisition, live verification, or the migration
  baseline where the policy file does not yet exist.
- **Why it matters:** implementations can incompatibly choose the base policy, candidate policy, current
  working-tree policy, or one policy for both graphs. Using candidate-owned policy for remote acquisition
  also changes the trust boundary in the same change being evaluated; using only the base policy makes
  an approved repository addition impossible. Historical recomputation can likewise drift if it reads
  today's policy.
- **Disposition:** **Autofix.** Add one enforceable policy-revision rule and bootstrap path. It must state
  which exact committed policy object governs each graph, how an authorized policy expansion is
  reviewed, how the first policy-bearing revision compares with a legacy base, and which policy identity
  or digest is recorded/bound so live verification reproduces the same semantics. Acquisition must not
  consult ambient or unsealed policy.

### H2 — The affected-module command inventory is unsafe to defer

- **Evidence:** AD-8 requires a static command map and defines edge-to-module selection, but Deferred
  leaves the exact supported modules/argv to implementation. AD-12 makes that map a single policy owner
  without requiring total coverage of every in-boundary build target or specifying what happens when an
  affected identity lacks a command.
- **Why it matters:** independent workflow, policy, and test implementations can select different
  solutions, omit different root modules, or treat an unmapped changed edge as success. That directly
  changes AC4's release-blocking restore/build proof.
- **Disposition:** **Autofix.** Move the invariant out of Deferred: every buildable depth-1 target and
  every possible depth-2 owner must have exactly one policy-owned argv entry, or collection must fail
  closed before reporting the graph gate successful. Pin the Release/NuGet restore/build shape and the
  deterministic fallback for removed/unbuildable candidate owners. The concrete current inventory may
  remain structural seed in the policy, but totality, uniqueness, and failure behavior belong in an AD.

### H3 — Semantic-profile selection is not a complete contract

- **Evidence:** AD-6 calls for semantic evaluation "for every selector edge or equivalent owner contract
  profile," while AD-12 assigns profiles to policy. Neither defines the selector key, profile schema,
  version, completeness requirement, or behavior when an owner has no/duplicate profile. The current C#
  checks and story Dev Notes contain the real package/import invariants, but the spine does not bind how
  they become the canonical Python policy.
- **Why it matters:** implementations can validate different catalogs or different subsets of the
  semantic contract and still satisfy the prose. Deduplicating evaluation by Builds commit is also
  incorrect when two owners select the same catalog under different contract profiles unless the
  semantic result is keyed by both catalog and profile.
- **Disposition:** **Autofix.** Define a versioned semantic-profile key and total selection rule (for
  example, owner repository plus gitlink path/profile ID), reject missing/duplicate/unknown profiles,
  and cache evaluation by `(Builds repository, Builds commit, profile identity/version)` while retaining
  every selector in diagnostics. State that migration must encode all currently governed semantic
  assertions before the C# SHA pins are removed.

### H4 — Manifest evolution and historical verification have no compatibility decision

- **Evidence:** AD-9 requires an atomic graph/schema rollout and invalidates evidence lacking the graph,
  but it does not say whether the upgraded verifier accepts existing pre-GOV-1 manifests, whether those
  can only be checked in legacy/offline mode, or whether a deliberate release-evidence format cutoff is
  being made. This is a brownfield extension of `release_evidence.py` 1.2.0 and an existing public
  evidence chain.
- **Why it matters:** one implementation may reject all historical evidence, another may silently treat
  an absent graph as valid, and a third may synthesize graph data. Those choices have different release
  and incident-response consequences.
- **Disposition:** **Discuss, then autofix.** Select an explicit migration rule. A safe default is:
  preparation of new governed candidates always emits the new manifest format; authorization of a new
  release never accepts a legacy manifest; historical legacy manifests may retain a clearly labeled
  structural verification path that cannot authorize publication and never fabricates graph evidence.
  If backward verification is intentionally dropped, record the cutoff and operational consequence.

## Medium Findings

### M1 — The canonical data grammar needs exact JSON type and member rules

- **Evidence:** AD-5 gives field names, hashes, ordering, and serializer settings, but does not state
  whether unknown members are rejected, whether `edge_count`/`depth` must be JSON integers rather than
  booleans or integral floats, or the exact allowed set/type of fields on Builds versus non-Builds
  edges. `catalog_contract_version` is nullable but its non-null grammar is unspecified.
- **Risk:** permissive and strict verifiers can disagree over the same manifest; Python's type hierarchy
  makes booleans a common integer-validation trap.
- **Disposition:** **Autofix.** Pin closed object shapes, exact JSON types/ranges, unknown-member behavior,
  and current marker grammar (or require `null` until BUILD-CAT-1 publishes a separately adopted grammar).

### M2 — The graph-diff removal fallback is incomplete

- **Evidence:** AD-8 says a removed depth-2 edge builds the candidate owner and a removed depth-1 edge
  builds FrontComposer. It does not cover an owner that is itself removed/renamed, an affected owner
  without a candidate solution, or a repository-identity/path remap where the logical tuple changes into
  remove-plus-add.
- **Risk:** CI implementations can skip the executable proof precisely on destructive graph changes.
- **Disposition:** **Autofix.** Define a deterministic conservative fallback: when the candidate owner
  cannot be materialized or mapped, build the FrontComposer root/full supported set or fail the gate;
  never downgrade to no affected module.

### M3 — Operational ownership is inherited but not named precisely enough

- **Evidence:** the spine correctly defers deployment/provider topology because GOV-1 adds no service,
  and AD-9 preserves pre/post-publication separation. However, it does not explicitly name who owns
  policy changes, ceiling exceptions, evidence retention, or graph-verification incident triage. The
  canonical architecture already assigns related responsibility to FrontComposer and the Release Owner.
- **Risk:** the technical gate is enforceable, but operational exceptions can bypass or stall it without
  a named authority.
- **Disposition:** **Autofix.** Inherit the existing ownership explicitly: FrontComposer owns collector,
  policy, evidence, and diagnostics; Architect + Release Owner approve trust/boundary/ceiling changes;
  Release Owner owns release exceptions and post-publication incident response. No new provider design
  is needed.

## Low Findings

### L1 — "Visited" language is misleading for the fixed v1 traversal

- **Evidence:** AD-4 says edges are recorded before a visited check, while AD-1 and AD-7 define a fixed
  two-level enumeration that needs object-read caching, not recursive cycle termination.
- **Disposition:** **Autofix.** Call this an owner-object cache/deduplication set for v1. Reserve recursive
  visited semantics for a future transitive schema so implementers do not accidentally suppress a valid
  second edge.

### L2 — The Deployment/Provider Deferred item points to FR-24 too broadly

- **Evidence:** FR-24 governs release evidence, but does not by itself own all environment/provider
  architecture.
- **Disposition:** **Autofix.** State that runtime deployment/provider topology is unchanged and inherited
  from the existing platform architecture; only the existing CI/release execution environments are in
  scope here.

## Checklist Assessment

| Good-spine criterion | Result | Assessment |
| --- | --- | --- |
| Real divergence points fixed | **Partial** | Strong on graph identity, ordering, canonicalization, failure, CI revision model, and verification modes; policy revision, profile selection, module-map totality, and manifest migration remain open. |
| Every AD enforceable and prevents its divergence | **Partial** | Most rules are testable. AD-1 is unratified; AD-6/AD-12 and AD-8 leave selection/coverage semantics under-specified. |
| Deferred items are safe | **Fail** | Historical traversal, marker mandate, and provider topology are safe deferrals; the affected-module inventory is not safe without a totality/fail-closed invariant. |
| Named technology verified-current | **Pass** | The spine pins the observed Git, Python, .NET SDK, and Git object format; selected plumbing/stdlib facilities are stable and no new third-party stack is introduced. |
| Brownfield fit | **Partial** | It correctly extends the existing Python release seam, C# Governance lane, CI, and exact-artifact pipeline. Historical-manifest behavior and policy bootstrap must be decided. |
| Source/spec capability coverage | **Fail pending C1** | Compatibility/provenance separation and AC2–AC6 are covered, but the bounded graph intentionally changes AC1/AC5's currently approved complete-reachable scope. |
| Inherited constraints/conflicts | **Fail pending C1** | No parent spine is declared. The known PRD/Epic/FC-DEP-1 conflict is surfaced rather than hidden, but remains unresolved and therefore blocks final status. |
| Owned dimensions complete | **Partial** | Data integrity, trust, CI, failure modes, and release behavior are well covered. Migration/backward verification and operational exception ownership need decisions; runtime infrastructure/UX are correctly out of scope. |

## Clear Strengths to Preserve

- The named **bounded committed-object graph** paradigm carries the design coherently and separates
  semantic compatibility from immutable provenance.
- AD-2, AD-5, AD-7, AD-9, and AD-10 form a strong fail-closed chain from exact Git objects through
  canonical graph bytes to offline/live release verification.
- Candidate-controlled URLs and commands are excluded; the design does not rely on ambient indexes,
  nested checkout HEADs, recursive initialization, or mutable fingerprint allowlists.
- The structural seed is lean and reuses the existing release-evidence path instead of introducing a
  competing manifest subsystem.
- Runtime/API/UX/package scope and the BUILD-CAT-1 upstream boundary are correctly excluded.

## Recommended Gate Closure Order

1. Resolve C1 and reconcile the authoritative requirements/decision artifacts.
2. Fix H1–H3 in the spine before implementation so policy, semantics, and build coverage converge.
3. Decide H4's brownfield migration rule.
4. Apply M1–M3 and the low-cost wording fixes; rerun deterministic lint and all configured reviewer
   lenses.

