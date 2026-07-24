using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBookmarks.Editor
{
    internal enum BookmarkKind
    {
        ProjectAsset,
        External,
        Url,
        SceneObject,
    }

    internal enum BookmarkOpenMode
    {
        Select,
        Open,
        Reveal,
        DefaultApplication,
    }

    internal enum BookmarkColor
    {
        None,
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        Gray,
    }

    [Serializable]
    internal sealed class Bookmark
    {
        [SerializeField] private string id;
        [SerializeField] private string path;
        [SerializeField] private string assetGuid;
        [SerializeField] private string globalObjectId;
        [SerializeField] private BookmarkKind kind;
        [SerializeField] private BookmarkOpenMode openMode;
        [SerializeField] private BookmarkColor color;
        [NonSerialized] private GameObject resolvedSceneObject;

        private Bookmark()
        {
        }

        internal string Id => id;
        internal string StoredPath => path;
        internal string AssetGuid => assetGuid;
        internal string GlobalId => globalObjectId;
        internal BookmarkKind Kind => kind;
        internal BookmarkOpenMode OpenMode => openMode;
        internal BookmarkColor Color => color;

        internal string ResolvedPath
        {
            get
            {
                if (kind == BookmarkKind.ProjectAsset &&
                    TryResolveProjectPath(out var currentPath))
                {
                    return currentPath;
                }

                if (kind == BookmarkKind.SceneObject &&
                    TryResolveScenePath(out currentPath))
                {
                    return currentPath;
                }

                return path;
            }
        }

        internal string DisplayName => GetDisplayName(ResolvedPath);

        internal bool IsAvailable => TryResolveTarget(out _);

        internal bool IsSceneLoaded
        {
            get
            {
                if (!TryResolveScenePath(out var scenePath))
                {
                    return false;
                }

                var scene = SceneManager.GetSceneByPath(scenePath);
                return scene.IsValid() && scene.isLoaded;
            }
        }

        internal bool TryResolveTarget(out string resolvedPath)
        {
            resolvedPath = path;
            if (kind == BookmarkKind.ProjectAsset)
            {
                return TryResolveProjectPath(out resolvedPath);
            }

            if (kind == BookmarkKind.SceneObject)
            {
                return TryResolveSceneObject(out _, out resolvedPath);
            }

            if (kind == BookmarkKind.Url)
            {
                return TryNormalizeUrl(resolvedPath, out _);
            }

            return File.Exists(resolvedPath) || Directory.Exists(resolvedPath);
        }

        internal string GetDisplayName(string resolvedPath)
        {
            if (kind == BookmarkKind.SceneObject)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return "Untitled";
                }

                return TryResolveScenePath(out var scenePath)
                    ? $"{path} ({Path.GetFileNameWithoutExtension(scenePath)})"
                    : path;
            }

            if (string.IsNullOrEmpty(resolvedPath))
            {
                return "Untitled";
            }

            if (kind == BookmarkKind.Url)
            {
                return GetUrlDisplayName(resolvedPath);
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

        private bool TryResolveProjectPath(out string resolvedPath)
        {
            resolvedPath = path;
            if (string.IsNullOrEmpty(assetGuid))
            {
                return false;
            }

            var currentPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(currentPath))
            {
                return false;
            }

            resolvedPath = currentPath;
            return true;
        }

        internal bool TryResolveSceneObject(out GameObject gameObject)
        {
            return TryResolveSceneObject(out gameObject, out _);
        }

        private bool TryResolveSceneObject(out GameObject gameObject, out string scenePath)
        {
            gameObject = null;
            if (!TryResolveScenePath(out scenePath, out var identifier))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (resolvedSceneObject != null && resolvedSceneObject.scene == scene)
            {
                gameObject = resolvedSceneObject;
                return true;
            }

            gameObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(identifier) as GameObject;
            resolvedSceneObject = gameObject;
            return gameObject != null && gameObject.scene == scene;
        }

        internal bool TryResolveScenePath(out string scenePath)
        {
            return TryResolveScenePath(out scenePath, out _);
        }

        private bool TryResolveScenePath(out string scenePath, out GlobalObjectId identifier)
        {
            scenePath = string.Empty;
            if (!GlobalObjectId.TryParse(globalObjectId, out identifier))
            {
                return false;
            }

            scenePath = AssetDatabase.GUIDToAssetPath(identifier.assetGUID.ToString());
            return !string.IsNullOrEmpty(scenePath);
        }

        internal static Bookmark CreateProjectAsset(string assetPath, BookmarkOpenMode mode)
        {
            return new Bookmark
            {
                id = Guid.NewGuid().ToString("N"),
                path = assetPath,
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath),
                globalObjectId = string.Empty,
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
                globalObjectId = string.Empty,
                kind = BookmarkKind.External,
                openMode = BookmarkOpenMode.DefaultApplication,
            };
        }

        internal static Bookmark CreateUrl(string url)
        {
            return new Bookmark
            {
                id = Guid.NewGuid().ToString("N"),
                path = url,
                assetGuid = string.Empty,
                globalObjectId = string.Empty,
                kind = BookmarkKind.Url,
                openMode = BookmarkOpenMode.DefaultApplication,
            };
        }

        internal static bool CanCreateSceneObject(GameObject gameObject)
        {
            return gameObject != null &&
                   !EditorUtility.IsPersistent(gameObject) &&
                   gameObject.scene.IsValid() &&
                   gameObject.scene.isLoaded &&
                   !string.IsNullOrEmpty(gameObject.scene.path);
        }

        internal static bool TryCreateSceneObject(GameObject gameObject, out Bookmark bookmark)
        {
            bookmark = null;
            if (!CanCreateSceneObject(gameObject))
            {
                return false;
            }

            var identifier = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
            var sceneGuid = AssetDatabase.AssetPathToGUID(gameObject.scene.path);
            if (string.IsNullOrEmpty(sceneGuid) ||
                !StringComparer.OrdinalIgnoreCase.Equals(identifier.assetGUID.ToString(), sceneGuid))
            {
                return false;
            }

            bookmark = new Bookmark
            {
                id = Guid.NewGuid().ToString("N"),
                path = gameObject.name,
                assetGuid = string.Empty,
                globalObjectId = identifier.ToString(),
                kind = BookmarkKind.SceneObject,
                openMode = BookmarkOpenMode.Select,
            };
            return true;
        }

        internal void EnsureValidData()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
            }

            path = kind == BookmarkKind.Url
                ? path?.Trim() ?? string.Empty
                : path ?? string.Empty;
            if (kind == BookmarkKind.ProjectAsset || kind == BookmarkKind.External)
            {
                path = path.Replace('\\', '/');
            }

            assetGuid = assetGuid ?? string.Empty;
            globalObjectId = globalObjectId ?? string.Empty;
            if (!Enum.IsDefined(typeof(BookmarkColor), color))
            {
                color = BookmarkColor.None;
            }

            if (kind == BookmarkKind.ProjectAsset && string.IsNullOrEmpty(assetGuid))
            {
                assetGuid = AssetDatabase.AssetPathToGUID(path);
            }

            if (kind == BookmarkKind.Url && TryNormalizeUrl(path, out var normalizedUrl))
            {
                path = normalizedUrl;
            }

            if (kind == BookmarkKind.External || kind == BookmarkKind.Url)
            {
                openMode = BookmarkOpenMode.DefaultApplication;
            }
            else if (kind == BookmarkKind.SceneObject)
            {
                openMode = BookmarkOpenMode.Select;
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

        internal bool RefreshSceneObjectName()
        {
            if (!TryResolveSceneObject(out var gameObject) || gameObject.name == path)
            {
                return false;
            }

            path = gameObject.name;
            return true;
        }

        internal void SetOpenMode(BookmarkOpenMode mode)
        {
            if (kind == BookmarkKind.ProjectAsset)
            {
                openMode = mode;
            }
        }

        internal void SetColor(BookmarkColor value)
        {
            color = value;
        }

        internal static bool TryNormalizeUrl(string value, out string normalizedUrl)
        {
            normalizedUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var candidate = value.Trim();
            if (candidate.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                candidate = $"https://{candidate}";
            }

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host))
            {
                return false;
            }

            normalizedUrl = uri.AbsoluteUri;
            return true;
        }

        private static string GetUrlDisplayName(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url;
            }

            var path = Uri.UnescapeDataString(uri.AbsolutePath).TrimEnd('/');
            var separatorIndex = path.LastIndexOf('/');
            var name = separatorIndex >= 0 ? path.Substring(separatorIndex + 1) : path;
            return string.IsNullOrEmpty(name) ? uri.Host : $"{name} · {uri.Host}";
        }
    }
}
