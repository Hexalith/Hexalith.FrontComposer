# Contributing

## Source Generator Debugging

This section is contributor-only guidance for debugging the FrontComposer source generator. Adopter onboarding should stay focused on generated application behavior, generated files, diagnostics, and `frontcomposer inspect`.

Use `Debugger.Launch()` only in short-lived local investigation branches. Put it behind a narrow condition such as a specific generated type name or analyzer-config flag, then remove it before review. Source generators run inside the compiler host, so an unconditional launch prompt can block ordinary builds and IDE design-time builds.

For Visual Studio, use JIT attach or attach to the active compiler server process when the launch prompt appears. If the generator does not hit a breakpoint, run a clean rebuild with shared compilation disabled:

```powershell
dotnet build Hexalith.FrontComposer.sln -p:UseSharedCompilation=false
```

For Rider and VS Code with C# Dev Kit, generated-code inspection and generator-host attach behavior can differ by vendor version. Treat generator-host attach as a contributor workflow, not an adopter parity promise. The adopter-facing contract is generated output under `obj/{Config}/{TFM}/generated/HexalithFrontComposer`, HFC diagnostics, XML docs, and documented fallback inspection through `frontcomposer inspect`.

The compiler server can cache analyzer and generator assemblies. If a change appears stale, close the IDE design-time build, run `dotnet build-server shutdown`, then rebuild. When debugging generated output layout, validate both Debug and Release because Story 9-3 treats `obj/{Config}/{TFM}/generated/HexalithFrontComposer` as a public path contract.

Roslyn package pins are sensitive for IDE loading. `Hexalith.FrontComposer.SourceTools` targets `netstandard2.0` and pins Microsoft.CodeAnalysis.CSharp through central package management. Do not broaden Roslyn upgrades as part of generator debugging unless the story explicitly owns that compatibility work.
