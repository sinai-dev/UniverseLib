#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UniverseLib;
using BF = System.Reflection.BindingFlags;
using UnityEngine;
using UniverseLib.Config;
using HarmonyLib;
using UniverseLib.Utility;
using System.Text.RegularExpressions;
using UniverseLib.Reflection;
using System.Diagnostics;
using UniverseLib.Runtime.Il2Cpp;
#if INTEROP
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Common.Attributes;
#endif
#if UNHOLLOWER
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnhollowerBaseLib.Runtime;
using UnhollowerBaseLib.Attributes;
#endif

namespace UniverseLib
{
    public class Il2CppReflection : ReflectionUtility
    {
        protected override void Initialize()
        {
            base.Initialize();
            Initializing = true;

            UniversalBehaviour.Instance.StartCoroutine(InitCoroutine().WrapToIl2Cpp());
        }

        internal Stopwatch initStopwatch = new();

        IEnumerator InitCoroutine()
        {
            initStopwatch.Start();
            Stopwatch sw = new();
            sw.Start();

            IEnumerator coro = TryLoadGameModules();
            while (coro.MoveNext())
                yield return null;

            Universe.Log($"Loaded Unhollowed modules in {sw.ElapsedMilliseconds * 0.001f} seconds.");

            sw.Reset();
            sw.Start();

            BuildDeobfuscationCache();

            Universe.Log($"Setup deobfuscation cache in {sw.ElapsedMilliseconds * 0.001f} seconds.");

            OnTypeLoaded += TryCacheDeobfuscatedType;

            Initializing = false;
        }

        internal override Type Internal_GetTypeByName(string fullName)
        {
            if (obfuscatedToDeobfuscatedTypes.TryGetValue(fullName, out Type deob))
                return deob;

            return base.Internal_GetTypeByName(fullName);
        }


#region Get Actual type

