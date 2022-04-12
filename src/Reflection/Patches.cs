using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UniverseLib.Utility;

namespace UniverseLib
{
    public static class ReflectionPatches
    {
        public static void Init()
        {
            try
            {
                MethodInfo method = typeof(Assembly).GetMethod(nameof(Assembly.GetTypes), new Type[0]);
                PatchProcessor processor = Universe.Harmony.CreateProcessor(method);
                processor.AddFinalizer(typeof(ReflectionPatches).GetMethod(nameof(Assembly_GetTypes)));
                processor.Patch();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception setting up Reflection patch: {ex}");
            }
        }

        public static Exception Assembly_GetTypes(Assembly __instance, Exception __exception, ref Type[] __result)
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
