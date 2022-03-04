using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UniverseLib.Utility
{
    public static class MiscUtility
    {
        private static bool isFirstCallToInspect = true;
        private static Type inspectorManagerType;
        private static Type cacheObjectBaseType;
        
        /// <summary>
        /// Check if a string contains another string, case-insensitive.
        /// </summary>
        public static bool ContainsIgnoreCase(this string _this, string s)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(_this, s, CompareOptions.IgnoreCase) >= 0;
        }

        /// <summary>
        /// Just to allow Enum to do .HasFlag() in NET 3.5
        /// </summary>
        public static bool HasFlag(this Enum flags, Enum value)
        {
            ulong flag = Convert.ToUInt64(value);
            return (Convert.ToUInt64(flags) & flag) == flag;
        }

        /// <summary>
        /// Returns true if the StringBuilder ends with the provided string.
        /// </summary>
        public static bool EndsWith(this StringBuilder sb, string _string)
        {
            int len = _string.Length;

            if (sb.Length < len)
                return false;

            int stringpos = 0;
            for (int i = sb.Length - len; i < sb.Length; i++, stringpos++)
            {
                if (sb[i] != _string[stringpos])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tells whether UnityExplorer plugin is currently loaded or not.
        /// </summary>
        /// <returns></returns>
        public static bool CanInspect() => !isFirstCallToInspect
            ? inspectorManagerType != null
            : (isFirstCallToInspect = false) !=
              ((inspectorManagerType = ReflectionUtility.GetTypeByName("UnityExplorer.InspectorManager")) != null);

        /// <summary>
        /// Sends a type to open in a new tab of UnityExplorer's Inspector, if available.
        /// </summary>
        /// <param name="type">a Type to inspect</param>
        public static void Inspect(Type type)
        {
            if (!CanInspect())
                return;

            inspectorManagerType.GetMethod("Inspect", ReflectionUtility.FLAGS, null, 
                new Type[] {typeof(Type)}, null)?.Invoke(null, new object[] {type});
        }

        /// <summary>
        /// Sends an object to UnityExplorer's Inspector, if available.
        /// </summary>
        /// <param name="obj">an object to inspect</param>
        public static void Inspect(object obj, object sourceCache = null)
        {
            if (!CanInspect())
                return;
            
            cacheObjectBaseType ??= ReflectionUtility.GetTypeByName("UnityExplorer.CacheObject.CacheObjectBase");

            inspectorManagerType.GetMethod("Inspect", ReflectionUtility.FLAGS, null,
                new Type[] {typeof(object), cacheObjectBaseType}, null)?.Invoke(null, new object[] {obj, sourceCache});
        }
    }
}
