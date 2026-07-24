# Asset Bookmarks

Asset Bookmarks is a simple, lightweight bookmark window for Unity—keep project assets, files and folders outside your Unity project, and websites just one click away.

https://github.com/user-attachments/assets/2be31124-1b9c-4079-a260-695ae99c118b

https://github.com/user-attachments/assets/3b518024-68bf-4d39-8dfc-016a8a57f9b6

https://github.com/user-attachments/assets/95a5ec9f-5301-46b2-a830-6b3bca41a671

Version 2 is a complete UI Toolkit rewrite with a clearer interface, automatic saving, resilient project-asset references, and migration from version 1.

## Features

- Bookmark Unity assets, GameObjects in saved Scenes, external files and folders, and websites such as Jenkins jobs.
- Add bookmarks by drag and drop or from the compact **+** menu.
- Search instantly by name or path.
- Choose what happens when a Unity asset is clicked.
- Select bookmarked GameObjects in the Hierarchy while their Scene is open.
- Drag bookmarked assets back into compatible Unity windows, including Prefabs into the Scene or Hierarchy.
- Keep Unity asset bookmarks working after files are moved or renamed.
- Color-code and reorder rows, and choose from three compact display sizes.
- Save changes automatically and locally for each project.

## Requirements

- Unity 6.3 (6000.3) or newer.

## Installation

1. Open **Window > Package Management > Package Manager**.
2. Select **Install package from git URL** from the add menu.
3. Enter:

   ```text
   https://github.com/kyubuns/AssetBookmarks.git?path=Assets/AssetBookmarks
   ```

## Usage

Open **Window > Asset Bookmarks**.

Add bookmarks in either of these ways:

- Drag assets from the Project window, GameObjects from the Hierarchy, or files and folders from Finder or Explorer anywhere onto the window.
- Use the **+** menu to add selected Unity assets or GameObjects, external files or folders, or a website.

Website bookmarks support HTTP and HTTPS. If the scheme is omitted, `https://` is added automatically.

Scene GameObjects appear as `GameObject (Scene)`. They can be selected and pinged in the Hierarchy only while their saved Scene is open; otherwise the row remains visible but disabled.

Click a row to use the bookmark. Type in the search field to filter the list. Right-click a row to assign a color, change its action, copy its path, move it, or remove it.

Drag the row body into another compatible Unity window to reuse the bookmarked item. Prefabs can be placed in the Scene or Hierarchy just like assets from the Project window. Open-Scene GameObjects, external files, and external folders can also be dragged; websites cannot.

Drag the right-edge grip to reorder rows. Use **Aa** to choose Small, Medium, or Large.

New Scene bookmarks use **Open in Unity** by default. Other Unity assets use **Select in Project**.

### Unity asset actions

| Action | Result |
| --- | --- |
| Select in Project | Selects and pings the asset in the Project window. |
| Open in Unity | Opens the asset with its Unity editor; folders fall back to selection. |
| Reveal in Finder | Reveals the asset in Finder, Explorer, or the platform file manager. |
| Open in Default App | Opens the asset with the operating system's associated application. |

External files and folders always open with their default application. Website bookmarks open in the default browser.

## Storage

Bookmarks are saved in `UserSettings/AssetBookmarks.json`, which is excluded by the standard Unity `.gitignore`. They remain local to each project and computer.

Unity asset bookmarks follow moved or renamed assets, and Scene GameObject bookmarks follow renamed objects. Existing bookmarks stored by version 1 or earlier version 2 releases are imported automatically.

## License

[MIT](LICENSE)
