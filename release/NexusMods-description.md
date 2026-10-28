# Moon Radio

Bring shared internet radio to the Moon Sweepers stations and vehicles.

Moon Radio adds a native-styled `RADIO  R` shortcut to the game's HUD. Press `R` to open an in-game radio panel, choose a station by genre, search for a station, or paste a direct HTTPS MP3 stream. Radio audio is positional, so nearby players can hear the same station from its station or vehicle emitter.

## Features

- In-game radio panel matching the Moon Sweepers interface style.
- `R` radio shortcut inside stations.
- `R` radio shortcut while seated in eligible vehicles.
- Supports internet radio stations and genres through Radio Browser discovery.
- Supports direct HTTPS MP3 stream URLs.
- Independent radio state for every station and every eligible vehicle.
- Station audio has a 200 metre positional range.
- Vehicle audio can be heard from outside the vehicle up to 15 metres away.
- Multiplayer synchronization for emitter identity, selected station, and playback state.
- Separate players can use different stations in different stations or vehicles.
- No external music player window is required.
- No video playback is required.
- Push mowers are excluded from vehicle radio controls.

## Controls

| Action | Control |
| --- | --- |
| Open radio | `R` |
| Choose a genre | Click a genre button in the radio panel |
| Search for stations | Enter a station name or genre, then select `SEARCH / PLAY` |
| Use a direct stream | Paste an HTTPS MP3 stream URL, then select `SEARCH / PLAY` |
| Stop the current radio | `STOP THIS RADIO` |
| Adjust volume | `VOL -` / `VOL +` |
| Close the panel | `CLOSE` |

## Installation

1. Close Moon Sweepers completely.
2. Install the required BepInEx framework and MoonSweepers BepInEx Patcher.
3. Extract the downloaded archive into the folder containing `MoonSweepers.exe`.
4. Confirm that the plugin is located at:

   `BepInEx/plugins/MoonRadio/MoonRadio.dll`

5. Launch Moon Sweepers.
6. Enter a station or eligible vehicle and press `R`.

Do not install multiple copies of `MoonRadio.dll`. If you are updating from an older build, remove the old `BepInEx/plugins/MoonRadio` folder first.

## Requirements

- Moon Sweepers 1.0.5, Windows x64
- BepInEx 6.0.0-be.791 Unity IL2CPP win-x64
- MoonSweepers BepInEx Patcher
- An internet connection for station discovery and streaming

## Multiplayer

Moon Radio synchronizes the radio emitter rather than transmitting music data between players. Each listener fetches the selected internet stream locally. This keeps bandwidth low while allowing station and vehicle radios to remain independent.

For multiplayer listeners to hear a radio, they must also have Moon Radio installed. Players may hear different audio if a station is unavailable in their region, temporarily offline, or blocked by their network.

Station radios are audible within 200 metres. Vehicle radios are audible from outside the vehicle within 15 metres. The push mower does not receive a vehicle radio control.

## Troubleshooting

### The `RADIO  R` shortcut is missing

Make sure the DLL is installed at `BepInEx/plugins/MoonRadio/MoonRadio.dll`, remove duplicate copies, and restart the game. The shortcut appears only inside stations or while seated in an eligible vehicle.

### Pressing `R` does nothing

Close other in-game menus and make sure the game window is focused. Check `BepInEx/LogOutput.log` for `Moon Radio loaded` and `Radio panel opened` entries.

### A station does not play

Internet radio streams can go offline or change format. Try another station or genre. Direct URLs must point to an HTTPS MP3 stream, not a normal YouTube, Spotify, Apple Music, or webpage URL.

### Other players cannot hear the radio

All players who want to hear synchronized radio need the mod installed. Confirm that players are within the emitter's range and that the station is available on their network.

## Known limitations

- The mod uses direct internet radio streams; ordinary video and music-service webpage links are not audio stream URLs.
- Stream availability and timing depend on the station host and each player's network.
- Each multiplayer listener downloads the stream independently.

## Credits and permissions

Moon Radio is an unofficial community mod for Moon Sweepers, created with permission from the game's creators. Moon Sweepers and its original assets belong to their respective creators. BepInEx, Il2CppInterop, NLayer, and the MoonSweepers BepInEx Patcher belong to their respective authors.

Please respect the terms and licensing of any internet radio station you choose to play.
