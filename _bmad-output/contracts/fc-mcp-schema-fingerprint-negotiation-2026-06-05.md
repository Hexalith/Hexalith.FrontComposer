# FC-MCP-SCHEMA v1 Schema Fingerprint Negotiation Contract

Date: 2026-06-05
Owner: FrontComposer MCP surface
Status: v1 implementation contract

## Scope

FC-MCP-SCHEMA v1 defines how FrontComposer MCP command and projection resource requests negotiate
schema fingerprints before side effects. It applies to generated command `tools/call`, projection
resource reads, generated command/resource descriptors, runtime aggregate manifest integrity, and
bounded schema diagnostics.

The contract is defensive by default: visible generated commands and visible projection resources
may expose sanitized schema failures, but hidden, unauthorized, tenant-hidden, policy-hidden, and
unknown tools or resources keep hidden-equivalent public responses.

## Header Wire Form

Clients may send at most one `x-frontcomposer-schema-fingerprint` HTTP header. The accepted wire
value is:

```text
algorithmId:64-lowercase-hex
```

The algorithm identifier must contain no whitespace and the fingerprint value must be exactly 64
lowercase hexadecimal characters. Empty and whitespace-only header values are treated as no client
hint. The accessor caches successful parse results, null results, and malformed failures for the
request lifetime so repeated admission, invocation, pre-query, or pre-render reads observe the same
decision.

Malformed, multi-valued, oversized, too-short, unsupported-algorithm, uppercase-hex, non-hex,
extra-colon, or whitespace-inside-algorithm values fail closed as `MalformedRequest`. Public
responses and logs must not echo the raw header value.

## Supported Algorithms

FC-MCP-SCHEMA v1 supports exactly these algorithm identifiers:

- `frontcomposer.schema.sha256.canonical-json.v1`
- `frontcomposer.schema.sha256.v1.sourcetools-blob`

`McpSchemaNegotiator` accepts both in v1 because generated descriptors currently carry
SourceTools-emitted fingerprints while runtime material may use canonical JSON fingerprints.
Changing this set requires updating the HTTP trust-boundary parser, negotiator, emitted descriptor
compatibility, and tests together.

## Decision Taxonomy

`McpSchemaNegotiator` classifies a client/server pair into stable bounded result kinds:

- `Exact`
- `CompatibleAdditive`
- `CompatibleWarning`
- `Incompatible`
- `UnknownClientVersion`
- `UnknownServerBaseline`
- `HiddenOrUnknown`
- `StaleDescriptor`
- `UnsupportedAlgorithm`
- `Unavailable`
- `SchemaIntegrityMismatch`

`Exact`, `CompatibleAdditive`, and `CompatibleWarning` allow side effects to continue. Command calls
that proceed still run current-server argument validation, server-controlled field rejection,
server-side identity allocation, derivable injection, DataAnnotations/current-contract validation,
dispatch, and lifecycle acknowledgement in that order.

`Incompatible`, `UnknownClientVersion`, `UnknownServerBaseline`, `StaleDescriptor`,
`UnsupportedAlgorithm`, `Unavailable`, `SchemaIntegrityMismatch`, and any future non-side-effect-safe
schema category block side effects. Generated command calls must stop before command construction,
ULID allocation, derivable injection, lifecycle tracking, or dispatch. Projection reads must stop
before query dispatch, renderer allocation, rendering, or cache writes.

## Precedence Order

Schema negotiation follows this order:

1. Hidden or unknown tool/resource equivalence.
2. Stale descriptor detection.
3. Schema integrity mismatch.
4. Server fingerprint presence.
5. Server algorithm support.
6. Client fingerprint presence.
7. Client algorithm support.
8. Trusted baseline availability unless byte-identical hashes prove exact compatibility.
9. Snapshot structural compatibility.
10. Fail-closed incompatible fallback.

Hidden-equivalent security has higher precedence than schema diagnostics. Auth failures,
tenant-hidden tools/resources, policy-hidden tools/resources, and unknown names must not reveal that
schema negotiation could have produced a mismatch, unsupported algorithm, stale descriptor, or
integrity category.

## Compatibility Authority

Byte-identical client/server fingerprints with the same supported algorithm may short-circuit to
`Exact` when no structural snapshot decision is available.

Otherwise, additive, warning, and breaking decisions must come from existing
`SchemaBaselineSnapshot` baseline/server inputs and `SchemaMigrationDeltaAnalyzer`. The legacy
`HasCompatibleAdditiveDrift` input is obsolete and ignored; callers must not supply or trust a
boolean compatibility override.

Runtime aggregate integrity compares generated descriptor fingerprints and registered
`ISkillCorpusFingerprintProvider` outputs where the live design supports runtime providers.
Zero-provider hosts remain valid when they ship no skill corpus. Tampered nested descriptor
fingerprints fail closed with `SchemaIntegrityMismatch`.

## Sanitized Payloads And Logs

Agent-visible structured content and structured logs must use bounded stable categories, message
keys, and docs codes such as `schema-mismatch`, `schema-unavailable`,
`unsupported-schema-fingerprint`, `unknown-version`, `schema-compatible-warning`, and
`unknown_resource`.

They must never include raw client or server fingerprint values, tenant IDs, user IDs, policy names,
descriptor internals, hidden tool/resource names, command arguments, raw resource URIs with query or
fragment data, exception messages, stack traces, API keys, bearer tokens, or raw payloads.

## Non-Goals

FC-MCP-SCHEMA v1 does not introduce package upgrades, MCP SDK transport changes,
`CanonicalSchemaMaterial` changes, baseline regeneration, a new authentication or authorization
model, resource URI grammar changes, lifecycle wire-shape changes, command identity changes,
command batching, an MCP retry loop, a caller-supplied compatibility boolean, or changes to
`SchemaBaselineProvenance` safe identifiers.
