using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UniverseLib.Runtime
{
    /// <summary>
    /// Handles getting/setting arbitrary field/property members which may have different names or member types in different Unity versions.
    /// </summary>
    /// <typeparam name="TClass">The containing Type for the member.</typeparam>
    /// <typeparam name="TValue">The Type of the value for the member.</typeparam>
    public class AmbiguousMemberHandler<TClass, TValue>
    {
        public readonly MemberInfo member;
        public readonly MemberTypes memberType;

        public AmbiguousMemberHandler(bool canWrite, bool canRead, params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                if (typeof(TClass).GetProperty(name, AccessTools.all) is PropertyInfo pi 
                    && typeof(TValue).IsAssignableFrom(pi.PropertyType)
                    && (!canWrite || pi.CanWrite)
                    && (!canRead || pi.CanRead))
                {
                    member = pi;
                    memberType = MemberTypes.Property;
                    break;
                }
                if (typeof(TClass).GetField(name, AccessTools.all) is FieldInfo fi 
                    && typeof(TValue).IsAssignableFrom(fi.FieldType)
                    && (!canWrite || !(fi.IsLiteral && !fi.IsInitOnly))) // (don't need to write or is not constant)
                {
                    member = fi;
                    memberType = MemberTypes.Field;
                    break;
                }
            }

            //if (member == null)
            //    Universe.LogWarning($"AmbigiousMemberHandler could not find any member on
            //      {typeof(TClass).Name} from possibleNames: {string.Join(", ", possibleNames)}");
            //else
            //    Universe.Log($"Resolved AmbiguousMemberHandler: {member}");
        }

        /// <summary>
        /// Gets the value of an instance member from the provided instance.
        /// </summary>
        /// <param name="instance">The instance to get from.</param>
        /// <returns>The value from the member, if successful.</returns>
        public TValue GetValue(object instance) 
            => DoGetValue(instance);

        /// <summary>
        /// Gets the value of a static member.
        /// </summary>
        /// <returns>The value from the member, if successful.</returns>
        public TValue GetValue() 
            => DoGetValue(null);

        private TValue DoGetValue(object instance)
        {
            if (member == null)
                return default;

            try
            {
                object value = memberType switch
                {
                    MemberTypes.Property => (member as PropertyInfo).GetValue(instance, null),
                    MemberTypes.Field => (member as FieldInfo).GetValue(instance),
                    _ => throw new NotImplementedException()
                };

                return value == null ? default : (TValue)value;
            }
            catch // (Exception ex)
            {
                // Universe.LogWarning($"Exception getting value from member {member}: {ex}");
                return default;
            }
        }

        /// <summary>
        /// Sets the value of an instance member to the instance.
        /// </summary>
        /// <param name="instance">The instance to set to.</param>
        /// <param name="value">The value to set to the instance.</param>
        public void SetValue(object instance, TValue value)
            => DoSetValue(instance, value);

        /// <summary>
        /// Sets the value of a static member.
        /// </summary>
        /// <param name="value">The value to set to the instance.</param>
        public void SetValue(TValue value)
            => DoSetValue(null, value);

        void DoSetValue(object instance, TValue value)
        {
            if (member == null)
                return;

            try
            {
                switch (memberType)
                {
                    case MemberTypes.Property:
                        (member as PropertyInfo).SetValue(instance, value, null);
                        break;

                    case MemberTypes.Field:
                        (member as FieldInfo).SetValue(instance, value);
                        break;
                }
            }
            catch // (Exception ex)
            {
                // Universe.LogWarning($"Exception setting value '{value}' to member {member.DeclaringType.Name}.{member}: {ex}");
            }
        }
    }
}
