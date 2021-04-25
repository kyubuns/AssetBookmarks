using UnityEditor;
using UnityEngine;

namespace AssetBookmarks.Editor
{
    public partial class AssetBookmarksWindow
    {
        private class EditState : IWindowState
        {
            private readonly Model _model;

            public EditState(Model model)
            {
                _model = model;
                _model.ReorderableList.draggable = true;
            }

            public void Dispose()
            {
                _model.ReorderableList.draggable = false;
            }

            public void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                EditorGUI.LabelField(rect, _model.StringList[index]);
            }

            public IWindowState OnGui()
            {
                _model.ReorderableList.DoLayoutList();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save")) return new RunState(_model);
                return null;
            }
        }
    }
}