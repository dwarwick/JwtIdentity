#!/bin/bash
# Script to install Playwright browsers for testing
# This script is designed to work in the GitHub Copilot environment

set -e

echo "Installing Playwright browsers..."

# Ensure the PlaywrightTests project is built first
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PLAYWRIGHT_PROJECT="$PROJECT_ROOT/JwtIdentity.PlaywrightTests"
BUILD_OUTPUT="$PLAYWRIGHT_PROJECT/bin/Debug/net9.0"

# Build the project if not already built
if [ ! -d "$BUILD_OUTPUT" ]; then
    echo "Building PlaywrightTests project..."
    dotnet build "$PLAYWRIGHT_PROJECT/JwtIdentity.PlaywrightTests.csproj" --configuration Debug
fi

# Check if playwright.ps1 exists
if [ ! -f "$BUILD_OUTPUT/playwright.ps1" ]; then
    echo "Error: playwright.ps1 not found. Please build the PlaywrightTests project first."
    exit 1
fi

# Try to install using pwsh (preferred method)
if command -v pwsh &> /dev/null; then
    echo "Attempting to install browsers using PowerShell..."
    cd "$PLAYWRIGHT_PROJECT"
    if pwsh bin/Debug/net9.0/playwright.ps1 install chromium 2>&1; then
        echo "Browsers installed successfully using PowerShell!"
        exit 0
    else
        echo "PowerShell installation failed, trying alternative method..."
    fi
fi

# Alternative: Manual download method (fallback for environments where pwsh install fails)
echo "Using alternative download method..."

CACHE_DIR="$HOME/.cache/ms-playwright"
CHROMIUM_VERSION="1129"
CHROMIUM_DIR="$CACHE_DIR/chromium-$CHROMIUM_VERSION"
DOWNLOAD_URL="https://playwright.azureedge.net/builds/chromium/$CHROMIUM_VERSION/chromium-linux.zip"

# Check if already installed
if [ -d "$CHROMIUM_DIR/chrome-linux" ]; then
    echo "Chromium browser already installed at $CHROMIUM_DIR"
    exit 0
fi

# Create cache directory
mkdir -p "$CHROMIUM_DIR"

# Download chromium
echo "Downloading Chromium from $DOWNLOAD_URL..."
cd "$CHROMIUM_DIR"
curl -L -o chromium-linux.zip "$DOWNLOAD_URL"

# Extract
echo "Extracting Chromium..."
unzip -q chromium-linux.zip
rm chromium-linux.zip

# Verify installation
if [ -f "$CHROMIUM_DIR/chrome-linux/chrome" ]; then
    echo "Chromium installed successfully!"
    "$CHROMIUM_DIR/chrome-linux/chrome" --version
    exit 0
else
    echo "Error: Failed to install Chromium browser"
    exit 1
fi
