# Adversarial Postfix Review — 2026-09-09

Reviewed snapshot:

- PRD: `_bmad-output/planning-artifacts/prd.md`, SHA-256 `f9013d795332f7d259d01b03a32d68129bb1ff78644f81968f887f3ff93596b8`
- Addendum: `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`, SHA-256 `c46c0deb0f2726bece8a3297d069f8881f4b9309e8c832345a3585616d289811`
- Prior review: `review-adversarial-post-2026-09-09.md`
- Architecture authority checked for the adopted target: `architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`, including adopted AD-17/AD-19 and the current halt/no-exception posture.

Critique only; the PRD and addendum were not edited.

## Verdict

**Send back, narrowly.** All six prior Critical/High findings are substantively repaired in the main PRD: D-13/OI-4 now gates G-4; Governed Release means the AD-19 split; the A1–A5 collision is gone; G-7 always requires independent security disposition; G-3 has an explicit ownership-transfer path; and UX closure now excludes both Critical and High findings. The live reconciliation also correctly adopts manifest v4 and the production halt.

No Critical finding remains. Four High inconsistencies still allow the final gate or downstream implementation to read a weaker contract than AD-19: one impossible “exact published bytes” manifest statement, a reviewer-evidence selector that excludes this postfix verification and is not byte-bound, missing canonical-architecture reconciliation from G-8's pass condition, and stale addendum text that again makes gated-list implementation a substitute for security acceptance.

Counts: **0 critical, 4 high, 3 medium, 1 low**.

## Prior-Finding Verification

| Prior finding | Result in reviewed snapshot |
| --- | --- |
| ADV-P09-01 — D-13 could remain open outside §0.1 | **Resolved.** G-4 requires OI-4; OI-4 maps to G-4. |
| ADV-P09-02 — old controls still defined Governed Release | **Resolved.** Glossary now requires the D-16/G-8 secretless-builder/candidate-free-publisher target. |
| ADV-P09-03 — GOV-1 A1–A5 collided with PRD assumptions | **Resolved.** G-8 and OI-13 now reference adopted AD-19 directly. |
| ADV-P09-04 — stale review selector and Critical-permitting UX condition | **Partly resolved.** The date and Critical/High condition are fixed; the current postfix review is still excluded and no immutable PRD digest is required (H2 below). |
| ADV-P09-05 — gated-list work could replace all security disposition | **Resolved in PRD, contradicted in addendum.** G-7/OI-2 are correct; Addendum §6 is stale (H4 below). |
| ADV-P09-06 — EventStore approver could be inferred away | **Resolved.** G-3/D-12/OI-18 require a separate dated ownership transfer. |
| ADV-P09-07/08 — absent matrices and ambiguous three-file UX authority | **Resolved.** The addendum says it carries schemas/obligations, OI-16 owns complete matrices, and D-8 names the exact three paths. |
| ADV-P09-09 — FR-23 falsely wholly delivered | **Resolved.** §5.0 splits the baseline from the gated semantic-documentation/UX deltas, and OI-10 gates G-4. |
| ADV-P09-10 — stale gate snapshot date | **Resolved in the PRD.** Both tables now say 2026-09-09. |

## Findings

### Critical

None.

### High

- **[high] ADV-PF09-01 — FR-24 still says a pre-publication manifest binds “exact published bytes”** (`prd.md:470-472`, `:556`; addendum `:20-21`; GOV-1 spine AD-17/AD-19) — The protected publisher seals manifest v4 and requires authorization before any NuGet publication, so it cannot bind the later NuGet.org-signed archive bytes. The same PRD correctly says GitHub candidate assets are byte-identical while NuGet may add root `.signature.p7s` and is compared by repository signature plus normalized-member equivalence. “Exact published bytes” reintroduces the raw-equality contract that AD-19 removed and makes the pre-publication seal circular or impossible. *Fix:* State that manifest v4 binds the exact authenticated candidate/GitHub asset bytes and the signed-NuGet equivalence rule; post-publication verification records the actual downloaded signed package and proves that equivalence without rewriting the sealed authorization.

