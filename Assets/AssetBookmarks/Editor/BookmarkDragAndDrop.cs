using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBookmarks.Editor
{
    internal static class BookmarkDragAndDrop
    {
        private const string SourceKey = "dev.kyubuns.assetbookmarks.drag-source";

        internal static bool IsBookmarkDrag => DragAndDrop.GetGenericData(SourceKey) != null;

        internal static bool TryCreatePayload(Bookmark bookmark, out UnityEngine.Object[] objectReferences, out string[] paths)
        {
            objectReferences = Array.Empty<UnityEngine.Object>();
            paths = Array.Empty<string>();
            if (bookmark == null || bookmark.Kind == BookmarkKind.Url || !bookmark.TryResolveTarget(out var path))
            {
                return false;
            }

            if (bookmark.Kind == BookmarkKind.ProjectAsset)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                {
                    return false;
                }

                objectReferences = new[] { asset };
            }

            paths = new[] { path };
            return true;
        }

        internal static bool Start(Bookmark bookmark)
        {
            if (!TryCreatePayload(bookmark, out var objectReferences, out var paths))
            {
                return false;
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = objectReferences;
            DragAndDrop.paths = paths;
            DragAndDrop.SetGenericData(SourceKey, true);
            DragAndDrop.StartDrag(bookmark.GetDisplayName(paths[0]));
            return true;
        }
    }

    internal sealed class BookmarkDragManipulator : PointerManipulator
    {
        private const float DragThreshold = 5f;

        private readonly Func<Bookmark> getBookmark;
        private Vector3 pointerStartPosition;
        private int pointerId = -1;

        internal BookmarkDragManipulator(Func<Bookmark> getBookmark)
        {
            this.getBookmark = getBookmark;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            ReleasePointer();
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            var bookmark = getBookmark();
            if (pointerId >= 0 || !CanStartManipulation(evt) || bookmark == null || bookmark.Kind == BookmarkKind.Url)
            {
                return;
            }

            pointerStartPosition = evt.position;
            pointerId = evt.pointerId;
            target.CapturePointer(pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointerId || !target.HasPointerCapture(pointerId))
            {
                return;
            }

            if ((evt.position - pointerStartPosition).sqrMagnitude < DragThreshold * DragThreshold)
            {
                return;
            }

            var bookmark = getBookmark();
            ReleasePointer();
            if (BookmarkDragAndDrop.Start(bookmark))
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == pointerId && CanStopManipulation(evt))
            {
                ReleasePointer();
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == pointerId)
            {
                pointerId = -1;
            }
        }

        private void ReleasePointer()
        {
            var capturedPointerId = pointerId;
            pointerId = -1;
            if (capturedPointerId >= 0 && target.HasPointerCapture(capturedPointerId))
            {
                target.ReleasePointer(capturedPointerId);
            }
        }
    }
}
