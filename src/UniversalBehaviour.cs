using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib
{
    /// <summary>
    /// Used for receiving Update events and starting Coroutines.
    /// </summary>
    internal class UniversalBehaviour : MonoBehaviour
    {
        internal static UniversalBehaviour Instance { get; private set; }

#if CPP
        public UniversalBehaviour(IntPtr ptr) : base(ptr) { }
#endif

        internal static void Setup()
        {
#if CPP
            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<UniversalBehaviour>();
#endif

            GameObject obj = new GameObject("UniverseLibBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();
        }

        internal void Update()
        {
            Universe.Update();
        }
    }
}
