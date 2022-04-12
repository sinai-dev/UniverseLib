using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UniverseLib.Config;
using UniverseLib.Runtime;
using UniverseLib.Utility;
using BF = System.Reflection.BindingFlags;

namespace UniverseLib
{
    /// <summary>
    /// Helper class for general Reflection API.
    /// </summary>
    public class ReflectionUtility
    {
        /// <summary>
        /// Shorthand for BF.Public | BF.Instance | BF.NonPublic | BF.Static
        /// </summary>
        public const BF FLAGS = BF.Public | BF.Instance | BF.NonPublic | BF.Static;

        internal static ReflectionUtility Instance { get; private set; }

        internal static void Init()
        {
            Instance =
#if CPP
                new Il2CppReflection();
#else
                new ReflectionUtility();
#endif
            ReflectionPatches.Init();

            Instance.Initialize();
        }

        protected virtual void Initialize()
        {
            SetupTypeCache();
        }

        #region Type cache

        public static Action<Type> OnTypeLoaded;

        /// <summary>Key: Type.FullName, Value: Type</summary>
        public static readonly SortedDictionary<string, Type> AllTypes = new(StringComparer.OrdinalIgnoreCase);

        public static readonly List<string> AllNamespaces = new();
        private static readonly HashSet<string> uniqueNamespaces = new();

        private static string[] allTypesArray;
        public static string[] GetTypeNameArray()
        {
            if (allTypesArray == null || allTypesArray.Length != AllTypes.Count)
            {
                allTypesArray = new string[AllTypes.Count];
                int i = 0;
                foreach (string name in AllTypes.Keys)
                {
                    allTypesArray[i] = name;
                    i++;
                }
            }
            return allTypesArray;
        }

        private static void SetupTypeCache()
        {
            float start = Time.realtimeSinceStartup;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                CacheTypes(asm);

            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;

            Universe.Log($"Cached AppDomain assemblies in {Time.realtimeSinceStartup - start} seconds");
        }

        private static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly == null || args.LoadedAssembly.GetName().Name == "completions")
                return;

