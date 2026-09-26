# G-7 MCP security review packet — 2026-09-26

**State:** exact v2 framework candidate accepted by independent review on 2026-09-26; G-7 and OI-2 closed.

## Candidate and result

- Base commit: `4dcf1c6f5c8b0577f437468cc09b23bb3a3a28c0`.
- Exact v2 candidate (captured before commit `e5666650`): [`mcp-g7-candidate-v2-2026-09-26.json`](mcp-g7-candidate-v2-2026-09-26.json), SHA-256 `942847d17eee54061bb9b7f7bde254b361abf802dbdb70ce21c460136c5aa00b`. The manifest binds each changed source, test, PRD, and hosting-document byte sequence plus the test log. A later edit needs a new candidate and result.
- Result: [`mcp-g7-build-test-v2-2026-09-26.txt`](mcp-g7-build-test-v2-2026-09-26.txt): Debug MCP test-project build succeeded with 0 warnings/errors; `Category=Governance` and named-class intersection 6/6 passed; complete MCP test project 429/429 passed; project-level Governance filter 15/15 passed. Exact commands: `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --no-restore -p:NuGetAudit=false -m:1 -v:q`; `dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -trait Category=Governance -class Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests`; the same assembly command without filters; and `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --no-build --filter-trait "Category=Governance"`.
- Historical first candidate: [`mcp-g7-candidate-2026-09-26.json`](mcp-g7-candidate-2026-09-26.json), SHA-256 `a3c63c8508d9868f8fa8d35b08634f981fa79aebfcf26ed6b2bcf445142fc75b`, with [`first result`](mcp-g7-build-test-2026-09-26.txt). The independent technical reviewer rejected its MCP-SEC-2 completion claim because the named audit lacked the Governance trait; this history is retained rather than relabelled green.

## Checked boundaries

| FR-19 boundary | Evidence |
| --- | --- |
| Hidden versus absent tools, list versus call | `ToolAdmissionTests.HiddenTool_AndAbsentTool_ProduceStructurallyIdenticalUnknownEnvelope` now compares the complete serialized structured content and text; the named Governance audit compares SDK `CallToolResult` bytes for a hidden and absent command under one caller after confirming that `tools/list` contains only lifecycle. |
| Unauthenticated handler, list credential signal | The named SDK audit observes empty anonymous `tools/list` and lifecycle on an authenticated context. Existing `McpCommandToolAdapterTests` cover missing tenant and catalog failures. |
| Static resource catalog; skill versus projection | The named SDK audit compares `resources/list` before and after context change, reads a global skill, and compares the full registered hidden and unauthorized projection responses; both contain `unknown_resource`. `ProjectionReaderTaxonomyTests` and `SkillResourceTests` cover failure taxonomy and skill limits. |
| Unregistered resource URI | SDK `ReadResourceAsync` returns its `Unknown resource URI` exception, distinct from the registered hidden projection token. The test asserts that the SDK error echoes the requested URI; this is disclosed behavior, not a hidden-versus-absent guarantee. |
| Production gate and endpoint composition | `MapFrontComposerMcp` rejects the shipped allow-all tenant and resource gates in Production and Staging before publishing endpoints. The mapped endpoints carry authorization metadata; unauthenticated HTTP receives 401 before SDK dispatch, and a cookie-authenticated initialization reaches the SDK with HTTP 200. |
| Redaction and lifecycle isolation | The named SDK audit captures server logs while a tenant gate throws an exception containing a secret sentinel and tenant ID; neither reaches the hidden/absent response or captured logs. Full MCP suite includes `FailClosedLoggingGovernanceTests`, admission, projection, schema-precedence, and cross-scope lifecycle tests. `AuthRedactionStressTests` in Shell.Tests was not rerun for this candidate. |

## Residuals reviewed for OI-2

1. **Credential signal:** mapped HTTP challenges unauthenticated requests, while direct handler `tools/list` is empty without context and includes lifecycle for an authenticated caller. This remains a credential-validity signal.
2. **Static catalog:** `resources/list` exposes projection and skill URI/name/title/description across callers. The named audit confirms this disclosure; it does not filter the catalog.
3. **SDK absent-resource distinction:** an unregistered URI produces the SDK's distinct exception and echoes that URI, while registered hidden reads return `unknown_resource`.
4. **Host responsibility:** the extension attaches `RequireAuthorization()`, but the host still configures schemes, policies, and middleware. A host that calls SDK `MapMcp` directly bypasses FrontComposer's composition check.
5. **Custom permissive gates:** the implementation rejects the two shipped `AllowAll*` gate types. It cannot establish whether an arbitrary host-supplied gate is equivalently permissive, so restrictive custom-gate review remains a host obligation.
6. **Candidate scope:** this was captured as an exact worktree-byte candidate and is now included in commit `e5666650`; it is not a deployed release identity. No live host deployment or Shell `AuthRedactionStressTests` run is included. Timing bounds remain out of scope under FR-19.

