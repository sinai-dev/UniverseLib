using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.Runtime;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UniverseLib.Input
{
    /// <summary>
    /// Handles taking control of the mouse/cursor and EventSystem (depending on Config settings) when a UniversalUI is being used.
    /// </summary>
    public class CursorUnlocker
    {
        /// <summary>
        /// True if a UI is being displayed and <see cref="ConfigManager.Force_Unlock_Mouse"/> is true.
        /// </summary>
        public static bool ShouldUnlock => ConfigManager.Force_Unlock_Mouse && UniversalUI.AnyUIShowing;

        /// <summary>
        /// The value of "EventSystem.current", or "EventSystem.main" in some older games.
        /// </summary>
        public static EventSystem CurrentEventSystem
        {
            get => EventSystemCurrent_Handler.GetValue();
            set => EventSystemCurrent_Handler.SetValue(value);
        }

        static readonly AmbiguousMemberHandler<EventSystem, EventSystem> EventSystemCurrent_Handler = new(true, true, "current", "main");

        private static bool currentlySettingCursor;
        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

        private static WaitForEndOfFrame waitForEndOfFrame = new();

        private static bool settingEventSystem;
        private static EventSystem lastEventSystem;
        private static BaseInputModule lastInputModule;

        internal static void Init()
        {
            lastLockMode = Cursor.lockState;
            lastVisibleState = Cursor.visible;

            SetupPatches();
            UpdateCursorControl();

            try
            {
                RuntimeHelper.Instance.Internal_StartCoroutine(UnlockCoroutine());
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception setting up Aggressive Mouse Unlock: {ex}");
            }
        }

        /// <summary>
        /// Uses WaitForEndOfFrame in a Coroutine to aggressively set the Cursor state every frame.
        /// </summary>
        private static IEnumerator UnlockCoroutine()
        {
            while (true)
            {
                yield return waitForEndOfFrame ??= new WaitForEndOfFrame();
                if (UniversalUI.AnyUIShowing)
                    UpdateCursorControl();
            }
        }

        /// <summary>
        /// Checks current ShouldUnlock state and sets the Cursor and EventSystem as required.
        /// </summary>
        internal static void UpdateCursorControl()
        {
            try
            {
                currentlySettingCursor = true;

                if (ShouldUnlock)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    if (!ConfigManager.Disable_EventSystem_Override)
                        EnableEventSystem();
                }
                else
                {
                    Cursor.lockState = lastLockMode;
                    Cursor.visible = lastVisibleState;

                    if (!ConfigManager.Disable_EventSystem_Override)
                        ReleaseEventSystem();
                }

                currentlySettingCursor = false;
            }
            catch (Exception e)
            {
                Universe.Log($"Exception setting Cursor state: {e.GetType()}, {e.Message}");
            }
        }

        static float timeOfLastEventSystemSearch;

        /// <summary>
        /// If the UniverseLib EventSystem is not enabled, this enables it and sets EventSystem.current to it, and stores the previous EventSystem.
        /// </summary>
        internal static void EnableEventSystem()
        {
            if (!UniversalUI.EventSys)
                return;

            EventSystem current = CurrentEventSystem;
            if (current && !current.ReferenceEqual(UniversalUI.EventSys) && current.isActiveAndEnabled)
            {
                lastEventSystem = current;
                lastInputModule = current.currentInputModule;
                lastEventSystem.enabled = false;
            }
            // In some cases we may need to set our own EventSystem active before the original EventSystem is created or enabled.
            // For that we will need to use Resources to find the other active EventSystem once it has been created.
            if (!ConfigManager.Disable_Fallback_EventSystem_Search 
                && !lastEventSystem
                && Time.realtimeSinceStartup - timeOfLastEventSystemSearch > 10f)
            {
                timeOfLastEventSystemSearch = Time.realtimeSinceStartup;
                // Universe.Log("No previous EventSystem detected, doing expensive manual search...");
                UnityEngine.Object[] allSystems = RuntimeHelper.FindObjectsOfTypeAll<EventSystem>();
                foreach (UnityEngine.Object obj in allSystems)
                {
                    EventSystem system = obj.TryCast<EventSystem>();
                    if (system.ReferenceEqual(UniversalUI.EventSys))
                        continue;
                    if (system.isActiveAndEnabled)
                    {
                        lastEventSystem = system;
                        lastInputModule = system.currentInputModule;
                        lastEventSystem.enabled = false;
                        break;
                    }
                }
            }

            if (!UniversalUI.EventSys.enabled)
            {
                // Set to our current system
                settingEventSystem = true;
                UniversalUI.EventSys.enabled = true;
                CurrentEventSystem = UniversalUI.EventSys;
                InputManager.ActivateUIModule();
                settingEventSystem = false;
            }

            CheckVRChatEventSystemFix();
        }

        /// <summary>
        /// If the UniverseLib EventSystem is enabled, this disables it and sets EventSystem.current to the previous EventSystem which was enabled.
        /// </summary>
        internal static void ReleaseEventSystem()
        {
            if (!UniversalUI.EventSys)
                return;

            CheckVRChatEventSystemFix();

            if (UniversalUI.EventSys.enabled)
            {
                settingEventSystem = true;

                UniversalUI.EventSys.enabled = false;
                UniversalUI.EventSys.currentInputModule?.DeactivateModule();

                if (lastEventSystem && lastEventSystem.gameObject.activeSelf)
                {
                    CurrentEventSystem = lastEventSystem;
                    lastEventSystem.enabled = true;
                    lastInputModule?.ActivateModule();
                }

                settingEventSystem = false;
            }
        }

        // Dirty fix for some VRChat weirdness
        static void CheckVRChatEventSystemFix()
        {
            try
            {
                if (Application.productName == "VRChat")
                {
                    if (GameObject.Find("EventSystem") is not GameObject strayEventSystem)
                        return;

                    // Try to make sure it's the right object I guess

                    int count = strayEventSystem.GetComponents<Component>().Length;
                    if (count != 3 && count != 4)
                        return;

                    if (strayEventSystem.transform.childCount > 0)
                        return;

                    Universe.LogWarning("Disabling extra VRChat EventSystem");
                    strayEventSystem.SetActive(false);
                }
            }
            catch { }
        }

        // Patches

        internal static void SetupPatches()
        {
            try
            {
                PrefixPropertySetter(typeof(Cursor),
                    "lockState",
                    new HarmonyMethod(AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_set_lockState))));

                PrefixPropertySetter(typeof(Cursor),
                    "visible",
                    new HarmonyMethod(AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_set_visible))));

                PrefixPropertySetter(typeof(EventSystem), 
                    "current", 
                    new HarmonyMethod(AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_EventSystem_set_current))),
                    // some games use "EventSystem.main"
                    "main");

                PrefixMethod(typeof(EventSystem),
                    "SetSelectedGameObject",
                    // some games use a modified version of uGUI that includes this extra int argument on this method.
                    new Type[] { typeof(GameObject), typeof(BaseEventData), typeof(int) },
                    new HarmonyMethod(AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_EventSystem_SetSelectedGameObject))),
                    // most games use these arguments, we'll use them as our "backup".
                    new Type[] { typeof(GameObject), typeof(BaseEventData) });
            }
            catch (Exception ex)
            {
                Universe.Log($"Exception setting up Harmony patches:\r\n{ex.ReflectionExToString()}");
            }
        }

        private static void PrefixMethod(Type type, string method, Type[] arguments, HarmonyMethod prefix, Type[] backupArgs = null)
        {
            try
            {
                MethodInfo methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, arguments, null);
                if (methodInfo == null)
                {
                    if (backupArgs != null)
                        methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, backupArgs, null);
                    
                    if (methodInfo == null)
                        throw new MissingMethodException($"Could not find method for patching - '{type.FullName}.{method}'!");
                }

                PatchProcessor processor = Universe.Harmony.CreateProcessor(methodInfo);
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                Universe.LogWarning($"Unable to patch {type.Name}.{method}: {e.Message}");
            }
        }

        private static void PrefixPropertySetter(Type type, string property, HarmonyMethod prefix, string backupName = null)
        {
            try
            {
                MethodInfo propInfo = type.GetProperty(property, ReflectionUtility.FLAGS)?.GetSetMethod();

                if (propInfo == null && !string.IsNullOrEmpty(backupName))
                    propInfo = type.GetProperty(backupName, ReflectionUtility.FLAGS).GetSetMethod();

                if (propInfo == null)
                    throw new MissingMethodException($"Could not find property {type.FullName}.{property}{(!string.IsNullOrEmpty(backupName) ? $" or {backupName}" : string.Empty)}!");

                PatchProcessor processor = Universe.Harmony.CreateProcessor(propInfo);
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                Universe.Log($"Unable to patch {type.Name}.set_{property}: {e.Message}");
            }
        }

        // Prevent setting non-UniverseLib objects as selected when a menu is open

        internal static bool Prefix_EventSystem_SetSelectedGameObject(GameObject __0)
        {
            if (ConfigManager.Allow_UI_Selection_Outside_UIBase || !UniversalUI.AnyUIShowing || !UniversalUI.CanvasRoot)
                return true;

            return __0 && __0.transform.root.gameObject.GetInstanceID() == UniversalUI.CanvasRoot.GetInstanceID();
        }

        // Force EventSystem.current to be UniverseLib's when menu is open

        internal static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!settingEventSystem && value && !value.ReferenceEqual(UniversalUI.EventSys))
            {
                lastEventSystem = value;
                lastInputModule = value.currentInputModule;
            }

            if (!UniversalUI.EventSys)
                return;

            if (!settingEventSystem && ShouldUnlock && !ConfigManager.Disable_EventSystem_Override)
            {
                value = UniversalUI.EventSys;
                value.enabled = true;
            }
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        internal static void Prefix_set_lockState(ref CursorLockMode value)
        {
            if (!currentlySettingCursor)
            {
                lastLockMode = value;

                if (ShouldUnlock)
                    value = CursorLockMode.None;
            }
        }

        internal static void Prefix_set_visible(ref bool value)
        {
            if (!currentlySettingCursor)
            {
                lastVisibleState = value;

                if (ShouldUnlock)
                    value = true; 
            }
        }
    }
}