using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal static class AssetBookmarksSettingsProvider
    {
        private const string SettingsPath = "Preferences/Asset Bookmarks";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.User)
            {
                label = "Asset Bookmarks",
                activateHandler = (_, root) => Build(root),
            };
        }

        internal static void Open()
        {
            SettingsService.OpenUserPreferences(SettingsPath);
        }

        private static void Build(VisualElement root)
        {
            root.Clear();
            var settings = BookmarkStorageSettings.Current;
            root.style.paddingTop = 8f;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;

            var searchHeading = new Label("Search Fields");
            searchHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(searchHeading);
            AddSearchFieldToggle(root, "Path", BookmarkSearchFields.Path);
            AddSearchFieldToggle(root, "Default Name", BookmarkSearchFields.DefaultName);
            AddSearchFieldToggle(root, "Custom Name", BookmarkSearchFields.CustomName);

            var storageHeading = new Label("Storage");
            storageHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
            storageHeading.style.marginTop = 12f;
            root.Add(storageHeading);

            var pathField = new TextField("Storage File") { value = settings.StoragePath };
            root.Add(pathField);

            void Apply()
            {
                if (!settings.TrySetStoragePath(pathField.value, out var error))
                {
                    EditorUtility.DisplayDialog("Asset Bookmarks", error, "OK");
                }

                pathField.SetValueWithoutNotify(settings.StoragePath);
            }

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.Add(new Button(Apply) { text = "Apply" });
            buttons.Add(new Button(() =>
            {
                var path = EditorUtility.OpenFilePanel(
                    "Choose Asset Bookmarks JSON",
                    Path.GetDirectoryName(settings.StoragePath),
                    "json"
                );
                if (!string.IsNullOrEmpty(path))
                {
                    pathField.value = path;
                    Apply();
                }
            }) { text = "Browse…" });
            buttons.Add(new Button(() =>
            {
                if (!settings.TryUseDefault(out var error))
                {
                    EditorUtility.DisplayDialog("Asset Bookmarks", error, "OK");
                }

                pathField.SetValueWithoutNotify(settings.StoragePath);
            }) { text = "Use Default" });
            root.Add(buttons);

            var defaultPath = new Label($"Default: {settings.DefaultStoragePath}");
            defaultPath.style.opacity = 0.6f;
            root.Add(defaultPath);
        }

        private static void AddSearchFieldToggle(
            VisualElement root,
            string label,
            BookmarkSearchFields field
        )
        {
            var toggle = new Toggle(label) { value = BookmarkSearchSettings.Includes(field) };
            toggle.RegisterValueChangedCallback(evt =>
                BookmarkSearchSettings.SetIncluded(field, evt.newValue));
            root.Add(toggle);
        }
    }
}
