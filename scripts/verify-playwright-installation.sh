#!/bin/bash
# Quick verification script to test if Playwright browsers are installed correctly

set -e

echo "Verifying Playwright browser installation..."

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PLAYWRIGHT_PROJECT="$PROJECT_ROOT/JwtIdentity.PlaywrightTests"
BUILD_OUTPUT="$PLAYWRIGHT_PROJECT/bin/Debug/net9.0"

# Build the project if not already built
if [ ! -d "$BUILD_OUTPUT" ]; then
    echo "Building PlaywrightTests project..."
    dotnet build "$PLAYWRIGHT_PROJECT/JwtIdentity.PlaywrightTests.csproj" --configuration Debug
fi

# Create a temporary test directory
TEMP_DIR=$(mktemp -d)
trap 'rm -rf "$TEMP_DIR"' EXIT

# Create test file
cat > "$TEMP_DIR/verify.cs" << 'EOF'
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Creating Playwright instance...");
        using var playwright = await Playwright.CreateAsync();
        
        Console.WriteLine("Launching Chromium browser...");
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        
        Console.WriteLine("Creating browser context...");
        var context = await browser.NewContextAsync();
        
        Console.WriteLine("Creating new page...");
        var page = await context.NewPageAsync();
        
        Console.WriteLine("Setting page content...");
        await page.SetContentAsync("<html><head><title>Playwright Test</title></head><body><h1>Success!</h1></body></html>");
        
        var title = await page.TitleAsync();
        Console.WriteLine($"Page title: {title}");
        
        var heading = await page.Locator("h1").TextContentAsync();
        Console.WriteLine($"Page heading: {heading}");
        
        await browser.CloseAsync();
        
        if (title == "Playwright Test" && heading == "Success!")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ SUCCESS: Playwright browsers are installed and working correctly!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ FAILED: Unexpected results - Title: '{title}', Heading: '{heading}'");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
}
EOF

# Create minimal csproj
cat > "$TEMP_DIR/verify.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.46.0" />
  </ItemGroup>
</Project>
EOF

echo "Running verification test..."
cd "$TEMP_DIR"
dotnet run
