# Scripts Directory

This directory contains utility scripts for the JwtIdentity project.

## Available Scripts

### install-playwright-browsers.sh / install-playwright-browsers.ps1

Installs Playwright browsers (Chromium) needed for running end-to-end tests.

**Usage:**
```bash
# Bash
./scripts/install-playwright-browsers.sh

# PowerShell
pwsh ./scripts/install-playwright-browsers.ps1
```

**Features:**
- Automatically builds the PlaywrightTests project if needed
- Attempts standard Playwright CLI installation first
- Falls back to manual download if CLI fails (common in some environments)
- Verifies installation was successful

### verify-playwright-installation.sh / verify-playwright-installation.ps1

Verifies that Playwright browsers are installed and working correctly.

**Usage:**
```bash
# Bash
./scripts/verify-playwright-installation.sh

# PowerShell
pwsh ./scripts/verify-playwright-installation.ps1
```

**Features:**
- Creates a minimal test project
- Launches Chromium in headless mode
- Tests basic browser functionality
- Provides clear success/failure feedback

For detailed information, see [PLAYWRIGHT_BROWSER_INSTALLATION.md](./PLAYWRIGHT_BROWSER_INSTALLATION.md).

## For GitHub Copilot Agents

When working with this repository and needing to run Playwright tests:

1. First, run the browser installation script: `./scripts/install-playwright-browsers.sh`
2. Optionally verify installation: `./scripts/verify-playwright-installation.sh`
3. Ensure the server is running on https://localhost:5001
4. Run tests with: `dotnet test JwtIdentity.PlaywrightTests`

The installation script handles edge cases and environment-specific issues automatically.
