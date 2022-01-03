# UniverseLib

Library used by [UnityExplorer](https://github.com/sinai-dev/UnityExplorer), [BepInExConfigManager](https://github.com/sinai-dev/BepInExConfigManager) and [MelonPreferencesManager](https://github.com/sinai-dev/MelonPreferencesManager).

## How do I use this? Is there documentation? Examples?

Not at the moment. I made this to handle conflicts between 3 similar tools I have released. It's for making "universal" Unity mods which work on IL2CPP and Mono games. It contains a UI and Input framework, and other tools for making universal mods.

Currently I don't have the time to deal with documentation or making this user-friendly, but it's here if you want to try to use it. You can look at UnityExplorer or my mod config managers to see examples.

Nuget package available [here](https://www.nuget.org/packages/UniverseLib/).

## Acknowledgements

* [HerpDerpinstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/6cc958ec23b5e2e8453a73bc2e0d5aa353d4f0d1/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) \[[license](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md)\], they were included for standalone IL2CPP coroutine support.
