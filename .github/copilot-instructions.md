This is a blazor webassembly project that uses a server project that provides an API. 
Both projects are in the same solution. They both use .Net9.
The solution also contains a Common Library for shared functionality, which can be utilized across both the Blazor WebAssembly and the server project for consistency and code reuse.
The common project contains ViewModels and static classes and helper methods that are used by the client and server project. This enhances maintainability and promotes a clean architecture in the solution.
The solution uses cookie authentication and authorization for the Blazor WebAssembly project. Additionally, best practices are followed for security and performance optimizations.
The Blazor WebAssembly project is configured to use the latest .NET 9 features and libraries, ensuring that it is up-to-date with the latest advancements in the .NET ecosystem.
The solution is designed to be modular and scalable, allowing for easy addition of new features and components in the future.
The project structure is organized to separate concerns, making it easier to manage and understand the codebase.
The solution is built using the latest .NET 9 features and libraries, ensuring that it is up-to-date with the latest advancements in the .NET ecosystem.
The server project uses ef core and has a database connection string in the appsettings.json file and appsettings.Development.json file. The connection string is used to connect to a SQL Server database.
The database is created using ef core migrations, and the initial migration is included in the project. The database is created when the application is run for the first time, and the data is seeded with some initial data.
When building datagrids, use Syncfusion Blazor components for data grids. Examples of how to use Syncfusion Blazor components are included in the project in LogsGrid.razor, ManageFeedback.razor, MyFeedback.razor, Filter.razor, SurveysIAnswered.razor, and SurverysICreated.razor.

When creating razor components or blazor pages, always create razor.cs code behind files.
The class name for the code behind file should be named the same as the razor component file, plus Model. For example, if the razor component file is named MyComponent.razor, the code behind file should be named MyComponentModel.razor.cs.
The razor component file should inherit from the code behind file. For example, if the code behind file is named MyComponentModel.razor.cs, the razor component file should inherit from MyComponentModel.
The code behind file should inherit from BlazorBase. All of the injected services are in the BlazorBase class. Do not inject services in the razor component file or the code behind file. Always use the injected services from BlazorBase.
When creating a new dialog, always use examples from Pages\Admin\Dialogs and Pages\Common.
When creating a <MudList, always add the T parameter as T="string". See Home.razor for an example, or Documentation.razor

The API project uses AutoMapper for mapping between entities and DTOs. The mapping profiles are located in the API project, and the AutoMapper configuration is done in Configurations/MapperConfig.cs.
The controller endpoints that take a body take and return Viewmodels. The ViewModels are located in the Common project. 
The client project makes API calls using IApiService. Please review the methods in this file to understand how to make API calls. The IApiService is injected in the BlazorBase class, so you can use it directly in inherited Razor components and access API methods through dependency injection.
When using Mudchip, do not add a Closable attribute. To make it closable, just define the OnClose event. See the example in EditUserDialog.razor.
When creating a new Razor component, always create a code behind file with the same name as the Razor component file, plus Model. For example, if the Razor component file is named MyComponent.razor, the code behind file should be named MyComponentModel.razor.cs.

Do not create a PR unless all tests pass.
AGENTS.md files in the solution also contain copilot instructions for specific projects.29.MudBlazor MudCheckBox API: Use Value and ValueChanged properties (NOT Checked/CheckedChanged which are deprecated). Example: `<MudCheckBox T="bool" Value="@myValue" ValueChanged="@((bool val) => HandleChange(val))" />`
Always create comprehensive BUnit Tests when creating new Razor components or pages. Use existing tests in the Tests project as examples.
When creating a new Razor component or page, always create a corresponding BUnit test class in the Tests project. The test class should be named the same as the Razor component or page, plus "Tests". For example, if the Razor component is named MyComponent.razor, the test class should be named MyComponentTests.cs.