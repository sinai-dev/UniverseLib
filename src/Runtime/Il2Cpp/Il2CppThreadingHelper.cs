#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if INTEROP
using Il2CppInterop.Runtime;
#else
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
#endif

namespace UniverseLib.Runtime.Il2Cpp
{
    public static class Il2CppThreadingHelper
    {
        /// <summary>
        /// Invokes your delegate on the main thread, necessary when using threads to work with certain Unity API, etc.
        /// </summary>
        public static void InvokeOnMainThread(Delegate method)
        {
            UniversalBehaviour.InvokeDelegate(method);
        }

        /// <summary>
        /// Start a new IL2CPP Thread with your entry point.
        /// </summary>
        public static Il2CppSystem.Threading.Thread StartThread(Action entryPoint)
        {
            if (entryPoint == null)
                throw new ArgumentNullException(nameof(entryPoint));

            System.Threading.ThreadStart entry = new(entryPoint);
            Il2CppSystem.Threading.Thread thread
                = new(DelegateSupport.ConvertDelegate<Il2CppSystem.Threading.ThreadStart>(entry));
            thread.Start();
            IL2CPP.il2cpp_thread_attach(thread.Pointer);
            return thread;
        }
    }
}

#endif