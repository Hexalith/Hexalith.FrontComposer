# 03-build-solution: Build the solution and fix introduced errors

Build `Hexalith.FrontComposer.slnx` after the AppHost SDK consolidation. Fix only errors introduced by the Aspire CLI/AppHost format/package cleanup changes.

Keep the scope root-repository focused; do not modify referenced submodule projects unless the build proves a root integration fix is impossible without doing so.

**Done when**: the solution build succeeds or any accepted limitation is documented with evidence that it was not introduced by the Aspire modernization work.

## Worker research

- Scenario target is Aspire `13.4.6`; the AppHost already targets `net10.0` and no TFM upgrade is in scope.
- Scope is root-focused: prefer fixes in `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`, `Directory.Packages.props`, `aspire.config.json`, or generated upgrade artifacts; referenced submodules remain out of scope unless a root integration fix is impossible.
- Task 01 verified the installed Aspire CLI reports `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`.
- Task 02 changed the AppHost to root SDK form `Sdk="Aspire.AppHost.Sdk/13.4.6"`, removed redundant `Aspire.Hosting.AppHost`, preserved `Aspire.Hosting.Keycloak`, and validated the AppHost project build successfully.
- Build-validation guidance selects `dotnet build` for SDK-style modern .NET projects unless diagnostics prove full Visual Studio MSBuild is required.

## Decomposition decision

No decomposition needed. This task is atomic: build the solution, diagnose any introduced build errors, apply root-focused fixes only if needed, and document the result.
