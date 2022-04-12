using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.Runtime;

namespace UniverseLib.Utility
{
    /// <summary>
    /// Provides utility for displaying an object's ToString result in a more user-friendly format.
    /// </summary>
    public static class ToStringUtility
    {
        internal static Dictionary<string, MethodInfo> toStringMethods = new();

        private const string nullString = "<color=grey>null</color>";
        private const string nullUnknown = nullString + " (?)";
        private const string destroyedString = "<color=red>Destroyed</color>";
        private const string untitledString = "<i><color=grey>untitled</color></i>";

        private const string eventSystemNamespace = "UnityEngine.EventSystem";

        /// <summary>
        /// Constrains the provided string to a maximum length, and maximum number of lines.
        /// </summary>
        public static string PruneString(string s, int chars = 200, int lines = 5)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            StringBuilder sb = new(Math.Max(chars, s.Length));
            int newlines = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (newlines >= lines || i >= chars)
                {
                    sb.Append("...");
                    break;
                }
                char c = s[i];
                if (c == '\r' || c == '\n')
                    newlines++;
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the ToString result with a rich-text highlighted Type in trailing brackets. 
        /// If the object does not implement ToString, then only the trailing highlighted Type will be returned.
        /// </summary>
        public static string ToStringWithType(object value, Type fallbackType, bool includeNamespace = true)
        {
            if (value.IsNullOrDestroyed() && fallbackType == null)
                return nullUnknown;

            Type type = value?.GetActualType() ?? fallbackType;

            string richType = SignatureHighlighter.Parse(type, includeNamespace);

            StringBuilder sb = new();

            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                {
                    sb.Append(nullString);
                    AppendRichType(sb, richType);
                    return sb.ToString();
                }
                else // destroyed unity object
                {
                    sb.Append(destroyedString);
                    AppendRichType(sb, richType);
                    return sb.ToString();
                }
            }

            if (value is UnityEngine.Object obj)
            {
                if (string.IsNullOrEmpty(obj.name))
                    sb.Append(untitledString);
                else
                {
                    sb.Append('"');
                    sb.Append(PruneString(obj.name, 50, 1));
                    sb.Append('"');
                }

                AppendRichType(sb, richType);
            }
            else if (type.FullName.StartsWith(eventSystemNamespace))
            {
                // UnityEngine.EventSystem classes can have some obnoxious ToString results with rich text.
                sb.Append(richType);
            }
            else
            {
                string toString = ToString(value);

                if (type.IsGenericType
                    || toString == type.FullName
                    || toString == $"{type.FullName} {type.FullName}"
                    || toString == $"Il2Cpp{type.FullName}" || type.FullName == $"Il2Cpp{toString}")
                {
                    sb.Append(richType);
                }
                else // the ToString contains some actual implementation, use that value.
                {
                    sb.Append(PruneString(toString, 200, 5));

                    AppendRichType(sb, richType);
                }
            }

            return sb.ToString();
        }

        private static void AppendRichType(StringBuilder sb, string richType)
        {
            sb.Append(' ');
            sb.Append('(');
            sb.Append(richType);
            sb.Append(')');
        }

        private static string ToString(object value)
        {
            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                    return nullString;
                else // destroyed unity object
                    return destroyedString;
            }

            Type type = value.GetActualType();

            // Find and cache the ToString method for this Type, if haven't already.

            if (!toStringMethods.ContainsKey(type.AssemblyQualifiedName))
            {
                MethodInfo toStringMethod = type.GetMethod("ToString", ArgumentUtility.EmptyTypes);
                toStringMethods.Add(type.AssemblyQualifiedName, toStringMethod);
            }

            // Invoke the ToString method on the object

            value = value.TryCast(type);

            string toString;
            try
            {
                toString = (string)toStringMethods[type.AssemblyQualifiedName].Invoke(value, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                toString = ex.ReflectionExToString();
            }

            toString = ReflectionUtility.ProcessTypeInString(type, toString);

#if CPP
            if (value is Il2CppSystem.Type cppType)
            {
                Type monoType = Il2CppReflection.GetUnhollowedType(cppType);
                if (monoType != null)
                    toString = ReflectionUtility.ProcessTypeInString(monoType, toString);
            }
#endif

            return toString;
        }
    }
}
