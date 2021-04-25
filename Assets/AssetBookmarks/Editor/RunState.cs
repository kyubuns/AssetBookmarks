using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    public partial class AssetBookmarksWindow
    {
        private class RunState : IWindowState
        {
            private readonly Model _model;

            public RunState(Model model)
            {
                _model = model;
            }

            public void Dispose()
            {
            }

            public void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                EditorGUI.LabelField(rect, _model.StringList[index]);
            }

            public IWindowState OnGui()
            {
                _model.ReorderableList.DoLayoutList();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Edit")) return new EditState(_model);
                return null;
            }
        }
    }
}