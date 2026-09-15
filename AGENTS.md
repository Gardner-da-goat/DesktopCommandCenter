# Agent guidance

- Keep `DesktopCommandCenter.Core` independent of WPF and Windows APIs.
- Put monitor, DPI, Win32, window-management, and hotkey integrations in `DesktopCommandCenter.Windows`.
- Keep view code-behind limited to view-only behavior; navigation and application state belong in view models/services.
- Reuse theme resources and control styles instead of hard-coding colors or control chrome.
- Do not add Phase 2 functionality until it has an approved scope.
- Run the full Release build and test suite before committing.