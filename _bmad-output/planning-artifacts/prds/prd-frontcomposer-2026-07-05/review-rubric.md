# PRD Quality Review — Hexalith.FrontComposer

## Overall verdict

This is a credible draft PRD for a complex brownfield developer framework: the product bet is concrete, the scope is honest, and most requirements are specific to FrontComposer rather than template filler. It is not yet green-light ready for v1.0 story creation because several release-shaping decisions remain open and the requirements do not consistently distinguish existing baseline from new remediation or v1.0-blocking work.

## Decision-readiness — thin

The PRD makes the main thesis explicit in §1: FrontComposer turns annotated domain types into aligned human UI, MCP surface, and tooling, with v1.0 readiness focused on package-consumer safety. It also names real trade-offs and risks in §8, §10, §12, and §13 instead of smoothing over them.

The weakness is that the open decisions are phase-shaping, not minor. §12 asks whether this PRD is canonical, whether discovery paths should change, whether Epic 11.7 becomes a pre-epic gate, what route family is approved, what FC-NIP payload source is approved, whether the Contracts kernel split ships before v1.0, whether success metrics need quantitative targets, and whether a standalone UX spec is required. Those are exactly the decisions architecture, UX, and story workflows need before treating this as a build contract.

### Findings

- **high** Phase-blocking decisions are listed but not triaged (§12, lines 447-454) — The open questions include route-contract approval, FC-NIP payload source, Contracts kernel split, UX-spec need, and canonical-artifact status, but they have no owner, decision criterion, default, or deadline. *Fix:* Convert each phase-blocking question into a `[NOTE FOR PM]` or decision table with owner, required input, default if unresolved, and whether it blocks UX, architecture, or story creation.
- **medium** Release-readiness assumptions are doing decision work (§13, lines 460-462) — A3, A4, and A5 state v1.0-blocking positions but remain assumptions. That makes it unclear whether Product has accepted them or they are merely inferred from source documents. *Fix:* Promote accepted v1.0 blockers into explicit scope decisions, and leave only genuinely unconfirmed inferences in the Assumptions Index.

## Substance over theater — strong

The PRD is product-specific. The Vision names annotated domain types, source generation, schema fingerprints, governance tests, generated UI, MCP descriptors, and package-consumer safety. The user journeys are not persona decoration: Nina, Marc, Ravi, Camille, and Sophie each connect to concrete feature clusters and operational failure modes.

The NFRs also mostly avoid generic "secure/scalable/reliable" phrasing by naming FrontComposer-specific boundaries: fail-closed MCP security, server-controlled fields, Fluent v5 governance, schema determinism, public API baselines, signed packages, SBOM, and release evidence. A few NFRs still need measurable bounds, but the content is earned.

### Findings

- **medium** Some NFRs still use unbounded adjectives (§6, lines 366-369) — "degrade visibly", "bounded", "sanitized structured logs", and "release gates as applicable" are directionally correct but not yet testable enough for release readiness. *Fix:* Add target bounds or evidence requirements for reliability recovery, palette/generated UI performance, observability redaction, and which test lanes gate v1.0.

## Strategic coherence — adequate

The PRD has a coherent thesis: one annotated domain surface should generate aligned operations UI, command lifecycle behavior, MCP affordances, tooling, and tests. The feature groups in §5 follow that thesis from generator contracts through Shell, projection operations, command lifecycle, MCP, CLI/testing, and release quality.

The success metrics in §9 validate the thesis better than activity metrics would. They measure adopter bootstrap, release readiness, contract drift visibility, MCP fail-closed coverage, testing harness usefulness, and UX governance stability. The remaining issue is that most metrics are qualitative gates rather than concrete release targets.

### Findings

- **medium** Success metrics need release target values (§9, lines 411-419; §12, line 453) — SM-1 through SM-6 are directionally strong, but the PRD itself admits the quantitative targets are unresolved. *Fix:* Define the minimum passing evidence for each metric, such as named adopter module, required generated projection/command count, exact CI lanes, MCP negative-test cases, and Testing harness scenarios.

## Done-ness clarity — thin

Most FRs include concrete consequences, which is a good base for story creation. Several are directly verifiable, such as diagnostic IDs, generated file sets, fail-closed MCP gates, CLI schema names, dry-run behavior, signed package evidence, and public API baseline updates.

