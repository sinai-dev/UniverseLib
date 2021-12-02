# UniverseLib

Library used by [UnityExplorer](https://github.com/sinai-dev/UnityExplorer), [BepInExConfigManager](https://github.com/sinai-dev/BepInExConfigManager) and [MelonPreferencesManager](https://github.com/sinai-dev/MelonPreferencesManager).

* Contains common implementations for creating UI-driven plugins which target IL2CPP and Mono Unity games.
* **No documentation or guaranteed stability**, this is mainly just for my own personal use, but you can use it if you wish.
* NuGet package available [here](https://www.nuget.org/packages/UniverseLib/)

Features:
* UI framework (using `Unity.UI`)
* Input framework (supports both Legacy and InputSystem)
* Runtime helpers for normalizing differences in Unity API between IL2CPP and Mono (Coroutines, etc)
* IL2CPP helper libraries (ICalls, IL2CPP reflection utility, etc)
* Various misc utility such as Reflection helpers, Signature Highlighting, Parsing, etc.

## Basic example

Without proper documentation, here is an extremely basic example for using this library.

```csharp
using System.IO;
using UnityEngine;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;

public class ExampleClass
{
    public static UIBase MyUI;
    public const string MyGUID = "com.me.mymod";

    private static void MyInit()
    {
        // Required initialization
        Universe.Init(1f, LateInit, Log, new UniverseLib.Config.UUConfig
        {
            Disable_EventSystem_Override = false, // To disable the EventSystem override if need be
            Force_Unlock_Mouse = true, // Should generally be true. Can adjust on the fly with ConfigManager.Force_Unlock_Mouse
            Unhollowed_Modules_Folder = Path.Combine("SomeDirectory", "unhollowed") // path to Unhollowed libs (for IL2CPP)
        });
    }

    private static void LateInit()
    {
        // Invoked after UniverseLib finishes initialization, after your startup delay value.

        // Create a UI on the UniverseLib canvas (required to use UI features properly)
        MyUI = UniversalUI.RegisterUI(MyGUID, Update);
        // Create objects with UIFactory on MyUI.RootObject as desired.
        var somePanel = UIFactory.CreatePanel("TestPanel", MyUI.RootObject, out GameObject panelContentHolder);
        var panelRect = somePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.3f);
        panelRect.anchorMax = new Vector2(0.8f, 0.8f);
        // etc...
    }

    private static void Update()
    {
        // Example of using InputManager and toggling our UI active state.
        if (InputManager.GetKeyDown(KeyCode.F5))
        {
            UniversalUI.SetUIActive(MyGUID, !MyUI.Enabled);
        }
    }

    private static void Log(string log, LogType logType)
    {
        // To handle log messages from UniverseLib
    }
}
```

## Acknowledgements

* [HerpDerpenstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/6cc958ec23b5e2e8453a73bc2e0d5aa353d4f0d1/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) \[[license](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md)\], they were included for standalone IL2CPP coroutine support.

