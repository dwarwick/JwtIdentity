# Authorization and Permissions

## Authentication flow overview
SurveyShark relies on ASP.NET Core Identity backed by secure cookies and short-lived JWTs. Each request carries claims for roles and permissions, refreshed by the custom authentication state provider.

## Role hierarchy
- **Administrators** – manage billing, integrations, users, and surveys.
- **Survey Owners** – build surveys, analyze responses, and export results for assigned workspaces.
- **Contributors** – collaborate on question content with limited analytics access.

## Permission model
Common permissions include:
- `Surveys.Read` – view surveys and analytics.
- `Surveys.Manage` – create or modify surveys.
- `Responses.Export` – download CSV or Excel exports.
- `Users.Manage` – invite or suspend users.
- `Settings.Configure` – adjust tenant-wide settings.

## Best practices
Grant the least privilege required, audit access quarterly, enforce MFA for administrators, and document how exports are approved in your organization.
