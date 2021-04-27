using System.IO;
using UnityEditor;
using UnityEditorInternal;
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
                _model.ReorderableList.displayRemove = true;
            }

            public void Dispose()
            {
                _model.Save();
                _model.ReorderableList.draggable = false;
                _model.ReorderableList.displayRemove = false;
            }

            public void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                var path = _model.Items[index].Path;
                var name = Path.GetFileNameWithoutExtension(path);

                var popupRect = new Rect(rect);
                popupRect.x += 10;
                popupRect.width = 60;
                var prevValue = _model.Items[index].OpenType;
                var newValue = (OpenType) EditorGUI.EnumPopup(popupRect, prevValue);
                if (newValue != prevValue)
                {
                    var tmp = _model.Items[index];
                    tmp.OpenType = newValue;
                    _model.Items[index] = tmp;
                }

                var labelRect = new Rect(rect);
                labelRect.x = popupRect.x + popupRect.width + 10;
                EditorGUI.LabelField(labelRect, name);
            }

            public void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);
            }

            public IWindowState OnGui()
            {
                _model.ReorderableList.DoLayoutList();
                EditorGUILayout.LabelField("Drag & Drop new asset");

                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        Event.current.Use();
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        foreach (var path in DragAndDrop.paths)
                        {
                            _model.Items.Add(new Item(path, OpenType.Focus));
                        }
                        Event.current.Use();
                        break;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save")) return new RunState(_model);
                return null;
            }
        }
    }
}