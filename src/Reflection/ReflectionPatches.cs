using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UniverseLib.Utility;

namespace UniverseLib
{
    internal static class ReflectionPatches
    {
        internal static void Init()
        {
            Universe.Patch(typeof(Assembly),
                nameof(Assembly.GetTypes),
                MethodType.Normal,
                new Type[0],
                finalizer: AccessTools.Method(typeof(ReflectionPatches), nameof(Finalizer_Assembly_GetTypes)));
        }

        public static Exception Finalizer_Assembly_GetTypes(Assembly __instance, Exception __exception, ref Type[] __result)
        {
            if (__exception != null)
            {
                if (__exception is ReflectionTypeLoadException rtle)
                {
                    __result = ReflectionUtility.TryExtractTypesFromException(rtle);
                }
                else // It was some other exception, try use GetExportedTypes
                {
                    try
                    {
                        __result = __instance.GetExportedTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        __result = ReflectionUtility.TryExtractTypesFromException(e);
                    }
                    catch
                    {
                        __result = ArgumentUtility.EmptyTypes;
                    }
                }
            }

            return null;
        }
    }
}
