using NUnit.Framework;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class UrlBookmarkTests
    {
        [TestCase("jenkins.example.com/job/Build", "https://jenkins.example.com/job/Build")]
        [TestCase("http://localhost:8080/job/Test/", "http://localhost:8080/job/Test/")]
        public void UrlNormalizationAcceptsWebAddresses(string input, string expectedPrefix)
        {
            Assert.That(Bookmark.TryNormalizeUrl(input, out var normalizedUrl), Is.True);
            Assert.That(normalizedUrl, Does.StartWith(expectedPrefix));
        }

        [TestCase("")]
        [TestCase("ftp://example.com/file")]
        [TestCase("not a url")]
        public void UrlNormalizationRejectsUnsupportedValues(string input)
        {
            Assert.That(Bookmark.TryNormalizeUrl(input, out _), Is.False);
        }

        [Test]
        public void UrlBookmarkUsesReadableNameWithoutNetworkAccess()
        {
            var bookmark = Bookmark.CreateUrl("https://jenkins.example.com/job/Build/");

            Assert.That(bookmark.Kind, Is.EqualTo(BookmarkKind.Url));
            Assert.That(bookmark.DisplayName, Is.EqualTo("Build · jenkins.example.com"));
            Assert.That(bookmark.IsAvailable, Is.True);
        }
    }
}
