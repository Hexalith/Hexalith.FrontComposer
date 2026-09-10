# Reviewer Gate — rubric walker pass 2 — GOV-1, 2026-09-09

**Reviewed artifact:** `ARCHITECTURE-SPINE.md`  
**SHA-256:** `9dc9ded5670839f24a49ebc4499b6086ddf2590215a178f28311399a01244e5a`  
**Lens:** every item in the BMad Architecture good-spine checklist  
**Intent:** independent review only; no spine, memlog, source, policy, workflow, or setting was changed

## Verdict

**FAIL — 0 Critical, 3 High, 2 Medium, 0 Low.** The spine now states the live release-stop mismatch
honestly, gives the split publication order and archive limits strong fail-closed detail, makes
incident evidence durable, and repairs the two-PR ledger-signoff protocol. Three load-bearing seams
remain non-convergent: the fallback approval cannot be merged through `main` without invalidating the
candidate it approves; the only permitted raw artifact download conflicts with GitHub's redirect-based
artifact API; and the newly named implementation gate is not closed enough to authenticate its own
claimed checks/runs.

The deterministic spine lint passed with zero findings.

## Severity counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 2 |
| Low | 0 |

## Good-spine checklist

| Checklist item | Result | Judgment |
| --- | --- | --- |
| Fixes every real divergence point for the level below | **Fail** | The core graph, evaluator, split-job, handoff, evidence, ordering, privilege, and incident seams are fixed, but fallback approval and implementation-gate evidence still admit incompatible or unreachable implementations (RW2-01, RW2-03). |
| Every AD Rule is enforceable and prevents its stated divergence | **Fail** | AD-9/AD-13's fallback ordering has no stable satisfying execution, and AD-7/AD-12's artifact-network rules prohibit the platform redirect required by AD-19 (RW2-01, RW2-02). |
| Nothing under Deferred could let two units diverge | **Fail** | The split implementation packet is under Deferred but leaves evidence-run member shapes, check-output provenance, and gate-validator authority unstated (RW2-03). Other deferrals have adequate triggers and release-ineligibility guards. |
| Named technology is verified-current and fits | **Fail on one seam** | Pinned Git/Python/.NET/action versions and GitHub.com-only constraints are current enough for the design. The Actions REST artifact download, however, resolves through a signed Azure Blob redirect, which the spine's network rule forbids (RW2-02). |
| Ratifies rather than contradicts the brownfield codebase | **Partial** | Adoption now records the live `HEXALITH_RELEASE_PUBLISH_ENABLED=true` blocker and the Deferred section prevents legacy code from being called conforming. The Structural Seed still labels future v4/v3 behavior as if current (RW2-04). |
| Covers the driving spec/source capabilities | **Pass, gated** | The graph boundary, semantic compatibility, exact-artifact pipeline, operator release, fallback, attestation, package/signature verification, and incident behavior are represented. Superseded manifest/same-job wording remains in the story/G2/parent sources, but reconciliation is explicitly pre-integration and release-gated. |
| Does not weaken or contradict inherited parent invariants | **Pass** | AD-9/AD-13/AD-19 strengthen exact-artifact and pre-publication authorization. The intentional same-job supersession is explicit and requires parent/source reconciliation before integration. |
| Every owned structural/operational dimension is decided, deferred, or open | **Pass** | Ownership, policy activation, CI/release boundaries, schemas, permissions, environment approval, artifact lifecycle, failure/incident operations, network ceilings, deployment/provider scope, and adoption are all addressed. |

## Findings

### RW2-01 — High — the committed fallback approval necessarily invalidates its release candidate

**Affected rules:** AD-9 lines 271–300; AD-13 lines 413–438.

Fallback authorization v3 binds `candidate` and `ci_run` to the already authenticated CI handoff. It
also requires an approval payload file to exist in an unchanged PR head that has merged through
protected `main`. AD-13 independently requires the dispatched/handoff candidate to equal the live
`main` ref and revalidates `main` before protected credentials.

No stable ordering satisfies both rules:

