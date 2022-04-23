using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.Input;

namespace UniverseLib.UI.Panels
{
    /// <summary>
    /// Handles updating, dragging and resizing all <see cref="PanelBase"/>s for the parent <see cref="UIBase"/>.
    /// </summary>
    public class PanelManager
    {
        #region STATIC

        /// <summary>Is a panel currently being resized?</summary>
        public static bool Resizing { get; internal set; }

        /// <summary>Is the resize cursor being displayed?</summary>
        public static bool ResizePrompting => resizeCursor && resizeCursorUIBase.Enabled;

        protected internal static readonly List<PanelDragger> allDraggers = new();

        protected internal static UIBase resizeCursorUIBase;
        protected internal static GameObject resizeCursor;

        protected internal static bool focusHandledThisFrame;
        protected internal static bool draggerHandledThisFrame;
        protected internal static bool wasAnyDragging;

        /// <summary>Force any current Resizing to immediately end.</summary>
        public static void ForceEndResize()
        {
            if (!resizeCursor || resizeCursorUIBase == null)
                return;

            resizeCursorUIBase.Enabled = false;
            resizeCursor.SetActive(false);
            wasAnyDragging = false;
            Resizing = false;

            foreach (PanelDragger instance in allDraggers)
            {
                instance.WasDragging = false;
                instance.WasResizing = false;
            }
        }

        protected static void CreateResizeCursorUI()
        {
            try
            {
                resizeCursorUIBase = UniversalUI.RegisterUI($"{Universe.GUID}.resizeCursor", null);
                GameObject parent = resizeCursorUIBase.RootObject;
                parent.transform.SetParent(UniversalUI.CanvasRoot.transform);

                Text text = UIFactory.CreateLabel(parent, "ResizeCursor", "↔", TextAnchor.MiddleCenter, Color.white, true, 35);
                resizeCursor = text.gameObject;

                Outline outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new(1, 1);

                RectTransform rect = resizeCursor.GetComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);

                resizeCursorUIBase.Enabled = false;
            }
            catch (Exception e)
            {
                Universe.LogWarning("Exception creating Resize Cursor UI!\r\n" + e.ToString());
            }
        }

        #endregion

        /// <summary>The UIBase which created this PanelManager.</summary>
        public UIBase Owner { get; }

        /// <summary>The GameObject which holds all of this PanelManager's Panels.</summary>
        public GameObject PanelHolder { get; }

        /// <summary>Invoked when the UIPanel heirarchy is reordered.</summary>
        public event Action OnPanelsReordered;

        /// <summary>Invoked when the user clicks outside of all panels.</summary>
        public event Action OnClickedOutsidePanels;

        protected readonly List<PanelBase> panelInstances = new();
        protected readonly Dictionary<int, PanelBase> transformIDToUIPanel = new();
        protected readonly List<PanelDragger> draggerInstances = new();

        public PanelManager(UIBase owner)
        {
            Owner = owner;
            PanelHolder = UIFactory.CreateUIObject("PanelHolder", owner.RootObject);
            RectTransform rect = PanelHolder.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
        }

        /// <summary>
        /// Determines if the PanelManager should update "focus" (ie. heirarchy order). 
        /// By default, returns true if user is clicking.
        /// </summary>
        protected virtual bool ShouldUpdateFocus
        {
            get => MouseInTargetDisplay && (InputManager.GetMouseButtonDown(0) || InputManager.GetMouseButtonDown(1));
        }

        /// <summary>
        /// The MousePosition which should be used for this PanelManager. By default, this is <see cref="InputManager.MousePosition"/>.
        /// </summary>
        protected internal virtual Vector3 MousePosition
        {
            get => InputManager.MousePosition;
        }

        /// <summary>
        /// The Screen dimensions which should be used for this PanelManager. By default, this is <see cref="Screen.width"/> x <see cref="Screen.height"/>.
        /// </summary>
        protected internal virtual Vector2 ScreenDimensions
        {
            get => new(Screen.width, Screen.height);
        }

