param(
  [string] $ManifestPath = "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json",
  [string] $ReportRoot = "artifacts/mutation",
  [string] $OutputPath = "artifacts/mutation/job-summary.md",
  [switch] $AllowMissingReports
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ManifestFullPath = Join-Path $RepoRoot $ManifestPath
$ReportRootFullPath = Join-Path $RepoRoot $ReportRoot
$OutputFullPath = Join-Path $RepoRoot $OutputPath
$errors = New-Object System.Collections.Generic.List[string]
$summaryLines = New-Object System.Collections.Generic.List[string]

function Add-Failure([string] $Message) {
  $errors.Add($Message) | Out-Null
}

function ConvertTo-RepoPath([string] $Path) {
  $full = [System.IO.Path]::GetFullPath($Path)
  $root = [System.IO.Path]::GetFullPath($RepoRoot)
  if (!$full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Path escapes repository root: $Path"
  }

  return $full.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Replace('\', '/')
}

function Read-Json([string] $Path) {
  Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 100
}

function Normalize-ArtifactText([string] $Text) {
  $normalizedRoot = [regex]::Escape($RepoRoot)
  $text = [regex]::Replace($Text, $normalizedRoot, ".", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $text = [regex]::Replace($text, 'C:\\Users\\[^\\]+', '<local-user>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $text = [regex]::Replace($text, '/home/[^/\s]+|/Users/[^/\s]+', '<local-user>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  return $text
}

function Get-JsonValuesByName($Node, [string] $Name) {
  $values = New-Object System.Collections.Generic.List[object]
  if ($null -eq $Node) { return $values }

  if ($Node -is [System.Management.Automation.PSCustomObject]) {
    foreach ($property in $Node.PSObject.Properties) {
      if ($property.Name -eq $Name) {
        $values.Add($property.Value) | Out-Null
      }

      foreach ($child in Get-JsonValuesByName $property.Value $Name) {
        $values.Add($child) | Out-Null
      }
    }
  }
  elseif ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) {
    foreach ($item in $Node) {
      foreach ($child in Get-JsonValuesByName $item $Name) {
        $values.Add($child) | Out-Null
      }
    }
  }

  return $values
}

function Test-UnsafeText([string] $Text) {
  if ([string]::IsNullOrEmpty($Text)) { return $null }
  if ($Text -match 'C:\\Users\\|/home/[^/\s]+|/Users/[^/\s]+') { return "machine-local path" }
  if ($Text -match '(?i)(bearer\s+[a-z0-9._-]{12,}|api[_-]?key\s*[:=]|password\s*[:=]|secret\s*[:=]|token\s*[:=])') { return "secret-like value" }
  if ($Text -match '(?i)tenant[_-]?id\s*[:=]\s*["'']?[0-9a-f-]{16,}') { return "tenant identifier" }
  return $null
}

function Get-MutatedFiles($Report) {
  $items = New-Object System.Collections.Generic.List[object]

  if ($null -ne $Report.files -and $Report.files -is [System.Management.Automation.PSCustomObject]) {
    foreach ($property in $Report.files.PSObject.Properties) {
      $statuses = @()
      if ($null -ne $property.Value.mutants) {
        $statuses = @($property.Value.mutants | ForEach-Object { [string] $_.status })
      }

      $items.Add([pscustomobject]@{
        Path = ([string] $property.Name).Replace('\', '/')
        Statuses = $statuses
      }) | Out-Null
    }
  }

  return $items
}

if (!(Test-Path -LiteralPath $ManifestFullPath)) {
  throw "Missing mutation target manifest: $ManifestPath"
}

$manifest = Read-Json $ManifestFullPath
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFullPath) | Out-Null

$approvedRoots = @($manifest.approvedTargetRoots | ForEach-Object { [string] $_ })
$segments = @($manifest.segments)
$allTargetFiles = New-Object System.Collections.Generic.HashSet[string]
$combinedConfigText = ""

foreach ($root in $approvedRoots) {
  $fullRoot = Join-Path $RepoRoot $root
  if (!(Test-Path -LiteralPath $fullRoot)) {
    Add-Failure "Approved target root missing: $root"
    continue
  }

  Get-ChildItem -LiteralPath $fullRoot -Recurse -File -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -notmatch '\.(g|generated)\.cs$' } |
    ForEach-Object { [void] $allTargetFiles.Add((ConvertTo-RepoPath $_.FullName)) }
}

foreach ($segment in $segments) {
  $configPath = Join-Path $RepoRoot ([string] $segment.config)
  if (!(Test-Path -LiteralPath $configPath)) {
    Add-Failure "Missing Stryker config for segment '$($segment.name)': $($segment.config)"
    continue
  }

  $combinedConfigText += "`n" + (Get-Content -LiteralPath $configPath -Raw)
  $config = Read-Json $configPath
  foreach ($field in @("solution", "project", "reporters", "coverage-analysis", "thresholds", "mutate")) {
    if ($null -eq $config."stryker-config".$field) {
      Add-Failure "Stryker config '$($segment.config)' is missing '$field'."
    }
  }

  foreach ($reporter in @("progress", "html", "json")) {
    if (@($config."stryker-config".reporters) -notcontains $reporter) {
      Add-Failure "Stryker segment '$($segment.name)' does not include reporter '$reporter'."
    }
  }

  if ([int] $config."stryker-config".thresholds.break -lt [int] $segment.threshold) {
    Add-Failure "Stryker segment '$($segment.name)' break threshold is below manifest threshold $($segment.threshold)."
  }

  $jsonReport = Get-ChildItem -LiteralPath $ReportRootFullPath -Recurse -File -Filter "*.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*$($segment.artifactPrefix)*" } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

  if ($null -eq $jsonReport) {
    if ($AllowMissingReports) {
      $summaryLines.Add("- $($segment.name): report missing (allowed for local config validation only)") | Out-Null
      continue
    }

    Add-Failure "Missing JSON mutation report for segment '$($segment.name)' under $ReportRoot."
    continue
  }

  $reportText = Normalize-ArtifactText (Get-Content -LiteralPath $jsonReport.FullName -Raw)
  $reportText | Set-Content -LiteralPath $jsonReport.FullName -Encoding utf8
  $unsafe = Test-UnsafeText $reportText
  if ($unsafe) {
    Add-Failure "Mutation report '$($jsonReport.Name)' failed redaction scan: $unsafe."
  }

  try {
    $report = $reportText | ConvertFrom-Json -Depth 100
  }
  catch {
    Add-Failure "Mutation report '$($jsonReport.Name)' is malformed JSON: $($_.Exception.Message)"
    continue
  }

  $mutatedFiles = @(Get-MutatedFiles $report)
  if ($mutatedFiles.Count -eq 0) {
    Add-Failure "Mutation report '$($jsonReport.Name)' contains zero mutated source files."
  }

  foreach ($file in $mutatedFiles) {
    $repoPath = [string] $file.Path
    if ([System.IO.Path]::IsPathRooted($repoPath)) {
      $repoPath = ConvertTo-RepoPath $repoPath
    }

    $inScope = $false
    foreach ($root in $approvedRoots) {
      if ($repoPath.StartsWith($root.TrimEnd('/') + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
        $inScope = $true
      }
    }

    $activeStatuses = @($file.Statuses | Where-Object { $_ -ne "Ignored" })
    if (!$inScope -and $activeStatuses.Count -gt 0) {
      Add-Failure "Mutation report '$($jsonReport.Name)' includes out-of-scope file '$repoPath'."
    }
  }

  $statuses = @(Get-JsonValuesByName $report "status" | ForEach-Object { [string] $_ })
  $mutantCount = @($statuses | Where-Object { $_ -ne "Ignored" }).Count
  if ($mutantCount -eq 0) {
    Add-Failure "Mutation report '$($jsonReport.Name)' contains zero mutants."
  }

  $survived = @($statuses | Where-Object { $_ -eq "Survived" }).Count
  $noCoverage = @($statuses | Where-Object { $_ -eq "NoCoverage" }).Count
  $timeout = @($statuses | Where-Object { $_ -eq "Timeout" }).Count
  $compileError = @($statuses | Where-Object { $_ -eq "CompileError" }).Count
  $summaryLines.Add("- $($segment.name): report $(ConvertTo-RepoPath $jsonReport.FullName), mutants=$mutantCount, survived=$survived, noCoverage=$noCoverage, timeout=$timeout, compileError=$compileError") | Out-Null
}

foreach ($targetFile in $allTargetFiles) {
  if (!$combinedConfigText.Contains("Parsing/**/*.cs") -and !$combinedConfigText.Contains("Transforms/**/*.cs")) {
    Add-Failure "Manifest/config drift check cannot prove glob coverage for $targetFile."
  }
}

$summary = @(
  "## Mutation Evidence",
  "",
  "- Stryker tool: dotnet-stryker 4.14.1 via .config/dotnet-tools.json",
  "- Target manifest: $ManifestPath",
  "- Approved roots: $($approvedRoots -join ', ')",
  "- Target file count: $($allTargetFiles.Count)",
  "- Submodules: root-level checkout only; no recursive nested submodule command is used",
  "",
  "### Segments"
) + $summaryLines

$summary | Set-Content -LiteralPath $OutputFullPath -Encoding utf8

if ($errors.Count -gt 0) {
  $failurePath = Join-Path (Split-Path -Parent $OutputFullPath) "mutation-validation-errors.txt"
  $errors | Set-Content -LiteralPath $failurePath -Encoding utf8
  throw "Mutation validation failed:`n - $($errors -join "`n - ")"
}

Write-Host "Mutation validation passed. Summary: $OutputPath"
