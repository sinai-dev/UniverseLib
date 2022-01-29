#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UniverseLib;
using CppType = Il2CppSystem.Type;
using BF = System.Reflection.BindingFlags;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UniverseLib.Config;
using HarmonyLib;
using UniverseLib.Utility;

namespace UniverseLib
{
    public class Il2CppReflection : ReflectionUtility
    {
        protected override void Initialize()
        {
            base.Initialize();

            float start = Time.realtimeSinceStartup;
            TryLoadGameModules();
            Universe.Log($"Loaded Unhollowed modules in {Time.realtimeSinceStartup - start} seconds");

            start = Time.realtimeSinceStartup;
            BuildDeobfuscationCache();
            OnTypeLoaded += TryCacheDeobfuscatedType;
            Universe.Log($"Setup IL2CPP reflection in {Time.realtimeSinceStartup - start} seconds, " +
                $"deobfuscated types count: {obfuscatedToDeobfuscatedTypes.Count}");
        }

#region IL2CPP Extern and pointers

        private static readonly Dictionary<string, IntPtr> cppClassPointers = new();

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        /// <summary>
        /// Returns true if the Type has a corresponding IL2CPP Type.
        /// </summary>
        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        /// <summary>
        /// Returns true if the Type has a corresponding IL2CPP Type, and assigns the IntPtr to the IL2CPP Type to <paramref name="il2cppPtr"/>.
        /// </summary>
        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (!cppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
            {
                il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

                cppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);
            }

            return il2cppPtr != IntPtr.Zero;
        }

#endregion


#region Deobfuscation cache

        private static readonly Dictionary<string, Type> obfuscatedToDeobfuscatedTypes = new();
        private static readonly Dictionary<string, string> deobfuscatedToObfuscatedNames = new();

        private static void BuildDeobfuscationCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                    TryCacheDeobfuscatedType(type);
            }
        }

        private static void TryCacheDeobfuscatedType(Type type)
        {
            try
            {
                if (!type.CustomAttributes.Any())
                    return;

                foreach (var att in type.CustomAttributes)
                {
                    if (att.AttributeType == typeof(ObfuscatedNameAttribute))
                    {
                        string obfuscatedName = att.ConstructorArguments[0].Value.ToString();

                        obfuscatedToDeobfuscatedTypes.Add(obfuscatedName, type);
                        deobfuscatedToObfuscatedNames.Add(type.FullName, obfuscatedName);

                        break;
                    }
                }
            }
            catch { }
        }

        internal override string Internal_ProcessTypeInString(string theString, Type type)
        {
            if (deobfuscatedToObfuscatedNames.TryGetValue(type.FullName, out string obfuscated))
                return theString.Replace(obfuscated, type.FullName);

            return theString;
        }

#endregion

        // Get type by name

        internal override Type Internal_GetTypeByName(string fullName)
        {
            if (obfuscatedToDeobfuscatedTypes.TryGetValue(fullName, out Type deob))
                return deob;

            return base.Internal_GetTypeByName(fullName);
        }


