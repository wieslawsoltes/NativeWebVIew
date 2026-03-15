$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-docs.ps1')

$docRoot = Join-Path $PSScriptRoot 'site/.lunet/build/www'
$repoName = 'NativeWebView'
$legacyRepoName = $repoName -replace 'View$', 'VIew'
$legacyGitHubRepoUrl = "https://github.com/wieslawsoltes/$legacyRepoName"
$legacyPagesUrl = "https://wieslawsoltes.github.io/$legacyRepoName"
$legacyBasepathPattern = "(href|src)=`"/$legacyRepoName/"

function Find-GeneratedHtmlMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [switch]$Fixed,
        [switch]$CaseSensitive
    )

    if (Get-Command rg -ErrorAction SilentlyContinue) {
        $arguments = @('-n')
        if ($Fixed) {
            $arguments += '-F'
        }
        else {
            $arguments += '-e'
        }

        if ($CaseSensitive) {
            $arguments += '--case-sensitive'
        }

        $arguments += $Pattern
        $arguments += $Paths
        return & rg @arguments
    }

    $matches = @()
    foreach ($path in $Paths) {
        $selectStringParams = @{
            Path        = $path
            Pattern     = $Pattern
            AllMatches  = $true
            SimpleMatch = $Fixed.IsPresent
        }

        if ($CaseSensitive) {
            $selectStringParams.CaseSensitive = $true
        }

        $results = Select-String @selectStringParams -ErrorAction SilentlyContinue
        if ($results) {
            $matches += $results | ForEach-Object { "{0}:{1}:{2}" -f $_.Path, $_.LineNumber, $_.Line.Trim() }
        }
    }

    return $matches
}

$requiredFiles = @(
    (Join-Path $docRoot 'index.html'),
    (Join-Path $docRoot 'api/index.html'),
    (Join-Path $docRoot 'articles/index.html'),
    (Join-Path $docRoot 'articles/getting-started/index.html'),
    (Join-Path $docRoot 'articles/controls/index.html'),
    (Join-Path $docRoot 'articles/rendering/index.html'),
    (Join-Path $docRoot 'articles/platforms/index.html'),
    (Join-Path $docRoot 'articles/diagnostics/index.html'),
    (Join-Path $docRoot 'articles/reference/index.html'),
    (Join-Path $docRoot 'articles/getting-started/overview/index.html'),
    (Join-Path $docRoot 'articles/getting-started/installation/index.html'),
    (Join-Path $docRoot 'articles/getting-started/quickstart/index.html'),
    (Join-Path $docRoot 'articles/getting-started/sample-feature-explorer/index.html'),
    (Join-Path $docRoot 'articles/controls/nativewebview/index.html'),
    (Join-Path $docRoot 'articles/controls/nativewebdialog/index.html'),
    (Join-Path $docRoot 'articles/controls/webauthenticationbroker/index.html'),
    (Join-Path $docRoot 'articles/rendering/render-modes/index.html'),
    (Join-Path $docRoot 'articles/platforms/windows/index.html'),
    (Join-Path $docRoot 'articles/platforms/browser/index.html'),
    (Join-Path $docRoot 'articles/diagnostics/platform-prerequisites/index.html'),
    (Join-Path $docRoot 'articles/reference/platform-support-matrix/index.html'),
    (Join-Path $docRoot 'articles/reference/package-layout/index.html'),
    (Join-Path $docRoot 'articles/reference/lunet-docs-pipeline/index.html'),
    (Join-Path $docRoot 'articles/reference/license/index.html'),
    (Join-Path $docRoot 'css/lite.css')
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        throw "Required docs output missing: $file"
    }
}

$htmlFiles = Get-ChildItem -Path $docRoot -Filter *.html -Recurse -File | ForEach-Object { $_.FullName }
$narrativeHtmlFiles = @((Join-Path $docRoot 'index.html'))
$narrativeHtmlFiles += Get-ChildItem -Path (Join-Path $docRoot 'articles') -Filter *.html -Recurse -File | ForEach-Object { $_.FullName }

$rawMarkdownLinks = Find-GeneratedHtmlMatches -Pattern 'href="[^"]*\.md([?#"][^"]*)?"' -Paths $htmlFiles
if ($rawMarkdownLinks) {
    $internalMarkdownLinks = $rawMarkdownLinks | Where-Object { $_ -notmatch 'href="https?://' }
    if ($internalMarkdownLinks) {
        $joinedInternalMarkdownLinks = $internalMarkdownLinks -join "`n"
        throw "Generated docs contain raw .md links.`n$joinedInternalMarkdownLinks"
    }
}

