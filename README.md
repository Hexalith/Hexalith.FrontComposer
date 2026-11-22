# Hexalith.FrontComposer
FrontComposer generates Blazor forms, projection views, and command lifecycle feedback from annotated .NET domain types.

## Run the Counter sample

Start with the [first projection tutorial](docs/tutorials/getting-started.md) for the direct-submodule checkout, SDK, build, sample URL, and CLI inspection commands. The sample uses a simulated command service. For integration with EventStore, follow the [adopter bootstrap proof](docs/how-to/adopter-bootstrap-proof.md). The [documentation index](docs/index.md) covers authoring, migration, and reference material.

## Dependency source selection

The superproject records each direct dependency revision in Git. The [dependency policy](eng/dependency-graph-policy.json) binds those revisions to the consumer source projection, Dapr compile inputs, and root selectors. Use `git submodule status` to inspect a checkout. A leading `+` identifies a working revision that differs from its recorded pin. The SDK selection and published package versions have their own manifests. See [dependency governance](eng/dependency-governance.md) for the qualification and activation procedure.

The pointer synchronization on 2026-11-06 initially selected a different
projection without its policy proof, and CI refused it. The corrective commit
`b08612337b791fef0d819094d6c345c3d8ab4562` restored six dependency selectors
from the last fully green source `aa8578318971da04ce7d7bd07928d08c6b95bc1a`.
That historical restoration is preserved. The maintenance refresh now records
the current committed selections and matching seals. Nested checkouts remain
uninitialized and governance guards remain enabled.

CI evaluates both the actual previous source and candidate under the previous
source policy. This reseal requires a separate later-base activation push; it
cannot authorize itself. Current qualification requires fresh CI results and
does not inherit runtime, operator, deployment or publication acceptance from
the historical source pins or local checks.

## Legacy compatibility surfaces

These are versioned compatibility binaries. They are not retired and stay outside every Hexalith.Platform composition.

- `Hexalith.FrontComposer.Cli` (.NET tool `frontcomposer`): removal gate "Retained local compiler exemption under merged AI.Tools plan 1.2.0; no retirement or Gateway replacement" ([technical-tools inventory 1.0.4](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/technical-tools-migration-inventory-v1.0.4.md) PS03; [FrontComposer inventory 1.1.0](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/frontcomposer-migration-inventory-v1.1.0.md)). Retained local compiler tooling under merged AI.Tools plan 1.2.0; no agent exposure and never counted as retired. Its build/freshness, apply/drift/path, partial-write and output privacy gates remain independent. Technical inventory SHA-256: `94985d72232864ab8f40ba212cd0fc73dbd0c5497856073cfd19c2972b89e8b8`. FrontComposer inventory SHA-256: `60c44b5c7c67ca315bfe4f7582ea7e50526dc2cfeebbf12838f4b0c71543ff63`.
- `Hexalith.FrontComposer.Mcp` (HTTP MCP host library): removal gate "G4: retire together with the Projects plug-in after every host consumer is accounted for" ([technical-tools inventory 1.0.4](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/technical-tools-migration-inventory-v1.0.4.md) PS04; [FrontComposer inventory 1.1.0](https://github.com/Hexalith/Hexalith.McpCli/blob/main/_bmad-output/implementation-artifacts/frontcomposer-migration-inventory-v1.1.0.md)). The FrontComposer host and the Projects plug-in (`Hexalith.Projects.Mcp`) retire together. Technical inventory SHA-256: `94985d72232864ab8f40ba212cd0fc73dbd0c5497856073cfd19c2972b89e8b8`. Coupled gate: FK01–FK04 consumers move first; K12 Web/Chatbot UI evidence, Projects 6.7 cutover, DW-82 publication and live/release/rollback acceptance remain required; K13 compiler is retained independently. FrontComposer inventory SHA-256: `60c44b5c7c67ca315bfe4f7582ea7e50526dc2cfeebbf12838f4b0c71543ff63`.

## Shared McpCli access (reconciliation candidate)

Generated command tools, projection/skill resources and lifecycle subscriptions below are historical migration sources. Eligible business data requires an owner-decorated Gateway Query; K07 documentation and K08 synthetic lifecycle are withdrawn. The K13 inspect/migrate compiler is retained local tooling under merged AI.Tools plan 1.2.0, never retired or migrated to generic diagnostics. FK01–FK04 and G4 still couple host and Projects plug-in retirement.

For the verified 2.0.1 examples, install `dotnet tool install --global Hexalith.McpCli --version 2.0.1`.
Run `hexalith modules`, `hexalith operations tenants` or
`hexalith operations parties`, and `hexalith describe tenants.list-tenants`
for offline discovery of the installed cohort. Start the MCP server with
`hexalith mcp --transport stdio`; it offers five generic tools, never a tool or
CLI subcommand per operation. Configure a McpCli profile with the documented
`hexalith config --help` flow before authorized `query` or `send` submission.
Do not copy the Admin `profiles.json`. Offline Catalog discovery proves neither
Gateway authorization, live availability nor retirement. A missing operation
fails safely; confidential human actions hand off to the owning UI, never to
an alternative module endpoint. No Gateway call is needed for this setup check.

