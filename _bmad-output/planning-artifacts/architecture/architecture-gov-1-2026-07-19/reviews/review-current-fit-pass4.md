# Current-Fit Review, Pass 4 — GOV-1 Dependency Provenance

**Target:** `ARCHITECTURE-SPINE.md` and its two named companions
**Lens:** final repository-fit gate for affected builds, immutable evaluator provenance, authenticated
policy reload, and manifest/handoff canonicalization
**Review date:** 2026-07-19
**Verdict:** **CHANGES REQUIRED.** No Critical finding remains, and AD-1 remains the sole intentional
ratification gate. One High handoff-to-manifest divergence remains.

## Critical

None.

## High

### H-1 — The CI evaluator digest and its handoff-to-manifest binding are not defined

AD-13 closes the handoff shape and gives `evaluator` exactly
`{caller,reusable,actions,definition_digest}` (`ARCHITECTURE-SPINE.md:242-250`). AD-14 defines only the
later manifest-wide `definition_digest` over both CI and release evaluator objects (`:273-282`). That
combined formula cannot define the handoff field: when primary CI creates the handoff, the actual
`workflow_run` release caller/reusable coordinates do not exist yet and may come from a later
default-branch commit.

No rule instead defines the CI-only digest material, and no rule explicitly requires:

- the handoff's `evaluator.definition_digest` to equal a canonical digest of its own
  `{caller,reusable,actions}`;
- manifest `workflow_provenance.ci.{caller,reusable,actions}` to equal the accepted handoff evaluator;
- manifest `workflow_provenance.ci.evidence_sha256` to equal SHA-256 of the exact accepted raw handoff
  bytes.

Consequently, independent CI and release implementations can hash different wrappers or the release
can authenticate one handoff while sealing different CI evaluator coordinates. The immutable action
and workflow closure is therefore recorded but not yet connected by an enforceable canonical contract.

**Required correction:** Define a named CI-only canonical digest, including its exact JSON wrapper and
AD-5 serialization, validate it both when producing and consuming the handoff, and require the three
handoff-to-manifest equalities above. Retain the existing combined CI+release digest as the separate
manifest/fallback definition digest.
