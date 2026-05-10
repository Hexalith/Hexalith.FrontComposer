param(
    [switch]$SkipDocFx,
    [switch]$SkipSnippetBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DocsRoot = Join-Path $RepoRoot 'docs'
$ArtifactsRoot = Join-Path $RepoRoot 'artifacts/docs'
$ManifestPath = Join-Path $ArtifactsRoot 'validation-manifest.json'
$McpSliceRoot = Join-Path $ArtifactsRoot 'mcp-reference'
$SnippetRoot = Join-Path $ArtifactsRoot 'snippets'

function ConvertTo-RepoPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($RepoRoot)
    if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes repository root: $Path"
    }

    return $full.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Replace('\', '/')
}

function Add-Failure([System.Collections.Generic.List[string]]$Failures, [string]$Message) {
    $Failures.Add($Message) | Out-Null
}

function Get-FrontMatter([string]$Path) {
    $text = Get-Content -LiteralPath $Path -Raw
    $metadata = @{}
    if (-not $text.StartsWith("---`n") -and -not $text.StartsWith("---`r`n")) {
        return [pscustomobject]@{ Metadata = $metadata; Body = $text; HasFrontMatter = $false }
    }

    $match = [regex]::Match($text, '(?s)\A---\r?\n(.*?)\r?\n---\r?\n?')
    if (-not $match.Success) {
        return [pscustomobject]@{ Metadata = $metadata; Body = $text; HasFrontMatter = $false }
    }

    foreach ($line in ($match.Groups[1].Value -split "\r?\n")) {
        if ($line -match '^\s*([A-Za-z][A-Za-z0-9_-]*)\s*:\s*(.*?)\s*$') {
            $value = $Matches[2].Trim()
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }

            $metadata[$Matches[1]] = $value
        }
    }

    $body = $text.Substring($match.Length)
    [pscustomobject]@{ Metadata = $metadata; Body = $body; HasFrontMatter = $true }
}

function Get-ContentFiles {
    $patterns = @(
        'index.md',
        'tutorials/**/*.md',
        'how-to/**/*.md',
        'reference/**/*.md',
        'concepts/**/*.md',
        'diagnostics/HFC*.md',
        'migrations/**/*.md'
    )

    $files = New-Object System.Collections.Generic.List[string]
    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $DocsRoot -Recurse -File -Filter '*.md' |
            Where-Object {
                $rel = ConvertTo-RepoPath $_.FullName
                foreach ($prefix in @('docs/tutorials/', 'docs/how-to/', 'docs/reference/', 'docs/concepts/', 'docs/diagnostics/HFC', 'docs/migrations/')) {
                    if ($rel.StartsWith($prefix, [System.StringComparison]::Ordinal)) { return $true }
                }

                return $rel -eq 'docs/index.md'
            } |
            ForEach-Object { if (-not $files.Contains($_.FullName)) { $files.Add($_.FullName) | Out-Null } }
    }

    return $files
}

function Normalize-Identity([string]$Value) {
    $normalized = $Value.Normalize([System.Text.NormalizationForm]::FormC).ToLowerInvariant()
    $normalized = [System.Uri]::UnescapeDataString($normalized)
    $normalized = $normalized.Trim().TrimEnd('/')
    $normalized = $normalized -replace '[\\/_\-\.\s]+', ''
    return $normalized
}

