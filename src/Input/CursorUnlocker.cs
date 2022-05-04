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

        private static bool currentlySettingCursor;
        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

        private static WaitForEndOfFrame waitForEndOfFrame = new();

        [Obsolete("Moved to EventSystemHelper")]
        public static EventSystem CurrentEventSystem => EventSystemHelper.CurrentEventSystem;

        internal static void Init()
        {
            lastLockMode = Cursor.lockState;
            lastVisibleState = Cursor.visible;

            InitPatches();
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
                if (UniversalUI.AnyUIShowing || !EventSystemHelper.lastEventSystem)
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
                        EventSystemHelper.EnableEventSystem();
                }
                else
                {
                    Cursor.lockState = lastLockMode;
                    Cursor.visible = lastVisibleState;

                    if (!ConfigManager.Disable_EventSystem_Override)
                        EventSystemHelper.ReleaseEventSystem();
                }

                currentlySettingCursor = false;
            }
            catch (Exception e)
            {
                Universe.Log($"Exception setting Cursor state: {e}");
            }
        }

        // Patches

        internal static void InitPatches()
        {
            Universe.Patch(typeof(Cursor),
                "lockState",
                MethodType.Setter,
                prefix: AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_set_lockState)));

            Universe.Patch(typeof(Cursor),
                "visible",
                MethodType.Setter,
                prefix: AccessTools.Method(typeof(CursorUnlocker), nameof(Prefix_set_visible)));
        }

        // Force mouse to stay unlocked and visible while UnlockMouse and ShowMenu are true.
        // Also keep track of when anything else tries to set Cursor state, this will be the
        // value that we set back to when we close the menu or disable force-unlock.

        internal static void Prefix_set_lockState(ref CursorLockMode value)
        {
            try
            {
                if (!currentlySettingCursor)
                {
                    lastLockMode = value;

                    if (ShouldUnlock)
                        value = CursorLockMode.None;
                }
            }
            catch { }
        }

        internal static void Prefix_set_visible(ref bool value)
        {
            try
            {
                if (!currentlySettingCursor)
                {
                    lastVisibleState = value;

                    if (ShouldUnlock)
                        value = true;
                }
            }
            catch { }
        }
    }
}