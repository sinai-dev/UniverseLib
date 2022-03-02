using UniverseLib.UI;

namespace UniverseLib.Config
{
    public struct UniverseLibConfig
    {
        /// <summary>If true, disables UniverseLib from overriding the EventSystem from the game when a UniversalUI is in use.</summary>
        public bool? Disable_EventSystem_Override;
        
        /// <summary>If true, attempts to force-unlock the mouse (<see cref="UnityEngine.Cursor"/>) when a UniversalUI is in use.</summary>
        public bool? Force_Unlock_Mouse;
        
        /// <summary>For IL2CPP games, this should be the full path to a folder containing the Unhollowed assemblies.</summary>
        public string Unhollowed_Modules_Folder;

        /// <summary>If the game does not use an EventSystem and you do not expect there to be any other EventSystems, set this to true.</summary>
        public bool? Disable_Fallback_EventSystem_Search;
        
        /// <summary>If true, GameObjects which are not a child to a <see cref="UIBase"/> can be selected as the selected GameObject by the EventSystem.</summary>
        public bool? Allow_UI_Selection_Outside_UIBase;
    }
}
