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
