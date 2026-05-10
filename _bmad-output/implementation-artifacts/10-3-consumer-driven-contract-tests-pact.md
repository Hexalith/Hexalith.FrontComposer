# Story 10.3: Consumer-Driven Contract Tests (Pact)

Status: review

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR78** and **NFR55**. Adds file-based Pact contracts at the REST-to-generated-UI boundary so FrontComposer and Hexalith.EventStore cannot drift silently. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, and the repository submodule rule.

---

## Executive Summary

Story 10-3 makes the generated UI's EventStore assumptions executable:

- Add file-based Pact consumer contracts under the Shell test project, not a floating repository folder and not Pact Broker.
- Cover command dispatch, query execution, ETag/not-modified behavior, tenant propagation, ULID message IDs, response classification, and bounded/redacted contract artifacts.
- Verify the provider against the local Hexalith.EventStore root-level submodule or a documented provider-verification handoff without requiring nested submodule initialization.
- Keep this story focused on REST contract drift. Mutation testing, property-based idempotency, flaky quarantine, accessibility, release signing, SBOM, and LLM benchmark work stay in their named Epic 10 follow-up stories.

---

## Story

As a developer,
I want contract tests that verify generated UI components consume EventStore API contracts correctly,
so that API changes never silently break the generated UI and I catch contract drift before deployment.

### Adopter Job To Preserve

