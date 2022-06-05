using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;
using UniverseLib.Utility;

namespace UniverseLib.UI
{
    /// <summary>Handles all <see cref="UIBase"/> UIs on the UniverseLib UI canvas.</summary>
    public static class UniversalUI
    {
        internal static readonly Dictionary<string, UIBase> registeredUIs = new();
        internal static readonly List<UIBase> uiBases = new();

        /// <summary>Returns true if UniverseLib is currently initializing it's UI.</summary>
        public static bool Initializing { get; internal set; } = true;

        /// <summary>Returns true if any <see cref="UIBase"/> is being displayed.</summary>
        public static bool AnyUIShowing => registeredUIs.Any(it => it.Value.Enabled);

        /// <summary>The UniverseLib global Canvas root.</summary>
        public static GameObject CanvasRoot { get; private set; }

        /// <summary>The UniverseLib global EventSystem.</summary>
        public static EventSystem EventSys { get; private set; }

        /// <summary>The GameObject used to hold returned <see cref="UI.ObjectPool.IPooledObject"/> objects.</summary>
        public static GameObject PoolHolder { get; private set; }

        /// <summary>The Consola font asset, if it was successfully loaded.</summary>
        public static Font ConsoleFont { get; private set; }

        /// <summary>The default font asset.</summary>
        public static Font DefaultFont { get; private set; }

        /// <summary>The backup UI shader, if it was loaded.</summary>
        public static Shader BackupShader { get; private set; }

        /// <summary>The default color used by UniverseLib for enabled buttons.</summary>
        public static Color EnabledButtonColor { get; } = new(0.2f, 0.4f, 0.28f);

        /// <summary>The default color used by UniverseLib for disabled buttons.</summary>
        public static Color DisabledButtonColor { get; } = new(0.25f, 0.25f, 0.25f);

        /// <summary>A safe value for the maximum amount of characters allowed in an InputField.</summary>
        public const int MAX_INPUTFIELD_CHARS = 16000;

        /// <summary>The maximum amount of vertices allowed in an InputField's UI mesh.</summary>
        public const int MAX_TEXT_VERTS = 65000;

        /// <summary>
        /// Create and register a <see cref="UIBase"/> with the provided ID, and optional update method.
        /// </summary>
        /// <param name="id">A unique ID for your UI.</param>
        /// <param name="updateMethod">An optional method to receive Update calls with, invoked when your UI is displayed.</param>
        /// <returns>Your newly created <see cref="UIBase"/>, if successful.</returns>
        public static UIBase RegisterUI(string id, Action updateMethod)
        {
            return new(id, updateMethod);
        }

        /// <summary>
        /// Create and register a <typeparamref name="T"/> with the provided ID, and optional update method.<br />
        /// You can use this to register a custom <see cref="UIBase"/> type instead of the default type.
        /// </summary>
        /// <param name="id">A unique ID for your UI.</param>
        /// <param name="updateMethod">An optional method to receive Update calls with, invoked when your UI is displayed.</param>
        /// <returns>Your newly created <typeparamref name="T"/>, if successful.</returns>
        public static T RegisterUI<T>(string id, Action updateMethod) where T : UIBase
        {
            return (T)Activator.CreateInstance(typeof(T), new object[] { id, updateMethod });
        }

        /// <summary>
        /// Sets the <see cref="UIBase"/> with the corresponding <paramref name="id"/> to be active or disabled.
        /// </summary>
        public static void SetUIActive(string id, bool active)
        {
            if (registeredUIs.TryGetValue(id, out UIBase uiBase))
            {
                uiBase.RootObject.SetActive(active);
                if (active)
                    uiBase.SetOnTop();
                else if (uiBase != PanelManager.resizeCursorUIBase)
                    PanelManager.ForceEndResize();

                CursorUnlocker.UpdateCursorControl();
                return;
            }
            throw new ArgumentException($"There is no UI registered with the id '{id}'");
        }

        // Initialization

        internal static void Init()
        {
            LoadBundle();

            CreateRootCanvas();

            // Global UI Pool Holder
            PoolHolder = new GameObject("PoolHolder");
            PoolHolder.transform.parent = CanvasRoot.transform;
            PoolHolder.SetActive(false);

            Initializing = false;
        }

        // Main UI Update loop

        internal static void Update()
        {
            if (!CanvasRoot || Initializing)
                return;

            if (!AnyUIShowing)
                return;

            // Prevent click-through
            if (EventSys.IsPointerOverGameObject())
            {
                if (InputManager.MouseScrollDelta.y != 0 
                    || InputManager.GetMouseButtonUp(0) 
                    || InputManager.GetMouseButtonUp(1))
                {
                    InputManager.ResetInputAxes();
                }
            }

            InputManager.Update();

            InputFieldRef.UpdateInstances();
            UIBehaviourModel.UpdateInstances();

            // Update registered UIs
            PanelManager.focusHandledThisFrame = false;
            PanelManager.draggerHandledThisFrame = false;

            for (int i = 0; i < uiBases.Count; i++)
            {
                UIBase ui = uiBases[i];
                if (ui.Enabled)
                    ui.Update();
            }
        }

