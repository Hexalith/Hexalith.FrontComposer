# Domain-Specific Requirements

### Framework-as-Foundation Constraints

Hexalith.FrontComposer is classified in the `general` domain because it is **horizontal infrastructure**: a framework for building event-sourced UIs across any vertical. It is not tied to healthcare, fintech, govtech, energy, or any other regulated sector. This positioning is a deliberate architectural commitment, not an absence of ambition — and it carries specific design rules that all later PRD sections must honor.

**Why horizontal:** Adopters will use FrontComposer to build UIs for domains FrontComposer itself has no expertise in — clinical trial management (HIPAA), payment processing (PCI-DSS), grid SCADA (NERC CIP), legal discovery (ABA ethics), aerospace telemetry (ITAR/DO-178C). The framework's job is to make those verticals *possible*, not to pre-bake them. A framework that assumes a vertical embeds assumptions that rule out every other vertical.

**Architectural commitments that preserve vertical-neutrality:**

| Commitment | Why it preserves vertical-neutrality |
|---|---|
| **No PII at the framework layer** | FrontComposer persists only UI preference state (theme, density, nav, filters) in client `localStorage`. All business data — including any regulated personal, health, financial, or classified data — lives in the adopter's microservices and Hexalith.EventStore. The framework never touches it. |
| **DAPR-only infrastructure coupling** | All infrastructure concerns (state store, pub/sub, secrets, config, observability) route through DAPR component bindings. Adopters swap DAPR components to meet their regulatory backend requirements (FIPS-validated crypto, in-country state stores, air-gapped pub/sub) without touching framework code. |
| **Shared typed contracts via NuGet, not schemas-on-the-wire** | Domain contracts travel as compiled C# types. There is no JSON schema surface the framework controls, so compliance-mandated data shapes (e.g., HL7 FHIR resources, ISO 20022 payment messages) are owned entirely by adopter microservices. |
| **Zero client-side business logic in auto-generated components** | Command validation, authorization, and business rules are enforced server-side by the adopter's microservices via FluentValidation + DAPR actor-based aggregates. The framework never client-validates anything that would need to also be validated server-side, and it never makes authorization decisions. Client-side ETag cache is hint-only; correctness comes from server queries. |
| **WCAG 2.1 AA baseline, not just aspiration** | Regulated verticals (govtech under Section 508, education under ADA, European public sector under EAA) require verifiable accessibility. FrontComposer enforces the baseline in CI (axe-core + specimen verification + manual screen-reader verification) so adopters inherit conformance rather than having to prove it themselves for framework-generated UI. |
| **Keycloak + Entra + GitHub + Google auth compatibility, no custom auth UI** | FrontComposer does not ship its own identity UI. Adopters bring their regulated IdP (e.g., Entra with Conditional Access policies, Keycloak in a customer's private tenant) and FrontComposer integrates via standard OIDC/SAML flows. |
| **Self-hostable on-premise, sovereign cloud, and major cloud providers** | No vendor lock-in at any layer. Adopters in data-residency-constrained verticals (public sector, healthcare, defense) can deploy the entire stack in-country on their chosen infrastructure. |

**Explicit non-commitments — what FrontComposer will NOT ship (adopter microservice responsibility):**

- **No vertical-specific audit logging primitives.** HIPAA §164.312(b), SOX audit trails, PCI DSS Requirement 10 — all are adopter responsibility. FrontComposer's command/event history is observable via the event store; shaping it into vertical-compliant audit records is the adopter's job.
- **No regulated-data classification, tagging, or DLP integration.** Fields marked as PHI, PII, PCI, CUI, or export-controlled are adopter-domain concerns. FrontComposer renders whatever types the domain declares.
- **No vertical-specific consent or retention management.** GDPR Article 17 right-to-erasure, CCPA opt-out, HIPAA authorization workflows — all are adopter responsibility. The framework's crypto-shredding-awareness story (referenced in the ES ecosystem research) is a v1.x / v2 enhancement, not v1 scope, and even when delivered it will be a cache-invalidation hook rather than a consent engine.
- **No vertical-specific data validation libraries.** FluentValidation handles structure; domain-semantic validation (valid ICD-10 code, valid ISO 4217 currency, valid FAR clause reference) is adopter responsibility.
- **No built-in encryption at rest beyond what DAPR state stores provide.** Adopter choice of DAPR state store determines encryption-at-rest posture; FIPS-validated backends, HSM-backed stores, etc. are configuration choices, not framework features.

**The horizontal/vertical boundary rule:** If a feature would help adopters in one vertical but be inappropriate, unused, or confusing in another, it belongs in a vertical-specific extension package (e.g., a hypothetical `Hexalith.FrontComposer.Healthcare` community module), not in the core framework. Future PRD sections must filter candidate features against this rule before admitting them to v1 scope. **A framework that is 80% horizontal and 20% vertical is neither — it is a horizontal framework with confusing baggage.**

**Implication for later PRD sections:** Functional and non-functional requirements must be written in vertical-neutral language. No requirement may assume a specific compliance regime, data type, or regulatory workflow. The test for inclusion is: *"Would an adopter building a healthcare EHR and an adopter building a fintech payment rail both need this, or only one of them?"* Features that help only one belong outside the core framework.
