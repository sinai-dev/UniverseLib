using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniverseLib.UI;

namespace UniverseLib.Config
{
    /// <summary>
    /// Contains properties used by UniverseLib to act as a "config", although it is not really a full config implementation. Changing 
    /// property values in this class has a direct and immediate effect on UniverseLib.
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// Applies the values from the provided <paramref name="config"/> which are not null.
        /// </summary>
        public static void LoadConfig(UniverseLibConfig config)
        {
            if (config.Disable_EventSystem_Override != null)
                Disable_EventSystem_Override = config.Disable_EventSystem_Override.Value;

            if (config.Force_Unlock_Mouse != null)
                Force_Unlock_Mouse = config.Force_Unlock_Mouse.Value;

            if (!string.IsNullOrEmpty(config.Unhollowed_Modules_Folder))
                Unhollowed_Modules_Folder = config.Unhollowed_Modules_Folder;

            if (config.Disable_Fallback_EventSystem_Search != null)
                Disable_Fallback_EventSystem_Search = config.Disable_Fallback_EventSystem_Search.Value;

            if (config.Allow_UI_Selection_Outside_UIBase != null)
                Allow_UI_Selection_Outside_UIBase = config.Allow_UI_Selection_Outside_UIBase.Value;
        }

        /// <summary>If true, disables UniverseLib from overriding the EventSystem from the game when a UniversalUI is in use.</summary>
        public static bool Disable_EventSystem_Override { get; set; }

        /// <summary>If true, attempts to force-unlock the mouse (<see cref="UnityEngine.Cursor"/>) when a UniversalUI is in use.</summary>
        public static bool Force_Unlock_Mouse { get; set; }

        /// <summary>For IL2CPP games, this should be the full path to a folder containing the Unhollowed assemblies.
        /// This property is only used during the intial startup process.</summary>
        public static string Unhollowed_Modules_Folder { get; set; }

        /// <summary>If the game does not use an EventSystem and you do not expect there to be any other EventSystems, set this to true.</summary>
        public static bool Disable_Fallback_EventSystem_Search { get; set; }

        /// <summary>If true, GameObjects which are not a child to a <see cref="UIBase"/> can be selected as the selected GameObject by the EventSystem.</summary>
        public static bool Allow_UI_Selection_Outside_UIBase { get; set; }
    }
}
