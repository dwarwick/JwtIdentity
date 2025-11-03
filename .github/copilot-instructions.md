# JwtIdentity - Copilot Instructions

## Project Overview
This is a Blazor WebAssembly project with a server-side API. The solution uses .NET 9 and follows a clean architecture pattern with separated concerns.

### Solution Structure
- **JwtIdentity**: ASP.NET Core server project providing the API and hosting the Blazor app
- **JwtIdentity.Client**: Blazor WebAssembly client application
- **JwtIdentity.Common**: Shared library containing ViewModels, DTOs, and helper methods
- **JwtIdentity.Tests**: Server-side unit tests using NUnit
- **JwtIdentity.BunitTests**: Blazor component tests using bUnit
- **JwtIdentity.PlaywrightTests**: End-to-end tests using Playwright and NUnit

## Technology Stack
- **.NET 9**: Latest .NET framework features and libraries
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

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test JwtIdentity.Tests
dotnet test JwtIdentity.BunitTests
dotnet test JwtIdentity.PlaywrightTests

# Install Playwright browsers (after first build)
pwsh bin/Debug/net9.0/playwright.ps1 install
```

## Build and Deployment

### Build Commands
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run the application
dotnet run --project JwtIdentity
```

### Database
- SQL Server database managed by EF Core migrations
- Database created automatically on first run with seed data
- Migration files located in `JwtIdentity/Migrations`

## Additional Resources
- See `AGENTS.md` for project-specific copilot instructions
- See `ADDING_NEW_QUESTION_TYPES.md` for guidance on extending question types