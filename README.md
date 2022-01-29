# UniverseLib

Library used by [UnityExplorer](https://github.com/sinai-dev/UnityExplorer), [BepInExConfigManager](https://github.com/sinai-dev/BepInExConfigManager) and [MelonPreferencesManager](https://github.com/sinai-dev/MelonPreferencesManager).

## How do I use this? Is there documentation? Examples?

Not at the moment. I made this to handle conflicts between 3 similar tools I have released. It's for making "universal" Unity mods which work on IL2CPP and Mono games. It contains a UI and Input framework, and other tools for making universal mods.

Currently I don't have the time to deal with documentation or making this user-friendly, but it's here if you want to try to use it. You can look at UnityExplorer or my mod config managers to see examples.

Nuget package available [here](https://www.nuget.org/packages/UniverseLib/).

## Acknowledgements

* [Geoffrey Horsington](https://github.com/ghorsington) and [BepInEx](https://github.com/BepInEx) for [ManagedIl2CppEnumerator](https://github.com/BepInEx/BepInEx/blob/master/BepInEx.IL2CPP/Utils/Collections/Il2CppManagedEnumerator.cs) \[[license](https://github.com/BepInEx/BepInEx/blob/master/LICENSE)\], included for IL2CPP coroutine support.
