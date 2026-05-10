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

function Test-WildcardRepoPath([string] $Path, [string] $Pattern) {
  $normalizedPattern = $Pattern.Replace('\', '/')
  return $Path -like $normalizedPattern
}

function Test-ExplicitlyExcluded([string] $Path, $Exclusions) {
  foreach ($exclusion in @($Exclusions)) {
    if (Test-WildcardRepoPath $Path ([string] $exclusion.path)) {
      if ([string]::IsNullOrWhiteSpace([string] $exclusion.reason) -or [string]::IsNullOrWhiteSpace([string] $exclusion.owner)) {
        Add-Failure "Exclusion '$($exclusion.path)' must include reason and owner."
      }

      return $true
    }
  }

  return $false
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

function Get-TriageCount($TriageEntries, [string] $SegmentName, [string] $Status) {
  $count = 0
  foreach ($entry in @($TriageEntries)) {
    if ([string] $entry.segment -ne $SegmentName -or [string] $entry.status -ne $Status) {
      continue
    }

    if ([string]::IsNullOrWhiteSpace([string] $entry.action) -or @($manifest.triageActions) -notcontains [string] $entry.action) {
      Add-Failure "Problem-mutant triage for segment '$SegmentName' status '$Status' has invalid action '$($entry.action)'."
    }

    if ([string]::IsNullOrWhiteSpace([string] $entry.owner) -or [string]::IsNullOrWhiteSpace([string] $entry.rationale)) {
      Add-Failure "Problem-mutant triage for segment '$SegmentName' status '$Status' must include owner and rationale."
    }

    $count += [int] $entry.count
  }

  return $count
}

if (!(Test-Path -LiteralPath $ManifestFullPath)) {
  throw "Missing mutation target manifest: $ManifestPath"
}

$manifest = Read-Json $ManifestFullPath
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputFullPath) | Out-Null

$approvedRoots = @($manifest.approvedTargetRoots | ForEach-Object { [string] $_ })
$segments = @($manifest.segments)
$explicitExclusions = @($manifest.explicitExclusions)
$problemMutantTriage = @($manifest.problemMutantTriage)
$allTargetFiles = New-Object System.Collections.Generic.HashSet[string]
$reportedTargetFiles = New-Object System.Collections.Generic.HashSet[string]
$combinedConfigText = ""
$hasAllRequiredReports = $true

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
  $projectDirectory = (Split-Path -Parent ([string] $config."stryker-config".project)).Replace('\', '/')
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
      $hasAllRequiredReports = $false
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
    else {
      $repoPath = $repoPath.TrimStart('.', '/', '\')
      $isRepoRelative = $repoPath.StartsWith("src/", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $repoPath.StartsWith("tests/", [System.StringComparison]::OrdinalIgnoreCase)
      if (!$isRepoRelative -and ![string]::IsNullOrWhiteSpace($projectDirectory)) {
        $repoPath = "$projectDirectory/$repoPath"
      }
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

    if ($inScope -and $activeStatuses.Count -gt 0) {
      [void] $reportedTargetFiles.Add($repoPath)
    }
  }

  $statuses = @(Get-JsonValuesByName $report "status" | ForEach-Object { [string] $_ })
  $mutantCount = @($statuses | Where-Object { $_ -ne "Ignored" }).Count
  if ($mutantCount -eq 0) {
    Add-Failure "Mutation report '$($jsonReport.Name)' contains zero mutants."
  }

  $minimumMutantCount = if ($null -ne $segment.minimumMutantCount) { [int] $segment.minimumMutantCount } else { 1 }
  if ($mutantCount -lt $minimumMutantCount) {
    Add-Failure "Mutation report '$($jsonReport.Name)' mutant count $mutantCount is below manifest baseline $minimumMutantCount for segment '$($segment.name)'."
  }

  $survived = @($statuses | Where-Object { $_ -eq "Survived" }).Count
  $noCoverage = @($statuses | Where-Object { $_ -eq "NoCoverage" }).Count
  $timeout = @($statuses | Where-Object { $_ -eq "Timeout" }).Count
  $compileError = @($statuses | Where-Object { $_ -eq "CompileError" }).Count
  foreach ($problem in @(
      @{ Status = "Survived"; Count = $survived },
      @{ Status = "NoCoverage"; Count = $noCoverage },
      @{ Status = "Timeout"; Count = $timeout },
      @{ Status = "CompileError"; Count = $compileError }
    )) {
    if ($problem.Count -gt (Get-TriageCount $problemMutantTriage ([string] $segment.name) ([string] $problem.Status))) {
      Add-Failure "Mutation report '$($jsonReport.Name)' has untriaged $($problem.Status) mutants in segment '$($segment.name)': count=$($problem.Count)."
    }
  }

  $summaryLines.Add("- $($segment.name): report $(ConvertTo-RepoPath $jsonReport.FullName), mutants=$mutantCount, survived=$survived, noCoverage=$noCoverage, timeout=$timeout, compileError=$compileError") | Out-Null
}

if (Test-Path -LiteralPath $ReportRootFullPath) {
  $textExtensions = @(".json", ".html", ".htm", ".md", ".txt", ".xml", ".log", ".js", ".css")
  Get-ChildItem -LiteralPath $ReportRootFullPath -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $textExtensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
      $artifactText = Normalize-ArtifactText (Get-Content -LiteralPath $_.FullName -Raw)
      $artifactText | Set-Content -LiteralPath $_.FullName -Encoding utf8
      $unsafe = Test-UnsafeText $artifactText
      if ($unsafe) {
        Add-Failure "Mutation artifact '$(ConvertTo-RepoPath $_.FullName)' failed redaction scan: $unsafe."
      }
    }
}

if ($hasAllRequiredReports) {
  foreach ($targetFile in $allTargetFiles) {
    if (!$reportedTargetFiles.Contains($targetFile) -and !(Test-ExplicitlyExcluded $targetFile $explicitExclusions)) {
      Add-Failure "Target drift: '$targetFile' did not appear in mutation reports and is not explicitly excluded with rationale."
    }
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