        internal override Type Internal_GetActualType(object obj)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            try
            {
                if (il2cppPrimitivesToMono.TryGetValue(type.FullName, out Type systemPrimitive))
                    return systemPrimitive;

                if (obj is Il2CppObjectBase cppBase)
                {
                    // Don't need to cast ArrayBase
                    if (type.BaseType.IsGenericType 
                        && type.BaseType.GetGenericTypeDefinition() == typeof(Il2CppArrayBase<>))
                        return type;

                    IntPtr classPtr = IL2CPP.il2cpp_object_get_class(cppBase.Pointer);

                    Il2CppSystem.Type cppType;
                    if (obj is Il2CppSystem.Object cppObject)
                        cppType = cppObject.GetIl2CppType();
                    else
                        cppType = Il2CppType.TypeFromPointer(classPtr);

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
        public static Type GetUnhollowedType(Il2CppSystem.Type cppType)
        {
            if (cppType.IsArray)
                return GetArrayBaseForArray(cppType);

            // Check for primitives (Unhollower will return "System.*" for Il2CppSystem types.
            if (AllTypes.TryGetValue(cppType.FullName, out Type primitive) && primitive.IsPrimitive)
                return primitive;

            if (IsString(cppType))
                return typeof(string);

            string fullname = cppType.FullName;

            if (obfuscatedToDeobfuscatedTypes.TryGetValue(fullname, out Type deob))
                return deob;

            // An Il2CppType cannot ever be a System type.
            // Unhollower returns Il2CppSystem types and System for some reason.
            // Let's just manually fix that.
            if (fullname.StartsWith("System."))
                fullname = $"Il2Cpp{fullname}";

            if (!AllTypes.TryGetValue(fullname, out Type monoType))
            {
                // If it's not in our dictionary, it's most likely a bound generic type.
                // Let's use GetType with the AssemblyQualifiedName, and fix System.* types to be Il2CppSystem.*
                string asmQualName;
                try
                {
                    asmQualName = Il2CppTypeRedirector.GetAssemblyQualifiedName(cppType);
                }
                catch
                {
                    asmQualName = cppType.AssemblyQualifiedName;
                }

                // Some il2cpp types may still have the generic </> characters in their name for some reason?
                for (int i = 0; i < asmQualName.Length; i++)
                {
                    char current = asmQualName[i];
                    if (current == '<' || current == '>')
                    {
                        asmQualName = asmQualName.Remove(i, 1);
                        asmQualName = asmQualName.Insert(i, "_");
                    }
                }

                monoType = Type.GetType(asmQualName);

                if (monoType == null)
                    Universe.LogWarning($"Failed to get Unhollowed type from '{asmQualName}' (originally '{cppType.AssemblyQualifiedName}')!");
            }

            return monoType;
        }

        internal static Type GetArrayBaseForArray(Il2CppSystem.Type cppType)
        {
            Type elementType = GetUnhollowedType(cppType.GetElementType());

            if (elementType == null)
                throw new Exception($"Could not get unhollowed Element type for Array: {cppType.FullName}");

            if (elementType.IsValueType)
                return typeof(Il2CppStructArray<>).MakeGenericType(elementType);
            else if (elementType == typeof(string))
                return typeof(Il2CppStringArray);
            else
                return typeof(Il2CppReferenceArray<>).MakeGenericType(elementType);
        }

#endregion


#region Casting

        internal override object Internal_TryCast(object obj, Type toType)
        {
            if (obj == null)
                return null;

            Type fromType = obj.GetType();

            if (fromType == toType)
                return obj;

            // from structs...
            if (fromType.IsValueType)
            {
                // from il2cpp primitive to system primitive
                if (IsIl2CppPrimitive(fromType) && toType.IsPrimitive)
                    return MakeMonoPrimitive(obj);
                
                // ...to il2cpp primitive
                if (IsIl2CppPrimitive(toType))
                    return MakeIl2CppPrimitive(toType, obj);
                
                // ...to il2cpp object
                if (typeof(Il2CppSystem.Object).IsAssignableFrom(toType))
                    return BoxIl2CppObject(obj).TryCast(toType);
                
                // else just return the object, no special casting should be required
                return obj;
            }

            // from system.string to il2cpp.Object
            if (obj is string && typeof(Il2CppSystem.Object).IsAssignableFrom(toType))
                return BoxStringToType(obj, toType);

            // if the object is not an il2cpp object just return the managed object and let the runtime cast it.
            // Note: This WILL catch something cast to, say, System.Object which is actually any kind of Il2CppSystem.Object underneath.
            if (obj is not Il2CppObjectBase cppObj)
                return obj;

            // from il2cpp objects...

            // ...to a struct
            if (toType.IsValueType)
                return UnboxCppObject(cppObj, toType);

            // ...to system string
            if (toType == typeof(string))
                return UnboxString(obj);

            // ... to another il2cpp object
            if (toType.IsSubclassOf(typeof(Il2CppObjectBase)))
            {
                if (!Il2CppTypeNotNull(toType, out IntPtr castToPtr))
                    return obj;

                // Casting from il2cpp object to il2cpp object...

                IntPtr castFromPtr = IL2CPP.il2cpp_object_get_class(cppObj.Pointer);

                if (!IL2CPP.il2cpp_class_is_assignable_from(castToPtr, castFromPtr))
                    return obj;

                if (RuntimeSpecificsStore.IsInjected(castToPtr)
                    && ClassInjectorBase.GetMonoObjectFromIl2CppPointer(cppObj.Pointer) is object monoObject)
                    return monoObject;

                try
                {
                    return Activator.CreateInstance(toType, cppObj.Pointer);
                }
                catch
                {
                    return obj;
                }
            }
            // else if not casting to il2cpp object, just return the object.
            else
                return obj;
        }

#endregion


#region Boxing and unboxing ValueTypes

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> unboxMethods = new();

        /// <summary>
        /// Unbox the provided Il2CppSystem.Object to the ValueType <paramref name="toType"/>.
        /// </summary>
        public static object UnboxCppObject(Il2CppObjectBase cppObj, Type toType)
        {
            if (!toType.IsValueType)
                return null;

            try
            {
                if (toType.IsEnum)
                {
                    // Check for nullable enums
                    Type type = cppObj.GetType();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Il2CppSystem.Nullable<>))
                    {
                        object nullable = cppObj.TryCast(type);
                        PropertyInfo nullableHasValueProperty = type.GetProperty("HasValue");
                        if ((bool)nullableHasValueProperty.GetValue(nullable, null))
                        {
                            // nullable has a value.
                            PropertyInfo nullableValueProperty = type.GetProperty("Value");
                            return Enum.Parse(toType, nullableValueProperty.GetValue(nullable, null).ToString());
                        }
                        // nullable and no current value.
                        return cppObj;
                    }

                    return Enum.Parse(toType, cppObj.TryCast<Il2CppSystem.Enum>().ToString());
                }

                // Not enum, unbox with Il2CppObjectBase.Unbox

                string name = toType.AssemblyQualifiedName;

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
        public static Il2CppSystem.Object BoxIl2CppObject(object value)
        {
            if (value == null)
                return null;

            try
            {
                Type type = value.GetType();
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
        public static object MakeMonoPrimitive(object cppPrimitive)
        {
            return AccessTools.Field(cppPrimitive.GetType(), "m_value").GetValue(cppPrimitive);
        }

        /// <summary>
        /// Creates a new equivalent Il2Cpp primitive object using the provided <paramref name="monoValue"/>.
        /// </summary>
        public static object MakeIl2CppPrimitive(Type cppType, object monoValue)
        {
            object cppStruct = Activator.CreateInstance(cppType);
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
        public static bool IsString(object obj)
        {
            if (obj is string || obj is Il2CppSystem.String)
                return true;
        
            if (obj is Il2CppSystem.Object cppObj)
            {
                Il2CppSystem.Type type = cppObj.GetIl2CppType();
                return type.FullName == IL2CPP_STRING_FULLNAME || type.FullName == STRING_FULLNAME;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the type is string or Il2CppSystem.String
        /// </summary>
        public static bool IsString(Type type)
        {
            return type == typeof(string) || type == typeof(Il2CppSystem.String);
        }

        /// <summary>
        /// Returns true if the type is string or Il2CppSystem.String
        /// </summary>
        public static bool IsString(Il2CppSystem.Type cppType)
        {
            return cppType.FullName == STRING_FULLNAME || cppType.FullName == IL2CPP_STRING_FULLNAME;
        }

        /// <summary>
        /// Box the provided string value into either an Il2CppSystem.Object or Il2CppSystem.String
        /// </summary>
        public static object BoxStringToType(object value, Type castTo)
        {
            if (castTo == typeof(Il2CppSystem.String))
                return (Il2CppSystem.String)(value as string);
            else
                return (Il2CppSystem.Object)(value as string);
        }

        /// <summary>
        /// Unbox the provided value from either Il2CppSystem.Object or Il2CppSystem.String into a System.String
        /// </summary>
        public static string UnboxString(object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (value is string s)
                return s;

            if (value is not Il2CppSystem.Object cppObject)
                throw new NotSupportedException($"Unable to unbox string from type {value.GetActualType().FullName}.");

            // I don't really know why this needs to be done this way.
            // Doing it in any other order can give corrupt or null results.

            // Try cast it to Il2CppSystem.String, and then cast that to System.String
            string ret = (string)(cppObject as Il2CppSystem.String);
            // If that fails, just do ToString()
            if (string.IsNullOrEmpty(ret))
                ret = cppObject.ToString();
            return ret;
        }

#endregion


#region IL2CPP Extern and pointers

        private static readonly Dictionary<string, IntPtr> cppClassPointers = new();

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
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in asm.GetTypes())
                    TryCacheDeobfuscatedType(type);
            }
        }

        private static void TryCacheDeobfuscatedType(Type type)
        {
            try
            {
                if (!type.CustomAttributes.Any())
                    return;

                foreach (CustomAttributeData att in type.CustomAttributes)
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


#region Singleton finder

        internal override void Internal_FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
        {
            PropertyInfo pi;
            foreach (string name in possibleNames)
            {
                pi = type.GetProperty(name, flags);
                if (pi != null)
                {
                    object instance = pi.GetValue(null, null);
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

        internal IEnumerator TryLoadGameModules()
        {
            string dir = ConfigManager.Unhollowed_Modules_Folder;
            if (Directory.Exists(dir))
            {
                foreach (string filePath in Directory.GetFiles(dir, "*.dll"))
                {
                    if (initStopwatch.ElapsedMilliseconds > 10)
                    {
                        yield return null;
                        initStopwatch.Reset();
                        initStopwatch.Start();
                    }

                    DoLoadModule(filePath);
                }
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
                //Universe.Log($"Loading assembly '{Path.GetFileName(fullPath)}'");
                Assembly.LoadFrom(fullPath);
                return true;
            }
            catch
            {
                //Universe.LogWarning($"Failed loading module '{Path.GetFileName(fullPath)}'! {e.ReflectionExToString()}");
                return false;
            }
        }

#endregion


#region IL2CPP IEnumerable and IDictionary

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
                    && IL2CPP.il2cpp_class_is_assignable_from(cppIEnumerablePointer, assignFromPtr))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

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

        protected override bool Internal_TryGetEnumerator(object instance, out IEnumerator enumerator)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (instance is IEnumerable)
                return base.Internal_TryGetEnumerator(instance, out enumerator);

            enumerator = null;
            Type type = instance.GetActualType();

            try
            {
                enumerator = new Il2CppEnumerator(instance, type);
                return true;
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"IEnumerable of type {type.FullName} failed to get enumerator: {ex}");
                return false;
            }
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
                    && IL2CPP.il2cpp_class_is_assignable_from(cppIDictionaryPointer, classPtr))
                    return true;
            }
            catch { }

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
                Type[] args = type.GetGenericArguments();
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

        protected override bool Internal_TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
        {
            if (dictionary is IDictionary)
                return base.Internal_TryGetDictEnumerator(dictionary, out dictEnumerator);

            try
            {
                Type type = dictionary.GetActualType();

                if (typeof(Il2CppSystem.Collections.Hashtable).IsAssignableFrom(type))
                {
                    dictEnumerator = EnumerateCppHashTable(dictionary.TryCast<Il2CppSystem.Collections.Hashtable>());
                    return true;
                }

                PropertyInfo p_Keys = type.GetProperty("Keys");
                object keys = p_Keys.GetValue(dictionary.TryCast(p_Keys.DeclaringType), null);
                PropertyInfo p_Values = type.GetProperty("Values");
                object values = p_Values.GetValue(dictionary.TryCast(p_Values.DeclaringType), null);

                Il2CppEnumerator keysEnumerator = new(keys, keys.GetActualType());
                Il2CppEnumerator valuesEnumerator = new(values, values.GetActualType());

                dictEnumerator = new Il2CppDictionary(keysEnumerator, valuesEnumerator);
                
                return true;
            }
            catch (Exception ex)
            {
                Universe.Log($"IDictionary failed to enumerate: {ex}");
                dictEnumerator = null;
                return false;
            }
        }

        static IEnumerator<DictionaryEntry> EnumerateCppHashTable(Il2CppSystem.Collections.Hashtable hashtable)
        {
            for (int i = 0; i < hashtable.buckets.Count; i++)
            {
                Il2CppSystem.Collections.Hashtable.bucket bucket = hashtable.buckets[i];
                if (bucket == null || bucket.key == null)
                    continue;

                yield return new DictionaryEntry(bucket.key, bucket.val);
            }
        }

#endregion

    }
}

#endif