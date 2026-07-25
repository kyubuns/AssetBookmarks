using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class BookmarkStorageTests
    {
        private string storageDirectory;
        private string storagePath;
        private string storageKey;
        private string legacyKey;

        [SetUp]
        public void SetUp()
        {
            var id = Guid.NewGuid().ToString("N");
            storageDirectory = Path.Combine(Path.GetTempPath(), $"AssetBookmarksStorageTests_{id}");
            storagePath = Path.Combine(storageDirectory, "AssetBookmarks.json");
            storageKey = $"AssetBookmarks.Tests.Storage.{id}";
            legacyKey = $"AssetBookmarks.Tests.Legacy.{id}";
        }

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.DeleteKey(storageKey);
            EditorPrefs.DeleteKey(legacyKey);
            if (Directory.Exists(storageDirectory))
            {
                Directory.Delete(storageDirectory, true);
            }
        }

        [Test]
        public void CurrentEditorPrefsDataMigratesToUserSettingsFile()
        {
            var bookmark = Bookmark.CreateUrl("https://example.com/job/");
            bookmark.SetColor(BookmarkColor.Purple);
            var legacyDocument = new LegacyV2Document
            {
                SchemaVersion = 2,
                Items = new List<Bookmark> { bookmark },
            };
            EditorPrefs.SetString(storageKey, JsonUtility.ToJson(legacyDocument));

            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            Assert.That(store.MigratedEditorPrefsData, Is.True);
            Assert.That(store.Items, Has.Count.EqualTo(1));
            Assert.That(store.Items[0].ResolvedPath, Is.EqualTo("https://example.com/job/"));
            Assert.That(store.Items[0].Color, Is.EqualTo(BookmarkColor.Purple));
            Assert.That(File.Exists(storagePath), Is.True);

            var storedDocument = JsonUtility.FromJson<StoredDocument>(File.ReadAllText(storagePath));
            Assert.That(storedDocument.SchemaVersion, Is.EqualTo(2));
            Assert.That(storedDocument.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void LegacyEditorPrefsDataMigratesToUserSettingsFile()
        {
            var externalPath = Path.Combine(storageDirectory, "External.txt");
            Directory.CreateDirectory(storageDirectory);
            File.WriteAllText(externalPath, "bookmark");
            EditorPrefs.SetString(
                legacyKey,
                $"Assets/AssetBookmarks/README.md|Focus,o|{externalPath}"
            );

            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            Assert.That(store.MigratedEditorPrefsData, Is.True);
            Assert.That(store.Items, Has.Count.EqualTo(2));
            Assert.That(store.Items.Exists(item => item.Kind == BookmarkKind.ProjectAsset), Is.True);
            Assert.That(store.Items.Exists(item => item.Kind == BookmarkKind.External), Is.True);
            Assert.That(File.Exists(storagePath), Is.True);
        }

        [Test]
        public void SavedFileTakesPrecedenceOverStaleEditorPrefs()
        {
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            store.AddUrl("https://example.com/current/");
            EditorPrefs.SetString(
                storageKey,
                JsonUtility.ToJson(new LegacyV2Document
                {
                    SchemaVersion = 2,
                    Items = new List<Bookmark>
                    {
                        Bookmark.CreateUrl("https://example.com/stale/"),
                    },
                })
            );

            var restoredStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            Assert.That(restoredStore.MigratedEditorPrefsData, Is.False);
            Assert.That(restoredStore.Items, Has.Count.EqualTo(1));
            Assert.That(restoredStore.Items[0].ResolvedPath, Is.EqualTo("https://example.com/current/"));
        }

        [Test]
        public void RepeatedSaveKeepsPreviousFileAsBackup()
        {
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            store.AddUrl("https://example.com/first/");
            store.AddUrl("https://example.com/second/");

            Assert.That(File.Exists(storagePath), Is.True);
            Assert.That(File.Exists($"{storagePath}.bak"), Is.True);
            Assert.That(BookmarkStore.Load(storagePath, storageKey, legacyKey).Items, Has.Count.EqualTo(2));
        }

        [Test]
        public void CompleteTemporaryFileIsRecoveredAfterInterruptedSave()
        {
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            store.AddUrl("https://example.com/old/");
            var pendingDocument = new StoredDocument
            {
                SchemaVersion = 2,
                Items = new List<Bookmark>
                {
                    Bookmark.CreateUrl("https://example.com/old/"),
                    Bookmark.CreateUrl("https://example.com/new/"),
                },
            };
            File.WriteAllText($"{storagePath}.tmp", JsonUtility.ToJson(pendingDocument));

            var restoredStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            Assert.That(restoredStore.Items, Has.Count.EqualTo(2));
            Assert.That(File.Exists($"{storagePath}.tmp"), Is.False);
        }

        [Test]
        public void NewStorageFileCopiesCurrentBookmarks()
        {
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            store.AddUrl("https://example.com/current/");
            var sharedPath = Path.Combine(storageDirectory, "Shared.json");

            BookmarkStore.PrepareStorageFile(storagePath, sharedPath);

            var sharedStore = BookmarkStore.Load(sharedPath, storageKey, legacyKey);
            Assert.That(sharedStore.Items, Has.Count.EqualTo(1));
            Assert.That(sharedStore.Items[0].ResolvedPath, Is.EqualTo("https://example.com/current/"));
        }

        [Test]
        public void ExistingStorageFileIsNotOverwritten()
        {
            var sharedPath = Path.Combine(storageDirectory, "Shared.json");
            var sharedStore = BookmarkStore.Load(sharedPath, storageKey, legacyKey);
            sharedStore.AddUrl("https://example.com/shared/");
            var store = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            store.AddUrl("https://example.com/current/");

            BookmarkStore.PrepareStorageFile(storagePath, sharedPath);

            sharedStore = BookmarkStore.Load(sharedPath, storageKey, legacyKey);
            Assert.That(sharedStore.Items, Has.Count.EqualTo(1));
            Assert.That(sharedStore.Items[0].ResolvedPath, Is.EqualTo("https://example.com/shared/"));
        }

        [Test]
        public void InvalidStorageFileIsRejected()
        {
            var sharedPath = Path.Combine(storageDirectory, "Invalid.json");
            Directory.CreateDirectory(storageDirectory);
            File.WriteAllText(sharedPath, "not json");

            Assert.Throws<InvalidDataException>(
                () => BookmarkStore.PrepareStorageFile(storagePath, sharedPath)
            );
        }

        [Test]
        public void IndependentStoresPreserveConcurrentAdds()
        {
            var firstStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            var secondStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            firstStore.AddUrl("https://example.com/first/");
            secondStore.AddUrl("https://example.com/second/");

            var restoredStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            Assert.That(restoredStore.Items, Has.Count.EqualTo(2));
            Assert.That(restoredStore.Items.Exists(item => item.ResolvedPath.EndsWith("/first/")), Is.True);
            Assert.That(restoredStore.Items.Exists(item => item.ResolvedPath.EndsWith("/second/")), Is.True);
        }

        [Test]
        public void StaleStoreEditPreservesConcurrentAdd()
        {
            var initialStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            initialStore.AddUrl("https://example.com/colored/");
            var firstStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            var secondStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);

            secondStore.AddUrl("https://example.com/keep/");
            firstStore.SetColor(firstStore.Items[0], BookmarkColor.Blue);

            var restoredStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            Assert.That(restoredStore.Items, Has.Count.EqualTo(2));
            Assert.That(
                restoredStore.Items.Find(item => item.ResolvedPath.EndsWith("/colored/")).Color,
                Is.EqualTo(BookmarkColor.Blue)
            );
        }

        [Test]
        public void StoreReloadsChangesWrittenByAnotherInstance()
        {
            var firstStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            var secondStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            firstStore.AddUrl("https://example.com/shared/");

            Assert.That(secondStore.ReloadIfChanged(), Is.True);
            Assert.That(secondStore.Items, Has.Count.EqualTo(1));
            Assert.That(secondStore.ReloadIfChanged(), Is.False);
        }

        [Test]
        public void ConcurrentStoresSerializeWrites()
        {
            var firstStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            var secondStore = BookmarkStore.Load(storagePath, storageKey, legacyKey);
            using var start = new ManualResetEventSlim(false);
            var tasks = new[]
            {
                Task.Run(() => AddUrls(firstStore, "first", start)),
                Task.Run(() => AddUrls(secondStore, "second", start)),
            };
            start.Set();

            Assert.That(Task.WaitAll(tasks, 5000), Is.True);
            Assert.That(BookmarkStore.Load(storagePath, storageKey, legacyKey).Items, Has.Count.EqualTo(20));
        }

        private static void AddUrls(BookmarkStore store, string prefix, ManualResetEventSlim start)
        {
            start.Wait();
            for (var index = 0; index < 10; index++)
            {
                store.AddUrl($"https://example.com/{prefix}-{index}/");
            }
        }

        [Serializable]
        private sealed class LegacyV2Document
        {
            public int SchemaVersion;
            public List<Bookmark> Items = new List<Bookmark>();
        }

        [Serializable]
        private sealed class StoredDocument
        {
            public int SchemaVersion;
            public List<Bookmark> Items = new List<Bookmark>();
        }
    }
}
