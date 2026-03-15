$ErrorActionPreference = 'Stop'

function Find-DocsMatches {
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

function Clear-DocsOutputs {
    Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter 'NativeWebView*.api.json' -Recurse -File |
        Where-Object { $_.FullName.Replace('\', '/') -like '*/obj/Release/*' } |
        Remove-Item -Force

    $apiCache = Join-Path $PSScriptRoot 'site/.lunet/build/cache/api/dotnet'
    $wwwRoot = Join-Path $PSScriptRoot 'site/.lunet/build/www'
    foreach ($path in @($apiCache, $wwwRoot)) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Push-Location $PSScriptRoot
try {
    $lockDir = Join-Path $PSScriptRoot 'site/.lunet/.build-lock'
    while ($true) {
        if (Test-Path $lockDir) {
            Start-Sleep -Seconds 1
            continue
        }

        try {
            New-Item -ItemType Directory -Path $lockDir -ErrorAction Stop | Out-Null
            break
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    dotnet tool restore
    Clear-DocsOutputs

    Push-Location site
    try {
        $lunetLog = [System.IO.Path]::GetTempFileName()
        try {
            dotnet tool run lunet --stacktrace build 2>&1 | Tee-Object -FilePath $lunetLog

            $lunetErrors = Find-DocsMatches -Pattern 'ERR lunet|Error while building api dotnet|Unable to select the api dotnet output' -Paths @($lunetLog)
            if ($LASTEXITCODE -eq 0 -and $lunetErrors) {
                throw "Lunet reported API/site build errors.`n$lunetErrors"
            }
        }
        finally {
            Remove-Item $lunetLog -Force -ErrorAction SilentlyContinue
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Remove-Item (Join-Path $PSScriptRoot 'site/.lunet/.build-lock') -Force -Recurse -ErrorAction SilentlyContinue
    Pop-Location
}
