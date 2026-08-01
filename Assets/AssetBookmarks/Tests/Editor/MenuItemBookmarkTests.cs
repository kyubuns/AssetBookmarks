using System;
using System.IO;
using NUnit.Framework;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class MenuItemBookmarkTests
    {
        private string storageDirectory;
        private string storagePath;
        private string storageKey;
        private string legacyKey;

        [SetUp]
        public void SetUp()
        {
            var id = Guid.NewGuid().ToString("N");
            storageDirectory = Path.Combine(Path.GetTempPath(), $"AssetBookmarksMenuItemTests_{id}");
            storagePath = Path.Combine(storageDirectory, "AssetBookmarks.json");
            storageKey = $"AssetBookmarks.Tests.MenuItem.Storage.{id}";
            legacyKey = $"AssetBookmarks.Tests.MenuItem.Legacy.{id}";
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(storageDirectory))
            {
                Directory.Delete(storageDirectory, true);
            }
        }

        [Test]
        public void MenuItemBookmarkUsesMenuPathAndLeafName()
        {
            const string menuPath = "Window/Asset Bookmarks";
            BookmarkMenuItems.Refresh();
            Assert.That(BookmarkMenuItems.Contains(menuPath), Is.True);

            var bookmark = Bookmark.CreateMenuItem(menuPath);
            Assert.That(bookmark.Kind, Is.EqualTo(BookmarkKind.MenuItem));
            Assert.That(bookmark.ResolvedPath, Is.EqualTo(menuPath));
            Assert.That(bookmark.DefaultDisplayName, Is.EqualTo("Asset Bookmarks"));
            Assert.That(BookmarkActions.GetActionLabel(bookmark), Is.EqualTo("Run"));
        }

        [Test]
        public void StoreRejectsDuplicateMenuItemPath()
        {
            const string menuPath = "Window/Asset Bookmarks";
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            var firstResult = store.AddMenuItem(menuPath);
            var secondResult = store.AddMenuItem(menuPath);

            Assert.That(firstResult.Added, Is.EqualTo(1));
            Assert.That(secondResult.Duplicate, Is.EqualTo(1));
            Assert.That(BookmarkStore.Load(storagePath, storageKey, legacyKey).Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void RemovedMenuItemRemainsVisibleAsUnavailable()
        {
            var bookmark = Bookmark.CreateMenuItem("Missing Menu/Missing Command");

            Assert.That(bookmark.DisplayName, Is.EqualTo("Missing Command"));
            Assert.That(bookmark.IsAvailable, Is.False);
        }

        [Test]
        public void MenuItemDoesNotCreateUnityDragPayload()
        {
            var bookmark = Bookmark.CreateMenuItem("Window/Asset Bookmarks");

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objects, out var paths), Is.False);
            Assert.That(objects, Is.Empty);
            Assert.That(paths, Is.Empty);
        }
    }
}
