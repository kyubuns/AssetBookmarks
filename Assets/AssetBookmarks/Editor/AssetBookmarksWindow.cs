using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    public partial class AssetBookmarksWindow : EditorWindow
    {
        [MenuItem("Window/Asset Bookmarks")]
        public static void ShowWindow()
        {
            GetWindow<AssetBookmarksWindow>(utility: false, title: "Asset Bookmarks", focus: true);
        }

        private IWindowState _state;

        public void OnEnable()
        {
            var model = new Model();
            _state = new RunState(model);
            model.ReorderableList.drawElementCallback += DrawElementCallback;
            model.ReorderableList.drawElementBackgroundCallback += DrawElementBackgroundCallback;
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            _state.DrawElementCallback(rect, index, isActive, isFocused);
        }

        private void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            _state.DrawElementBackgroundCallback(rect, index, isActive, isFocused);
        }

        public void OnGUI()
        {
            var nextState = _state.OnGui();
            if (nextState != null)
            {
                _state.Dispose();
                _state = nextState;
            }
        }

        private class Model
        {
            public Model()
            {
                Items = new List<IItem>();
                ReorderableList = new ReorderableList(
                    elements: Items,
                    elementType: typeof(string),
                    draggable: false,
                    displayHeader: false,
                    displayAddButton: false,
                    displayRemoveButton: false
                );

                var elements = EditorPrefs.GetString(PlayerPrefsKey, "").Split(',');
                foreach (var element in elements)
                {
                    var e = element.Split('|');

                    if (e.Length == 2 && Enum.TryParse<OpenType>(e[1], out var t))
                    {
                        Items.Add(new ProjectItem(e[0], t));
                    }

                    if (e.Length == 2 && e[0] == "o")
                    {
                        Items.Add(new OutsideItem(e[1]));
                    }
                }
            }

            public List<IItem> Items { get; }
            public ReorderableList ReorderableList { get; }

            private static string PlayerPrefsKey => $"AssetBookmarks{Application.productName}";

            public void Save()
            {
                var stringBuilder = new StringBuilder();
                foreach (var item in Items) item.Serialize(stringBuilder);
                EditorPrefs.SetString(PlayerPrefsKey, stringBuilder.ToString());
            }
        }

        private interface IItem
        {
            void Serialize(StringBuilder stringBuilder);
        }

        private class ProjectItem : IItem
        {
            public ProjectItem(string path, OpenType openType)
            {
                Path = path;
                OpenType = openType;
            }

            public void Serialize(StringBuilder stringBuilder)
            {
                stringBuilder.Append(Path);
                stringBuilder.Append("|");
                stringBuilder.Append(OpenType);
                stringBuilder.Append(",");
            }

            public string Path { get; }
            public OpenType OpenType { get; set; }
        }

        private class OutsideItem : IItem
        {
            public OutsideItem(string path)
            {
                Path = path;
            }

            public void Serialize(StringBuilder stringBuilder)
            {
                stringBuilder.Append("o");
                stringBuilder.Append("|");
                stringBuilder.Append(Path);
                stringBuilder.Append(",");
            }

            public string Path { get; }
        }

        private interface IWindowState : IDisposable
        {
            IWindowState OnGui();
            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused);
            void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused);
        }

        private enum OpenType
        {
            Open,
            Focus,
            Finder,
        }
    }
}