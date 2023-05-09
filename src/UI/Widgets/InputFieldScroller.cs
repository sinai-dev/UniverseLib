using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib.UI.Models;
using Il2CppInterop.Runtime;

namespace UniverseLib.UI.Widgets
{
    /// <summary>
    /// A wrapper for a scrollable InputField created with <see cref="UIFactory.CreateScrollInputField"/>.<br/><br/>
    /// 
    /// This is otherwise a normal InputField, but which handles scrolling more nicely than a vanilla one.
    /// </summary>
    public class InputFieldScroller : UIBehaviourModel
    {
        public override GameObject UIRoot => InputField?.UIRoot;

        /// <summary>
        /// Invoked whenever this InputField is scrolled through (ie, 
        /// </summary>
        public Action OnScroll;

        public InputFieldRef InputField { get; }
        public AutoSliderScrollbar Slider { get; }
        public RectTransform ContentRect { get; }
        public RectTransform ViewportRect { get; }

        public static CanvasScaler RootScaler { get; private set; }

        internal string lastText;
        internal bool updateWanted;
        internal bool wantJumpToBottom;
        private float desiredContentHeight;
        private float lastContentPosition;
        private float lastViewportHeight;

        public InputFieldScroller(AutoSliderScrollbar sliderScroller, InputFieldRef inputField)
        {
            this.Slider = sliderScroller;
            this.InputField = inputField;

            inputField.OnValueChanged += OnTextChanged;

            ContentRect = inputField.UIRoot.GetComponent<RectTransform>();
            ViewportRect = ContentRect.transform.parent.GetComponent<RectTransform>();

            if (!RootScaler)
#if IL2CPP
                RootScaler = inputField.Component.gameObject.GetComponentInParent(Il2CppType.Of<CanvasScaler>()).TryCast<CanvasScaler>();
#else
                RootScaler = inputField.Component.gameObject.GetComponentInParent<CanvasScaler>();
#endif
        }

        public override void Update()
        {
            if (this.ContentRect.localPosition.y != lastContentPosition)
            {
                lastContentPosition = ContentRect.localPosition.y;
                OnScroll?.Invoke();
            }

            if (ViewportRect.rect.height != lastViewportHeight)
            {
                lastViewportHeight = ViewportRect.rect.height;
                updateWanted = true;
            }

            if (updateWanted)
            {
                updateWanted = false;
                ProcessInputText();

                float desiredHeight = Math.Max(desiredContentHeight, ViewportRect.rect.height);

                if (ContentRect.rect.height < desiredHeight)
                {
                    ContentRect.sizeDelta = new Vector2(ContentRect.sizeDelta.x, desiredHeight);
                    this.Slider.UpdateSliderHandle();
                }
                else if (ContentRect.rect.height > desiredHeight)
                {
                    ContentRect.sizeDelta = new Vector2(ContentRect.sizeDelta.x, desiredHeight);
                    this.Slider.UpdateSliderHandle();
                }
            }

            if (wantJumpToBottom)
            {
                Slider.Slider.value = 1f;
                wantJumpToBottom = false;
            }
        }

        internal void OnTextChanged(string text)
        {
            lastText = text;
            updateWanted = true;
        }

        internal void ProcessInputText()
        {
            Rect curInputRect = InputField.Component.textComponent.rectTransform.rect;
            float scaleFactor = RootScaler.scaleFactor;

            // Current text settings
            TextGenerationSettings texGenSettings = InputField.Component.textComponent.GetGenerationSettings(curInputRect.size);
            texGenSettings.generateOutOfBounds = false;
            texGenSettings.scaleFactor = scaleFactor;

            // Preferred text rect height
            TextGenerator textGen = InputField.Component.textComponent.cachedTextGeneratorForLayout;
            desiredContentHeight = textGen.GetPreferredHeight(lastText, texGenSettings) + 10;
        }

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}