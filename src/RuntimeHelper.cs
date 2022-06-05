using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniverseLib
{
    /// <summary>
    /// Class to help with some differences between Mono and Il2Cpp Runtimes.
    /// </summary>
    public abstract class RuntimeHelper
    {
        internal static RuntimeHelper Instance { get; private set; }

        internal static void Init()
        {
#if CPP
            Instance = new Runtime.Il2Cpp.Il2CppProvider();
#else
            Instance = new Runtime.Mono.MonoProvider();
#endif
            Instance.OnInitialize();
        }

        protected internal abstract void OnInitialize();

        /// <summary>
        /// Start any <see cref="IEnumerator"/> as a <see cref="Coroutine"/>, handled by UniverseLib's <see cref="MonoBehaviour"/> Instance.
        /// </summary>
        public static Coroutine StartCoroutine(IEnumerator routine)
            => Instance.Internal_StartCoroutine(routine);
        
        protected internal abstract Coroutine Internal_StartCoroutine(IEnumerator routine);

        /// <summary>
        /// Stop a <see cref="Coroutine"/>, which needs to have been started with <see cref="StartCoroutine(IEnumerator)"/>.
        /// </summary>
        /// <param name="coroutine"></param>
        public static void StopCoroutine(Coroutine coroutine)
            => Instance.Internal_StopCoroutine(coroutine);
        
        protected internal abstract void Internal_StopCoroutine(Coroutine coroutine);

        /// <summary>
        /// Helper to add a component of Type <paramref name="type"/>, and return it as Type <typeparamref name="T"/> (provided <typeparamref name="T"/> is assignable from <paramref name="type"/>).
        /// </summary>
        public static T AddComponent<T>(GameObject obj, Type type) where T : Component
            => Instance.Internal_AddComponent<T>(obj, type);

        protected internal abstract T Internal_AddComponent<T>(GameObject obj, Type type) where T : Component;

        /// <summary>
        /// Helper to create an instance of the ScriptableObject of Type <paramref name="type"/>.
        /// </summary>
        public static ScriptableObject CreateScriptable(Type type)
            => Instance.Internal_CreateScriptable(type);
        
        protected internal abstract ScriptableObject Internal_CreateScriptable(Type type);

        /// <summary>
        /// Helper to invoke Unity's <see cref="LayerMask.LayerToName"/> method.
        /// </summary>
        public static string LayerToName(int layer)
            => Instance.Internal_LayerToName(layer);
        
        protected internal abstract string Internal_LayerToName(int layer);

        /// <summary>
        /// Helper to invoke Unity's <see cref="Resources.FindObjectsOfTypeAll"/> method.
        /// </summary>
        public static T[] FindObjectsOfTypeAll<T>() where T : UnityEngine.Object
            => Instance.Internal_FindObjectsOfTypeAll<T>();

        /// <summary>
        /// Helper to invoke Unity's <see cref="Resources.FindObjectsOfTypeAll}"/> method.
        /// </summary>
        public static UnityEngine.Object[] FindObjectsOfTypeAll(Type type)
            => Instance.Internal_FindObjectsOfTypeAll(type);

        protected internal abstract T[] Internal_FindObjectsOfTypeAll<T>() where T : UnityEngine.Object;

        protected internal abstract UnityEngine.Object[] Internal_FindObjectsOfTypeAll(Type type);

        /// <summary>
        /// Helper to invoke Unity's <see cref="GraphicRaycaster.Raycast"/> method.
        /// </summary>
        public static void GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
            => Instance.Internal_GraphicRaycast(raycaster, data, list);

        protected internal abstract void Internal_GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list);

        /// <summary>
        /// Helper to invoke Unity's <see cref="Scene.GetRootGameObjects"/> method.
        /// </summary>
        public static GameObject[] GetRootGameObjects(Scene scene)
            => Instance.Internal_GetRootGameObjects(scene);
        
        protected internal abstract GameObject[] Internal_GetRootGameObjects(Scene scene);

        /// <summary>
        /// Helper to get the value of Unity's <see cref="Scene.rootCount"/> property.
        /// </summary>
        public static int GetRootCount(Scene scene)
            => Instance.Internal_GetRootCount(scene);
        
        protected internal abstract int Internal_GetRootCount(Scene scene);

        /// <summary>
        /// Automatically sets the base, highlighted and pressed values of the <paramref name="selectable"/>'s <see cref="ColorBlock"/>, 
        /// with <paramref name="baseColor"/> * 1.2f for the highlighted color and * 0.8f for the pressed color.
        /// </summary>
        public static void SetColorBlockAuto(Selectable selectable, Color baseColor) 
            => Instance.Internal_SetColorBlock(selectable, baseColor, baseColor * 1.2f, baseColor * 0.8f);

        /// <summary>
        /// Sets the <paramref name="colors"/> to the <paramref name="selectable"/>.
        /// </summary>
        public static void SetColorBlock(Selectable selectable, ColorBlock colors)
            => Instance.Internal_SetColorBlock(selectable, colors);

        protected internal abstract void Internal_SetColorBlock(Selectable selectable, ColorBlock colors);

        /// <summary>
        /// Sets the provided non-<see langword="null"/> colors to the <paramref name="selectable"/>.
        /// </summary>
        public static void SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null, Color? disabled = null)
            => Instance.Internal_SetColorBlock(selectable, normal, highlighted, pressed, disabled);

        protected internal abstract void Internal_SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null, Color? disabled = null);
    }
}