        /// <summary>
        /// Determines if the mouse is currently in the Display used by this PanelManager. By default, this always returns true.
        /// </summary>
        protected virtual bool MouseInTargetDisplay => true;

        // invoked from UIPanel ctor
        internal protected virtual void AddPanel(PanelBase panel)
        {
            allDraggers.Add(panel.Dragger);
            this.draggerInstances.Add(panel.Dragger);

            this.panelInstances.Add(panel);
            this.transformIDToUIPanel.Add(panel.UIRoot.transform.GetInstanceID(), panel);
        }

        // invoked from UIPanel.Destroy
        internal protected virtual void RemovePanel(PanelBase panel)
        {
            allDraggers.Remove(panel.Dragger);
            this.draggerInstances.Remove(panel.Dragger);

            this.panelInstances.Remove(panel);
            this.transformIDToUIPanel.Remove(panel.UIRoot.transform.GetInstanceID());
        }

        // invoked from UIPanel.Enable
        internal protected virtual void InvokeOnPanelsReordered()
        {
            Owner.SetOnTop();
            SortDraggerHeirarchy();
            OnPanelsReordered?.Invoke();
        }

        // invoked from parent UIBase.Update
        internal protected virtual void Update()
        {
            if (!ResizePrompting && ShouldUpdateFocus)
                UpdateFocus();

            if (!draggerHandledThisFrame)
                UpdateDraggers();
        }

        protected virtual void UpdateFocus()
        {
            bool clickedInAny = false;

            // If another UIBase has already handled a user's click for focus, don't update it for this UIBase.
            if (!focusHandledThisFrame)
            {
                Vector3 mousePos = MousePosition;
                int count = PanelHolder.transform.childCount;

                for (int i = count - 1; i >= 0; i--)
                {
                    // make sure this is a real recognized panel
                    Transform transform = PanelHolder.transform.GetChild(i);
                    if (!transformIDToUIPanel.TryGetValue(transform.GetInstanceID(), out PanelBase panel))
                        continue;

                    // check if our mouse is clicking inside the panel
                    Vector3 pos = panel.Rect.InverseTransformPoint(mousePos);
                    if (!panel.Enabled || !panel.Rect.rect.Contains(pos))
                        continue;

                    // Panel was clicked in.
                    focusHandledThisFrame = true;
                    clickedInAny = true;

                    Owner.SetOnTop();

                    // if this is not the top panel, reorder and invoke the onchanged event
                    if (transform.GetSiblingIndex() != count - 1)
                    {
                        // Set the clicked panel to be on top
                        transform.SetAsLastSibling();

                        InvokeOnPanelsReordered();
                    }

                    break;
                }
            }

            if (!clickedInAny)
                OnClickedOutsidePanels?.Invoke();
        }

        // Resizing

        /// <summary>Invoked when panels are reordered.</summary>
        protected virtual void SortDraggerHeirarchy()
        {
            draggerInstances.Sort((a, b) => b.Rect.GetSiblingIndex().CompareTo(a.Rect.GetSiblingIndex()));
        }

        /// <summary>
        /// Updates all PanelDraggers owned by this PanelManager.
        /// </summary>
        internal protected virtual void UpdateDraggers()
        {
            if (!MouseInTargetDisplay)
                return;

            if (!resizeCursor)
                CreateResizeCursorUI();

            MouseState state;
            if (InputManager.GetMouseButtonDown(0))
                state = MouseState.Down;
            else if (InputManager.GetMouseButton(0))
                state = MouseState.Held;
            else
                state = MouseState.NotPressed;

            Vector3 mousePos = MousePosition;

            foreach (PanelDragger instance in draggerInstances)
            {
                if (!instance.Rect.gameObject.activeSelf)
                    continue;
        
                instance.Update(state, mousePos);

                if (draggerHandledThisFrame)
                    break;
            }

            if (wasAnyDragging && state == MouseState.NotPressed)
            {
                foreach (PanelDragger instance in draggerInstances)
                    instance.WasDragging = false;
                wasAnyDragging = false;
            }
        }
    }
}
