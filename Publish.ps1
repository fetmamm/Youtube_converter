# Paketerar YoutubeConverter som en självständig Windows-app.
# Resultatet: en enda .exe + ffmpeg-mapp som kan kopieras till valfri Win10/11-maskin.

$ErrorActionPreference = 'Stop'
$projectDir = Join-Path $PSScriptRoot 'src\YoutubeConverter'
$publishDir = Join-Path $PSScriptRoot 'dist'

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

Write-Host 'Publicerar self-contained single-file build for win-x64...' -ForegroundColor Cyan
dotnet publish $projectDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw 'Publish misslyckades.' }

Write-Host ''
Write-Host "Klart! Appen finns har: $publishDir" -ForegroundColor Green
Write-Host '   Dubbelklicka YoutubeConverter.exe for att kora.'
