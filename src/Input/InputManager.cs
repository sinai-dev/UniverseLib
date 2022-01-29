using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    /// <summary>
    /// A universal Input handler which works with both legacy Input and the new InputSystem.
    /// </summary>
    public static class InputManager
    {
        /// <summary>
        /// The current Input package which is being used by the game.
        /// </summary>
        public static InputType CurrentType { get; private set; }

        private static IHandleInput inputHandler;

        // Core init

        internal static void Init()
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

        /// <summary>
        /// The current user Cursor position, with (0,0) being the bottom-left of the game display window.
        /// </summary>
        public static Vector3 MousePosition
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return inputHandler.MousePosition;
            }
        }

        /// <summary>
        /// The current mouse scroll delta from this frame. x is horizontal, y is vertical.
        /// </summary>
        public static Vector2 MouseScrollDelta
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return inputHandler.MouseScrollDelta;
            }
        }

        /// <summary>
        /// Returns true if the provided KeyCode was pressed this frame. Translates KeyCodes into Key if InputSystem is being used.
        /// </summary>
        public static bool GetKeyDown(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return inputHandler.GetKeyDown(key);
        }

        /// <summary>
        /// Returns true if the provided KeyCode is being held down (not necessarily just pressed). Translates KeyCodes into Key if InputSystem is being used.
        /// </summary>
        public static bool GetKey(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return inputHandler.GetKey(key);
        }

        /// <summary>
        /// Returns true if the provided mouse button was pressed this frame. 0 is left click, 1 is right button, 2 is middle button, etc.
        /// </summary>
        public static bool GetMouseButtonDown(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return inputHandler.GetMouseButtonDown(btn);
        }

        /// <summary>
        /// Returns true if the provided mouse button is being held down (not necessarily just pressed). 0 is left click, 1 is right button, 2 is middle button, etc.
        /// </summary>
        public static bool GetMouseButton(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return inputHandler.GetMouseButton(btn);
        }

        // UI

        /// <summary>
        /// The current BaseInputModule being used for the UI.
        /// </summary>
        public static BaseInputModule UIInput => inputHandler.UIInputModule;

        internal static void AddUIModule()
        {
            inputHandler.AddUIInputModule();
            ActivateUIModule();
        }

        internal static void ActivateUIModule()
        {
            UniversalUI.EventSys.m_CurrentInputModule = UIInput;
            inputHandler.ActivateModule();
        }

        // Rebinding and Update

        /// <summary>
        /// Whether anything is currently using the Rebinding feature.
        /// </summary>
        public static bool Rebinding { get; internal set; }

        /// <summary>
        /// The last pressed Key during rebinding.
        /// </summary>
        public static KeyCode? LastRebindKey { get; set; }

        internal static IEnumerable<KeyCode> allKeycodes;
        internal static Action<KeyCode> onRebindPressed;
        internal static Action<KeyCode?> onRebindFinished;

        internal static void Update()
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

        internal static KeyCode? GetCurrentKeyDown()
        {
            foreach (var kc in allKeycodes)
            {
                if (inputHandler.GetKeyDown(kc))
                    return kc;
            }

            return null;
        }

        /// <summary>
        /// Begins the Rebinding process, keys pressed will be recorded. Call <see cref="EndRebind"/> to finish rebinding.
        /// </summary>
        /// <param name="onSelection">Will be invoked whenever any key is pressed, even if rebinding has not finished yet.</param>
        /// <param name="onFinished">Invoked when EndRebind is called.</param>
        public static void BeginRebind(Action<KeyCode> onSelection, Action<KeyCode?> onFinished)
        {
            if (Rebinding)
                return;

            onRebindPressed = onSelection;
            onRebindFinished = onFinished;

            Rebinding = true;
            LastRebindKey = null;
        }

        /// <summary>
        /// Call this to finish Rebinding. The onFinished Action supplied to <see cref="BeginRebind"/> will be invoked if we are currently Rebinding.
        /// </summary>
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