#region Get actual type

        internal override Type Internal_GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();
            try
            {
                if (type.IsGenericType)
                    return type;

                if (IsString(obj))
                    return typeof(string);

                if (IsIl2CppPrimitive(type))
                    return il2cppPrimitivesToMono[type.FullName];

                if (obj is Il2CppSystem.Object cppObject)
                {
                    var cppType = cppObject.GetIl2CppType();

                    // check if type is injected
                    IntPtr classPtr = il2cpp_object_get_class(cppObject.Pointer);
                    if (RuntimeSpecificsStore.IsInjected(classPtr))
                    {
                        // Note: This will fail on injected subclasses.
                        // - {Namespace}.{Class}.{Subclass} would be {Namespace}.{Subclass} when injected.
                        // Not sure on solution yet.
                        return GetTypeByName(cppType.FullName) ?? type;
                    }

                    if (AllTypes.TryGetValue(cppType.FullName, out Type primitive) && primitive.IsPrimitive)
                        return primitive;

                    return GetUnhollowedType(cppType) ?? type;
                }
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception in IL2CPP GetActualType: " + ex);
            }

            return type;
        }

        /// <summary>
        /// Try to get the Unhollowed <see cref="System.Type"/> for the provided <paramref name="cppType"/>.
        /// </summary>
        public static Type GetUnhollowedType(CppType cppType)
        {
            var fullname = cppType.FullName;

            if (obfuscatedToDeobfuscatedTypes.TryGetValue(fullname, out Type deob))
                return deob;

            if (fullname.StartsWith("System."))
                fullname = $"Il2Cpp{fullname}";

            if (!AllTypes.TryGetValue(fullname, out Type monoType))
                Universe.LogWarning($"Failed to get type by name '{fullname}'!");
            return monoType;
        }

#endregion


#region Casting

        internal override object Internal_TryCast(object obj, Type castTo)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            if (type == castTo)
                return obj;

            // from structs
            if (type.IsValueType)
            {
                // from il2cpp primitive to system primitive
                if (IsIl2CppPrimitive(type) && castTo.IsPrimitive)
                {
                    return MakeMonoPrimitive(obj);
                }
                // from system primitive to il2cpp primitive
                else if (IsIl2CppPrimitive(castTo))
                {
                    return MakeIl2CppPrimitive(castTo, obj);
                }
                // from other structs to il2cpp object
                else if (typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
                {
                    return BoxIl2CppObject(obj).TryCast(castTo);
                }
                else
                    return obj;
            }

            // from string to il2cpp.Object / il2cpp.String
            if (obj is string && typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
            {
                return BoxStringToType(obj, castTo);
            }

            // from il2cpp objects...

            if (obj is not Il2CppObjectBase cppObj)
                return obj;

            // from Il2CppSystem.Object to a struct
            if (castTo.IsValueType)
                return UnboxCppObject(cppObj, castTo);
            // or to system string
            else if (castTo == typeof(string))
                return UnboxString(obj);

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

            // Casting from il2cpp object to il2cpp object...

            IntPtr castFromPtr = il2cpp_object_get_class(cppObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, castFromPtr))
                return null;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
            {
                var injectedObj = UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(cppObj.Pointer);
                return injectedObj ?? obj;
            }

            try
            {
                return Activator.CreateInstance(castTo, cppObj.Pointer);
            }
            catch
            {
                return obj;
            }
        }

#endregion


