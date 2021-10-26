# CatCore
CatCore is a .NET Standard 2.° library which provides a shared connection to mods and other applications for Twitch (and other future streaming platforms).

## Installation

The installation is fairly simple.

1) Grab the latest library from BeatMods/ModAssistant (once it's available) or from [the releases page](https://github.com/ErisApps/CatCore/releases/latest) (once there is actually a release)
2) Extract the zip in the main Beat Saber folder. This implies putting the DLL in the "Libs" folder and the manifest file in the "Plugins" folder of your Beat Saber installation.

### Setup

1) Boot up Beat Saber
2) If there's a mod installed that relies on CatCore, then the web portal will automatically pop up.
   If it does not, either make sure there's actually mod installed the depends on it and try navigating to http://localhost:8338.
3) Log in with your Twitch account (and possibly toggle some of the other settings as you see fit)
4) Press the save button
5) We're done, you should have set it up correctly now ^^

## Developing

Compiling this library requires VS 2022 Preview or Rider 2021.3 EAP or newer and at least the .NET 6 RC2 SDK tooling.
CatCore leverages source generators to improve performance and throughput in some areas, which is why it relies on the latest compiler version, despite being a .NET Standard 2.0 library.
You also need to compile on Windows if you want to create a non-debug build, this because CatCore relies on ILRepack (to merge in all its dependencies) which currently, due to a bug in the underlying (older) version of Mono.Cecil, only works on Windows.
However, [a new maintainer recently joined the ILRepack project and plans to address a few of those issues as well.](https://github.com/gluck/il-repack/issues/304)