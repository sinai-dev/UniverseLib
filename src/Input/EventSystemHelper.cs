using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.Config;
using UniverseLib.Runtime;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    public static class EventSystemHelper
    {
        /// <summary>
        /// The value of "EventSystem.current", or "EventSystem.main" in some older games.
        /// </summary>
        public static EventSystem CurrentEventSystem
        {
            get => EventSystemCurrent_Handler.GetValue();
            set => EventSystemCurrent_Handler.SetValue(value);
        }

        /// <summary>
        /// The current BaseInputModule being used for the UniverseLib UI.
        /// </summary>
        public static BaseInputModule UIInput => InputManager.inputHandler.UIInputModule;

        internal static EventSystem lastEventSystem;
        static BaseInputModule lastInputModule;
        static bool settingEventSystem;
        static float timeOfLastEventSystemSearch;

        static readonly AmbiguousMemberHandler<EventSystem, EventSystem> EventSystemCurrent_Handler = new(true, true, "current", "main");
        
        static bool usingEventSystemDictionaryMembers;

        static readonly AmbiguousMemberHandler<EventSystem, GameObject> m_CurrentSelected_Handler_Normal
            = new(true, true, "m_CurrentSelected", "m_currentSelected");
        static readonly AmbiguousMemberHandler<EventSystem, bool> m_SelectionGuard_Handler_Normal
            = new(true, true, "m_SelectionGuard", "m_selectionGuard");

        static readonly AmbiguousMemberHandler<EventSystem, Dictionary<int, GameObject>> m_CurrentSelected_Handler_Dictionary
            = new(true, true, "m_CurrentSelected", "m_currentSelected");
        static readonly AmbiguousMemberHandler<EventSystem, Dictionary<int, bool>> m_SelectionGuard_Handler_Dictionary
            = new(true, true, "m_SelectionGuard", "m_selectionGuard");

        internal static void Init()
        {
            InitPatches();

            usingEventSystemDictionaryMembers = m_CurrentSelected_Handler_Dictionary.member != null;
        }

        /// <summary>
        /// Helper to call EventSystem.SetSelectedGameObject and bypass UniverseLib's override patch.
        /// </summary>
        public static void SetSelectedGameObject(GameObject obj)
        {
            try
            {
                EventSystem system = CurrentEventSystem;
                BaseEventData pointer = new(system);

                GameObject currentSelected;
                if (usingEventSystemDictionaryMembers)
                    currentSelected = m_CurrentSelected_Handler_Dictionary.GetValue(system)[0];
                else
                    currentSelected = m_CurrentSelected_Handler_Normal.GetValue(system);

                ExecuteEvents.Execute(currentSelected, pointer, ExecuteEvents.deselectHandler);

                if (usingEventSystemDictionaryMembers)
                    m_CurrentSelected_Handler_Dictionary.GetValue(system)[0] = obj;
                else
                    m_CurrentSelected_Handler_Normal.SetValue(system, obj);

                ExecuteEvents.Execute(obj, pointer, ExecuteEvents.selectHandler);
            }
            catch //(Exception e)
            {
                //Universe.LogWarning($"Exception setting current selected GameObject: {e}");
            }
        }

        /// <summary>
        /// Helper to set the SelectionGuard property on the current EventSystem with safe API.
        /// </summary>
        public static void SetSelectionGuard(bool value)
        {
            EventSystem system = CurrentEventSystem;

            if (usingEventSystemDictionaryMembers)
                m_SelectionGuard_Handler_Dictionary.GetValue(system)[0] = value;
            else
                m_SelectionGuard_Handler_Normal.SetValue(system, value);
        }

        /// <summary>
        /// If the UniverseLib EventSystem is not enabled, this enables it and sets EventSystem.current to it, and stores the previous EventSystem.
        /// </summary>
        internal static void EnableEventSystem()
        {
            if (!UniversalUI.EventSys)
                return;

            // Deactivate and store the current EventSystem

            EventSystem current = CurrentEventSystem;

            // If it's enabled and it's not the UniverseLib system, store it.
            if (current && !current.ReferenceEqual(UniversalUI.EventSys) && current.isActiveAndEnabled)
            {
                lastEventSystem = current;
                lastInputModule = current.currentInputModule;
                lastEventSystem.enabled = false;
            }
            else if (!lastEventSystem
                && !ConfigManager.Disable_Fallback_EventSystem_Search
                && Time.realtimeSinceStartup - timeOfLastEventSystemSearch > 10f)
            {
                FallbackEventSystemSearch();
                if (lastEventSystem)
                    lastEventSystem.enabled = false;
            }

            if (!UniversalUI.EventSys.enabled)
            {
                // Set to our current system
                settingEventSystem = true;
                UniversalUI.EventSys.enabled = true;
                ActivateUIModule();
                CurrentEventSystem = UniversalUI.EventSys;
                settingEventSystem = false;
            }

            CheckVRChatEventSystemFix();
        }

        // In some cases we may need to set our own EventSystem active before the original EventSystem is created or enabled.
        // For that we will need to use Resources to find the other active EventSystem once it has been created.
        static void FallbackEventSystemSearch()
        {
            timeOfLastEventSystemSearch = Time.realtimeSinceStartup;
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
                    //lastEventSystem.enabled = false;
                    break;
                }
            }
        }
