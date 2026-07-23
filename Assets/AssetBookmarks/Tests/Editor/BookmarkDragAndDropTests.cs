using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class BookmarkDragAndDropTests
    {
        private string temporaryFolder;
        private string externalFile;

        [SetUp]
        public void SetUp()
        {
            var folderName = $"AssetBookmarksDragTests_{Guid.NewGuid():N}";
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

            if (!string.IsNullOrEmpty(externalFile) && File.Exists(externalFile))
            {
                File.Delete(externalFile);
            }
        }

        [Test]
        public void PrefabPayloadContainsPrefabObjectAndProjectPath()
        {
            var prefabPath = $"{temporaryFolder}/Dragged.prefab";
            var gameObject = new GameObject("Dragged");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            var bookmark = Bookmark.CreateProjectAsset(prefabPath, BookmarkOpenMode.Select);

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objectReferences, out var paths), Is.True);
            Assert.That(paths, Is.EqualTo(new[] { prefabPath }));
            Assert.That(objectReferences, Has.Length.EqualTo(1));
            Assert.That(objectReferences[0], Is.TypeOf<GameObject>());
            Assert.That(PrefabUtility.IsPartOfPrefabAsset(objectReferences[0]), Is.True);
        }

        [Test]
        public void NonPrefabPayloadContainsMainAssetObjectAndProjectPath()
        {
            var assetPath = $"{temporaryFolder}/Dragged.anim";
            AssetDatabase.CreateAsset(new AnimationClip(), assetPath);
            var bookmark = Bookmark.CreateProjectAsset(assetPath, BookmarkOpenMode.Select);

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objectReferences, out var paths), Is.True);
            Assert.That(paths, Is.EqualTo(new[] { assetPath }));
            Assert.That(objectReferences, Has.Length.EqualTo(1));
            Assert.That(objectReferences[0], Is.TypeOf<AnimationClip>());
            Assert.That(AssetDatabase.GetAssetPath(objectReferences[0]), Is.EqualTo(assetPath));
        }

        [Test]
        public void ExternalPayloadContainsOnlyAbsolutePath()
        {
            externalFile = Path.Combine(Path.GetTempPath(), $"AssetBookmarksDrag_{Guid.NewGuid():N}.txt");
            File.WriteAllText(externalFile, "bookmark");
            var bookmark = Bookmark.CreateExternal(externalFile);

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objectReferences, out var paths), Is.True);
            Assert.That(objectReferences, Is.Empty);
            Assert.That(paths, Is.EqualTo(new[] { externalFile }));
        }

        [Test]
        public void WebsiteDoesNotCreateUnityDragPayload()
        {
            var bookmark = Bookmark.CreateUrl("https://example.com/");

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objectReferences, out var paths), Is.False);
            Assert.That(objectReferences, Is.Empty);
            Assert.That(paths, Is.Empty);
        }
    }
}
