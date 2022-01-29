using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UniverseLib.Input
{
    public class InputSystem : IHandleInput
    {
        public InputSystem()
        {
            SetupSupportedDevices();

            kbCurrentProp = TKeyboard.GetProperty("current");
            kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            btnIsPressedProp = btnControl.GetProperty("isPressed");
            btnWasPressedProp = btnControl.GetProperty("wasPressedThisFrame");

            mouseCurrentProp = TMouse.GetProperty("current");
            leftButtonProp = TMouse.GetProperty("leftButton");
            rightButtonProp = TMouse.GetProperty("rightButton");
            scrollDeltaProp = TMouse.GetProperty("scroll");

            positionProp = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer")
                            .GetProperty("position");

            ReadV2ControlMethod = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                      .MakeGenericType(typeof(Vector2))
                                      .GetMethod("ReadValue");
        }

        internal static void SetupSupportedDevices()
        {
            try
            {
                // typeof(InputSystem)
                Type TInputSystem = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputSystem");
                // InputSystem.settings
                var settings = TInputSystem.GetProperty("settings", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                // typeof(InputSettings)
                Type TSettings = settings.GetActualType();
                // InputSettings.supportedDevices
                PropertyInfo supportedProp = TSettings.GetProperty("supportedDevices", BindingFlags.Public | BindingFlags.Instance);
                var supportedDevices = supportedProp.GetValue(settings, null);
                // An empty supportedDevices list means all devices are supported.
#if CPP
                // weird hack for il2cpp, use the implicit operator and cast Il2CppStringArray to ReadOnlyArray<string>
                var emptyStringArray = new object[] { new UnhollowerBaseLib.Il2CppStringArray(0) };
                var op_implicit = supportedDevices.GetActualType().GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public);
                supportedProp.SetValue(settings, op_implicit.Invoke(null, emptyStringArray), null);
#else
                supportedProp.SetValue(settings, Activator.CreateInstance(supportedDevices.GetActualType(), new object[] { new string[0] }), null);
#endif
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception setting up InputSystem.settings.supportedDevices list!");
                Universe.Log(ex);
            }
        }

#region reflection cache

        public static Type TKeyboard => m_tKeyboard ?? (m_tKeyboard = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard"));
        private static Type m_tKeyboard;

        public static Type TMouse => m_tMouse ?? (m_tMouse = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse"));
        private static Type m_tMouse;

        public static Type TKey => m_tKey ?? (m_tKey = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key"));
        private static Type m_tKey;

        private static PropertyInfo btnIsPressedProp;
        private static PropertyInfo btnWasPressedProp;

        private static object CurrentKeyboard => m_currentKeyboard ?? (m_currentKeyboard = kbCurrentProp.GetValue(null, null));
        private static object m_currentKeyboard;
        private static PropertyInfo kbCurrentProp;
        private static PropertyInfo kbIndexer;

        private static object CurrentMouse => m_currentMouse ?? (m_currentMouse = mouseCurrentProp.GetValue(null, null));
        private static object m_currentMouse;
        private static PropertyInfo mouseCurrentProp;

        private static object LeftMouseButton => m_lmb ?? (m_lmb = leftButtonProp.GetValue(CurrentMouse, null));
        private static object m_lmb;
        private static PropertyInfo leftButtonProp;

        private static object RightMouseButton => m_rmb ?? (m_rmb = rightButtonProp.GetValue(CurrentMouse, null));
        private static object m_rmb;
        private static PropertyInfo rightButtonProp;

        private static MethodInfo ReadV2ControlMethod;

        private static object MousePositionInfo => m_pos ?? (m_pos = positionProp.GetValue(CurrentMouse, null));
        private static object m_pos;
        private static PropertyInfo positionProp;

        private static object MouseScrollInfo => m_scrollInfo ?? (m_scrollInfo = scrollDeltaProp.GetValue(CurrentMouse, null));
        private static object m_scrollInfo;
        private static PropertyInfo scrollDeltaProp;

#endregion

        public Vector2 MousePosition => (Vector2)ReadV2ControlMethod.Invoke(MousePositionInfo, ArgumentUtility.EmptyArgs);

        public Vector2 MouseScrollDelta => (Vector2)ReadV2ControlMethod.Invoke(MouseScrollInfo, ArgumentUtility.EmptyArgs);

        public bool GetMouseButtonDown(int btn)
        {
            return btn switch
            {
                0 => (bool)btnWasPressedProp.GetValue(LeftMouseButton, null),
                1 => (bool)btnWasPressedProp.GetValue(RightMouseButton, null),
                // case 2: return (bool)_btnWasPressedProp.GetValue(MiddleMouseButton, null);
                _ => throw new NotImplementedException(),
            };
        }

        public bool GetMouseButton(int btn)
        {
            return btn switch
            {
                0 => (bool)btnIsPressedProp.GetValue(LeftMouseButton, null),
                1 => (bool)btnIsPressedProp.GetValue(RightMouseButton, null),
                // case 2: return (bool)_btnIsPressedProp.GetValue(MiddleMouseButton, null);
                _ => throw new NotImplementedException(),
            };
        }

        #region Button Helpers

        public static Dictionary<KeyCode, object> KeyCodeToKeyDict = new();
        public static Dictionary<KeyCode, object> KeyCodeToKeyEnumDict = new();

        internal static Dictionary<string, string> keycodeToKeyFixes = new()
        {
            { "Control", "Ctrl" },
            { "Return", "Enter" },
            { "Alpha", "Digit" },
            { "Keypad", "Numpad" },
            { "Numlock", "NumLock" },
            { "Print", "PrintScreen" },
            { "BackQuote", "Backquote" }
        };

        public static object KeyCodeToActualKey(KeyCode key)
        {
            if (!KeyCodeToKeyDict.ContainsKey(key))
            {
                try
                {
                    var parsed = KeyCodeToKeyEnum(key);
                    var actualKey = kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });
                    KeyCodeToKeyDict.Add(key, actualKey);
                }
                catch
                {
                    KeyCodeToKeyDict.Add(key, default);
                }
            }

            return KeyCodeToKeyDict[key];
        }

        public static object KeyCodeToKeyEnum(KeyCode key)
        {
            if (!KeyCodeToKeyEnumDict.ContainsKey(key))
            {
                var s = key.ToString();
                try
                {
                    if (keycodeToKeyFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                        s = s.Replace(entry.Key, entry.Value);
                }
                catch { }

                try
                {
                    var parsed = Enum.Parse(TKey, s);
                    KeyCodeToKeyEnumDict.Add(key, parsed);
                }
                catch
                {
                    KeyCodeToKeyEnumDict.Add(key, default);
                }
            }

            return KeyCodeToKeyEnumDict[key];
        }

        #endregion

        public bool GetKeyDown(KeyCode key)
        {
            try
            {
                return (bool)btnWasPressedProp.GetValue(KeyCodeToActualKey(key), null);
            }
            catch
            {
                return false;
            }
        }

        public bool GetKey(KeyCode key)
        {
            try
            {
                return (bool)btnIsPressedProp.GetValue(KeyCodeToActualKey(key), null);
            }
            catch
            {
                return false;
            }
        }

        // UI Input

        public Type TInputSystemUIInputModule
            => typeOfUIInputModule ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
        internal Type typeOfUIInputModule;

        public BaseInputModule UIInputModule => newInputModule;
        internal BaseInputModule newInputModule;

        public void AddUIInputModule()
        {
            if (TInputSystemUIInputModule == null)
            {
                Universe.LogWarning("Unable to find UI Input Module Type, Input will not work!");
                return;
            }

            var assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
            newInputModule = RuntimeHelper.Instance.Internal_AddComponent<BaseInputModule>(UniversalUI.CanvasRoot, TInputSystemUIInputModule);
            var asset = RuntimeHelper.Instance.Internal_CreateScriptable(assetType)
                .TryCast(assetType);

            inputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

            var addMap = inputExtensions.GetMethod("AddActionMap", new Type[] { assetType, typeof(string) });
            var map = addMap.Invoke(null, new object[] { asset, "UI" })
                .TryCast(ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionMap"));

            CreateAction(map, "point", new[] { "<Mouse>/position" }, "point");
            CreateAction(map, "click", new[] { "<Mouse>/leftButton" }, "leftClick");
            CreateAction(map, "rightClick", new[] { "<Mouse>/rightButton" }, "rightClick");
            CreateAction(map, "scrollWheel", new[] { "<Mouse>/scroll" }, "scrollWheel");

            UI_Enable = map.GetType().GetMethod("Enable");
            UI_Enable.Invoke(map, ArgumentUtility.EmptyArgs);
            UI_ActionMap = map;
        }

        private Type inputExtensions;
        private object UI_ActionMap;
        private MethodInfo UI_Enable;

        private void CreateAction(object map, string actionName, string[] bindings, string propertyName)
        {
            var disable = map.GetType().GetMethod("Disable");
            disable.Invoke(map, ArgumentUtility.EmptyArgs);

            var inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction");
            var addAction = inputExtensions.GetMethod("AddAction");
            var action = addAction.Invoke(null, new object[] { map, actionName, default, null, null, null, null, null })
                .TryCast(inputActionType);

            var addBinding = inputExtensions.GetMethod("AddBinding",
                new Type[] { inputActionType, typeof(string), typeof(string), typeof(string), typeof(string) });

            foreach (string binding in bindings)
                addBinding.Invoke(null, new object[] { action.TryCast(inputActionType), binding, null, null, null });

            var refType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference");
            var inputRef = refType.GetMethod("Create")
                            .Invoke(null, new object[] { action })
                            .TryCast(refType);

            TInputSystemUIInputModule
                .GetProperty(propertyName)
                .SetValue(newInputModule.TryCast(TInputSystemUIInputModule), inputRef, null);
        }

        public void ActivateModule()
        {
            try
            {
                newInputModule.m_EventSystem = UniversalUI.EventSys;
                newInputModule.ActivateModule();
                UI_Enable.Invoke(UI_ActionMap, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception enabling InputSystem UI Input Module: " + ex);
            }
        }
    }
}