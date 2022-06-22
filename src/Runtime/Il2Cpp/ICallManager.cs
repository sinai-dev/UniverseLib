#if CPP
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
#if INTEROP
using Il2CppInterop.Runtime;
#else
using UnhollowerBaseLib;
#endif

namespace UniverseLib.Runtime.Il2Cpp
{
    /// <summary>
    /// Helper class for using Unity ICalls (internal calls).
    /// </summary>
    public static class ICallManager
    {
        // cache used by GetICall
        private static readonly Dictionary<string, Delegate> iCallCache = new();
        // cache used by GetICallUnreliable
        private static readonly Dictionary<string, Delegate> unreliableCache = new();

        /// <summary>
        /// Helper to get and cache an iCall by providing the signature (eg. "UnityEngine.Resources::FindObjectsOfTypeAll").
        /// </summary>
        /// <typeparam name="T">The Type of Delegate to provide for the iCall.</typeparam>
        /// <param name="signature">The signature of the iCall you want to get.</param>
        /// <returns>The <typeparamref name="T"/> delegate if successful.</returns>
        /// <exception cref="MissingMethodException" />
        public static T GetICall<T>(string signature) where T : Delegate
        {
            if (iCallCache.ContainsKey(signature))
                return (T)iCallCache[signature];

            IntPtr ptr = IL2CPP.il2cpp_resolve_icall(signature);

            if (ptr == IntPtr.Zero)
                throw new MissingMethodException($"Could not find any iCall with the signature '{signature}'!");

            Delegate iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(signature, iCall);

            return (T)iCall;
        }

        /// <summary>
        /// Get an iCall which may be one of multiple different signatures (ie, the name changed in different Unity versions).
        /// Each possible signature must have the same Delegate type, it can only vary by name.
        /// </summary>
        public static T GetICallUnreliable<T>(params string[] possibleSignatures) where T : Delegate
        {
            // use the first possible signature as the 'key'.
            string key = possibleSignatures.First();

            if (unreliableCache.ContainsKey(key))
                return (T)unreliableCache[key];

            T iCall;
            IntPtr ptr;
            foreach (string sig in possibleSignatures)
            {
                ptr = IL2CPP.il2cpp_resolve_icall(sig);
                if (ptr != IntPtr.Zero)
                {
                    iCall = (T)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
                    unreliableCache.Add(key, iCall);
                    return iCall;
                }
            }

            throw new MissingMethodException($"Could not find any iCall from list of provided signatures starting with '{key}'!");
        }
    }
}
#endif