An adopter should be able to update FrontComposer or EventStore with confidence that generated command forms and projection grids still send the request shapes EventStore accepts, still interpret response classes correctly, and still protect tenant/user boundaries without relying on a full end-to-end environment for every PR.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Contract home | Store generated pact JSON under `tests/Hexalith.FrontComposer.Shell.Tests/Pact/`. Keep names stable and deterministic. Do not add Pact Broker or PactFlow requirements. |
| Test project | Add PactNet to the smallest test project that owns the consumer boundary, currently `tests/Hexalith.FrontComposer.Shell.Tests`. Add a separate provider verification fixture only if it can run against a real TCP socket. |
| Consumer under test | Exercise the actual FrontComposer EventStore adapters (`EventStoreCommandClient`, `EventStoreQueryClient`, `EventStoreResponseClassifier`, `EventStoreOptions`) through `ICommandService` and `IQueryService` behavior. Do not create a second HTTP client just for Pact. |
| Command contract | POST `/api/v1/commands`, JSON body with `messageId`, `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, optional `correlationId`, optional bounded `extensions`; bearer auth present when required; expect `202 Accepted`, `Location`, optional `X-Correlation-ID`, `Retry-After`, and typed failures for non-202 responses. |
| Query contract | POST `/api/v1/queries`, JSON body with `tenant`, `domain`, `aggregateId`, `queryType`, `projectionType`, `payload`, optional `entityId`, optional `projectionActorType`; `If-None-Match` emitted only from validated ETags/cache; expect `200` with payload/ETag and `304 Not Modified` handling. |
| Provider under test | Verify against Hexalith.EventStore's real ASP.NET Core pipeline on a TCP port. PactNet provider verification cannot use in-memory `TestServer`/`WebApplicationFactory` because the native verifier must call an HTTP endpoint. |
| CI lane | Add a contract lane or step that fails when checked-in pact files change unexpectedly, provider verification fails, zero interactions are verified, or artifacts are missing. Keep runtime within the existing full CI budget. |
| Submodules | Use root-level submodules only. Do not run recursive nested submodule initialization or scan nested submodule internals. |
| Evidence | Contract JSON and verification output must be deterministic, bounded, and redacted. Do not persist bearer tokens, local paths, full business payloads, cookies, or environment secrets. |

Start here: T1 dependency and folder shape -> T2 consumer command pact -> T3 consumer query/ETag pact -> T4 provider verification fixture -> T5 CI/artifact validation -> T6 docs/handoff.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | Pact contract testing setup is added | Consumer contracts are written | Pact JSON files are file-based, committed under `tests/Hexalith.FrontComposer.Shell.Tests/Pact/`, and no Pact Broker, PactFlow, or external contract service is required for v1. |
| AC2 | A command dispatch consumer contract runs | The generated UI command adapter submits a command | The contract verifies `POST /api/v1/commands`, JSON content type, bearer authorization when required, ULID-shaped `messageId`, tenant value, domain, aggregate ID, command type, payload object, and bounded extension metadata. |
| AC3 | EventStore accepts command dispatch | The consumer contract receives the response | The contract expects `202 Accepted`, a status `Location` header, `Retry-After` when present, optional `X-Correlation-ID`, and `CommandResult.Status == "Accepted"` with the original generated message ID preserved. |
| AC4 | EventStore rejects or warns on command dispatch | Non-202 responses are modelled | Provider/consumer coverage includes at least 400 validation, 401 auth redirect, 403/404 warning, 409 domain rejection/conflict, 429 rate limit, and unexpected 5xx classification without leaking ProblemDetails payloads beyond bounded fields. |
| AC5 | A query execution consumer contract runs | The generated UI query adapter requests projection data | The contract verifies `POST /api/v1/queries`, JSON content type, tenant, domain, aggregate ID, query type, projection type, payload shape, optional entity/projection actor fields, and bearer authorization when required. |
| AC6 | A cached projection query runs | The adapter has a validated cache ETag | `If-None-Match` is sent with at most ten validators, rejects CRLF/control characters before send, and never derives validators from raw untrusted user input. |
| AC7 | A query returns fresh data | EventStore responds with `200 OK` | The contract verifies an `ETag` response header, payload item shape, total-count handling when present, and deserialization through existing `QueryResult<T>` behavior. |
| AC8 | A query returns no changes | EventStore responds with `304 Not Modified` | The contract verifies `QueryResult<T>.IsNotModified` behavior and distinguishes cached-payload reuse from an explicit no-change signal when no compatible cache entry exists. |
| AC9 | Provider verification runs | The EventStore provider is started | Verification uses a real loopback TCP socket, not in-memory `TestServer` or `WebApplicationFactory`, and fails if the native Pact verifier cannot reach the provider endpoint. |
| AC10 | Provider states are needed | Pact verifies interactions | Provider state setup is deterministic, reset per interaction, tenant-scoped, and uses EventStore testing fakes or local sample fixtures instead of live DAPR, external network, or persisted shared state unless an explicit provider lane documents the dependency. |
| AC11 | Contract files are generated | The same tests are run twice | Pact JSON output is byte-stable aside from approved Pact metadata fields, uses deterministic interaction descriptions, and does not include local machine paths, random ports, tokens, or full production payload samples. |
| AC12 | Contract verification is wired into CI | The lane runs on PR and main | Contract tests fail the build on changed-uncommitted pacts, provider verification failures, zero verified interactions, missing verification reports, or stale pact files for removed interactions. |
| AC13 | The repo has submodules | Contract tests and CI checkout run | Only root-level submodules are initialized; no command uses recursive nested submodule initialization. |
| AC14 | EventStore API contracts drift | FrontComposer or EventStore changes request/response shape | The failing contract names the interaction, endpoint, expected field/header/status, actual mismatch, and owning story/source path without dumping sensitive payloads. |
| AC15 | PactNet is added | Package metadata is reviewed | `Directory.Packages.props` pins an explicit PactNet version, dependency usage is limited to test projects, and native verifier platform limitations are documented for Linux and Windows CI agents. |
| AC16 | FrontComposer abstractions evolve | Contracts are updated | The story preserves the existing `ICommandService`, `IQueryService`, `CommandResult`, `QueryRequest`, `QueryResult<T>`, and `EventStoreOptions` public contracts unless a separate API-drift story explicitly approves changes. |
| AC17 | Generated UI or SourceTools output drives the consumer | Contract tests build request payloads | The tests use generated command/query metadata or existing Shell adapters rather than hand-assembled JSON that could pass while generated UI is broken. |
| AC18 | Provider verification cannot run in the current repository lane | The implementation reaches that constraint | The consumer pacts still land, and the provider-verification handoff records the exact EventStore project/command, required pact path, expected report artifact, and blocking reason instead of silently marking provider verification done. |
| AC19 | Contract artifacts are uploaded | CI fails or succeeds | The lane retains pact JSON, verifier logs, and concise mismatch reports with bounded size and redaction; it does not upload bearer tokens, cookies, full DOM/browser traces, local user paths, or environment dumps. |
| AC20 | Release readiness is assessed | A release branch is cut | NFR55 is satisfied only when all file-based pacts verify against the pinned EventStore provider version or the release is blocked with a named contract-drift issue. |
| AC21 | Provider-state support is designed | Provider verification is implemented | A deterministic provider-state catalog exists for every interaction category, names setup/teardown behavior, seeded tenant/user/aggregate/message IDs, expected status/result, persistence isolation, and the exact EventStore test-only seam or documented handoff that owns it. |
| AC22 | Pact consumer tests are implemented | The generated UI adapter path is exercised | Tests instantiate the production DI/configuration path for `EventStoreCommandClient` and `EventStoreQueryClient` where possible, and fail if route generation, DTO serialization, headers, ETag transport, or response mapping diverge from the generated adapter behavior. |
| AC23 | Pact verification runs | Interactions are evaluated | The lane fails on zero interactions, unmatched or unused interactions, wrong method/path, omitted required headers, skipped provider verification, or adapter tests that assert return values without matching the expected HTTP request. |
| AC24 | Contract CI gate runs | Build artifacts are evaluated | CI order is build, consumer pact generation, stale-pact diff, provider verification or explicit blocked handoff, redaction scan, artifact publication, and job summary; the build fails if generated pact JSON differs from committed files, expected pacts are deleted, verifier output is missing, or stale files remain. |
| AC25 | Pact files and logs are scanned | Contract artifacts are committed or uploaded | Automated checks reject bearer tokens, raw `Authorization` headers, API keys, cookies, tenant secrets, connection strings, local user paths, environment dumps, authorization payloads, and user PII; only allowlisted synthetic fixture values may appear. |
| AC26 | Native Pact verifier startup is validated | CI and local verification run | PactNet and native verifier/runtime versions are pinned, verification runs on the same supported OS image used by CI, and a containerized or documented fallback is recorded if native verifier startup fails before interactions are evaluated. |
| AC27 | A contract mismatch is discovered | Implementation decides how to fix it | The smallest compatible adapter/test/provider-state correction is attempted first; public FrontComposer API changes or EventStore contract changes require an explicit story update, follow-up story, or product/architecture approval before implementation proceeds. |
| AC28 | Provider verification is repeated or parallelized | Pact provider states are exercised | Provider state setup is isolated by run/interactions, proves teardown of tenant/user/aggregate/cache data, avoids ambient static state, detects stale provider processes, and fails on port collision, failed health probe, or provider startup race before reporting green verification. |
| AC29 | Pact interactions are generated | The test suite writes pact files | Each committed interaction is mapped in a small manifest or test metadata record to the generated command/query source, adapter path, provider state, owning AC, and expected request/response classifier; CI fails orphaned, duplicate, or unowned interactions. |
| AC30 | Redaction scanning is implemented | Scanner tests run against pact and verifier artifacts | Negative fixtures cover tokens in headers, bodies, query strings, encoded/JWT-like strings, ProblemDetails, local paths, and environment-shaped values; synthetic allowlisted values are the only permitted identifiers. |
| AC31 | Contract evidence is reviewed | A PR changes pacts or provider verification behavior | The job summary includes the provider commit/version, Pact specification/version metadata, interaction manifest, stale-diff result, redaction result, and release-blocking status when provider verification is deferred or handed off. |

---

## Tasks / Subtasks

- [x] T1. Add Pact test dependency and file layout (AC1, AC11, AC15)
  - [x] Add `PactNet` as a centrally pinned test dependency in `Directory.Packages.props`; at story creation time NuGet lists PactNet `5.0.1` as latest stable.
  - [x] Reference PactNet only from the test project that owns the consumer boundary, preferably `tests/Hexalith.FrontComposer.Shell.Tests`.
  - [x] Create `tests/Hexalith.FrontComposer.Shell.Tests/Pact/` for committed pact files, with stable names by capability such as command dispatch, query execution, cache validation, and auth/tenant propagation.
  - [x] Ensure pact JSON output ordering and interaction descriptions are deterministic to avoid noisy diffs.
  - [x] Add `.gitignore` exceptions or cleanup rules only for transient verifier logs, not for committed pact JSON.
  - [x] Document that PactNet 5.x uses native verifier/runtime pieces, requires supported CI OS/architecture lanes, and has a documented containerized or explicit fallback path if native startup fails.

- [x] T2. Add command dispatch consumer pacts through existing adapters (AC2-AC4, AC14, AC16, AC17)
  - [x] Exercise `EventStoreCommandClient` through `ICommandService` or `ICommandServiceWithLifecycle`, configured with the Pact mock server base URI.
  - [x] Use the existing `IUlidFactory`, tenant/user context, `EventStoreIdentity`, `EventStoreRequestContent`, and `EventStoreResponseClassifier` behavior; do not build an unrelated JSON client in the test.
  - [x] Verify command body fields: `messageId`, `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, optional `correlationId`, and optional bounded `extensions`.
  - [x] Verify request headers: `Content-Type: application/json`, bearer `Authorization` when `RequireAccessToken` is true, and no control/header-injection values.
  - [x] Add negative assertions proving tenant/auth/correlation values are forwarded from adapter input and are not defaulted, hard-coded, reused from a previous test, or leaked into persisted artifacts.
  - [x] Verify accepted response behavior: `202`, `Location`, `Retry-After`, optional `X-Correlation-ID`, `CommandResult.MessageId`, `Status`, `CorrelationId`, `Location`, and `RetryAfter`.
  - [x] Add negative/edge interactions for validation, auth, forbidden/not-found, conflict/rejection, rate limit, and unexpected status classification using bounded ProblemDetails bodies.
  - [x] Assert command payload examples are synthetic and redacted; do not commit production tenant/user IDs or business data.