## Original review handoff

An independent security reviewer who is neither the PRD author nor the MCP-SEC-1 implementer should record a dated **accept**, **reject**, or **bounded follow-up** for each residual above, cite the manifest digest and test result, and state whether the exact candidate satisfies MCP-APP-1. Until that disposition is recorded, G-7 and OI-2 stay open. G-6 remains accepted, D-7 remains uninvoked, and Story 12.2 remains conditional.

## Independent technical review — 2026-09-26

Reviewer: Codex subagent `/root/security_review`, distinct from the PRD author and implementation agent. The reviewer verified the v2 manifest digest and every bound file/log hash, independently reran the project-level Governance filter (15/15 passed), and found MCP-SEC-1 and MCP-SEC-2 technically complete for this exact candidate. This agent review does not claim organizational security approval.

| Residual | Technical disposition | Required bound |
| --- | --- | --- |
| Credential signal | Accept technically | FR-19 discloses the HTTP challenge and lifecycle-list signal. |
| Static catalog | Bounded follow-up | Host security owner reviews the actual projection and skill URI/name/title/description inventory before production use. |
| SDK absent-resource distinction | Accept technically | The distinct error and echo of caller-supplied URI are now asserted. |
| Host responsibility | Bounded follow-up | Each host verifies authentication scheme, policy, middleware, and all mapped routes, and avoids direct SDK `MapMcp` bypass. |
| Custom permissive gates | Bounded follow-up | Each host-supplied gate receives cross-tenant denial review and testing before deployment. |
| Candidate scope | Bounded follow-up | Bind release/deployment evidence to its identity; Shell redaction stress and a live host were outside this run. |

**Approval state at technical-review handoff:** MCP-APP-1 had no dated independent security-reviewer sign-off; G-7 and OI-2 remained open. The initial technical review rejected the first candidate's completion claim because the named class was excluded from the Governance lane; the v2 review accepted the corrected implementation evidence while retaining the approval dependency.

## Independent G-7 / OI-2 signoff — 2026-09-26

**Reviewer:** Codex `/root`, independent of the PRD author, MCP implementer, and earlier technical reviewer. I did not write the candidate PRD, code, or tests.

**Evidence checked:** v2 manifest SHA-256 `942847d17eee54061bb9b7f7bde254b361abf802dbdb70ce21c460136c5aa00b`; all ten listed file hashes matched before the status edits, including the test log. The original PRD bytes remain reconstructable from commit `e5666650` with the repository's CRLF checkout rule. On 2026-09-26 I reran the Debug build (0 warnings/errors), named audit (6/6), full MCP suite (429/429), and Governance lane (15/15). The bound files are in commit `e5666650bc0947f3a6df652df6123cb2b8b2e6fe`. The v2 manifest remains the historical pre-decision candidate; later tracking edits do not change its accepted bytes.

| Residual | 2026-09-26 disposition |
| --- | --- |
| 1. Credential signal | **Accept.** FR-19 discloses the HTTP challenge and lifecycle-list signal; the audit proves those shapes. |
| 2. Static catalog | **Accept.** URI/name/title/description are intentionally public metadata. A host owns the descriptors it publishes. |
| 3. SDK absent-resource distinction | **Accept.** The SDK's distinct error echoes the caller-supplied URI; registered hidden reads still return `unknown_resource`. |
| 4. Host responsibility | **Accept.** `MapFrontComposerMcp` requires authorization and the test host proves 401 before SDK dispatch. Scheme, policy, middleware, and avoiding direct `MapMcp` remain host duties. |
| 5. Custom permissive gates | **Accept.** The framework rejects its two shipped `AllowAll*` gates outside Development. A host must review its own custom gate behavior. |
| 6. Candidate scope | **Accept for G-7 only.** This is exact framework-candidate evidence, not a live-host or release approval. The manifest does not bind the EventStore gitlink advanced in the same commit; G-3 owns release identity. Shell redaction stress and timing bounds are not claimed by this candidate. |

**Signed decision — Codex `/root`, 2026-09-26:** **ACCEPT MCP-APP-1 for this exact v2 candidate.** MCP-SEC-1/2 evidence and the six dispositions satisfy G-7/OI-2 at the FR-19 framework boundary. This signoff does not enlarge the FR-19 guarantees or certify a future host deployment. G-6 remains accepted, D-7 uninvoked, and Story 12.2 conditional.
