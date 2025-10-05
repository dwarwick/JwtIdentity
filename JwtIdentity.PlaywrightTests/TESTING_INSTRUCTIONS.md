# Testing Instructions for Parallel Playwright Tests

## Prerequisites
1. The JwtIdentity server must be running (typically on https://localhost:5001)
2. The database must be set up with the Playwright test user
3. Playwright browsers must be installed

## Running Tests

### Individual Test Execution (Previous Behavior)
To run a single test class:
```bash
dotnet test JwtIdentity.PlaywrightTests/JwtIdentity.PlaywrightTests.csproj --filter "FullyQualifiedName~AuthTests"
```

This should pass (worked before the changes).

### Parallel Test Execution (New Behavior)
To run all tests in parallel:
```bash
dotnet test JwtIdentity.PlaywrightTests/JwtIdentity.PlaywrightTests.csproj
```

This should now pass with all tests running concurrently. Previously, this would fail due to shared Page instances.

### Verifying Parallel Execution
You can verify tests are running in parallel by:

1. **Check test output**: Tests will show overlapping start/completion times
2. **Check browser instances**: Only one browser should be launched, but multiple contexts/pages
3. **Performance**: Running all tests should be significantly faster than running them sequentially

### Expected Behavior

#### Before the Changes
- Running individual tests: ✅ Pass
- Running all tests together: ❌ Fail (race conditions on shared Page)

#### After the Changes
- Running individual tests: ✅ Pass (same as before)
- Running all tests together: ✅ Pass (isolated contexts prevent interference)
- Running with high parallelism: ✅ Pass (up to configured LevelOfParallelism)

## Test Isolation

Each test now has:
- **Own browser context**: Isolated cookies, storage, authentication
- **Own page instance**: No interference from other tests
- **Shared browser**: Efficient resource usage

## Performance Considerations

### Browser Startup
- Browser starts once per test run
- Browser closes when all test fixtures complete
- Much faster than launching browser per test

### Context Creation
- Each test creates a new context (~10-50ms overhead)
- Negligible compared to full browser launch (1-2 seconds)
- Ensures complete test isolation

## Troubleshooting

### Tests Still Fail in Parallel
If tests still fail when run in parallel, check:
1. Test dependencies on specific data state
2. Database race conditions
3. Server-side concurrency issues
4. Resource limits (too many concurrent connections)

### Browser Launch Errors
If browser fails to launch:
1. Ensure Playwright browsers are installed: `playwright install chromium`
2. Check executable path in PlaywrightHelper.cs
3. Verify headless mode configuration

### Context Creation Errors
If context creation fails:
1. Check browser instance is properly initialized
2. Verify locking mechanisms are working
3. Look for browser crashes or resource exhaustion

## Rollback Instructions

If the changes cause issues, you can revert to the previous behavior by:
1. Reverting the changes to `PlaywrightHelper.cs`
2. Reverting the changes to `AssemblyInfo.cs`
3. Or simply adjusting `AssemblyInfo.cs` to use `ParallelScope.Fixtures` instead of `ParallelScope.All`

## Additional Notes

- The `ParallelExecutionTests.cs` class demonstrates proper test isolation
- Tests can be run with different levels of parallelism by adjusting `LevelOfParallelism`
- AutoLogin functionality works correctly with per-test setup
- All existing tests should work without modification
