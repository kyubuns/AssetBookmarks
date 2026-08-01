using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class BookmarkNameSearchTests
    {
        [Test]
        public void CustomNameOverridesDisplayNameAndSurvivesSerialization()
        {
            var bookmark = Bookmark.CreateExternal(Path.Combine(Path.GetTempPath(), "Tools", "Default Name"));
            bookmark.SetCustomName("  Custom Name  ");

            var restoredBookmark = JsonUtility.FromJson<Bookmark>(JsonUtility.ToJson(bookmark));
            restoredBookmark.EnsureValidData();

            Assert.That(restoredBookmark.CustomName, Is.EqualTo("Custom Name"));
            Assert.That(restoredBookmark.DefaultDisplayName, Is.EqualTo("Default Name"));
            Assert.That(restoredBookmark.DisplayName, Is.EqualTo("Custom Name"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void BlankCustomNameRestoresDefaultName(string blankName)
        {
            var bookmark = Bookmark.CreateExternal(Path.Combine(Path.GetTempPath(), "Tools", "Default Name"));
            bookmark.SetCustomName("Custom Name");

            bookmark.SetCustomName(blankName);

            Assert.That(bookmark.CustomName, Is.Empty);
            Assert.That(bookmark.DisplayName, Is.EqualTo("Default Name"));
        }

        [Test]
        public void SearchUsesOnlyEnabledFields()
        {
            var bookmark = Bookmark.CreateExternal(
                Path.Combine(Path.GetTempPath(), "Tools", "Path Target", "Default Target")
            );
            bookmark.SetCustomName("Custom Target");

            Assert.That(
                BookmarkSearch.Matches(bookmark, "Path Target", BookmarkSearchFields.Path),
                Is.True
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Path Target", BookmarkSearchFields.DefaultName),
                Is.False
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Default Target", BookmarkSearchFields.DefaultName),
                Is.True
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Default Target", BookmarkSearchFields.CustomName),
                Is.False
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Custom Target", BookmarkSearchFields.CustomName),
                Is.True
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Custom Target", BookmarkSearchFields.Path),
                Is.False
            );
            Assert.That(
                BookmarkSearch.Matches(bookmark, "Target", BookmarkSearchFields.None),
                Is.False
            );
        }
    }
}