- [x] T3. Add query execution and ETag consumer pacts through existing adapters (AC5-AC8, AC14, AC16, AC17)
  - [x] Exercise `EventStoreQueryClient` through `IQueryService`, configured with the Pact mock server base URI.
  - [x] Verify query body fields: `tenant`, `domain`, `aggregateId`, `queryType`, `projectionType`, `payload`, optional `entityId`, and optional `projectionActorType`.
  - [x] Verify cache-enabled requests send `If-None-Match` only after `IETagCache` and `QueryRequest.CacheDiscriminator` allowlist behavior accepts the value.
  - [x] Add tests for a single validator, multiple validators up to the configured max, too many validators, and CRLF/control-character rejection before HTTP send.
  - [x] Verify `200 OK` response behavior: `ETag`, payload item shape, total-count defaulting, schema mismatch handling, and cache write-through when applicable.
  - [x] Verify observable `304 Not Modified` behavior for both no-cache caller-owned cases and framework-cache reuse through `QueryResult<T>.NotModifiedFromCache`, without coupling tests to internal cache storage details.
  - [x] Include empty result, max page size, malformed query payload, and large-but-valid metadata cases as named interactions or provider states.
  - [x] Include the mixed-projection/self-routing ETag safety expectation from EventStore provider behavior where relevant.

