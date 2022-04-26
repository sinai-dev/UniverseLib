#if CPP
using System;
using System.Reflection;
using System.Collections;
using UniverseLib.Utility;

namespace UniverseLib
{
    internal class Il2CppEnumerator : IEnumerator
    {
        readonly object enumerator;
        readonly MethodInfo m_GetEnumerator;

        readonly object instanceForMoveNext;
        readonly MethodInfo m_MoveNext;

        readonly object instanceForCurrent;
        readonly MethodInfo p_Current;

        public object Current => p_Current.Invoke(instanceForCurrent, null);

        public bool MoveNext()
        {
            return (bool)m_MoveNext.Invoke(instanceForMoveNext, null);
        }

        public void Reset() => throw new NotImplementedException();

        public Il2CppEnumerator(object instance, Type type)
        {
            m_GetEnumerator = type.GetMethod("GetEnumerator")
                           ?? type.GetMethod("System_Collections_IEnumerable_GetEnumerator", ReflectionUtility.FLAGS);

            enumerator = m_GetEnumerator.Invoke(
                instance.TryCast(m_GetEnumerator.DeclaringType),
                ArgumentUtility.EmptyArgs);

            if (enumerator == null)
                throw new Exception($"GetEnumerator returned null");

            Type enumeratorType = enumerator.GetActualType();

            m_MoveNext = enumeratorType.GetMethod("MoveNext")
                      ?? enumeratorType.GetMethod("System_Collections_IEnumerator_MoveNext", ReflectionUtility.FLAGS);

            instanceForMoveNext = enumerator.TryCast(m_MoveNext.DeclaringType);

            p_Current = enumeratorType.GetProperty("Current")?.GetGetMethod()
                      ?? enumeratorType.GetMethod("System_Collections_IEnumerator_get_Current", ReflectionUtility.FLAGS);

            instanceForCurrent = enumerator.TryCast(p_Current.DeclaringType);
        }
    }
}

#endif