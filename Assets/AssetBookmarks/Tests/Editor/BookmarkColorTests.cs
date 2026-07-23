using NUnit.Framework;
using UnityEngine;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class BookmarkColorTests
    {
        [TestCase(BookmarkColor.None)]
        [TestCase(BookmarkColor.Red)]
        [TestCase(BookmarkColor.Orange)]
        [TestCase(BookmarkColor.Yellow)]
        [TestCase(BookmarkColor.Green)]
        [TestCase(BookmarkColor.Blue)]
        [TestCase(BookmarkColor.Purple)]
        [TestCase(BookmarkColor.Gray)]
        public void ColorSurvivesSerialization(BookmarkColor color)
        {
            var bookmark = Bookmark.CreateUrl("https://example.com/");
            bookmark.SetColor(color);

            var restoredBookmark = JsonUtility.FromJson<Bookmark>(JsonUtility.ToJson(bookmark));
            restoredBookmark.EnsureValidData();

            Assert.That(restoredBookmark.Color, Is.EqualTo(color));
        }

        [Test]
        public void MissingColorDefaultsToNone()
        {
            const string json = "{\"id\":\"bookmark\",\"path\":\"https://example.com/\",\"kind\":2,\"openMode\":3}";
            var bookmark = JsonUtility.FromJson<Bookmark>(json);

            bookmark.EnsureValidData();

            Assert.That(bookmark.Color, Is.EqualTo(BookmarkColor.None));
        }

        [Test]
        public void InvalidColorResetsToNone()
        {
            const string json = "{\"id\":\"bookmark\",\"path\":\"https://example.com/\",\"kind\":2,\"openMode\":3,\"color\":99}";
            var bookmark = JsonUtility.FromJson<Bookmark>(json);

            bookmark.EnsureValidData();

            Assert.That(bookmark.Color, Is.EqualTo(BookmarkColor.None));
        }
    }
}