$readmeRoutes = Find-GeneratedHtmlMatches -Pattern 'href="[^"]*/readme([?#"][^"]*)?"' -Paths $htmlFiles
if ($readmeRoutes) {
    $joinedReadmeRoutes = $readmeRoutes -join "`n"
    throw "Generated docs contain /readme routes instead of directory routes.`n$joinedReadmeRoutes"
}

$staleApiRoutes = Find-GeneratedHtmlMatches -Pattern 'href="[^"]*/api/index\.md([?#"][^"]*)?"' -Paths $htmlFiles
if ($staleApiRoutes) {
    $joinedStaleApiRoutes = $staleApiRoutes -join "`n"
    throw "Generated docs contain stale /api/index.md links.`n$joinedStaleApiRoutes"
}

$rawMarkdownOutputs = Get-ChildItem -Path (Join-Path $docRoot 'articles') -Filter *.md -Recurse -ErrorAction SilentlyContinue
if ($rawMarkdownOutputs.Count -gt 0) {
    $paths = ($rawMarkdownOutputs | ForEach-Object { $_.FullName }) -join "`n"
    throw "Generated docs still contain raw .md article outputs.`n$paths"
}

$oldGitHubRepoMatches = Find-GeneratedHtmlMatches -Pattern $legacyGitHubRepoUrl -Paths $narrativeHtmlFiles -Fixed
if ($oldGitHubRepoMatches) {
    $joinedOldGitHubRepoMatches = $oldGitHubRepoMatches -join "`n"
    throw "Generated docs still contain the legacy GitHub repository URL.`n$joinedOldGitHubRepoMatches"
}

$oldPagesUrlMatches = Find-GeneratedHtmlMatches -Pattern $legacyPagesUrl -Paths $narrativeHtmlFiles -Fixed
if ($oldPagesUrlMatches) {
    $joinedOldPagesUrlMatches = $oldPagesUrlMatches -join "`n"
    throw "Generated docs still contain the legacy GitHub Pages URL.`n$joinedOldPagesUrlMatches"
}

$oldBasepathMatches = Find-GeneratedHtmlMatches -Pattern $legacyBasepathPattern -Paths $htmlFiles
if ($oldBasepathMatches) {
    $joinedOldBasepathMatches = $oldBasepathMatches -join "`n"
    throw "Generated docs still contain the legacy production base path.`n$joinedOldBasepathMatches"
}

$badFooterText = Find-GeneratedHtmlMatches -Pattern 'Creative Commons|CC BY 2.5' -Paths @(
    (Join-Path $docRoot 'index.html'),
    (Join-Path $docRoot 'articles/getting-started/overview/index.html')
)
if ($badFooterText) {
    $joinedBadFooterText = $badFooterText -join "`n"
    throw "Generated docs contain the default Creative Commons footer instead of the project MIT license footer.`n$joinedBadFooterText"
}

$missingMitFooter = Find-GeneratedHtmlMatches -Pattern 'MIT license' -Paths @((Join-Path $docRoot 'index.html')) -Fixed
if (-not $missingMitFooter) {
    throw 'Generated site footer is missing the project MIT license text.'
}

$nativeWebViewApiPage = Join-Path $docRoot 'api/NativeWebView.Controls.NativeWebView/index.html'
if (-not (Test-Path $nativeWebViewApiPage)) {
    throw "Expected NativeWebView API page is missing: $nativeWebViewApiPage"
}

$missingAvaloniaLink = Find-GeneratedHtmlMatches -Pattern 'https://api-docs.avaloniaui.net/docs/Avalonia.Controls.Control/' -Paths @($nativeWebViewApiPage) -Fixed
if (-not $missingAvaloniaLink) {
    throw 'Generated NativeWebView API page is missing the external Avalonia.Controls.Control link.'
}

$gettingStartedIndexPage = Join-Path $docRoot 'articles/getting-started/index.html'
$missingBasepathCss = Find-GeneratedHtmlMatches -Pattern "/$repoName/css/lite.css" -Paths @($gettingStartedIndexPage) -Fixed
if (-not $missingBasepathCss) {
    throw 'Production getting-started page is missing the project-basepath-prefixed lite.css URL.'
}
