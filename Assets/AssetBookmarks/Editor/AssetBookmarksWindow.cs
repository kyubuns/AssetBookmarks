using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal sealed class AssetBookmarksWindow : EditorWindow
    {
        private const string DisplaySizeKey = "AssetBookmarks.v2.display-size";
        private const long StatusDurationMs = 2000;

        private readonly List<Bookmark> visibleItems = new List<Bookmark>();

        private BookmarkStore store;
        private ListView listView;
        private VisualElement emptyState;
        private Label emptyLabel;
        private VisualElement dropOverlay;
        private Label statusLabel;
        private IVisualElementScheduledItem statusDismissal;
        private DisplaySize displaySize;
        private string searchQuery = string.Empty;
        private bool reorderHandlePressed;

        [MenuItem("Window/Asset Bookmarks")]
        private static void ShowWindow()
        {
            var window = GetWindow<AssetBookmarksWindow>();
            window.titleContent = new GUIContent("Asset Bookmarks");
            window.minSize = new Vector2(220f, 120f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Asset Bookmarks");
            minSize = new Vector2(220f, 120f);
            displaySize = LoadDisplaySize();
            store = BookmarkStore.Load();
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            statusDismissal?.Pause();
        }

        public void CreateGUI()
        {
            if (store == null)
            {
                store = BookmarkStore.Load();
            }

            statusDismissal?.Pause();
            statusDismissal = null;
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("asset-bookmarks");
            AddDisplaySizeClass();

            var styleSheet = LoadStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            RegisterDropTarget();
            rootVisualElement.Add(CreateToolbar());
            rootVisualElement.Add(CreateContent());
            rootVisualElement.Add(CreateDropOverlay());
            rootVisualElement.Add(CreateStatusLabel());
            RefreshView();

            if (store.MigratedEditorPrefsData)
            {
                rootVisualElement.schedule.Execute(() =>
                    ShowStatus("Existing bookmarks were migrated to UserSettings."));
            }
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("asset-bookmarks__toolbar");

            var searchField = new ToolbarSearchField();
            searchField.tooltip = "Filter by name or path";
            searchField.AddToClassList("asset-bookmarks__search");
            searchField.RegisterValueChangedCallback(evt =>
            {
                searchQuery = evt.newValue ?? string.Empty;
                RefreshView();
            });
            toolbar.Add(searchField);

            var addMenu = new ToolbarMenu { text = "+" };
            addMenu.tooltip = "Add bookmark";
            addMenu.AddToClassList("asset-bookmarks__add-button");
            addMenu.menu.AppendAction(
                "Selected Assets / GameObjects",
                _ => AddSelectedItems(),
                _ => HasSelectedItems()
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            addMenu.menu.AppendSeparator();
            addMenu.menu.AppendAction("External File…", _ => AddExternalFile());
            addMenu.menu.AppendAction("External Folder…", _ => AddExternalFolder());
            addMenu.menu.AppendSeparator();
            addMenu.menu.AppendAction("Website…", _ => AddWebsiteWindow.Show(this));
            toolbar.Add(addMenu);

            var optionsMenu = new ToolbarMenu { text = "Aa" };
            optionsMenu.tooltip = "Display size";
            optionsMenu.AddToClassList("asset-bookmarks__options-button");
            AppendDisplaySizeOption(optionsMenu, DisplaySize.Small);
            AppendDisplaySizeOption(optionsMenu, DisplaySize.Medium);
            AppendDisplaySizeOption(optionsMenu, DisplaySize.Large);
            toolbar.Add(optionsMenu);

            return toolbar;
        }

        private void AppendDisplaySizeOption(ToolbarMenu menu, DisplaySize size)
        {
            menu.menu.AppendAction(
                $"Row Size/{size}",
                _ => SetDisplaySize(size),
                _ => displaySize == size
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        private VisualElement CreateContent()
        {
            var content = new VisualElement();
            content.AddToClassList("asset-bookmarks__content");

            listView = new ListView
            {
                itemsSource = visibleItems,
                fixedItemHeight = GetRowHeight(displaySize),
                selectionType = SelectionType.None,
                reorderable = visibleItems.Count > 1,
                reorderMode = ListViewReorderMode.Animated,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                showBorder = false,
                makeItem = () => new BookmarkRow(this),
                bindItem = (element, index) =>
                    ((BookmarkRow)element).Bind(visibleItems[index]),
            };
            listView.AddToClassList("asset-bookmarks__list");
            listView.canStartDrag += _ => reorderHandlePressed;
            listView.RegisterCallback<PointerDownEvent>(OnListPointerDown, TrickleDown.TrickleDown);
            listView.RegisterCallback<PointerUpEvent>(_ => reorderHandlePressed = false, TrickleDown.TrickleDown);
            listView.RegisterCallback<PointerCaptureOutEvent>(_ => reorderHandlePressed = false);
            listView.itemIndexChanged += (_, _) =>
            {
                store.ApplyVisibleOrder(visibleItems);
                RefreshView();
            };
            content.Add(listView);

            emptyState = new VisualElement();
            emptyState.AddToClassList("asset-bookmarks__empty");

            emptyLabel = new Label("Drop assets, GameObjects, files, or folders here");
            emptyLabel.AddToClassList("asset-bookmarks__empty-label");
            emptyState.Add(emptyLabel);
            content.Add(emptyState);

            return content;
        }

        private void OnListPointerDown(PointerDownEvent evt)
        {
            reorderHandlePressed = evt.button == 0 && IsReorderHandle(evt.target as VisualElement);
        }

        private bool IsReorderHandle(VisualElement element)
        {
            while (element != null && element != listView)
            {
                if (element.ClassListContains("unity-list-view__reorderable-handle"))
                {
                    return true;
                }

                element = element.parent;
            }

            return false;
        }

        private VisualElement CreateDropOverlay()
        {
            dropOverlay = new VisualElement { pickingMode = PickingMode.Ignore };
            dropOverlay.AddToClassList("asset-bookmarks__drop-overlay");

            var label = new Label("Drop to bookmark");
            label.AddToClassList("asset-bookmarks__drop-overlay-label");
            dropOverlay.Add(label);
            return dropOverlay;
        }

        private Label CreateStatusLabel()
        {
            statusLabel = new Label { pickingMode = PickingMode.Ignore };
            statusLabel.AddToClassList("asset-bookmarks__status");
            statusLabel.style.display = DisplayStyle.None;
            return statusLabel;
        }

        private void ShowStatus(string message)
        {
            statusDismissal?.Pause();
            statusLabel.text = message;
            statusLabel.style.display = DisplayStyle.Flex;
            statusLabel.BringToFront();
            statusDismissal = statusLabel.schedule.Execute(HideStatus).StartingIn(StatusDurationMs);
        }

        private void HideStatus()
        {
            statusLabel.style.display = DisplayStyle.None;
            statusDismissal = null;
        }

        private void RegisterDropTarget()
        {
            rootVisualElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!HasDraggedItems())
                {
                    return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                SetDropOverlayVisible(true);
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            rootVisualElement.RegisterCallback<DragLeaveEvent>(_ => SetDropOverlayVisible(false));
            rootVisualElement.RegisterCallback<DragExitedEvent>(_ => SetDropOverlayVisible(false));

            rootVisualElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (!HasDraggedItems())
                {
                    return;
                }

                DragAndDrop.AcceptDrag();
                SetDropOverlayVisible(false);
                AddItems(DragAndDrop.paths, GetDraggedSceneObjects());
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        private static bool HasDraggedItems()
        {
            if (BookmarkDragAndDrop.IsBookmarkDrag)
            {
                return false;
            }

            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                return true;
            }

            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                if (objectReference is GameObject gameObject && Bookmark.CanCreateSceneObject(gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<GameObject> GetDraggedSceneObjects()
        {
            var sceneObjects = new List<GameObject>();
            foreach (var objectReference in DragAndDrop.objectReferences)
            {
                if (objectReference is GameObject gameObject && Bookmark.CanCreateSceneObject(gameObject))
                {
                    sceneObjects.Add(gameObject);
                }
            }

            return sceneObjects;
        }

        private void SetDropOverlayVisible(bool visible)
        {
            if (dropOverlay != null)
            {
                dropOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private StyleSheet LoadStyleSheet()
        {
            var script = MonoScript.FromScriptableObject(this);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath))
            {
                return null;
            }

            var editorDirectory = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            return string.IsNullOrEmpty(editorDirectory)
                ? null
                : AssetDatabase.LoadAssetAtPath<StyleSheet>($"{editorDirectory}/UI/AssetBookmarks.uss");
        }

        private void AddSelectedItems()
        {
            var paths = new List<string>();
            var sceneObjects = new List<GameObject>();
            foreach (var selectedObject in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
                else if (selectedObject is GameObject gameObject && Bookmark.CanCreateSceneObject(gameObject))
                {
                    sceneObjects.Add(gameObject);
                }
            }

            AddItems(paths, sceneObjects);
        }

        private static bool HasSelectedItems()
        {
            foreach (var selectedObject in Selection.objects)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selectedObject)) ||
                    selectedObject is GameObject gameObject && Bookmark.CanCreateSceneObject(gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddExternalFile()
        {
            var path = EditorUtility.OpenFilePanel("Add External File", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                AddPaths(new[] { path });
            }
        }

        private void AddExternalFolder()
        {
            var path = EditorUtility.OpenFolderPanel("Add External Folder", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                AddPaths(new[] { path });
            }
        }

        private void AddPaths(IEnumerable<string> paths)
        {
            AddItems(paths, Array.Empty<GameObject>());
        }

        private void AddItems(IEnumerable<string> paths, IEnumerable<GameObject> sceneObjects)
        {
            var result = store.AddItems(paths, sceneObjects);
            RefreshView();

            if (result.Added > 0)
            {
                var suffix = result.Added == 1 ? string.Empty : "s";
                ShowStatus($"Added {result.Added} bookmark{suffix}.");
            }
            else if (result.Duplicate > 0 && result.Invalid == 0)
            {
                ShowStatus("Already bookmarked.");
            }
            else
            {
                ShowStatus("No valid items found.");
            }
        }

        internal BookmarkAddResult AddWebsite(string url)
        {
            var result = store.AddUrl(url);
            if (result.Added > 0)
            {
                RefreshView();
                ShowStatus("Website bookmarked.");
            }

            return result;
        }

        private void RemoveBookmark(Bookmark bookmark)
        {
            store.Remove(bookmark);
            RefreshView();
        }

        private void SetOpenMode(Bookmark bookmark, BookmarkOpenMode mode)
        {
            bookmark.SetOpenMode(mode);
            store.Save();
            RefreshView();
        }

        private void SetBookmarkColor(Bookmark bookmark, BookmarkColor color)
        {
            if (bookmark.Color == color)
            {
                return;
            }

            bookmark.SetColor(color);
            store.Save();
            listView.RefreshItems();
        }

        private void MoveBookmark(Bookmark bookmark, int offset)
        {
            var currentIndex = visibleItems.IndexOf(bookmark);
            var nextIndex = currentIndex + offset;
            if (currentIndex < 0 || nextIndex < 0 || nextIndex >= visibleItems.Count)
            {
                return;
            }

            visibleItems.RemoveAt(currentIndex);
            visibleItems.Insert(nextIndex, bookmark);
            store.ApplyVisibleOrder(visibleItems);
            RefreshView();
        }

        private void SetDisplaySize(DisplaySize size)
        {
            if (displaySize == size)
            {
                return;
            }

            displaySize = size;
            EditorPrefs.SetInt(DisplaySizeKey, (int)displaySize);
            rootVisualElement.RemoveFromClassList("asset-bookmarks--size-small");
            rootVisualElement.RemoveFromClassList("asset-bookmarks--size-medium");
            rootVisualElement.RemoveFromClassList("asset-bookmarks--size-large");
            AddDisplaySizeClass();
            listView.fixedItemHeight = GetRowHeight(displaySize);
            listView.Rebuild();
        }

        private void AddDisplaySizeClass()
        {
            rootVisualElement.AddToClassList($"asset-bookmarks--size-{displaySize.ToString().ToLowerInvariant()}");
        }

        private static DisplaySize LoadDisplaySize()
        {
            var savedValue = EditorPrefs.GetInt(DisplaySizeKey, (int)DisplaySize.Small);
            return Enum.IsDefined(typeof(DisplaySize), savedValue)
                ? (DisplaySize)savedValue
                : DisplaySize.Small;
        }

        private static float GetRowHeight(DisplaySize size)
        {
            switch (size)
            {
                case DisplaySize.Medium:
                    return 28f;
                case DisplaySize.Large:
                    return 36f;
                default:
                    return 22f;
            }
        }

        private void OpenBookmark(Bookmark bookmark)
        {
            if (!BookmarkActions.Open(bookmark))
            {
                ShowStatus("The bookmarked item no longer exists.");
            }
        }

        private void OnProjectChanged()
        {
            store?.RefreshProjectPaths();
            RefreshView();
        }

        private void OnHierarchyChanged()
        {
            store?.RefreshSceneObjectNames();
            RefreshView();
        }

        private void RefreshView()
        {
            if (listView == null)
            {
                return;
            }

            RebuildVisibleItems();

            var visibleCount = visibleItems.Count;
            var isFiltering = !string.IsNullOrWhiteSpace(searchQuery);
            emptyLabel.text = isFiltering
                ? "No matching bookmarks"
                : "Drop assets, GameObjects, files, or folders here";
            listView.reorderable = visibleCount > 1;
            listView.style.display = visibleCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyState.style.display = visibleCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            listView.RefreshItems();
        }

        private void RebuildVisibleItems()
        {
            visibleItems.Clear();
            var query = searchQuery?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                visibleItems.AddRange(store.Items);
                return;
            }

            foreach (var bookmark in store.Items)
            {
                var resolvedPath = bookmark.ResolvedPath;
                var displayName = bookmark.GetDisplayName(resolvedPath);
                if (displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    resolvedPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    visibleItems.Add(bookmark);
                }
            }
        }

        private sealed class BookmarkRow : VisualElement
        {
            private static readonly BookmarkOpenMode[] OpenModes =
            {
                BookmarkOpenMode.Select,
                BookmarkOpenMode.Open,
                BookmarkOpenMode.Reveal,
                BookmarkOpenMode.DefaultApplication,
            };

            private static readonly (BookmarkColor Value, string MenuLabel, Color Tint)[] ColorOptions =
            {
                (BookmarkColor.None, "No Color", Color.clear),
                (BookmarkColor.Red, "🔴 Red", new Color32(255, 69, 72, 42)),
                (BookmarkColor.Orange, "🟠 Orange", new Color32(255, 159, 10, 42)),
                (BookmarkColor.Yellow, "🟡 Yellow", new Color32(255, 214, 10, 42)),
                (BookmarkColor.Green, "🟢 Green", new Color32(48, 209, 88, 42)),
                (BookmarkColor.Blue, "🔵 Blue", new Color32(10, 132, 255, 42)),
                (BookmarkColor.Purple, "🟣 Purple", new Color32(191, 90, 242, 42)),
                (BookmarkColor.Gray, "⚪ Gray", new Color32(142, 142, 147, 42)),
            };

            private readonly AssetBookmarksWindow window;
            private readonly VisualElement colorTint;
            private readonly Image icon;
            private readonly Label nameLabel;
            private readonly Label actionLabel;
            private readonly Label missingLabel;

            private Bookmark bookmark;
            private bool isAvailable;

            internal BookmarkRow(AssetBookmarksWindow window)
            {
                this.window = window;
                AddToClassList("asset-bookmarks__row");

                colorTint = new VisualElement { pickingMode = PickingMode.Ignore };
                colorTint.AddToClassList("asset-bookmarks__row-color");
                Add(colorTint);

                icon = new Image { scaleMode = ScaleMode.ScaleToFit };
                icon.AddToClassList("asset-bookmarks__row-icon");
                Add(icon);

                nameLabel = new Label();
                nameLabel.AddToClassList("asset-bookmarks__row-name");
                Add(nameLabel);

                missingLabel = new Label("!");
                missingLabel.tooltip = "The bookmarked item is missing.";
                missingLabel.AddToClassList("asset-bookmarks__missing");
                Add(missingLabel);

                actionLabel = new Label();
                actionLabel.AddToClassList("asset-bookmarks__row-action");
                Add(actionLabel);

                this.AddManipulator(new BookmarkDragManipulator(() => bookmark));
                this.AddManipulator(new Clickable(() =>
                {
                    if (isAvailable)
                    {
                        window.OpenBookmark(bookmark);
                    }
                }));
                this.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));
            }

            internal void Bind(Bookmark item)
            {
                bookmark = item;
                isAvailable = item.TryResolveTarget(out var resolvedPath);
                var inactiveSceneObject = item.Kind == BookmarkKind.SceneObject && !isAvailable;

                tooltip = inactiveSceneObject
                    ? "Open this GameObject's scene to select it."
                    : resolvedPath;
                nameLabel.text = item.GetDisplayName(resolvedPath);
                nameLabel.tooltip = tooltip;
                actionLabel.text = BookmarkActions.GetActionLabel(item);
                icon.image = GetIcon(item, resolvedPath, isAvailable);
                colorTint.style.backgroundColor = GetColorTint(item.Color);
                missingLabel.style.display = !isAvailable && !inactiveSceneObject
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                EnableInClassList("asset-bookmarks__row--unavailable", !isAvailable);
            }

            private void PopulateContextMenu(ContextualMenuPopulateEvent evt)
            {
                if (bookmark == null)
                {
                    return;
                }

                evt.menu.AppendAction(
                    BookmarkActions.GetActionLabel(bookmark),
                    _ => window.OpenBookmark(bookmark),
                    _ => bookmark.IsAvailable
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Copy Path", _ => GUIUtility.systemCopyBuffer = bookmark.ResolvedPath);
                evt.menu.AppendSeparator();
                AppendColorMenu(evt.menu);

                if (bookmark.Kind == BookmarkKind.ProjectAsset)
                {
                    foreach (var mode in OpenModes)
                    {
                        var capturedMode = mode;
                        evt.menu.AppendAction(
                            $"Action/{BookmarkActions.GetModeLabel(capturedMode)}",
                            _ => window.SetOpenMode(bookmark, capturedMode),
                            _ => bookmark.OpenMode == capturedMode
                                ? DropdownMenuAction.Status.Checked
                                : DropdownMenuAction.Status.Normal);
                    }
                }

                evt.menu.AppendSeparator();
                evt.menu.AppendAction(
                    "Move Up",
                    _ => window.MoveBookmark(bookmark, -1),
                    _ => window.visibleItems.IndexOf(bookmark) > 0
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction(
                    "Move Down",
                    _ => window.MoveBookmark(bookmark, 1),
                    _ => window.visibleItems.IndexOf(bookmark) < window.visibleItems.Count - 1
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Remove Bookmark", _ => window.RemoveBookmark(bookmark));
            }

            private void AppendColorMenu(DropdownMenu menu)
            {
                foreach (var option in ColorOptions)
                {
                    var capturedOption = option;
                    menu.AppendAction(
                        $"Color/{capturedOption.MenuLabel}",
                        _ => window.SetBookmarkColor(bookmark, capturedOption.Value),
                        _ => bookmark.Color == capturedOption.Value
                            ? DropdownMenuAction.Status.Checked
                            : DropdownMenuAction.Status.Normal);
                }
            }

            private static Color GetColorTint(BookmarkColor color)
            {
                foreach (var option in ColorOptions)
                {
                    if (option.Value == color)
                    {
                        return option.Tint;
                    }
                }

                return Color.clear;
            }

            private static Texture GetIcon(Bookmark bookmark, string resolvedPath, bool available)
            {
                if (bookmark.Kind == BookmarkKind.SceneObject)
                {
                    return EditorGUIUtility.IconContent("GameObject Icon").image;
                }

                if (!available)
                {
                    return EditorGUIUtility.IconContent("console.warnicon.sml").image;
                }

                if (bookmark.Kind == BookmarkKind.ProjectAsset)
                {
                    return AssetDatabase.GetCachedIcon(resolvedPath);
                }

                if (bookmark.Kind == BookmarkKind.Url)
                {
                    var webIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
                    return webIcon != null
                        ? webIcon
                        : EditorGUIUtility.IconContent("DefaultAsset Icon").image;
                }

                var iconName = Directory.Exists(resolvedPath) ? "Folder Icon" : "DefaultAsset Icon";
                return EditorGUIUtility.IconContent(iconName).image;
            }
        }

        private enum DisplaySize
        {
            Small,
            Medium,
            Large,
        }
    }
}
