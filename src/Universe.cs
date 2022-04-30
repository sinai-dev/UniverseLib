using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
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
        public const string VERSION = "1.3.14";
        public const string AUTHOR = "Sinai";
        public const string GUID = "com.sinai.universelib";

        /// <summary>The current runtime context (Mono or IL2CPP).</summary>
        public static RuntimeContext Context { get; } =
#if MONO
            RuntimeContext.Mono;
#else
            RuntimeContext.IL2CPP;
#endif

        /// <summary>The current setup state of UniverseLib.</summary>
        public static GlobalState CurrentGlobalState { get; private set; }

        internal static Harmony Harmony { get; } = new Harmony(GUID);

        static float startupDelay;
        static event Action OnInitialized;

        static readonly Dictionary<Assembly, Action<string, LogType>> logHandlers = new();

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

            ConfigManager.LoadConfig(config);

            OnInitialized += onInitialized;

            if (logHandler != null)
                logHandlers[logHandler.Method.DeclaringType.Assembly] = logHandler;

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

                Log($"Finished UniverseLib initial setup.");
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

            Stopwatch sw = new();
            sw.Start();
            while (ReflectionUtility.Initializing || sw.ElapsedMilliseconds * 0.001f < startupDelay)
                yield return null;

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

            foreach (Delegate listener in onInitialized.GetInvocationList())
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

        static readonly Assembly thisAssembly = typeof(Universe).Assembly;

        internal static void Log(object message)
            => Log(message, LogType.Log);

        internal static void LogWarning(object message)
            => Log(message, LogType.Warning);

        internal static void LogError(object message)
            => Log(message, LogType.Error);

        static void Log(object message, LogType logType)
        {
            if (!logHandlers.Any())
                return;

            // Get the calling assembly and use their log handler, if possible.
            // Not the best way to do this, but the best we can do without a huge refactor of the whole project.
            // We would require giving an instance of Universe to each Init caller and having all
            // functionality of UniverseLib go through that instance, instead of being static.
            Assembly callingAssembly = null;
            StackTrace trace = new(false);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                Assembly ass = frame.GetMethod().DeclaringType.Assembly;
                if (ass != thisAssembly)
                {
                    callingAssembly = ass;
                    break;
                }
            }

            if (callingAssembly == null || !logHandlers.TryGetValue(callingAssembly, out Action<string, LogType> handler))
                handler = logHandlers.First().Value;

            handler.Invoke($"[UniverseLib] {message?.ToString() ?? string.Empty}", logType);
        }

        // Patching helpers

        internal static bool Patch(Type type, string methodName, MethodType methodType, Type[] arguments = null,
            MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo finalizer = null)
        {
            try
            {
                string namePrefix = methodType switch
                {
                    MethodType.Getter => "get_",
                    MethodType.Setter => "set_",
                    _ => string.Empty
                };

                MethodInfo target;
                if (arguments != null)
                    target = type.GetMethod($"{namePrefix}{methodName}", AccessTools.all, null, arguments, null);
                else
                    target = type.GetMethod($"{namePrefix}{methodName}", AccessTools.all);

                if (target == null)
                {
                    // LogWarning($"\t Couldn't find any method on type {type.FullName} called {methodName}!");
                    return false;
                }

#if CPP
                // if this is an IL2CPP type, ensure method wasn't stripped.
                if (UnhollowerRuntimeLib.Il2CppType.From(type, false) != null
                    && UnhollowerBaseLib.UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target) == null)
                {
                    Log($"\t IL2CPP method has no corresponding pointer, aborting patch of {type.FullName}.{methodName}");
                    return false;
                }
#endif

                PatchProcessor processor = Harmony.CreateProcessor(target);

                if (prefix != null)
                    processor.AddPrefix(new HarmonyMethod(prefix));
                if (postfix != null)
                    processor.AddPostfix(new HarmonyMethod(postfix));
                if (finalizer != null)
                    processor.AddFinalizer(new HarmonyMethod(finalizer));

                processor.Patch();

                // Log($"\t Successfully patched {type.FullName}.{methodName}");
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"\t Exception patching {type.FullName}.{methodName}: {ex}");
                return false;
            }
        }

        internal static bool Patch(Type type, string[] possibleNames, MethodType methodType, Type[] arguments = null,
            MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo finalizer = null)
        {
            foreach (var name in possibleNames)
            {
                if (Patch(type, name, methodType, arguments, prefix, postfix, finalizer))
                    return true;
            }
            return false;
        }

        internal static bool Patch(Type type, string[] possibleNames, MethodType methodType, Type[][] possibleArguments,
            MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo finalizer = null)
        {
            foreach (var name in possibleNames)
            {
                foreach (var arguments in possibleArguments)
                {
                    if (Patch(type, name, methodType, arguments, prefix, postfix, finalizer))
                        return true;
                }
            }
            return false;
        }

        internal static bool Patch(Type type, string methodName, MethodType methodType, Type[][] possibleArguments,
            MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo finalizer = null)
        {
            foreach (var arguments in possibleArguments)
            {
                if (Patch(type, methodName, methodType, arguments, prefix, postfix, finalizer))
                    return true;
            }
            return false;
        }
    }
}