#if MONO
        static readonly AmbiguousMemberHandler<EventSystem, List<EventSystem>> m_EventSystems_handler
            = new(true, true, "m_EventSystems", "m_eventSystems");
#endif

        /// <summary>
        /// If the UniverseLib EventSystem is enabled, this disables it and sets EventSystem.current to the previous EventSystem which was enabled.
        /// </summary>
        internal static void ReleaseEventSystem()
        {
            if (!UniversalUI.EventSys)
                return;

            CheckVRChatEventSystemFix();

            if (!lastEventSystem
                && !ConfigManager.Disable_Fallback_EventSystem_Search
                && Time.realtimeSinceStartup - timeOfLastEventSystemSearch > 10f)
            {
                FallbackEventSystemSearch();
            }

            if (!lastEventSystem)
            {
                //Universe.LogWarning($"No previous EventSystem found to set back to!");
                return;
            }

            settingEventSystem = true;

            UniversalUI.EventSys.enabled = false;
            UniversalUI.EventSys.currentInputModule?.DeactivateModule();

            if (lastEventSystem && lastEventSystem.gameObject.activeSelf)
            {
                if (lastInputModule)
                {
                    lastInputModule.ActivateModule();
                    lastEventSystem.m_CurrentInputModule = lastInputModule;
                }

#if MONO
                if (m_EventSystems_handler.member != null)
                {
                    List<EventSystem> list = m_EventSystems_handler.GetValue();
                    if (list != null && !list.Contains(lastEventSystem))
                        list.Add(lastEventSystem);
                }
#else
                    if (EventSystem.m_EventSystems != null && !EventSystem.m_EventSystems.Contains(lastEventSystem))
                        EventSystem.m_EventSystems.Add(lastEventSystem);

#endif
                CurrentEventSystem = lastEventSystem;
                lastEventSystem.enabled = true;
            }

            settingEventSystem = false;
        }

        // UI Input Module

        internal static void AddUIModule()
        {
            InputManager.inputHandler.AddUIInputModule();
            ActivateUIModule();
        }

        internal static void ActivateUIModule()
        {
            UniversalUI.EventSys.m_CurrentInputModule = UIInput;
            InputManager.inputHandler.ActivateModule();
        }

        // Dirty fix for some VRChat weirdness

        static void CheckVRChatEventSystemFix()
        {
            try
            {
                if (Application.productName != "VRChat")
                    return;

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
            catch { }
        }

        // ~~~~~~~~~~~~ Patches ~~~~~~~~~~~~

        static void InitPatches()
        {
            Universe.Patch(typeof(EventSystem),
                new string[] { "current", "main" },
                MethodType.Setter,
                prefix: AccessTools.Method(typeof(EventSystemHelper), nameof(Prefix_EventSystem_set_current)));

            Universe.Patch(typeof(EventSystem),
                "SetSelectedGameObject",
                MethodType.Normal,
                new Type[][]
                {
                    new Type[] { typeof(GameObject), typeof(BaseEventData), typeof(int) },
                    new Type[] { typeof(GameObject), typeof(BaseEventData) }
                },
                prefix: AccessTools.Method(typeof(EventSystemHelper), nameof(Prefix_EventSystem_SetSelectedGameObject)));
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

            if (!settingEventSystem && CursorUnlocker.ShouldUnlock && !ConfigManager.Disable_EventSystem_Override)
            {
                ActivateUIModule();
                value = UniversalUI.EventSys;
                value.enabled = true;
            }
        }
    }
}
