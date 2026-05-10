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
$endSize = if ($MaxTest -ge 10000) { 96 } else { 64 }
$replayCommand = "pwsh ./eng/run-lifecycle-property-suite.ps1 -MaxTest $MaxTest -Replay `"$Replay`" -ResultsDirectory `"$ResultsDirectory`""
$operationDistribution = [ordered]@{
  submit = "token % 10 == 0"
  acknowledge = "token % 10 == 1"
  syncing = "token % 10 == 2"
  confirmed = "token % 10 == 3"
  rejected = "token % 10 == 4"
  duplicateTerminal = "token % 10 == 5"
  reconnectObservation = "token % 10 == 6"
  retryObservation = "token % 10 == 7"
  staleObservation = "token % 10 == 8"
  resetToIdle = "token % 10 == 9"
}

$summaryDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "artifacts/property"
New-Item -ItemType Directory -Force -Path $summaryDir | Out-Null
@{
  fsCheckPackage = "FsCheck.Xunit.v3 3.3.1"
  replaySeed = $Replay
  size = 0
  maxSize = $endSize
  sequenceCount = $MaxTest
  filter = $filter
  resultsDirectory = $ResultsDirectory
  replayCommand = $replayCommand
  generatedOperationDistribution = $operationDistribution
  shrinkResult = "none-for-passing-run; on failure, retain the FsCheck shrink output from TRX/console and convert confirmed bugs to command-idempotency-counterexamples.json"
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $summaryDir "property-run-evidence.json") -Encoding utf8

@(
  "## Lifecycle Property Run",
  "",
  "- FsCheck package: FsCheck.Xunit.v3 3.3.1",
  "- MaxTest: $MaxTest",
  "- MaxSize: $endSize",
  "- Replay seed: $Replay",
  "- Filter: $filter",
  "- Results: $ResultsDirectory",
  "- Generated operation distribution: uniform token modulo 10 across submit, acknowledge, syncing, confirmed, rejected, duplicate terminal, reconnect observation, retry observation, stale observation, and reset/idling",
  "- Shrink result: none for a passing run; failing runs must retain FsCheck shrink output and fixture conversion evidence",
  "- Replay command: $replayCommand"
) | Set-Content -LiteralPath (Join-Path $summaryDir "property-seed-summary.md") -Encoding utf8

dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj `
  --configuration Release `
  --filter $filter `
  --results-directory $ResultsDirectory `
  --logger "trx;LogFileName=lifecycle-property.trx"
