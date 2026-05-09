# FrontComposer Diagnostic Registry

Story 9-4 makes `diagnostic-registry.json` the authoritative source for HFC ownership, lifecycle, canonical help links, release-row metadata, runtime channel severity, and deprecation linkage. The validation entry point is the `DiagnosticRegistryTests` suite in `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics`, which loads this registry, analyzer descriptors, runtime constants, release rows, docs stubs, obsolete attributes, submodule boundaries, and compatibility evidence from checked-in files only.

The supported registry schema is exactly `1.0`. Newer or malformed schema versions fail closed with a named unsupported-schema category. Diagnostics docs slugs must remain `diagnostics/HFCxxxx`; encoded traversal, case variants, query strings, fragments, backslashes, whitespace, and zero-width characters are invalid.

Normal CI must not call live package feeds, GitHub, or the public docs site to validate this contract. Package/API compatibility gates use .NET package validation / ApiCompat for packable FrontComposer packages, with `PackageValidationBaselineVersion` set in `Directory.Build.props`, checked-in suppression evidence in `compatibility-suppressions.json`, and normalized reports so unavailable network/cache state cannot silently skip diagnostics governance.

The `samples/` folder contains stable blocking-report examples for registry drift, docs-stub drift, release-row drift, and compatibility drift. These examples intentionally avoid timestamps, absolute paths, machine names, SDK banners, and live feed URLs.
