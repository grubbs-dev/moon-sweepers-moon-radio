# Moon Radio 0.1.4

Adds in-game internet radio to Moon Sweepers stations and eligible vehicles.

## Features

- Press `R` beside the station controls to open Moon Radio.
- Press `R` while seated in a vehicle to open that vehicle's radio.
- Choose an internet radio station by genre or search, or paste a direct HTTPS MP3 stream URL.
- Stations and vehicles keep independent radio selections.
- Positional audio is audible to nearby players; station range is 200 m and vehicle range is 15 m outside the vehicle.
- The push mower is excluded from vehicle radio controls.
- No external player window or video playback is required.

## Requirements

- Moon Sweepers 1.0.5 (Windows x64)
- BepInEx 6.0.0-be.791 Unity IL2CPP win-x64
- MoonSweepers BepInEx Patcher

## Installation

1. Close Moon Sweepers.
2. Extract this archive into the folder containing `MoonSweepers.exe`.
3. Confirm the file exists at `BepInEx/plugins/MoonRadio/MoonRadio.dll`.
4. Launch the game and press `R` in a station or eligible vehicle.

Remove `BepInEx/plugins/MoonRadio` to uninstall. Do not keep duplicate copies of `MoonRadio.dll`.

## Multiplayer

Each emitter synchronizes its station identity and playback state. Every player needs Moon Radio installed to hear synchronized radio emitters.

## Source and credits

Source is included in the GitHub repository. Moon Sweepers, BepInEx, Il2CppInterop, NLayer, and the community patcher belong to their respective creators. Moon Radio is an unofficial community mod.

Internet radio availability depends on the station and network connection. Only use streams you are allowed to access.