- [x] T4. Add provider verification against EventStore without in-memory hosting (AC9, AC10, AC13, AC18, AC20, AC28)
  - [x] Start a provider on a real loopback TCP port using Hexalith.EventStore's available host/sample/test fixture, or document the exact EventStore-side verification command if the provider lane must live in the submodule.
  - [x] Do not use `WebApplicationFactory` or ASP.NET Core `TestServer` for Pact provider verification.
  - [x] Define the provider-state seam before implementation proceeds: add minimal EventStore test-only hooks if allowed, or record the exact owning repo/work item and verification adapter/handoff strategy if hooks cannot land here.
  - [x] Use deterministic provider states for command accepted, validation failure, unauthorized, forbidden, not found, conflict/rejection, rate limit, tenant mismatch, query 200, query empty result, query malformed payload, query ETag match, and query ETag non-match.
  - [x] For each provider state, document setup, teardown, seeded IDs, expected status/result mapping, and whether persistence is isolated per interaction.
  - [x] Reset provider state per interaction and keep tenant/user fixtures explicit.
  - [x] Isolate provider-state data by verification run and interaction so parallel or retried verifier execution cannot reuse tenant/user/aggregate/cache data from a previous interaction.
  - [x] Add provider startup guards: unique loopback port allocation, health probe before verification, bounded startup timeout, process cleanup on failure, and stale-process detection before reporting success.
  - [x] Include at least one intentionally mismatched provider-state verification check during implementation to prove the verifier fails before the final green path is recorded.
  - [x] Avoid live DAPR, Aspire, Keycloak, external network, or persistent state for default PR verification unless the lane is explicitly marked as integration/provider and has a documented fallback.
  - [x] If provider verification cannot be implemented in this repository without broader EventStore changes, add a blocking handoff note with the exact project, command, pact path, and missing provider-state hook.

- [x] T5. Add CI contract lane and artifact validation (AC11-AC15, AC19, AC20, AC29-AC31)
  - [x] Add a contract test step or lane in `.github/workflows/ci.yml` after build and before release-readiness checks, ordered as build, consumer pact generation, stale-pact diff, provider verification or explicit blocked handoff, redaction scan, artifact publication, and job summary.
  - [x] Ensure checkout and any helper scripts use root-level submodules only; do not add recursive nested submodule commands.
  - [x] Fail if Pact tests produce uncommitted pact JSON changes, delete expected pacts, verify zero interactions, leave unmatched/unused interactions, skip provider verification, or omit verifier output.
  - [x] Validate a small interaction manifest or equivalent test metadata that maps every pact interaction to its generated command/query source, adapter path, provider state, owning AC, and classifier expectation.
  - [x] Upload bounded pact JSON, verifier output, provider-state logs, stale-pact check results, and concise mismatch reports with stable names and retention.
  - [x] Add a redaction check over committed pact JSON and uploaded artifact candidates that rejects raw `Authorization` headers, bearer tokens, API keys, cookies, tenant secrets, connection strings, local user paths, environment dumps, authorization payloads, and user PII.
  - [x] Include negative scanner fixtures for header, body, query-string, JWT-like, base64-like, ProblemDetails, local path, and environment-shaped secret leaks, plus allowlisted synthetic identifiers to prevent false-positive workarounds.
  - [x] Add a concise summary to the GitHub job output listing pact files, interaction count, provider verification result, and any mismatch categories.
  - [x] Include provider commit/version, Pact specification/version metadata, interaction manifest location, stale-diff result, redaction result, and release-blocking status in the job summary.
  - [x] Keep the lane within the existing full CI budget; if provider verification is too slow or infrastructure-dependent, split PR consumer-pact generation from main/release provider verification and document the gate split.

- [x] T6. Add documentation and adopter-facing contract evidence (AC1, AC12, AC18-AC20, AC31)
  - [x] Document how to regenerate pacts intentionally, how to review pact diffs, and how to run provider verification locally.
  - [x] Add a short docs/reference section explaining that file-based pacts are the v1 source of truth for the FrontComposer/EventStore REST contract.
  - [x] Document the release rule: NFR55 blocks release when checked-in pacts do not verify against the pinned EventStore provider version.
  - [x] Include troubleshooting for native verifier/platform failures, provider state failures, and stale pact file cleanup.
  - [x] Document provider startup failure modes: port collision, failed health probe, stale provider process, verifier timeout, provider-state teardown failure, and how each blocks or hands off release evidence.
  - [x] Document the Pact Broker/PactFlow migration trigger: reconsider only when multiple provider versions, external consumers, or cross-repo release coordination require it.
  - [x] Record a lightweight ADR note for the file-based Pact and real-TCP provider-verification decision, including rejected alternatives: broker-first workflow, in-memory provider verification, hand-built JSON-only tests, and browser-only contract coverage.
  - [x] Document that this story does not introduce Pact Broker, PactFlow, mutation testing, property-based idempotency, flaky quarantine, accessibility/visual gates, SBOM, signing, or LLM benchmark governance.

- [x] T7. Final verification and handoff (AC1-AC31)
  - [x] Run `dotnet restore Hexalith.FrontComposer.sln`.
  - [x] Run the Shell contract tests that generate/verify consumer pacts.
  - [x] Run provider verification against the local provider or record the blocking provider-verification handoff.
  - [x] Run `git diff -- tests/Hexalith.FrontComposer.Shell.Tests/Pact/` and confirm pact file changes are intentional.
  - [x] Run the default .NET test lane touched by EventStore adapter or contract test changes.
  - [x] Run `dotnet build Hexalith.FrontComposer.sln --configuration Release`.
  - [x] Record PactNet version, native verifier runtime/platform, Pact specification/version metadata, provider commit/version, pact files, provider command/URL, provider-state catalog location, interaction manifest location, interaction count, stale-pact result, redaction scan result, CI lane name, artifact paths, submodule behavior, public API mismatch decisions, and any deferred provider-state hooks in completion notes.

