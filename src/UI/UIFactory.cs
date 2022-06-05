using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib.Runtime;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;
using UniverseLib.UI.Widgets;
using UniverseLib.UI.Widgets.ScrollView;

namespace UniverseLib.UI
{
    /// <summary>
    /// Helper class to create Unity uGUI UI objects at runtime, as well as use some custom UniverseLib UI classes such as ScrollPool, InputFieldScroller and AutoSliderScrollbar.
    /// </summary>
    public static class UIFactory
    {
        internal static Vector2 largeElementSize = new(100, 30);
        internal static Vector2 smallElementSize = new(25, 25);
        internal static Color defaultTextColor = Color.white;

        /// <summary>
        /// Create a simple UI object with a RectTransform. <paramref name="parent"/> can be null.
        /// </summary>
        public static GameObject CreateUIObject(string name, GameObject parent, Vector2 sizeDelta = default)
        {
            //if (!parent)
            //{
            //    Universe.LogWarning($"Warning: Creating {name} but parent is null");
            //    Universe.Log(Environment.StackTrace);
            //}

            GameObject obj = new(name)
            {
                layer = 5,
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (parent)
                obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            return obj;
        }

        internal static void SetDefaultTextValues(Text text)
        {
            text.color = defaultTextColor;
            text.font = UniversalUI.DefaultFont;
            text.fontSize = 14;
        }

        internal static void SetDefaultSelectableValues(Selectable selectable)
        {
            Navigation nav = selectable.navigation;
            nav.mode = Navigation.Mode.Explicit;
            selectable.navigation = nav;

            RuntimeHelper.Instance.Internal_SetColorBlock(selectable, new Color(0.2f, 0.2f, 0.2f),
                new Color(0.3f, 0.3f, 0.3f), new Color(0.15f, 0.15f, 0.15f));
        }


        #region Layout Helpers

        /// <summary>
        /// Get and/or Add a LayoutElement component to the GameObject, and set any of the values on it.
        /// </summary>
        public static LayoutElement SetLayoutElement(GameObject gameObject, int? minWidth = null, int? minHeight = null,
            int? flexibleWidth = null, int? flexibleHeight = null, int? preferredWidth = null, int? preferredHeight = null,
            bool? ignoreLayout = null)
        {
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            if (!layout)
                layout = gameObject.AddComponent<LayoutElement>();

            if (minWidth != null)
                layout.minWidth = (int)minWidth;

            if (minHeight != null)
                layout.minHeight = (int)minHeight;

            if (flexibleWidth != null)
                layout.flexibleWidth = (int)flexibleWidth;

            if (flexibleHeight != null)
                layout.flexibleHeight = (int)flexibleHeight;

            if (preferredWidth != null)
                layout.preferredWidth = (int)preferredWidth;

            if (preferredHeight != null)
                layout.preferredHeight = (int)preferredHeight;

            if (ignoreLayout != null)
                layout.ignoreLayout = (bool)ignoreLayout;

            return layout;
        }

        /// <summary>
        /// Get and/or Add a HorizontalOrVerticalLayoutGroup (must pick one) to the GameObject, and set the values on it.
        /// </summary>
        public static T SetLayoutGroup<T>(GameObject gameObject, bool? forceWidth = null, bool? forceHeight = null,
            bool? childControlWidth = null, bool? childControlHeight = null, int? spacing = null, int? padTop = null,
            int? padBottom = null, int? padLeft = null, int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            T group = gameObject.GetComponent<T>();
            if (!group)
                group = gameObject.AddComponent<T>();

            return SetLayoutGroup(group, forceWidth, forceHeight, childControlWidth, childControlHeight, spacing, padTop,
                padBottom, padLeft, padRight, childAlignment);
        }

        /// <summary>
        /// Set the values on a HorizontalOrVerticalLayoutGroup.
        /// </summary>
        public static T SetLayoutGroup<T>(T group, bool? forceWidth = null, bool? forceHeight = null,
            bool? childControlWidth = null, bool? childControlHeight = null, int? spacing = null, int? padTop = null,
            int? padBottom = null, int? padLeft = null, int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            if (forceWidth != null)
                group.childForceExpandWidth = (bool)forceWidth;
            if (forceHeight != null)
                group.childForceExpandHeight = (bool)forceHeight;
            if (childControlWidth != null)
                group.SetChildControlWidth((bool)childControlWidth);
            if (childControlHeight != null)
                group.SetChildControlHeight((bool)childControlHeight);
            if (spacing != null)
                group.spacing = (int)spacing;
            if (padTop != null)
                group.padding.top = (int)padTop;
            if (padBottom != null)
                group.padding.bottom = (int)padBottom;
            if (padLeft != null)
                group.padding.left = (int)padLeft;
            if (padRight != null)
                group.padding.right = (int)padRight;
            if (childAlignment != null)
                group.childAlignment = (TextAnchor)childAlignment;

            return group;
        }
        
        #endregion


        #region Layout Objects

        /// <summary>
        /// Create a simple UI Object with a VerticalLayoutGroup and an Image component.
        /// <br /><br />See also: <see cref="PanelBase"/>
        /// </summary>
        /// <param name="name">The name of the panel GameObject, useful for debugging purposes</param>
        /// <param name="parent">The parent GameObject to attach this to</param>
        /// <param name="bgColor">The background color of your panel. Defaults to dark grey if null.</param>
        /// <param name="contentHolder">The GameObject which you should add your actual content on to.</param>
        /// <returns>The base panel GameObject (not for adding content to).</returns>
        public static GameObject CreatePanel(string name, GameObject parent, out GameObject contentHolder, Color? bgColor = null)
        {
            GameObject panelObj = CreateUIObject(name, parent);
            SetLayoutGroup<VerticalLayoutGroup>(panelObj, true, true, true, true, 0, 1, 1, 1, 1);

            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<Image>().color = Color.black;
            panelObj.AddComponent<RectMask2D>();

            contentHolder = CreateUIObject("Content", panelObj);

            Image bgImage = contentHolder.AddComponent<Image>();
            bgImage.type = Image.Type.Filled;
            bgImage.color = bgColor == null
                ? new(0.07f, 0.07f, 0.07f)
                : (Color)bgColor;

            SetLayoutGroup<VerticalLayoutGroup>(contentHolder, true, true, true, true, 3, 3, 3, 3, 3);

            return panelObj;
        }

        /// <summary>
        /// Create a VerticalLayoutGroup object with an Image component. Use SetLayoutGroup to create one without an image.
        /// </summary>
        public static GameObject CreateVerticalGroup(GameObject parent, string name, bool forceWidth, bool forceHeight,
            bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default, Color bgColor = default,
            TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<VerticalLayoutGroup>(groupObj, forceWidth, forceHeight, childControlWidth, childControlHeight,
                spacing, (int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a HorizontalLayoutGroup object with an Image component. Use SetLayoutGroup to create one without an image.
        /// </summary>
        public static GameObject CreateHorizontalGroup(GameObject parent, string name, bool forceExpandWidth, bool forceExpandHeight,
            bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default, Color bgColor = default,
            TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<HorizontalLayoutGroup>(groupObj, forceExpandWidth, forceExpandHeight, childControlWidth, childControlHeight,
                spacing, (int)padding.x, (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a GridLayoutGroup object with an Image component. 
        /// </summary>
        public static GameObject CreateGridGroup(GameObject parent, string name, Vector2 cellSize, Vector2 spacing, Color bgColor = default)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            GridLayoutGroup gridGroup = groupObj.AddComponent<GridLayoutGroup>();
            gridGroup.childAlignment = TextAnchor.UpperLeft;
            gridGroup.cellSize = cellSize;
            gridGroup.spacing = spacing;

            Image image = groupObj.AddComponent<Image>();

            image.color = bgColor == default
                ? new Color(0.17f, 0.17f, 0.17f)
                : bgColor;

            return groupObj;
        }

        #endregion


        #region Control and Graphic Components

        /// <summary>
        /// Create a Text component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your label</param>
        /// <param name="defaultText">The default text of the label</param>
        /// <param name="alignment">The alignment of the Text component</param>
        /// <param name="color">The Text color (default is White)</param>
        /// <param name="supportRichText">Should the Text support rich text? (Can be changed afterwards)</param>
        /// <param name="fontSize">The default font size</param>
        /// <returns>Your new Text component</returns>
        public static Text CreateLabel(GameObject parent, string name, string defaultText, TextAnchor alignment = TextAnchor.MiddleLeft,
            Color color = default, bool supportRichText = true, int fontSize = 14)
        {
            GameObject obj = CreateUIObject(name, parent);
            Text textComp = obj.AddComponent<Text>();

            SetDefaultTextValues(textComp);

            textComp.text = defaultText;
            textComp.color = color == default ? defaultTextColor : color;
            textComp.supportRichText = supportRichText;
            textComp.alignment = alignment;
            textComp.fontSize = fontSize;

            return textComp;
        }

        /// <summary>
        /// Create a ButtonRef wrapper and a Button component, providing only the default Color (highlighted and pressed colors generated automatically).
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your button</param>
        /// <param name="text">The default button text</param>
        /// <param name="normalColor">The base color for your button, with the highlighted and pressed colors generated from this.</param>
        /// <returns>A ButtonRef wrapper for your Button component.</returns>
        public static ButtonRef CreateButton(GameObject parent, string name, string text, Color? normalColor = null)
        {
            normalColor ??= new Color(0.25f, 0.25f, 0.25f);
            ButtonRef buttonRef = CreateButton(parent, name, text, default(ColorBlock));
            RuntimeHelper.Instance.Internal_SetColorBlock(buttonRef.Component, normalColor, normalColor * 1.2f, normalColor * 0.7f);
            return buttonRef;
        }

        /// <summary>
        /// Create a ButtonRef wrapper and a Button component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your button</param>
        /// <param name="text">The default button text</param>
        /// <param name="colors">The ColorBlock used for your Button component</param>
        /// <returns>A ButtonRef wrapper for your Button component.</returns>
        public static ButtonRef CreateButton(GameObject parent, string name, string text, ColorBlock colors)
        {
            GameObject buttonObj = CreateUIObject(name, parent, smallElementSize);

            GameObject textObj = CreateUIObject("Text", buttonObj);

            Image image = buttonObj.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, 1);

            Button button = buttonObj.AddComponent<Button>();
            SetDefaultSelectableValues(button);

            colors.colorMultiplier = 1;
            RuntimeHelper.Instance.Internal_SetColorBlock(button, colors);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            SetDefaultTextValues(textComp);
            textComp.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            SetButtonDeselectListener(button);

            return new ButtonRef(button);
        }
        
        // Automatically deselect buttons when clicked.
        internal static void SetButtonDeselectListener(Button button)
        {
            button.onClick.AddListener(() =>
            {
                button.OnDeselect(null);
            });
        }

        /// <summary>
        /// Create a Slider control component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your slider</param>
        /// <param name="slider">Returns the created Slider component</param>
        /// <returns>The root GameObject for your Slider</returns>
        public static GameObject CreateSlider(GameObject parent, string name, out Slider slider)
        {
            GameObject sliderObj = CreateUIObject(name, parent, smallElementSize);

            GameObject bgObj = CreateUIObject("Background", sliderObj);
            GameObject fillAreaObj = CreateUIObject("Fill Area", sliderObj);
            GameObject fillObj = CreateUIObject("Fill", fillAreaObj);
            GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", sliderObj);
            GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = new Vector2(0f, 0f);

            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);

            fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideRect.sizeDelta = new Vector2(-20f, 0f);
            handleSlideRect.anchorMin = new Vector2(0f, 0f);
            handleSlideRect.anchorMax = new Vector2(1f, 1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 0f);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            RuntimeHelper.Instance.Internal_SetColorBlock(slider, new Color(0.4f, 0.4f, 0.4f),
                new Color(0.55f, 0.55f, 0.55f), new Color(0.3f, 0.3f, 0.3f));

            return sliderObj;
        }

        /// <summary>
        /// Create a standard Unity Scrollbar component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your scrollbar</param>
        /// <param name="scrollbar">Returns the created Scrollbar component</param>
        /// <returns>The root GameObject for your Scrollbar</returns>
        public static GameObject CreateScrollbar(GameObject parent, string name, out Scrollbar scrollbar)
        {
            GameObject scrollObj = CreateUIObject(name, parent, smallElementSize);

            GameObject slideAreaObj = CreateUIObject("Sliding Area", scrollObj);
            GameObject handleObj = CreateUIObject("Handle", slideAreaObj);

            Image scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.type = Image.Type.Sliced;
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.type = Image.Type.Sliced;
            handleImage.color = new Color(0.4f, 0.4f, 0.4f);

            RectTransform slideAreaRect = slideAreaObj.GetComponent<RectTransform>();
            slideAreaRect.sizeDelta = new Vector2(-20f, -20f);
            slideAreaRect.anchorMin = Vector2.zero;
            slideAreaRect.anchorMax = Vector2.one;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 20f);

            scrollbar = scrollObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            SetDefaultSelectableValues(scrollbar);

            return scrollObj;
        }

        /// <summary>
        /// Create a Toggle control component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your toggle</param>
        /// <param name="toggle">Returns the created Toggle component</param>
        /// <param name="text">Returns the Text component for your Toggle</param>
        /// <param name="bgColor">The background color of the checkbox</param>
        /// <param name="checkWidth">The width of your checkbox</param>
        /// <param name="checkHeight">The height of your checkbox</param>
        /// <returns>The root GameObject for your Toggle control</returns>
        public static GameObject CreateToggle(GameObject parent, string name, out Toggle toggle, out Text text, Color bgColor = default, 
            int checkWidth = 20, int checkHeight = 20)
        {
            // Main obj
            GameObject toggleObj = CreateUIObject(name, parent, smallElementSize);
            SetLayoutGroup<HorizontalLayoutGroup>(toggleObj, false, false, true, true, 5, 0,0,0,0, childAlignment: TextAnchor.MiddleLeft);
            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            SetDefaultSelectableValues(toggle);
            // need a second reference so we can use it inside the lambda, since 'toggle' is an out var.
            Toggle t2 = toggle;
            toggle.onValueChanged.AddListener((bool _) => { t2.OnDeselect(null); });

            // Check mark background

            GameObject checkBgObj = CreateUIObject("Background", toggleObj);
            Image bgImage = checkBgObj.AddComponent<Image>();
            bgImage.color = bgColor == default ? new Color(0.04f, 0.04f, 0.04f, 0.75f) : bgColor;

            SetLayoutGroup<HorizontalLayoutGroup>(checkBgObj, true, true, true, true, 0, 2, 2, 2, 2);
            SetLayoutElement(checkBgObj, minWidth: checkWidth, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // Check mark image

            GameObject checkMarkObj = CreateUIObject("Checkmark", checkBgObj);
            Image checkImage = checkMarkObj.AddComponent<Image>();
            checkImage.color = new Color(0.8f, 1, 0.8f, 0.3f);

            // Label 

            GameObject labelObj = CreateUIObject("Label", toggleObj);
            text = labelObj.AddComponent<Text>();
            text.text = "";
            text.alignment = TextAnchor.MiddleLeft;
            SetDefaultTextValues(text);

            SetLayoutElement(labelObj, minWidth: 0, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // References

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            return toggleObj;
        }

        /// <summary>
        /// Create a standard InputField control and an InputFieldRef wrapper for it.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your InputField</param>
        /// <param name="placeHolderText">The placeholder text for your InputField component</param>
        /// <returns>An InputFieldRef wrapper for your InputField</returns>
        public static InputFieldRef CreateInputField(GameObject parent, string name, string placeHolderText)
        {
            GameObject mainObj = CreateUIObject(name, parent);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Sliced;
            mainImage.color = new Color(0, 0, 0, 0.5f);

            InputField inputField = mainObj.AddComponent<InputField>();
            Navigation nav = inputField.navigation;
            nav.mode = Navigation.Mode.None;
            inputField.navigation = nav;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.interactable = true;
            inputField.transition = Selectable.Transition.ColorTint;
            inputField.targetGraphic = mainImage;

            RuntimeHelper.Instance.Internal_SetColorBlock(inputField, new Color(1, 1, 1, 1),
                new Color(0.95f, 0.95f, 0.95f, 1.0f), new Color(0.78f, 0.78f, 0.78f, 1.0f));

            GameObject textArea = CreateUIObject("TextArea", mainObj);
            textArea.AddComponent<RectMask2D>();

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            GameObject placeHolderObj = CreateUIObject("Placeholder", textArea);
            Text placeholderText = placeHolderObj.AddComponent<Text>();
            SetDefaultTextValues(placeholderText);
            placeholderText.text = placeHolderText ?? "...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.fontSize = 14;

            RectTransform placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
            placeHolderRect.anchorMax = Vector2.one;
            placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

            inputField.placeholder = placeholderText;

            GameObject inputTextObj = CreateUIObject("Text", textArea);
            Text inputText = inputTextObj.AddComponent<Text>();
            SetDefaultTextValues(inputText);
            inputText.text = "";
            inputText.color = new Color(1f, 1f, 1f, 1f);
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.fontSize = 14;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            inputField.textComponent = inputText;
            inputField.characterLimit = UniversalUI.MAX_INPUTFIELD_CHARS;

            return new InputFieldRef(inputField);
        }

        /// <summary>
        /// Create a standard DropDown control.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your Dropdown</param>
        /// <param name="dropdown">Returns your created Dropdown component</param>
        /// <param name="defaultItemText">The default displayed text (suggested is 14)</param>
        /// <param name="itemFontSize">The font size of the displayed text</param>
        /// <param name="onValueChanged">Invoked when your Dropdown value is changed</param>
        /// <param name="defaultOptions">Optional default options for the dropdown</param>
        /// <returns>The root GameObject for your Dropdown control</returns>
        public static GameObject CreateDropdown(GameObject parent, string name, out Dropdown dropdown, string defaultItemText, int itemFontSize,
            Action<int> onValueChanged, string[] defaultOptions = null)
        {
            GameObject dropdownObj = CreateUIObject(name, parent, largeElementSize);

            GameObject labelObj = CreateUIObject("Label", dropdownObj);
            GameObject arrowObj = CreateUIObject("Arrow", dropdownObj);
            GameObject templateObj = CreateUIObject("Template", dropdownObj);
            GameObject viewportObj = CreateUIObject("Viewport", templateObj);
            GameObject contentObj = CreateUIObject("Content", viewportObj);
            GameObject itemObj = CreateUIObject("Item", contentObj);
            GameObject itemBgObj = CreateUIObject("Item Background", itemObj);
            GameObject itemCheckObj = CreateUIObject("Item Checkmark", itemObj);
            GameObject itemLabelObj = CreateUIObject("Item Label", itemObj);

            GameObject scrollbarObj = CreateScrollbar(templateObj, "DropdownScroll", out Scrollbar scrollbar);
            scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);
            RuntimeHelper.Instance.Internal_SetColorBlock(scrollbar, new Color(0.45f, 0.45f, 0.45f), new Color(0.6f, 0.6f, 0.6f), new Color(0.4f, 0.4f, 0.4f));

            RectTransform scrollRectTransform = scrollbarObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.right;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = Vector2.one;
            scrollRectTransform.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x, 0f);

            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            SetDefaultTextValues(itemLabelText);
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.text = defaultItemText;
            itemLabelText.fontSize = itemFontSize;

            Text arrowText = arrowObj.AddComponent<Text>();
            SetDefaultTextValues(arrowText);
            arrowText.text = "▼";
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 20f);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);

            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.25f, 0.35f, 0.25f, 1.0f);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.isOn = true;
            RuntimeHelper.Instance.Internal_SetColorBlock(itemToggle,
                new Color(0.35f, 0.35f, 0.35f, 1.0f), new Color(0.25f, 0.55f, 0.25f, 1.0f));

            itemToggle.onValueChanged.AddListener((bool val) => { itemToggle.OnDeselect(null); });
            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.type = Image.Type.Sliced;
            templateImage.color = Color.black;

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.scrollSensitivity = 35;
            scrollRect.content = contentObj.GetComponent<RectTransform>();
            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3f;

            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.type = Image.Type.Sliced;

            Text labelText = labelObj.AddComponent<Text>();
            SetDefaultTextValues(labelText);
            labelText.alignment = TextAnchor.MiddleLeft;

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.04f, 0.04f, 0.04f, 0.75f);
            dropdownImage.type = Image.Type.Sliced;

            dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            //itemLabelText.text = "DEFAULT";

            dropdown.RefreshShownValue();

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-28f, -2f);

            RectTransform templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.sizeDelta = new Vector2(-18f, 0f);
            viewportRect.pivot = new Vector2(0f, 1f);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(0f, 28f);

            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 25f);

            RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;

            RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(20f, 1f);
            itemLabelRect.offsetMax = new Vector2(-10f, -2f);
            templateObj.SetActive(false);

            if (onValueChanged != null)
                dropdown.onValueChanged.AddListener(onValueChanged);

            if (defaultOptions != null)
            {
                foreach (string option in defaultOptions)
                    dropdown.options.Add(new Dropdown.OptionData(option));
            }

            return dropdownObj;
        }


        #endregion


        #region Custom Scroll Components

        /// <summary>
        /// Create a ScrollPool for the <typeparamref name="T"/> ICell. You should call scrollPool.Initialize(handler) after this.
        /// </summary>
        /// <typeparam name="T">The ICell type which will be used for the ScrollPool.</typeparam>
        /// <param name="parent">The parent GameObject which the ScrollPool will be built on to.</param>
        /// <param name="name">The GameObject name for your ScrollPool</param>
        /// <param name="uiRoot">Returns the root GameObject for your ScrollPool</param>
        /// <param name="content">Returns the content GameObject for your ScrollPool (where cells will be populated)</param>
        /// <param name="bgColor">The background color for your ScrollPool. If default, it will be dark grey.</param>
        /// <returns>Your created ScrollPool instance.</returns>
        public static ScrollPool<T> CreateScrollPool<T>(GameObject parent, string name, out GameObject uiRoot,
            out GameObject content, Color? bgColor = null) where T : ICell
        {
            GameObject mainObj = CreateUIObject(name, parent, new Vector2(1, 1));
            mainObj.AddComponent<Image>().color = bgColor ?? new Color(0.12f, 0.12f, 0.12f);
            SetLayoutGroup<HorizontalLayoutGroup>(mainObj, false, true, true, true);
            SetLayoutElement(mainObj, flexibleHeight: 9999, flexibleWidth: 9999);

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            SetLayoutElement(viewportObj, flexibleWidth: 9999, flexibleHeight: 9999);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportRect.sizeDelta = new Vector2(0f, 0.0f);
            viewportRect.offsetMax = new Vector2(-10.0f, 0.0f);
            viewportObj.AddComponent<RectMask2D>();
            viewportObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);
            viewportObj.AddComponent<Mask>();

            content = CreateUIObject("Content", viewportObj);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);
            SetLayoutGroup<VerticalLayoutGroup>(content, true, false, true, true, 0, 2, 2, 2, 2, TextAnchor.UpperCenter);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            //scrollRect.inertia = false;
            scrollRect.inertia = true;
            scrollRect.elasticity = 0.125f;
            scrollRect.scrollSensitivity = 25;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Slider

            GameObject sliderContainer = CreateVerticalGroup(mainObj, "SliderContainer",
                false, false, true, true, 0, default, new Color(0.05f, 0.05f, 0.05f));
            SetLayoutElement(sliderContainer, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            sliderContainer.AddComponent<Mask>().showMaskGraphic = false;

            CreateSliderScrollbar(sliderContainer, out Slider slider);

            RuntimeHelper.Instance.Internal_SetColorBlock(slider, disabled: new Color(0.1f, 0.1f, 0.1f));

            // finalize and create ScrollPool

            uiRoot = mainObj;
            ScrollPool<T> scrollPool = new(scrollRect);

            return scrollPool;
        }

        /// <summary>
        /// Create a SliderScrollbar, using a Slider to mimic a Scrollbar. This fixes several issues with Unity's Scrollbar implementation.<br/><br/>
        /// 
        /// Note that this will not have any actual functionality. Use this along with an <see cref="AutoSliderScrollbar"/> to automate the functionality.
        /// </summary>
        /// <param name="parent">The parent to create on to.</param>
        /// <param name="slider">Your created Slider component</param>
        /// <returns>The root GameObject for your SliderScrollbar.</returns>
        public static GameObject CreateSliderScrollbar(GameObject parent, out Slider slider)
        {
            GameObject mainObj = CreateUIObject("SliderScrollbar", parent, smallElementSize);
            mainObj.AddComponent<Mask>().showMaskGraphic = false;
            mainObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);

            GameObject bgImageObj = CreateUIObject("Background", mainObj);
            GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", mainObj);
            GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

            Image bgImage = bgImageObj.AddComponent<Image>();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1.0f);

            bgImageObj.AddComponent<Mask>();

            RectTransform bgRect = bgImageObj.GetComponent<RectTransform>();
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.offsetMax = new Vector2(0f, 0f);

            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideRect.anchorMin = Vector3.zero;
            handleSlideRect.anchorMax = Vector3.one;
            handleSlideRect.pivot = new Vector3(0.5f, 0.5f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            SetLayoutElement(handleObj, minWidth: 21, flexibleWidth: 0);

            LayoutElement sliderBarLayout = mainObj.AddComponent<LayoutElement>();
            sliderBarLayout.minWidth = 25;
            sliderBarLayout.flexibleWidth = 0;
            sliderBarLayout.minHeight = 30;
            sliderBarLayout.flexibleHeight = 9999;

            slider = mainObj.AddComponent<Slider>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.TopToBottom;

            SetLayoutElement(mainObj, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);

            RuntimeHelper.Instance.Internal_SetColorBlock(slider,
                new Color(0.4f, 0.4f, 0.4f),
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.3f, 0.3f, 0.3f),
                new Color(0.5f, 0.5f, 0.5f));

            return mainObj;
        }

        /// <summary>
        /// Create a ScrollView and a SliderScrollbar for non-pooled content.
        /// </summary>
        /// <param name="parent">The parent GameObject to build on to.</param>
        /// <param name="name">The GameObject name for your ScrollView.</param>
        /// <param name="content">The GameObject for your content to be placed on.</param>
        /// <param name="autoScrollbar">A created AutoSliderScrollbar instance for your ScrollView.</param>
        /// <param name="color">The background color, defaults to grey.</param>
        /// <returns>The root GameObject for your ScrollView.</returns>
        public static GameObject CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar autoScrollbar,
            Color color = default)
        {
            GameObject mainObj = CreateUIObject(name, parent);
            RectTransform mainRect = mainObj.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Filled;
            mainImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

            SetLayoutElement(mainObj, flexibleHeight: 9999, flexibleWidth: 9999);

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportRect.offsetMax = new Vector2(-28, 0);
            viewportObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            content = CreateUIObject("Content", viewportObj);
            SetLayoutGroup<VerticalLayoutGroup>(content, true, false, true, true, childAlignment: TextAnchor.UpperLeft);
            SetLayoutElement(content, flexibleHeight: 9999);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.0f, 1.0f);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Slider

            GameObject scrollBarObj = CreateUIObject("AutoSliderScrollbar", mainObj);
            RectTransform scrollBarRect = scrollBarObj.GetComponent<RectTransform>();
            scrollBarRect.anchorMin = new Vector2(1, 0);
            scrollBarRect.anchorMax = Vector2.one;
            scrollBarRect.offsetMin = new Vector2(-25, 0);
            SetLayoutGroup<VerticalLayoutGroup>(scrollBarObj, false, true, true, true);
            scrollBarObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);
            scrollBarObj.AddComponent<Mask>().showMaskGraphic = false;

            GameObject hiddenBar = CreateScrollbar(scrollBarObj, "HiddenScrollviewScroller", out Scrollbar hiddenScrollbar);
            hiddenScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                Transform child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            autoScrollbar = new AutoSliderScrollbar(hiddenScrollbar, scrollSlider, contentRect, viewportRect);

            // Set up the ScrollRect component

            ScrollRect scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = hiddenScrollbar;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return mainObj;
        }

        /// <summary>
        /// Create a Scrollable Input Field control, using an AutoSliderScrollbar for the scroll bar.
        /// </summary>
        /// <param name="parent">The parent GameObject to build on to.</param>
        /// <param name="name">The GameObject name for your InputField.</param>
        /// <param name="placeHolderText">Optional placeholder text for your InputField</param>
        /// <param name="inputScroll">An InputFieldScroller, you don't need to do anything with this necessarily, it will automate itself.</param>
        /// <param name="fontSize">The font size for your InputField</param>
        /// <param name="color">The text color for your InputField</param>
        /// <returns>The root GameObject for your ScrollInputField</returns>
        public static GameObject CreateScrollInputField(GameObject parent, string name, string placeHolderText, out InputFieldScroller inputScroll,
            int fontSize = 14, Color color = default)
        {
            if (color == default)
                color = new Color(0.12f, 0.12f, 0.12f);

            GameObject mainObj = CreateUIObject(name, parent);
            SetLayoutElement(mainObj, minWidth: 100, minHeight: 30, flexibleWidth: 5000, flexibleHeight: 5000);
            SetLayoutGroup<HorizontalLayoutGroup>(mainObj, false, true, true, true, 2);
            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Filled;
            mainImage.color = (color == default) ? new Color(0.3f, 0.3f, 0.3f, 1f) : color;

            GameObject viewportObj = CreateUIObject("Viewport", mainObj);
            SetLayoutElement(viewportObj, minWidth: 1, flexibleWidth: 9999, flexibleHeight: 9999);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.0f, 1.0f);
            viewportObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            // Input Field

            InputFieldRef inputField = CreateInputField(viewportObj, "InputField", placeHolderText);
            GameObject content = inputField.UIRoot;
            Text textComp = inputField.Component.textComponent;
            textComp.alignment = TextAnchor.UpperLeft;
            textComp.fontSize = fontSize;
            textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.Component.targetGraphic.color = color;
            inputField.PlaceholderText.alignment = TextAnchor.UpperLeft;
            inputField.PlaceholderText.fontSize = fontSize;
            inputField.PlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;

            //var content = CreateInputField(viewportObj, name, placeHolderText ?? "...", out InputField inputField, fontSize, 0);
            SetLayoutElement(content, flexibleHeight: 9999, flexibleWidth: 9999);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(2, 0);
            contentRect.offsetMax = new Vector2(2, 0);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.Component.targetGraphic.color = color;

            // Slider

            GameObject scrollBarObj = CreateUIObject("AutoSliderScrollbar", mainObj);
            SetLayoutGroup<VerticalLayoutGroup>(scrollBarObj, true, true, true, true);
            SetLayoutElement(scrollBarObj, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            scrollBarObj.AddComponent<Image>().color = new(0.1f, 0.1f, 0.1f, 1);
            scrollBarObj.AddComponent<Mask>().showMaskGraphic = false;

            GameObject hiddenBar = CreateScrollbar(scrollBarObj, "HiddenScrollviewScroller", out Scrollbar hiddenScrollbar);
            hiddenScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            for (int i = 0; i < hiddenBar.transform.childCount; i++)
            {
                Transform child = hiddenBar.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            CreateSliderScrollbar(scrollBarObj, out Slider scrollSlider);

            // Set up the AutoSliderScrollbar module

            AutoSliderScrollbar autoScroller = new(hiddenScrollbar, scrollSlider, contentRect, viewportRect);

            GameObject sliderContainer = autoScroller.Slider.m_HandleContainerRect.gameObject;
            SetLayoutElement(sliderContainer, minWidth: 25, flexibleWidth: 0, flexibleHeight: 9999);
            //sliderContainer.AddComponent<Mask>().showMaskGraphic = false;

            // Set up the InputFieldScroller module

            inputScroll = new InputFieldScroller(autoScroller, inputField);
            inputScroll.ProcessInputText();

            // Set up the ScrollRect component

            ScrollRect scrollRect = mainObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = hiddenScrollbar;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;


            return mainObj;
        }

        #endregion
    }
}
