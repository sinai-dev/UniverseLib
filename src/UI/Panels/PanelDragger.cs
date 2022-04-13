using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UniverseLib.UI.Panels
{
    public class PanelDragger
    {
        // Static

        private const int RESIZE_THICKNESS = 10;

        // Instance

        public PanelBase UIPanel { get; private set; }
        public bool AllowDragAndResize => UIPanel.CanDragAndResize;
        
        public RectTransform Rect { get; set; }
        public event Action OnFinishResize;
        public event Action OnFinishDrag;
        
        // Dragging
        public RectTransform DragableArea { get; set; }
        public bool WasDragging { get; set; }
        private Vector2 lastDragPosition;
        
        // Resizing
        public bool WasResizing { get; internal set; }
        private bool WasHoveringResize => PanelManager.resizeCursor.activeInHierarchy;

        private ResizeTypes currentResizeType = ResizeTypes.NONE;
        private Vector2 lastResizePos;
        private ResizeTypes lastResizeHoverType;
        private Rect totalResizeRect;

        public PanelDragger(PanelBase uiPanel)
        {
            this.UIPanel = uiPanel;
            this.DragableArea = uiPanel.TitleBar.GetComponent<RectTransform>();
            this.Rect = uiPanel.Rect;
        
            UpdateResizeCache();
        }
        
        protected internal virtual void Update(MouseState state, Vector3 rawMousePos)
        {
            ResizeTypes type;
            Vector3 resizePos = Rect.InverseTransformPoint(rawMousePos);
            bool inResizePos = MouseInResizeArea(resizePos);
        
            Vector3 dragPos = DragableArea.InverseTransformPoint(rawMousePos);
            bool inDragPos = DragableArea.rect.Contains(dragPos);
        
            if (WasHoveringResize && PanelManager.resizeCursor)
                UpdateHoverImagePos();
        
            switch (state)
            {
                case MouseState.Down:
                    if (inDragPos || inResizePos)
                        UIPanel.SetActive(true);
        
                    if (inDragPos)
                    {
                        if (AllowDragAndResize)
                            OnBeginDrag();
                        PanelManager.draggerHandledThisFrame = true;
                        return;
                    }
                    else if (inResizePos)
                    {
                        type = GetResizeType(resizePos);
                        if (type != ResizeTypes.NONE)
                            OnBeginResize(type);

                        PanelManager.draggerHandledThisFrame = true;
                    }
                    break;
        
                case MouseState.Held:
                    if (WasDragging)
                    {
                        OnDrag();
                        PanelManager.draggerHandledThisFrame = true;
                    }
                    else if (WasResizing)
                    {
                        OnResize();
                        PanelManager.draggerHandledThisFrame = true;
                    }
                    break;
        
                case MouseState.NotPressed:
                    if (AllowDragAndResize && inDragPos)
                    {
                        if (WasDragging)
                            OnEndDrag();
        
                        if (WasHoveringResize)
                            OnHoverResizeEnd();

                        PanelManager.draggerHandledThisFrame = true;
                    }
                    else if (inResizePos || WasResizing)
                    {
                        if (WasResizing)
                            OnEndResize();
        
                        type = GetResizeType(resizePos);
                        if (type != ResizeTypes.NONE)
                            OnHoverResize(type);
                        else if (WasHoveringResize)
                            OnHoverResizeEnd();

                        PanelManager.draggerHandledThisFrame = true;
                    }
                    else if (WasHoveringResize)
                        OnHoverResizeEnd();
                    break;
            }
        
            return;
        }
        
        #region DRAGGING

        public virtual void OnBeginDrag()
        {
            PanelManager.wasAnyDragging = true;
            WasDragging = true;
            lastDragPosition = UIPanel.Owner.Panels.MousePosition;
        }
        
        public virtual void OnDrag()
        {
            Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
        
            Vector2 diff = (Vector2)mousePos - lastDragPosition;
            lastDragPosition = mousePos;
        
            Rect.localPosition = Rect.localPosition + (Vector3)diff;
        
            UIPanel.EnsureValidPosition();
        }
        
        public virtual void OnEndDrag()
        {
            WasDragging = false;

            OnFinishDrag?.Invoke();
        }
        
        #endregion
        
        #region RESIZE
        
        private readonly Dictionary<ResizeTypes, Rect> m_resizeMask = new()
        {
            { ResizeTypes.Top, default },
            { ResizeTypes.Left, default },
            { ResizeTypes.Right, default },
            { ResizeTypes.Bottom, default },
        };
        
        [Flags]
        public enum ResizeTypes : ulong
        {
            NONE = 0,
            Top = 1,
            Left = 2,
            Right = 4,
            Bottom = 8,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
        }
        
        // private const int HALF_THICKESS = RESIZE_THICKNESS / 2;
        private const int DBL_THICKESS = RESIZE_THICKNESS * 2;
        
        private void UpdateResizeCache()
        {
            totalResizeRect = new Rect(Rect.rect.x - RESIZE_THICKNESS + 1,
                Rect.rect.y - RESIZE_THICKNESS + 1,
                Rect.rect.width + DBL_THICKESS - 2,
                Rect.rect.height + DBL_THICKESS - 2);
        
            // calculate the four cross sections to use as flags
            if (AllowDragAndResize)
            {
                m_resizeMask[ResizeTypes.Bottom] = new Rect(
                    totalResizeRect.x,
                    totalResizeRect.y,
                    totalResizeRect.width,
                    RESIZE_THICKNESS);
        
                m_resizeMask[ResizeTypes.Left] = new Rect(
                    totalResizeRect.x,
                    totalResizeRect.y,
                    RESIZE_THICKNESS,
                    totalResizeRect.height);
        
                m_resizeMask[ResizeTypes.Top] = new Rect(
                    totalResizeRect.x,
                    Rect.rect.y + Rect.rect.height - 2,
                    totalResizeRect.width,
                    RESIZE_THICKNESS);
        
                m_resizeMask[ResizeTypes.Right] = new Rect(
                    totalResizeRect.x + Rect.rect.width + RESIZE_THICKNESS - 2,
                    totalResizeRect.y,
                    RESIZE_THICKNESS,
                    totalResizeRect.height);
            }
        }
        
        protected virtual bool MouseInResizeArea(Vector2 mousePos)
        {
            return totalResizeRect.Contains(mousePos);
        }
        
        private ResizeTypes GetResizeType(Vector2 mousePos)
        {
            // Calculate which part of the resize area we're in, if any.

            ResizeTypes mask = 0;
        
            if (m_resizeMask[ResizeTypes.Top].Contains(mousePos))
                mask |= ResizeTypes.Top;
            else if (m_resizeMask[ResizeTypes.Bottom].Contains(mousePos))
                mask |= ResizeTypes.Bottom;
        
            if (m_resizeMask[ResizeTypes.Left].Contains(mousePos))
                mask |= ResizeTypes.Left;
            else if (m_resizeMask[ResizeTypes.Right].Contains(mousePos))
                mask |= ResizeTypes.Right;
        
            return mask;
        }
        
        public virtual void OnHoverResize(ResizeTypes resizeType)
        {
            if (WasHoveringResize && lastResizeHoverType == resizeType)
                return;
        
            // we are entering resize, or the resize type has changed.
        
            lastResizeHoverType = resizeType;

            PanelManager.resizeCursorUIBase.Enabled = true;
            PanelManager.resizeCursor.SetActive(true);
        
            // set the rotation for the resize icon
            float iconRotation = 0f;
            switch (resizeType)
            {
                case ResizeTypes.TopRight:
                case ResizeTypes.BottomLeft:
                    iconRotation = 45f; break;
                case ResizeTypes.Top:
                case ResizeTypes.Bottom:
                    iconRotation = 90f; break;
                case ResizeTypes.TopLeft:
                case ResizeTypes.BottomRight:
                    iconRotation = 135f; break;
            }
        
            Quaternion rot = PanelManager.resizeCursor.transform.rotation;
            rot.eulerAngles = new Vector3(0, 0, iconRotation);
            PanelManager.resizeCursor.transform.rotation = rot;
        
            UpdateHoverImagePos();
        }
        
        // update the resize icon position to be above the mouse
        private void UpdateHoverImagePos()
        {
            Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
            RectTransform rect = UIPanel.Owner.RootRect;
            PanelManager.resizeCursorUIBase.SetOnTop();

            PanelManager.resizeCursor.transform.localPosition = rect.InverseTransformPoint(mousePos);
        }
        
        public virtual void OnHoverResizeEnd()
        {
            PanelManager.resizeCursorUIBase.Enabled = false;
            PanelManager.resizeCursor.SetActive(false);
        }
        
        public virtual void OnBeginResize(ResizeTypes resizeType)
        {
            currentResizeType = resizeType;
            lastResizePos = UIPanel.Owner.Panels.MousePosition;
            WasResizing = true;
            PanelManager.Resizing = true;
        }
        
        public virtual void OnResize()
        {
            Vector3 mousePos = UIPanel.Owner.Panels.MousePosition;
            Vector2 diff = lastResizePos - (Vector2)mousePos;
        
            if ((Vector2)mousePos == lastResizePos)
                return;

            Vector2 screenDimensions = UIPanel.Owner.Panels.ScreenDimensions;

            if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > screenDimensions.x || mousePos.y > screenDimensions.y)
                return;
        
            lastResizePos = mousePos;
        
            float diffX = (float)((decimal)diff.x / (decimal)screenDimensions.x);
            float diffY = (float)((decimal)diff.y / (decimal)screenDimensions.y);

            Vector2 anchorMin = Rect.anchorMin;
            Vector2 anchorMax = Rect.anchorMax;
        
            if (currentResizeType.HasFlag(ResizeTypes.Left))
                anchorMin.x -= diffX;
            else if (currentResizeType.HasFlag(ResizeTypes.Right))
                anchorMax.x -= diffX;
        
            if (currentResizeType.HasFlag(ResizeTypes.Top))
                anchorMax.y -= diffY;
            else if (currentResizeType.HasFlag(ResizeTypes.Bottom))
                anchorMin.y -= diffY;
        
            Vector2 prevMin = Rect.anchorMin;
            Vector2 prevMax = Rect.anchorMax;
        
            Rect.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
            Rect.anchorMax = new Vector2(anchorMax.x, anchorMax.y);

            if (Rect.rect.width < UIPanel.MinWidth)
            {
                Rect.anchorMin = new Vector2(prevMin.x, Rect.anchorMin.y);
                Rect.anchorMax = new Vector2(prevMax.x, Rect.anchorMax.y);
            }
            if (Rect.rect.height < UIPanel.MinHeight)
            {
                Rect.anchorMin = new Vector2(Rect.anchorMin.x, prevMin.y);
                Rect.anchorMax = new Vector2(Rect.anchorMax.x, prevMax.y);
            }
        }
        
        public virtual void OnEndResize()
        {
            WasResizing = false;
            PanelManager.Resizing = false;
            try { OnHoverResizeEnd(); } catch { }
            UpdateResizeCache();
            OnFinishResize?.Invoke();
        }
        
        #endregion
    }
}