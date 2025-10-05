# Playwright Parallel Testing Support

## Overview
This document describes the changes made to enable unlimited parallel test execution in the Playwright test suite.

## Problem
Previously, the `PlaywrightHelper` base class used `[OneTimeSetUp]` and `[OneTimeTearDown]` attributes, which created a single browser instance, context, and page that was shared across all tests in a fixture. This caused issues when running multiple test suites in parallel because:

1. Multiple tests would try to use the same `Page` instance concurrently
2. This led to race conditions and test failures
3. Tests would pass individually but fail when run together

## Solution
The solution involves separating the lifecycle of browser resources:

### Shared Resources (Static, Thread-Safe)
- **Browser Instance**: A single browser instance is shared across all test fixtures
- **Playwright Instance**: A single Playwright driver instance is shared
- **Thread Safety**: Proper locking ensures thread-safe initialization

### Per-Test Resources (Instance-Based)
- **Browser Context**: Each test gets its own isolated browser context
- **Page**: Each test gets its own page instance

### Implementation Details

#### OneTimeSetUp (Per Fixture)
- Initializes shared browser instance on first use
- Uses double-checked locking pattern for thread safety
- Tracks number of active test fixtures
- Configures Playwright execution mode once

#### SetUp (Per Test)
- Creates a new browser context for each test
- Creates a new page within that context
- Handles auto-login if configured
- Calls `OnAfterSetupAsync()` hook

#### TearDown (Per Test)
- Closes the page after each test
- Closes the browser context after each test
- Ensures no resource leaks

#### OneTimeTearDown (Per Fixture)
- Closes shared browser when last fixture completes
- Disposes Playwright instance
- Restores environment configuration

## Benefits

1. **Parallel Execution**: Tests can now run in parallel without interference
2. **Isolation**: Each test has its own isolated browser context and page
3. **Efficiency**: Browser instance is shared, reducing overhead
4. **Scalability**: Supports unlimited number of parallel test suites
5. **Thread Safety**: Proper locking prevents race conditions during initialization

## Configuration

The `AssemblyInfo.cs` file has been updated to enable full parallelization:

```csharp
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(10)]
```

This allows:
- Tests within the same fixture to run in parallel
- Tests across different fixtures to run in parallel
- Up to 10 concurrent tests by default

## Migration Notes

No changes are required to existing test classes. All tests that inherit from `PlaywrightHelper` will automatically benefit from the parallel testing support.

## Performance Considerations

- Browser initialization happens only once per test run (or when all fixtures complete)
- Each test creates a new context and page, which has minimal overhead
- Context isolation ensures cookies, storage, and authentication are not shared between tests
- The shared browser approach is more efficient than launching a new browser for each test
