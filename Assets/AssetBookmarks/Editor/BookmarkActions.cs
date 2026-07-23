using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    internal static class BookmarkActions
    {
        internal static bool Open(Bookmark bookmark)
        {
            if (!bookmark.IsAvailable)
            {
                return false;
            }

            var path = bookmark.ResolvedPath;
            if (bookmark.Kind == BookmarkKind.Url)
            {
                Application.OpenURL(path);
                return true;
            }

            if (bookmark.Kind == BookmarkKind.External)
            {
                EditorUtility.OpenWithDefaultApp(path);
                return true;
            }

            switch (bookmark.OpenMode)
            {
                case BookmarkOpenMode.Select:
                    SelectAsset(path);
                    break;
                case BookmarkOpenMode.Open:
                    OpenAsset(path);
                    break;
                case BookmarkOpenMode.Reveal:
                    EditorUtility.RevealInFinder(ToAbsoluteProjectPath(path));
                    break;
                case BookmarkOpenMode.DefaultApplication:
                    EditorUtility.OpenWithDefaultApp(ToAbsoluteProjectPath(path));
                    break;
                default:
                    SelectAsset(path);
                    break;
            }

            return true;
        }

        internal static string GetActionLabel(Bookmark bookmark)
        {
            if (bookmark.Kind == BookmarkKind.External || bookmark.Kind == BookmarkKind.Url)
            {
                return "Open";
            }

            switch (bookmark.OpenMode)
            {
                case BookmarkOpenMode.Select:
                    return "Select";
                case BookmarkOpenMode.Open:
                    return "Open";
                case BookmarkOpenMode.Reveal:
                    return "Reveal";
                case BookmarkOpenMode.DefaultApplication:
                    return "Launch";
                default:
                    return "Open";
            }
        }

        internal static string GetModeLabel(BookmarkOpenMode mode)
        {
            switch (mode)
            {
                case BookmarkOpenMode.Select:
                    return "Select in Project";
                case BookmarkOpenMode.Open:
                    return "Open in Unity";
                case BookmarkOpenMode.Reveal:
                    return "Reveal in Finder";
                case BookmarkOpenMode.DefaultApplication:
                    return "Open in Default App";
                default:
                    return "Select in Project";
            }
        }

        private static void SelectAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.FocusProjectWindow();
        }

        private static void OpenAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || AssetDatabase.IsValidFolder(path) || !AssetDatabase.OpenAsset(asset))
            {
                SelectAsset(path);
            }
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
