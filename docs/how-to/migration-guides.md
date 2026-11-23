---
title: "Apply migration guidance"
description: "Use migration-guide requirements when FrontComposer APIs or shipped skill corpus examples change."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-07-11
uid: frontcomposer.how-to.migration-guides
slug: how-to/migration-guides/
fromVersion: "9.1.0"
toVersion: "9.2.0"
diagnosticId: "HFCM9001"
skillCorpusImpact: "review-required"
codeFixAvailable: false
---

# Apply migration guidance

Migration pages are required when a shipped skill corpus example breaks, even when the semantic-version bucket is minor.

Old code:

```csharp no-compile reason="Obsolete API shown only as migration input."
services.AddFrontComposerDebugOverlay();
```

New code:

```csharp no-compile reason="Host setup snippet requires the application service collection."
services.AddFrontComposerDevMode();
```

Why it changed: the CLI migration contract moved migration reports to dry-run/apply semantics, and the HFC diagnostic governance contract reserves the related rows.

Affected packages: `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`.

The `9.1.0` to `9.2.0` edge is `cli-supported`: the CLI-only `HFCM9001` safe fix is an allowlisted
syntactic rename of `IdentifierNameSyntax` nodes named `AddFrontComposerDebugOverlay` to
`AddFrontComposerDevMode`, excluding identifiers inside `nameof`. This is not semantic validation:
it can rename same-named non-API identifiers, including variable references and method groups.
Review the diff before applying it. No IDE analyzer/code fix is published (`codeFixAvailable: false`).
`HFCM9002` remains synthetic/manual-only; production emission is not approved and normal builds do
not produce those migration sidecars.

For customization diagnostics, follow the [Customization prerequisites](../reference/components/customization.md)
and [customization cookbook](../how-to/customization-gradient-cookbook.md). The Shell assembly itself
must be compiled with `DEBUG`, and DI must contain an `IHostEnvironment` explicitly identifying
Development. Register logging and the environment before `AddFrontComposerDevMode()`; the
parameterless overload recognizes instance/factory environment registrations, or use the explicit
environment overload while keeping `IHostEnvironment` registered in DI. The published Release
NuGet package does not emit these customization diagnostics/panels, even for a Debug adopter in
Development.

Use the [classified migration index](../migrations/index.md) to select an exact edge. A
`manual-only` package/API guide does not enable CLI execution of that edge. Explicit versions
are required; the CLI does not infer an upgrade chain.

```shell
dotnet tool run frontcomposer migrate --project Acme.Orders.csproj --from 9.1.0 --to 9.2.0 --dry-run --format json
```

Dry-run is the default and writes nothing. Review the `frontcomposer.cli.migrate.v1` report,
then replace `--dry-run` with `--apply` to write eligible source changes. Each file is replaced
atomically through a same-directory temporary file after revalidating its path and source hash.
This is per-file atomicity: earlier successful writes remain after a later failure or cancellation;
there is no whole-run rollback. Handled write failures return code 4 and set `applied=false` on
emitted reports. Cancellation during planning can return code 4 before a report is emitted.
If cancellation stops apply, the report lists completed writes and each planned file left
unwritten. Migration sidecars support arrays of diagnostic objects or objects containing a
`diagnostics` array. Malformed payloads and directory read failures appear as manual-only
sentinels in the report. Other readable diagnostics are still included. If their source is
unavailable to the migration scan, the report asks you to regenerate the diagnostics.

Generated output, `bin`, `obj`, `.git`, package caches, submodules and other nested repositories, paths outside the project,
file symlinks, and linked ancestor directories below the project directory (including junctions) are ineligible. Paths below the selected project directory are ineligible when they are generated output, nested repositories, file symlinks, or linked directories. Explicit `Compile` Link metadata does not authorize an outside-root or
linked write. The planner refuses
unsafe documents before source reads and rechecks safety at apply time. Complex MSBuild globs
and imported Compile items are not fully evaluated. Source reads support UTF-8 (with or without
BOM), UTF-16 LE/BE, and UTF-32 LE/BE; unknown encodings and files over 16 MiB are refused.

For inspection without migration, use `inspect --format json` and its
`frontcomposer.cli.inspect.v1` contract. See [CLI reference](../reference/cli.md) for generated
families, ordering, filters, sidecar handling, bounded output, and path opt-in. Sidecars support
arrays of diagnostic objects or objects containing a `diagnostics` array of objects. Parse errors,
read errors, and unsupported JSON shapes produce an `HFCM0002` warning for the affected sidecar.
If the generated directory cannot be enumerated, inspection exits with code 3.

Skill-corpus evidence: `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` remains the producer input.

Related links: [CLI inspect and migrate](../reference/cli.md), [diagnostics](../reference/diagnostics/index.md), and [executable migration guide](../migrations/9.1-to-9.2.md).

For the Contracts/Contracts.UI package split, runtime/testing ownership moves, and composed-query
migration, use the [FrontComposer 1.12 to 2.0 guide](../migrations/1.12-to-2.0.md). That major-version
guide is manual: HFC0001 identifies flattened query usage, while package/namespace moves require an
explicit reference and source audit.
