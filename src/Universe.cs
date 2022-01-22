using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.Runtime;
using UniverseLib.UI;

namespace UniverseLib
{
    public class Universe
    {
        public enum GlobalState
        {
            WaitingToSetup,
            SettingUp,
            SetupCompleted
        }

        public const string NAME = "UniverseLib";
        public const string VERSION = "1.1.1";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.universelib";

        public static RuntimeContext Context { get; internal set; }
        public static HarmonyLib.Harmony Harmony { get; } = new HarmonyLib.Harmony(GUID);

        public static GlobalState CurrentGlobalState { get; private set; }

        private static event Action OnInitialized;
        private static float startupDelay;

        private static Action<string, LogType> LogHandler;

        /// <summary>
        /// Initialize UniverseLib.
        /// </summary>
        public static void Init(float startupDelay, Action onInitialized, Action<string, LogType> logHandler, UUConfig config)
        {
            if (CurrentGlobalState == GlobalState.SetupCompleted)
            {
                InvokeOnInitialized(onInitialized);
                return;
            }

            if (startupDelay > Universe.startupDelay)
                Universe.startupDelay = startupDelay;

            ConfigManager.LoadConfig(config);

            OnInitialized += onInitialized;

            if (CurrentGlobalState == GlobalState.WaitingToSetup)
            {
                CurrentGlobalState = GlobalState.SettingUp;
                Log($"{NAME} {VERSION} initializing...");

                LogHandler = logHandler;

                UniversalBehaviour.Setup();
                ReflectionUtility.Init();
                RuntimeProvider.Init();

                RuntimeProvider.Instance.StartCoroutine(SetupCoroutine());

                Log($"Finished UniverseLib initial setup");
            }
        }

        // Do a delayed setup so that objects aren't destroyed instantly.
        // This can happen for a multitude of reasons.
        // Default delay is 1 second which is usually enough.
        private static IEnumerator SetupCoroutine()
        {
            yield return null;
            float prevRealTime = Time.realtimeSinceStartup;
            while (startupDelay > 0)
            {
                startupDelay -= Math.Max(Time.deltaTime, Time.realtimeSinceStartup - prevRealTime);
                prevRealTime = Time.realtimeSinceStartup;
                yield return null;
            }

            InputManager.Init();
            UniversalUI.Init();

            Log($"{NAME} {VERSION} initialized.");
            CurrentGlobalState = GlobalState.SetupCompleted;

            InvokeOnInitialized(OnInitialized);
        }

        private static void InvokeOnInitialized(Action onInitialized)
        {
            if (onInitialized == null)
                return;

            foreach (var listener in onInitialized.GetInvocationList())
            {
                try
                {
                    listener.DynamicInvoke();
                }
                catch (Exception ex)
                {
                    LogHandler?.Invoke($"Exception invoking onInitialized callback! {ex}", LogType.Warning);
                }
            }
        }

        internal static void Update()
        {
            UniversalUI.Update();
            RuntimeProvider.Instance.Update();
        }

        internal static void FixedUpdate()
        {
            RuntimeProvider.Instance.ProcessFixedUpdate();
        }

        internal static void OnPostRender()
        {
            RuntimeProvider.Instance.ProcessOnPostRender();
        }


        public static void Log(object message)
            => Log(message, LogType.Log);

        public static void LogWarning(object message)
            => Log(message, LogType.Warning);

        public static void LogError(object message)
            => Log(message, LogType.Error);

        private static void Log(object message, LogType logType)
        {
            LogHandler?.Invoke(message?.ToString() ?? "", logType);
        }
    }
}