The thinness comes from the broadest requirements and NFRs. FR-26 bundles three epics and multiple architecture remediation streams into one requirement. §8.2 similarly names Epic 11 areas as a list rather than as individually testable outcomes. For a v1.0/package-release PRD, those are too large to tell engineering what "done" means.

### Findings

- **high** FR-26 is too broad to be acceptance-ready (§5.7, lines 347-355; §8.2, lines 392-396) — "Resolve post-MVP hardening backlog" covers FC-NIP, tooling-governance, evidence reconciliation, Testing redaction, token lifecycle, realtime resilience, MCP lifecycle, security tests, visual conformance, route contracts, package kernel split, shell layering, and convention alignment. *Fix:* Split FR-26 into separate readiness requirements with one verifiable outcome each, or move epic inventory into scope and put testable acceptance conditions under dedicated FRs.
- **medium** Several FR consequences rely on external labels instead of local criteria (§5.3, lines 202 and 220; §10, lines 429-433) — References such as "Epic 8 colored-icon status model", "Story 9.1", and "Epic 11 defects" are useful provenance but not self-contained acceptance criteria. *Fix:* Add a one-line local definition of the expected behavior, then keep the epic/story reference as provenance.

## Scope honesty — strong

This is one of the stronger parts of the draft. §2.3 names non-users, §8.3 names v1 exclusions, §12 lists open questions, §13 indexes assumptions, and the addendum explains that the PRD was reverse-engineered from planning and brownfield documents rather than authored from fresh discovery.

The PRD also does not hide the uncomfortable state of Epic 11 or the readiness report. §10 explicitly says Epic 11 is not implementation-ready as written and names the route-contract gate/order contradiction, Story 11.10 split, and the need to narrow or split Stories 11.8 and 11.9.

### Findings

- **low** Assumptions lack ownership and revisit conditions (§13, lines 458-464) — The index is complete, but release-facing assumptions need a path to closure. *Fix:* Add owner/revisit columns or move closure details into §12 so assumptions do not linger through finalization.

## Downstream usability — adequate

The PRD is usable by downstream workflows in several important ways. FR IDs are contiguous, user journeys have named protagonists, the glossary defines key domain terms and FC contract abbreviations, success metrics reference FRs, and the addendum records source inventory and technical details intentionally held out of the PRD.

The main downstream risk is extraction ambiguity. Story creation will struggle to tell which FR consequences are already implemented, which are validation gates, which are remediation, and which are new build work. Architecture and UX workflows will also need local definitions for externally referenced epics and story IDs.

### Findings

- **high** Brownfield status is not encoded at requirement level (§5, lines 95-355; §8, lines 380-396) — The PRD says it covers both existing baseline and post-MVP backlog, but individual FRs do not mark `existing`, `gap`, `v1-blocker`, `post-MVP`, or `validate-only`. *Fix:* Add status metadata per FR or consequence so downstream story generation can separate implementation, verification, remediation, and deferral work.
- **medium** External artifact references should resolve from the PRD (§10 and addendum, lines 429-433; addendum lines 7-28) — The addendum inventories source files, but the PRD body cites readiness and epic defects without local anchors or short summaries. *Fix:* Add links or explicit file references beside the cited defects and include enough detail for downstream readers who only source-extract the PRD.

## Shape fit — adequate

The selected shape fits a brownfield, chain-top developer-framework PRD better than a pure consumer journey doc would. Capability groups are the right organizing principle, with user journeys present only where they clarify adoption, operations, MCP, compatibility, and testing workflows.

The one shape concern is breadth. Combining existing baseline, v1.0 release readiness, and post-MVP remediation in one PRD is defensible for this repository, but it requires sharper status markings than a greenfield PRD. Without them, the document can read like a complete product inventory and a future backlog at the same time.

### Findings

- **medium** Existing baseline and readiness backlog need clearer visual separation (§8.1-§8.2, lines 382-396) — The scope section separates them, but §5 blends baseline features and remediation requirements under the same FR style. *Fix:* Add section-level or FR-level labels such as `Baseline`, `Readiness Gap`, `V1 Blocker`, and `Deferred`, then keep §8 as the summary roll-up.

## Mechanical notes

- FR IDs are contiguous from FR-1 through FR-26.
- UJ IDs are contiguous from UJ-1 through UJ-6 and each has a named protagonist.
- Assumptions A1 through A7 appear inline and are indexed in §13.
- Success metric IDs are contiguous for SM-1 through SM-6 plus SM-C1 through SM-C3.
- No duplicate glossary terms were found.
