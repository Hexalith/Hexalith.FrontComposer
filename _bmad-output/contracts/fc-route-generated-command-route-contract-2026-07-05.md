# FC-ROUTE Generated Command Route Contract

Status: approved for Story 11.7 implementation
Date: 2026-07-05
Owner: Architect + Product
Story: 11.0 - Command/projection route-contract decision gate

## Decision

The canonical generated command route family is:

```text
/commands/{BoundedContext}/{CommandTypeName}
```

This selects the route family already emitted for generated full-page command pages by SourceTools.
Story 11.7 must update every framework-owned command activation path to target this family.

Projection routes remain separate and unchanged:

```text
/{bounded-context-route}/{projection-route}
```

The historical command activation family below is not canonical:

```text
/domain/{bounded-context-kebab}/{command-type-kebab}
```

Story 11.7 must not create new palette, CTA, or generated command links that target `/domain/...`.
If compatibility for previously persisted recent routes is required, Story 11.7 may add a narrow
internal redirect or alias from the historical `/domain/...` shape to the canonical `/commands/...`
shape. Such compatibility must be treated as transitional and must not become the advertised route
contract.

## Canonical Route Shape

| Segment | Contract |
| --- | --- |
| Prefix | Literal `/commands` |
| Bounded context | The generated command page bounded-context route segment. Current SourceTools behavior uses `CommandRendererTransform` `SanitizeRouteSegment` over the bounded context, falling back to `Default` when no bounded context is declared. |
| Command type | The generated command page command-type route segment. Current SourceTools behavior uses `CommandRendererTransform` `SanitizeRouteSegment` over the command type name, not the fully qualified type name. |
| Casing | Preserve the generated route segment casing. Do not kebab-case command page routes for this contract. |
| Query string | Optional. Existing `returnPath` and `projectionTypeFqn` query parameters remain allowed when validated by the existing return-path and internal-route guards. |

Example:

```text
/commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
```

## Required Story 11.7 Implementation Targets

Story 11.7 must align all command activation surfaces with the canonical family:

- `CommandRouteBuilder.BuildRoute(...)`
- command palette command entries
- projection empty-state CTA resolution
- home/navigation command activation if it emits command targets
- generated full-page command links and route metadata
- tests and e2e pins that assert command activation lands on an existing generated page

Story 11.7 should centralize route construction so activation links and generated page routes cannot
drift again.

## Non-Goals

- Do not change projection route families in Story 11.7.
- Do not change EventStore HTTP command endpoints such as `POST /api/v1/commands` or
  `GET /api/v1/commands/status/{id}`.
- Do not rename command types, bounded contexts, generated output paths, MCP tool names, or schema
  fingerprints merely to satisfy route aesthetics.
- Do not hand-edit generated output.

## Evidence

- Generated command pages already register `/commands/{BoundedContext}/{CommandTypeName}` through
  `CommandRendererTransform.FullPageRoute`.
- Existing e2e and generated-command tests already navigate to `/commands/Counter/ConfigureCounterCommand`.
- The defect that triggered Story 11.0 is that palette and empty-state CTA command links target
  `/domain/{kebab}/{kebab}`, while no page resolves that family today.

## Handoff

Story 11.0 is complete at the decision level. Story 11.7 owns implementation of this contract and
must add the route-activation e2e pin before it can be marked done.
