# UniverseLib

UniverseLib is a library for making plugins which target IL2CPP and Mono Unity games, with a focus on UI-driven plugins.

It was developed for personal use so that my [UnityExplorer](https://github.com/rainbowblood666/UnityExplorer) tool and my Config Manager plugins could use a shared environment without overwriting or conflicting with each other, but I made it available as a public tool cause why not.

## NuGet

[![](https://img.shields.io/nuget/v/rainbowblood.UniverseLib.Mono?label=UniverseLib.Mono)](https://www.nuget.org/packages/rainbowblood.UniverseLib.Mono)  

[![](https://img.shields.io/nuget/v/rainbowblood.UniverseLib.IL2CPP?label=UniverseLib.IL2CPP)](https://www.nuget.org/packages/rainbowblood.UniverseLib.IL2CPP)

## Documentation

Documentation and usage guides can currently be found on the [Wiki](https://github.com/rainbowblood666/UniverseLib/wiki).

## UniverseLib.Analyzers

[![](https://img.shields.io/nuget/v/UniverseLib.Analyzers)](https://www.nuget.org/packages/UniverseLib.Analyzers) 
[![](https://img.shields.io/badge/-source-blue?logo=github)](https://github.com/rainbowblood666/UniverseLib.Analyzers)

The Analyzers package contains IDE analyzers for using UniverseLib and avoiding common mistakes when making universal Unity mods and tools.

## Acknowledgements

* [Geoffrey Horsington](https://github.com/ghorsington) and [BepInEx](https://github.com/BepInEx) for [ManagedIl2CppEnumerator](https://github.com/BepInEx/BepInEx/blob/master/BepInEx.IL2CPP/Utils/Collections/Il2CppManagedEnumerator.cs) \[[license](https://github.com/BepInEx/BepInEx/blob/master/LICENSE)\], included for IL2CPP coroutine support.