- **[high] ADV-PF09-02 — G-4 is not bound to the bytes reviewed and excludes the final postfix review** (`prd.md:7`, `:41`; run-folder review filenames) — G-4 cites only the calendar `updated` value and `review-*-post-2026-09-09.md`. Multiple materially different PRD/addendum revisions share that date. The selector matches the earlier post reviews containing Critical/High findings, but it does not match this requested `review-adversarial-postfix-2026-09-09.md`; no cited disposition report proves which old findings were resolved against which bytes. Product can therefore approve later same-day bytes or ignore this final adverse verification while satisfying the literal citation list. *Fix:* Gate D-9 on one explicit synthesized postfix report that records SHA-256 for both PRD and addendum and incorporates every selected postfix reviewer; require a fresh review whenever either digest changes.

- **[high] ADV-PF09-03 — G-8 can close while the source declared canonical by D-2 remains contradictory** (`prd.md:17`, `:24`, `:35`, `:676`, `:690`, `:720`; addendum `:10`, `:45`; GOV-1 spine `:60-63`, `:927-938`) — The G-8 state and OI-13 correctly admit that canonical `architecture.md` still needs AD-19 propagation, but G-8's authoritative pass condition requires acceptance of the spine, implementation, and a reviewer pass without requiring OI-13 or canonical-source convergence. Because §0.1 says its table is the only milestone authority, the state note and an OI-to-gate mapping are weaker than a pass condition. A closer can satisfy the literal G-8 predicate while D-2's canonical architecture still specifies the superseded same-job mechanism. *Fix:* Add OI-13 closure to G-8's pass condition and evidence: canonical `architecture.md` plus named companion contract/story/request must agree with the adopted spine before implementation acceptance or gate closure.

- **[high] ADV-PF09-04 — The addendum again permits implementation to replace independent MCP risk acceptance** (`prd.md:34`, `:709`; addendum `:151`) — G-7 and OI-2 now correctly require a dated independent disposition after final behavior is known, and explicitly say gated-list evidence cannot substitute for it. Addendum §6 still says “OI-2 requires an independent dated security disposition **or gated-list implementation**.” As the addendum is the PRD's named mechanism/rationale companion, a security gate operator can select the weaker branch from it. *Fix:* Replace `or` with the PRD rule: gated-list implementation may change or eliminate resource residuals, but a distinct reviewer must still disposition every residual behavior that remains.

### Medium

- **[medium] ADV-PF09-05 — The historical lineage anchor is still labelled as the accepted split contract** (`prd.md:31`, `:475`, `:685`, `:696`; addendum `:35-46`; GOV-1 spine AD-16 `:610-631`, AD-19 `:765-766`) — `a8a50859…` is the 2026-08-08 owner-accepted lineage anchor; AD-19 requires a new owner-accepted Builds revision implementing the split before caller selection. The PRD repeatedly calls `a8a50859…` the “owner-accepted BUILD-REL-1 revision,” while the addendum also says the owner-accepted split revision remains open. *Fix:* Label `a8a50859…` as the accepted lineage anchor/predecessor and reserve “owner-accepted split reusable revision” for the future exact commit required by G-8/OI-14.

- **[medium] ADV-PF09-06 — The only-authority statement ignores explicitly open non-gating items** (`prd.md:24`, `:713`, `:716`, `:718`) — §0.1 says anything outside the table is a regression baseline or closed record, yet OI-6, OI-9, and OI-11 are open with `Gate: —`. Some may intentionally be non-gating, but the document never names that third category. *Fix:* Say that non-gating residuals/assumptions remain in §12.2 and cannot change milestone truth, then give each a revisit condition; otherwise promote any item needed for the milestone into a gate.

