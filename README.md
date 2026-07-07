# Quest Starship Cabin

A small Unity/OpenXR relaxation cabin for Meta Quest, built for sideloaded immersive ambience.

The current prototype is a seated, comfort-first VR room with a forward starfield window, warm cabin lighting, simple procedural furnishings, and generated ambient audio. It is intended as an original optimistic sci-fi relaxation space, not a recreation of any copyrighted franchise.

## Current Status

Stable comfort baseline: `Atmosphere V3`

Current tested build: `VisibleStars/Input V10`

- Native Quest APK build path proven
- OpenXR immersive mode working on Quest 3
- Head tracking working
- Seated static scene, no locomotion
- Procedural cabin geometry
- Visible forward-window starfield with transparent observation glass, window star dots, and procedural particle depth
- Procedural engine hum, air circulation, and occasional panel beeps
- Lighting and room scale tuned through headset testing
- Face-button ambience cycling with Quest controllers: A/B/X/Y cycles Drift / Orbit / Nebula
- Console strip feedback and a short haptic pulse confirm each mode change

The attempted first large in-world text panel was removed because it was visually messy and controller buttons were not mapped correctly. Current controls are intentionally small and diegetic.

## Near-Term Design Direction

The current scene is a functional comfort and interaction prototype, not the target visual experience. The next major iteration should focus on making the cabin beautiful and believable: stronger spatial composition, less box-like architecture, better wall/window modules, more convincing sci-fi materials, richer lighting, and a more intentional relaxation atmosphere while preserving the seated, low-motion comfort baseline.

## Requirements

- Unity 6000.5.2f1 or a compatible Unity 6 editor
- Android Build Support
- Android SDK and NDK Tools
- OpenJDK
- Meta Quest 3 or compatible Quest headset
- Developer Mode enabled on the headset
- USB debugging authorized

## Project Layout

- `Assets/Scripts/` - runtime C# scripts for starfield, ambience, session logic, and XR input
- `Assets/Editor/` - editor automation for scene setup and Android APK build
- `Assets/Scenes/Cabin_Seated_MVP.unity` - generated stable cabin scene
- `Assets/XR/` - Unity XR/OpenXR settings assets
- `Packages/` - Unity package manifest and lock file
- `ProjectSettings/` - Unity project settings
- `ROADMAP.md` - planned work

## Build

Open the project in Unity, then run:

`Starship Cabin -> Setup MVP Scene`

To build from the editor menu, use the included editor build tooling or call:

```bash
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PWD" \
  -executeMethod StarshipCabin.EditorTools.BuildStarshipCabin.BuildAndroidApk
```

The build output is:

`Builds/StarshipCabin-MVP.apk`

## Sideload

With the Quest connected and authorized:

```bash
adb install -r Builds/StarshipCabin-MVP.apk
adb shell monkey -p jp.openclaw.starshipcabin 1
```

## IP Boundary

Do not add copyrighted franchise names, logos, interface layouts, sound effects, music, voice clips, meshes, or fan assets unless their license is explicit and compatible with this repository. Keep the project original and generic.

## License

MIT. See `LICENSE`.
