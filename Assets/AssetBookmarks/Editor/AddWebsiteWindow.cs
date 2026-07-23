using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal sealed class AddWebsiteWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(360f, 72f);

        [SerializeField]
        private AssetBookmarksWindow owner;

        private TextField urlField;

        internal static void Show(AssetBookmarksWindow owner)
        {
            var window = GetWindow<AddWebsiteWindow>(true, "Add Website", true);
            window.owner = owner;
            window.minSize = WindowSize;
            window.maxSize = WindowSize;

            var ownerPosition = owner.position;
            window.position = new Rect(
                ownerPosition.x + Mathf.Max(0f, (ownerPosition.width - WindowSize.x) * 0.5f),
                ownerPosition.y + 36f,
                WindowSize.x,
                WindowSize.y);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            urlField = new TextField("URL");
            urlField.tooltip = "Website URL (http or https)";
            urlField.labelElement.style.minWidth = 32f;
            urlField.labelElement.style.width = 32f;
            root.Add(urlField);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.marginTop = 6f;

            var cancelButton = new Button(Close) { text = "Cancel" };
            buttons.Add(cancelButton);

            var addButton = new Button(AddWebsite) { text = "Add" };
            addButton.style.marginLeft = 4f;
            buttons.Add(addButton);
            root.Add(buttons);

            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            urlField.schedule.Execute(() => urlField.Focus());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                AddWebsite();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        }

        private void AddWebsite()
        {
            if (owner == null)
            {
                Close();
                return;
            }

            var result = owner.AddWebsite(urlField.value);
            if (result.Added > 0)
            {
                Close();
                return;
            }

            var message = result.Duplicate > 0
                ? "This website is already bookmarked."
                : "Enter a valid http or https URL.";
            ShowNotification(new GUIContent(message));
            urlField.Focus();
        }
    }
}
