using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace UniverseLib
{
    // Handles all Behaviour update calls for UniverseLib (Update, FixedUpdate, OnPostRender).
    // Basically just a wrapper which calls the corresponding methods in UniverseLib.

    public class UniversalBehaviour : MonoBehaviour
    {
        internal static UniversalBehaviour Instance { get; private set; }

        internal static void Setup()
        {
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<UniversalBehaviour>();
#endif

            var obj = new GameObject("UniverseLibBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();
        }

#if CPP
        public UniversalBehaviour(IntPtr ptr) : base(ptr) { }
#endif

        private static bool onPostRenderFailed;

        internal void Awake()
        {
            try
            {
#if CPP
                Camera.onPostRender = Camera.onPostRender == null
                   ? new Action<Camera>(OnPostRender)
                   : Il2CppSystem.Delegate.Combine(Camera.onPostRender, 
                        (Camera.CameraCallback)new Action<Camera>(OnPostRender)).Cast<Camera.CameraCallback>();

                if (Camera.onPostRender == null || Camera.onPostRender.delegates == null)
                {
                    Universe.LogWarning("Failed to add Camera.onPostRender listener, falling back to LateUpdate instead!");
                    onPostRenderFailed = true;
                }
#else
                Camera.onPostRender += OnPostRender;
#endif
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception adding onPostRender listener: {ex.ReflectionExToString()}\r\nFalling back to LateUpdate!");
                onPostRenderFailed = true;
            }
        }

        internal void Update()
        {
            Universe.Update();
        }

        internal void FixedUpdate()
        {
            Universe.FixedUpdate();
        }

        internal void LateUpdate()
        {
            if (onPostRenderFailed)
                OnPostRender(null);
        }

        internal static void OnPostRender(Camera _)
        {
            Universe.OnPostRender();
        }
    }
}
