# G-6 Tenants adopter evidence — Product Owner decision

**Decision date:** 2026-09-26

**Decision owner:** Product Owner

**Decision:** **Accept** the independent Hexalith.Tenants bootstrap evidence for G-6 and SM-1 on FrontComposer candidate `05d122005d328ec8f15ecd276058be29730288c2`. Close OI-5 and record EXT-ADOPTER-1 as delivered and accepted for this candidate.

This is an explicit decision after reviewing the evidence, not an approval inferred from `result: pass`. The source of record is the Tenants maintainer's dated [acceptance account](../../../references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md) and [machine result](../../../references/Hexalith.Tenants/_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.json), committed in Tenants `04f68c60da0a85ec066d68b80aecb9b4417f586d`. The [PRD G-6 and SM-1 criteria](../../planning-artifacts/prd.md) and the [adopter proof procedure](../../../docs/how-to/adopter-bootstrap-proof.md) define the scope of this decision.

| Review point | Finding |
| --- | --- |
| Date and source | Both evidence files identify the 2026-09-25 run and exact FrontComposer candidate SHA. That SHA exists in FrontComposer history; Tenants source `8431d9926ed655d32396f1f68973e0573ccc38e2` contains the annotated `TenantSummaryProjection`, `CreateTenantCommand`, and `TenantsFrontComposerDomain` used by the run. |
| Packages | The four reported `4.4.0-g6.05d12200` archives are Contracts, Contracts.UI, Shell, and SourceTools. Their SHA-512 values were recomputed from the candidate archives and matched the JSON; each NuGet manifest identifies the expected package ID and version, FrontComposer repository URL, and exact candidate commit. The runner's result also reports a fresh-cache restore match with `package_identity.verified: true`. |
| Runtime | The result identifies .NET SDK `10.0.401`, `Microsoft.NETCore.App` `10.0.12`, and `Microsoft.AspNetCore.App` `10.0.12`; the account agrees. The SDK matches the candidate's `global.json`. These are recorded run identities, not a claim that the reviewer reran the historical host. |
| Generated Tenants renders | The three-call host used Quickstart with a Tenants assembly scan, `AddHexalithDomain<TenantsFrontComposerDomain>()`, then `AddHexalithEventStore(...)`, retaining real EventStore command and query registrations. The result names `TenantSummaryProjectionView@/adopter/tenants-projection` and `CreateTenantCommandForm@/adopter/tenants-command`; the account records the projection heading/empty placeholder and command form/required `Tenant ID` field. Both render assertions are true. These are Tenants-generated surfaces, not the kit's Proof fixtures. |
| Negative and empty cases | Missing Quickstart and misordered EventStore produced startup diagnostics before render. Quickstart with an empty registry rendered `fc-home-empty-no-microservices`. Both machine assertions are true and the account identifies the two negative cases. |
| Redaction and result | The JSON has only the proof schema's allowlisted identity, surface, assertion, result, and failure fields. It reports `result: pass`, `failure: null`, and contains no endpoint, credential, tenant or user identifier, payload, stack trace, or machine path. The account describes a redacted loopback placeholder and says raw diagnostics remained local. |

**Scope:** This proves candidate-bound registration and generated HTML rendering. No command was submitted and no EventStore response was tested; G-6 and SM-1 require the renders, not a live command outcome. The temporary `/adopter/` routes and build settings documented by Tenants did not change the candidate packages or the checked bootstrap sequence.

**Tracking effect:** G-6 is closed; SM-1 is met; OI-5 is closed; EXT-ADOPTER-1 is delivered and accepted. Tenants remains the selected adopter. D-7 was not invoked, and Story 12.2 / ADOPT-APP-1 remains conditional in the backlog. This decision does not grant D-9 PRD re-approval, release authorization, or the v1.0 readiness milestone; the other gates remain open.
