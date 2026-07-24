using System;
using System.Collections.Generic;
using System.IO;
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
