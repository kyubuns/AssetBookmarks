# Asset Bookmarks

Asset Bookmarks is a focused Unity Editor window for keeping project assets and files outside Unity one click away.

https://github.com/user-attachments/assets/102e4924-064e-4193-9c98-c9422674f284

Version 2 is a complete UI Toolkit rewrite with a clearer interface, automatic saving, resilient project-asset references, and migration from version 1.

## Features

- Bookmark assets and folders from `Assets` or `Packages`.
- Bookmark files and folders anywhere on your computer.
- Drag one or more Unity assets, external files, or folders anywhere onto the window.
- Choose what a Unity asset does when clicked: select, open, reveal, or launch in its default application.
- Drag rows to reorder them; use the context menu to change actions, move, or remove a bookmark.
- Switch the text, icon, and row height together with Small, Medium, and Large display presets.
- Keep bookmarks valid when Unity assets move or are renamed.
- See missing targets without losing the bookmark unexpectedly.
- Store bookmarks locally per project; no project assets are modified.

## Requirements

- Unity 6000.3.11f1 or newer in the Unity 6000.3 release line.

## Installation

1. Open **Window > Package Management > Package Manager**.
2. Select **Install package from git URL** from the add menu.
3. Enter:

   ```text
   https://github.com/kyubuns/AssetBookmarks.git?path=Assets/AssetBookmarks
   ```

To pin a release, append a Git tag such as `#2.0.0` to the URL.

## Usage

Open **Window > Asset Bookmarks**.

Add bookmarks by dragging them anywhere onto the window:

- Drag assets from the Project window.
- Drag files or folders from Finder or Explorer.

Click a bookmark row to use it. Drag rows to reorder them. Right-click a row to change its Unity asset action, move it, or remove it. Changes are saved immediately.

Use **Aa > Row Size** to choose Small, Medium, or Large. Small is the default and maximizes the number of visible bookmarks. The preference is saved locally.

### Unity asset actions

| Action | Result |
| --- | --- |
| Select in Project | Selects and pings the asset in the Project window. |
| Open in Unity | Opens the asset with its Unity editor; folders fall back to selection. |
| Reveal in Finder | Reveals the asset in Finder, Explorer, or the platform file manager. |
| Open in Default App | Opens the asset with the operating system's associated application. |

External bookmarks always open with their default application.

## Data and migration

Bookmarks are stored in `EditorPrefs`, scoped by the project's absolute path. They are local to the current user and are not committed with the Unity project. External bookmarks therefore remain machine-specific.

The first time version 2 opens, it imports bookmarks from the version 1 key for the current project. The old value is left untouched so downgrading does not destroy existing data.

## Development

This repository's sample project targets Unity 6000.3.11f1. Player builds are not required; the package is Editor-only.

## License

[MIT](LICENSE)
