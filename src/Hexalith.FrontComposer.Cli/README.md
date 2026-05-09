# Hexalith FrontComposer CLI

`frontcomposer` is the command-line inspection and migration tool for Hexalith FrontComposer.

Use `frontcomposer inspect` to view generated source output under the deterministic
`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer` contract, and
`frontcomposer migrate --from <version> --to <version>` to preview or apply allowlisted
FrontComposer migration fixes.

Dry-run is the default migration mode. Source-writing migrations require `--apply`.

On SDKs that provide `dnx`, `dnx frontcomposer ...` can be used as a convenience after the package
is available from a feed. Local tool manifests and `dotnet tool install` remain the primary
installation paths.
