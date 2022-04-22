using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UniverseLib.UI.Widgets
{
    /// <summary>
    /// A scrollbar which automatically resizes itself (and its handle) depending on the size of the content and viewport.
    /// </summary>
    public class AutoSliderScrollbar : UIBehaviourModel
    {
        public override GameObject UIRoot => Slider?.gameObject;

        public Slider Slider { get; }
        public Scrollbar Scrollbar { get; }
        public RectTransform ContentRect { get; }
        public RectTransform ViewportRect { get; }

        public AutoSliderScrollbar(Scrollbar scrollbar, Slider slider, RectTransform contentRect, RectTransform viewportRect)
        {
            this.Scrollbar = scrollbar;
            this.Slider = slider;
            this.ContentRect = contentRect;
            this.ViewportRect = viewportRect;

            this.Scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
            this.Slider.onValueChanged.AddListener(this.OnSliderValueChanged);

            //this.RefreshVisibility();
            this.Slider.Set(0f, false);

            UpdateSliderHandle();
        }

        private float lastAnchorPosition;
        private float lastContentHeight;
        private float lastViewportHeight;
        private bool _refreshWanted;

        public override void Update()
        {
            if (!Enabled)
                return;

            _refreshWanted = false;
            if (ContentRect.localPosition.y != lastAnchorPosition)
            {
                lastAnchorPosition = ContentRect.localPosition.y;
                _refreshWanted = true;
            }
            if (ContentRect.rect.height != lastContentHeight)
            {
                lastContentHeight = ContentRect.rect.height;
                _refreshWanted = true;
            }
            if (ViewportRect.rect.height != lastViewportHeight)
            {
                lastViewportHeight = ViewportRect.rect.height;
                _refreshWanted = true;
            }

            if (_refreshWanted)
                UpdateSliderHandle();
        }

        public void UpdateSliderHandle()
        {
            // calculate handle size based on viewport / total data height
            float totalHeight = ContentRect.rect.height;
            float viewportHeight = ViewportRect.rect.height;

            if (totalHeight <= viewportHeight)
            {
                Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
                Slider.value = 0f;
                Slider.interactable = false;
                return;
            }

            float handleHeight = viewportHeight * Math.Min(1, viewportHeight / totalHeight);
            handleHeight = Math.Max(15f, handleHeight);

            // resize the handle container area for the size of the handle (bigger handle = smaller container)
            RectTransform container = Slider.m_HandleContainerRect;
            container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
            container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

            // set handle size
            Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

            // if slider is 100% height then make it not interactable
            Slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);

            float val = 0f;
            if (totalHeight > 0f)
                val = (float)((decimal)ContentRect.localPosition.y / (decimal)(totalHeight - ViewportRect.rect.height));

            Slider.value = val;
        }

        public void OnScrollbarValueChanged(float value)
        {
            value = 1f - value;
            if (this.Slider.value != value)
                this.Slider.Set(value, false);
            //OnValueChanged?.Invoke(value);
        }

        public void OnSliderValueChanged(float value)
        {
            value = 1f - value;
            this.Scrollbar.value = value;
            //OnValueChanged?.Invoke(value);
        }

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}