# Asset Bookmarks

Asset Bookmarks is a simple, lightweight bookmark window for Unity—keep project assets, files and folders outside your Unity project, and websites just one click away.

Version 2 is a complete UI Toolkit rewrite with a clearer interface, automatic saving, resilient project-asset references, and migration from version 1.

## Features

- Bookmark assets and folders from `Assets` or `Packages`.
- Bookmark files and folders anywhere on your computer.
- Bookmark HTTP or HTTPS websites such as Jenkins jobs and open them in your default browser.
- Drag one or more Unity assets, external files, or folders anywhere onto the window.
- Use the compact **+** menu to add selected Unity assets, external files or folders, and websites.
- Choose what a Unity asset does when clicked: select, open, reveal, or launch in its default application.
- Filter bookmarks immediately by typing part of their name or path in the search field.
- Drag only the grip at the right edge of a row to reorder it.
- Use the context menu to change actions, copy the current path, move, or remove a bookmark.
- Switch the text, icon, and row height together with Small, Medium, and Large display presets.
- Open newly added Scene assets in Unity by default; other Unity assets default to selection.
- Keep bookmarks valid when Unity assets move or are renamed.
- See missing targets without losing the bookmark unexpectedly.
- Follow the active Unity Editor theme without maintaining separate fixed light and dark colors.
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

Add bookmarks in either of these ways:

- Drag assets from the Project window, or files and folders from Finder or Explorer, anywhere onto the window.
- Open the **+** menu to add the current Unity selection, choose an external file or folder, or enter a website URL in a compact popup.

Website bookmarks support HTTP and HTTPS. If the scheme is omitted, Asset Bookmarks adds `https://`. URL availability is validated by format only, so opening the window never performs a network request.

Click a bookmark row to use it. Type in the search field to filter immediately by name or path. Drag the grip at the right edge of a row to reorder it; dragging the rest of the row does not reorder. Right-click a row to change its Unity asset action, copy its current path, move it, or remove it. Changes are saved immediately.

When a Scene is added, its initial action is **Open in Unity**. Other Unity assets initially use **Select in Project**. Change either from the row's context menu.

Use **Aa > Row Size** to choose Small, Medium, or Large. Small is the default and maximizes the number of visible bookmarks. The preference is saved locally.

### Unity asset actions

| Action | Result |
| --- | --- |
| Select in Project | Selects and pings the asset in the Project window. |
| Open in Unity | Opens the asset with its Unity editor; folders fall back to selection. |
| Reveal in Finder | Reveals the asset in Finder, Explorer, or the platform file manager. |
| Open in Default App | Opens the asset with the operating system's associated application. |

External files and folders always open with their default application. Website bookmarks open in the default browser.

## Data and migration

Bookmarks are stored in `EditorPrefs`, scoped by the project's absolute path. They are local to the current user and are not committed with the Unity project. External bookmarks therefore remain machine-specific.

Unity project assets and folders are stored by GUID, while their latest project-relative path is kept for display and missing-item recovery. This allows a bookmark to follow the asset when it is moved or renamed. `GlobalObjectId` is intentionally unnecessary while bookmarks target whole assets rather than sub-objects.

The first time version 2 opens, it imports bookmarks from the version 1 key for the current project. The old value is left untouched so downgrading does not destroy existing data.

## License

[MIT](LICENSE.md)
