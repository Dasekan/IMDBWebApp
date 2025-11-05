# IMDBWebApp (C# Minimal API)
## What this is
- Minimal ASP.NET Core web app that mimics your Flask app behavior.
- Renders search results into simple HTML templates.
- Uses `appsettings.json` for the connection string (provided by you).

## Run
1. Install .NET SDK (recommended .NET 8). 
2. Open this folder in VS Code.
3. Run `dotnet restore` to fetch packages.
4. Run `dotnet run` to start the app.
5. Open `http://localhost:5000` in your browser.

## Notes
- The project references `Microsoft.Data.SqlClient`. If your environment blocks package restore, run `dotnet add package Microsoft.Data.SqlClient` manually.
- If your SQL Server requires domain credentials, make sure your process has access to it (Windows integrated auth).