---

## Dev Notes

### Current Repository State

- `EventStoreOptions` already defines the default REST paths: `/api/v1/commands` and `/api/v1/queries`, plus `/hubs/projection-changes` for SignalR notifications. This story covers only the REST contracts.
- `EventStoreCommandClient` currently serializes a private command request shape containing `messageId`, `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, optional `correlationId`, and optional `extensions`, sends it through `HttpClientName = "Hexalith.FrontComposer.EventStore.Commands"`, and expects `202 Accepted`.
- `EventStoreQueryClient` currently serializes a private query request shape containing `tenant`, `domain`, `aggregateId`, `queryType`, `projectionType`, `payload`, optional `entityId`, and optional `projectionActorType`, sends it through `HttpClientName = "Hexalith.FrontComposer.EventStore.Queries"`, and classifies `200 OK` and `304 Not Modified`.
- `EventStoreResponseClassifier` is the shared Shell-side response taxonomy. Contract tests should lock this behavior rather than duplicating status parsing in separate test helpers.
- `QueryRequest` includes `ETag`, `ETags`, `CacheDiscriminator`, and `CachePayloadVersion`; `EventStoreQueryClient` adds cached validators only after cache-key allowlisting and rejects control characters before send.
- `EventStoreValidation` already enforces maximum ETag count and no-colon route segments. Keep those checks in the contract path.
- `Hexalith.EventStore` is a root-level git submodule pinned in this repository. Its controllers currently expose `api/v1/commands` and `api/v1/queries`; do not initialize nested submodules.
- Current CI uses `actions/checkout` with `submodules: true` in the build/test lane, which initializes root-level submodules. Keep that pattern and do not add recursive submodule commands.
- `tests/e2e` is for Playwright/browser gates and belongs primarily to Story 10-2. Pact contracts should live with .NET Shell/EventStore adapter tests unless implementation proves a browser consumer contract is needed.
- `tests/Hexalith.FrontComposer.E2E` currently contains MCP/prerender evidence, not the primary Pact home.

### Architecture and Package Boundaries

| Surface | Story 10-3 responsibility |
| --- | --- |
| `tests/Hexalith.FrontComposer.Shell.Tests/Pact/` | File-based pact JSON generated by consumer tests and committed for review. |
| `tests/Hexalith.FrontComposer.Shell.Tests` | Consumer Pact tests for FrontComposer EventStore adapters and response classification. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore` | Existing HTTP adapters under test; change only if a real contract mismatch is found. |
| `src/Hexalith.FrontComposer.Contracts/Communication` | Public command/query abstractions must stay stable unless a separate API-drift decision is made. |
| `Hexalith.EventStore` root submodule | Provider verification target or provider-verification handoff source. Do not package or scan nested internals beyond the files needed for verification. |
| `.github/workflows/ci.yml` | Contract lane, artifact upload, root-level submodule checkout, and stale-pact detection. |
| Documentation | How to regenerate, verify, review, and release-gate file-based pacts. |

### Contract Quality Gates

Story 10-3 is ready for development only if the implementation treats Pact output as executable release evidence, not advisory test logs:

- **Provider-state ownership:** Provider states must be declared in a catalog before verifier work proceeds. The catalog must cover command accepted/rejected, validation failure, unauthorized, forbidden, not found, rate limit, tenant mismatch, query fresh result, query empty result, ETag match, ETag non-match, malformed query payload, and large-but-valid metadata.
- **Generated adapter path:** Consumer tests must exercise the production FrontComposer EventStore adapter path through DI/configuration where possible. Hand-built JSON may support assertions but must not be the only contract producer.
- **Zero-interaction guard:** The lane must fail on no requests, wrong method/path, omitted required headers, unmatched interactions, unused interactions, skipped verifier execution, or missing verifier output.
- **Interaction ownership:** Every interaction must have an explicit generated command/query source, adapter path, provider state, owning AC, and classifier expectation. Orphaned or duplicate interactions should fail the lane instead of silently expanding pact files.
- **Provider process isolation:** Provider verification must fail on startup races, port collisions, failed health probes, stale provider processes, or state teardown failures. Retried or parallel verification cannot reuse tenant/user/aggregate/cache data from another interaction.
- **CI evidence:** CI must publish bounded pact JSON, verifier output, provider-state logs, stale-pact check results, redaction scan results, and concise mismatch summaries with stable artifact names.
- **Redaction scan:** Contract files and uploaded logs must be scanned for bearer tokens, raw `Authorization` headers, API keys, cookies, tenant secrets, connection strings, local user paths, environment dumps, authorization payloads, and user PII.
- **Verifier platform control:** PactNet and verifier runtime versions must be pinned, run on the supported CI OS image, and document a containerized or explicit fallback if native startup fails.
- **Mismatch escalation:** If Pact exposes drift, the implementation should prefer the smallest compatible adapter/test/provider-state correction. Public FrontComposer API changes or EventStore contract changes require story update, follow-up story, or product/architecture approval.

### REST Contract Details To Lock

