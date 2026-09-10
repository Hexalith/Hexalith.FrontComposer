# PRD Quality Review — Hexalith.FrontComposer

## Overall verdict

This is a substantively strong brownfield PRD: it has a specific product thesis, unusually concrete functional consequences, explicit delivered-versus-gated status, and a serious decision/open-item register. It is not yet safe as the milestone-closure instrument it claims to be, however, because three evidence-gate pass conditions permit closure without evidence or approval that other sections say is required; for this high-stakes chain-top use, the overall verdict is **fair pending gate repair**.

## Decision-readiness — thin

The PRD does most of the hard work well. Section 0.1 identifies one authoritative milestone table, separates evidence gates from approval gates, and assigns each row an owner, artifact, and current state; §12 and Addendum §6 also expose real trade-offs instead of smoothing them away. The decisive weakness is that the authoritative pass-condition wording does not consistently encode the decisions and evidence obligations stated elsewhere, so a literal gate operator can reach a different closure result from a reader of the full document.

### Findings

- **high** G-2 allows absence of its end-to-end proof to satisfy the pass-condition branch (§0.1 G-2) — The authoritative row requires the v4.2.0 run IDs to be cited “**or the proof is declared not yet produced**,” while the same row's state says it is open on “proof citation” and §9 SM-2a says the run citation is open. Declaring required proof absent cannot both satisfy and leave open the evidence gate. *Fix:* require the authenticated run IDs and proof, or define a separately owned, dated waiver approval with explicit residual risk; do not make “not produced” an evidence-pass alternative.
- **high** G-6's fallback can close the item without proving a substitute adopter (§0.1 G-6; §12.2 OI-5) — The main path requires dated, candidate-bound Tenants evidence, but the fallback only says Product chooses Parties or holds the milestone, and OI-5's unblock condition is “File exists, **or D-7 fallback decided**.” A choice of Parties therefore has no stated requirement for equivalent Parties bootstrap evidence even though the gate and §4 thesis require named adoption proof. *Fix:* make the fallback branch require the same dated/candidate-bound projection-and-command proof from Parties; only a decision to hold should close the decision without closing G-6.
- **high** G-7 omits the security acceptance that the PRD says is mandatory (§0.1 G-7; §12 D-14; §12.2 OI-2; Addendum §6) — G-7's pass condition and evidence artifact are limited to an audit test class, but D-14/OI-2 require a dated sign-off by a security reviewer distinct from the author for the catalog disclosure and credential/resource oracles. The addendum also lists “Gate `resources/list` ... before v1.0” as a rejected alternative while OI-2 still presents that story as a live alternative, so the supposedly open choice is pre-decided in supporting material. *Fix:* add the dated OI-2 sign-off (or the completed gated-list alternative) to G-7's pass condition and evidence column, and change Addendum §6 to mark the alternative pending rather than rejected until that decision is signed.
- **medium** D-13 is simultaneously accepted, proposed, and non-gating (§12 D-13; §12.1; §12.2 OI-4) — D-13 says RC pins “are accepted for the milestone,” §12.1 says the answer is “proposed, not decided,” and OI-4 has no gate. The milestone can therefore be classified while its named Product co-owner has not accepted the dependency posture. *Fix:* either record the Product decision before calling D-13 accepted, or state explicitly that Architecture owns an accepted time-bounded risk and remove Product confirmation as an open decision; if Product approval is required for readiness, add it to §0.1.

## Substance over theater — strong

The content is earned. The journeys drive concrete behaviors; NFRs carry product-specific bounds; the MCP and release sections state uncomfortable disclosures and known breaches; and the addendum holds mechanism and rejected-alternative rationale rather than padding the main narrative. Repetition is extensive, but most of it serves distinct status, requirement, metric, risk, and decision views rather than persona, innovation, or NFR theater.

## Strategic coherence — strong

The thesis in §1—describe the operational surface once and derive aligned human and AI access—is specific to FrontComposer, and §4 turns that into a two-part readiness bet: package-consumer safety plus named domain-module adoption. The feature groups serve that arc, while SM-1/SM-2/SM-2a/SM-4 test adoption and governance and SM-7/SM-8/SM-10 test operator trust; SM-C2/SM-C4/SM-C5 defend against plausible metric gaming. The explicit choice to accept one named adopter for this milestone is narrow but coherent, provided G-6's fallback is repaired as above.

## Done-ness clarity — adequate

Most FRs have testable consequences, and the strongest requirements give exact files, diagnostics, wire shapes, timing budgets, cardinality limits, or state transitions. Two broad umbrella requirements remain too dependent on undocumented interpretation for clean downstream story or regression extraction.

