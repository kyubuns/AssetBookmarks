using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class BookmarkGuidTests
    {
        private string temporaryFolder;

        [SetUp]
        public void SetUp()
        {
            var folderName = $"AssetBookmarksTests_{Guid.NewGuid():N}";
            temporaryFolder = $"Assets/{folderName}";
            Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(temporaryFolder))
            {
                AssetDatabase.DeleteAsset(temporaryFolder);
            }
        }

        [Test]
        public void ProjectBookmarkFollowsAssetRenameByGuid()
        {
            var originalPath = $"{temporaryFolder}/Original.anim";
            var renamedPath = $"{temporaryFolder}/Renamed.anim";
            AssetDatabase.CreateAsset(new AnimationClip(), originalPath);

            var bookmark = Bookmark.CreateProjectAsset(originalPath, BookmarkOpenMode.Select);
            var originalGuid = bookmark.AssetGuid;

            Assert.That(originalGuid, Is.Not.Empty);
            Assert.That(AssetDatabase.MoveAsset(originalPath, renamedPath), Is.Empty);
            Assert.That(AssetDatabase.AssetPathToGUID(renamedPath), Is.EqualTo(originalGuid));
            Assert.That(bookmark.ResolvedPath, Is.EqualTo(renamedPath));
            Assert.That(bookmark.RefreshProjectPath(), Is.True);
            Assert.That(bookmark.StoredPath, Is.EqualTo(renamedPath));
        }
    }
}
