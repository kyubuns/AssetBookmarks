# Changelog

All notable changes to Asset Bookmarks are documented in this file.

## [Unreleased]

### Added

- Scene GameObject bookmarks from Hierarchy drag and drop or the **+** menu, with Hierarchy selection while the saved Scene is open.
- Disabled styling for Scene GameObject bookmarks while their Scene is closed.

## [2.3.0] - 2026-07-24

### Changed

- Store bookmarks in an atomically replaced `UserSettings` file, with automatic migration from version 1 and version 2 `EditorPrefs` data.

## [2.2.0] - 2026-07-23

### Added

- Per-bookmark row colors with seven presets and a **No Color** option in the context menu.

### Changed

- Set the minimum supported Editor version to Unity 6.3 without requiring a specific patch release.

### Fixed

- Kept status messages readable when the window is docked at a very small height.

## [2.1.0] - 2026-07-23

### Added

- Standard outgoing Unity drag-and-drop from bookmark rows for project assets, external files, and external folders.
- Project-asset drag payloads that let compatible Editor targets handle Prefabs and other asset types exactly as they handle Project-window drags.

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
