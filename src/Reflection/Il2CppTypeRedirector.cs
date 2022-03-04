#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniverseLib.Reflection
{
    // This class exists to fix a bug(?) with Unhollower, where "Il2CppSystem" types are returned as the equivalent "System" type.
    // Specifically, this is for Generic Types, as all other types should be handled by the GetUnhollowedType method.
    // It does not replace System.String or any System primitive types, since "fixing" those seems to be incorrect behaviour.
    internal static class Il2CppTypeRedirector
    {
        static readonly Dictionary<string, string> redirectors = new();

        public static string GetAssemblyQualifiedName(Il2CppSystem.Type type)
        {
            var sb = new StringBuilder();
            ProcessType(sb, type);
            return sb.ToString();
        }

        static string ProcessType(StringBuilder sb, Il2CppSystem.Type type)
        {
            if (type.IsPrimitive || type.FullName == "System.String")
                return type.AssemblyQualifiedName;

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                if (type.FullName.StartsWith("System."))
                    sb.Append("Il2Cpp");

                sb.Append(type.Namespace)
                  .Append('.');
            }

            int start = sb.Length;
            var declaring = type.DeclaringType;
            while (declaring != null)
            {
                sb.Insert(start, $"{declaring.Name}+");
                declaring = declaring.DeclaringType;
            }

            sb.Append(type.Name);

            if (type.IsConstructedGenericType)
            {
                Il2CppSystem.Type[] genericArgs = type.GetGenericArguments();

                // Process and append each type argument (recursive)
                sb.Append('[');
                int i = 0;
                foreach (var typeArg in genericArgs)
                {
                    sb.Append('[');
                    sb.Append(ProcessType(sb, typeArg));
                    sb.Append(']');
                    i++;
                    if (i < genericArgs.Length)
                        sb.Append(',');
                }
                sb.Append(']');
            }

            // Append the assembly signature
            sb.Append(", ");

            if (type.FullName.StartsWith("System."))
            {
                if (!redirectors.ContainsKey(type.Assembly.FullName) && !TryRedirectSystemType(type))
                {
                    // No redirect found for type?
                    Universe.LogWarning($"No Il2CppSystem redirect found for system type: {type.FullName}");
                    return sb.Append(type.Assembly.FullName).ToString();
                }

                // Type redirect was set up
                sb.Append(redirectors[type.Assembly.FullName]);
            }
            else // no redirect required
                sb.Append(type.Assembly.FullName);

            return sb.ToString();
        }

        static bool TryRedirectSystemType(Il2CppSystem.Type type)
        {
            if (type.IsConstructedGenericType)
                type = type.GetGenericTypeDefinition();

            if (ReflectionUtility.AllTypes.TryGetValue($"Il2Cpp{type.FullName}", out Type il2cppType))
            {
                redirectors.Add(type.Assembly.FullName, il2cppType.Assembly.FullName);
                return true;
            }

            return false;
        }
    }
}

#endif
