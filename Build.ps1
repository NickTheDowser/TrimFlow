# TrimFlow Build Script
# Simple script to build and prepare TrimFlow for distribution

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TrimFlow Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$Version = "1.0.0"
$ProjectName = "TrimFlow"
$Configuration = "Release"
$Runtime = "win-x64"

# Paths
$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "$ProjectName.csproj"
$PublishDir = Join-Path $ProjectRoot "bin\$Configuration\net10.0-windows\$Runtime\publish"
$DistDir = Join-Path $ProjectRoot "dist"

Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}
if (Test-Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

Write-Host "[2/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectFile

Write-Host "[3/4] Building and publishing..." -ForegroundColor Yellow
dotnet publish $ProjectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[4/4] Creating portable package..." -ForegroundColor Yellow
$ZipFile = Join-Path $DistDir "TrimFlow_v${Version}_Portable.zip"
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipFile -Force

$ZipSize = "{0:N2} MB" -f ((Get-Item $ZipFile).Length / 1MB)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $ZipFile" -ForegroundColor White
Write-Host "Size: $ZipSize" -ForegroundColor White
Write-Host ""
Write-Host "You can now:" -ForegroundColor Cyan
Write-Host "  1. Test the portable version" -ForegroundColor Gray
Write-Host "  2. Create an installer with Inno Setup" -ForegroundColor Gray
Write-Host "  3. Upload to GitHub Releases" -ForegroundColor Gray
Write-Host ""