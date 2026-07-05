# PRD Quality Review — Hexalith.FrontComposer Product Requirements Document

## Overall verdict
This is a high-substance PRD that is broadly usable for a brownfield v1.0/package-readiness effort: the product thesis is specific, the scope is honest, user journeys are tied to real actors, and the decision register surfaces several true gates. The main risk is downstream execution: some release-critical requirements still rely on external artifacts, "configured" values, or bundled remediation lists instead of PRD-carried acceptance bounds. It is adequate to proceed into architecture/story work if the high-severity done-ness gaps are tightened before treating the PRD as a build contract.

## Decision-readiness — adequate
The PRD makes several decisions visible instead of smoothing them over. The Requirement Status Map (§5.0) marks FR-13, FR-24, FR-26, FR-28, and FR-29 as readiness or v1 gates, and the Decision And Gate Register (§12) explicitly leaves the generated command route family, FC-NIP payload source, and Contracts kernel split open. Those are real trade-offs, not template theater.

The weakness is that some decisions remain buried inside functional requirements rather than promoted into the register that decision-makers will scan. FR-27 and FR-29 both contain release-impacting choices, but §12 does not give every such choice the same owner/default/blocker treatment as D-3 to D-5. A decision-maker can act, but only after stitching together several sections.

### Findings
- **[medium]** Migration-emission decision is buried (§5.7 FR-27, §12) — FR-27 requires "HFCM9002 migration-emission behavior" to be decided and documented before release, but §12 has no matching decision ID, owner, default, or blocker. *Fix:* Add a D-9 entry for HFCM9002 migration-emission behavior with owner, current/default state, unblock condition, and affected stories or release gates.
- **[medium]** Architecture remediation decisions are rolled up too tightly (§5.7 FR-29, §10, §12) — FR-29 bundles token lifecycle, realtime resilience, MCP lifecycle, security-validation tests, visual-conformance guards, Testing harness failure modes, shell layering, helper consolidation, logging, and enforcement-policy alignment under one requirement. §12 only exposes route-contract and Contracts-split gates, so decision-makers cannot see which remediation items are release blockers versus quality backlog. *Fix:* Add a remediation gate table with one row per defect class, release-blocking classification, owner, and decision/exit criteria.

## Substance over theater — strong
The PRD earns its structure. The Vision (§1) is specific to FrontComposer's bet that "domain teams should describe their operational surface once, in code"; the user journeys (§2.4) are concrete and named; and the glossary (§3) carries domain terms that appear throughout the FRs and NFRs. The document does not pad itself with generic personas or generic innovation claims.

The NFRs (§6) are also mostly product-specific: schema determinism, fail-closed MCP security, generated-output drift, Fluent UI governance, release evidence, and command lifecycle truthfulness all reflect actual FrontComposer risks. This reads as a release-readiness product contract, not a filled template.

### Findings
- None.

## Strategic coherence — strong
The strategic thesis is clear: FrontComposer should make annotated domain types the source of truth for generated human UI, MCP agent access, lifecycle state, tooling, and release evidence (§1). The feature groups follow that thesis in order: source generation (§5.1), shell adoption (§5.2), projection operations (§5.3), command lifecycle (§5.4), MCP (§5.5), tooling/testing (§5.6), and package/brownfield remediation (§5.7).

The success metrics (§9) mostly validate the thesis rather than measuring vanity activity. SM-1 tests adopter bootstrap, SM-2 tests release readiness, SM-3 tests drift visibility, SM-4 tests MCP fail-closed coverage, and the counter-metrics explicitly reject generated file count, visual polish, and CLI output volume as false wins.

### Findings
- **[low]** Representative adopter proof is slightly soft (§9 SM-1) — SM-1 requires "at least one representative Hexalith adopter module, preferably Tenants," which leaves the core adoption proof open to a weaker unnamed substitute. *Fix:* Either require Tenants explicitly or define the objective substitute criteria that make another adopter module representative enough.

## Done-ness clarity — adequate
Most FRs are written with testable consequences, and many are strong: invalid projections fail with HFC1003 (§5.1 FR-1), command density thresholds are numeric (§5.1 FR-4), startup throws when MCP gates are missing (§5.5 FR-19), and release artifacts are enumerated (§5.7 FR-24). This is much better than a capability list.

The main gap is that some release-critical acceptance criteria are intentionally compressed or externalized. Phrases such as "within configured budgets" (§6 NFR-8), "existing benchmark thresholds and cache caps" (§6 NFR-9), and broad remediation bundles (§5.7 FR-29) are acceptable as pointers during planning, but they are not sufficient as a build contract unless the exact bounds and verification artifacts are attached or named.

