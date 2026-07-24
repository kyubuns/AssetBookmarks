using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class SceneObjectBookmarkTests
    {
        private string temporaryFolder;
        private string scenePath;
        private GameObject target;

        [SetUp]
        public void SetUp()
        {
            var folderName = $"AssetBookmarksSceneObjectTests_{Guid.NewGuid():N}";
            temporaryFolder = $"Assets/{folderName}";
            scenePath = $"{temporaryFolder}/BookmarkScene.unity";
            Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            target = new GameObject("Target");
            Assert.That(EditorSceneManager.SaveScene(scene, scenePath), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(temporaryFolder))
            {
                AssetDatabase.DeleteAsset(temporaryFolder);
            }
        }

        [Test]
        public void SavedSceneObjectCreatesResolvableBookmark()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);

            Assert.That(bookmark.Kind, Is.EqualTo(BookmarkKind.SceneObject));
            Assert.That(bookmark.GlobalId, Is.Not.Empty);
            Assert.That(bookmark.DisplayName, Is.EqualTo("Target (BookmarkScene)"));
            Assert.That(bookmark.ResolvedPath, Is.EqualTo(scenePath));
            Assert.That(bookmark.IsSceneLoaded, Is.True);
            Assert.That(bookmark.TryResolveTarget(out var resolvedPath), Is.True);
            Assert.That(resolvedPath, Is.EqualTo(scenePath));
            Assert.That(bookmark.TryResolveSceneObject(out var resolvedObject), Is.True);
            Assert.That(resolvedObject, Is.SameAs(target));
        }

        [Test]
        public void SceneObjectBookmarkFollowsRename()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);
            var originalGlobalId = bookmark.GlobalId;

            target.name = "Renamed";

            Assert.That(bookmark.RefreshSceneObjectName(), Is.True);
            Assert.That(bookmark.StoredPath, Is.EqualTo("Renamed"));
            Assert.That(bookmark.DisplayName, Is.EqualTo("Renamed (BookmarkScene)"));
            Assert.That(bookmark.GlobalId, Is.EqualTo(originalGlobalId));
        }

        [Test]
        public void SceneObjectBookmarkIsUnavailableUntilSceneIsOpen()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.That(bookmark.IsSceneLoaded, Is.False);
            Assert.That(bookmark.IsAvailable, Is.False);
            Assert.That(BookmarkActions.Open(bookmark), Is.False);

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Assert.That(bookmark.IsSceneLoaded, Is.True);
            Assert.That(bookmark.IsAvailable, Is.True);
            Assert.That(bookmark.TryResolveSceneObject(out var reopenedObject), Is.True);
            Assert.That(reopenedObject.name, Is.EqualTo("Target"));
        }

        [Test]
        public void OpeningSceneObjectBookmarkSelectsGameObject()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);
            Selection.activeObject = null;

            Assert.That(BookmarkActions.Open(bookmark), Is.True);
            Assert.That(Selection.activeGameObject, Is.SameAs(target));
        }

        [Test]
        public void SceneObjectBookmarkCanOpenItsScene()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.That(BookmarkActions.OpenScene(bookmark), Is.True);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
        }

        [Test]
        public void SceneObjectBookmarkCreatesUnityDragPayload()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);

            Assert.That(BookmarkDragAndDrop.TryCreatePayload(bookmark, out var objectReferences, out var paths), Is.True);
            Assert.That(objectReferences, Is.EqualTo(new UnityEngine.Object[] { target }));
            Assert.That(paths, Is.Empty);
        }

        [Test]
        public void SceneObjectGlobalIdSurvivesSerialization()
        {
            Assert.That(Bookmark.TryCreateSceneObject(target, out var bookmark), Is.True);

            var restored = JsonUtility.FromJson<Bookmark>(JsonUtility.ToJson(bookmark));
            restored.EnsureValidData();

            Assert.That(restored.GlobalId, Is.EqualTo(bookmark.GlobalId));
            Assert.That(restored.DisplayName, Is.EqualTo("Target (BookmarkScene)"));
            Assert.That(restored.TryResolveSceneObject(out var restoredTarget), Is.True);
            Assert.That(restoredTarget, Is.SameAs(target));
        }

        [Test]
        public void StorePersistsSceneObjectAndRejectsDuplicate()
        {
            var id = Guid.NewGuid().ToString("N");
            var storageDirectory = Path.Combine(Path.GetTempPath(), $"AssetBookmarksSceneObjectStoreTests_{id}");
            var storagePath = Path.Combine(storageDirectory, "AssetBookmarks.json");
            try
            {
                var store = BookmarkStore.Load(storagePath, $"SceneObject.Storage.{id}", $"SceneObject.Legacy.{id}");
                var result = store.AddItems(Array.Empty<string>(), new[] { target, target });

                Assert.That(result.Added, Is.EqualTo(1));
                Assert.That(result.Duplicate, Is.EqualTo(1));
                Assert.That(result.Invalid, Is.Zero);

                var restoredStore = BookmarkStore.Load(
                    storagePath,
                    $"SceneObject.Storage.{id}",
                    $"SceneObject.Legacy.{id}"
                );
                Assert.That(restoredStore.Items, Has.Count.EqualTo(1));
                Assert.That(restoredStore.Items[0].TryResolveSceneObject(out var restoredTarget), Is.True);
                Assert.That(restoredTarget, Is.SameAs(target));
            }
            finally
            {
                if (Directory.Exists(storageDirectory))
                {
                    Directory.Delete(storageDirectory, true);
                }
            }
        }

        [Test]
        public void NewObjectInSavedSceneCanBeBookmarkedBeforeNextSave()
        {
            var newObject = new GameObject("New Object");

            Assert.That(Bookmark.TryCreateSceneObject(newObject, out var bookmark), Is.True);
            Assert.That(EditorSceneManager.SaveOpenScenes(), Is.True);
            Assert.That(bookmark.TryResolveSceneObject(out var resolvedObject), Is.True);
            Assert.That(resolvedObject, Is.SameAs(newObject));
        }

        [Test]
        public void UnsavedSceneObjectCannotBeBookmarked()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var unsavedObject = new GameObject("Unsaved");

            Assert.That(Bookmark.CanCreateSceneObject(unsavedObject), Is.False);
            Assert.That(Bookmark.TryCreateSceneObject(unsavedObject, out _), Is.False);
        }
    }
}