- **[medium] ADV-PF09-07 — D-9 and the Table-A approval summary omit two prerequisites now enforced by G-4** (`prd.md:41`, `:151`, `:683`, `:711`, `:717`, `:723`) — G-4 requires OI-4, OI-10, and OI-16, while D-9 and the Table-A D-9 row mention only OI-16 plus reviewer closure. The authoritative gate prevents false milestone closure, but the decision record and status extract can still trigger premature `product_approval`. *Fix:* Mirror all three G-4 prerequisites in D-9 and the Table-A approval row.

### Low

- **[low] ADV-PF09-08 — Gate evidence labels remain non-self-resolving** (`prd.md:31-35`, `:41`) — `release.yml`, “nonconformance register,” “authenticated run IDs,” and generic reviewer-gate labels are acceptable while open but not as final evidence. *Fix:* Before closure, replace each label with an exact root-relative artifact path or immutable authenticated coordinate.

## Closure Assessment

- Release authorization is now correctly assigned exclusively to the candidate-free publisher; current production is explicitly halted and no exception is authorized.
- UX requirements are testable enough for story extraction, while OI-16 honestly keeps the canonical matrices, source repair, and evidence open.
- G-1, G-2, G-3, G-4, G-5, G-6, G-7, and G-8 must remain open. The four High findings above prevent this review from satisfying G-4's clean-review condition.

---

## Final Snapshot Resolution — 2026-09-09

This section preserves the original postfix review above as an immutable audit snapshot and evaluates the later reconciled documents separately.

### Reviewed snapshot

- Canonical PRD SHA-256: `4bd96544f1225e21896033465da4735a0df53d482442f850daebcf42e89e156c`
- Addendum SHA-256: `017ab349327be6159b5b0edb49fe6fcc66b85a385ce87089954422b0565085a6`

### Verdict

**Pass at the Critical/High document-quality threshold: 0 unresolved Critical, 0 unresolved High.** This verdict supersedes the severity conclusion only for the reviewed hashes above; it does not retroactively change the original snapshot or close any product gate whose external evidence remains outstanding.

### Resolution verification

| Prior finding / requested repair | Final-snapshot disposition |
| --- | --- |
| PF09-01 — published-byte ambiguity | Resolved. FR-24 now binds the exact authenticated candidate and GitHub asset bytes, while NuGet is checked by package signature plus normalized-member equivalence; the later signed NuGet record does not rewrite the pre-publication seal. |
| PF09-02 — G-4 approval not digest-bound | Resolved. G-4 now requires Product approval of the exact PRD/addendum digest pair, names the synthesized postfix report, and requires a fresh report after either digest changes. |
| PF09-03 — G-8 did not require canonical dependency convergence | Resolved. G-8 and OI-13 now make convergence mandatory across the architecture, governance contract, GOV-1 story, and G-2 request before acceptance. |
| PF09-04 — independent OI-2 acceptance could be substituted | Resolved. Both PRD and addendum now require the independent Security disposition unconditionally; implementation evidence cannot substitute for it. |
| PF09-05 — predecessor mistaken for accepted split contract | Resolved. The documents consistently identify `a8a50859…` as a lineage anchor/predecessor and distinguish it from the future owner-accepted reusable split revision. |
| PF09-06 — every claim forced into owner/evidence closure | Resolved. Section 0.1 now explicitly permits a non-gating residual/assumption category in Section 12.2. |
| PF09-07 — D-9/Table A prerequisite mismatch | Resolved. D-9 and Table A now mirror OI-4, OI-10, OI-16, OI-19, and the digest-bound G-4 acceptance path. |
| FR-23 / OI-19 precision | Resolved at document level. FR-23 now distinguishes current partial status-map coverage, the full public component surface, CLI-executable migration edges, and manual-only guides. OI-19 requires four-source convergence, explicit migration classification, complete parity tests, and no unclassified items, and G-4 explicitly depends on that closure. |

### Remaining Critical/High findings

None. G-1 through G-8 remain open where required evidence or owner acceptance has not yet been produced; the reviewed text now represents those states as open prerequisites rather than as completed authorization. No exploitable Critical/High contradiction remains between the canonical PRD and addendum for the areas reviewed.
