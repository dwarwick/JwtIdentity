#!/usr/bin/env pwsh
# Script to install Playwright browsers for testing
# This script is designed to work in the GitHub Copilot environment

Write-Host "Installing Playwright browsers..." -ForegroundColor Green

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$PlaywrightProject = Join-Path $ProjectRoot "JwtIdentity.PlaywrightTests"
$BuildOutput = Join-Path $PlaywrightProject "bin\Debug\net9.0"

# Build the project if not already built
if (-not (Test-Path $BuildOutput)) {
    Write-Host "Building PlaywrightTests project..." -ForegroundColor Yellow
    $ProjectFile = Join-Path $PlaywrightProject "JwtIdentity.PlaywrightTests.csproj"
    dotnet build $ProjectFile --configuration Debug
}

# Check if playwright.ps1 exists
$PlaywrightScript = Join-Path $BuildOutput "playwright.ps1"
if (-not (Test-Path $PlaywrightScript)) {
    Write-Error "playwright.ps1 not found. Please build the PlaywrightTests project first."
    exit 1
}

# Try to install using pwsh playwright.ps1
Write-Host "Attempting to install browsers using Playwright CLI..." -ForegroundColor Yellow
Push-Location $PlaywrightProject
try {
    & $PlaywrightScript install chromium
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Browsers installed successfully!" -ForegroundColor Green
        Pop-Location
        exit 0
    }
    else {
        Write-Host "Playwright CLI installation encountered issues, trying alternative method..." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Error during installation: $_" -ForegroundColor Red
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
}
finally {
    Pop-Location
}

# Alternative: Manual download method (fallback)
Write-Host "Using alternative download method..." -ForegroundColor Yellow

$CacheDir = if ($IsWindows) { 
    Join-Path $env:LOCALAPPDATA "ms-playwright" 
} else { 
    Join-Path $HOME ".cache/ms-playwright" 
}
$ChromiumVersion = "1129"
$ChromiumDir = Join-Path $CacheDir "chromium-$ChromiumVersion"
$DownloadUrl = "https://playwright.azureedge.net/builds/chromium/$ChromiumVersion/chromium-linux.zip"

# Check if already installed
$ChromeLinux = Join-Path $ChromiumDir "chrome-linux"
if (Test-Path $ChromeLinux) {
    Write-Host "Chromium browser already installed at $ChromiumDir" -ForegroundColor Green
    exit 0
}

# Create cache directory
New-Item -ItemType Directory -Path $ChromiumDir -Force | Out-Null

# Download chromium
Write-Host "Downloading Chromium from $DownloadUrl..." -ForegroundColor Yellow
$ZipPath = Join-Path $ChromiumDir "chromium-linux.zip"
try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -UseBasicParsing
}
catch {
    Write-Error "Failed to download Chromium: $_"
    exit 1
}

# Extract
Write-Host "Extracting Chromium..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $ZipPath -DestinationPath $ChromiumDir -Force
    Remove-Item $ZipPath
}
catch {
    Write-Error "Failed to extract Chromium: $_"
    exit 1
}

# Verify installation
$ChromeBinary = Join-Path $ChromeLinux "chrome"
if (Test-Path $ChromeBinary) {
    Write-Host "Chromium installed successfully!" -ForegroundColor Green
    if (-not $IsWindows) {
        & $ChromeBinary --version
    }
    exit 0
}
else {
    Write-Error "Failed to install Chromium browser"
    exit 1
}
