# Moon Radio — requested behavior

- Use the game's own UI style. No external player windows in the finished mod.
- Inside a station, display `R — Radio` alongside the existing right-hand control hints.
- Seated in any vehicle except the push mower, display the same radio action.
- Open a themed in-game panel with a URL input and playback controls.
- Each individual station and each individual eligible vehicle owns independent playback state; starting one must not replace another.
- Station sound is positional and reaches up to 200 metres, fading with distance.
- Vehicle sound follows its vehicle and is audible outside it. Vehicle range is not yet specified; make it configurable, initially 200 metres.
- Requested source is a YouTube URL, with audio-only playback and no video requirement.
- Multiplayer listeners must receive per-emitter track and playback position rather than one global track.

## Confirmed inspection

InteropInspector's `radio` mode enumerates the actual 1.0.5 types and members into `tools/InteropInspector/radio-inspection.txt`.
`MoonActionPromptCard` provides the game's action hint UI. `MoonWalker.mountedVehicle`, `MoonVehiclePassenger.Seated`, `MoonVehiclePassenger.Vehicle`, and `MoonPrototypeVehicle` provide vehicle context to investigate further.
`MoonPrototypeStation` exists but is equipment-related; do not assume it represents a station building or interior volume.

## Approved source change

The user approved choosing internet radio stations and genres instead of exact songs. Use Radio Browser for discovery and direct HTTPS MP3 streams for the first implementation. No local music files or visible video player are required. Every listener fetches audio directly; multiplayer packets should carry emitter identity and station selection rather than rebroadcast encoded music.

## Original YouTube experiment limitation

The existing experiment uses the official visible YouTube embed in WebView2. Actual playback was observed, but it does not implement this design.
YouTube's documented player integration does not provide a supported audio-only source for Unity AudioSource. Its developer policies prohibit isolating audio and hidden/background players:
https://developers.google.com/youtube/terms/developer-policies

Therefore the existing experiment cannot simply be hidden to satisfy the requested design. The internet radio source change above replaces this experiment as the intended playback approach.

## Native radio discovery

The inspected assembly also contains MoonRadioMenu, MoonRadioPlayer, MoonRadioStation, MoonRadioCatalog, MoonRadioState and multiplayer radio commands in MoonCrewSession. Their presence in the assembly does not establish that all of them are enabled in the running game. Inspect and test these before selecting how to extend the native menu and network transport. The native player appears to hold a global state, so independent simultaneous emitters cannot be assumed to work automatically.

## Current testing blocker

Radio Browser search successfully returned live MP3 URLs for SomaFM Groove Salad. `Audio/LiveStream.cs` implements a background MP3 decoder with bounded mono PCM buffering, cancellation, and no Unity calls on the decoder thread. `SmokeTest` builds a live decoding and cancellation check.

Windows Application Control blocked SmokeTest.exe before execution, and also blocked the updated InteropInspector.dll. The actual live decoding test has not run, and no audio success is claimed. Do not try alternate executable hosts or weaken the policy to evade these blocks. Native UI, Unity audio integration, and multiplayer playback still require runtime verification after this development execution blocker is resolved.

## Prototype status

MoonRadio.dll builds and loads with Sleep Until Dawn and Station Clock. Its F8 action opens/focuses a separate WebView2 experiment, and is not the intended final interaction. The hotkey was deployed but not verified before computer control was stopped. Station detection, R controls, positional emitters, per-radio state, vehicle exclusions, and multiplayer synchronization are not implemented or tested.
