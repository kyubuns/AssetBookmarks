using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private const int LockRetryCount = 20;
        private const int LockRetryDelayMs = 10;

        private readonly string storagePath;

        private BookmarkStore(string storagePath, List<Bookmark> items, bool migratedEditorPrefsData)
        {
            this.storagePath = storagePath;
            Items = items;
            MigratedEditorPrefsData = migratedEditorPrefsData;
        }

        internal List<Bookmark> Items { get; }
        internal bool MigratedEditorPrefsData { get; }

        internal static BookmarkStore Load()
        {
            return Load(BookmarkStorageSettings.Current.StoragePath, CreateStorageKey(), CreateLegacyKey());
        }

        internal static BookmarkStore Load(string storagePath, string storageKey, string legacyKey)
        {
            var migratedEditorPrefsData = false;
            var document = WithStorageLock(storagePath, () =>
            {
                var storedDocument = ReadStoredDocument(storagePath, out var recoveredFile);
                migratedEditorPrefsData = storedDocument == null && HasEditorPrefsData(storageKey, legacyKey);
                storedDocument = NormalizeDocument(
                    storedDocument ?? CreateDocumentFromEditorPrefs(storageKey, legacyKey)
                );
                var refreshed = RefreshProjectPaths(storedDocument.Items);
                refreshed |= RefreshSceneObjectNames(storedDocument.Items);
                if (migratedEditorPrefsData ||
                    recoveredFile ||
                    refreshed ||
                    storedDocument.SchemaVersion != CurrentSchemaVersion)
                {
                    storedDocument.SchemaVersion = CurrentSchemaVersion;
                    WriteDocument(storagePath, storedDocument);
                }

                return storedDocument;
            });
            return new BookmarkStore(storagePath, document.Items, migratedEditorPrefsData);
        }

        internal static void PrepareStorageFile(string sourcePath, string targetPath)
        {
            var sourceDocument = WithStorageLock(
                sourcePath,
                () => NormalizeDocument(ReadStoredDocument(sourcePath, out _))
            );
            WithStorageLock(targetPath, () =>
            {
                var targetDocument = ReadStoredDocument(targetPath, out var recoveredFile);
                if ((targetDocument == null && File.Exists(targetPath)) ||
                    (targetDocument != null && targetDocument.SchemaVersion <= 0))
                {
                    throw new InvalidDataException("The selected file is not an Asset Bookmarks JSON file.");
                }

                if (targetDocument == null)
                {
                    WriteDocument(targetPath, sourceDocument);
                }
                else if (recoveredFile)
                {
                    targetDocument.SchemaVersion = CurrentSchemaVersion;
                    WriteDocument(targetPath, targetDocument);
                }
            });
        }

        internal BookmarkAddResult AddPaths(IEnumerable<string> paths)
        {
            return AddItems(paths, Array.Empty<GameObject>());
        }

        internal BookmarkAddResult AddItems(IEnumerable<string> paths, IEnumerable<GameObject> sceneObjects)
        {
            var invalid = 0;
            var candidates = new List<Bookmark>();

            foreach (var sourcePath in paths)
            {
                if (!TryCreateBookmark(sourcePath, out var bookmark))
                {
                    invalid++;
                    continue;
                }

                candidates.Add(bookmark);
            }

            foreach (var sceneObject in sceneObjects)
            {
                if (!Bookmark.TryCreateSceneObject(sceneObject, out var bookmark))
                {
                    invalid++;
                    continue;
                }

                candidates.Add(bookmark);
            }

            var added = 0;
            var duplicate = 0;
            Update(items =>
            {
                foreach (var bookmark in candidates)
                {
                    if (ContainsIdentity(items, bookmark))
                    {
                        duplicate++;
                        continue;
                    }

                    items.Add(bookmark);
                    added++;
                }

                return added > 0;
            });
            return new BookmarkAddResult(added, duplicate, invalid);
        }

        internal BookmarkAddResult AddUrl(string value)
        {
            if (!Bookmark.TryNormalizeUrl(value, out var normalizedUrl))
            {
                return new BookmarkAddResult(0, 0, 1);
            }

            var bookmark = Bookmark.CreateUrl(normalizedUrl);
            var duplicate = false;
            Update(items =>
            {
                if (ContainsIdentity(items, bookmark))
                {
                    duplicate = true;
                    return false;
                }

                items.Add(bookmark);
                return true;
            });
            return duplicate
                ? new BookmarkAddResult(0, 1, 0)
                : new BookmarkAddResult(1, 0, 0);
        }

        internal void Remove(Bookmark bookmark)
        {
            Update(items => items.RemoveAll(item => item.Id == bookmark.Id) > 0);
        }

        internal void SetOpenMode(Bookmark bookmark, BookmarkOpenMode mode)
        {
            Update(items =>
            {
                var storedBookmark = FindById(items, bookmark.Id);
                if (storedBookmark == null || storedBookmark.OpenMode == mode)
                {
                    return false;
                }

                storedBookmark.SetOpenMode(mode);
                return true;
            });
        }

        internal void SetColor(Bookmark bookmark, BookmarkColor color)
        {
            Update(items =>
            {
                var storedBookmark = FindById(items, bookmark.Id);
                if (storedBookmark == null || storedBookmark.Color == color)
                {
                    return false;
                }

                storedBookmark.SetColor(color);
                return true;
            });
        }

        internal void SetCustomName(Bookmark bookmark, string customName)
        {
            Update(items =>
            {
                var storedBookmark = FindById(items, bookmark.Id);
                if (storedBookmark == null)
                {
                    return false;
                }

                var previousName = storedBookmark.CustomName;
                storedBookmark.SetCustomName(customName);
                return storedBookmark.CustomName != previousName;
            });
        }

        internal void ApplyVisibleOrder(IReadOnlyList<Bookmark> orderedVisibleItems)
        {
            var orderedIds = new List<string>();
            foreach (var item in orderedVisibleItems)
            {
                orderedIds.Add(item.Id);
            }

            Update(items =>
            {
                var visibleIds = new HashSet<string>(orderedIds, StringComparer.Ordinal);
                var orderedItems = new List<Bookmark>();
                foreach (var id in orderedIds)
                {
                    var item = FindById(items, id);
                    if (item != null)
                    {
                        orderedItems.Add(item);
                    }
                }

                var orderedIndex = 0;
                var changed = false;
                for (var index = 0; index < items.Count; index++)
                {
                    if (!visibleIds.Contains(items[index].Id) || orderedIndex >= orderedItems.Count)
                    {
                        continue;
                    }

                    var orderedItem = orderedItems[orderedIndex++];
                    changed |= items[index].Id != orderedItem.Id;
                    items[index] = orderedItem;
                }

                return changed;
            });
        }

        internal bool RefreshProjectPaths()
        {
            return Update(RefreshProjectPaths);
        }

        internal bool RefreshSceneObjectNames()
        {
            return Update(RefreshSceneObjectNames);
        }

        internal bool ReloadIfChanged()
        {
            var storedItems = WithStorageLock(storagePath, () =>
            {
                var document = ReadStoredDocument(storagePath, out var recoveredFile);
                if (document == null)
                {
                    return null;
                }

                document = NormalizeDocument(document);
                if (recoveredFile || document.SchemaVersion != CurrentSchemaVersion)
                {
                    document.SchemaVersion = CurrentSchemaVersion;
                    WriteDocument(storagePath, document);
                }

                return document.Items;
            });
            if (storedItems == null || ItemsEqual(Items, storedItems))
            {
                return false;
            }

            ReplaceItems(storedItems);
            return true;
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

        private static bool HasEditorPrefsData(string storageKey, string legacyKey)
        {
            return EditorPrefs.HasKey(storageKey) || EditorPrefs.HasKey(legacyKey);
        }

        private static BookmarkDocument CreateDocumentFromEditorPrefs(string storageKey, string legacyKey)
        {
            var hasCurrentData = EditorPrefs.HasKey(storageKey);
            var document = hasCurrentData
                ? DeserializeDocument(EditorPrefs.GetString(storageKey, string.Empty))
                : null;
            document ??= new BookmarkDocument
            {
                Items = DeserializeLegacy(EditorPrefs.GetString(legacyKey, string.Empty)),
            };
            document.SchemaVersion = CurrentSchemaVersion;
            return document;
        }

        private static BookmarkDocument ReadDocument(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return DeserializeDocument(File.ReadAllText(path));
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static BookmarkDocument ReadStoredDocument(string path, out bool recoveredFile)
        {
            var document = ReadDocument($"{path}.tmp");
            recoveredFile = document != null;
            if (document == null)
            {
                document = ReadDocument(path);
            }

            if (document == null)
            {
                document = ReadDocument($"{path}.bak");
                recoveredFile = document != null;
            }

            return document;
        }

        private static BookmarkDocument DeserializeDocument(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<BookmarkDocument>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void WriteDocument(string path, BookmarkDocument document)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException($"Cannot determine the settings directory for '{path}'.");
            }

            Directory.CreateDirectory(directory);
            var temporaryPath = $"{path}.tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(document, true));
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, $"{path}.bak");
                return;
            }

            File.Move(temporaryPath, path);
        }

        private static void WithStorageLock(string path, Action action)
        {
            using (AcquireStorageLock(path))
            {
                action();
            }
        }

        private static T WithStorageLock<T>(string path, Func<T> action)
        {
            using (AcquireStorageLock(path))
            {
                return action();
            }
        }

        private static FileStream AcquireStorageLock(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException($"Cannot determine the settings directory for '{path}'.");
            }

            Directory.CreateDirectory(directory);
            var lockPath = $"{path}.lock";
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (attempt < LockRetryCount - 1)
                {
                    Thread.Sleep(LockRetryDelayMs);
                }
            }
        }

        private bool Update(Func<List<Bookmark>, bool> update)
        {
            List<Bookmark> storedItems = null;
            var changed = WithStorageLock(storagePath, () =>
            {
                var document = ReadStoredDocument(storagePath, out var recoveredFile);
                document = NormalizeDocument(document);
                var contentChanged = update(document.Items);
                if (contentChanged || recoveredFile || document.SchemaVersion != CurrentSchemaVersion)
                {
                    document.SchemaVersion = CurrentSchemaVersion;
                    WriteDocument(storagePath, document);
                }

                storedItems = document.Items;
                return contentChanged;
            });
            ReplaceItems(storedItems);
            return changed;
        }

        private static bool ItemsEqual(List<Bookmark> first, List<Bookmark> second)
        {
            return JsonUtility.ToJson(new BookmarkDocument { Items = first }) ==
                   JsonUtility.ToJson(new BookmarkDocument { Items = second });
        }

        private void ReplaceItems(List<Bookmark> items)
        {
            Items.Clear();
            Items.AddRange(items);
        }

        private static Bookmark FindById(List<Bookmark> items, string id)
        {
            return items.Find(item => item.Id == id);
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
                var hasStoredTarget = item.Kind == BookmarkKind.SceneObject
                    ? !string.IsNullOrEmpty(item.GlobalId)
                    : !string.IsNullOrEmpty(item.StoredPath);
                if (!hasStoredTarget ||
                    string.IsNullOrEmpty(identity) ||
                    !identities.Add(identity))
                {
                    items.RemoveAt(index);
                }
            }
        }

        private static BookmarkDocument NormalizeDocument(BookmarkDocument document)
        {
            document ??= new BookmarkDocument { SchemaVersion = CurrentSchemaVersion };
            document.Items ??= new List<Bookmark>();
            Sanitize(document.Items);
            return document;
        }

        private static bool RefreshProjectPaths(List<Bookmark> items)
        {
            var changed = false;
            foreach (var bookmark in items)
            {
                changed |= bookmark.RefreshProjectPath();
            }

            return changed;
        }

        private static bool RefreshSceneObjectNames(List<Bookmark> items)
        {
            var changed = false;
            foreach (var bookmark in items)
            {
                changed |= bookmark.RefreshSceneObjectName();
            }

            return changed;
        }

        private static bool ContainsIdentity(List<Bookmark> items, Bookmark candidate)
        {
            var candidateIdentity = GetIdentity(candidate);
            return items.Exists(item =>
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

            if (bookmark.Kind == BookmarkKind.SceneObject)
            {
                return string.IsNullOrEmpty(bookmark.GlobalId)
                    ? string.Empty
                    : $"scene-object:{bookmark.GlobalId}";
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
