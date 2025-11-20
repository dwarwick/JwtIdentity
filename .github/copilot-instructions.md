# JwtIdentity - Copilot Instructions

## Project Overview
This is a Blazor WebAssembly project with a server-side API. The solution uses .NET 10 and follows a clean architecture pattern with separated concerns.

### Solution Structure
- **JwtIdentity**: ASP.NET Core server project providing the API and hosting the Blazor app
- **JwtIdentity.Client**: Blazor WebAssembly client application
- **JwtIdentity.Common**: Shared library containing ViewModels, DTOs, and helper methods
- **JwtIdentity.Tests**: Server-side unit tests using NUnit
- **JwtIdentity.BunitTests**: Blazor component tests using bUnit
- **JwtIdentity.PlaywrightTests**: End-to-end tests using Playwright and NUnit

## Technology Stack
- **.NET 10**: Latest .NET framework features and libraries
- **Blazor WebAssembly**: Client-side SPA framework
- **Entity Framework Core**: ORM for SQL Server database
- **AutoMapper**: Object-to-object mapping
- **MudBlazor**: UI component library
- **Syncfusion Blazor**: Advanced data grid components
- **Cookie Authentication**: For user authentication and authorization
- **SignalR**: Real-time communication (if applicable)

## Development Guidelines

### Architecture Patterns
- The Common project contains ViewModels and helper methods shared between client and server
- Controller endpoints accept and return ViewModels (located in Common project)
- Use AutoMapper for entity-to-DTO mapping (configuration in `Configurations/MapperConfig.cs`)
- Database uses EF Core migrations; connection strings in `appsettings.json` and `appsettings.Development.json`

### Razor Component Conventions
- **Always create code-behind files** for Razor components and pages
- Code-behind file naming: `[ComponentName]Model.razor.cs` (e.g., `MyComponent.razor` → `MyComponentModel.razor.cs`)
- The Razor component must inherit from its code-behind class
- Code-behind classes must inherit from `BlazorBase`
- **Never inject services** in the component or code-behind; use services from `BlazorBase`
- For dialogs, use examples from `Pages\Admin\Dialogs` and `Pages\Common`

### API and Service Usage
- Client makes API calls using `IApiService` (injected via `BlazorBase`)
- Review `IApiService` methods to understand available API endpoints
- Access API methods through dependency injection in Razor components

### UI Component Guidelines
- **Syncfusion Data Grids**: Use for data tables; see examples in `LogsGrid.razor`, `ManageFeedback.razor`, `MyFeedback.razor`, `Filter.razor`, `SurveysIAnswered.razor`, `SurveysICreated.razor`
- **MudList**: Always add `T="string"` parameter; see `Home.razor` or `Documentation.razor`
- **MudChip**: Do not use `Closable` attribute; define `OnClose` event instead (see `EditUserDialog.razor`)
- **MudStack**: Use `Wrap` enum value for the Wrap attribute (see `DemoLanding.razor`)

### Styling Conventions
- `app-dark.css`: Dark mode styles only
- `app-light.css`: Light mode styles only
- `app.css`: Common styles for both modes
- Do not mix dark/light mode styles into common CSS

### Code Quality Standards
- **Nullable Reference Types**: Disabled in all projects; do not enable or use nullable annotations
- **Warnings**: Treat as errors and fix before committing
- Check for new warnings after each build
- All tests must pass before creating a PR

## Testing

### Test Frameworks
- **NUnit**: Server-side unit tests and Playwright E2E tests
- **bUnit**: Blazor component tests
- **Playwright**: End-to-end browser tests

### Testing Requirements
**IMPORTANT**: When making code changes, always create or update appropriate tests:

#### When to Create bUnit Tests
- New Blazor components or pages
- Updates to existing component behavior
- Demo functionality changes
- UI interaction logic
- Component state management

#### When to Create Unit Tests
- New API controllers or endpoints
- Service layer logic
- Business logic in helpers or utilities
- Data access layer changes
- AutoMapper configurations

#### When to Create Playwright Tests (Optional)
- Critical end-to-end user workflows
- Multi-page interactions
- Only in local development environment (not automated CI/CD)

### Test Structure Guidelines

#### bUnit Test Structure
**CRITICAL: All bUnit test classes MUST inherit from `BUnitTestBase`** 