| Interaction | Required request | Required response |
| --- | --- | --- |
| Command accepted | `POST /api/v1/commands`, JSON command envelope, bearer token when required | `202 Accepted`, `Location`, optional `X-Correlation-ID`, `Retry-After`, body with correlation metadata when present |
| Command validation failure | Same endpoint with invalid payload or route data | `400` ProblemDetails bounded to title/detail/status/errors/globalErrors/entityLabel |
| Command auth failure | Missing/invalid bearer context | `401` mapped to auth redirect requirement |
| Command forbidden/not found | Valid auth but invalid tenant/resource | `403` or `404` mapped to command warning kinds |
| Command conflict/rejection | Domain conflict or rejection | `409`/rejection path mapped to `CommandRejectedException` or the current classifier taxonomy |
| Command rate limit | Too many submissions | `429` plus optional `Retry-After` |
| Query fresh data | `POST /api/v1/queries`, query envelope, optional `If-None-Match` | `200 OK`, optional `ETag`, payload object/array, total-count handling |
| Query not modified | Same query with matching validator | `304 Not Modified`, optional `ETag`, no payload required |
| Query auth/forbidden/not found/rate limit | Invalid or unauthorized query context | Existing `QueryFailureException`/auth taxonomy |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 2-3 and 2-4 | Story 10-3 | Command lifecycle, correlation/message IDs, `CommandResult`, and accepted/rejected outcomes must remain contract-visible. |
| Stories 4-1 through 4-6 | Story 10-3 | Generated DataGrid/query payload shape and formatting metadata must flow through `IQueryService` rather than bespoke test JSON. |
| Stories 5-1 through 5-7 | Story 10-3 | EventStore REST adapters, ETag cache, response classifier, SignalR/reconciliation assumptions, and failure taxonomy are the primary contract surface. |
| Stories 7-1 through 7-3 | Story 10-3 | Tenant/user context, bearer authorization, policy enforcement, and redacted diagnostics must remain fail-closed. |
| Story 10-1 | Story 10-3 | Testing package fakes/builders may help create deterministic command/query payloads; Pact remains this story's responsibility. |
| Story 10-2 | Story 10-3 | Accessibility/visual specimen gates stay separate from REST contract verification. |
| Story 10-4 | Story 10-3 | Property-based idempotency and mutation testing must not be folded into Pact setup. |
| Story 10-5 | Story 10-3 | Flaky quarantine and CI duration governance remain later work; this story only adds contract-lane basics. |
| Story 10-6 | Story 10-3 | Release signing, SBOM, and LLM benchmark provenance are outside Pact scope. |

### Security and Redaction Requirements

- Contract examples must use synthetic tenants, users, domains, aggregate IDs, commands, and projections.
- Pact files must not include bearer tokens, cookies, local machine paths, environment variables, real customer payloads, or full production ProblemDetails bodies.
- Mismatch output should include endpoint, status, field/header path, and interaction name, but bound long strings and redact tenant/user/token-like values.
- Provider state setup must reset tenant/user state between interactions and must not depend on ambient static state.
- Redaction scanner tests must include encoded and disguised leak cases, including JWT-like strings, base64-like payloads, query-string credentials, ProblemDetails echo fields, local paths, and environment-shaped key/value dumps.
- Preserve `CommandEnvelope.ToString()` payload redaction and existing telemetry redaction patterns; do not loosen them to make tests easier.

### Latest Technical Notes

- PactNet `5.0.1` is the latest stable release visible from NuGet and GitHub at story creation time.
- PactNet 5.x supports Pact specification versions 2, 3, and 4 and uses the current `Pact.V4(...)`, `IPactBuilderV4`, and `PactVerifier` APIs.
- PactNet's provider verifier must call a real HTTP endpoint. Do not host provider verification through ASP.NET Core in-memory test infrastructure.
- PactNet supports `WithFileSource(new FileInfo(...))` for verifying local pact JSON files, which matches this story's file-based/no-broker requirement.
- PactNet has native runtime support limits by OS/architecture. CI should use supported Windows x64 or Linux x64/ARM64 runners and fail with a clear message otherwise.

### Scope Guardrails

Do not implement these in Story 10-3:

