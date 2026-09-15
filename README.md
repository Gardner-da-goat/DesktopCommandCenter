# DesktopCommandCenter

A customizable right-side Windows command center for launching apps, controlling windows, running macros, and accessing quick desktop actions.

## Current status

Early development — Phase 1 sidebar and settings foundation.

## Current features

- Collapsible right-edge sidebar
- Home shell with modular cards
- Settings shell with functional General preferences
- Windows tray support
- Persisted, versioned preferences

## Planned

- App launcher
- Window manager
- Opacity controls
- Global hotkeys
- Macros
- Updates

## Development

Requires the .NET 8 SDK and Windows.

```powershell
dotnet restore DesktopCommandCenter.sln
dotnet build DesktopCommandCenter.sln --configuration Release --no-restore
dotnet test DesktopCommandCenter.sln --configuration Release --no-build
```