### Findings
- **[high]** Release-critical NFR bounds are implicit (§6 NFR-8, NFR-9, NFR-11) — NFR-8 requires degraded/reconnecting/fallback states within "configured budgets," NFR-9 relies on "existing benchmark thresholds and cache caps," and NFR-11 invokes test lanes "required by the changed surface." Story authors cannot derive exact pass/fail criteria from the PRD alone. *Fix:* Add a threshold table or explicit references naming the budget values, benchmark IDs, cache caps, and mandatory test lanes for v1.0.
- **[high]** FR-29 is not decomposed enough for acceptance (§5.7 FR-29) — The requirement says several architecture risks "each have focused stories or gates," but it does not state the minimal acceptable outcome for each risk area. This is risky because FR-29 is release-readiness work, not optional cleanup. *Fix:* Split FR-29 into sub-requirements or a table with risk area, v1 minimum outcome, verification artifact, owner/story, and release-blocking status.
- **[medium]** Documentation done-ness is mostly validation-gate based (§5.6 FR-23) — FR-23 says component, diagnostic, migration, and skill-corpus docs must stay synchronized, but the consequences only name DocFX validation, skill front matter, and scratch-doc placement. Those gates can pass while public-surface coverage remains incomplete. *Fix:* Add a minimum documentation inventory, such as every public HFC diagnostic, CLI schema, attribute vocabulary item, package, migration edge, and MCP descriptor class that must have docs before release.

## Scope honesty — strong
The PRD is unusually direct about what is in and out. Non-users (§2.3), Product Form Factor (§4), MVP/V1 scope (§8), Out Of Scope (§8.3), and the Assumptions Index (§13) all constrain the reader's expectations. It also refuses to pretend unresolved work is solved: FC-NIP row identity, route contracts, Contracts split posture, release evidence, and architecture remediation remain visible as gates (§5.0, §12).

The remaining issue is not denial; it is scanability. The addendum says the post-correction readiness report still marked readiness `NEEDS_WORK`, while the PRD distributes active blockers across the status map, scope section, requirements, risks, and decision register. That is honest, but a release owner should not have to reconcile the blocker inventory manually.

### Findings
- **[medium]** V1 release blocker inventory is distributed (§5.0, §8.2, §12, addendum §Reconciliation Notes) — The PRD names active gates in several places, but does not provide one release-blocker view for v1.0. *Fix:* Add a compact "V1.0 release blockers" table listing blocker ID, source FR/D ID, owner, unblock condition, and whether it blocks story creation, implementation start, or publication.

## Downstream usability — adequate
The document is generally friendly to downstream UX, architecture, and story workflows. FR, UJ, NFR, SM, D, and assumption IDs are stable and scan well; each UJ has a named protagonist (§2.4); the glossary (§3) makes domain nouns source-extractable; and the addendum preserves source inventory and technical detail boundaries without bloating the PRD.

The main downstream risk is artifact routing. §0 and D-1 distinguish a readiness-discoverable canonical copy from the BMad run copy, while the addendum lists `_bmad-output/planning-artifacts/prd.md` among source artifacts. If those copies diverge, automation and human reviewers can extract from different PRDs and both think they are right.

### Findings
- **[medium]** Canonical PRD path duplication needs an anti-drift rule (§0, §12 D-1, addendum §Source Inventory) — The PRD declares `_bmad-output/planning-artifacts/prd.md` as canonical and this workspace copy as the BMad run copy, but it does not say how they stay synchronized. *Fix:* Add an explicit mirror/sync rule, checksum note, or owner responsibility; alternatively make only one path canonical and mark the other as generated output.
- **[low]** Addendum source inventory is self-referential (addendum §Source Inventory) — The addendum lists `review-rubric.md` and `validation-report.md` under "PRD validation artifacts" in a section introduced as sources used to draft/update the PRD. Those are outputs, not source inputs, and can create validation loops for future extractors. *Fix:* Move them to a separate "Generated validation artifacts" subsection or remove them from source inventory.

## Shape fit — strong
The shape fits the product. FrontComposer is a brownfield developer framework and package surface, not a consumer app or generic SaaS, so the PRD correctly emphasizes public contracts, package evidence, generated artifacts, MCP security, source-generation governance, and downstream story readiness. The UJs are useful because the product has multiple operator/developer/agent-facing surfaces, but they do not crowd out the capability specification.

The addendum also fits: it holds source inventory and technical detail that would make the PRD heavier without improving its decision value. For a chain-top PRD feeding UX, architecture, and stories, this is the right level of structure.

### Findings
- None.

## Mechanical notes
- ID continuity looks clean: UJ-1 through UJ-6, FR-1 through FR-29, NFR-1 through NFR-12, SM-1 through SM-6 plus SM-C1 through SM-C3, D-1 through D-8, and A1 through A2 are contiguous and unique.
- Assumptions Index roundtrip is intact: A1 appears inline in §2.4 and §5.6 and is indexed in §13; A2 appears inline in §4 and is indexed in §13.
- Glossary usage is mostly stable. "FrontComposer Shell" and "Shell" are used interchangeably after definition, but not in a way that blocks understanding.
- The addendum's validation artifact entries should be treated as generated outputs, not as source inputs for future reconciliation.
