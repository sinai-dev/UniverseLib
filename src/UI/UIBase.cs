using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UniverseLib.UI
{
    /// <summary>
    /// A simple wrapper to handle a UI created with <see cref="UniversalUI.RegisterUI"/>.
    /// </summary>
    public class UIBase
    {
        public string ID { get; }
        public GameObject RootObject { get; }
        public Canvas Canvas { get; }
        public Action UpdateMethod { get; }

        /// <summary>
        /// Whether this UI is currently being displayed or not. Disabled UIs will not receive Update calls.
        /// </summary>
        public bool Enabled
        {
            get => RootObject.activeSelf;
            set => UniversalUI.SetUIActive(this.ID, value);
        }

        internal UIBase(string id, Action updateMethod)
        {
            ID = id;
            UpdateMethod = updateMethod;

            RootObject = UIFactory.CreateUIObject($"{id}_Root", UniversalUI.CanvasRoot);
            RootObject.SetActive(false);

            Canvas = RootObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.referencePixelsPerUnit = 100;
            Canvas.sortingOrder = 9999;

            CanvasScaler scaler = RootObject.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RootObject.AddComponent<GraphicRaycaster>();

            RectTransform uiRect = RootObject.GetComponent<RectTransform>();
            uiRect.anchorMin = Vector2.zero;
            uiRect.anchorMax = Vector2.one;
            uiRect.pivot = new Vector2(0.5f, 0.5f);
            uiRect.SetParent(UniversalUI.CanvasRoot.transform, false);

            RootObject.SetActive(true);
        }

        internal void Update()
        {
            try
            {
                UpdateMethod?.Invoke();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception invoking update method for {ID}: {ex}");
            }
        }
    }
}
