# Packages YoutubeConverter as a self-contained Windows app.
# Result: a single .exe + ffmpeg folder that can be copied to any Win10/11 machine.

$ErrorActionPreference = 'Stop'
$projectDir = Join-Path $PSScriptRoot 'src\YoutubeConverter'
$publishDir = Join-Path $PSScriptRoot 'dist'

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

Write-Host 'Publishing self-contained single-file build for win-x64...' -ForegroundColor Cyan
dotnet publish $projectDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host ''
Write-Host "Done! The app is in: $publishDir" -ForegroundColor Green
Write-Host '   Double-click YoutubeConverter.exe to run.'
