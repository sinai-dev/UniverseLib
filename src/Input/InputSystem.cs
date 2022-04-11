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
        #region Reflection cache

        // typeof(InputSystem.Keyboard)
        public static Type TKeyboard => m_tKeyboard ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard");
        private static Type m_tKeyboard;

        // typeof(InputSystem.Mouse)
        public static Type TMouse => m_tMouse ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse");
        private static Type m_tMouse;

        // typeof (InputSystem.Key)
        public static Type TKey => m_tKey ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key");
        private static Type m_tKey;

        // InputSystem.Controls.ButtonControl.isPressed
        private static PropertyInfo btnIsPressedProp;
        // InputSystem.Controls.ButtonControl.wasPressedThisFrame
        private static PropertyInfo btnWasPressedProp;

        // Keyboard.current
        private static object CurrentKeyboard => m_currentKeyboard ??= kbCurrentProp.GetValue(null, null);
        private static object m_currentKeyboard;
        private static PropertyInfo kbCurrentProp;
        // Keyboard.this[Key]
        private static PropertyInfo kbIndexer;

        // Mouse.current
        private static object CurrentMouse => m_currentMouse ??= mouseCurrentProp.GetValue(null, null);
        private static object m_currentMouse;
        private static PropertyInfo mouseCurrentProp;

        // Mouse.current.leftButton
        private static object LeftMouseButton => m_lmb ??= leftButtonProp.GetValue(CurrentMouse, null);
        private static object m_lmb;
        private static PropertyInfo leftButtonProp;

        // Mouse.current.rightButton
        private static object RightMouseButton => m_rmb ??= rightButtonProp.GetValue(CurrentMouse, null);
        private static object m_rmb;
        private static PropertyInfo rightButtonProp;

        // InputSystem.InputControl<Vector2>.ReadValue()
        private static MethodInfo ReadV2ControlMethod;

        // Mouse.current.position
        private static object MousePositionInfo => m_pos ??= positionProp.GetValue(CurrentMouse, null);
        private static object m_pos;
        private static PropertyInfo positionProp;

        // Mouse.current.scroll
        private static object MouseScrollInfo => m_scrollInfo ??= scrollDeltaProp.GetValue(CurrentMouse, null);
        private static object m_scrollInfo;
        private static PropertyInfo scrollDeltaProp;

        // typeof(InputSystem.UI.InputSystemUIInputModule)
        public Type TInputSystemUIInputModule => typeOfUIInputModule
                                              ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
        internal Type typeOfUIInputModule;

        // Our UI input module
        public BaseInputModule UIInputModule => newInputModule;
        internal BaseInputModule newInputModule;

        // UI input action maps
        private Type typeofInputExtensions;
        private object UIActionMap;
        private MethodInfo mi_UI_Enable;
        private PropertyInfo pi_actionsAsset;

        #endregion

        public InputSystem()
        {
            SetupSupportedDevices();

            kbCurrentProp = TKeyboard.GetProperty("current");
            kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            Type btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
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
                object settings = TInputSystem.GetProperty("settings", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                // typeof(InputSettings)
                Type TSettings = settings.GetActualType();
                // InputSettings.supportedDevices
                PropertyInfo supportedProp = TSettings.GetProperty("supportedDevices", BindingFlags.Public | BindingFlags.Instance);
                object supportedDevices = supportedProp.GetValue(settings, null);
                // An empty supportedDevices list means all devices are supported.
#if CPP
                // weird hack for il2cpp, use the implicit operator and cast Il2CppStringArray to ReadOnlyArray<string>
                object[] emptyStringArray = new object[] { new UnhollowerBaseLib.Il2CppStringArray(0) };
                MethodInfo op_implicit = supportedDevices.GetActualType().GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public);
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

        // Input API

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

        #region KeyCode <-> Key Helpers

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
                    object parsed = KeyCodeToKeyEnum(key);
                    object actualKey = kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });
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
                string s = key.ToString();
                try
                {
                    if (keycodeToKeyFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                        s = s.Replace(entry.Key, entry.Value);
                }
                catch { }

                try
                {
                    object parsed = Enum.Parse(TKey, s);
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

        public void AddUIInputModule()
        {
            if (TInputSystemUIInputModule == null)
            {
                Universe.LogWarning("Unable to find UI Input Module Type, Input will not work!");
                return;
            }

            Type assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
            newInputModule = RuntimeHelper.AddComponent<BaseInputModule>(UniversalUI.CanvasRoot, TInputSystemUIInputModule);
            object asset = RuntimeHelper.CreateScriptable(assetType).TryCast(assetType);

            typeofInputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

            MethodInfo addMap = typeofInputExtensions.GetMethod("AddActionMap", new Type[] { assetType, typeof(string) });
            object map = addMap.Invoke(null, new object[] { asset, "UI" })
                .TryCast(ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionMap"));

            CreateAction(map, "point", new[] { "<Mouse>/position" }, "point");
            CreateAction(map, "click", new[] { "<Mouse>/leftButton" }, "leftClick");
            CreateAction(map, "rightClick", new[] { "<Mouse>/rightButton" }, "rightClick");
            CreateAction(map, "scrollWheel", new[] { "<Mouse>/scroll" }, "scrollWheel");

            mi_UI_Enable = map.GetType().GetMethod("Enable");
            mi_UI_Enable.Invoke(map, ArgumentUtility.EmptyArgs);
            UIActionMap = map;

            pi_actionsAsset = TInputSystemUIInputModule.GetProperty("actionsAsset");
        }

        private void CreateAction(object map, string actionName, string[] bindings, string propertyName)
        {
            MethodInfo disable = map.GetType().GetMethod("Disable");
            disable.Invoke(map, ArgumentUtility.EmptyArgs);

            Type inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction");
            MethodInfo addAction = typeofInputExtensions.GetMethod("AddAction");
            object action = addAction.Invoke(null, new object[] { map, actionName, default, null, null, null, null, null })
                .TryCast(inputActionType);

            MethodInfo addBinding = typeofInputExtensions.GetMethod("AddBinding",
                new Type[] { inputActionType, typeof(string), typeof(string), typeof(string), typeof(string) });

            foreach (string binding in bindings)
                addBinding.Invoke(null, new object[] { action.TryCast(inputActionType), binding, null, null, null });

            Type refType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference");
            object inputRef = refType.GetMethod("Create")
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
                mi_UI_Enable.Invoke(UIActionMap, ArgumentUtility.EmptyArgs);

                // if the actionsAsset is null, call the AssignDefaultActions method.
                if (pi_actionsAsset.GetValue(newInputModule, null) == null)
                {
                    newInputModule.GetType()
                        .GetMethod("AssignDefaultActions")
                        .Invoke(newInputModule, new object[0]);
                }
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception enabling InputSystem UI Input Module: " + ex);
            }
        }
    }
}