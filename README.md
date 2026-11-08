# Open76 for Mac (Apple silicon)

> **TL;DR:** Interstate '76 running natively on an Apple silicon Mac, no Wine, no Rosetta. This is a fork of [r1sc/Open76](https://github.com/r1sc/Open76), a Unity reimplementation of the 1997 Activision game, ported to Unity 6 and a Mac player build. You supply your own copy of the game data. All 40 missions load and run, but it is not a finished game yet (see below).

![Screenshot](Screenshots/sc1.png)

**Not affiliated with Activision.** No game data is included. You need your own legal copy of Interstate '76 (the retail discs or a GOG install). The Nitro Pack is not supported.

## What works
- Builds and runs as a native arm64 macOS app, intro cutscene included (a native `libsmacker` plays the Smacker videos).
- All 40 missions (the training run, M01-M15, S01-S07, T01-T17) load and run through a 25-second random key-mashing stress test with no unhandled exceptions.
- Driving, weapons fire, the camera views, music and sound effects.
- The Esc menu, including Audio Control (music, SFX and voice levels, saved between runs) and Abort Mission, which returns to the main menu.

## What does not work yet
- Gun turrets are not loaded (they are skipped with a warning). Missions S06, T11, T12 and T15 have them.
- No explosion or hit graphics. The original code only lowers a health number and plays a sound.
- The HUD (damage, gear, radar, compass, weapons) is painted on the cockpit dashboard, so it only shows in the first-person view (F1). The default third-person view has none. Not verified yet on the Mac build.
- The car physics are Open76's generic model, not the original game's handling.
- Many mission script commands are still unimplemented, so some missions will not play through to the end. The first training mission holds input on a scripted camera.

## Run it on a Mac
1. Get the game data into a folder with `i76.exe`, `I76.ZFS`, `MISSIONS`, `CUTSCENE` and `DATABASE.MW2` (copy them from your discs or a GOG install; `i76.exe` is only checked for, never run).
2. Build the app with Unity 6000.3.28f1 (Apple silicon editor):

   ```
   /Applications/Unity/Hub/Editor/6000.3.28f1/Unity.app/Contents/MacOS/Unity \
     -batchmode -nographics -quit -projectPath . \
     -executeMethod Assets.Editor.BuildMac.Build \
     -buildOutput /path/to/gamefolder/Open76.app
   ```

   The app looks for the game data two folders above its own `Contents` folder, so `Open76.app` must sit inside the game folder. Or set the `I76_GAME_PATH` environment variable to the data folder.
3. Start `Open76.app` (the executable inside is named `I76`).

Test flags for the built binary: `-i76mission m01.msn`, `-i76drive`, `-i76mute`, `-i76screenshot out.png -i76delay 20`, `-i76chaos 25`, `-i76menutest`, `-i76aborttest`. Logs go to `~/Library/Logs/DefaultCompany/I76/Player.log`.

## Credits and prior work
This stands entirely on other people's work. Please credit them first.

- **Erik Sandberg ([r1sc](https://github.com/r1sc))** created Open76 and wrote the file-format parsers, renderer and mission machine. Original repo: [r1sc/Open76](https://github.com/r1sc/Open76).
- **Johan Rensenbrink ([JJJohan](https://github.com/JJJohan))** contributed a large share of upstream Open76: vehicle sounds, the GDF parser, radar, weapons and mission-script updates.
- **"That Tony"** reverse engineered most I'76 file formats on the blog *Hacking on Space* (hackingonspace.blogspot.se). Without that, none of this exists.
- **Hopper's guide** and the modders who documented the formats years ago (kept in `Notes/`).
- **Greg Kennedy**, author of [libsmacker](http://libsmacker.sourceforge.net) (Smacker video decoding, in `libsmacker/`).
- **Activision / Zipper Interactive** made Interstate '76. This project does not include or redistribute their game files.

What this fork adds: Unity 6 port, native macOS arm64 player build, native `libsmacker.dylib`, a Rigidbody pose fix for the car spawn, camera black-screen fixes, robust mission loading, a working Audio Control menu, a centered main menu, Abort Mission, and the test flags above. See the git history. The changes were made with the help of Claude Code.

License: GPL, like upstream (see `LICENSE`).

---

# Original README (from r1sc/Open76)

Open76 is an engine reimplementation of Activision's Interstate '76 on the Unity platform.

![Screenshot 1](Screenshots/sc1.png)

## What works?
Parsing and rendering of pretty much everything contained in a level (including cars).

The mission stack machines are now underway; Open76 can parse and run the stack machines. However, there's still a lot of machine actions that needs implementation.

The simulation does not work yet. Driving around works as well as the various camera modes. The car simulation needs a lot of work (engine simulation, gearbox, etc.).

Some features of the above are not yet fully implemented or have problems - see Issues.

## How do I run it?
1. Open Level.scene in Unity.
2. Find the gameobject called "Game" and set the "Game path" property to your Interstate '76 install directory.

~~For now, Open76 requires the uncompressed ZFS of version 1.0 as delivered on the original CD. It cannot parse the compressed ZFS present in later versions.~~ 

Open76 can now parse both the original uncompressed version and the compressed version of the ZFS. Nitro pack seems to have a different mission file format, this needs to be investigated.

## How do I contribute?
Fork this repository. Look in the Issues list. Communicate that you're committing to fixing an issue and finally submit a pull request.

## Why?
For fun. Tinkering with Interstate '76 started many years ago, but I never made much progress on the monstrous number of file formats presented. Until early 2017 when I discovered that blogger "That Tony" reverse engineered many of the formats on his blog http://hackingonspace.blogspot.se. Some formats had been reversed years ago by modders, although these specifications were hard to find since most of the sites were long gone (bless web-archive!).

In my opinion, Interstate '76 is one of the best games ever made. It suffers from an aging engine though, and in recent years it has become increasingly harder to play it on modern platforms.
The primary goal of this project is to provide the same playing experience in a modern engine while conforming to the original data file specifications.
A long term goal is to extend the engine with various modern features, such as HMD and VR support.

## Acknowledgements
Without "That Tony"'s wonderful blog (http://hackingonspace.blogspot.se) and reverse engineering skills, this project would never have gotten anywhere. Tony reverse-engineered most of the formats used by I76.

## More screenshots
![Screenshot 2](Screenshots/sc2.png)
![Screenshot 3](Screenshots/sc3.png)
![Screenshot 4](Screenshots/sc4.png)
![Screenshot 5](Screenshots/sc5.png)
![Screenshot 6](Screenshots/sc6.png)
![Screenshot 7](Screenshots/sc7.png)

## License
Licensed under the GPL version 3.

This is in no way affiliated with or endorsed by Activision or any other company.

## macOS (Apple Silicon) notes

This branch removes the Windows only dependencies so the project can run natively on a Mac:

- LZO decompression is now pure C# (`Assets/Scripts/System/Compression/LZO.cs`), verified byte for byte against liblzo2 for LZO1X and LZO1Y. The `lzo2.dll` plugins are gone.
- Music MP3s load through Unity's built in decoder instead of NAudio (which needed Windows codecs).
- The cutscene decoder (libsmacker) builds for macOS with `libsmacker/build-macos.sh` (universal arm64 + x86_64). Its Unity interface now uses fixed width types so the C# struct layout matches on every OS. If the native plugin is missing, videos are skipped instead of crashing.
- Deprecated packages (collab-proxy, ide.vscode, multiplayer-hlapi, xr.legacyinputhelpers) removed so the project opens in Unity 6.
