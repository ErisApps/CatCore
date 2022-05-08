# CatCore
CatCore is a high-performance .NET Standard 2.0 library which provides a shared connection to mods and other applications for Twitch (and other future streaming platforms).


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


## Documentation

CatCore is documented using XML docs, which are provided in the release zip as well. However, documentation and information covering several subjects will also be added to [the CatCore wiki](https://github.com/ErisApps/CatCore/wiki).
It will cover several subjects, including but not limited to, performance benchmarks, configuration, et cetera. However, please keep in mind that this is still a work in progress.


## Origin and name

**CatCore** is the spiritual successor to ChatCore, a library made that is/was used by several Beat Saber mods in order to interact with Twitch (it did support Mixer as well, but only starting roughly 1 month before it got shut down.)

I took over maintaining ChatCore sometime around August/September 2020, during which I was often working on said library.
However, I noticed rather quickly that some things could be done in a different manner, leading to better maintainability, while also being able to gain more performance out of it, by leveraging newer techniques.
And thus... did I start on a journey on the 1st of March... by starting with the full-on rewrite of this library...

Now, you might be wondering about things like "**Why is this library actually called CatCore?**" or "**Why was the name actually changed?**".
There are a few reasons for it, some of them being sillier than others though.
1) As some might know, I'm terrible sometimes when it comes down to writing.
   While I was still working on ChatCore, I happened to typo its name _very_ often, so I would pretty much always end up accidentally sending "CatCore" instead.
   And... believe it or not, but the name kinda stuck.
2) The second reason is pretty much a continuation of the first one, but in case you're familiar with other languages, you might know that the english word "Cat" translates to "Chat" in french.
   So yeah... other than being a typo, it could also be a fun wordplay.
3) As the library is initially used in mods, we would often receive the question that the mod that displayed chat "wasn't working". In which the case often was that people had mistaken ChatCore for said mod.
   By slightly changing its name, I hope that it would actually get rid of some of said confusion.


## Developing

Compiling this library requires VS 2022 Preview or Rider 2021.3 EAP or newer and at least the .NET 6 RC2 SDK tooling.
CatCore leverages source generators to improve performance and throughput in some areas, which is why it relies on the latest compiler version, despite being a .NET Standard 2.0 library.
You also need to compile on Windows if you want to create a non-debug build, this because CatCore relies on ILRepack (to merge in all its dependencies) which currently, due to a bug in the underlying (older) version of Mono.Cecil, only works on Windows.
~~However, [a new maintainer recently joined the ILRepack project and plans to address a few of those issues as well.](https://github.com/gluck/il-repack/issues/304)~~