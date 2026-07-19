# Final Epics Reconciliation — Ratified GOV-1

**Target:** `_bmad-output/planning-artifacts/epics.md`
**Line references:** current file before reconciliation
**Result:** the following passages must change; no other epic/story section contains GOV-1 boundary or
release-provenance language requiring reconciliation.

## Required Changes

### 1. FR-24 coverage terminology — line 199

Replace `the exact reachable dependency-graph provenance correction` with
`the complete defined-v1 depth-1/2 dependency-graph provenance correction`.

### 2. GOV-1 roadmap update and source of record — lines 214-221

Replace the whole update. It currently repeats the superseded “every reachable” / “complete reachable”
contract and points only to the internally conflicting sprint proposal. The replacement must state:

- `hexalith.dependency-graph.v1` contains every FrontComposer-root gitlink at depth 1 and every gitlink
  in each exact root-selected commit at depth 2; deeper edges are excluded and require a new schema;
- catalog compatibility applies to every Builds selector in that complete defined-v1 graph and does not
  use a historical Builds-commit or catalog-fingerprint allowlist;
- pointer CI uses immutable base/before policy, explicit event revisions, affected-module proof, and an
  authenticated versioned CI handoff; release v2 seals graph, policy, and actual CI/release
  workflow/action provenance;
- Architect + Release Owner ratification resolved the implementation entry gate; the 8 + 32 census is
  evidence, not an acceptance constant;
- the ratified FC-DEP-1 decision and focused GOV-1 architecture spine are the sources of record; the
  sprint proposal remains historical input only.

### 3. Superseded release split — lines 223-232

Mark this as a **historical REL-2 update superseded for FR-24 authorization** by the later REL-3/GOV-1
rules. Its current present-tense statement that the supplemental `release-evidence.yml` owns signing,
manifest preparation, classification, and evidence conflicts with the ratified flow. Replace that
present-tense split with: primary pinned CI produces the authenticated handoff; the pinned reusable
release performs pre-publication preparation/signing/sealing/live verification/classification for the
exact `workflow_run.head_sha`; `release-evidence.yml` remains an independent read-only
post-publication verifier and cannot authorize or reseal.

### 4. GOV-1 status and resolved implementation gate — lines 2207-2210

Change `Status: backlog` to `Status: ready-for-dev` and add an explicit gate-resolution line naming
Architect + Release Owner ratification of the depth-1/2 v1 boundary. Keep the priority. Add the focused
spine beside FC-DEP-1 as the binding implementation decision, and state that deeper traversal is a
separately approved future schema rather than unresolved GOV-1 work.

### 5. Graph/semantic acceptance — lines 2216-2227

Replace the first and third acceptance scenarios so they use the ratified model:

- collection reads exact committed objects for all depth-1 and depth-2 edges, records self/back-reference
  edges, and does not traverse below depth 2; fixed depth, not “complete reachable ... with cycle
  detection,” guarantees termination;
- distinct Builds blob work may cache by `(repository, commit)`, but every selecting owner is evaluated
  through its required named semantic profile and remains in evidence;
- remove the overly broad `without any expected 40-hex SHA literal`; say specifically that semantic
  compatibility contains no historical Builds-commit or catalog-fingerprint allowlist, because approved
  workflow/action provenance intentionally uses full 40-hex pins;
- failure diagnostics identify owner repository/commit/path, selected Builds repository/commit/catalog,
  and the precise semantic mismatch.

### 6. CI revision and affected-module acceptance — lines 2229-2232

Replace `compares the merge base with the candidate head` with the exact event rules:

- PR: `event_base = github.event.pull_request.base.sha`, candidate = `github.sha` (the CI merge
  revision), require `git merge-base(event_base, candidate) == event_base`, and collect/build that same
  candidate;
- push: compare `github.event.before` with `github.sha`; zero/unavailable before is full-affected,
  fail-closed, and non-release-eligible;
- classify depth-1 changes first, let changed/removed depth-1 edges subsume descendant depth-2 churn,
  then map remaining depth-2 changes to a surviving candidate owner or FrontComposer root;
- run each closed-policy build disposition once from the exact candidate commit with its bounded exact
  Builds regular-file contract tree and static Release/NuGet argv, without nested initialization.

### 7. Insert immutable policy and authenticated handoff acceptance after line 2232

Add two scenarios; these are absent from the epic despite being release-authorizing GOV-1 invariants:

1. The same policy blob from PR `event_base` or non-zero push `before` governs both graphs; its
   repository/commit/schema/raw SHA-256 are evidence; a candidate policy activates only from a later
   base. The one-time no-base bootstrap requires unchanged graph, frozen publication, the exact
   `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256`, and Architect + Release Owner approval.
2. A successful `push`/`main` primary CI run uses literal 40-hex reusable workflow/action closure and
   emits the single closed `hexalith.dependency-release-handoff.v1` artifact. Release authenticates the
   run ID/attempt, workflow/event/branch/conclusion/head SHA through the Actions API, requires the
   artifact candidate to equal `workflow_run.head_sha`, verifies the CI-only evaluator digest and raw
   handoff hash, reloads the exact recorded policy blob, and checks out/publishes only that head SHA.
   Mutable CI/release `@main` evaluator references remain non-conforming and cannot lift REL-4.

### 8. Release-manifest acceptance — lines 2234-2238

Replace `complete reachable dependency graph` with `complete defined-v1 dependency graph` and remove
`cycled-without-termination` as a failure class: self/back-reference edges are valid recorded evidence
inside the fixed boundary. Require the atomic v2 manifest members `dependency_graph`,
`dependency_policy`, and `workflow_provenance`; the latter must project the authenticated handoff
run/evaluator/raw hash and actual release caller/reusable/action sources. State that the outer seal and
fallback digest bind graph, policy, and the exact combined CI/release workflow-definition digest;
offline verification rejects schema/order/digest/projection defects, live verification reconstructs the
same exact root graph/policy, and legacy manifests are audit-only and never publishable or
fallback-eligible.