            CacheTypes(args.LoadedAssembly);
        }

        private static void CacheTypes(Assembly asm)
        {
            foreach (Type type in asm.TryGetTypes())
            {
                // Cache namespace if there is one
                if (!string.IsNullOrEmpty(type.Namespace) && !uniqueNamespaces.Contains(type.Namespace))
                {
                    uniqueNamespaces.Add(type.Namespace);
                    int i = 0;
                    while (i < AllNamespaces.Count)
                    {
                        if (type.Namespace.CompareTo(AllNamespaces[i]) < 0)
                            break;
                        i++;
                    }
                    AllNamespaces.Insert(i, type.Namespace);
                }

                // Cache the type. Overwrite type if one exists with the full name
                AllTypes[type.FullName] = type;

                // Invoke listener
                OnTypeLoaded?.Invoke(type);

                // Check type inheritance cache, add this to any lists it should be in
                foreach (string key in typeInheritance.Keys)
                {
                    try
                    {
                        Type baseType = AllTypes[key];
                        if (baseType.IsAssignableFrom(type) && !typeInheritance[key].Contains(type))
                            typeInheritance[key].Add(type);
                    }
                    catch { }
                }
            }
        }

        #endregion


        #region Main Utility methods

        /// <summary>
        /// Find a <see cref="Type"/> in the current AppDomain whose <see cref="Type.FullName"/> matches the provided <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName">The <see cref="Type.FullName"/> you want to search for - case sensitive and full matches only.</param>
        /// <returns>The Type if found, otherwise null.</returns>
        public static Type GetTypeByName(string fullName)
            => Instance.Internal_GetTypeByName(fullName);

        internal virtual Type Internal_GetTypeByName(string fullName)
        {
            AllTypes.TryGetValue(fullName, out Type type);
            return type;
        }

        // Getting the actual type of an object
        internal virtual Type Internal_GetActualType(object obj)
            => obj?.GetType();

        // Force-casting an object to a type
        internal virtual object Internal_TryCast(object obj, Type castTo)
            => obj;

        /// <summary>
        /// Sanitize <paramref name="theString"/> which contains the obfuscated name of the provided <paramref name="type"/>. Returns the sanitized string.
        /// </summary>
        public static string ProcessTypeInString(Type type, string theString)
            => Instance.Internal_ProcessTypeInString(theString, type);

        internal virtual string Internal_ProcessTypeInString(string theString, Type type)
            => theString;

        /// <summary>
        /// Used by UnityExplorer's Singleton search. Checks all <paramref name="possibleNames"/> as field members (and properties in IL2CPP) for instances of the <paramref name="type"/>, 
        /// and populates the <paramref name="instances"/> list with non-null values.
        /// </summary>
        public static void FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
            => Instance.Internal_FindSingleton(possibleNames, type, flags, instances);

        internal virtual void Internal_FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
        {
            // Look for a typical Instance backing field.
            FieldInfo fi;
            foreach (string name in possibleNames)
            {
                fi = type.GetField(name, flags);
                if (fi != null)
                {
                    object instance = fi.GetValue(null);
                    if (instance != null)
                    {
                        instances.Add(instance);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to extract loaded Types from a ReflectionTypeLoadException.
        /// </summary>
        public static Type[] TryExtractTypesFromException(ReflectionTypeLoadException e)
        {
            try
            {
                return e.Types.Where(it => it != null).ToArray();
            }
            catch
            {
                return ArgumentUtility.EmptyTypes;
            }
        }

        #endregion


        #region Type inheritance cache

        // cache for GetBaseTypes
        internal static readonly Dictionary<string, Type[]> baseTypes = new();

        /// <summary>
        /// Get all base types of the Type of the provided object, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(object obj) => GetAllBaseTypes(obj?.GetActualType());

        /// <summary>
        /// Get all base types of the provided Type, including itself.
        /// </summary>
        public static Type[] GetAllBaseTypes(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            string name = type.AssemblyQualifiedName;

            if (baseTypes.TryGetValue(name, out Type[] ret))
                return ret;

            List<Type> list = new();

            while (type != null)
            {
                list.Add(type);
                type = type.BaseType;
            }

            ret = list.ToArray();

            baseTypes.Add(name, ret);

            return ret;
        }

        #endregion


        #region Type and Generic Parameter implementation cache

        // cache for GetImplementationsOf
        internal static readonly Dictionary<string, HashSet<Type>> typeInheritance = new();
        internal static readonly Dictionary<string, HashSet<Type>> genericParameterInheritance = new();

        /// <summary>
        /// Returns the key used for checking implementations of this type.
        /// </summary>
        public static string GetImplementationKey(Type type)
        {
            if (!type.IsGenericParameter)
                return type.FullName;
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(type.GenericParameterAttributes)
                    .Append('|');
                foreach (Type c in type.GetGenericParameterConstraints())
                    sb.Append(c.FullName).Append(',');
                return sb.ToString();
            }
        }

        /// <summary>
        /// Get all implementations of the provided type (include itself, if not abstract) in the current AppDomain.
        /// Also works for generic parameters by analyzing the constraints.
        /// </summary>
        /// <param name="baseType">The base type, which can optionally be abstract / interface.</param>
        /// <returns>All implementations of the type in the current AppDomain.</returns>
        public static HashSet<Type> GetImplementationsOf(Type baseType, bool allowAbstract, bool allowGeneric, bool allowEnum, bool allowRecursive = true)
        {
            string key = GetImplementationKey(baseType);

            int count = AllTypes.Count;
            HashSet<Type> ret;
            if (!baseType.IsGenericParameter)
                ret = GetImplementations(key, baseType, allowAbstract, allowGeneric, allowEnum);
            else
                ret = GetGenericParameterImplementations(key, baseType, allowAbstract, allowGeneric);

            // types were resolved during the parse, do it again if we're not already rebuilding.
            if (allowRecursive && AllTypes.Count != count)
                ret = GetImplementationsOf(baseType, allowAbstract, allowGeneric, false);

            return ret;
        }

        private static HashSet<Type> GetImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric, bool allowEnum)
        {
            if (!typeInheritance.ContainsKey(key))
            {
                HashSet<Type> set = new HashSet<Type>();
                string[] names = GetTypeNameArray();
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    try
                    {
                        Type type = AllTypes[name];

                        if (set.Contains(type)
                            || (type.IsAbstract && type.IsSealed) // ignore static classes
                            || (!allowAbstract && type.IsAbstract)
                            || (!allowGeneric && (type.IsGenericType || type.IsGenericTypeDefinition))
                            || (!allowEnum && type.IsEnum))
                            continue;

                        if (type.FullName.Contains("PrivateImplementationDetails")
                            || type.FullName.Contains("DisplayClass")
                            || type.FullName.Contains('<'))
                            continue;

                        if (baseType.IsAssignableFrom(type))
                            set.Add(type);
                    }
                    catch { }
                }

                typeInheritance.Add(key, set);
            }

            return typeInheritance[key];
        }

        private static HashSet<Type> GetGenericParameterImplementations(string key, Type baseType, bool allowAbstract, bool allowGeneric)
        {
            if (!genericParameterInheritance.ContainsKey(key))
            {
                HashSet<Type> set = new HashSet<Type>();

                string[] names = GetTypeNameArray();
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    try
                    {
                        Type type = AllTypes[name];

                        if (set.Contains(type)
                            || (type.IsAbstract && type.IsSealed) // ignore static classes
                            || (!allowAbstract && type.IsAbstract)
                            || (!allowGeneric && (type.IsGenericType || type.IsGenericTypeDefinition)))
                            continue;

                        if (type.FullName.Contains("PrivateImplementationDetails")
                            || type.FullName.Contains("DisplayClass")
                            || type.FullName.Contains('<'))
                            continue;

                        if (baseType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
                            && type.IsClass)
                            continue;

                        if (baseType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)
                            && type.IsValueType)
                            continue;

                        if (baseType.GetGenericParameterConstraints().Any(it => !it.IsAssignableFrom(type)))
                            continue;

                        set.Add(type);
                    }
                    catch { }
                }

                genericParameterInheritance.Add(key, set);
            }

            return genericParameterInheritance[key];
        }

        #endregion


        #region IL2CPP IEnumerable / IDictionary

        // Temp fix for IL2CPP until interface support improves

        // IsEnumerable 

        /// <summary>
        /// Returns true if the provided type is an IEnumerable, including Il2Cpp IEnumerables.
        /// </summary>
        public static bool IsEnumerable(Type type) => Instance.Internal_IsEnumerable(type);

        protected virtual bool Internal_IsEnumerable(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        // TryGetEnumerator (list)

        /// <summary>
        /// Attempts to get the <see cref="IEnumerator"/> from the provided <see cref="IEnumerable"/> <paramref name="ienumerable"/>.
        /// </summary>
        public static bool TryGetEnumerator(object ienumerable, out IEnumerator enumerator)
            => Instance.Internal_TryGetEnumerator(ienumerable, out enumerator);

        protected virtual bool Internal_TryGetEnumerator(object list, out IEnumerator enumerator)
        {
            enumerator = (list as IEnumerable).GetEnumerator();
            return true;
        }

        // TryGetEntryType

        /// <summary>
        /// Attempts to get the entry type (the Type of the entries) from the provided <paramref name="enumerableType"/>.
        /// </summary>
        public static bool TryGetEntryType(Type enumerableType, out Type type)
            => Instance.Internal_TryGetEntryType(enumerableType, out type);

        protected virtual bool Internal_TryGetEntryType(Type enumerableType, out Type type)
        {
            // Check for arrays
            if (enumerableType.IsArray)
            {
                type = enumerableType.GetElementType();
                return true;
            }

            // Check for implementation of IEnumerable<T>, IList<T> or ICollection<T>
            foreach (Type t in enumerableType.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    Type typeDef = t.GetGenericTypeDefinition();
                    if (typeDef == typeof(IEnumerable<>) || typeDef == typeof(IList<>) || typeDef == typeof(ICollection<>))
                    {
                        type = t.GetGenericArguments()[0];
                        return true;
                    }
                }
            }

            // Unable to determine any generic element type, just use object.
            type = typeof(object);
            return false;
        }

        // IsDictionary

        /// <summary>
        /// Returns true if the provided type implements IDictionary, or Il2CPP IDictionary.
        /// </summary>
        public static bool IsDictionary(Type type) => Instance.Internal_IsDictionary(type);

        protected virtual bool Internal_IsDictionary(Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type);
        }

        // TryGetEnumerator (dictionary)

        /// <summary>
        /// Try to get a DictionaryEnumerator for the provided IDictionary.
        /// </summary>
        public static bool TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
            => Instance.Internal_TryGetDictEnumerator(dictionary, out dictEnumerator);

        protected virtual bool Internal_TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
        {
            dictEnumerator = EnumerateDictionary((IDictionary)dictionary);
            return true;
        }

        private IEnumerator<DictionaryEntry> EnumerateDictionary(IDictionary dict)
        {
            IDictionaryEnumerator enumerator = dict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return new DictionaryEntry(enumerator.Key, enumerator.Value);
            }
        }

        // TryGetEntryTypes

        /// <summary>
        /// Try to get the Type of Keys and Values in the provided dictionary type.
        /// </summary>
        public static bool TryGetEntryTypes(Type dictionaryType, out Type keys, out Type values)
            => Instance.Internal_TryGetEntryTypes(dictionaryType, out keys, out values);

        protected virtual bool Internal_TryGetEntryTypes(Type dictionaryType, out Type keys, out Type values)
        {
            foreach (Type t in dictionaryType.GetInterfaces())
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    Type[] args = t.GetGenericArguments();
                    keys = args[0];
                    values = args[1];
                    return true;
                }
            }

            keys = typeof(object);
            values = typeof(object);
            return false;
        }

        #endregion
    }
}
