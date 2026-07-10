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

## Design Direction: Crew Quarters V2

The next major iteration turns the cabin into personal starship crew quarters: a cosy, lived-in room built around star-watching. The defining architectural move is a steeply sloped glazed hull wall — the windows sit *in* the slope, so the starfield hangs above you from the couch, and lying on the bed you look straight up into space. Two zones (a lounge and a raised sleeping alcove) each get their own glass and their own perspective on the room.

Movement uses seat anchors plus real walking: natural head-tracked walking within the playspace, and point-to-teleport hops (with a short fade, never visible translation) between couch, bed, and desk anchors. The seated, no-artificial-motion comfort baseline is preserved.

Full concept document: [`docs/design/quarters-concept-v2.html`](docs/design/quarters-concept-v2.html) (open locally in a browser).

### Section — glass in the slope

![Section through the lounge](docs/design/section.svg)

### Plan — two zones, four perspectives

![Plan view with seat anchors](docs/design/plan.svg)

### The window wall from the couch

![Window wall elevation](docs/design/window-wall.svg)

### Palette — soft bright

![Palette](docs/design/palette.svg)

### Build milestones

1. **Shell + glazing** — procedural room shell with 55° glazed hull slope, four rounded-trapezoid window frames, shader-based starfield (replaces the star-dot cubes and particle box).
2. **Furniture + palette** — couch, bed, alcove platform, desk, console strips, plants; soft bright material set.
3. **Seat anchors** — `SeatAnchorController`, fade transitions, four anchors (couch / bed-sit / bed-lie / desk).
4. **URP migration + baked lighting** — cove-lit baked GI, single runtime light, legacy cleanup.

All geometry remains procedural C#; the design is code-reviewable. Original, generic sci-fi only — see the IP Boundary section below.

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

For the Quarters V2 milestone scene, run:

`Starship Cabin -> Setup Quarters Scene (V2)`

To build the Quarters APK from the editor menu, use `Starship Cabin -> Build Quarters APK`, or call:

```bash
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PWD" \
  -executeMethod StarshipCabin.EditorTools.QuartersSceneSetup.BuildQuartersApk
```

The Quarters build output is:

`Builds/StarshipCabin-Quarters.apk`

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
