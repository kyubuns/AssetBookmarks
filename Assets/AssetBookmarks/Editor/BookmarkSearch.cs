using System;
using UnityEditor;

namespace AssetBookmarks.Editor
{
    [Flags]
    internal enum BookmarkSearchFields
    {
        None = 0,
        Path = 1 << 0,
        DefaultName = 1 << 1,
        CustomName = 1 << 2,
        All = Path | DefaultName | CustomName,
    }

    internal static class BookmarkSearchSettings
    {
        private const string SearchFieldsKey = "AssetBookmarks.v2.search-fields";

        internal static event Action Changed;

        internal static BookmarkSearchFields Fields
        {
            get
            {
                var storedFields = (BookmarkSearchFields)EditorPrefs.GetInt(
                    SearchFieldsKey,
                    (int)BookmarkSearchFields.All
                );
                return storedFields & BookmarkSearchFields.All;
            }
        }

        internal static bool Includes(BookmarkSearchFields field)
        {
            return (Fields & field) != 0;
        }

        internal static void SetIncluded(BookmarkSearchFields field, bool included)
        {
            var fields = Fields;
            var updatedFields = included ? fields | field : fields & ~field;
            if (updatedFields == fields)
            {
                return;
            }

            EditorPrefs.SetInt(SearchFieldsKey, (int)updatedFields);
            Changed?.Invoke();
        }
    }

    internal static class BookmarkSearch
    {
        internal static bool Matches(Bookmark bookmark, string query, BookmarkSearchFields fields)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            query = query.Trim();
            var resolvedPath = bookmark.ResolvedPath;
            return (fields & BookmarkSearchFields.Path) != 0 && Contains(resolvedPath, query) ||
                   (fields & BookmarkSearchFields.DefaultName) != 0 &&
                   Contains(bookmark.GetDefaultDisplayName(resolvedPath), query) ||
                   (fields & BookmarkSearchFields.CustomName) != 0 && Contains(bookmark.CustomName, query);
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
