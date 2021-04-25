using System;
using System.Collections.Generic;
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
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            _state.DrawElementCallback(rect, index, isActive, isFocused);
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
                StringList = new List<string> { "0", "1", "2" };
                ReorderableList = new ReorderableList(
                    elements: StringList,
                    elementType: typeof(string),
                    draggable: false,
                    displayHeader: false,
                    displayAddButton: false,
                    displayRemoveButton: false
                );
            }

            public List<string> StringList { get; }
            public ReorderableList ReorderableList { get; }
        }

        private interface IWindowState : IDisposable
        {
            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused);
            IWindowState OnGui();
        }
    }
}