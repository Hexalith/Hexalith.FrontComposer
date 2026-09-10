# Reviewer Gate — security / supply-chain lens — pass 2 — 2026-09-09

- **Object:** `ARCHITECTURE-SPINE.md` — GOV-1 dependency provenance
- **Reviewed SHA-256:** `4a765adbf2026396d9d1258163a41f155522d8c3a7304d6d6857533a5a37624f`
- **Mode:** independent final recheck; no spine, memlog, source, workflow, or policy file changed
- **Scope:** prior `SEC-UPD-01..05`; deployment-comment fallback; `incident_recovery`; reviewer sidecars and packet approval projection; Critical/High only

## Verdict

**PASS — Critical 0, High 0.** All five prior security findings are closed without weakening the
candidate/publisher privilege boundary. The new fallback authority is authenticated, request- and
attempt-bound; raw fallback bytes are durable; incident evidence has an isolated protected writer and
an immutable run-specific destination; platform action/redirect acquisition is narrowly stated; and
the publication order is singular. The reviewer sidecars and two-PR packet approval projection bind
the evidence to authenticated producer/artifact/review coordinates without a self-reference or
authority gap.

Lower-severity concerns were intentionally not sought in this timeboxed final recheck.

## Prior-finding closure

| Prior finding | Status | Security evidence in the current spine |
| --- | --- | --- |
| `SEC-UPD-01` — fallback issuer self-asserted | **Closed** | The dispatch record is a request, not authority. Pinned publisher code authenticates an `approved` review of the same run's protected `production` environment; the exact comment binds the request SHA-256 plus current Release run ID and run attempt; API user IDs/logins must be active-policy Release Owners; environment ID/name and `can_admins_bypass:false` must match live sampled state. Generic, stale-attempt, absent, unequal-duplicate, unauthorized-user, and mixed-environment rows fail closed. |
| `SEC-UPD-02` — fallback bytes not retained | **Closed** | Pinned code creates a closed v3 authorization containing the byte-exact request, request digest, Release run, and authenticated approval projection. Its raw canonical bytes are bound in manifest/handoff by path and SHA-256 and are a mandatory immutable Release asset. Post-release verification can therefore replay the sole fallback authority without relying on a mutable input. |
| `SEC-UPD-03` — no durable partial-publication evidence | **Closed** | AD-15 defines a separately approved, candidate-free `incident_recovery` stage that writes an immutable prerelease in a reserved run/recovery-specific namespace. The closed slot inventory records every evidence class as present, quarantined, or reasoned absent; authenticated error/API evidence backs absence; later recoveries never adopt/replace a partial predecessor and must account for every earlier attempt. Product retry remains blocked until immutable preservation succeeds. |
| `SEC-UPD-04` — runtime action delivery contradicted | **Closed** | AD-12 explicitly accepts only GitHub runner resolution of closure-listed literal-commit actions. The one raw-artifact redirect is data-only, strips authorization/cookies/caller headers, permits one HTTPS 302 and no second redirect, applies the streaming cap, suppresses the signed query from logs, and authenticates resulting bytes by coordinate and digest. Other downloaded/redirected executable content remains forbidden. |
| `SEC-UPD-05` — conflicting pre-publication order | **Closed** | AD-9 now states one complete order: authenticate archive/descriptor → verify bytes and limits → mint/verify attestation or authenticate fallback → prepare → seal → offline/live verify → classify → set owner-controlled pre-side-effect marker → publish. AD-19 follows that order, and the manifest can bind the completed attestation/fallback projection without circularity. |

## New-surface security checks

### Deployment-comment fallback

The exact comment
`hexalith-attestation-fallback-v1:<request_sha256>:<release_run.run_id>:<release_run.run_attempt>`
closes reuse across both distinct runs and rerun attempts. It binds the approver's protected-environment
decision to the byte-exact candidate/CI/policy/fingerprint request while keeping the request itself
non-authoritative. The final authorization repeats the Release coordinate and must byte-match the
safe-integer comment suffix and authenticated current run/attempt. A new attempt therefore needs a new
qualifying approval even if the API returns prior approval history. The API exposes no approval-event
timestamp; using a sampling time and requiring it no later than request expiry avoids fabricated
evidence and rejects an expired approval path.

No candidate code handles the environment token, approval query, or authorization construction. The
request is untrusted workflow input until pinned candidate-free publisher code recomputes every binding
and authenticates the API actor/environment. Current public-GitHub capability remains `supported`, so
operational attestation failures cannot exercise fallback.

### `incident_recovery` authority

Recovery is its own AD-12 evaluator stage rather than an ambient post-release script. Its caller and
every action are literal-commit closure members authorized through delayed policy activation. It runs
candidate-free on GitHub-hosted `ubuntu-24.04` behind `production` review with `contents:write` plus
read-only evidence access, but no NuGet secret, package scope, OIDC, attestation scope, arbitrary tag
input, or candidate checkout/execution. GitHub's Contents permission is necessarily broader than a
release-only capability; the compensating boundary is the protected approval plus exact owner closure,
run-derived reserved tag, and static hostile-namespace/product-tag/NuGet/candidate-code/permission
fixtures. That is explicit rather than presented as token-level path restriction.

Each recovery attempt uses a unique tag containing both source Release and recovery run coordinates.
It cannot overwrite/adopt a failed predecessor, and successful immutable evidence must account for all
earlier states. This prevents a green recovery from hiding missing or quarantined bytes and preserves
the incident-monotonic ledger rule.

### Reviewer sidecars and packet approval projection

Each lens result is bound four ways: an authenticated producer run, a run-owned artifact/archive
digest, committed report and sidecar bytes equal to distinct artifact members, and a duplicate-rejecting
sidecar whose canonical header is embedded byte-identically in the report. The header binds lens,
spine, FrontComposer, Builds, policy, verdict/counts, and reviewer run; the sidecar separately binds the
finished report digest, so there is no report self-hash cycle. The equality map rejects mixed commits,
policies, callers, runs, artifacts, or hostile-fixture ancestry.

The evidence-only packet PR is authoritative only after an API-authenticated Release Owner review whose
exact canonical body binds the packet digest and all principal identity coordinates and covers the
unchanged head. A distinct protected-main projection PR records that server review without altering the
packet; validator/API equality plus the no-bypass, non-squash path rejects a collaborator-authored or
stale approval record. The second PR does not invent authority—it durably projects the authenticated
first review—so it introduces neither a trust bootstrap nor a hash cycle.

## Confirmed retained security properties

- Candidate-controlled restore/build/pack/release-planning executes only in the secretless, read-only
  builder; protected publisher, post-release, and recovery code never execute candidate/archive/package
  content.
- The protected publisher authenticates the bounded raw artifact and every digest before requesting an
  OIDC token or invoking a publication credential/write API.
- The attestation verifier binds GitHub issuer, source repository/ref/commit, direct reusable signer
  digest, GitHub-hosted runner, exact run/attempt, fixed SLSA workflow predicate shape, and the exact
  no-extra package subject set.
- Publication is honestly cross-service rather than falsely atomic: draft staging, version concurrency,
  immediate uniqueness recheck, immutable Release verification, NuGet repository-signature/member
  comparison, and incident-first precedence cover every observable side effect.
- The read-only verifier cannot preserve evidence or mutate the ledger; recovery and sign-off are
  separately approved, separately pinned authority paths.

## Finding counts

Critical: **0** · High: **0**.
