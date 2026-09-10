# Adversarial Divergence Review — GOV-1 Architecture Update

- Review date: 2026-09-09
- Lens: adversarial divergence
- Artifact: `ARCHITECTURE-SPINE.md`
- Spine SHA-256: `95cbe2f2754ad3c1d7c896e73822b49edb676c0f5921c4f05fded82519c6e6e8`
- Verdict: **BLOCK — 1 Critical, 5 High, 3 Medium findings**

The spine has materially improved its state model, split-publication boundary, manifest lineage, and
fail-closed dispositions. The remaining blocker is not a missing implementation detail: one temporal
privilege invariant cannot be represented by the named GitHub Actions topology. The High findings are
places where two competent implementers can still make incompatible security or evidence decisions.

## Critical

### ADV-C1 — The “authority only after hash verification” boundary is not realizable in one publisher job

- **Applies to:** AD-18, AD-19, structural flow.
- **Hole:** AD-18 says OIDC/attestation authority is exposed only after artifact hashes are verified,
  while AD-19 makes the protected `publish-publication-candidate` role download and authenticate that
  artifact. GitHub Actions grants `id-token: write`, environment secrets, and job permissions at job
  scope, before its steps execute; it has no step-level permission elevation. AD-19 also fixes two
  roles and the flow depicts a single protected publisher, so an unprivileged verifier predecessor is
  neither required nor given a closed handoff contract.
- **Divergence:** one implementer grants the publisher authority at job start and violates the temporal
  rule; another adds a third verifier job and invents an unauthenticated verifier-to-publisher proof;
  a third treats “candidate-free” as sufficient and silently weakens “only after.”
- **Falsifier:** compile a conforming two-job workflow and request an OIDC token in the publisher's
  first step, before archive download. If it succeeds, the stated order is false. If permission is
  removed, the later attestation step cannot mint a token.
- **Required closure:** either (a) replace the temporal claim with the actual invariant—candidate bytes
  are never executed and only pinned owner code can access job-scoped authority—or (b) require a named
  unprivileged verifier job plus an exact, content-addressed cross-job proof and define what the
  privileged job must recheck without parsing attacker-controlled archives. Align AD-19 topology and
  mandatory fixtures with that choice.

## High

### ADV-H1 — `approved-unsupported` is a reusable bypass, not the claimed single-use approval

- **Applies to:** AD-9, AD-15, AD-19.
- **Hole:** the approval digest is bound to the selected CI `(candidate, run_id, run_attempt)`, but not
  to a Release run/attempt. Multiple Release dispatches against the same CI handoff can therefore
  reuse it. The contract also does not close the condition that makes attestation “unsupported,” the
  allowed failure taxonomy, approval expiry, or one-time consumption.
- **Divergence:** a publisher may fall back after any attestation error indefinitely; another may allow
  it only for a documented platform capability outage. Both can validate the same schema and digest.
- **Falsifier:** dispatch two Release runs against one approved CI handoff with attestation available;
  accept `approved-unsupported` in both. Current equations do not distinguish them.
- **Required closure:** define a closed fallback-authorization record with reason enum, issuer, created
  and expiry times, exact candidate/CI/Release coordinates or nonce, and consumption semantics; prove
  the permitted unsupported condition live and fail closed for operational or verification errors.

### ADV-H2 — Attestation acceptance authenticates digests but not a closed provenance identity

- **Applies to:** AD-9, AD-15, AD-17, AD-19.
- **Hole:** `bundle authenticates every package digest` does not specify the accepted issuer, source
  repository, workflow/ref/environment identity, predicate type, exact subject set, or whether extra
  subjects are forbidden. The manifest merely seals the resulting projection.
- **Divergence:** one verifier can accept any valid bundle containing the package digest; another can
  require the protected FrontComposer Release workflow identity. Both satisfy the written digest rule.
- **Falsifier:** supply a cryptographically valid GitHub/Sigstore bundle for identical bytes created by
  another repository or workflow. Acceptance demonstrates the gap.
- **Required closure:** pin the attestation bundle/schema and verification predicate: trusted issuer,
  repository owner/name, workflow path and exact workflow commit, ref/candidate, environment, predicate
  type, and an exact sorted subject set with no extras. Add hostile cross-repository/workflow fixtures.

### ADV-H3 — Static source closure does not close runtime-loaded executable code in protected jobs