1. CI authenticates candidate **C**.
2. A later approval PR can add a file whose payload names **C**, but merging that PR advances `main`
   to a different commit **A** (or merge commit **M**).
3. Releasing **C** then fails AD-13 because live `main` is **A/M**.
4. Re-running CI for **A/M** does not help: the committed approval file still names **C**. Updating
   the file to name the new candidate creates another commit and repeats the cycle.

Merging the approval after the publisher's final `main` check merely turns the problem into a race:
the approved candidate is no longer the exact current `main` that AD-13 is intended to preserve. The
current public repository's policy says attestation capability is supported, so this does not weaken
today's safe path, but it makes the architecture's required `approved-unsupported` path unreachable
precisely when the platform capability is declared unsupported.

**Disposition:** **discuss and amend before implementation.** Store/approve the fallback receipt on an
immutable authority surface that does not advance the release-candidate branch (for example, a
separate protected governance repository/ref or a closed server-authenticated approval object), while
retaining the exact candidate/CI/fingerprint/policy bindings and owner review. Do not weaken AD-13's
exact-current-main check to accommodate the receipt.

### RW2-02 — High — AD-12 forbids the redirect that AD-7/AD-19 require to obtain the raw artifact ZIP

**Affected rules:** AD-7 lines 207–218; AD-12 lines 381–391; AD-19 lines 865–868.

AD-7 and AD-19 correctly forbid auto-extraction and require the publisher to stream the raw ZIP from
the authenticated Actions artifact archive endpoint before extraction. AD-12, however, allows
data-only requests only to named GitHub/Sigstore/NuGet endpoints and expressly forbids downloading a
`redirect target`.

GitHub's **Download an artifact** REST operation is redirect-based. A live read-only download of
FrontComposer artifact `10094906985` through
`repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/10094906985/zip` returned the final payload
from `Windows-Azure-Blob/1.0` with `x-ms-*` headers; the GitHub client followed the API's temporary
signed blob URL. Thus an implementation either follows a non-GitHub redirect and violates AD-12 or
refuses it and cannot obtain the bytes AD-7/AD-19 require. GitHub documents the endpoint's `302 Found`
response and temporary `Location` URL in its official REST Actions artifact documentation.

**Disposition:** **autofix the network rule.** Permit exactly the artifact archive endpoint's
authenticated, bounded redirect as a data-only transfer: one redirect, HTTPS, no credential/header
forwarding, host/scheme validation against the returned signed URL policy, no further redirects,
streaming hard stop, and archive authentication by run/artifact ID/name plus recomputed digest. Keep
runtime executable acquisition forbidden.