This is mandatory to avoid MudPopoverProvider and service registration issues. The base class provides:
- Pre-configured `TestContext` with all necessary services
- Pre-rendered `MudPopoverProvider` (prevents MudBlazor popover errors)
- Mocked services (AuthService, ApiService, LocalStorage, etc.) via protected properties
- MockNavigationManager for navigation testing
- MudBlazor and Syncfusion services registered
- `AssertPopoverText(string expectedText)` helper method for verifying demo popup content

**Key Requirements:**
1. **Never** create your own `TestContext` - use the `Context` property from the base class
2. **Always** use base class mock properties: `ApiServiceMock`, `AuthServiceMock`, `AuthStateProviderMock`, etc.
3. **Reset mocks** in `[SetUp]` if you need clean state between tests: `ApiServiceMock.Reset();`
4. **Do not** duplicate MockNavigationManager or service registration code

**IMPORTANT: Demo Popup Testing**
When testing demo functionality with `DemoPopup` components:
- **ALWAYS** use the `AssertPopoverText(string expectedText)` method to verify popup content
- This method checks that the expected text appears in the MudPopoverProvider markup
- Call it after rendering the component and waiting for the demo step to be active
- Example:
  ```csharp
  AssertPopoverText("Welcome to Branching Configuration!");
  AssertPopoverText("Click the button to continue");
  ```

**Example Test Structure:**
```csharp
[TestFixture]
public class MyComponentTests : BUnitTestBase
{
    [SetUp]
    public void Setup()
    {
        // Reset mocks if needed for clean state between tests
        ApiServiceMock.Reset();
        AuthServiceMock.Reset();
        
        // Additional test-specific setup
        // Context, AuthServiceMock, ApiServiceMock, etc. are available from base class
        ApiServiceMock.Setup(x => x.GetAsync<SomeType>(It.IsAny<string>()))
            .ReturnsAsync(new SomeType());
    }

    [Test]
    public void Component_Scenario_ExpectedBehavior()
    {
        // Arrange - use base class properties
        
        // Act - use Context.RenderComponent from base class
        var cut = Context.RenderComponent<MyComponent>();
        
        // Assert
        Assert.That(cut.Markup, Does.Contain("Expected Text"));
    }
}
```

#### Unit Test Structure
```csharp
[TestFixture]
public class MyServiceTests
{
    private MyService _service;
    private Mock<IDependency> _dependencyMock;

    [SetUp]
    public void Setup()
    {
        _dependencyMock = new Mock<IDependency>();
        _service = new MyService(_dependencyMock.Object);
    }

    [Test]
    public async Task Method_Scenario_ExpectedResult()
    {
        // Arrange
        // Act
        var result = await _service.MethodAsync();
        // Assert
        Assert.That(result, Is.Not.Null);
    }
}
```

### Running Tests
```bash
# Run all tests (excluding Playwright)
dotnet test --filter "FullyQualifiedName!~Playwright"

# Run specific test project
dotnet test JwtIdentity.Tests
dotnet test JwtIdentity.BunitTests
dotnet test JwtIdentity.PlaywrightTests  # Local only

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage Expectations
- Aim for >80% coverage on new code
- All critical paths must have tests
- Demo functionality must have comprehensive bUnit tests
- API endpoints must have unit tests

## Testing Environment Restrictions

**DO NOT RUN PLAYWRIGHT TESTS in the automated testing environment.** Playwright tests require:
- A running server instance
- Chrome/Chromium browser installation
- Full browser automation infrastructure

These are not available in the CI/CD testing environment. When running tests:
- ✅ DO run: `dotnet test --filter "FullyQualifiedName!~Playwright"`
- ❌ DO NOT run: `dotnet test` (includes Playwright tests)
- ✅ DO run: BUnit tests and integration tests only
- ❌ DO NOT attempt to install browsers or run Playwright in automated workflows

Playwright tests should only be run in local development environments with full browser support.


## Build and Deployment

### Build Commands
bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run the application
dotnet run --project JwtIdentity


### Database
- SQL Server database managed by EF Core migrations
- Database created automatically on first run with seed data
- Migration files located in `JwtIdentity/Migrations`

## Additional Resources
- See `AGENTS.md` for project-specific copilot instructions
- See `ADDING_NEW_QUESTION_TYPES.md` for guidance on extending question types