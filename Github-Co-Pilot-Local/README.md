# Github-Co-Pilot-Local

A .NET 10 console app that connects to GitHub Copilot and exposes custom local tools for:

- API version change lookup from Swagger JSON files
- package inventory lookup
- license lookup from `node_modules`
- CAE API invocation helpers

## Prerequisites

- .NET 10 SDK
- Access to a GitHub Copilot CLI session
- `GITHUB_TOKEN` set in the current user environment variables
- Access to the local files/directories referenced in `appsettings.json`

## Configuration

Update `Github-Co-Pilot-Local\appsettings.json` before running.

### Required settings

- `cliServerMode`
  - `server` to use a remote Copilot CLI server
  - `local` to use the local Copilot CLI executable
- `cliServerUrl`
  - required when `cliServerMode` is `server`
- `LocalBlobStoragePath`
  - directory containing Swagger files named like `swagger-1.0.0.json`
- `AppPackageDirectoryPath`
  - directory containing the package metadata file used by the package tool
- `NodeModulesDirectoryPath`
  - root directory of the `node_modules` tree used by the license tool

### Example `appsettings.json`

```json
{
  "McpServers": {
    "remote-change-log-mcp": {
      "Type": "http",
      "Url": "http://localhost:5994/mcp",
      "Tools": [ "get_version_changes_content" ]
    }
  },
  "cliServerMode": "local",
  "cliServerUrl": "20.127.34.138:4321",
  "LocalBlobStoragePath": "C:\\CAE_Github\\Se.Cae.Web\\src\\docs\\api",
  "AppPackageDirectoryPath": "C:\\CAE_Github\\Se.Cae.Web\\src\\frontend\\cae",
  "NodeModulesDirectoryPath": "C:\\CAE_Github\\Se.Cae.Web\\src\\frontend\\cae\\node_modules"
}
```

## Running

### Local Copilot CLI mode

1. Make sure Copilot CLI is installed at:
   - `%APPDATA%\vendor\copilot\copilot.exe`
2. Set `cliServerMode` to `local`
3. Run:

```powershell
dotnet run --project .\Github-Co-Pilot-Local\Github-Co-Pilot-Local.csproj
```

### Remote Copilot CLI server mode

1. Set `cliServerMode` to `server`
2. Set `cliServerUrl` to the remote CLI endpoint
3. Run:

```powershell
dotnet run --project .\Github-Co-Pilot-Local\Github-Co-Pilot-Local.csproj
```

## What the app does

When the app starts it:

- loads `appsettings.json`
- initializes Serilog JSON logging
- creates the Copilot client
- loads custom tools
- starts an interactive console loop

## Logging

Logs are written in JSON format to:

- console
- `Github-Co-Pilot-Local\bin\...\logs\github-copilot-local-.json`

## Tool summary

- `GetVersionChangesAsContent`
  - reads Swagger JSON for a requested version
- `GetPackageInformation`
  - reads package metadata for installed packages
- `GetLicenseFromNodeModules`
  - searches `node_modules` for a package and returns license file content
- `CallCAEApi`
  - invokes CAE-related API workflows

## Notes

- If `cliServerMode` is `local`, the app expects the Copilot CLI executable to already exist on the machine.
- If a configured path does not exist, the app will fail fast with a clear error message.
- The console prompt accepts `exit` to quit.