function Get-FileHashHex([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Test-UnsafeText([string]$Text) {
    if ($Text -match 'C:\\Users\\|/home/[^/\s]+|/Users/[^/\s]+') { return 'absolute-private-path' }
    if ($Text -match 'tenant[_-]?id\s*[:=]\s*["'']?[0-9a-fA-F-]{16,}') { return 'tenant-id' }
    if ($Text -match '(?i)(api[_-]?key|token|secret|password)\s*[:=]\s*["''][^"'']{6,}') { return 'secret-like-value' }
    if ($Text -match "`e\[[0-9;]*[A-Za-z]") { return 'terminal-control-sequence' }
    return $null
}

function Assert-Markers([string]$Path, [string]$Body, $Metadata, [System.Collections.Generic.List[string]]$Failures) {
    $rel = ConvertTo-RepoPath $Path
    $known = @(
        'hfc:narrative:start',
        'hfc:narrative:end',
        'hfc:reference:start',
        'hfc:reference:end',
        'story-9-5:narrative-start',
        'story-9-5:narrative-end',
        'story-9-5:metadata-start',
        'story-9-5:metadata-end'
    )

    $matches = [regex]::Matches($Body, '<!--\s*([^>]+?)\s*-->')
    $stack = New-Object System.Collections.Generic.Stack[string]
    $referenceRegions = New-Object System.Collections.Generic.List[string]
    foreach ($match in $matches) {
        $name = $match.Groups[1].Value.Trim()
        if ($name.StartsWith('hfc:') -or $name.StartsWith('story-9-5:')) {
            if ($known -notcontains $name) {
                Add-Failure $Failures "$rel unknown marker '$name'."
                continue
            }

            if ($name.EndsWith(':start') -or $name.EndsWith('-start')) {
                if ($stack.Count -gt 0) {
                    Add-Failure $Failures "$rel nested marker '$name' inside '$($stack.Peek())'."
                }

                $stack.Push($name)
            }
            elseif ($name.EndsWith(':end') -or $name.EndsWith('-end')) {
                if ($stack.Count -eq 0) {
                    Add-Failure $Failures "$rel unmatched marker '$name'."
                    continue
                }

                $start = $stack.Pop()
                $expected = if ($start.EndsWith(':start', [System.StringComparison]::Ordinal)) {
                    $start.Substring(0, $start.Length - 6) + ':end'
                }
                else {
                    $start.Substring(0, $start.Length - 6) + '-end'
                }
                if ($expected -ne $name) {
                    Add-Failure $Failures "$rel marker '$start' closed by '$name'."
                }
            }
        }
    }

    if ($stack.Count -gt 0) {
        Add-Failure $Failures "$rel marker '$($stack.Peek())' was not closed."
    }

    $requiresReference = ($Metadata.ContainsKey('mcpReference') -and $Metadata['mcpReference'] -eq 'true') -or $Body.Contains('hfc:reference:start')
    if ($requiresReference) {
        $regions = [regex]::Matches($Body, '(?s)<!--\s*hfc:reference:start\s*-->(.*?)<!--\s*hfc:reference:end\s*-->')
        if ($regions.Count -eq 0) {
            Add-Failure $Failures "$rel is marked as MCP/reference content but has no hfc:reference region."
        }

        foreach ($region in $regions) {
            $content = $region.Groups[1].Value.Trim()
            if ([string]::IsNullOrWhiteSpace($content)) {
                Add-Failure $Failures "$rel has an empty hfc:reference region."
            }
            else {
                $referenceRegions.Add($content) | Out-Null
            }
        }
    }

    return $referenceRegions
}

function Write-McpSlices([array]$Slices) {
    if (Test-Path -LiteralPath $McpSliceRoot) { Remove-Item -LiteralPath $McpSliceRoot -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $McpSliceRoot | Out-Null
    $outputs = New-Object System.Collections.Generic.List[object]
    foreach ($slice in $Slices) {
        $source = $slice.Source
        $name = ($slice.Uid -replace '[^A-Za-z0-9_.-]', '-')
        $target = Join-Path $McpSliceRoot "$name.md"
        Set-Content -LiteralPath $target -Value $slice.Content -NoNewline
        $outputs.Add([ordered]@{
            source = ConvertTo-RepoPath $source
            output = ConvertTo-RepoPath $target
            sourceHash = Get-FileHashHex $source
            outputHash = Get-FileHashHex $target
        }) | Out-Null
    }

    return $outputs
}

function Invoke-Process([string]$FileName, [string[]]$Arguments, [string]$WorkingDirectory) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $FileName
    foreach ($arg in $Arguments) { $psi.ArgumentList.Add($arg) }
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($psi)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Command failed ($FileName $($Arguments -join ' '))`n$stdout`n$stderr"
    }

    return ($stdout + $stderr)
}

function Test-Snippets([array]$ContentFiles, [System.Collections.Generic.List[string]]$Failures) {
    if ($SkipSnippetBuild) { return @() }

    if (Test-Path -LiteralPath $SnippetRoot) { Remove-Item -LiteralPath $SnippetRoot -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $SnippetRoot | Out-Null
    $results = New-Object System.Collections.Generic.List[object]
    $index = 0

    foreach ($file in $ContentFiles) {
        $text = Get-Content -LiteralPath $file -Raw
        $rel = ConvertTo-RepoPath $file
        $fences = [regex]::Matches($text, '(?ms)^```csharp(?<attrs>[^\r\n]*)\r?\n(?<code>.*?)^```')
        foreach ($fence in $fences) {
            $attrs = $fence.Groups['attrs'].Value.Trim()
            $code = $fence.Groups['code'].Value
            if ($attrs -match '\bno-compile\b') {
                if ($attrs -notmatch '\breason=') {
                    Add-Failure $Failures "$rel has no-compile csharp snippet without a reason."
                }
            }
            elseif ($attrs -match '\bcompile\b') {
                $index++
                $dir = Join-Path $SnippetRoot ("snippet-{0:D3}" -f $index)
                New-Item -ItemType Directory -Force -Path $dir | Out-Null
                Set-Content -LiteralPath (Join-Path $dir 'Snippet.cs') -Value $code
                $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../../../src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj" />
    <ProjectReference Include="../../../../src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj" />
  </ItemGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>
"@
                Set-Content -LiteralPath (Join-Path $dir 'Snippet.csproj') -Value $csproj
                try {
                    Invoke-Process 'dotnet' @('build', 'Snippet.csproj', '--nologo', '-v:minimal', '-p:UseSharedCompilation=false') $dir | Out-Null
                    $results.Add([ordered]@{ source = $rel; snippet = $index; result = 'passed' }) | Out-Null
                }
                catch {
                    Add-Failure $Failures "$rel compile snippet $index failed: $($_.Exception.Message)"
                }
            }
            else {
                Add-Failure $Failures "$rel has csharp snippet without compile or no-compile reason."
            }

            $unsafe = Test-UnsafeText $code
            if ($unsafe) {
                Add-Failure $Failures "$rel snippet rejected for $unsafe."
            }
        }
    }

    return $results
}

New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null
$failures = New-Object System.Collections.Generic.List[string]
$contentFiles = @(Get-ContentFiles | Sort-Object -Unique)

$requiredFrontMatter = @('title', 'description', 'genre', 'audience', 'ownerStory', 'status', 'reviewed')
$allowedGenres = @('tutorial', 'how-to', 'reference', 'concept')
$allowedAudiences = @('adopter', 'framework-contributor', 'agent', 'operator')
$uids = @{}
$slugs = @{}
$mcpSlices = New-Object System.Collections.Generic.List[object]

foreach ($file in $contentFiles) {
    $rel = ConvertTo-RepoPath $file
    $parsed = Get-FrontMatter $file
    if (-not $parsed.HasFrontMatter) {
        Add-Failure $failures "$rel is missing YAML front matter."
        continue
    }

    foreach ($field in $requiredFrontMatter) {
        if (-not $parsed.Metadata.Contains($field) -or [string]::IsNullOrWhiteSpace([string]$parsed.Metadata[$field])) {
            Add-Failure $failures "$rel is missing required front matter '$field'."
        }
    }

    if (-not ($parsed.Metadata.Contains('uid') -or $parsed.Metadata.Contains('slug'))) {
        Add-Failure $failures "$rel must declare a stable uid or slug."
    }

    if ($parsed.Metadata.Contains('genre') -and $allowedGenres -notcontains $parsed.Metadata['genre']) {
        Add-Failure $failures "$rel has invalid genre '$($parsed.Metadata['genre'])'."
    }

    if ($parsed.Metadata.Contains('audience') -and $allowedAudiences -notcontains $parsed.Metadata['audience']) {
        Add-Failure $failures "$rel has invalid audience '$($parsed.Metadata['audience'])'."
    }

    if ($parsed.Metadata.Contains('status') -and $parsed.Metadata['status'] -eq 'placeholder') {
        foreach ($field in @('placeholderReason', 'deferredOwner', 'expiresAfterStory')) {
            if (-not $parsed.Metadata.Contains($field)) {
                Add-Failure $failures "$rel placeholder is missing '$field'."
            }
        }
    }

    foreach ($key in @('uid', 'slug')) {
        if ($parsed.Metadata.Contains($key)) {
            $canonical = Normalize-Identity ([string]$parsed.Metadata[$key])
            $map = if ($key -eq 'uid') { $uids } else { $slugs }
            if ($map.ContainsKey($canonical)) {
                Add-Failure $failures "$rel $key collides with $($map[$canonical]) after canonicalization."
            }
            else {
                $map[$canonical] = $rel
            }
        }
    }

    $unsafeBody = Test-UnsafeText $parsed.Body
    if ($unsafeBody) {
        Add-Failure $failures "$rel body rejected for $unsafeBody."
    }

    $regions = @(Assert-Markers $file $parsed.Body $parsed.Metadata $failures)
    if ($regions.Count -gt 0) {
        $mcpSlices.Add([ordered]@{
            Source = $file
            Uid = if ($parsed.Metadata.Contains('uid')) { $parsed.Metadata['uid'] } else { [System.IO.Path]::GetFileNameWithoutExtension($file) }
            Content = ($regions -join "`n`n")
        }) | Out-Null
    }
}

$toc = Join-Path $DocsRoot 'toc.yml'
if (-not (Test-Path -LiteralPath $toc)) {
    Add-Failure $failures 'docs/toc.yml is missing.'
}
else {
    $tocText = Get-Content -LiteralPath $toc -Raw
    foreach ($name in @('Tutorials', 'How-to', 'Reference', 'Concepts')) {
        if (-not $tocText.Contains("name: $name")) {
            Add-Failure $failures "docs/toc.yml is missing top-level Diataxis entry '$name'."
        }
    }
}

$registryPath = Join-Path $DocsRoot 'diagnostics/diagnostic-registry.json'
if (-not (Test-Path -LiteralPath $registryPath)) {
    Add-Failure $failures 'docs/diagnostics/diagnostic-registry.json is missing.'
}
else {
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    foreach ($diagnostic in $registry.diagnostics) {
        if ($diagnostic.lifecycle -in @('active', 'reserved', 'deprecated')) {
            $page = Join-Path $DocsRoot "$($diagnostic.docsSlug).md"
            if (-not (Test-Path -LiteralPath $page)) {
                Add-Failure $failures "Diagnostic $($diagnostic.id) is missing page $($diagnostic.docsSlug).md."
                continue
            }

            $diagnosticText = Get-Content -LiteralPath $page -Raw
            foreach ($section in @('## Problem', '## Common Causes', '## How To Fix', '## Example', '## Suppression Guidance', '## Migration/Deprecation', '## Related Diagnostics')) {
                if (-not $diagnosticText.Contains($section)) {
                    Add-Failure $failures "$(ConvertTo-RepoPath $page) is missing diagnostic section '$section'."
                }
            }
        }
    }
}

foreach ($migration in Get-ChildItem -Path (Join-Path $DocsRoot 'migrations') -Filter '*.md' -File) {
    if ($migration.Name -eq 'index.md') { continue }
    $front = (Get-FrontMatter $migration.FullName).Metadata
    foreach ($field in @('fromVersion', 'toVersion', 'diagnosticId', 'ownerStory', 'skillCorpusImpact', 'codeFixAvailable')) {
        if (-not $front.Contains($field)) {
            Add-Failure $failures "$(ConvertTo-RepoPath $migration.FullName) migration guide is missing '$field'."
        }
    }
}

$submodules = @()
$gitmodules = Join-Path $RepoRoot '.gitmodules'
if (Test-Path -LiteralPath $gitmodules) {
    $moduleText = Get-Content -LiteralPath $gitmodules -Raw
    foreach ($match in [regex]::Matches($moduleText, '(?m)^\s*path\s*=\s*(.+?)\s*$')) {
        $submodules += $match.Groups[1].Value.Trim().Replace('\', '/')
    }
}

foreach ($file in $contentFiles) {
    $rel = ConvertTo-RepoPath $file
    foreach ($submodule in $submodules) {
        if ($rel.StartsWith("$submodule/", [System.StringComparison]::Ordinal)) {
            Add-Failure $failures "$rel is inside root submodule $submodule and cannot be a docs input."
        }
    }
}

$snippetResults = @(Test-Snippets $contentFiles $failures)
$mcpOutputs = @(Write-McpSlices $mcpSlices)

$docfxOutput = $null
if (-not $SkipDocFx) {
    Invoke-Process 'dotnet' @('docfx', 'metadata', 'docs/docfx.json') $RepoRoot | Out-Null
    Invoke-Process 'dotnet' @('docfx', 'build', 'docs/docfx.json') $RepoRoot | Out-Null
    $docfxOutput = 'docs/_site'
}

$producerInputs = @(
    @{ story = '8-5-skill-corpus-and-build-time-agent-support'; path = 'docs/skills/frontcomposer/index.md' },
    @{ story = '9-1-build-time-drift-detection'; path = 'docs/diagnostics/samples/registry-drift-report.json' },
    @{ story = '9-2-cli-inspection-and-migration-tools'; path = 'docs/migrations/9.1-to-9.2.md' },
    @{ story = '9-3-ide-parity-and-developer-experience'; path = 'docs/ide-parity-matrix.md' },
    @{ story = '9-4-diagnostic-id-system-and-deprecation-policy'; path = 'docs/diagnostics/diagnostic-registry.json' }
)

$producerFingerprints = @()
foreach ($input in $producerInputs) {
    $full = Join-Path $RepoRoot $input.path
    $producerFingerprints += [ordered]@{
        story = $input.story
        path = $input.path
        exists = Test-Path -LiteralPath $full
        sha256 = Get-FileHashHex $full
        placeholderAllowed = $false
    }

    if (-not (Test-Path -LiteralPath $full)) {
        Add-Failure $failures "Producer artifact missing for $($input.story): $($input.path)."
    }
}

$toolVersions = [ordered]@{
    dotnet = (Invoke-Process 'dotnet' @('--version') $RepoRoot).Trim()
    docfx = if ($SkipDocFx) { 'skipped' } else { (Invoke-Process 'dotnet' @('docfx', '--version') $RepoRoot).Trim() }
}

$manifest = [ordered]@{
    schemaVersion = '1.0'
    generatedAt = '2026-05-10T00:00:00Z'
    validationCommand = 'pwsh ./eng/validate-docs.ps1'
    toolVersions = $toolVersions
    inputRoots = @('docs/index.md', 'docs/tutorials', 'docs/how-to', 'docs/reference', 'docs/concepts', 'docs/diagnostics', 'docs/migrations')
    producerFingerprints = $producerFingerprints
    generatedOutputRoots = @($docfxOutput, 'artifacts/docs/mcp-reference', 'artifacts/docs/snippets') | Where-Object { $_ }
    mcpReferenceSlices = $mcpOutputs
    snippetResults = $snippetResults
    acceptedPlaceholders = @()
    blockingFailures = $failures
}

$json = $manifest | ConvertTo-Json -Depth 20
Set-Content -LiteralPath $ManifestPath -Value $json

if ($failures.Count -gt 0) {
    $message = "Docs validation failed with $($failures.Count) issue(s):`n - " + ($failures -join "`n - ")
    throw $message
}

Write-Host "Docs validation passed. Evidence manifest: $(ConvertTo-RepoPath $ManifestPath)"
