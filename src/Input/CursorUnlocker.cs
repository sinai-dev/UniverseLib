using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    public class CursorUnlocker
    {
        public static bool ShouldUnlock => ConfigManager.Force_Unlock_Mouse && UniversalUI.AnyUIShowing;

        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

        private static bool currentlySettingCursor = false;

        public static void Init()
        {
            lastLockMode = Cursor.lockState;
            lastVisibleState = Cursor.visible;

            SetupPatches();
            UpdateCursorControl();

            // Aggressive Mouse Unlock
            try
            {
                RuntimeProvider.Instance.StartCoroutine(FailsafeUnlockCoroutine());
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception setting up Aggressive Mouse Unlock: {ex}");
            }
        }

        private static WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        private static IEnumerator FailsafeUnlockCoroutine()
        {
            while (true)
            {
                yield return _waitForEndOfFrame ?? (_waitForEndOfFrame = new WaitForEndOfFrame());
                if (UniversalUI.AnyUIShowing)
                    UpdateCursorControl();
            }
        }

        public static void UpdateCursorControl()
        {
            try
            {
                currentlySettingCursor = true;

                if (ShouldUnlock)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    if (!ConfigManager.Disable_EventSystem_Override && UniversalUI.EventSys)
                        SetEventSystem();
                }
                else
                {
                    Cursor.lockState = lastLockMode;
                    Cursor.visible = lastVisibleState;

                    if (!ConfigManager.Disable_EventSystem_Override && UniversalUI.EventSys)
                        ReleaseEventSystem();
                }

                currentlySettingCursor = false;
            }
            catch (Exception e)
            {
                Universe.Log($"Exception setting Cursor state: {e.GetType()}, {e.Message}");
            }
        }

        // Event system overrides


        private static PropertyInfo EventSystemCurrentInfo
        {
            get => pi_currentEventSystem
                    ?? (pi_currentEventSystem = ReflectionUtility.GetPropertyInfo(typeof(EventSystem), "current"))
                    ?? (pi_currentEventSystem = ReflectionUtility.GetPropertyInfo(typeof(EventSystem), "main"))
                    ?? throw new MissingMemberException("This game has no EventSystem.current or EventSystem.main property!");
        }
        private static PropertyInfo pi_currentEventSystem;

        public static EventSystem CurrentEventSystem
        {
            get => (EventSystem)EventSystemCurrentInfo.GetValue(null, null);
            set => EventSystemCurrentInfo.SetValue(null, value, null);
        }

        private static bool settingEventSystem;
        private static EventSystem lastEventSystem;
        private static BaseInputModule lastInputModule;

        public static void SetEventSystem()
        {
            var current = CurrentEventSystem;
            if (current && current != UniversalUI.EventSys)
            {
                lastEventSystem = current;
                lastInputModule = current.currentInputModule;
                lastEventSystem.enabled = false;
            }
            if (!lastEventSystem)
            {
                var allSystems = RuntimeProvider.Instance.FindObjectsOfTypeAll(typeof(EventSystem));
                foreach (var obj in allSystems)
                {
                    var system = obj.TryCast<EventSystem>();
                    if (system == UniversalUI.EventSys)
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

            if (current == UniversalUI.EventSys)
                return;

            // Set to our current system
            settingEventSystem = true;
            UniversalUI.EventSys.enabled = true;
            CurrentEventSystem = UniversalUI.EventSys;
            InputManager.ActivateUIModule();
            settingEventSystem = false;
        }

        public static void ReleaseEventSystem()
        {
            if (lastEventSystem && lastEventSystem.gameObject.activeSelf)
            {
                lastEventSystem.enabled = true;

                settingEventSystem = true;
                UniversalUI.EventSys.enabled = false;
                CurrentEventSystem = lastEventSystem;
                lastInputModule?.ActivateModule();
                settingEventSystem = false;
            }
        }

        // Patches

        public static void SetupPatches()
        {
            try
            {
                PrefixPropertySetter(typeof(Cursor),
                    "lockState",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_lockState))));

                PrefixPropertySetter(typeof(Cursor),
                    "visible",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_set_visible))));

                PrefixPropertySetter(typeof(EventSystem),
                    "current",
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_EventSystem_set_current))));

                PrefixMethod(typeof(EventSystem),
                    "SetSelectedGameObject",
                    // some games use a modified version of uGUI that includes this extra int argument on this method.
                    new Type[] { typeof(GameObject), typeof(BaseEventData), typeof(int) },
                    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_EventSystem_SetSelectedGameObject))),
                    // most games use these arguments, we'll use them as our "backup".
                    new Type[] { typeof(GameObject), typeof(BaseEventData) });

                //// Not sure if this one is needed.
                //PrefixMethod(typeof(PointerInputModule),
                //    "ClearSelection",
                //    new Type[0],
                //    new HarmonyMethod(typeof(CursorUnlocker).GetMethod(nameof(CursorUnlocker.Prefix_PointerInputModule_ClearSelection))));
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
                var methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, arguments, null);
                if (methodInfo == null)
                {
                    if (backupArgs != null)
                        methodInfo = type.GetMethod(method, ReflectionUtility.FLAGS, null, backupArgs, null);
                    
                    if (methodInfo == null)
                        throw new MissingMethodException($"Could not find method for patching - '{type.FullName}.{method}'!");
                }

                var processor = Universe.Harmony.CreateProcessor(methodInfo);
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                Universe.LogWarning($"Unable to patch {type.Name}.{method}: {e.Message}");
            }
        }

        private static void PrefixPropertySetter(Type type, string property, HarmonyMethod prefix)
        {
            try
            {
                var processor = Universe.Harmony.CreateProcessor(type.GetProperty(property, ReflectionUtility.FLAGS).GetSetMethod());
                processor.AddPrefix(prefix);
                processor.Patch();
            }
            catch (Exception e)
            {
                Universe.Log($"Unable to patch {type.Name}.set_{property}: {e.Message}");
            }
        }

        // Prevent setting non-UniverseLib objects as selected when menu is open

        public static bool Prefix_EventSystem_SetSelectedGameObject(GameObject __0)
        {
            if (!UniversalUI.AnyUIShowing || !UniversalUI.CanvasRoot)
                return true;

            return __0 && __0.transform.root.gameObject.GetInstanceID() == UniversalUI.CanvasRoot.GetInstanceID();
        }

        //public static bool Prefix_PointerInputModule_ClearSelection()
        //{
        //    return !(UIManager.ShowMenu && UIManager.CanvasRoot);
        //}

        // Force EventSystem.current to be UniverseLib's when menu is open

        public static void Prefix_EventSystem_set_current(ref EventSystem value)
        {
            if (!settingEventSystem && value && value != UniversalUI.EventSys)
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

        public static void Prefix_set_lockState(ref CursorLockMode value)
        {
            if (!currentlySettingCursor)
            {
                lastLockMode = value;

                if (ShouldUnlock)
                    value = CursorLockMode.None;
            }
        }

        public static void Prefix_set_visible(ref bool value)
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