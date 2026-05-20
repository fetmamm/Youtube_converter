# Bygger projektet i Release-läge.
$ErrorActionPreference = 'Stop'
$projectDir = Join-Path $PSScriptRoot 'src\YoutubeConverter'
dotnet build $projectDir -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build misslyckades.' }
Write-Host 'Build OK. Kor Run.vbs for att starta appen.' -ForegroundColor Green
