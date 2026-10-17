# Stand-ins (delete on merge)

Everything in this folder, in `wwwroot/standins/`, and in the stand-in views listed below exists only so the
demo runs on its own. The real Reflect XR app already provides each piece.

| Stand-in | Replaced on merge by |
|---|---|
| `Auth/DemoAuthenticationHandler.cs`, `Auth/DemoUsers.cs`, `Controllers/DemoUserController.cs` | Microsoft Entra External ID sign-in |
| `Auth/DemoCurrentUserService.cs` | The real `ICurrentUserService` |
| `Security/CspNonceExtensions.cs`, `Security/SecurityHeadersMiddleware.cs` | The real `GetCspNonce()` and CSP header |
| `Errors/EntityNotFoundExceptionFilter.cs` | The real error page / JSON error mapping |
| `Data/**` (in-memory repositories, synthetic seeder, analytics event source) | EF Core repositories and the BigQuery sync job |
| `Menu/**` | The real sidebar menu builder (copy `Areas/Provider/provider-menu.json` into it) |
| `StandinServiceCollectionExtensions.cs` | Real DI registrations |
| `Views/Shared/_Layout.cshtml`, `Views/Shared/_Standin*.cshtml`, `Views/Shared/Components/SidebarMenu/*`, `Views/Account/*`, `Views/Shared/NotFound.cshtml`, `Views/Shared/Error.cshtml`, `Controllers/HomeController.cs` | The real layout, sign-in area, and error pages |
