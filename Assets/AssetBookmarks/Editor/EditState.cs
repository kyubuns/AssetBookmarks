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
                var item = _model.Items[index];

                var popupRect = new Rect(rect);
                popupRect.x += 10;
                popupRect.width = 60;

                var labelRect = new Rect(rect);

                if (item is ProjectItem projectItem)
                {
                    var path = projectItem.Path;
                    var name = Path.GetFileNameWithoutExtension(path);

                    var prevValue = projectItem.OpenType;
                    var newValue = (OpenType) EditorGUI.EnumPopup(popupRect, prevValue);
                    if (newValue != prevValue)
                    {
                        var tmp = projectItem;
                        tmp.OpenType = newValue;
                        _model.Items[index] = tmp;
                    }

                    labelRect.x = popupRect.x + popupRect.width + 10;
                    EditorGUI.LabelField(labelRect, name);
                }

                if (item is OutsideItem outsideItem)
                {
                    var path = outsideItem.Path;

                    labelRect.x = popupRect.x + popupRect.width + 10;
                    EditorGUI.LabelField(labelRect, path);
                }
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
                            if (path.StartsWith("Assets"))
                            {
                                _model.Items.Add(new ProjectItem(path, OpenType.Focus));
                            }
                            else
                            {
                                _model.Items.Add(new OutsideItem(path));
                            }
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