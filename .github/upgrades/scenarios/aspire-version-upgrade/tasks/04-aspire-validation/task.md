# 04-aspire-validation: Run the Aspire validation gate

Validate that root `aspire.config.json` still points to `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`. Run appropriate Aspire CLI validation or inspection commands against the root AppHost, and confirm Docker remains available before any runtime validation that requires containers.

**Done when**: Aspire CLI validation results are recorded, the AppHost configuration path is confirmed, and any runtime validation outcome or limitation is documented.

## Research findings

- Scenario validation requires a build before runtime validation and Docker confirmation before container-dependent validation.
- Root `aspire.config.json` currently resolves the AppHost path to `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`.
- The AppHost project uses `Aspire.AppHost.Sdk/13.4.6` and targets `net10.0`, matching the scenario target.
- This task is atomic: it only requires configuration inspection and safe validation commands, so no decomposition is needed.