### Findings

- **medium** FR-3 does not define done for most of the vocabulary it claims (§5.1 FR-3) — “must support the documented vocabulary” names roughly seventeen attribute/metadata concepts, but the consequences specify behavior only for invalid usage, editable-field suppression, and accessible badge/status metadata. A passing diagnostic catalog does not establish the emitted/runtime semantics of projection roles, field groups, empty-state CTAs, defaults, templates, or command targets. *Fix:* add a compact vocabulary-to-observable-consequence table (or stable contract/test reference per concept), preserving FR-3 as the umbrella while making every listed capability verifiable.
- **medium** FR-23's synchronization claim has no content-level completion rule (§5.6 FR-23) — DocFX success and frontmatter/snippet validation can pass while docs are semantically stale; the requirement itself demonstrates this by recording that `docs/fluent-ui-v5-contingency.md` still cites rc.2. *Fix:* name the authoritative inventories and drift checks that prove component, diagnostic, migration-edge, and skill-corpus coverage, and require zero known stale public-doc items for the milestone or explicitly classify each exception outside FR-23's done baseline.

## Scope honesty — strong

The PRD is candid about delivered baseline, open evidence, approval-only gates, accepted residuals, and non-goals. It distinguishes document approval, governed publication, and milestone classification; puts the server-allocated-key and MCP timing omissions where readers will encounter them; and gives all eleven open items an owner and unblock condition. The active open-item density is appropriate because frontmatter remains `draft`, `product_approval` is pending, and milestone readiness is explicitly false.

### Findings

- **low** A4 does not round-trip through the inline assumption mechanism the PRD promises (§0 Document Purpose; §13 A4) — §0 says inline `[ASSUMPTION]` callouts are indexed, but A4 exists only in the index while its cited reliance points (§0.1 G-1 and FR-25) state the publication interpretation as fact. *Fix:* place `[ASSUMPTION A4: ...]` at the first relied-on claim, or recast A4 solely as an evidence uncertainty owned by G-1/OI-8 and remove it from the Assumptions Index.

## Downstream usability — adequate

The glossary is unusually useful, UJ-1 through UJ-6 each have named protagonists, status families are explicit, and functional/NFR/public-surface identifiers are stable and unique. The primary extraction hazard is that historical records still look syntactically like active requirements, compounded by a smaller path-precision issue in the success metrics.

### Findings

- **medium** Closed decision records remain imperative FRs that story generators can treat as work (§5.0 Table C; §5.7 FR-27 and FR-28) — Table C says FR-27/FR-28 are “closed decision records,” but their canonical headings remain under Features and Functional Requirements and their bodies say “FrontComposer preserves” and “delivery follows,” followed by consequence lists. A downstream extractor operating per-FR can reasonably generate new acceptance work from these tombstones. *Fix:* preserve the frozen IDs but label each heading/body explicitly `retired/closed — no implementation work`; move enduring behavioral invariants into active FRs and move history to §12/addendum.
- **low** Evidence references are not consistently self-resolving when sections are extracted (§9 SM-2, SM-7, SM-8, SM-10) — Examples include basename-only `rel-ai-1-release-evidence-ledger.md`, `tests/9-8-live-acceptance.md`, and `spec-11-2-projection-realtime-resilience.md`, whereas gate rows use canonical paths. *Fix:* use repository-root-relative paths consistently for every evidence artifact in §9.

## Shape fit — strong

The document fits a high-stakes chain-top brownfield technical product. It explicitly distinguishes the delivered baseline from the remaining engineering/evidence/approval work (§0.1, §5.0, §8), carries six named journeys because the shell has meaningful operator, adopter, maintainer, release, and AI-agent surfaces, inventories the public package/wire contracts, and moves mechanism and rejected alternatives to the addendum. The status map and stable IDs are the right shape for UX, architecture, and story handoff once the closed-FR labeling and gate semantics above are corrected.

## Mechanical notes

- FR headings are unique and cover FR-1 through FR-30; FR-19a and FR-29.1–FR-29.7 are intentional nested/frozen extensions, not accidental duplicates. UJ-1–UJ-6, G-1–G-7, D-1–D-15, and OI-1–OI-11 are present and unique in their defining sections.
- SM identifiers are unique but intentionally presented by primary/secondary priority rather than numeric order; §9 explains the frozen SM-5/SM-6 placement and retired SM-C1/SM-C3 identifiers.
- A3 round-trips between its inline tag and §13. A4 is index-only as noted above; A1/A2 are explicitly retired historical assumptions.
- Every user journey has a named protagonist carrying role context inline. No material glossary synonym drift was found across the reviewed PRD and addendum.
