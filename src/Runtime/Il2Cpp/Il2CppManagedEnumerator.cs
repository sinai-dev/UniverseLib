#if CPP
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Il2CppSystem;
using IntPtr = System.IntPtr;
using Type = System.Type;
using ArgumentNullException = System.ArgumentNullException;
using NotSupportedException = System.NotSupportedException;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;
#if INTEROP
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime;
#else
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
#endif

// Credit to Horse/BepInEx for this wrapper.
// https://github.com/BepInEx/BepInEx/tree/master/BepInEx.IL2CPP/Utils/Collections

namespace UniverseLib.Runtime.Il2Cpp
{
    public static class CollectionExtensions
    {
        public static Il2CppIEnumerator WrapToIl2Cpp(this IEnumerator self)
            => new(new Il2CppManagedEnumerator(self).Pointer);
    }

    public class Il2CppManagedEnumerator : Object
    {
        private static readonly Dictionary<Type, System.Func<object, Object>> boxers = new();

        private readonly IEnumerator enumerator;

        static Il2CppManagedEnumerator()
        {
            try
            {
                // Using reflection for this since API is different between BepInEx and the main branch.
                // This method is just obsoleted in BepInEx's branch, but still works.

                // ClassInjector.RegisterTypeInIl2CppWithInterfaces(typeof(Il2CppManagedEnumerator), true, typeof(Il2CppIEnumerator));
#if UNHOLLOWER
                AccessTools.Method(typeof(ClassInjector), "RegisterTypeInIl2CppWithInterfaces", new Type[] { typeof(Type), typeof(bool), typeof(Type[]) })
                    .Invoke(null, new object[] { typeof(Il2CppManagedEnumerator), true, new[] { typeof(Il2CppSystem.Collections.IEnumerator) } });
#else
                ClassInjector.RegisterTypeInIl2Cpp<Il2CppManagedEnumerator>(new RegisterTypeOptions
                {
                    Interfaces = new[] { typeof(Il2CppIEnumerator) }
                });
#endif

            }
            catch (System.Exception ex)
            {
                Universe.LogWarning(ex);
            }
        }

        public Il2CppManagedEnumerator(IntPtr ptr) : base(ptr) { }

        public Il2CppManagedEnumerator(IEnumerator enumerator)
            : base(ClassInjector.DerivedConstructorPointer<Il2CppManagedEnumerator>())
        {
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            ClassInjector.DerivedConstructorBody(this);
        }

        public Object Current => enumerator.Current switch
        {
            Il2CppIEnumerator i => i.Cast<Object>(),
            IEnumerator e => new Il2CppManagedEnumerator(e),
            Object il2cppObj => il2cppObj,
            { } obj => ManagedToIl2CppObject(obj),
            null => null
        };

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();

        private static Object ManagedToIl2CppObject(object obj)
        {
            Type t = obj.GetType();
            if (obj is string s)
                return new Object(IL2CPP.ManagedStringToIl2Cpp(s));
            if (t.IsPrimitive)
                return GetValueBoxer(t)(obj);
            throw new NotSupportedException($"Type {t} cannot be converted directly to an Il2Cpp object");
        }

        private static System.Func<object, Object> GetValueBoxer(Type t)
        {
            if (boxers.TryGetValue(t, out System.Func<object, Object> conv))
                return conv;

            DynamicMethod dm = new($"Il2CppUnbox_{t.FullDescription()}", typeof(Object),
                                       new[] { typeof(object) });
            ILGenerator il = dm.GetILGenerator();
            LocalBuilder loc = il.DeclareLocal(t);
            System.Reflection.FieldInfo classField = typeof(Il2CppClassPointerStore<>).MakeGenericType(t)
                                                              .GetField(nameof(Il2CppClassPointerStore<int>
                                                                                   .NativeClassPtr));
            il.Emit(OpCodes.Ldsfld, classField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Unbox_Any, t);
            il.Emit(OpCodes.Stloc, loc);
            il.Emit(OpCodes.Ldloca, loc);
            il.Emit(OpCodes.Call,
                    typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box)));
            il.Emit(OpCodes.Newobj, typeof(Object).GetConstructor(new[] { typeof(IntPtr) }));
            il.Emit(OpCodes.Ret);

            System.Func<object, Object> converter = dm.CreateDelegate(typeof(System.Func<object, Object>)) as System.Func<object, Object>;
            boxers[t] = converter;
            return converter;
        }
    }
}

#endif