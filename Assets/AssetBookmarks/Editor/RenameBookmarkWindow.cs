using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal sealed class RenameBookmarkWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(420f, 72f);

        [SerializeField]
        private AssetBookmarksWindow owner;

        [SerializeField]
        private string bookmarkId;

        [SerializeField]
        private string customName;

        [SerializeField]
        private string defaultName;

        private TextField nameField;

        internal static void Show(AssetBookmarksWindow owner, Bookmark bookmark)
        {
            var window = GetWindow<RenameBookmarkWindow>(true, "Rename Bookmark", true);
            window.owner = owner;
            window.bookmarkId = bookmark.Id;
            window.customName = bookmark.CustomName;
            window.defaultName = bookmark.DefaultDisplayName;
            window.minSize = WindowSize;
            window.maxSize = WindowSize;

            var ownerPosition = owner.position;
            window.position = new Rect(
                ownerPosition.x + Mathf.Max(0f, (ownerPosition.width - WindowSize.x) * 0.5f),
                ownerPosition.y + 36f,
                WindowSize.x,
                WindowSize.y
            );
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            nameField = new TextField("Name") { value = customName ?? string.Empty };
            nameField.tooltip = "Leave blank to use the default name.";
            nameField.labelElement.style.minWidth = 40f;
            nameField.labelElement.style.width = 40f;
            nameField.textEdition.placeholder = defaultName ?? string.Empty;
            nameField.textEdition.hidePlaceholderOnFocus = false;
            root.Add(nameField);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 6f;

            buttons.Add(new Button(Close) { text = "Cancel" });
            var saveButton = new Button(Save) { text = "Save" };
            saveButton.style.marginLeft = 4f;
            buttons.Add(saveButton);
            root.Add(buttons);

            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            nameField.schedule.Execute(() => nameField.Focus());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Save();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }

        private void Save()
        {
            if (owner != null)
            {
                owner.RenameBookmark(bookmarkId, nameField.value);
            }

            Close();
        }
    }
}
