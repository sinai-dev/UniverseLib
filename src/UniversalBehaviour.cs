using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif

namespace UniverseLib
{
    /// <summary>
    /// Used for receiving Update events and starting Coroutines.
    /// </summary>
    internal class UniversalBehaviour : MonoBehaviour
    {
        internal static UniversalBehaviour Instance { get; private set; }

        internal static void Setup()
        {
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<UniversalBehaviour>();
#endif

            GameObject obj = new("UniverseLibBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();
        }

        internal void Update()
        {
            Universe.Update();
        }

#if CPP
        public UniversalBehaviour(IntPtr ptr) : base(ptr) { }

        static Delegate queuedDelegate;

        internal static void InvokeDelegate(Delegate method)
        {
            queuedDelegate = method;
            Instance.Invoke(nameof(InvokeQueuedAction), 0f);
        }

        void InvokeQueuedAction()
        {
            try
            {
                Delegate method = queuedDelegate;
                queuedDelegate = null;
                method?.DynamicInvoke();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception invoking action from IL2CPP thread: {ex}");
            }
        }
#endif
    }
}