Primary reference: [GitHub REST — Download an artifact](https://docs.github.com/en/rest/actions/artifacts?apiVersion=2022-11-28#download-an-artifact).

### RW2-03 — High — the split implementation gate records assertions but does not authenticate them

**Affected rule:** Deferred lines 1093–1124.

The new conformance packet is a strong improvement: it binds the spine, FrontComposer, Builds,
policy, caller, register, reviews, and required rehearsals. Its exact top-level member set is closed,
but the evidence it relies on is not:

- `evidence_runs` names four keys but gives no exact value schema. “Using authenticated AD-15 run
  coordinates” is ambiguous because CI, Release, hostile-candidate, and verification runs have
  different authoritative projections, and AD-15 itself defines more than one run shape.
- All four are required to have successful conclusions, but it is not stated whether the hostile
  candidate's success means a successful denial test, a successful workflow, or a publisher that was
  correctly skipped/failed.
- A check row carries only a free-form `command`, caller-supplied `conclusion: success`, and a hash. It
  does not identify where the hashed output is durably stored, which authenticated run/job produced
  it, or which exact pinned validator recomputed the packet.
- The packet's owner-approved PR prevents casual mutation but does not stop two conforming producers
  from choosing incompatible run projections or hashing arbitrary local output while both satisfying
  the prose shape.

Because this packet is the named machine gate that converts a draft target into release eligibility,
human review alone is not an adequate substitute for the missing evidence contract.

**Disposition:** **autofix before relying on the gate.** Close each `evidence_runs` projection
member-by-member, define exact expected outcomes (especially hostile candidate and gate-frozen), bind
each check to authenticated workflow/run/attempt/job/artifact coordinates and durable raw-output
digest, name the pinned verifier/command that validates the packet, and require that validator's
result as a protected check on the unchanged packet PR.

### RW2-04 — Medium — Structural Seed mixes current brownfield state with the target state

**Affected section:** Structural Seed lines 996–1023.

The seed labels `release_evidence.py` a manifest-v4 producer/sealer/verifier and describes the release
handoff/ledger files by their target responsibilities. At the reviewed brownfield HEAD,
`release_evidence.py` still defines current manifest v3, `dependency_handoff.py` still defines Release
handoff v1, and the policy lacks the new owner/capability/archive-limit fields. The Deferred/adoption
text is honest about migration, but the seed is not explicitly marked “target after implementation.”
That conflicts with the skill's seed rule: structural seed should be true at cold start and become
code-owned once the code exists.

**Disposition:** **autofix.** Mark the tree explicitly as target structure, or describe current
responsibility first and name the target migration in a separate compact mapping. Keep the Deferred
register as the authoritative delta.

### RW2-05 — Medium — AD-12's primary policy declaration omits two fields later made authoritative

**Affected text:** AD-12 lines 336–344; Executable Policy Authority lines 962–969.

AD-12's Rule says the closed policy is the sole authority for trusted identities, profiles, build
registry, `resource_limits`, and `evaluator_authorizations`. The later policy section additionally
makes `release_owner_logins` and `attestation_capability` executable authority. Those fields are
load-bearing for fallback approval and ledger sign-off, but the governing AD's primary declaration
does not name them. This is not a deep design conflict—the later section is clear—but schema
implementers and fixtures reading only AD-12 can legitimately produce a different top-level policy
shape.

**Disposition:** **autofix.** Add `release_owner_logins` and `attestation_capability` to AD-12's sole-
authority list and to the Structural Seed policy annotation. Keep the values out of the spine.

## Confirmed strengths after the latest hardening

- **Fallback:** v3 now binds exact candidate/CI, policy authority, fingerprint material, an owner API
  review, expiry, and a raw durable asset. Its data shape is strong; RW2-01 is the remaining lifecycle
  contradiction.
- **Incident evidence:** a distinct protected, candidate-free workflow creates an immutable reserved
  evidence Release, inventories all available raw evidence, excludes the tag from product-version
  discovery, and retains a fail-closed no-retry rule when preservation is unavailable.
- **Ordering:** AD-9 and AD-19 now align on archive authentication, bounded byte verification,
  attestation/fallback, final manifest preparation/seal/offline/live/classification, the immediate
  owner marker, draft/asset verification, NuGet publication/verification, and one final draft publish.
- **Archive safety:** raw and expanded byte/member/path/ratio ceilings, redirect-free pre-scan intent,
  central-directory/local-header/CRC agreement, ZIP64 consistency, non-regular member rejection, and
  bounded materialization are explicit. RW2-02 concerns transport, not scanner completeness.
- **Ledger protocol:** the machine record is append-only and monotonic; the repaired sign-off uses a
  separate approval PR and projection PR, authenticates both through protected `main`, and avoids
  changing an already approved head.
- **Adoption reality:** the live `HEXALITH_RELEASE_PUBLISH_ENABLED=true` observation is now stated as a
  Critical operational blocker with an exact Release Owner/admin remediation and authenticated
  recheck; the spine no longer pretends the legacy path is frozen.

## Incomplete lower-severity checks

- No accepted split reusable or conformance packet exists yet, so job-name rendering, artifact action
  behavior, attestation-result paths, hostile-candidate isolation, gate-frozen rehearsal, and immutable
  incident-Release creation could not be exercised end to end.
- This rubric pass did not re-run the full repository test suite; it relied on the deterministic spine
  lint, exact source inspection, the live artifact-download response, and prior focused test evidence.
- Product Owner/Release Owner acceptance, the incident response target, ruleset/CODEOWNERS, and the
  literal-false emergency stop remain external prerequisites exactly as the spine states.

