using UnityEditor;

namespace AssetBookmarks.Editor
{
    public class AssetBookmarksWindow : EditorWindow
    {
        [MenuItem("Window/Asset Bookmarks")]
        public static void ShowWindow()
        {
            GetWindow<AssetBookmarksWindow>(utility: false, title: "Asset Bookmarks", focus: true);
        }
    }
}