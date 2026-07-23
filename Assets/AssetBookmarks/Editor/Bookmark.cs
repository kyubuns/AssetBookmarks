using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    internal enum BookmarkKind
    {
        ProjectAsset,
        External,
    }

    internal enum BookmarkOpenMode
    {
        Select,
        Open,
        Reveal,
        DefaultApplication,
    }

    [Serializable]
    internal sealed class Bookmark
    {
        [SerializeField] private string id;
        [SerializeField] private string path;
        [SerializeField] private string assetGuid;
        [SerializeField] private BookmarkKind kind;
        [SerializeField] private BookmarkOpenMode openMode;

        private Bookmark()
        {
        }

        internal string Id => id;
        internal string StoredPath => path;
        internal string AssetGuid => assetGuid;
        internal BookmarkKind Kind => kind;
        internal BookmarkOpenMode OpenMode => openMode;

        internal string ResolvedPath
        {
            get
            {
                if (kind != BookmarkKind.ProjectAsset || string.IsNullOrEmpty(assetGuid))
                {
                    return path;
                }

                var currentPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                return string.IsNullOrEmpty(currentPath) ? path : currentPath;
            }
        }

        internal string DisplayName
        {
            get
            {
                var resolvedPath = ResolvedPath;
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    return "Untitled";
                }

                var trimmedPath = resolvedPath.TrimEnd('/', '\\');
                var fileName = Path.GetFileName(trimmedPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    return resolvedPath;
                }

                return kind == BookmarkKind.ProjectAsset && !AssetDatabase.IsValidFolder(resolvedPath)
                    ? Path.GetFileNameWithoutExtension(fileName)
                    : fileName;
            }
        }

        internal bool IsAvailable
        {
            get
            {
                var resolvedPath = ResolvedPath;
                if (kind == BookmarkKind.ProjectAsset)
                {
                    return AssetDatabase.IsValidFolder(resolvedPath) ||
                           AssetDatabase.LoadMainAssetAtPath(resolvedPath) != null;
                }

                return File.Exists(resolvedPath) || Directory.Exists(resolvedPath);
            }
        }

        internal static Bookmark CreateProjectAsset(string assetPath, BookmarkOpenMode mode)
        {
            return new Bookmark
            {
                id = Guid.NewGuid().ToString("N"),
                path = assetPath,
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath),
                kind = BookmarkKind.ProjectAsset,
                openMode = mode,
            };
        }

        internal static Bookmark CreateExternal(string absolutePath)
        {
            return new Bookmark
            {
                id = Guid.NewGuid().ToString("N"),
                path = absolutePath,
                assetGuid = string.Empty,
                kind = BookmarkKind.External,
                openMode = BookmarkOpenMode.DefaultApplication,
            };
        }

        internal void EnsureValidData()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
            }

            path = path?.Replace('\\', '/') ?? string.Empty;
            assetGuid = assetGuid ?? string.Empty;

            if (kind == BookmarkKind.ProjectAsset && string.IsNullOrEmpty(assetGuid))
            {
                assetGuid = AssetDatabase.AssetPathToGUID(path);
            }

            if (kind == BookmarkKind.External)
            {
                openMode = BookmarkOpenMode.DefaultApplication;
            }
        }

        internal bool RefreshProjectPath()
        {
            if (kind != BookmarkKind.ProjectAsset || string.IsNullOrEmpty(assetGuid))
            {
                return false;
            }

            var currentPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(currentPath) || currentPath == path)
            {
                return false;
            }

            path = currentPath;
            return true;
        }

        internal void SetOpenMode(BookmarkOpenMode mode)
        {
            if (kind == BookmarkKind.ProjectAsset)
            {
                openMode = mode;
            }
        }
    }
}