#region Boxing and unboxing ValueTypes

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> unboxMethods = new();

        /// <summary>
        /// Unbox the provided Il2CppSystem.Object to the ValueType <paramref name="toType"/>.
        /// </summary>
        public object UnboxCppObject(Il2CppObjectBase cppObj, Type toType)
        {
            if (!toType.IsValueType)
                return null;

            try
            {
                if (toType.IsEnum)
                {
                    // Check for nullable enums
                    var type = cppObj.GetType();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Il2CppSystem.Nullable<>))
                    {
                        var nullable = cppObj.TryCast(type);
                        var nullableHasValueProperty = type.GetProperty("HasValue");
                        if ((bool)nullableHasValueProperty.GetValue(nullable, null))
                        {
                            // nullable has a value.
                            var nullableValueProperty = type.GetProperty("Value");
                            return Enum.Parse(toType, nullableValueProperty.GetValue(nullable, null).ToString());
                        }
                        // nullable and no current value.
                        return cppObj;
                    }

                    return Enum.Parse(toType, cppObj.ToString());
                }

                // Not enum, unbox with Il2CppObjectBase.Unbox

                var name = toType.AssemblyQualifiedName;

                if (!unboxMethods.ContainsKey(name))
                {
                    unboxMethods.Add(name, typeof(Il2CppObjectBase)
                                                .GetMethod("Unbox")
                                                .MakeGenericMethod(toType));
                }

                return unboxMethods[name].Invoke(cppObj, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception Unboxing Il2Cpp object to struct: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Box the provided Il2Cpp ValueType object into an Il2CppSystem.Object.
        /// </summary>
        public Il2CppSystem.Object BoxIl2CppObject(object value)
        {
            if (value == null)
                return null;

            try
            {
                var type = value.GetType();
                if (!type.IsValueType)
                    return null;

                if (type.IsEnum)
                    return Il2CppSystem.Enum.Parse(Il2CppType.From(type), value.ToString());

                if (type.IsPrimitive && AllTypes.TryGetValue($"Il2Cpp{type.FullName}", out Type cppType))
                    return BoxIl2CppObject(MakeIl2CppPrimitive(cppType, value), cppType);

                return BoxIl2CppObject(value, type);
            }
            catch (Exception ex)
            {
                Universe.LogWarning("Exception in BoxIl2CppObject: " + ex);
                return null;
            }
        }

        private static Il2CppSystem.Object BoxIl2CppObject(object cppStruct, Type structType)
        {
            return AccessTools.Method(structType, "BoxIl2CppObject", ArgumentUtility.EmptyTypes)
                   .Invoke(cppStruct, ArgumentUtility.EmptyArgs)
                   as Il2CppSystem.Object;
        }

        // Helpers for Il2Cpp primitive <-> Mono

        internal static readonly Dictionary<string, Type> il2cppPrimitivesToMono = new()
        {
            { "Il2CppSystem.Boolean", typeof(bool) },
            { "Il2CppSystem.Byte",    typeof(byte) },
            { "Il2CppSystem.SByte",   typeof(sbyte) },
            { "Il2CppSystem.Char",    typeof(char) },
            { "Il2CppSystem.Double",  typeof(double) },
            { "Il2CppSystem.Single",  typeof(float) },
            { "Il2CppSystem.Int32",   typeof(int) },
            { "Il2CppSystem.UInt32",  typeof(uint) },
            { "Il2CppSystem.Int64",   typeof(long) },
            { "Il2CppSystem.UInt64",  typeof(ulong) },
            { "Il2CppSystem.Int16",   typeof(short) },
            { "Il2CppSystem.UInt16",  typeof(ushort) },
            { "Il2CppSystem.IntPtr",  typeof(IntPtr) },
            { "Il2CppSystem.UIntPtr", typeof(UIntPtr) }
        };

        /// <summary>
        /// Returns true if the provided object is actually an Il2Cpp primitive.
        /// </summary>
        public static bool IsIl2CppPrimitive(object obj) => IsIl2CppPrimitive(obj.GetType());

        /// <summary>
        /// Returns true if the provided Type is an Il2Cpp primitive.
        /// </summary>
        public static bool IsIl2CppPrimitive(Type type) => il2cppPrimitivesToMono.ContainsKey(type.FullName);

        /// <summary>
        /// Returns the underlying <c>m_value</c> System primitive from the provided Il2Cpp primitive object.
        /// </summary>
        public object MakeMonoPrimitive(object cppPrimitive)
        {
            return AccessTools.Field(cppPrimitive.GetType(), "m_value").GetValue(cppPrimitive);
        }

        /// <summary>
        /// Creates a new equivalent Il2Cpp primitive object using the provided <paramref name="monoValue"/>.
        /// </summary>
        public object MakeIl2CppPrimitive(Type cppType, object monoValue)
        {
            var cppStruct = Activator.CreateInstance(cppType);
            AccessTools.Field(cppType, "m_value").SetValue(cppStruct, monoValue);
            return cppStruct;
        }

#endregion


#region String boxing/unboxing

        private const string IL2CPP_STRING_FULLNAME = "Il2CppSystem.String";
        private const string STRING_FULLNAME = "System.String";

        /// <summary>
        /// Returns true if the object is a string or Il2CppSystem.String
        /// </summary>
        public bool IsString(object obj)
        {
            if (obj is string || obj is Il2CppSystem.String)
                return true;
        
            if (obj is Il2CppSystem.Object cppObj)
            {
                var type = cppObj.GetIl2CppType();
                return type.FullName == IL2CPP_STRING_FULLNAME || type.FullName == STRING_FULLNAME;
            }

            return false;
        }

        /// <summary>
        /// Box the provided string value into either an Il2CppSystem.Object or Il2CppSystem.String
        /// </summary>
        public object BoxStringToType(object value, Type castTo)
        {
            if (castTo == typeof(Il2CppSystem.String))
                return (Il2CppSystem.String)(value as string);
            else
                return (Il2CppSystem.Object)(value as string);
        }

        /// <summary>
        /// Unbox the provided value from either Il2CppSystem.Object or Il2CppSystem.String into a System.String
        /// </summary>
        public string UnboxString(object value)
        {
            if (value is string s)
                return s;

            s = null;
            if (value is Il2CppSystem.Object cppObject)
                s = cppObject.ToString();
            else if (value is Il2CppSystem.String cppString)
                s = cppString;

            return s;
        }

#endregion


#region Singleton finder

        internal override void Internal_FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
        {
            PropertyInfo pi;
            foreach (var name in possibleNames)
            {
                pi = type.GetProperty(name, flags);
                if (pi != null)
                {
                    var instance = pi.GetValue(null, null);
                    if (instance != null)
                    {
                        instances.Add(instance);
                        return;
                    }
                }
            }

            base.Internal_FindSingleton(possibleNames, type, flags, instances);
        }

#endregion


#region Force-loading game modules

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

        // Force loading all il2cpp modules

        internal void TryLoadGameModules()
        {
            var dir = ConfigManager.Unhollowed_Modules_Folder;
            if (Directory.Exists(dir))
            {
                foreach (var filePath in Directory.GetFiles(dir, "*.dll"))
                    DoLoadModule(filePath);
            }
            else
                Universe.LogWarning($"Expected Unhollowed folder path does not exist: '{dir}'. " +
                    $"If you are using the standalone release, you can specify the Unhollowed modules path when you call CreateInstance().");
        }

        internal bool DoLoadModule(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return false;

            try
            {
                Assembly.LoadFile(fullPath);
                return true;
            }
            catch //(Exception e)
            {
                //UniverseLib.LogWarning($"Failed loading module '{Path.GetFileName(fullPath)}'! {e.ReflectionExToString()}");
                return false;
            }
        }

#endregion


#region IL2CPP IEnumerable and IDictionary

        protected override bool Internal_TryGetEntryType(Type enumerableType, out Type type)
        {
            // Check for system types (not unhollowed)
            if (base.Internal_TryGetEntryType(enumerableType, out type))
                return true;

            // Type is either an IL2CPP enumerable, or its not generic.

            if (type.IsGenericType)
            {
                // Temporary naive solution until IL2CPP interface support improves.
                // This will work fine for most cases, but there are edge cases which would not work.
                type = type.GetGenericArguments()[0];
                return true;
            }

            // Unable to determine entry type
            type = typeof(object);
            return false;
        }

        protected override bool Internal_TryGetEntryTypes(Type type, out Type keys, out Type values)
        {
            if (base.Internal_TryGetEntryTypes(type, out keys, out values))
                return true;

            // Type is either an IL2CPP dictionary, or its not generic.
            if (type.IsGenericType)
            {
                // Naive solution until IL2CPP interfaces improve.
                var args = type.GetGenericArguments();
                if (args.Length == 2)
                {
                    keys = args[0];
                    values = args[1];
                    return true;
                }
            }

            keys = typeof(object);
            values = typeof(object);
            return false;
        }

        // Temp fix until Unhollower interface support improves

        internal static readonly Dictionary<string, MethodInfo> getEnumeratorMethods = new();
        internal static readonly Dictionary<string, EnumeratorInfo> enumeratorInfos = new();
        internal static readonly HashSet<string> notSupportedTypes = new();

        // IEnumerables

        internal static IntPtr cppIEnumerablePointer;

        protected override bool Internal_IsEnumerable(Type type)
        {
            if (base.Internal_IsEnumerable(type))
                return true;

            try
            {
                if (cppIEnumerablePointer == IntPtr.Zero)
                    Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IEnumerable), out cppIEnumerablePointer);

                if (cppIEnumerablePointer != IntPtr.Zero
                    && Il2CppTypeNotNull(type, out IntPtr assignFromPtr)
                    && il2cpp_class_is_assignable_from(cppIEnumerablePointer, assignFromPtr))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        internal class EnumeratorInfo
        {
            internal MethodInfo moveNext;
            internal PropertyInfo current;
        }

        protected override bool Internal_TryGetEnumerator(object list, out IEnumerator enumerator)
        {
            if (list is IEnumerable)
                return base.Internal_TryGetEnumerator(list, out enumerator);

            try
            {
                PrepareCppEnumerator(list, out object cppEnumerator, out EnumeratorInfo info);
                enumerator = EnumerateCppList(info, cppEnumerator);
                return true;
            }
            catch //(Exception ex)
            {
                //UniverseLib.LogWarning($"Exception enumerating IEnumerable: {ex.ReflectionExToString()}");
                enumerator = null;
                return false;
            }
        }

        private static void PrepareCppEnumerator(object list, out object cppEnumerator, out EnumeratorInfo info)
        {
            info = null;
            cppEnumerator = null;
            if (list == null)
                throw new ArgumentNullException("list");

            // Some ugly reflection to use the il2cpp interface for the instance type

            var type = list.GetActualType();
            var key = type.AssemblyQualifiedName;

            if (!getEnumeratorMethods.ContainsKey(key))
            {
                var method = type.GetMethod("GetEnumerator")
                             ?? type.GetMethod("System_Collections_IEnumerable_GetEnumerator", FLAGS);
                getEnumeratorMethods.Add(key, method);

                // ensure the enumerator type is supported
                try
                {
                    var test = getEnumeratorMethods[key].Invoke(list, null);
                    test.GetActualType().GetMethod("MoveNext").Invoke(test, null);
                }
                catch (Exception ex)
                {
                    Universe.Log($"IEnumerable failed to enumerate: {ex}");
                    notSupportedTypes.Add(key);
                }
            }

            if (notSupportedTypes.Contains(key))
                throw new NotSupportedException($"The IEnumerable type '{type.FullName}' does not support MoveNext.");

            cppEnumerator = getEnumeratorMethods[key].Invoke(list, null);
            var enumeratorType = cppEnumerator.GetActualType();

            var enumInfoKey = enumeratorType.AssemblyQualifiedName;

            if (!enumeratorInfos.ContainsKey(enumInfoKey))
            {
                enumeratorInfos.Add(enumInfoKey, new EnumeratorInfo
                {
                    current = enumeratorType.GetProperty("Current"),
                    moveNext = enumeratorType.GetMethod("MoveNext"),
                });
            }

            info = enumeratorInfos[enumInfoKey];
        }

        internal static IEnumerator EnumerateCppList(EnumeratorInfo info, object enumerator)
        {
            // Yield and return the actual entries
            while ((bool)info.moveNext.Invoke(enumerator, null))
                yield return info.current.GetValue(enumerator);
        }

        // IDictionary

        internal static IntPtr cppIDictionaryPointer;

        protected override bool Internal_IsDictionary(Type type)
        {
            if (base.Internal_IsDictionary(type))
                return true;
        
            try
            {
                if (cppIDictionaryPointer == IntPtr.Zero)
                    if (!Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IDictionary), out cppIDictionaryPointer))
                        return false;

                if (Il2CppTypeNotNull(type, out IntPtr classPtr)
                    && il2cpp_class_is_assignable_from(cppIDictionaryPointer, classPtr))
                    return true;
            }
            catch { }
        
            return false;
        }

        protected override bool Internal_TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
        {
            if (dictionary is IDictionary)
                return base.Internal_TryGetDictEnumerator(dictionary, out dictEnumerator);

            try
            {
                var type = dictionary.GetActualType();

                if (typeof(Il2CppSystem.Collections.Hashtable).IsAssignableFrom(type))
                {
                    dictEnumerator = EnumerateCppHashTable(dictionary.TryCast<Il2CppSystem.Collections.Hashtable>());
                    return true;
                }

                var keys = type.GetProperty("Keys").GetValue(dictionary, null);

                var keyCollType = keys.GetActualType();
                var cacheKey = keyCollType.AssemblyQualifiedName;
                if (!getEnumeratorMethods.ContainsKey(cacheKey))
                {
                    var method = keyCollType.GetMethod("GetEnumerator")
                                 ?? keyCollType.GetMethod("System_Collections_IDictionary_GetEnumerator", FLAGS);
                    getEnumeratorMethods.Add(cacheKey, method);

                    // test support
                    try
                    {
                        var test = getEnumeratorMethods[cacheKey].Invoke(keys, null);
                        test.GetActualType().GetMethod("MoveNext").Invoke(test, null);
                    }
                    catch (Exception ex)
                    {
                        Universe.Log($"IDictionary failed to enumerate: {ex}");
                        notSupportedTypes.Add(cacheKey);
                    }
                }

                if (notSupportedTypes.Contains(cacheKey))
                    throw new Exception($"The IDictionary type '{type.FullName}' does not support MoveNext.");

                var keyEnumerator = getEnumeratorMethods[cacheKey].Invoke(keys, null);
                var keyInfo = new EnumeratorInfo
                {
                    current = keyEnumerator.GetActualType().GetProperty("Current"),
                    moveNext = keyEnumerator.GetActualType().GetMethod("MoveNext"),
                };

                var values = type.GetProperty("Values").GetValue(dictionary, null);
                var valueEnumerator = values.GetActualType().GetMethod("GetEnumerator").Invoke(values, null);
                var valueInfo = new EnumeratorInfo
                {
                    current = valueEnumerator.GetActualType().GetProperty("Current"),
                    moveNext = valueEnumerator.GetActualType().GetMethod("MoveNext"),
                };

                dictEnumerator = EnumerateCppDict(keyInfo, keyEnumerator, valueInfo, valueEnumerator);
                return true;
            }
            catch //(Exception ex)
            {
                //UniverseLib.LogWarning($"Exception enumerating IDictionary: {ex.ReflectionExToString()}");
                dictEnumerator = null;
                return false;
            }
        }

        internal static IEnumerator<DictionaryEntry> EnumerateCppDict(EnumeratorInfo keyInfo, object keyEnumerator, 
            EnumeratorInfo valueInfo, object valueEnumerator)
        {
            while ((bool)keyInfo.moveNext.Invoke(keyEnumerator, null))
            {
                valueInfo.moveNext.Invoke(valueEnumerator, null);

                var key = keyInfo.current.GetValue(keyEnumerator, null);
                var value = valueInfo.current.GetValue(valueEnumerator, null);

                yield return new DictionaryEntry(key, value);
            }
        }

        internal static IEnumerator<DictionaryEntry> EnumerateCppHashTable(Il2CppSystem.Collections.Hashtable hashtable)
        {
            for (int i = 0; i < hashtable.buckets.Count; i++)
            {
                var bucket = hashtable.buckets[i];
                if (bucket == null || bucket.key == null)
                    continue;

                yield return new DictionaryEntry(bucket.key, bucket.val);
            }
        }

#endregion

    }
}

#endif