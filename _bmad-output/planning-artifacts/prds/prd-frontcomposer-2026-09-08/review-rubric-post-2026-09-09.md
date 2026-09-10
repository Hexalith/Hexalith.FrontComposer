# PRD Quality Review — Hexalith.FrontComposer

## Overall verdict

**Adequate — changes required before the G-4 reviewer gate can pass.** The post-update PRD has a clear dual product thesis, unusually explicit release/security trade-offs, test-oriented UX outcomes, named owners, and an honest milestone gate model. Two internal status contradictions still make approval and downstream story extraction unsafe: G-4 does not consistently account for OI-4, and several newly expanded UX/testing obligations are simultaneously labeled delivered and unmet.

## Decision-readiness — adequate

The decision and gate system is substantially actionable. Section 0.1 separates evidence gates from approval gates, names owners and required artifacts, and refuses to equate protected-environment approval with publication safety. Sections 10 and 12 also preserve the real tensions around candidate-controlled code, MCP disclosure, EventStore provenance, the Fluent RC posture, and the interim release posture rather than smoothing them into generic risks.

One approval dependency is internally inconsistent, however. A Product Owner cannot determine from the current text whether resolving the Fluent RC posture is a prerequisite for PRD re-approval.

### Findings

- **high** G-4 has two incompatible closure definitions (§0.1 G-4; §12 D-13; §12.2 OI-4) — G-4 says re-approval waits for OI-16 and a clean post-update reviewer gate, while D-13 says the Fluent RC decision blocks G-4 through OI-4 and the OI-4 row shows no gate. This creates two valid but different readings of the milestone authority. *Fix:* either add OI-4/D-13 explicitly to G-4's pass condition and OI-4's Gate cell, or state that the RC decision is non-blocking and remove the D-13 claim that it blocks G-4.

## Substance over theater — strong

The content is earned by the product's actual concerns. User roles connect to distinct requirements and journeys; UX requirements name concrete focus, reflow, lifecycle, announcement, and recovery behavior; release and MCP requirements expose exact residual risks rather than hiding them behind generic “secure” or “reliable” language. The addendum carries mechanism and rejected-alternative rationale that would otherwise swamp the main product narrative.

### Findings

No substantive finding.

## Strategic coherence — strong

The PRD states a coherent bet: one annotated domain surface should produce aligned human, agent, and tooling experiences. The dual readiness thesis—safe framework consumption and trustworthy operator outcomes—is carried through the journeys, feature groups, milestone gates, and primary metrics. SM-1/2/2a/4/7/8/9/10 test the stated bets, while SM-C2/C4/C5 guard against substituting polish, workflow greenness, or bookkeeping for actual trust.

### Findings

No substantive finding.

## Done-ness clarity — adequate

Most FRs include observable consequences, and the amended UX and release requirements are much more testable than adjective-led NFRs: timings, retry limits, focus destinations, announcement rules, request shapes, package identities, and evidence artifacts are usually explicit. The document is also candid when evidence is absent.

The main weakness is not missing detail but contradictory delivery classification. A downstream story workflow cannot reliably tell whether the 2026-09-09 UX delta is a delivered regression baseline, an evidence-only gap, or implementation work.

### Findings

- **high** Updated UX/testing consequences are labeled both delivered and unmet (§5.0 Tables A/B; FR-8, FR-10–FR-16, FR-22, FR-23; §9 SM-5/SM-6; §12.2 OI-16) — Table B declares FR-1–FR-12, FR-14–FR-19a, and FR-20–FR-23 delivered regression baselines, yet FR-22 now promises helpers that verify the amended focus/validation/announcement contract, SM-5 says that assertion expansion is still OI-16, and SM-6 says the added contract is unmet. Table A describes the UX work as approval/evidence-only even though OI-16 can entail Framework-maintainer implementation. *Fix:* split the amended consequences into “delivered behavior,” “required evidence,” and “implementation delta,” then move every open implementation-bearing FR consequence into Table A (or a fourth explicit status group) with its closing story/evidence.
- **medium** FR-3's acceptance surface is too aggregated for its breadth (§5.1 FR-3) — the requirement names projection roles, badges, field groups, policies, derived fields, localization/display metadata, templates, and command targets, but its consequences group many behaviors into two long sentences without a per-vocabulary-item observable outcome or an authoritative contract-table link. That is weaker than the surrounding FRs and makes regression completeness hard to judge. *Fix:* link a versioned vocabulary/behavior matrix as the executable acceptance authority, or give each supported attribute family a compact expected-output and invalid-input consequence.

## Scope honesty — strong

The PRD is explicit about non-users, milestone exclusions, server-allocated-key limitations, timing-side-channel exclusions, stale downstream documents, current nonconforming release execution, and pending approvals. The open-item density is high but appropriate for a chain-top, security-sensitive readiness document, and each item has an owner and unblock condition. The single live assumption, A3, round-trips to the Assumptions Index and an open confirmation item.

### Findings

No substantive finding.

## Downstream usability — adequate

The glossary is strong, journeys have named protagonists, identifiers are stable, FRs are feature-grouped, and the public-surface section gives architecture and story workflows useful extraction anchors. The explicit distinction among delivery, evidence, approval, and closed decisions is valuable, subject to the status conflict above.

Audit dating and one addendum routing label need correction because downstream tools are likely to extract them literally.

### Findings

- **medium** The milestone-gate state columns carry the wrong as-of date (§0.1) — both tables say “State on 2026-09-08,” but G-8 and several row states incorporate the 2026-09-09 validation, while frontmatter says `updated: 2026-09-09`. In an evidence-driven release PRD, this makes the status snapshot provenance ambiguous. *Fix:* change both column headings to 2026-09-09, or give each row its own last-observed date if states were gathered at different times.
- **low** The addendum's UX routing label omits most requirements it now supports (Addendum §4) — the heading says it supports only FR-8/FR-9/FR-10/FR-11, while §§4.1–4.4 directly define acceptance material for FR-12–FR-16, FR-22, FR-23, NFR-3, and SM-6. *Fix:* expand the heading's support range so source-extraction workflows do not skip the addendum for those requirements.

## Shape fit — strong

This is appropriately shaped as a high-stakes brownfield framework PRD that feeds Product, UX, architecture, and stories. The six journeys are justified by the distinct adopter, operator, agent, maintainer, and test-harness experiences; they are not persona decoration. Exact security and release-boundary constraints belong in the main PRD because they define acceptable outcomes, while lower-level canonicalization, ledger, dependency-graph, and UX mechanism detail is pushed into the addendum.

### Findings

No substantive finding.

## Mechanical notes

- UJ-1 through UJ-6 are present and each has a named protagonist; Marc's two journeys intentionally cover projection investigation and command execution.
- FR-1 through FR-30 are present with the explicitly stable nested identifiers FR-19a and FR-29.1–FR-29.7. NFR-1 through NFR-13, D-1 through D-16, G-1 through G-8, and OI-1 through OI-17 are present without duplicate definitions.
- SM numbering is intentionally frozen; the document explains SM-2a and the retirement of SM-C1/SM-C3, so the apparent sequence gaps are not defects.
- The sole inline assumption, A3, appears in §13 with the same disposition and OI-9 closure path; there are no orphaned assumption-index entries.
- Severity count: **0 critical, 2 high, 2 medium, 1 low**.
