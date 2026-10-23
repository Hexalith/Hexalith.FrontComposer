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
    $rootWithSeparator = $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not ($full.Equals($root, [System.StringComparison]::OrdinalIgnoreCase) -or $full.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase))) {
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
        'fluent-ui-v5-contingency.md',
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

                return $rel -in @('docs/index.md', 'docs/fluent-ui-v5-contingency.md')
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
    $text = Get-Content -LiteralPath $Path -Raw
    $normalized = $text -replace "`r`n", "`n"
    $normalized = $normalized -replace "`r", "`n"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    [System.Convert]::ToHexString($hash).ToLowerInvariant()
}

function Test-UnsafeText([string]$Text) {
    if ($Text -match 'C:\\Users\\|/home/[^/\s]+|/Users/[^/\s]+') { return 'absolute-private-path' }
    if ($Text -match 'tenant[_-]?id\s*[:=]\s*["'']?[0-9a-fA-F-]{16,}') { return 'tenant-id' }
    if ($Text -match '(?i)(api[_-]?key|token|secret|password)\s*[:=]\s*["''][^"'']{6,}') { return 'secret-like-value' }
    if ($Text -match "`e\[[0-9;]*[A-Za-z]") { return 'terminal-control-sequence' }
    return $null
}

function Resolve-DiagnosticPagePath($Diagnostic, [System.Collections.Generic.List[string]]$Failures) {
    $id = [string]$Diagnostic.id
    $slug = ([string]$Diagnostic.docsSlug).Replace('\', '/').Trim()
    if ([string]::IsNullOrWhiteSpace($slug)) {
        Add-Failure $Failures "Diagnostic $id has an empty docsSlug."
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($slug) -or $slug.Contains('..') -or $slug -notmatch '^diagnostics/HFC[0-9A-Za-z_-]+$') {
        Add-Failure $Failures "Diagnostic $id has unsafe docsSlug '$slug'. Expected diagnostics/HFCxxxx."
        return $null
    }

    $expectedSlug = "diagnostics/$id"
    if ($slug -ne $expectedSlug) {
        Add-Failure $Failures "Diagnostic $id docsSlug '$slug' must match '$expectedSlug'."
        return $null
    }

    $full = [System.IO.Path]::GetFullPath((Join-Path $DocsRoot "$slug.md"))
    $diagnosticsRoot = [System.IO.Path]::GetFullPath((Join-Path $DocsRoot 'diagnostics'))
    $diagnosticsRootWithSeparator = $diagnosticsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($diagnosticsRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        Add-Failure $Failures "Diagnostic $id docsSlug '$slug' escapes docs/diagnostics."
        return $null
    }

    return $full
}

function Assert-DiagnosticCompleteness([string]$Page, [string]$DiagnosticId, [System.Collections.Generic.List[string]]$Failures) {
    $text = Get-Content -LiteralPath $Page -Raw
    $rel = ConvertTo-RepoPath $Page
    foreach ($section in @('## Problem', '## Common Causes', '## How To Fix', '## Example', '## Suppression Guidance', '## Migration/Deprecation', '## Related Diagnostics')) {
        if (-not $text.Contains($section)) {
            Add-Failure $Failures "$rel is missing diagnostic section '$section'."
        }
    }

    foreach ($placeholder in @(
        'The framework detected a condition represented by',
        'Expected: Follow the FrontComposer diagnostic contract.',
        'Fix: See https://hexalith.github.io/FrontComposer/diagnostics/',
        'auto-synthesized-placeholder-pending-authoring')) {
        if ($text.Contains($placeholder)) {
            Add-Failure $Failures "$rel still contains placeholder diagnostic prose for $DiagnosticId."
            break
        }
    }
}

function Assert-MigrationCompleteness([string]$Path, [System.Collections.Generic.List[string]]$Failures) {
    $rel = ConvertTo-RepoPath $Path
    $text = Get-Content -LiteralPath $Path -Raw
    foreach ($heading in @('## Affected Versions', '## Why This Changed', '## Old Code', '## New Code', '## Analyzer And Code Fix', '## Skill Corpus Evidence')) {
        if (-not $text.Contains($heading)) {
            Add-Failure $Failures "$rel migration guide is missing section '$heading'."
        }
    }

    if ($text -match '(?i)\bstub\b|pending authoring') {
        Add-Failure $Failures "$rel migration guide still contains stub or pending-authoring language."
    }

    if ($text -notmatch '(?ms)## Old Code.*?```csharp' -or $text -notmatch '(?ms)## New Code.*?```csharp') {
        Add-Failure $Failures "$rel migration guide must include old and new C# examples."
    }
}

function Get-MigrationCatalogEdges([string]$CatalogPath, [System.Collections.Generic.List[string]]$Failures) {
    # Tokenize string/character literals and comments before locating the private Edges initializer. This prevents
    # comments, string contents, or other new(...) initializers from becoming executable edges.
    $tokens = [System.Collections.Generic.List[string]]::new()
    $literals = [System.Collections.Generic.List[string]]::new()
    $source = Get-Content -LiteralPath $CatalogPath -Raw
    foreach ($token in [regex]::Matches($source, '(?s)/\*.*?\*/|//[^\r\n]*|(?<raw>"{3,})(?!").*?\k<raw>(?!")|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|''(?:\\.|[^''\\\r\n])*''|[A-Za-z_]\w*|\S')) {
        $value = $token.Value
        if ($value.StartsWith('//') -or $value.StartsWith('/*')) { continue }
        if ($value.StartsWith('"') -or $value.StartsWith('@"') -or $value.StartsWith("'")) {
            # The colon cannot be part of a source identifier; lexed punctuation is space-separated.
            # Character literals are consumed so unrelated quotes cannot corrupt string tokens, and they
            # are not catalog tuples unless the plain-string check below accepts them.
            $tokens.Add('__literal:' + $literals.Count)
            $literals.Add($value)
        }
        else { $tokens.Add($value) }
    }

    $tokenText = $tokens -join ' '
    if ($tokenText -match '# (?:if|elif|else|endif)\b') {
        Add-Failure $Failures 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs conditional catalog syntax is unsupported.'
        return
    }
    $initializers = [regex]::Matches($tokenText, 'private static readonly MigrationEdge \[ \] Edges = BuildEdges \( \[ (?<body>.*?) \] \) ;')
    if ($initializers.Count -ne 1) {
        Add-Failure $Failures 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs must contain one recognizable private Edges initializer.'
        return
    }

    $body = $initializers[0].Groups['body'].Value
    $entryPattern = 'new \( (?<from>__literal:\d+) , (?<to>__literal:\d+) , (?<path>__literal:\d+) \)\s*,?'
    $remaining = [regex]::Replace($body, $entryPattern, '').Trim()
    if ($remaining.Length -gt 0) {
        Add-Failure $Failures 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs Edges initializer has unsupported tuple syntax.'
        return
    }

    foreach ($entry in [regex]::Matches($body, $entryPattern)) {
        $values = @()
        foreach ($field in @('from', 'to', 'path')) {
            $literal = $literals[[int]$entry.Groups[$field].Value.Substring('__literal:'.Length)]
            if ($literal -notmatch '^"[^"\\\r\n]*"$') {
                Add-Failure $Failures 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs Edges tuples require plain string literals.'
                return
            }
            $values += $literal.Substring(1, $literal.Length - 2)
        }
        [pscustomobject]@{ From = $values[0]; To = $values[1]; Path = $values[2] }
    }
}

function Assert-MigrationParity([string]$MigrationsRoot, [string]$CatalogPath, [System.Collections.Generic.List[string]]$Failures) {
    $catalog = @(Get-MigrationCatalogEdges $CatalogPath $Failures)
    $seenEdges = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($edge in $catalog) {
        if (-not $seenEdges.Add($edge.From + '->' + $edge.To)) {
            Add-Failure $Failures "src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs duplicate executable edge $($edge.From)->$($edge.To)."
        }
        if ($edge.Path -cnotmatch '^docs/migrations/[^/\\]+\.md$' -or $edge.Path.Contains('..')) {
            Add-Failure $Failures 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs executable guide path must be under docs/migrations.'
        }
    }

    $guides = [System.Collections.Generic.Dictionary[string,object]]::new([System.StringComparer]::Ordinal)
    [string[]]$guidePaths = @(Get-ChildItem -LiteralPath $MigrationsRoot -Filter '*.md' -File | ForEach-Object { $_.FullName })
    [array]::Sort($guidePaths, [System.StringComparer]::Ordinal)
    foreach ($guidePath in $guidePaths) {
        $guide = Get-Item -LiteralPath $guidePath
        if ($guide.Name -eq 'index.md') { continue }
        $path = 'docs/migrations/' + $guide.Name
        $front = (Get-FrontMatter $guide.FullName).Metadata
        $mode = [string]$front['migrationMode']
        if ($front.Keys -cnotcontains 'migrationMode' -or $mode -cnotin @('cli-supported', 'manual-only')) {
            Add-Failure $Failures "$path migrationMode must be cli-supported or manual-only using the canonical migrationMode key."
        }
        foreach ($versionField in @('fromVersion', 'toVersion')) {
            if ([string]$front[$versionField] -cnotmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
                Add-Failure $Failures "$path $versionField must be an exact semantic version."
            }
        }
        $guides[$path] = [pscustomobject]@{ From = [string]$front['fromVersion']; To = [string]$front['toVersion']; Mode = $mode }
        $matches = @($catalog | Where-Object { $_.From -ceq $front['fromVersion'] -and $_.To -ceq $front['toVersion'] -and $_.Path -ceq $path })
        if ($mode -ceq 'cli-supported' -and $matches.Count -ne 1) {
            Add-Failure $Failures "$path cli-supported guide must match exactly one complete catalog tuple."
        }
        if ($mode -ceq 'manual-only' -and $seenEdges.Contains([string]$front['fromVersion'] + '->' + [string]$front['toVersion'])) {
            Add-Failure $Failures "$path manual-only guide falsely classifies an executable catalog tuple."
        }
    }
    foreach ($edge in $catalog) {
        if (-not $guides.ContainsKey($edge.Path)) {
            Add-Failure $Failures "src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs executable edge $($edge.From)->$($edge.To) is missing catalog guidance."
        }
        elseif ($guides[$edge.Path].From -cne $edge.From -or $guides[$edge.Path].To -cne $edge.To -or $guides[$edge.Path].Mode -cne 'cli-supported') {
            Add-Failure $Failures "$($edge.Path) must match catalog versions and cli-supported classification."
        }
    }

    $indexPath = Join-Path $MigrationsRoot 'index.md'
    if (-not (Test-Path -LiteralPath $indexPath)) {
        Add-Failure $Failures 'docs/migrations/index.md is missing.'
        return
    }
    $index = [regex]::Replace((Get-FrontMatter $indexPath).Body, '(?s)<!--.*?(?:-->|\z)', '')
    $indexed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $fenceCharacter = $null
    $fenceLength = 0
    foreach ($line in ($index -split '\r?\n')) {
        if ($null -ne $fenceCharacter) {
            if ($line -match ('^ {0,3}' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}\s*$')) {
                $fenceCharacter = $null
            }
            continue
        }
        if ($line -match '^ {0,3}(?<fence>`{3,}|~{3,})') {
            $fenceCharacter = $Matches['fence'].Substring(0, 1)
            $fenceLength = $Matches['fence'].Length
            continue
        }
        if ($line -cmatch '^\|\s*fromVersion\s*\|\s*toVersion\s*\|\s*migrationMode\s*\|\s*Guide\s*\|\s*$' -or
            $line -match '^\|(?:\s*:?-{3,}:?\s*\|){4}\s*$') { continue }
        if ($line -notmatch '^\|') {
            if ($line -match '\[[^\]]+\]\((?:\./|\.\./migrations/)?[^/\\:#?()]+\.md(?:[?#][^)]*)?\)') {
                Add-Failure $Failures 'docs/migrations/index.md guide links must declare versions and migrationMode in a classified row.'
            }
            continue
        }
        $row = [regex]::Match($line, '^\|\s*(?<from>[^|]+?)\s*\|\s*(?<to>[^|]+?)\s*\|\s*(?<mode>[^|]+?)\s*\|\s*\[[^\]]+\]\((?<link>[^)]+)\)\s*\|\s*$')
        if (-not $row.Success) {
            Add-Failure $Failures 'docs/migrations/index.md has a malformed classified migration row.'
            continue
        }
        $link = $row.Groups['link'].Value
        if ($link -cnotmatch '^[^/\\:#?]+\.md$' -or $link.Contains('..')) {
            Add-Failure $Failures 'docs/migrations/index.md has an unsafe or stale guide link.'
            continue
        }
        $path = 'docs/migrations/' + $link
        if (-not $indexed.Add($path)) {
            Add-Failure $Failures "docs/migrations/index.md has duplicate guide link $path."
        }
        if (-not $guides.ContainsKey($path)) {
            Add-Failure $Failures "docs/migrations/index.md has stale guide link $path."
            continue
        }
        $guide = $guides[$path]
        if ($row.Groups['mode'].Value -cnotin @('cli-supported', 'manual-only')) {
            Add-Failure $Failures "docs/migrations/index.md $path migrationMode must be cli-supported or manual-only."
        }
        if ($row.Groups['from'].Value -cne $guide.From -or $row.Groups['to'].Value -cne $guide.To -or $row.Groups['mode'].Value -cne $guide.Mode) {
            Add-Failure $Failures "docs/migrations/index.md $path versions or migrationMode do not match guide metadata."
        }
    }
    [string[]]$paths = @($guides.Keys)
    [array]::Sort($paths, [System.StringComparer]::Ordinal)
    foreach ($path in $paths) {
        if (-not $indexed.Contains($path)) {
            Add-Failure $Failures "docs/migrations/index.md is missing guide link $path."
        }
    }
}

function Invoke-FluentCatalogGit([string]$Root, [string[]]$Arguments) {
    # Do not surface Git's stderr: missing objects/repositories can contain private machine paths.
    try {
        $start = [System.Diagnostics.ProcessStartInfo]::new('git')
        $start.WorkingDirectory = $Root
        foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $start.UseShellExecute = $false
        $process = [System.Diagnostics.Process]::Start($start)
        try {
            $output = $process.StandardOutput.ReadToEndAsync()
            $errorOutput = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            $null = $errorOutput.GetAwaiter().GetResult()
            return [pscustomobject]@{ ExitCode = $process.ExitCode; Output = $output.GetAwaiter().GetResult() }
        }
        finally { $process.Dispose() }
    }
    catch { return [pscustomobject]@{ ExitCode = -1; Output = '' } }
}

function Get-FluentCatalogVersions([string]$Root, [System.Collections.Generic.List[string]]$Failures) {
    $authority = 'references/Hexalith.Builds/Props/Directory.Packages.props'
    $repository = Invoke-FluentCatalogGit $Root @('rev-parse', '--show-toplevel')
    if ($repository.ExitCode -ne 0 -or $repository.Output.Trim().Replace('\', '/') -cne [System.IO.Path]::GetFullPath($Root).Replace('\', '/')) {
        Add-Failure $Failures 'references/Hexalith.Builds: root HEAD Git authority is unavailable.'
        return
    }
    $selection = Invoke-FluentCatalogGit $Root @('ls-tree', 'HEAD', '--', 'references/Hexalith.Builds')
    $gitlink = [regex]::Match($selection.Output, '\A160000 commit (?<sha>[0-9a-f]{40}|[0-9a-f]{64})\treferences/Hexalith\.Builds\r?\n?\z')
    if ($selection.ExitCode -ne 0 -or -not $gitlink.Success) {
        Add-Failure $Failures 'references/Hexalith.Builds: root HEAD must select exactly one Builds gitlink.'
        return
    }
    $buildsRoot = Join-Path $Root 'references/Hexalith.Builds'
    $buildsRepository = Invoke-FluentCatalogGit $buildsRoot @('rev-parse', '--show-toplevel')
    if ($buildsRepository.ExitCode -ne 0 -or $buildsRepository.Output.Trim().Replace('\', '/') -cne [System.IO.Path]::GetFullPath($buildsRoot).Replace('\', '/')) {
        Add-Failure $Failures "${authority}: selected catalog Git object is unavailable."
        return
    }
    $catalog = Invoke-FluentCatalogGit $buildsRoot @('show', ($gitlink.Groups['sha'].Value + ':Props/Directory.Packages.props'))
    if ($catalog.ExitCode -ne 0) {
        Add-Failure $Failures "${authority}: selected catalog Git object is unavailable."
        return
    }
    try {
        $settings = [System.Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($catalog.Output), $settings)
        try {
            $xml = [System.Xml.XmlDocument]::new()
            $xml.XmlResolver = $null
            $xml.Load($reader)
        }
        finally { $reader.Dispose() }
    }
    catch {
        Add-Failure $Failures "${authority}: selected catalog must be valid XML without a DTD."
        return
    }
    $packageEntries = @($xml.SelectNodes('//*[local-name()="PackageVersion"]'))
    foreach ($entry in $packageEntries) {
        foreach ($attribute in @('Include', 'Update', 'Remove')) {
            $target = $entry.GetAttribute($attribute)
            if ($target -and $target -cnotmatch '^[A-Za-z0-9_.-]+$') {
                # An item expression or glob may update the Fluent entries despite naming neither package literally.
                Add-Failure $Failures "${authority}: selected catalog contains an unresolved PackageVersion target."
                return
            }
        }
    }
    $versions = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::Ordinal)
    foreach ($package in @('Microsoft.FluentUI.AspNetCore.Components', 'Microsoft.FluentUI.AspNetCore.Components.Icons')) {
        $entries = @($packageEntries | Where-Object {
            $_.GetAttribute('Include').Equals($package, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.GetAttribute('Update').Equals($package, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.GetAttribute('Remove').Equals($package, [System.StringComparison]::OrdinalIgnoreCase)
        })
        if ($entries.Count -ne 1) {
            Add-Failure $Failures "${authority}: selected catalog must contain exactly one literal PackageVersion for $package."
            continue
        }
        $entry = $entries[0]
        $version = $entry.GetAttribute('Version')
        # No MSBuild evaluation or checkout fallback: exclusions and conditional/property/range/floating versions are unresolved authority.
        if ($entry.GetAttribute('Include') -cne $package -or $entry.HasAttribute('Update') -or $entry.HasAttribute('Remove') -or
            $entry.HasAttribute('Exclude') -or
            $entry.SelectNodes('ancestor-or-self::*[@Condition]').Count -gt 0 -or $entry.HasChildNodes -or
            $entry.ParentNode.LocalName -cne 'ItemGroup' -or $entry.ParentNode.ParentNode.LocalName -cne 'Project' -or
            $version -cnotmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
            Add-Failure $Failures "${authority}: selected catalog requires an unconditional literal version without Exclude for $package."
            continue
        }
        $versions.Add($package, $version)
    }
    return ,$versions
}

function Assert-FluentContingencyParity([string]$Root, [System.Collections.Generic.List[string]]$Failures) {
    $versions = Get-FluentCatalogVersions $Root $Failures
    $relative = 'docs/fluent-ui-v5-contingency.md'
    $page = Join-Path $Root $relative
    if (-not (Test-Path -LiteralPath $page -PathType Leaf)) {
        Add-Failure $Failures "${relative}: missing contingency page."
        return
    }
    # Comments, fenced examples and raw HTML pre blocks do not publish an identity table.
    $body = [regex]::Replace((Get-FrontMatter $page).Body, '(?s)<!--.*?(?:-->|\z)', '')
    $lines = [System.Collections.Generic.List[string]]::new()
    $fenceCharacter = $null
    $fenceLength = 0
    $preBlock = $false
    foreach ($line in ($body -split '\r?\n')) {
        if ($null -ne $fenceCharacter) {
            if ($line -match ('^ {0,3}' + [regex]::Escape($fenceCharacter) + '{' + $fenceLength + ',}\s*$')) { $fenceCharacter = $null }
            continue
        }
        if ($preBlock) {
            if ($line -match '(?i)</pre\s*>') { $preBlock = $false }
            continue
        }
        if ($line -match '^ {0,3}(?<fence>`{3,}|~{3,})') {
            $fenceCharacter = $Matches['fence'].Substring(0, 1)
            $fenceLength = $Matches['fence'].Length
            continue
        }
        if ($line -match '(?i)^ {0,3}<pre(?:\s|>)') {
            $preBlock = $line -notmatch '(?i)</pre\s*>'
            continue
        }
        $lines.Add($line)
    }
    $published = $lines -join "`n"
    $tables = [regex]::Matches($published, '(?m)^\| Package ID \| Selected version \|\n\| --- \| --- \|\n(?<rows>(?:\|[^\n]+\|\n?)+)')
    if ($tables.Count -ne 1) {
        Add-Failure $Failures "${relative}: must publish exactly one Package ID / Selected version table."
    }
    foreach ($package in @('Microsoft.FluentUI.AspNetCore.Components', 'Microsoft.FluentUI.AspNetCore.Components.Icons')) {
        $rowPattern = '(?m)^\|\s*`?' + [regex]::Escape($package) + '`?\s*\|(?<version>[^|]*)\|\s*$'
        $rows = [regex]::Matches($published, $rowPattern)
        if ($tables.Count -ne 1 -or $rows.Count -ne 1 -or [regex]::Matches($tables[0].Groups['rows'].Value, $rowPattern).Count -ne 1) {
            Add-Failure $Failures "${relative}: must publish exactly one identity table row for $package."
        }
        elseif ($null -ne $versions -and $versions.ContainsKey($package) -and $rows[0].Groups['version'].Value.Trim().Trim('`') -cne $versions[$package]) {
            Add-Failure $Failures "${relative}: version for $package must match the root HEAD-selected catalog."
        }
    }
}

function Test-DocFxExclusion([string]$Page, [array]$Patterns) {
    # Reuse the pinned DocFX matcher: **/ includes zero directories, unlike PowerShell -like.
    $toolManifest = Get-Content -LiteralPath (Join-Path $RepoRoot '.config/dotnet-tools.json') -Raw | ConvertFrom-Json
    $version = [string]$toolManifest.tools.docfx.version
    if ($version -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') { throw 'Unresolved DocFX tool version.' }
    $packageRoot = $env:NUGET_PACKAGES
    if ([string]::IsNullOrWhiteSpace($packageRoot)) {
        $location = Invoke-Process 'dotnet' @('nuget', 'locals', 'global-packages', '--list', '--force-english-output') $RepoRoot
        $packageRoot = [regex]::Match($location, '(?m)^global-packages: (.+?)\r?$').Groups[1].Value
    }
    if ([string]::IsNullOrWhiteSpace($packageRoot)) { throw 'DocFX package cache is unavailable.' }
    $assemblies = @(Get-ChildItem -LiteralPath (Join-Path $packageRoot "docfx/$version/tools") -Filter 'Docfx.Glob.dll' -Recurse -File |
        Where-Object { $_.Directory.Name -eq 'any' -and $_.Directory.Parent.Name -match '^net[0-9]+\.[0-9]+$' -and [version]$_.Directory.Parent.Name.Substring(3) -le [System.Environment]::Version } |
        Sort-Object { [version]$_.Directory.Parent.Name.Substring(3) } -Descending)
    if ($assemblies.Count -eq 0) { throw 'Pinned DocFX glob matcher is unavailable.' }
    [System.Reflection.Assembly]::LoadFrom($assemblies[0].FullName) | Out-Null
    foreach ($pattern in $Patterns) {
        $matcher = [Docfx.Glob.GlobMatcher]::new([string]$pattern, [Docfx.Glob.GlobMatcher]::DefaultOptions)
        if ($matcher.Match($Page)) { return $true }
    }
    return $false
}

function Assert-FluentContingencyPublication([System.Collections.Generic.List[string]]$Failures) {
    $page = 'fluent-ui-v5-contingency.md'
    $tocPath = Join-Path $DocsRoot 'toc.yml'
    $toc = if (Test-Path -LiteralPath $tocPath -PathType Leaf) { Get-Content -LiteralPath $tocPath -Raw } else { '' }
    $howTo = [regex]::Match($toc, '(?ms)^- name: How-to\r?\n(?<section>.*?)(?=^- name:|\z)').Groups['section'].Value
    $items = [regex]::Match($howTo, '(?ms)^  items:[ \t]*\r?\n(?<items>.*?)(?=^  (?!- )[^\s#]|\z)').Groups['items'].Value
    $linked = $false
    foreach ($item in [regex]::Matches($items, '(?ms)^  - name:[ \t]+[^\r\n|>]+\r?\n(?<item>.*?)(?=^  - |\z)')) {
        if ([regex]::IsMatch($item.Groups['item'].Value, '(?m)^    href: ' + [regex]::Escape($page) + '[ \t]*\r?$')) { $linked = $true }
    }
    if (-not $linked) {
        Add-Failure $Failures 'docs/toc.yml: missing Fluent contingency page under How-to.'
    }
    try {
        $config = Get-Content -LiteralPath (Join-Path $DocsRoot 'docfx.json') -Raw | ConvertFrom-Json -AsHashtable
        $entries = @($config.build.content | Where-Object {
            @($_.files | Where-Object { $_ -ceq $page }).Count -eq 1 -and
            (-not $_.ContainsKey('src') -or $_.src -ceq '.') -and
            (-not $_.ContainsKey('dest') -or $_.dest -ceq '.') -and
            (-not $_.ContainsKey('exclude') -or -not (Test-DocFxExclusion $page @($_.exclude)))
        })
        if ($entries.Count -ne 1) { throw 'Missing publication mapping.' }
    }
    catch { Add-Failure $Failures 'docs/docfx.json: must publish the Fluent contingency page at its documentation path.' }
}

function Get-ComponentSection([string]$Text, [string]$Heading) {
    $start = $Text.IndexOf($Heading, [System.StringComparison]::Ordinal)
    if ($start -lt 0) { return '' }
    $remaining = $Text.Substring($start + $Heading.Length)
    $level = ([regex]::Match($Heading, '^#+')).Length
    $end = [regex]::Match($remaining, "(?m)^#{1,$level} ")
    if ($end.Success) { return $remaining.Substring(0, $end.Index) }
    return $remaining
}

function Get-ComponentNames([array]$Sources) {
    $componentTypes = @{}
    foreach ($name in @('ComponentBase', 'IComponent', 'FluxorComponent', 'LayoutComponentBase', 'ErrorBoundaryBase', 'ErrorBoundary')) { $componentTypes[$name] = $true }
    $components = @{}
    $declarations = @{}
    foreach ($source in $Sources) {
        if ($source.Name.EndsWith('.razor')) {
            $name = $source.Name.Split('.')[0]
            $components[$name] = $true
            $componentTypes[$name] = $true
            continue
        }
        $text = Get-Content -LiteralPath $source.FullName -Raw
        foreach ($declaration in [regex]::Matches($text, '(?m)^\s*(?:(?:public|internal|private|protected|abstract|sealed|partial|static|new)\s+)*(?<kind>class|interface)\s+(?<name>\w+)(?:<[^>{}]+>)?\s*(?:\([^{};]*\))?\s*(?::\s*(?<bases>[^{;]+))?[{;]')) {
            $bases = [regex]::Replace($declaration.Groups['bases'].Value, '(?s)\bwhere\b.*', '')
            while ($bases -match '<[^<>]*>') { $bases = [regex]::Replace($bases, '<[^<>]*>', '') }
            $name = $declaration.Groups['name'].Value
            $inheritedTypes = @($bases.Split(',') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim().Split('.')[-1] })
            if ($declarations.ContainsKey($name)) { $inheritedTypes = @($declarations[$name].Bases) + $inheritedTypes }
            $declarations[$name] = @{
                IsClass = $declaration.Groups['kind'].Value -eq 'class'
                Bases = $inheritedTypes
            }
        }
    }
    do {
        $changed = $false
        foreach ($name in @($declarations.Keys)) {
            $declaration = $declarations[$name]
            if (-not $componentTypes.ContainsKey($name) -and @($declaration.Bases | Where-Object { $componentTypes.ContainsKey($_) }).Count -gt 0) {
                $componentTypes[$name] = $true
                $changed = $true
            }
            if ($declaration.IsClass -and $componentTypes.ContainsKey($name)) { $components[$name] = $true }
        }
    } while ($changed)
    return @($components.Keys | Sort-Object)
}

function Assert-ComponentDocumentation([System.Collections.Generic.List[string]]$Failures) {
    $inventoryPath = '_bmad-output/project-docs/component-inventory.md'
    $statusPath = '_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md'
    $inventory = (Get-Content -LiteralPath (Join-Path $RepoRoot $inventoryPath) -Raw) -split '## H. Current component documentation classifications' | Select-Object -Last 1
    $status = Get-Content -LiteralPath (Join-Path $RepoRoot $statusPath) -Raw
    $index = [regex]::Replace((Get-Content -LiteralPath (Join-Path $DocsRoot 'reference/components/index.md') -Raw), '<!--.*?(?:-->|\z)', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $toc = Get-Content -LiteralPath (Join-Path $DocsRoot 'toc.yml') -Raw
    $rows = @{}
    foreach ($row in [regex]::Matches($inventory, '(?m)^\| `(?<name>\w+)` \| (?<classification>[^|]+) \| `(?<page>docs/reference/components/[^`]+)` \|')) {
        $rows[$row.Groups['name'].Value] = @{ Classification = $row.Groups['classification'].Value.Trim(); Page = $row.Groups['page'].Value }
    }
    $componentRoot = Join-Path $RepoRoot 'src/Hexalith.FrontComposer.Shell/Components'
    $sources = @(Get-ChildItem -LiteralPath $componentRoot -Recurse -File | Where-Object { $_.Name.EndsWith('.cs') -or $_.Name.EndsWith('.razor') } | Sort-Object FullName)
    $names = @(Get-ComponentNames $sources)
    $contractSources = @($sources) + @(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'src/Hexalith.FrontComposer.Contracts') -Filter '*.cs' -Recurse -File | Where-Object { $_.FullName -notmatch '[/\\](bin|obj)[/\\]' }) +
        @(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'src/Hexalith.FrontComposer.Contracts.UI/Rendering') -Filter '*Context.cs' -File) +
        @(Get-Item -LiteralPath (Join-Path $RepoRoot 'src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs'))
    $contracts = @{}
    foreach ($source in $contractSources) {
        $text = Get-Content -LiteralPath $source.FullName -Raw
        foreach ($contract in [regex]::Matches($text, 'public (?:sealed )?class (?<name>\w+Context)(?:<|\s)|public enum (?<name>\w+State|ProjectionConnectionStatus)\b')) { $contracts[$contract.Groups['name'].Value] = $text }
    }
    $requiredContracts = @{}
    foreach ($name in $names) {
        if (-not $rows.ContainsKey($name) -or $rows[$name].Classification -notin @('adopter', 'infrastructure')) {
            Add-Failure $Failures "${inventoryPath}: unclassified component $name."
            continue
        }
        $row = $rows[$name]
        $path = Join-Path $RepoRoot $row.Page
        if (-not (Test-Path -LiteralPath $path)) {
            Add-Failure $Failures "$($row.Page): missing classified component page."
            continue
        }
        $page = Get-Content -LiteralPath $path -Raw
        if (-not $requiredContracts.ContainsKey($row.Page)) { $requiredContracts[$row.Page] = @{} }
        $sourceText = (@($sources | Where-Object { $_.Name.Split('.')[0] -eq $name } | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n")
        foreach ($contract in $contracts.Keys) {
            if ([regex]::IsMatch($sourceText, '\b' + [regex]::Escape($contract) + '\b')) { $requiredContracts[$row.Page][$contract] = $true }
        }
        $parameters = Get-ComponentSection $page '## Parameters / slots'
        $typed = Get-ComponentSection $parameters ('### `' + $name + '`')
        if ($row.Classification -eq 'infrastructure' -and [string]::IsNullOrEmpty($typed)) { continue }
        if (-not [string]::IsNullOrEmpty($typed)) { $parameters = $typed }
        $actual = @{}
        if ([regex]::IsMatch($sourceText, '(?m)^@inherits\s+(?:Microsoft\.AspNetCore\.Components\.)?LayoutComponentBase\b')) { $actual['Body'] = 'RenderFragment?' }
        foreach ($parameter in [regex]::Matches($sourceText, '\[(?:global::)?(?:Microsoft\.AspNetCore\.Components\.)?Parameter(?:Attribute)?\b[^\]]*\](?:\s*\[[^\]]*\])*\s*public\s+(?<type>[^\r\n{]+?)\s+(?<name>\w+)\s*\{')) {
            $actual[$parameter.Groups['name'].Value] = $parameter.Groups['type'].Value.Trim()
        }
        foreach ($parameterName in @($actual.Keys | Sort-Object)) {
            $parameterType = $actual[$parameterName]
            $docRows = @($parameters -split "\r?\n" | Where-Object { $_.StartsWith('|') -and ($_ -split '\|')[1].Contains('`' + $parameterName + '`') })
            $documentedNames = @()
            $documentedTypes = @()
            if ($docRows.Count -gt 0) {
                $documentedNames = @([regex]::Matches(($docRows[0] -split '\|')[1], '`(\w+)`') | ForEach-Object { $_.Groups[1].Value })
                $documentedTypes = @([regex]::Matches(($docRows[0] -split '\|')[2], '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
            }
            $parameterIndex = [array]::IndexOf($documentedNames, $parameterName)
            if ($parameterIndex -lt 0 -or $documentedNames.Count -ne $documentedTypes.Count -or $documentedTypes[$parameterIndex] -cne $parameterType) {
                Add-Failure $Failures "$($row.Page): $name.$parameterName missing or stale parameter/type."
            }
        }
        foreach ($parameter in [regex]::Matches($parameters, '(?m)^\| (?<names>[^|]+) \| (?<types>[^|]+) \|')) {
            foreach ($identifier in [regex]::Matches($parameter.Groups['names'].Value, '`(\w+)`')) {
                if (-not $actual.ContainsKey($identifier.Groups[1].Value)) {
                    Add-Failure $Failures "$($row.Page): $name has stale parameter $($identifier.Groups[1].Value)."
                }
            }
        }
    }
    foreach ($stale in @($rows.Keys | Where-Object { $names -notcontains $_ } | Sort-Object)) {
        Add-Failure $Failures "${inventoryPath}: stale component classification $stale."
    }
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $DocsRoot 'reference/components') -Filter '*.md' -File | Where-Object { $_.Name -ne 'index.md' } | Sort-Object Name) {
        $rel = ConvertTo-RepoPath $file.FullName
        $page = Get-Content -LiteralPath $file.FullName -Raw
        if (-not [regex]::IsMatch($index, '\[[^\]\r\n]+\]\(' + [regex]::Escape($file.Name) + '\)')) { Add-Failure $Failures "docs/reference/components/index.md: missing page $($file.Name)." }
        if (-not [regex]::IsMatch($toc, '(?m)^[ \t]*(?:-[ \t]+)?href:[ \t]+reference/components/' + [regex]::Escape($file.Name) + '[ \t]*(?:#.*)?\r?$')) { Add-Failure $Failures "docs/toc.yml: missing page $($file.Name)." }
        if (-not $status.Contains('`' + $rel + '` | authored')) { Add-Failure $Failures "${statusPath}: missing current authored status for $($file.Name)." }
        foreach ($section in @('Overview', 'Usage', 'Parameters / slots', 'Layout (FC-LYT)', 'Accessibility (FC-A11Y)', 'Localization (FC-L10N)', 'Related')) {
            if (-not $page.Contains('## ' + $section)) { Add-Failure $Failures "${rel}: missing FC-DOC section $section." }
        }
        if ($requiredContracts.ContainsKey($rel)) {
            foreach ($required in @($requiredContracts[$rel].Keys | Sort-Object)) {
                if (-not [regex]::IsMatch($page, '(?m)^### `' + [regex]::Escape($required) + '`\r?$')) { Add-Failure $Failures "${rel}: missing source-referenced contract section $required." }
            }
        }
        foreach ($heading in [regex]::Matches($page, '(?m)^### `(?<name>\w+Context)`')) {
            $name = $heading.Groups['name'].Value
            if (-not $contracts.ContainsKey($name)) { Add-Failure $Failures "${rel}: unresolved context $name."; continue }
            $source = $contracts[$name]
            $documented = Get-ComponentSection $page ('### `' + $name + '`')
            $expected = @{}
            foreach ($property in [regex]::Matches($source, 'public (?<type>[^\r\n]+?) (?<name>\w+) (?:\{\s*get;\s*(?:(?:(?:private|protected|internal)\s+)*(?:set|init);\s*)?\}|=>[^\r\n]+;)')) {
                $propertyName = $property.Groups['name'].Value
                $expected[$propertyName] = $true
                $docRows = @($documented -split "\r?\n" | Where-Object { $_.StartsWith('| `' + $propertyName + '` |') })
                if ($docRows.Count -eq 0 -or -not (($docRows[0] -split '\|')[2].Contains('`' + $property.Groups['type'].Value + '`'))) {
                    Add-Failure $Failures "${rel}: $name.$propertyName missing or stale context property/type."
                }
            }
            foreach ($property in [regex]::Matches($documented, '(?m)^\| `(\w+)` \|')) {
                if (-not $expected.ContainsKey($property.Groups[1].Value)) { Add-Failure $Failures "${rel}: $name has stale context property $($property.Groups[1].Value)." }
            }
        }
        foreach ($heading in [regex]::Matches($page, '(?m)^### `(?<name>\w+State|ProjectionConnectionStatus)`')) {
            $name = $heading.Groups['name'].Value
            if ($rows.ContainsKey($name)) { continue }
            if (-not $contracts.ContainsKey($name)) { Add-Failure $Failures "${rel}: unresolved state vocabulary $name."; continue }
            $stateSource = $contracts[$name]
            $expected = @([regex]::Matches($stateSource, '(?m)^    (\w+)(?=[\t ]*(?:=|,|\r?$))') | ForEach-Object { $_.Groups[1].Value } | Sort-Object)
            $actualStates = @([regex]::Matches((Get-ComponentSection $page ('### `' + $name + '`')), '(?m)^\| `(\w+)` \|') | ForEach-Object { $_.Groups[1].Value } | Sort-Object)
            if (($expected -join ',') -cne ($actualStates -join ',')) { Add-Failure $Failures "${rel}: $name state vocabulary differs from source." }
        }
    }
}

function Get-ApiItemsMissingSummaries {
    $missing = New-Object System.Collections.Generic.List[string]
    $apiRoot = Join-Path $DocsRoot 'reference/api'
    if (-not (Test-Path -LiteralPath $apiRoot)) {
        return $missing
    }

    foreach ($file in Get-ChildItem -Path $apiRoot -Filter '*.yml' -File) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        $items = [regex]::Matches($text, '(?ms)^- uid:\s*(?<uid>.+?)\r?\n(?<body>.*?)(?=^- uid:|\z)')
        foreach ($item in $items) {
            $uid = $item.Groups['uid'].Value.Trim()
            $body = $item.Groups['body'].Value
            $isPublicType = $uid.StartsWith('Hexalith.FrontComposer', [System.StringComparison]::Ordinal) -and
                $body -match '(?m)^\s+commentId:\s*T:' -and
                $body -match '(?m)^\s+type:\s*(Class|Struct|Interface|Enum|Delegate)\s*$'
            if (-not $isPublicType) {
                continue
            }

            $hasSummary = $body -match '(?m)^\s+summary:\s*(>-|\|-|\S+)' -and $body -notmatch '(?m)^\s+summary:\s*\[\]\s*$'
            if (-not $hasSummary) {
                $missing.Add($uid) | Out-Null
            }
        }
    }

    return $missing
}

function Assert-ApiSummaryBaseline([System.Collections.Generic.List[string]]$Failures) {
    $baselinePath = Join-Path $DocsRoot 'validation/api-summary-baseline.txt'
    if (-not (Test-Path -LiteralPath $baselinePath)) {
        Add-Failure $Failures 'docs/validation/api-summary-baseline.txt is missing.'
        return
    }

    $expected = @(Get-Content -LiteralPath $baselinePath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith('#') } | Sort-Object -Unique)
    $actual = @(Get-ApiItemsMissingSummaries | Sort-Object -Unique)
    $expectedSet = @{}
    foreach ($uid in $expected) { $expectedSet[$uid] = $true }
    $actualSet = @{}
    foreach ($uid in $actual) { $actualSet[$uid] = $true }

    foreach ($uid in $actual) {
        if (-not $expectedSet.ContainsKey($uid)) {
            Add-Failure $Failures "API reference item '$uid' is missing a summary and is not in docs/validation/api-summary-baseline.txt."
        }
    }

    foreach ($uid in $expected) {
        if (-not $actualSet.ContainsKey($uid)) {
            Add-Failure $Failures "API summary baseline contains resolved or missing UID '$uid'; update docs/validation/api-summary-baseline.txt."
        }
    }
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
    $seenOutputNames = @{}
    foreach ($slice in $Slices) {
        $source = $slice.Source
        $name = ($slice.Uid -replace '[^A-Za-z0-9_.-]', '-')
        if ([string]::IsNullOrWhiteSpace($name) -or $name -in @('.', '..')) {
            throw "MCP slice from $(ConvertTo-RepoPath $source) has unsafe generated name '$name'."
        }

        $key = $name.ToLowerInvariant()
        if ($seenOutputNames.ContainsKey($key)) {
            throw "MCP slice output collision for '$name.md' between $(ConvertTo-RepoPath $source) and $($seenOutputNames[$key])."
        }

        $seenOutputNames[$key] = ConvertTo-RepoPath $source
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
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) {
        throw "Command failed ($FileName $($Arguments -join ' '))`n$stdout`n$stderr"
    }

    return ($stdout + $stderr)
}

function Prepare-DocFxReferences {
    # Resolve package/project dependencies from the pinned SDK/catalog. DocFx resolves each
    # assembly's framework itself; a netstandard input can select an older installed runtime
    # than a net10 input. Adding SDK facades or one runtime's assemblies to every input mixes
    # core libraries and makes Roslyn lose predefined types such as System.Object.
    $config = Get-Content -LiteralPath (Join-Path $DocsRoot 'docfx.json') -Raw | ConvertFrom-Json
    $references = @{}
    foreach ($metadata in $config.metadata) {
        foreach ($source in $metadata.src) {
            $sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $DocsRoot $source.src))
            foreach ($file in $source.files) {
                $normalized = $file.Replace('\', '/')
                if ($normalized -notmatch '^(?<project>.+)/bin/(?<configuration>[^/]+)/(?<framework>[^/]+)/(?<assembly>[^/]+)\.dll$') {
                    throw "Cannot resolve DocFx assembly references for input '$file'."
                }
                $project = Join-Path $sourceRoot "$($Matches.project)/$($Matches.assembly).csproj"
                $configuration = $Matches.configuration
                $framework = $Matches.framework
                $resolved = Invoke-Process 'dotnet' @(
                    'msbuild', $project, '-target:ResolveReferences', '-getItem:ReferencePath',
                    "-property:Configuration=$configuration", "-property:TargetFramework=$framework",
                    '-property:BuildProjectReferences=false', '-maxcpucount:1', '-nodeReuse:false'
                ) $RepoRoot | ConvertFrom-Json
                foreach ($reference in $resolved.Items.ReferencePath) {
                    $frameworkReference = $reference.PSObject.Properties['FrameworkReferenceName']
                    $packageId = $reference.PSObject.Properties['NuGetPackageId']
                    if (($frameworkReference -and -not [string]::IsNullOrWhiteSpace($frameworkReference.Value)) -or
                        ($packageId -and $packageId.Value -eq 'NETStandard.Library')) {
                        continue
                    }
                    $path = [string]$reference.Identity
                    $identity = [System.Reflection.AssemblyName]::GetAssemblyName($path)
                    $name = [System.IO.Path]::GetFileName($path)
                    if (-not $references.ContainsKey($name) -or $identity.Version -gt $references[$name].Identity.Version) {
                        $references[$name] = @{ Path = $path; Identity = $identity }
                    }
                }
            }
        }
    }
    if ($references.Count -eq 0) {
        throw 'DocFx reference resolution did not produce package or project references.'
    }
    $referenceRoot = Join-Path $ArtifactsRoot 'api-references'
    if (Test-Path -LiteralPath $referenceRoot) { Remove-Item -LiteralPath $referenceRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $referenceRoot -Force | Out-Null
    foreach ($name in ($references.Keys | Sort-Object)) {
        Copy-Item -LiteralPath $references[$name].Path -Destination (Join-Path $referenceRoot $name)
    }
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
    <ProjectReference Include="../../../../src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj" />
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
    $tocLines = @(Get-Content -LiteralPath $toc)
    $topLevelNames = @()
    foreach ($line in $tocLines) {
        if ($line -match '^- name:\s*(.+?)\s*$') {
            $topLevelNames += $Matches[1]
        }
    }

    $expectedTopLevelNames = @('Tutorials', 'How-to', 'Reference', 'Concepts')
    if ($topLevelNames.Count -ne $expectedTopLevelNames.Count) {
        Add-Failure $failures "docs/toc.yml must have exactly four top-level Diataxis entries: $($expectedTopLevelNames -join ', ')."
    }

    for ($i = 0; $i -lt $expectedTopLevelNames.Count -and $i -lt $topLevelNames.Count; $i++) {
        if ($topLevelNames[$i] -ne $expectedTopLevelNames[$i]) {
            Add-Failure $failures "docs/toc.yml top-level entry $($i + 1) must be '$($expectedTopLevelNames[$i])', got '$($topLevelNames[$i])'."
        }
    }
}

Assert-FluentContingencyParity $RepoRoot $failures
Assert-FluentContingencyPublication $failures
Assert-ComponentDocumentation $failures

$registryPath = Join-Path $DocsRoot 'diagnostics/diagnostic-registry.json'
if (-not (Test-Path -LiteralPath $registryPath)) {
    Add-Failure $failures 'docs/diagnostics/diagnostic-registry.json is missing.'
}
else {
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    foreach ($policy in @('messageTemplatePolicy', 'docsStubProsePolicy')) {
        if ($registry.PSObject.Properties.Name -contains $policy -and [string]$registry.PSObject.Properties[$policy].Value -match 'placeholder|pending-authoring') {
            Add-Failure $failures "docs/diagnostics/diagnostic-registry.json $policy still describes placeholder diagnostic authoring."
        }
    }

    foreach ($diagnostic in $registry.diagnostics) {
        if ($diagnostic.lifecycle -in @('active', 'reserved', 'deprecated')) {
            $page = Resolve-DiagnosticPagePath $diagnostic $failures
            if ($null -eq $page) {
                continue
            }

            if (-not (Test-Path -LiteralPath $page)) {
                Add-Failure $failures "Diagnostic $($diagnostic.id) is missing page $($diagnostic.docsSlug).md."
                continue
            }

            Assert-DiagnosticCompleteness $page $diagnostic.id $failures
        }
    }
}

$tocText = if (Test-Path -LiteralPath $toc) { Get-Content -LiteralPath $toc -Raw } else { $null }
foreach ($migration in Get-ChildItem -Path (Join-Path $DocsRoot 'migrations') -Filter '*.md' -File) {
    if ($migration.Name -eq 'index.md') { continue }
    if ($null -ne $tocText -and -not [regex]::IsMatch($tocText, '(?m)^[ \t]*(?:-[ \t]+)?href:[ \t]+migrations/' + [regex]::Escape($migration.Name) + '[ \t]*(?:#.*)?\r?$')) {
        Add-Failure $failures "docs/toc.yml: missing migration guide $($migration.Name)."
    }
    $front = (Get-FrontMatter $migration.FullName).Metadata
    foreach ($field in @('fromVersion', 'toVersion', 'diagnosticId', 'ownerStory', 'skillCorpusImpact', 'codeFixAvailable', 'migrationMode')) {
        if (-not $front.Contains($field)) {
            Add-Failure $failures "$(ConvertTo-RepoPath $migration.FullName) migration guide is missing '$field'."
        }
    }

    Assert-MigrationCompleteness $migration.FullName $failures
}

Assert-MigrationParity (Join-Path $DocsRoot 'migrations') (Join-Path $RepoRoot 'src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs') $failures

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
    Invoke-Process 'dotnet' @('build', 'Hexalith.FrontComposer.slnx', '--configuration', 'Release') $RepoRoot | Out-Null
    Prepare-DocFxReferences
    $apiMetadataRoot = Join-Path $DocsRoot 'reference/api'
    $apiMetadataBackup = Join-Path ([System.IO.Path]::GetTempPath()) "frontcomposer-docfx-api-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $apiMetadataBackup | Out-Null
    try {
        Get-ChildItem -Path $apiMetadataRoot -Filter '*.yml' -File -ErrorAction SilentlyContinue |
            Copy-Item -Destination $apiMetadataBackup -Force
        $apiManifestPath = Join-Path $apiMetadataRoot '.manifest'
        if (Test-Path -LiteralPath $apiManifestPath) {
            Copy-Item -LiteralPath $apiManifestPath -Destination $apiMetadataBackup -Force
        }

        Get-ChildItem -Path $apiMetadataRoot -Filter '*.yml' -File -ErrorAction SilentlyContinue |
            Remove-Item -Force
        Remove-Item -LiteralPath $apiManifestPath -Force -ErrorAction SilentlyContinue
        Invoke-Process 'dotnet' @('docfx', 'metadata', 'docs/docfx.json') $RepoRoot | Out-Null
    }
    catch {
        Get-ChildItem -Path $apiMetadataRoot -Filter '*.yml' -File -ErrorAction SilentlyContinue |
            Remove-Item -Force
        Remove-Item -LiteralPath (Join-Path $apiMetadataRoot '.manifest') -Force -ErrorAction SilentlyContinue
        Get-ChildItem -Path $apiMetadataBackup -File -ErrorAction SilentlyContinue |
            Copy-Item -Destination $apiMetadataRoot -Force
        throw
    }
    finally {
        Remove-Item -LiteralPath $apiMetadataBackup -Recurse -Force -ErrorAction SilentlyContinue
    }

    Assert-ApiSummaryBaseline $failures
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

$expectedProducerFingerprintPath = Join-Path $DocsRoot 'validation/producer-fingerprints.json'
$expectedProducerFingerprints = @{}
if (-not (Test-Path -LiteralPath $expectedProducerFingerprintPath)) {
    Add-Failure $failures 'docs/validation/producer-fingerprints.json is missing.'
}
else {
    $expectedProducerData = Get-Content -LiteralPath $expectedProducerFingerprintPath -Raw | ConvertFrom-Json
    foreach ($expected in $expectedProducerData.producers) {
        $expectedProducerFingerprints[[string]$expected.path] = [pscustomobject]@{
            story = [string]$expected.story
            sha256 = [string]$expected.sha256
        }
    }
}

$producerFingerprints = @()
foreach ($input in $producerInputs) {
    $full = Join-Path $RepoRoot $input.path
    $actualHash = Get-FileHashHex $full
    $producerFingerprints += [ordered]@{
        story = $input.story
        path = $input.path
        exists = Test-Path -LiteralPath $full
        sha256 = $actualHash
        placeholderAllowed = $false
    }

    if (-not (Test-Path -LiteralPath $full)) {
        Add-Failure $failures "Producer artifact missing for $($input.story): $($input.path)."
    }
    elseif (-not $expectedProducerFingerprints.ContainsKey($input.path)) {
        Add-Failure $failures "Producer fingerprint baseline missing for $($input.story): $($input.path)."
    }
    else {
        $expected = $expectedProducerFingerprints[$input.path]
        if ($expected.story -ne $input.story) {
            Add-Failure $failures "Producer fingerprint baseline story mismatch for $($input.path): expected $($input.story), got $($expected.story)."
        }

        if ($expected.sha256 -ne $actualHash) {
            Add-Failure $failures "Producer artifact stale for $($input.story): $($input.path). Expected $($expected.sha256), actual $actualHash."
        }
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
    inputRoots = @('docs/index.md', 'docs/fluent-ui-v5-contingency.md', 'docs/tutorials', 'docs/how-to', 'docs/reference', 'docs/concepts', 'docs/diagnostics', 'docs/migrations', 'docs/validation/producer-fingerprints.json', 'docs/validation/api-summary-baseline.txt')
    producerFingerprints = $producerFingerprints
    generatedOutputRoots = @($docfxOutput, 'artifacts/docs/mcp-reference', 'artifacts/docs/snippets', $(if (-not $SkipDocFx) { 'artifacts/docs/api-references' })) | Where-Object { $_ }
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
