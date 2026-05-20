# Builds the project in Release mode.
$ErrorActionPreference = 'Stop'
$projectDir = Join-Path $PSScriptRoot 'src\YoutubeConverter'
dotnet build $projectDir -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Host 'Build OK. Run Run.vbs to start the app.' -ForegroundColor Green
