param(
  [int] $MaxTest = 1000,
  [string] $Replay = "",
  [string] $ResultsDirectory = "TestResults/property"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function New-ReplaySeed {
  $bytes = [byte[]]::new(16)
  [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
  $seed = [System.BitConverter]::ToUInt64($bytes, 0)
  $gamma = [System.BitConverter]::ToUInt64($bytes, 8) -bor 1
  return "$seed,$gamma,0"
}

if ([string]::IsNullOrWhiteSpace($Replay)) {
  $Replay = New-ReplaySeed
}

$env:FC_PROPERTY_MAX_TEST = [string] $MaxTest
$env:FC_PROPERTY_REPLAY = $Replay
$env:DiffEngine_Disabled = "true"
$filter = if ($MaxTest -ge 10000) {
  "Category=NightlyProperty"
} else {
  "Category=LifecycleIdempotency&Category!=NightlyProperty"
}

$summaryDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "artifacts/property"
New-Item -ItemType Directory -Force -Path $summaryDir | Out-Null
@(
  "## Lifecycle Property Run",
  "",
  "- FsCheck package: FsCheck.Xunit.v3 3.3.1",
  "- MaxTest: $MaxTest",
  "- Replay seed: $Replay",
  "- Filter: $filter",
  "- Results: $ResultsDirectory",
  "- Replay command: pwsh ./eng/run-lifecycle-property-suite.ps1 -MaxTest $MaxTest -Replay `"$Replay`" -ResultsDirectory `"$ResultsDirectory`""
) | Set-Content -LiteralPath (Join-Path $summaryDir "property-seed-summary.md") -Encoding utf8

dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj `
  --configuration Release `
  --filter $filter `
  --results-directory $ResultsDirectory `
  --logger "trx;LogFileName=lifecycle-property.trx"
