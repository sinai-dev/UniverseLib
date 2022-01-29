using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    public class LegacyInput : IHandleInput
    {
        public LegacyInput()
        {
            mousePositionProp = TInput.GetProperty("mousePosition");
            mouseDeltaProp = TInput.GetProperty("mouseScrollDelta");
            getKeyMethod = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            getKeyDownMethod = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            getMouseButtonMethod = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            getMouseButtonDownMethod = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }

        public static Type TInput => m_tInput ??= ReflectionUtility.GetTypeByName("UnityEngine.Input");
        private static Type m_tInput;

        private static PropertyInfo mousePositionProp;
        private static PropertyInfo mouseDeltaProp;
        private static MethodInfo getKeyMethod;
        private static MethodInfo getKeyDownMethod;
        private static MethodInfo getMouseButtonMethod;
        private static MethodInfo getMouseButtonDownMethod;

        public Vector2 MousePosition => (Vector3)mousePositionProp.GetValue(null, null);

        public Vector2 MouseScrollDelta => (Vector2)mouseDeltaProp.GetValue(null, null);

        public bool GetKey(KeyCode key) => (bool)getKeyMethod.Invoke(null, new object[] { key });

        public bool GetKeyDown(KeyCode key) => (bool)getKeyDownMethod.Invoke(null, new object[] { key });

        public bool GetMouseButton(int btn) => (bool)getMouseButtonMethod.Invoke(null, new object[] { btn });

        public bool GetMouseButtonDown(int btn) => (bool)getMouseButtonDownMethod.Invoke(null, new object[] { btn });

        // UI Input module

        public BaseInputModule UIInputModule => m_inputModule;
        internal StandaloneInputModule m_inputModule;

        public void AddUIInputModule()
        {
            m_inputModule = UniversalUI.CanvasRoot.gameObject.AddComponent<StandaloneInputModule>();
            m_inputModule.m_EventSystem = UniversalUI.EventSys;
        }

        public void ActivateModule()
        {
            try
            {
                m_inputModule.ActivateModule();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception enabling StandaloneInputModule: {ex}");
            }
        }
    }
}