- Pact Broker, PactFlow, can-i-deploy, pending pacts, branch selectors, or hosted contract services.
- Browser-level Playwright contract tests unless a specific generated UI request cannot be covered through the Shell/EventStore adapters.
- Story 10-2 accessibility/visual specimen gates.
- Story 10-4 Stryker.NET mutation testing or FsCheck command idempotency suites.
- Story 10-5 flaky quarantine automation, reintroduction PRs, or CI diet governance.
- Story 10-6 LLM benchmark, signed releases, SBOM, symbol publishing, or provenance work.
- Broad EventStore API redesign, GraphQL/gRPC/OpenAPI generation, or a new transport abstraction.
- Public changes to `ICommandService`, `IQueryService`, `CommandResult`, `QueryRequest`, or `QueryResult<T>` solely to make Pact easier; any public API or EventStore contract change discovered by Pact must be escalated through a story update, follow-up story, or product/architecture approval.
- Recursive or nested submodule initialization.
- Live external EventStore, DAPR, Keycloak, cloud service, or network dependency in the default PR contract lane.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Provider verification may need a provider-state endpoint or fixture in the EventStore submodule if no current test host can expose deterministic states on TCP; Story 10-3 may add only minimal test-only provider-state hooks, otherwise it must record an exact blocked handoff. | Story 10-3 implementation handoff if blocked |
| Pact Broker / PactFlow workflow, branch selectors, and can-i-deploy, triggered only by multiple provider versions, external consumers, or cross-repo release coordination needs. | Future release-governance story after file-based pacts stabilize |
| Mutation and property-based command idempotency proof. | Story 10-4 |
| Flaky quarantine and CI duration governance. | Story 10-5 |
| Release signing, SBOM, and LLM benchmark provenance. | Story 10-6 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.3`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md#FR78`] - Pact consumer-driven contract requirement.
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md#NFR55`] - contract violations fail build.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Test-Infrastructure-Conventions`] - Pact anchored to Shell.Tests and provider verification expectations.
- [Source: `_bmad-output/planning-artifacts/architecture.md#EventStore-communication-contract`] - REST commands/queries, 202/200/304, ETag, ULID, tenant constraints.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L01`] - cross-story contract clarity.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06-L08`] - review/elicitation budget and testing-cost discipline.
- [Source: `_bmad-output/implementation-artifacts/10-1-adopter-test-host-and-component-testing-utilities.md`] - Testing package boundaries and Pact deferral.
- [Source: `_bmad-output/implementation-artifacts/10-2-accessibility-ci-gates-and-visual-specimen-verification.md`] - accessibility/visual scope boundary and Pact deferral.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`] - current command adapter behavior.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] - current query adapter and cache behavior.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`] - current response taxonomy.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`] - default endpoint paths and auth options.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs`] - command result contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`] - query request and ETag contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs`] - query result and 304 behavior.
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`] - provider command endpoint shape.
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs`] - provider query endpoint shape and ETag behavior.
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`] - provider command envelope contract and payload redaction.
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/SubmitQueryRequest.cs`] - provider query request contract.
- [Source: `Directory.Packages.props`] - current central package version style.
- [Source: `.github/workflows/ci.yml`] - current checkout, build, test, and artifact lanes.
- [Source: `git submodule status`] - root-level submodule pins for EventStore and Tenants.
- [Source: PactNet README](https://github.com/pact-foundation/pact-net) - current PactNet consumer/provider APIs and TCP provider-verification warning.
- [Source: PactNet 5 upgrade guide](https://docs.pact.io/implementation_guides/net/docs/upgrading-to-5) - PactNet 5 API changes.
- [Source: NuGet PactNet](https://www.nuget.org/packages/PactNet/) - current package version.

---

## Party-Mode Review

- ISO date and time: 2026-05-09T12:10:51+02:00
- Selected story key: `10-3-consumer-driven-contract-tests-pact`
- Command/skill invocation used: `/bmad-party-mode 10-3-consumer-driven-contract-tests-pact; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Provider-state ownership was too easy to defer; Story 10-3 needed an explicit state catalog, allowed test-only EventStore seam, and blocked handoff path before provider verification work starts.
  - Generated-adapter coverage needed to be named so hand-built JSON cannot pass while production route generation, serialization, headers, ETag behavior, or response mapping breaks.
  - CI contract gates needed concrete stale-pact, zero-interaction, skipped-verifier, missing-artifact, and job-order failure rules.
  - Redaction needed an enforceable secret/PII denylist rather than advisory artifact language.
  - Native verifier platform risk, root-level submodule behavior, Broker/PactFlow migration triggers, and public API mismatch escalation needed clearer implementation guardrails.
- Changes applied:
  - Added AC21-AC27 for provider-state catalog, generated adapter path, zero/unmatched interaction guards, CI gate order, redaction scanning, native verifier fallback, and mismatch escalation.
  - Hardened T1-T7 with stable pact naming, deterministic output, provider-state seam definition, negative header propagation tests, observable ETag/304 mapping, CI artifact/redaction checks, and final handoff evidence.
  - Added a `Contract Quality Gates` section covering provider-state ownership, generated adapter coverage, CI evidence, redaction, verifier platform control, and public/API mismatch escalation.
  - Updated scope guardrails and known gaps to keep Broker/PactFlow, public API changes, and EventStore test-harness work bounded.
- Findings deferred:
  - Detailed provider-state JSON/schema shape remains an implementation choice as long as AC21 and T4 are satisfied.
  - Pact Broker/PactFlow remains a future release-governance decision triggered only by multiple provider versions, external consumers, or cross-repo release coordination.
- Final recommendation: ready-for-dev

---

## Advanced Elicitation

- ISO date and time: 2026-05-09T13:02:47+02:00
- Selected story key: `10-3-consumer-driven-contract-tests-pact`
- Command/skill invocation used: `/bmad-advanced-elicitation 10-3-consumer-driven-contract-tests-pact`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records
- Findings summary:
  - Provider verification could report false confidence if startup races, port collisions, stale processes, static provider state, or retry/parallel bleed are not first-class failure cases.
  - Pact files could grow without clear ownership unless each interaction maps back to generated metadata, adapter path, provider state, acceptance criterion, and classifier expectation.
  - Redaction checks needed adversarial fixtures for encoded, query-string, ProblemDetails, local-path, and environment-shaped leaks, not only obvious bearer-token text.
  - CI evidence needed enough metadata to review release impact: provider version/commit, Pact spec metadata, manifest, stale-diff result, redaction result, and release-blocking status for handoffs.
  - The story should keep the provider-state catalog and decision record lightweight to avoid turning contract testing into a governance framework.
- Changes applied:
  - Added AC28-AC31 covering provider process/state isolation, interaction ownership metadata, adversarial redaction scanner tests, and PR/release evidence summary requirements.
  - Hardened T4-T7 with startup health probes, unique port/process cleanup, interaction manifest validation, redaction fixture coverage, provider/Pact metadata evidence, and a lightweight ADR note.
  - Expanded `Contract Quality Gates` and `Security and Redaction Requirements` with interaction ownership, provider process isolation, and encoded/disguised secret leak checks.
- Findings deferred:
  - Exact manifest file format remains an implementation choice; the required fields and CI behavior are now specified.
  - Exact ADR location remains an implementation choice as long as the decision and rejected alternatives are captured in story completion evidence or docs.
- Final recommendation: ready-for-dev

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet restore Hexalith.FrontComposer.sln` - passed.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Contract"` - passed, 3 contract tests.
- `pwsh ./eng/validate-contract-artifacts.ps1` - passed; redaction scan clean, 19 interactions validated.
- `git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact` - passed; generated pact artifacts match committed output.
- `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette"` - passed; default lane reported no failures.
- `dotnet build Hexalith.FrontComposer.sln --configuration Release` - passed with one MSB3101 SourceTools state-cache warning and 0 errors.
- `pwsh ./eng/validate-docs.ps1` - passed; evidence manifest written under `artifacts/docs/validation-manifest.json`.

### Completion Notes List

- 2026-05-09: Story created via `/bmad-create-story 10-3-consumer-driven-contract-tests-pact` during recurring pre-dev hardening job. Ready for later BMAD hardening.
- 2026-05-09T12:10:51+02:00: Party-mode review completed via `/bmad-party-mode 10-3-consumer-driven-contract-tests-pact; review;`.
  - Findings summary: Provider-state ownership, generated-adapter coverage, zero-interaction guards, CI stale-pact/artifact rules, redaction scans, verifier platform fallback, and public API mismatch escalation needed clearer pre-dev constraints.
  - Changes applied: Added AC21-AC27; hardened T1-T7; added Contract Quality Gates; preserved `ready-for-dev` because changes clarify implementation constraints without changing product scope.
  - Findings deferred: Exact provider-state catalog schema and future Pact Broker/PactFlow workflow remain implementation/follow-up decisions.
  - Final recommendation: ready-for-dev
- 2026-05-09T13:02:47+02:00: Advanced elicitation completed via `/bmad-advanced-elicitation 10-3-consumer-driven-contract-tests-pact`.
  - Batch 1 methods: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
  - Batch 2 methods: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
  - Changes applied: Added AC28-AC31; hardened T4-T7; added interaction ownership, provider process isolation, adversarial redaction fixtures, and release evidence metadata requirements.
  - Findings deferred: Exact interaction manifest format and ADR file location remain implementation choices.
  - Final recommendation: ready-for-dev
- 2026-05-10: Implemented Story 10-3 consumer-driven contract evidence.
  - Added PactNet 5.0.1 as a centrally pinned Shell test dependency and committed deterministic file-based pact artifacts under `tests/Hexalith.FrontComposer.Shell.Tests/Pact/`.
  - Added contract tests that exercise `EventStoreCommandClient` and `EventStoreQueryClient` through the existing adapter path, generating command, query, cache/ETag, and auth/tenant interactions with synthetic redacted fixtures.
  - Added `interaction-manifest.json`, `provider-state-catalog.json`, and `provider-verification-handoff.md`; provider verification is explicitly blocked/handoff because deterministic provider-state hooks and real TCP lifecycle checks belong in the `Hexalith.EventStore` provider host.
  - Added CI Gate 2c for consumer pact generation, artifact validation, stale pact diff detection, redaction scanning, artifact upload, and job-summary evidence without recursive nested submodule commands.
  - Added docs/reference guidance and a lightweight decision record for file-based Pact JSON, real-TCP provider verification, release blocking, troubleshooting, and rejected alternatives.
  - Evidence: PactNet package `5.0.1`; Pact specification metadata `4.0`; provider `Hexalith.EventStore`; provider command and blocked handoff in `provider-verification-handoff.md`; provider-state catalog in `provider-state-catalog.json`; interaction manifest count `19`; stale-pact result clean; redaction scan clean; CI lane `Gate 2c: Contract pacts`; artifacts under `artifacts/contracts/**`; public FrontComposer command/query contracts unchanged.

### File List

- `.github/workflows/ci.yml`
- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/10-3-consumer-driven-contract-tests-pact.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/reference/index.md`
- `docs/reference/pact-contracts.md`
- `eng/validate-contract-artifacts.ps1`
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/frontcomposer-eventstore-auth-tenant-propagation.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/frontcomposer-eventstore-cache-validation.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/frontcomposer-eventstore-command-dispatch.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/frontcomposer-eventstore-query-execution.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md`

### Change Log

- 2026-05-10: Added file-based Pact consumer contract generation, committed pact artifacts, provider-state/handoff evidence, CI contract gate, redaction validator, and adopter-facing documentation for Story 10-3.
