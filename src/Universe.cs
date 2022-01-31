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
        public const string VERSION = "1.2.1";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.universelib";

        /// <summary>The current runtime context (Mono or IL2CPP).</summary>
        public static RuntimeContext Context { get; internal set; }

        /// <summary>The current setup state of UniverseLib.</summary>
        public static GlobalState CurrentGlobalState { get; private set; }

        internal static HarmonyLib.Harmony Harmony { get; } = new HarmonyLib.Harmony(GUID);

        private static event Action OnInitialized;

        private static float startupDelay;
        private static Action<string, LogType> logHandler;

        /// <summary>
        /// Initialize UniverseLib with default settings, if you don't require any finer control over the startup process.
        /// </summary>
        /// <param name="onInitialized">Invoked after the <c>startupDelay</c>and after UniverseLib has finished initializing.</param>
        /// <param name="logHandler">Should be used for printing UniverseLib's own internal logs. Your listener will only be used if no listener has 
        /// yet been provided to handle it. It is not required to implement this but it may be useful to diagnose internal errors.</param>
        public static void Init(Action onInitialized = null, Action<string, LogType> logHandler = null)
            => Init(1f, onInitialized, logHandler, default);

        /// <summary>
        /// Initialize UniverseLib with the provided parameters.
        /// </summary>
        /// <param name="startupDelay">Will be used only if it is the highest value supplied to this method compared to other assemblies.
        /// If another assembly calls this Init method with a higher startupDelay, their value will be used instead.</param>
        /// <param name="onInitialized">Invoked after the <paramref name="startupDelay"/> and after UniverseLib has finished initializing.</param>
        /// <param name="logHandler">Should be used for printing UniverseLib's own internal logs. Your listener will only be used if no listener has 
        /// yet been provided to handle it. It is not required to implement this but it may be useful to diagnose internal errors.</param>
        /// <param name="config">Can be used to set certain values of UniverseLib's configuration. Null config values will be ignored.</param>
        public static void Init(float startupDelay, Action onInitialized, Action<string, LogType> logHandler, UniverseLibConfig config)
        {
            // If already finished intializing, just return (and invoke onInitialized if supplied)
            if (CurrentGlobalState == GlobalState.SetupCompleted)
            {
                InvokeOnInitialized(onInitialized);
                return;
            }

            // Only use the provided startup delay if its higher than the current value
            if (startupDelay > Universe.startupDelay)
                Universe.startupDelay = startupDelay;

            // Try to load the supplied configuration
            ConfigManager.LoadConfig(config);

            OnInitialized += onInitialized;

            // We only need one log handler, it would be redundant to have multiple things logging UniverseLib's internal logs
            if (Universe.logHandler == null)
                Universe.logHandler = logHandler;

            // If this is the first Init call, begin the startup process
            if (CurrentGlobalState == GlobalState.WaitingToSetup)
            {
                CurrentGlobalState = GlobalState.SettingUp;
                Log($"{NAME} {VERSION} initializing...");

                // Run immediate setups which don't require any delay
                UniversalBehaviour.Setup();
                ReflectionUtility.Init();
                RuntimeHelper.Init();

                // Begin the startup delay coroutine
                RuntimeHelper.Instance.Internal_StartCoroutine(SetupCoroutine());

                Log($"Finished UniverseLib initial setup");
            }
        }

        internal static void Update()
        {
            UniversalUI.Update();
        }

        private static IEnumerator SetupCoroutine()
        {
            // Always yield at least one frame, otherwise if the first startupDelay is 0f this would run immediately and
            // not allow other Init calls to set a higher delay.
            yield return null;

            float prevRealTime = Time.realtimeSinceStartup;
            while (startupDelay > 0)
            {
                // In some games, Time.realtimeSinceStartup can give strange values during startup and cause this to break.
                // So instead, we take the higher of either the realtime delta or the game's Time.deltaTime.
                startupDelay -= Math.Max(Time.deltaTime, Time.realtimeSinceStartup - prevRealTime);
                prevRealTime = Time.realtimeSinceStartup;
                yield return null;
            }

            // Initialize late startup processes
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
                    LogWarning($"Exception invoking onInitialized callback! {ex}");
                }
            }
        }

        // UniverseLib internal logging. These are assumed to be handled by a logHandler supplied to Init().
        // Not for external use.

        internal static void Log(object message)
            => Log(message, LogType.Log);

        internal static void LogWarning(object message)
            => Log(message, LogType.Warning);

        internal static void LogError(object message)
            => Log(message, LogType.Error);

        private static void Log(object message, LogType logType)
        {
            logHandler?.Invoke(message?.ToString() ?? string.Empty, logType);
        }
    }
}
