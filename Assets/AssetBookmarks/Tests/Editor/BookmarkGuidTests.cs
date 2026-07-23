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
            Assert.That(bookmark.TryResolveTarget(out var resolvedPath), Is.True);
            Assert.That(resolvedPath, Is.EqualTo(renamedPath));
            Assert.That(bookmark.RefreshProjectPath(), Is.True);
            Assert.That(bookmark.StoredPath, Is.EqualTo(renamedPath));
        }

        [Test]
        public void ProjectBookmarkDoesNotResolveReplacementAtSamePath()
        {
            var assetPath = $"{temporaryFolder}/Original.anim";
            var replacementPath = $"{temporaryFolder}/Replacement.anim";
            AssetDatabase.CreateAsset(new AnimationClip(), assetPath);
            AssetDatabase.CreateAsset(new AnimationClip(), replacementPath);

            var bookmark = Bookmark.CreateProjectAsset(assetPath, BookmarkOpenMode.Select);
            var originalGuid = bookmark.AssetGuid;
            var replacementGuid = AssetDatabase.AssetPathToGUID(replacementPath);

            Assert.That(AssetDatabase.DeleteAsset(assetPath), Is.True);
            Assert.That(AssetDatabase.MoveAsset(replacementPath, assetPath), Is.Empty);
            Assert.That(AssetDatabase.AssetPathToGUID(assetPath), Is.EqualTo(replacementGuid));
            Assert.That(replacementGuid, Is.Not.EqualTo(originalGuid));

            Assert.That(bookmark.TryResolveTarget(out var resolvedPath), Is.False);
            Assert.That(resolvedPath, Is.EqualTo(assetPath));
            Assert.That(bookmark.IsAvailable, Is.False);
        }
    }
}