        // UI Construction

        private static void CreateRootCanvas()
        {
            CanvasRoot = new GameObject("UniverseLibCanvas");
            UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
            CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
            CanvasRoot.layer = 5;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            CanvasRoot.SetActive(false);

            EventSys = CanvasRoot.AddComponent<EventSystem>();
            EventSystemHelper.AddUIModule();
            EventSys.enabled = false;

            CanvasRoot.SetActive(true);
        }

        // UI AssetBundle

        internal static AssetBundle UIBundle;

        private static void LoadBundle()
        {
            SetupAssetBundlePatches();

            try
            {
                // Get the Major and Minor of the Unity version
                string[] split = Application.unityVersion.Split('.');
                int major = int.Parse(split[0]);
                int minor = int.Parse(split[1]);
                int patch = int.Parse(split[2][0].ToString()); // get first digit of patch, thats all we need.

                // Use appropriate AssetBundle for Unity version
                // 2017 or newer
                if (major > 5)
                    UIBundle = LoadBundle("modern");
                // 5.6.X
                else if (major == 5 && minor >= 6)
                    UIBundle = LoadBundle("legacy.5.6");
                // 5.3.4 to 5.5.X
                else if (major == 5 && ((minor == 3 && patch >= 4) || minor > 3))
                    UIBundle = LoadBundle("legacy.5.3.4");
                // 5.2.5 to <5.3.4
                else
                    UIBundle = LoadBundle("legacy");
            }
            catch
            {
                Universe.LogWarning($"Exception parsing Unity version, falling back to old AssetBundle load method...");
                UIBundle = LoadBundle("modern") 
                    ?? LoadBundle("legacy.5.6")
                    ?? LoadBundle("legacy.5.3.4")
                    ?? LoadBundle("legacy");
            }

            if (UIBundle == null)
            {
                Universe.LogWarning("Could not load the UniverseLib UI Bundle!");
                DefaultFont = ConsoleFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return;
            }

            // Bundle loaded

            ConsoleFont = UIBundle.LoadAsset<Font>("CONSOLA");
            ConsoleFont.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(ConsoleFont);

            DefaultFont = UIBundle.LoadAsset<Font>("arial");
            DefaultFont.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(DefaultFont);

            BackupShader = UIBundle.LoadAsset<Shader>("DefaultUI");
            BackupShader.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(BackupShader);
            // Fix for games which don't ship with 'UI/Default' shader.
            if (Graphic.defaultGraphicMaterial.shader?.name != "UI/Default")
            {
                Universe.Log("This game does not ship with the 'UI/Default' shader, using manual Default Shader...");
                Graphic.defaultGraphicMaterial.shader = BackupShader;
            }
            else
                BackupShader = Graphic.defaultGraphicMaterial.shader;
        }

        static AssetBundle LoadBundle(string id)
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(ReadFully(typeof(Universe)
                    .Assembly
                    .GetManifestResourceStream($"UniverseLib.Resources.{id}.bundle")));
            if (bundle)
                Universe.Log($"Loaded {id} bundle for Unity {Application.unityVersion}");
            return bundle;
        }

        static byte[] ReadFully(Stream input)
        {
            using MemoryStream ms = new();
            byte[] buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                ms.Write(buffer, 0, read);
            return ms.ToArray();
        }

        // AssetBundle patch

        static void SetupAssetBundlePatches()
        {
            Universe.Patch(
                ReflectionUtility.GetTypeByName("UnityEngine.AssetBundle"), 
                "UnloadAllAssetBundles", 
                MethodType.Normal, 
                prefix: AccessTools.Method(typeof(UniversalUI), nameof(Prefix_UnloadAllAssetBundles)));
        }

        static bool Prefix_UnloadAllAssetBundles(bool unloadAllObjects)
        {
            try
            {
                MethodInfo method = typeof(AssetBundle).GetMethod("GetAllLoadedAssetBundles", AccessTools.all);
                if (method == null)
                    return true;
                AssetBundle[] bundles = method.Invoke(null, ArgumentUtility.EmptyArgs) as AssetBundle[];
                foreach (AssetBundle obj in bundles)
                {
                    if (obj.m_CachedPtr == UIBundle.m_CachedPtr)
                        continue;

                    obj.Unload(unloadAllObjects);
                }
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception unloading AssetBundles: {ex}");
            }

            return false;
        }
    }
}
