# Playwright Browser Installation for GitHub Copilot

This document explains how to install Playwright browsers in the GitHub Copilot environment to enable running Playwright tests.

## Quick Start

To install Playwright browsers (Chromium), run one of the following commands from the repository root:

### Using Bash (Recommended)
```bash
./scripts/install-playwright-browsers.sh
```

### Using PowerShell
```powershell
pwsh ./scripts/install-playwright-browsers.ps1
```

## Verification

After installation, you can verify that browsers are working correctly:

### Using Bash
```bash
./scripts/verify-playwright-installation.sh
```

### Using PowerShell
```powershell
pwsh ./scripts/verify-playwright-installation.ps1
```

The verification script will launch a headless Chromium browser and test basic functionality. You should see:
```
✓ SUCCESS: Playwright browsers are installed and working correctly!
```

## What the Script Does

The installation script performs the following steps:

1. **Builds the PlaywrightTests project** (if not already built) to ensure the playwright.ps1 script is available
2. **Attempts to install using Playwright CLI** (pwsh playwright.ps1 install chromium)
3. **Falls back to manual download** if the CLI method fails (which is common in some environments)
4. **Downloads Chromium** from the official Playwright CDN
5. **Extracts the browser** to `~/.cache/ms-playwright/chromium-1129`
6. **Verifies the installation** by checking the browser version

## Prerequisites

- .NET 9 SDK (for building the test project)
- PowerShell (pwsh) - usually available in GitHub Copilot environments
- curl and unzip utilities (for the fallback method)

## Manual Installation (Alternative)

If the scripts don't work, you can manually install the browsers:

```bash
# Build the test project first
dotnet build JwtIdentity.PlaywrightTests/JwtIdentity.PlaywrightTests.csproj

# Navigate to the test project
cd JwtIdentity.PlaywrightTests

# Install browsers using PowerShell
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

## Troubleshooting

### "playwright.ps1 not found"
Make sure you've built the PlaywrightTests project first:
```bash
dotnet build JwtIdentity.PlaywrightTests/JwtIdentity.PlaywrightTests.csproj
```

### "Download failed: size mismatch"
This is a known issue with the Playwright CLI installer in some environments. The installation script automatically falls back to a manual download method that works reliably.

### "Permission denied"
If you get permission errors when running the bash script, make sure it's executable:
```bash
chmod +x scripts/install-playwright-browsers.sh
```

## Running Tests After Installation

Once browsers are installed, you can run the Playwright tests:

```bash
# Run all tests
dotnet test JwtIdentity.PlaywrightTests

# Run specific test class
dotnet test JwtIdentity.PlaywrightTests --filter "FullyQualifiedName~AuthTests"
```

## Notes for GitHub Copilot Agents

When working with Playwright tests in this repository:

1. **Always install browsers first** before attempting to run tests
2. Use the installation script: `./scripts/install-playwright-browsers.sh`
3. The script handles both normal and fallback installation methods
4. Browser binaries are cached in `~/.cache/ms-playwright/` and don't need to be reinstalled for each test run
5. The server must be running on https://localhost:5001 before running tests (see TESTING_INSTRUCTIONS.md)

## Browser Location

Browsers are installed to:
- **Linux/macOS**: `~/.cache/ms-playwright/chromium-1129/chrome-linux/`
- **Windows**: `%LOCALAPPDATA%\ms-playwright\chromium-1129\chrome-win\`

## References

- [Playwright Documentation](https://playwright.dev/docs/intro)
- [JwtIdentity.PlaywrightTests/TESTING_INSTRUCTIONS.md](../JwtIdentity.PlaywrightTests/TESTING_INSTRUCTIONS.md) - Detailed testing instructions
- [AGENTS.md](../AGENTS.md) - General agent instructions including Playwright test guidelines
