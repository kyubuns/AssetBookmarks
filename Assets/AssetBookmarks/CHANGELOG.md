# Changelog

All notable changes to Asset Bookmarks are documented in this file.

## [2.0.0] - 2026-07-23

### Added

- UI Toolkit interface with light and dark Editor themes.
- Drag-and-drop targets for Unity assets, external files, and folders.
- Window-wide drag and drop for adding one or more bookmarks without persistent controls.
- Stable GUID tracking for Unity assets that move or are renamed.
- Missing-target states, duplicate detection, and context-menu removal.
- Compact single-line rows with direct click actions and context-menu editing.
- Persistent Small, Medium, and Large display-size presets for text, icons, and row height.
- Automatic migration from the version 1 `EditorPrefs` format.

### Changed

- Replaced the IMGUI and `ReorderableList` implementation with a compact retained-mode UI.
- Replaced the delimiter-based save format with versioned JSON scoped to the project path.
- Save edits, additions, removals, and reorder operations immediately.
- Clarified Unity asset actions and exposed the full stored path in each row.

### Removed

- Explicit Run and Edit state classes and the manual Save workflow.
