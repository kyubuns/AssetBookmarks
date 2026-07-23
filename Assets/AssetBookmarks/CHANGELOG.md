# Changelog

All notable changes to Asset Bookmarks are documented in this file.

## [2.0.0] - 2026-07-23

### Added

- UI Toolkit interface with light and dark Editor themes.
- Drag-and-drop targets for Unity assets, external files, and folders.
- Compact **+** menu for adding selected Unity assets, external files, external folders, and websites.
- HTTP and HTTPS website bookmarks with a focused URL-entry popup and default-browser opening.
- Stable GUID tracking for Unity assets that move or are renamed.
- Missing-target states, duplicate detection, and context-menu removal.
- Compact single-line rows with direct click actions and context-menu editing.
- **Copy Path** in each row's context menu.
- Persistent Small, Medium, and Large display-size presets for text, icons, and row height.
- Instant name-and-path filtering from the compact toolbar search field.
- Dedicated right-edge-only drag grips with animated reorder feedback.
- **Open in Unity** as the default action for newly added Scene assets.
- Automatic migration from the version 1 `EditorPrefs` format.

### Changed

- Replaced the IMGUI and `ReorderableList` implementation with a compact retained-mode UI.
- Replaced the delimiter-based save format with versioned JSON scoped to the project path.
- Save edits, additions, removals, and reorder operations immediately.
- Clarified Unity asset actions and exposed the full stored path in each row.
- Reused Unity's native Editor background and USS theme tokens instead of fixed light and dark color constants.
- Reused virtualized list rows and avoided loading asset objects for display-only availability checks.

### Removed

- Explicit Run and Edit state classes and the manual Save workflow.
