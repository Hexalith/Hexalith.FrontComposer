# FC-MCP-RESOURCES v1 Contract

Date: 2026-06-05
Story: 5.3 Projection and skill-corpus resources
Status: v1 implementation contract

## Scope

FC-MCP-RESOURCES v1 defines the MCP resource surface exposed by `AddFrontComposerMcp(...)`.
It covers tenant-scoped projection reads and framework-global skill-corpus reads. Command tools,
lifecycle tools, schema-fingerprint negotiation redesign, new authorization frameworks, resource
streaming, URI templates, package upgrades, and new public skill-corpus authoring beyond test fixes
are out of scope.

## Projection Resource URI Grammar

The v1 canonical grammar is the live generated-manifest grammar:

```text
frontcomposer://<bounded-context>/projections/<projection-name>
```

The epic shorthand `frontcomposer://<context>/<projection>` is descriptive only. v1 does not
silently normalize that shorthand and does not add alternate projection URI forms. Matching is by
the exact descriptor URI string advertised in the MCP resource, and resource templates are not
supported.

Projection descriptors advertise `text/markdown` through `ProtocolResource`. Reads return
tenant-scoped Markdown rendered by `McpMarkdownProjectionRenderer`. The query request tenant is
always the authenticated agent context tenant; client-supplied tenant/user identity is not accepted
as resource input.

## Skill Resource URI Grammar

Skill resource URIs are canonical lowercase exact strings under:

```text
frontcomposer://skills/<id-or-path>
frontcomposer://skills/manifest
```

Per-skill resources are loaded from embedded `docs/skills/frontcomposer/**/*.md` documents and
serve only the single `agent-reference` section as `text/markdown`. Narrative sections are not
served. Each per-skill descriptor carries the parsed skill descriptor as metadata and includes a
schema fingerprint that covers metadata plus the served Markdown body digest.

`frontcomposer://skills/manifest` is a deterministic aggregate Markdown manifest. It includes
`manifestSchemaVersion`, `corpusVersion`, `resourceCount`, and each resource's id, URI, source,
version, owning story, migration owner, public API references, and sample paths when present.

## Bounds and Failure Tokens

Projection resources use `FrontComposerMcpOptions` render bounds for rows, fields, cells, timeline
entries, status groups, suggestions, and document characters. A document-budget failure returns the
sanitized `ResponseTooLarge` projection taxonomy with no partial Markdown.

Skill resources use a default 32 KB read cap. Oversized skill resources return
`SkillResourceTooLarge` internally and the public `response_too_large` token. They are not
truncated.

Projection read failures return the existing sanitized MCP projection failure taxonomy. Malformed,
unknown, tenant-hidden, auth/tenant-invalid, stale descriptor, schema-incompatible, canceled, timed
out, unsupported-render, downstream, degraded, and response-too-large cases must not leak tenant IDs,
user IDs, raw URIs, JWT fragments, query exception messages, descriptor internals, command args, or
raw payloads.

Skill read failures use stable public text tokens:

- `unknown_resource` for unknown, auth-equivalent, tenant-equivalent, and hidden-equivalent reads
- `malformed_request` for blank or malformed skill resource requests
- `canceled` for cancellation
- `response_too_large` for oversized skill resources

## Security Split

Projection resources are tenant-scoped and fail closed:

- `IFrontComposerMcpResourceVisibilityGate` is mandatory at startup.
- The same gate instance is resolved once per read.
- Visibility is checked at admission, before query, and before render.
- Query execution starts only after auth context, tenant validation, descriptor lookup,
  visibility, epoch, and schema gates pass.
- Visibility loss before query or before render returns the hidden-equivalent projection failure and
  never renders partial Markdown.

Skill resources are framework-global reference material:

- They intentionally bypass `IFrontComposerMcpResourceVisibilityGate`.
- They do not query tenant data.
- They are loaded from embedded docs and served from the skill provider.
- Projection descriptors may not use the reserved `frontcomposer://skills/` URI namespace.

## Registration Binding

`AddFrontComposerMcp(...)` registers both manifest projection resources (`FrontComposerMcpResource`)
and skill-corpus resources (`FrontComposerSkillMcpResource`) with the MCP SDK via
`.WithResources(...)`. Resources advertise `text/markdown`, carry their descriptors in `Metadata`,
match exact canonical URI strings, and throw `NotSupportedException` for resource templates.

Projection URI collisions with skill resources, including `frontcomposer://skills/manifest`, are
startup failures. The entire `frontcomposer://skills/` prefix is reserved for skill resources so a
bounded context named `skills` cannot become a projection namespace.
