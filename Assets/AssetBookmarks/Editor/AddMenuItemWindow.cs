using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal sealed class AddMenuItemWindow : EditorWindow
    {
        private static readonly Vector2 InitialSize = new Vector2(480f, 360f);
        private static readonly Vector2 MinimumSize = new Vector2(360f, 240f);

        [SerializeField]
        private AssetBookmarksWindow owner;

        private readonly List<string> visibleMenuItems = new List<string>();
        private ListView listView;
        private Label emptyLabel;
        private Button addButton;

        internal static void Show(AssetBookmarksWindow owner)
        {
            BookmarkMenuItems.Refresh();
            var window = GetWindow<AddMenuItemWindow>(true, "Add Unity Menu Item", true);
            window.owner = owner;
            window.minSize = MinimumSize;

            var ownerPosition = owner.position;
            window.position = new Rect(
                ownerPosition.x + Mathf.Max(0f, (ownerPosition.width - InitialSize.x) * 0.5f),
                ownerPosition.y + 36f,
                InitialSize.x,
                InitialSize.y
            );
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            var searchField = new ToolbarSearchField();
            searchField.tooltip = "Filter Unity menu items by path";
            searchField.style.marginBottom = 6f;
            searchField.RegisterValueChangedCallback(evt => RefreshItems(evt.newValue));
            searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
            root.Add(searchField);

            var content = new VisualElement();
            content.style.flexGrow = 1f;
            content.style.minHeight = 0f;

            listView = new ListView
            {
                itemsSource = visibleMenuItems,
                fixedItemHeight = 20f,
                selectionType = SelectionType.Single,
                makeItem = () => new Label(),
                bindItem = (element, index) => BindItem((Label)element, visibleMenuItems[index]),
            };
            listView.style.flexGrow = 1f;
            listView.selectionChanged += _ => UpdateAddButton();
            listView.itemsChosen += AddChosenItem;
            content.Add(listView);

            emptyLabel = new Label("No matching menu items");
            emptyLabel.style.flexGrow = 1f;
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.display = DisplayStyle.None;
            content.Add(emptyLabel);
            root.Add(content);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 6f;

            buttons.Add(new Button(Close) { text = "Cancel" });
            addButton = new Button(AddSelectedItem) { text = "Add" };
            addButton.style.marginLeft = 4f;
            buttons.Add(addButton);
            root.Add(buttons);

            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            RefreshItems(string.Empty);
            searchField.schedule.Execute(() => searchField.Focus());
        }

        private static void BindItem(Label label, string menuPath)
        {
            label.text = menuPath;
            label.tooltip = menuPath;
            label.style.paddingLeft = 4f;
            label.style.overflow = Overflow.Hidden;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.textOverflow = TextOverflow.Ellipsis;
        }

        private void RefreshItems(string query)
        {
            visibleMenuItems.Clear();
            query = query?.Trim();
            foreach (var menuPath in BookmarkMenuItems.All)
            {
                if (string.IsNullOrEmpty(query) ||
                    menuPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    visibleMenuItems.Add(menuPath);
                }
            }

            listView.ClearSelection();
            listView.RefreshItems();
            var hasItems = visibleMenuItems.Count > 0;
            listView.style.display = hasItems ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = hasItems ? DisplayStyle.None : DisplayStyle.Flex;
            if (hasItems)
            {
                listView.selectedIndex = 0;
            }

            UpdateAddButton();
        }

        private void AddChosenItem(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is string menuPath)
                {
                    AddMenuItem(menuPath);
                    return;
                }
            }
        }

        private void AddSelectedItem()
        {
            if (listView.selectedItem is string menuPath)
            {
                AddMenuItem(menuPath);
            }
        }

        private void AddMenuItem(string menuPath)
        {
            if (owner == null)
            {
                Close();
                return;
            }

            var result = owner.AddMenuItem(menuPath);
            if (result.Added > 0)
            {
                Close();
                return;
            }

            var message = result.Duplicate > 0
                ? "This menu item is already bookmarked."
                : "This menu item is no longer available.";
            ShowNotification(new GUIContent(message));
        }

        private void UpdateAddButton()
        {
            addButton?.SetEnabled(listView?.selectedItem is string);
        }

        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            AddSelectedItem();
            evt.StopPropagation();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }
    }
}
