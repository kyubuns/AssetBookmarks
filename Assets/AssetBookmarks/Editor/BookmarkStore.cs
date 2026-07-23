using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    internal readonly struct BookmarkAddResult
    {
        internal BookmarkAddResult(int added, int duplicate, int invalid)
        {
            Added = added;
            Duplicate = duplicate;
            Invalid = invalid;
        }

        internal int Added { get; }
        internal int Duplicate { get; }
        internal int Invalid { get; }
    }

    internal sealed class BookmarkStore
    {
        private const int CurrentSchemaVersion = 2;
        private const string StorageKeyPrefix = "AssetBookmarks.v2";
        private const string LegacyKeyPrefix = "AssetBookmarks";

        private readonly string storageKey;

        private BookmarkStore(string storageKey, List<Bookmark> items, bool migratedLegacyData)
        {
            this.storageKey = storageKey;
            Items = items;
            MigratedLegacyData = migratedLegacyData;
        }

        internal List<Bookmark> Items { get; }
        internal bool MigratedLegacyData { get; }

        internal static BookmarkStore Load()
        {
            var storageKey = CreateStorageKey();
            var hasCurrentData = EditorPrefs.HasKey(storageKey);
            var items = hasCurrentData
                ? Deserialize(EditorPrefs.GetString(storageKey, string.Empty))
                : DeserializeLegacy(EditorPrefs.GetString(CreateLegacyKey(), string.Empty));

            Sanitize(items);

            var migratedLegacyData = !hasCurrentData && items.Count > 0;
            var store = new BookmarkStore(storageKey, items, migratedLegacyData);
            if (migratedLegacyData)
            {
                store.Save();
            }
            else
            {
                store.RefreshProjectPaths();
            }

            return store;
        }

        internal BookmarkAddResult AddPaths(IEnumerable<string> paths)
        {
            var added = 0;
            var duplicate = 0;
            var invalid = 0;

            foreach (var sourcePath in paths)
            {
                if (!TryCreateBookmark(sourcePath, out var bookmark))
                {
                    invalid++;
                    continue;
                }

                if (Contains(bookmark))
                {
                    duplicate++;
                    continue;
                }

                Items.Add(bookmark);
                added++;
            }

            if (added > 0)
            {
                Save();
            }

            return new BookmarkAddResult(added, duplicate, invalid);
        }

        internal BookmarkAddResult AddUrl(string value)
        {
            if (!Bookmark.TryNormalizeUrl(value, out var normalizedUrl))
            {
                return new BookmarkAddResult(0, 0, 1);
            }

            var bookmark = Bookmark.CreateUrl(normalizedUrl);
            if (Contains(bookmark))
            {
                return new BookmarkAddResult(0, 1, 0);
            }

            Items.Add(bookmark);
            Save();
            return new BookmarkAddResult(1, 0, 0);
        }

        internal void Remove(Bookmark bookmark)
        {
            if (Items.Remove(bookmark))
            {
                Save();
            }
        }

        internal void ApplyVisibleOrder(IReadOnlyList<Bookmark> orderedVisibleItems)
        {
            var visibleItems = new HashSet<Bookmark>(orderedVisibleItems);
            var orderedIndex = 0;
            var changed = false;
            for (var index = 0; index < Items.Count; index++)
            {
                if (!visibleItems.Contains(Items[index]))
                {
                    continue;
                }

                var orderedItem = orderedVisibleItems[orderedIndex++];
                changed |= !ReferenceEquals(Items[index], orderedItem);
                Items[index] = orderedItem;
            }

            if (changed)
            {
                Save();
            }
        }

        internal bool RefreshProjectPaths()
        {
            var changed = false;
            foreach (var bookmark in Items)
            {
                changed |= bookmark.RefreshProjectPath();
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        internal void Save()
        {
            var document = new BookmarkDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                Items = Items,
            };
            EditorPrefs.SetString(storageKey, JsonUtility.ToJson(document));
        }

        private static string CreateStorageKey()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/')
                .TrimEnd('/');
            return $"{StorageKeyPrefix}.{Hash128.Compute(projectRoot)}";
        }

        private static string CreateLegacyKey()
        {
            return $"{LegacyKeyPrefix}{Application.productName}";
        }

        private static List<Bookmark> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Bookmark>();
            }

            try
            {
                var document = JsonUtility.FromJson<BookmarkDocument>(json);
                return document?.Items ?? new List<Bookmark>();
            }
            catch (ArgumentException)
            {
                return new List<Bookmark>();
            }
        }

        private static List<Bookmark> DeserializeLegacy(string serializedItems)
        {
            var items = new List<Bookmark>();
            if (string.IsNullOrEmpty(serializedItems))
            {
                return items;
            }

            foreach (var serializedItem in serializedItems.Split(','))
            {
                var separatorIndex = serializedItem.IndexOf('|');
                if (separatorIndex <= 0 || separatorIndex >= serializedItem.Length - 1)
                {
                    continue;
                }

                var firstValue = serializedItem.Substring(0, separatorIndex);
                var secondValue = serializedItem.Substring(separatorIndex + 1);
                if (firstValue == "o")
                {
                    items.Add(Bookmark.CreateExternal(NormalizeExternalPath(secondValue)));
                    continue;
                }

                if (Enum.TryParse(secondValue, out LegacyOpenType legacyOpenType))
                {
                    items.Add(Bookmark.CreateProjectAsset(
                        NormalizeAssetPath(firstValue),
                        ConvertLegacyOpenType(legacyOpenType)));
                }
            }

            return items;
        }

        private static BookmarkOpenMode ConvertLegacyOpenType(LegacyOpenType legacyOpenType)
        {
            switch (legacyOpenType)
            {
                case LegacyOpenType.Open:
                    return BookmarkOpenMode.Open;
                case LegacyOpenType.Focus:
                    return BookmarkOpenMode.Select;
                case LegacyOpenType.Finder:
                    return BookmarkOpenMode.Reveal;
                case LegacyOpenType.App:
                    return BookmarkOpenMode.DefaultApplication;
                default:
                    return BookmarkOpenMode.Select;
            }
        }

        private static void Sanitize(List<Bookmark> items)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (var index = items.Count - 1; index >= 0; index--)
            {
                var item = items[index];
                if (item == null)
                {
                    items.RemoveAt(index);
                    continue;
                }

                item.EnsureValidData();
                var identity = GetIdentity(item);
                if (string.IsNullOrEmpty(item.StoredPath) ||
                    string.IsNullOrEmpty(identity) ||
                    !identities.Add(identity))
                {
                    items.RemoveAt(index);
                }
            }
        }

        private bool Contains(Bookmark candidate)
        {
            var candidateIdentity = GetIdentity(candidate);
            return Items.Exists(item =>
                StringComparer.Ordinal.Equals(GetIdentity(item), candidateIdentity));
        }

        private static string GetIdentity(Bookmark bookmark)
        {
            if (bookmark.Kind == BookmarkKind.ProjectAsset && !string.IsNullOrEmpty(bookmark.AssetGuid))
            {
                return $"asset:{bookmark.AssetGuid.ToLowerInvariant()}";
            }

            if (bookmark.Kind == BookmarkKind.Url)
            {
                return Bookmark.TryNormalizeUrl(bookmark.ResolvedPath, out var normalizedUrl)
                    ? $"url:{normalizedUrl}"
                    : string.Empty;
            }

            var path = bookmark.ResolvedPath;
            if (Application.platform != RuntimePlatform.LinuxEditor)
            {
                path = path.ToUpperInvariant();
            }

            return $"external:{path}";
        }

        private static bool TryCreateBookmark(string sourcePath, out Bookmark bookmark)
        {
            bookmark = null;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return false;
            }

            var normalizedSourcePath = sourcePath.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalizedSourcePath))
            {
                var absolutePath = NormalizeExternalPath(normalizedSourcePath);
                var projectRelativePath = FileUtil.GetProjectRelativePath(absolutePath);
                if (IsProjectAssetPath(projectRelativePath))
                {
                    normalizedSourcePath = NormalizeAssetPath(projectRelativePath);
                }
                else
                {
                    if (!File.Exists(absolutePath) && !Directory.Exists(absolutePath))
                    {
                        return false;
                    }

                    bookmark = Bookmark.CreateExternal(absolutePath);
                    return true;
                }
            }

            if (IsProjectAssetPath(normalizedSourcePath))
            {
                var assetPath = NormalizeAssetPath(normalizedSourcePath);
                if (!AssetDatabase.IsValidFolder(assetPath) &&
                    string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                {
                    return false;
                }

                bookmark = Bookmark.CreateProjectAsset(assetPath, GetDefaultOpenMode(assetPath));
                return true;
            }

            var externalPath = NormalizeExternalPath(normalizedSourcePath);
            if (!File.Exists(externalPath) && !Directory.Exists(externalPath))
            {
                return false;
            }

            bookmark = Bookmark.CreateExternal(externalPath);
            return true;
        }

        internal static BookmarkOpenMode GetDefaultOpenMode(string assetPath)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset)
                ? BookmarkOpenMode.Open
                : BookmarkOpenMode.Select;
        }

        private static bool IsProjectAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var normalizedPath = path.Replace('\\', '/');
            return normalizedPath == "Assets" ||
                   normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                   normalizedPath == "Packages" ||
                   normalizedPath.StartsWith("Packages/", StringComparison.Ordinal);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static string NormalizeExternalPath(string path)
        {
            var absolutePath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), path));
            var normalizedPath = absolutePath.Replace('\\', '/');
            var root = Path.GetPathRoot(absolutePath)?.Replace('\\', '/');
            return string.IsNullOrEmpty(root) || normalizedPath.Length <= root.Length
                ? normalizedPath
                : normalizedPath.TrimEnd('/');
        }

        [Serializable]
        private sealed class BookmarkDocument
        {
            public int SchemaVersion;
            public List<Bookmark> Items = new List<Bookmark>();
        }

        private enum LegacyOpenType
        {
            Open,
            Focus,
            Finder,
            App,
        }
    }
}