- **Applies to:** AD-12, AD-18, AD-19.
- **Hole:** closure follows `uses:` and stops JavaScript actions at a pinned commit. It does not state a
  machine rule for an otherwise pinned workflow/action/shell step that downloads and executes mutable
  scripts, installers, plugins, or tools at runtime. AD-19's “pinned owner-controlled code” principle
  is stronger than the specified closure algorithm and has no acceptance rule.
- **Divergence:** one implementation is hermetic; another runs `curl ... | sh`, `npm install` without a
  lock-integrity check, or an equivalent bootstrap from a mutable endpoint inside the publisher.
- **Falsifier:** add a commit-pinned action whose committed entry point fetches and executes an unpinned
  URL. If source-closure Governance passes, publication authority depends on bytes outside the seal.
- **Required closure:** explicitly prohibit runtime acquisition/execution of code, actions, plugins, and
  installers in publisher/attester/post-release jobs, or define a digest-pinned allowlist and verifier.
  Add closure fixtures for shell, JavaScript child processes, package-manager bootstrap, and redirects.

### ADV-H4 — The privileged publisher parses an archive with no archive-specific resource limits

- **Applies to:** AD-7, AD-18, AD-19, Deferred network ceilings.
- **Hole:** AD-7 caps graphs, blobs, trees, and workflow closure, but not publication archive bytes,
  member count, total uncompressed bytes, per-member bytes, path depth, or compression ratio. The
  candidate controls this archive, and the protected publisher must download and inspect it while
  holding job-scoped environment credentials/permissions.
- **Divergence:** implementations may fully extract, stream, or rely on platform artifact limits, with
  materially different denial-of-service and parser exposure. None has a spine-defined ceiling.
- **Falsifier:** submit a validly named, digest-bound archive with millions of entries or extreme
  expansion ratio. Exhaustion before fail-closed validation demonstrates the gap.
- **Required closure:** add explicit raw/member/count/per-file/total-uncompressed/ratio/path-depth caps;
  measure from a streaming central-directory pass before extraction, forbid filesystem extraction when
  unnecessary, and require failure before privileged publication logic consumes the contents.

### ADV-H5 — “Bounded integers” has no interoperable bound for run IDs, sizes, and artifact IDs

- **Applies to:** AD-5, AD-15, AD-19.
- **Hole:** canonical schemas allow only bounded integers, but later shapes say merely positive or
  non-negative. Python accepts arbitrary precision while JavaScript/GitHub expression paths may round
  above `2^53-1`; canonical bytes, observation keys, and SHA-256 seals can then diverge.
- **Divergence:** a Python producer may emit values another conforming implementation cannot represent
  exactly, or implementations may choose `int64`, `uint64`, or safe-integer bounds independently.
- **Falsifier:** run cross-language vectors at `2^53`, `2^63`, and `2^64`; any rounded/rejected/different
  canonical byte sequence demonstrates incompatible conformance.
- **Required closure:** give every integer field an exact inclusive domain—prefer decimal strings for
  external IDs or mandate `0..2^53-1`/`1..2^53-1`—and add boundary/overflow golden fixtures.

## Medium

### ADV-M1 — Durable evidence does not explicitly include the candidate descriptor and raw CI handoff

AD-15 calls Release assets durable, while the descriptor itself is not a `files` row and the raw CI
handoff is not explicitly a mandatory durable asset. After run-artifact expiry, implementations can
disagree whether a sealed projection/hash is enough for an independent replay. State the historical
audit objective and require the exact descriptor/handoff bytes as immutable assets if replay is
required; otherwise say that hash-only historical verification is the intended limit.

### ADV-M2 — `asset_name` lacks a portable destination-name grammar

Slash-free/no-control plus ordinal uniqueness permits backslashes, leading/trailing dots or spaces,
Unicode normalization variants, and option-like names. GitHub/API/client normalization may collapse
names that are distinct ordinally. Use a conservative ASCII regex, length bound, forbidden reserved
names, and uniqueness after the destination's documented normalization; verify returned names exactly.

### ADV-M3 — RFC 3339 UTC is not a canonical byte spelling

`Z` versus `+00:00` and differing fractional-second precision are semantically equal but byte-distinct.
That matters for sealed records and the byte-identical duplicate-observation rule. Pin one timestamp
grammar and precision (for example `YYYY-MM-DDTHH:MM:SSZ`) and reject alternate spellings.

## Gate disposition

Do not finalize or enable `split-publication-v1` while ADV-C1 is open. ADV-H1 through ADV-H5 should be
closed in the spine or converted into explicit pre-integration gates with an owner, acceptance test,
and no-bypass halt. Medium findings may be resolved in implementation contracts only if the spine
names that authority and forbids incompatible alternatives.
