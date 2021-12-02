using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniverseLib.Config
{
    public struct UUConfig
    {
        public bool? Disable_EventSystem_Override;
        public bool? Force_Unlock_Mouse;
        public string Unhollowed_Modules_Folder;
    }

    public static class ConfigManager
    {
        public static void LoadConfig(UUConfig config)
        {
            if (config.Disable_EventSystem_Override != null)
                Disable_EventSystem_Override = config.Disable_EventSystem_Override.Value;

            if (config.Force_Unlock_Mouse != null)
                Force_Unlock_Mouse = config.Force_Unlock_Mouse.Value;

            if (!string.IsNullOrEmpty(config.Unhollowed_Modules_Folder))
                Unhollowed_Modules_Folder = config.Unhollowed_Modules_Folder;
        }

        public static bool Disable_EventSystem_Override;
        public static bool Force_Unlock_Mouse;
        public static string Unhollowed_Modules_Folder;
    }
}
