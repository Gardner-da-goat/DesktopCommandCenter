# DesktopCommandCenter

A customizable Windows desktop command center for launching apps, controlling windows, running macros, searching desktop content, and accessing quick system actions.

## Current status

Active early development. The sidebar shell is functional and the project now includes working window management, search, automation, customization, updates, and system controls.

## Current features

- Collapsible desktop sidebar with smooth animation
- Left-edge or right-edge placement
- Adjustable width with presets and larger step controls
- Optional always-on-top behavior
- Automatically creates a desktop shortcut
- Optional launch with Windows
- Tray menu with Open, Collapse, Settings, and Exit

### Window management

- Enumerates normal application windows
- Current-window and selected-window controls
- Focus, minimize, maximize, restore, and close
- Per-window opacity
- Per-window always-on-top
- Snap left and right
- Center windows
- Move windows between monitors
- Optional automatic desktop reflow while the sidebar is open
- Reflows all visible primary-monitor windows together
- Restores their previous geometry when the sidebar closes or exits
- DWM frame compensation to avoid visible gaps around resized windows

### Search and commands

- Search installed apps
- Search open windows
- Search custom commands
- Search macros
- Search settings pages and jump directly to them
- Bounded background index for Desktop, Documents, and Downloads
- Search files and folders from that index
- Web search only with the explicit `search QUERY` prefix
- Press Enter on `search QUERY` to open a Google search in Chrome (falls back to the default browser if Chrome is unavailable)
- Direct commands such as:
  - `open spotify`
  - `focus chrome`
  - `close discord`
  - `opacity 70`
  - `top on`
  - `snap left`
  - `center`
  - `next monitor`
  - `macro Work Mode`
  - `search best keyboard shortcuts`

### Home modules

- Search
- Favorites
- Current Window
- Quick Actions
- Macros
- Optional Media controls
- Optional Recent activity

Home modules can be shown or hidden from Settings.

### Quick actions and system controls

- Downloads
- Task Manager
- Windows Settings
- Terminal
- Screenshot / screen capture
- Mute
- Volume up and down
- Clipboard History
- Media play / pause
- Previous / next track
- Show Desktop
- Lock Computer

Quick Actions can be individually shown or hidden.

### Favorites, commands, and macros

- Pin discovered apps as favorites
- Persist favorites across restarts
- Create custom searchable commands for apps, files, folders, and URLs
- Create and save macros
- Run macros from Home or Search
- Safe constrained macro commands instead of arbitrary shell execution
- Recent activity module for frequently used actions

### Hotkeys

- System-wide sidebar toggle
- System-wide Focus Search
- Enable or disable global shortcuts
- Create your own global shortcut combinations from Settings
- Editable shortcuts for sidebar toggle, Focus Search, snap left/right, always-on-top, and opacity up/down
- Supports Ctrl/Alt/Shift/Win combinations with letters, numbers, arrows, navigation keys, Space, and F1-F12

### Appearance

- Dark theme
- Light theme
- Follow Windows system theme
- Blue, purple, green, and orange accent presets
- Compact sidebar layout

### Updates

- GitHub Releases update channel
- Automatic update checks
- Manual Check for Updates
- Update download progress
- Update & Restart
- Self-contained Windows x64 release builds
- GitHub Actions build, test, package, and release workflows

## Data

User preferences, favorites, commands, and macros are stored separately from the application binaries under:

```text
%LOCALAPPDATA%\DesktopCommandCenter
```

## Development

Requires the .NET 8 SDK and Windows.

```powershell
dotnet restore DesktopCommandCenter.sln
dotnet build DesktopCommandCenter.sln --configuration Release --no-restore
dotnet test DesktopCommandCenter.sln --configuration Release --no-build
```

## Still planned

The project is still evolving. Areas that can be expanded further include richer clipboard tools, more window layouts, persistent recent-history data, additional hotkey bindings, notification controls, updater rollback/channel options, and deeper desktop search.
