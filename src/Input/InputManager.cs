using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    public enum InputType
    {
        InputSystem,
        Legacy,
        None
    }

    public static class InputManager
    {
        public static InputType CurrentType { get; private set; }

        private static IHandleInput inputHandler;

        // Core init

        public static void Init()
        {
            InitHandler();
            InitKeycodes();
            CursorUnlocker.Init();
        }

        private static void InitHandler()
        {
            // First, just try to use the legacy input, see if its working.
            // The InputSystem package may be present but not actually activated, so we can find out this way.

            if (LegacyInput.TInput != null)
            {
                try
                {
                    inputHandler = new LegacyInput();
                    CurrentType = InputType.Legacy;

                    // make sure its working
                    inputHandler.GetKeyDown(KeyCode.F5);

                    Universe.Log("Initialized Legacy Input support");
                    return;
                }
                catch
                {
                    // It's not working, we'll fall back to InputSystem.
                }
            }

            if (InputSystem.TKeyboard != null)
            {
                try
                {
                    inputHandler = new InputSystem();
                    CurrentType = InputType.InputSystem;
                    Universe.Log("Initialized new InputSystem support.");
                    return;
                }
                catch (Exception ex)
                {
                    Universe.Log(ex);
                }
            }

            Universe.LogWarning("Could not find any Input Module Type!");
            inputHandler = new NoInput();
            CurrentType = InputType.None;
        }

        private static void InitKeycodes()
        {
            // Cache keycodes for rebinding

            var keycodes = Enum.GetValues(typeof(KeyCode));
            var list = new List<KeyCode>();
            foreach (KeyCode kc in keycodes)
            {
                string s = kc.ToString();
                if (!s.Contains("Mouse") && !s.Contains("Joystick"))
                    list.Add(kc);
            }
            allKeycodes = list.ToArray();
        }

        // Main Input API

        public static Vector3 MousePosition
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return inputHandler.MousePosition;
            }
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return inputHandler.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return inputHandler.GetKey(key);
        }

        public static bool GetMouseButtonDown(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return inputHandler.GetMouseButtonDown(btn);
        }

        public static bool GetMouseButton(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return inputHandler.GetMouseButton(btn);
        }

        public static Vector2 MouseScrollDelta
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return inputHandler.MouseScrollDelta;
            }
        }

        // UI

        public static BaseInputModule UIInput => inputHandler.UIInputModule;

        public static void AddUIModule()
        {
            inputHandler.AddUIInputModule();
            ActivateUIModule();
        }

        public static void ActivateUIModule()
        {
            UniversalUI.EventSys.m_CurrentInputModule = UIInput;
            inputHandler.ActivateModule();
        }

        // Rebinding and Update

        public static bool Rebinding { get; internal set; }
        public static KeyCode? LastRebindKey { get; set; }

        internal static IEnumerable<KeyCode> allKeycodes;
        internal static Action<KeyCode> onRebindPressed;
        internal static Action<KeyCode?> onRebindFinished;

        public static void Update()
        {
            if (Rebinding)
            {
                var kc = GetCurrentKeyDown();
                if (kc != null)
                {
                    LastRebindKey = kc;
                    onRebindPressed?.Invoke((KeyCode)kc);
                }
            }
        }

        public static KeyCode? GetCurrentKeyDown()
        {
            foreach (var kc in allKeycodes)
            {
                if (inputHandler.GetKeyDown(kc))
                    return kc;
            }

            return null;
        }

        public static void BeginRebind(Action<KeyCode> onSelection, Action<KeyCode?> onFinished)
        {
            if (Rebinding)
                return;

            onRebindPressed = onSelection;
            onRebindFinished = onFinished;

            Rebinding = true;
            LastRebindKey = null;
        }

        public static void EndRebind()
        {
            if (!Rebinding)
                return;

            Rebinding = false;
            onRebindFinished?.Invoke(LastRebindKey);

            onRebindFinished = null;
            onRebindPressed = null;
        }
    }
}