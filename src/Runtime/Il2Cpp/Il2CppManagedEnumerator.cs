#if CPP
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Il2CppSystem;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using IntPtr = System.IntPtr;
using Type = System.Type;
using ArgumentNullException = System.ArgumentNullException;
using NotSupportedException = System.NotSupportedException;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;

// Credit to Horse/BepInEx for this wrapper.
// https://github.com/BepInEx/BepInEx/tree/master/BepInEx.IL2CPP/Utils/Collections

namespace UniverseLib.Runtime.Il2Cpp
{
    public static class CollectionExtensions
    {
        public static Il2CppIEnumerator WrapToIl2Cpp(this IEnumerator self) 
            => new Il2CppManagedEnumerator(self).TryCast<Il2CppIEnumerator>();
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

                // ClassInjector.RegisterTypeInIl2CppWithInterfaces<Il2CppManagedEnumerator>(typeof(Il2CppIEnumerator));
                AccessTools.Method(typeof(ClassInjector), "RegisterTypeInIl2CppWithInterfaces", new Type[] { typeof(Type[]) })
                    .MakeGenericMethod(typeof(Il2CppManagedEnumerator))
                    .Invoke(null, new[] { new[] { typeof(Il2CppIEnumerator) } });
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

        private static System.Func<object, Object> GetValueBoxer(Type t)
        {
            if (boxers.TryGetValue(t, out var conv))
                return conv;

            var dm = new DynamicMethod($"Il2CppUnbox_{t.FullDescription()}", typeof(Object),
                                       new[] { typeof(object) });
            var il = dm.GetILGenerator();
            var loc = il.DeclareLocal(t);
            var classField = typeof(Il2CppClassPointerStore<>).MakeGenericType(t)
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

            var converter = dm.CreateDelegate(typeof(System.Func<object, Object>)) as System.Func<object, Object>;
            boxers[t] = converter;
            return converter;
        }

        private static Object ManagedToIl2CppObject(object obj)
        {
            var t = obj.GetType();
            if (obj is string s)
                return new Object(IL2CPP.ManagedStringToIl2Cpp(s));
            if (t.IsPrimitive)
                return GetValueBoxer(t)(obj);
            throw new NotSupportedException($"Type {t} cannot be converted directly to an Il2Cpp object");
        }
    }
}

#endif