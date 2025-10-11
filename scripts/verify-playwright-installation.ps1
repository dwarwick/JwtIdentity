#!/usr/bin/env pwsh
# Quick verification script to test if Playwright browsers are installed correctly
# This creates a minimal Playwright test without requiring the full server

Write-Host "Verifying Playwright browser installation..." -ForegroundColor Green

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$PlaywrightProject = Join-Path $ProjectRoot "JwtIdentity.PlaywrightTests"
$BuildOutput = Join-Path $PlaywrightProject "bin\Debug\net9.0"

# Check if project is built
if (-not (Test-Path $BuildOutput)) {
    Write-Host "Building PlaywrightTests project..." -ForegroundColor Yellow
    $ProjectFile = Join-Path $PlaywrightProject "JwtIdentity.PlaywrightTests.csproj"
    dotnet build $ProjectFile --configuration Debug
}

# Create a temporary test script
$TestScript = @"
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
"@

# Create temp directory and test file
$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) "playwright-verify"
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

$TempTestFile = Join-Path $TempDir "verify.cs"
$TempProjectFile = Join-Path $TempDir "verify.csproj"

# Create minimal csproj
$ProjectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.46.0" />
  </ItemGroup>
</Project>
"@

$TestScript | Out-File -FilePath $TempTestFile -Encoding UTF8
$ProjectContent | Out-File -FilePath $TempProjectFile -Encoding UTF8

Write-Host "Running verification test..." -ForegroundColor Yellow
Push-Location $TempDir
try {
    dotnet run
    $exitCode = $LASTEXITCODE
    Pop-Location
    
    # Clean up
    Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    
    exit $exitCode
}
catch {
    Pop-Location
    Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Error "Verification failed: $_"
    exit 1
}
