# FC-AUTH v1 Policy-Gated Command Authorization Contract

Date: 2026-06-04

## Scope

FC-AUTH v1 defines how FrontComposer command policy metadata is parsed, surfaced, presented, and enforced for generated and direct command dispatch paths.

## Command Policy Metadata

- A command may declare at most one `[RequiresPolicy]` attribute.
- The policy name is a host-owned stable identifier. It is not tenant data, user data, customer data, claim data, command payload data, or a server token.
- Policy names are trimmed and must contain only letters, digits, `.`, `:`, `_`, or `-`, with at least one alphanumeric character.
- Invalid policy metadata is a build-time failure: duplicate metadata emits HFC1057, invalid policy values emit HFC1056, and no usable command model is produced for the invalid command.

## Presentation Decision Matrix

| Decision | Presentation behavior |
| --- | --- |
| `Pending` | Render the pending branch or generated checking-permission feedback. |
| `Allowed` | Render the authorized branch and enable protected command affordances. |
| `Denied` | Render the not-authorized branch or opaque unavailable feedback. |
| `FailedClosed` | Render the not-authorized branch or opaque unavailable feedback. |

Missing or blank policy metadata, missing authorization state, stale tenant/auth state, evaluator exceptions, evaluator cancellation, and null evaluator results fail closed. These failures must not tear down the Blazor circuit.

## Dispatch Rule

Protected commands must pass `ICommandDispatchAuthorizationGate` before any Stub, EventStore, lifecycle, HTTP, FC-CNC admission, pending-state, or command side effect. Decisions other than `Allowed` block dispatch.

Unprotected commands short-circuit with `NoPolicy` and do not require host policy evaluation.

User-visible denial payloads are opaque. They must not expose policy names, command fully qualified names, tenant/user claims, command payloads, or server tokens.

## Configuration and Catalog Validation

Hosts own real authorization policy registration and handlers.

Generated manifests declare command policy metadata through `DomainManifest.CommandPolicies`. `KnownPolicies` is the host-owned catalog input used by `FrontComposerAuthorizationPolicyCatalogValidator`.

When protected commands exist and the catalog is incomplete:

- An empty `KnownPolicies` catalog emits a startup warning.
- Missing catalog entries emit startup warnings when `StrictPolicyCatalogValidation` is `false`.
- Missing catalog entries fail startup when `StrictPolicyCatalogValidation` is `true`.

Catalog diagnostics may include policy names because they are host-owned identifiers. They must not include command fully qualified names, command payloads, tenant/user claims, or tokens.

## Non-Goals

FC-AUTH v1 does not introduce a custom authorization framework, queue/retry/degraded policy behavior, FC-CNC behavior changes, MCP tool admission changes, command identity changes, or a third-party UI/toast package.
