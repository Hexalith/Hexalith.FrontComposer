# Hexalith FrontComposer CLI

`frontcomposer` is the command-line inspection and migration tool for Hexalith FrontComposer.

Use `frontcomposer inspect` to view generated source output under the deterministic
`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer` contract, and
`frontcomposer migrate --from <version> --to <version>` to preview or apply allowlisted
FrontComposer migration fixes.

Dry-run is the default migration mode. Source-writing migrations require `--apply`.

## Migration Output Notes

`migrate --format json` reports source paths as selected-project-relative paths whenever the CLI can
identify a project document. Apply-time failures use the planned source file's project-relative path.
Early planning failures that occur before any source document is identified, such as workspace
initialization failures, report the selected project file name instead.

The migration planner reads explicit `<Compile Include="...">` project items and the SDK-style
default `**/*.cs` shape used by FrontComposer fixtures. More complex MSBuild glob semantics, imports,
and item transforms are intentionally conservative in Story 9-2; files that are not resolved as
explicit project documents are not migrated.

## Manual-Only Migration Diagnostics (HFCM9002)

Manual-only migration diagnostics (HFCM9002) are detected by reading
`*.diagnostics.json` sidecar files written by the SourceTools generator under
`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`. **In Story 9-2 the
sidecar reader is wired to the synthetic test fixture only — there is no
production SourceTools emitter that writes these sidecars yet. AC11 ("a migration
diagnostic has no safe automated fix") fires today only against hand-crafted
fixtures.** Story 9-4 owns the final HFC ID assignment and will add the real
generator emitter so AC11 fires for adopter code.

Sidecar files that fail to parse or read are surfaced as a single sentinel
manual-only entry per file rather than being silently dropped, so corrupted
sidecars are visible in the migration output.

On SDKs that provide `dnx`, `dnx frontcomposer ...` can be used as a convenience after the package
is available from a feed. Local tool manifests and `dotnet tool install` remain the primary
installation paths.
