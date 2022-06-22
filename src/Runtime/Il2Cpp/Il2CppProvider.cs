#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UniverseLib.Input;
using UnityEngine.EventSystems;
using HarmonyLib;
using UniverseLib.Utility;
#if INTEROP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#else
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
#endif

namespace UniverseLib.Runtime.Il2Cpp
{
    internal class Il2CppProvider : RuntimeHelper
    {
        readonly AmbiguousMemberHandler<ColorBlock, Color> normalColor = new(true, true, "normalColor", "m_NormalColor");
        readonly AmbiguousMemberHandler<ColorBlock, Color> highlightedColor = new(true, true, "highlightedColor", "m_HighlightedColor");
        readonly AmbiguousMemberHandler<ColorBlock, Color> pressedColor = new(true, true, "pressedColor", "m_PressedColor");
        readonly AmbiguousMemberHandler<ColorBlock, Color> disabledColor = new(true, true, "disabledColor", "m_DisabledColor");

        internal delegate IntPtr d_LayerToName(int layer);

        internal delegate IntPtr d_FindObjectsOfTypeAll(IntPtr type);

        internal delegate void d_GetRootGameObjects(int handle, IntPtr list);

        internal delegate int d_GetRootCountInternal(int handle);

        protected internal override void OnInitialize()
        {
            new Il2CppTextureHelper();
        }

        /// <inheritdoc/>
        protected internal override Coroutine Internal_StartCoroutine(IEnumerator routine)
            => UniversalBehaviour.Instance.StartCoroutine(routine.WrapToIl2Cpp());

        /// <inheritdoc/>
        protected internal override void Internal_StopCoroutine(Coroutine coroutine)
            => UniversalBehaviour.Instance.StopCoroutine(coroutine);

        /// <inheritdoc/>
        protected internal override T Internal_AddComponent<T>(GameObject obj, Type type)
            => obj.AddComponent(Il2CppType.From(type)).TryCast<T>();

        /// <inheritdoc/>
        protected internal override ScriptableObject Internal_CreateScriptable(Type type)
            => ScriptableObject.CreateInstance(Il2CppType.From(type));

        /// <inheritdoc/>
        protected internal override void Internal_GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
        {
            Il2CppSystem.Collections.Generic.List<RaycastResult> il2cppList = new();

            raycaster.Raycast(data, il2cppList);

            if (il2cppList.Count > 0)
                list.AddRange(il2cppList.ToArray());
        }

        /// <inheritdoc/>
        protected internal override string Internal_LayerToName(int layer)
        {
            d_LayerToName iCall = ICallManager.GetICall<d_LayerToName>("UnityEngine.LayerMask::LayerToName");
            return IL2CPP.Il2CppStringToManaged(iCall.Invoke(layer));
        }

        /// <inheritdoc/>
        protected internal override UnityEngine.Object[] Internal_FindObjectsOfTypeAll(Type type)
        {
            return new Il2CppReferenceArray<UnityEngine.Object>(
                    ICallManager.GetICallUnreliable<d_FindObjectsOfTypeAll>(
                        "UnityEngine.Resources::FindObjectsOfTypeAll",
                        "UnityEngine.ResourcesAPIInternal::FindObjectsOfTypeAll") // Unity 2020+ updated to this
                    .Invoke(Il2CppType.From(type).Pointer));
        }

        /// <inheritdoc/>
        protected internal override T[] Internal_FindObjectsOfTypeAll<T>()
        {
            return new Il2CppReferenceArray<T>(
                    ICallManager.GetICallUnreliable<d_FindObjectsOfTypeAll>(
                        "UnityEngine.Resources::FindObjectsOfTypeAll",
                        "UnityEngine.ResourcesAPIInternal::FindObjectsOfTypeAll") // Unity 2020+ updated to this
                    .Invoke(Il2CppType.From(typeof(T)).Pointer));
        }

        /// <inheritdoc/>
        protected internal override GameObject[] Internal_GetRootGameObjects(Scene scene)
        {
            if (!scene.isLoaded || scene.handle == -1)
                return new GameObject[0];

            int count = GetRootCount(scene.handle);
            if (count < 1)
                return new GameObject[0];

            Il2CppSystem.Collections.Generic.List<GameObject> list = new(count);
            ICallManager.GetICall<d_GetRootGameObjects>("UnityEngine.SceneManagement.Scene::GetRootGameObjectsInternal")
                .Invoke(scene.handle, list.Pointer);
            return list.ToArray();
        }

        /// <inheritdoc/>
        protected internal override int Internal_GetRootCount(Scene scene) => GetRootCount(scene.handle);

        /// <summary>
        /// Gets the <see cref="Scene.rootCount"/> for the provided scene handle.
        /// </summary>
        protected internal static int GetRootCount(int sceneHandle)
        {
            return ICallManager.GetICall<d_GetRootCountInternal>("UnityEngine.SceneManagement.Scene::GetRootCountInternal")
                   .Invoke(sceneHandle);
        }

        /// <inheritdoc/>
        protected internal override void Internal_SetColorBlock(Selectable selectable, ColorBlock colorBlock)
        {
            try
            {
                AccessTools.Property(typeof(Selectable), "m_Colors")
                    .SetValue(selectable, colorBlock, null);

                AccessTools.Method(typeof(Selectable), "OnSetProperty")
                    .Invoke(selectable, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                Universe.LogWarning(ex);
            }
        }

        /// <inheritdoc/>
        protected internal override void Internal_SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null, Color? disabled = null)
        {
            ColorBlock colors = selectable.colors;
            colors.colorMultiplier = 1;

            object boxedColors = colors;

            if (normal != null)
                normalColor.SetValue(boxedColors, (Color)normal);

            if (highlighted != null)
                highlightedColor.SetValue(boxedColors, (Color)highlighted);

            if (pressed != null)
                pressedColor.SetValue(boxedColors, (Color)pressed);

            if (disabled != null)
                disabledColor.SetValue(boxedColors, (Color)disabled);

            SetColorBlock(selectable, (ColorBlock)boxedColors);
        }
    }
}

namespace UniverseLib
{
    public static class Il2CppExtensions
    {
        public static void AddListener(this UnityEvent action, Action listener)
        {
            action.AddListener(listener);
        }

        public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
        {
            action.AddListener(listener);
        }

        public static void RemoveListener(this UnityEvent action, Action listener)
        {
            action.RemoveListener(listener);
        }

        public static void RemoveListener<T>(this UnityEvent<T> action, Action<T> listener)
        {
            action.RemoveListener(listener);
        }

        public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlHeight = value;
        public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value) => group.childControlWidth = value;
    }
}

#endif