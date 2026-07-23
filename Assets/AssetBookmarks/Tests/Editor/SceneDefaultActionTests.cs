using NUnit.Framework;

namespace AssetBookmarks.Editor.Tests
{
    internal sealed class SceneDefaultActionTests
    {
        [TestCase("Assets/Scenes/SampleScene.unity", BookmarkOpenMode.Open)]
        [TestCase("Assets/AssetBookmarks/README.md", BookmarkOpenMode.Select)]
        public void DefaultActionMatchesAssetType(string assetPath, BookmarkOpenMode expectedMode)
        {
            Assert.That(BookmarkStore.GetDefaultOpenMode(assetPath), Is.EqualTo(expectedMode));
        }
    }
}
