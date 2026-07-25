using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    [FilePath("UserSettings/AssetBookmarksSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BookmarkStorageSettings : ScriptableSingleton<BookmarkStorageSettings>
    {
        private const string DefaultRelativePath = "UserSettings/AssetBookmarks.json";

        [SerializeField]
        private string customStoragePath = string.Empty;

        internal static BookmarkStorageSettings Current => instance;
        internal event Action Changed;
        internal string StoragePath => UsesCustomStorage ? customStoragePath : DefaultStoragePath;
        internal string DefaultStoragePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultRelativePath));
        internal bool UsesCustomStorage => !string.IsNullOrEmpty(customStoragePath) && Path.IsPathRooted(customStoragePath);

        internal bool TrySetStoragePath(string value, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TryUseDefault(out error);
            }

            try
            {
                var path = NormalizePath(value);
                if (PathsEqual(path, DefaultStoragePath))
                {
                    return TryUseDefault(out error);
                }

                if (!PathsEqual(path, StoragePath))
                {
                    BookmarkStore.PrepareStorageFile(StoragePath, path);
                    SetPath(path);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        internal bool TryUseDefault(out string error)
        {
            try
            {
                if (UsesCustomStorage)
                {
                    SetPath(string.Empty);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void SetPath(string path)
        {
            var previousPath = customStoragePath;
            customStoragePath = path;
            try
            {
                Save(true);
            }
            catch
            {
                customStoragePath = previousPath;
                throw;
            }

            Changed?.Invoke();
        }

        private static string NormalizePath(string value)
        {
            value = value.Trim();
            if (!Path.IsPathRooted(value))
            {
                throw new InvalidDataException("Choose an absolute path.");
            }

            var path = Path.GetFullPath(value);
            if (Directory.Exists(path) ||
                !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Choose a JSON file.");
            }

            return path;
        }

        private static bool PathsEqual(string first, string second)
        {
            var comparison = Application.platform == RuntimePlatform.LinuxEditor
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            return string.Equals(first, second, comparison);
        }
    }
}
