# Quest Starship Cabin

A small Unity/OpenXR relaxation cabin for Meta Quest, built for sideloaded immersive ambience.

The current prototype is a seated, comfort-first VR room with a forward starfield window, warm cabin lighting, simple procedural furnishings, and generated ambient audio. It is intended as an original optimistic sci-fi relaxation space, not a recreation of any copyrighted franchise.

## Current review candidate

M11 is the current **calm working harbour and chess terminal candidate**, based on merged M10 (`290b4d7`). It populates six harbour corridors with 32 staggered craft, keeps station lights steady, changes First Question cruise to right-to-left motion, applies the proven chess lighting treatment to books/computer parts, and replaces the placeholder screen with an original static Fischer–Spassky chess study. Proposed version: `2.0.0-m11-calm-harbour-cruise` (`20017`). See the [OpenClaw build and review guide](docs/development/calm-harbour-review.md). **Unity compilation, regeneration, fresh bake, 95 captures, APK and headset validation remain pending.**

**Current controls:** A changes vista. In First Question, tap B to start/stop cruising; stick-click controls the same motion setting. In other vistas, tap B changes Quiet/Living. Hold B for about one second to preview the current event, including the comet in First Question. Cruise starts stopped on entry and resets when leaving First Question. The cabin and camera do not translate.

The historical baseline sections below describe earlier controls and builds. The M9 [five-vista guide](docs/development/five-vista-overhaul-review.md) records the prior source pass; the latest user feedback reports a substantial visual improvement, with Harbour, engine alignment, First Question's dust and room detail selected for this follow-up.

M6.5 was merged as `f82a718` after Unity/bake/capture/APK/install validation. It corrected the chess lighting and removed the view sign and obsolete table indicator pad. See the [diagnosis and build review](docs/development/chess-lighting-review.md). The next exterior milestone is defined in the [post-M6 visual roadmap](docs/development/exterior-roadmap.md).

M6.4 was merged as `46447d5`; OpenClaw's merge record reports Unity/bake/capture/APK/install validation. The September 6 headset feedback confirms a substantial exterior improvement but unchanged chess highlights. Its version is `2.0.0-m6.4-exterior-study` (`20011`). The [M6.4 implementation guide](docs/development/exterior-spectacle-review.md) records the original source-stage work; M6.5 addresses the remaining chess issue.

The September M6.3 candidate implements transition/timing fixes and a Great Weather distance, weather-map and shared-shadow benchmark. It was source-tested, regenerated, baked, desktop-render reviewed, package-audited and installed on Quest 3 on 2026-09-05. **In-headset visual, comfort and performance approval remains open.** Follow the [implementation and review guide](docs/development/reliability-great-weather-review.md) for the remaining device acceptance. The installed candidate is `2.0.0-m6.3-reliability-scale` (`20010`); APK SHA-256: `7ea7e41f258b445a06ab8078e19bdd857edcb89ab77010e84424a43709407531`.

The records below describe the earlier M6.2 baseline. M6.3 preserves its approved controls: switching to Quiet cancels and resets event state, holding B jumps directly to a readable preview phase, and the formation remains visibly underway in both Quiet and Living even when comfort motion is set to Still.

## Previous baseline status

Active development branch: `main`

- Foundation: tested Milestone 8 `main` commit `25e1d818`
- Direction: exterior-first 2.0 replacement vision
- Current branch status: M6.3 extends the installed M6.2 foundation with reliability hardening and the Great Weather scale study; headset art-direction, comfort, thermal, and live performance sign-off remain open
- Historical experiments: `beta/m9-opus-fix` and `newhope` remain available for comparison but are not the 2.0 foundation

Current headset build: `Quiet Watch 2.0.0-m6.3-reliability-scale`

Stable rollback tag: `quarters-v2-m7-tested-20260711`

- Baseline rollback commit: `6ae1bf3` (`Add Quarters V2 milestone 7: fitted book labels + starfield V3`)
- Tested on: Meta Quest 3
- Installed/launched on device: 2026-07-12 18:33:05 JST
- Unity: 6000.5.2f1
- Package: `jp.openclaw.starshipcabin`
- APK SHA-256: `97ec2a0a804f8db99e34c300b920baaa5eed251c6ee5eef5a4c0905b1b0e768b`

The older `VisibleStars/Input V10` build is now a previous MVP baseline, not the current tested build.

### Quiet Watch M6.2 Release Candidate

Built, desktop-render validated, and installed on 2026-09-04 with Unity 6000.5.2f1:

- Product: `Starship Cabin - The Quiet Watch`
- Package: `jp.openclaw.starshipcabin.quietwatch`
- Version: `2.0.0-m6.2-release-candidate` (`20009`)
- APK: `Builds/StarshipCabin-QuietWatch-MultiVista.apk`
- APK SHA-256: `45e746d07fca80ee5e1076170c5266cf295b363afca04e8d034519aa1a3543f4`
- Installation status: installed successfully on Meta Quest 3 at 2026-09-04 19:35 JST. Package/version/ARM64 resolution passed; the headset was asleep behind Meta's controller-required launch check, so live telemetry and visual sign-off remain pending.

The review build contains:

- A reusable `VistaEnvironment` lifecycle and deterministic `VistaDirector`.
- One shared comfort blackout owner for seat hops and future vista changes.
- Persisted vista, `Quiet`/`Living`, and `Still`/`Drift` choices.
- A restrained diegetic selector on the cabin table with controller shortcuts. Tap `B` for Quiet/Living; hold `B` for about one second to force Living and preview the current vista's grace note from its clearly visible middle phase. Normal unattended playback retains the 12–15-minute release timing, counted only while Living remains continuously active; returning to Quiet cancels and resets the event state.
- Four named fixed-seat capture points and repeatable editor renders.
- Lightweight ten-second `adb logcat` evidence with active vista/mode, session time, frame average/worst/late percentage, CPU/GPU timing, actual display refresh, allocated memory, battery temperature, and Android thermal status. The app explicitly requests 72 Hz through Meta's OpenXR display utility.
- `The First Question`: a camera-ray-anchored deep-space field with galactic structure, dust, three stellar depth layers, colour variation, true negative space, near-silent spatial ambience, and a restrained comet cue.
- `Harbour of Ten Thousand Lights`: a Blender-authored inhabited station sector with layered torus structure, axial core, observation drum, load trusses, docking causeways, hangars, traffic masts, PBR surface response, and three authored service craft. Traffic follows banked Catmull-Rom approach/departure corridors around 44 conservative station clearance volumes, with deterministic station and craft-to-craft separation validation before capture or build. Habitat-light variation, illuminated docking guidance, and a muffled mechanical spatial bed tie activity to the infrastructure.
- `Blue Morning`: a brighter curved Earth-like dawn horizon with domain-warped continents, ocean depth and glint, shelves, cloud decks and shadows, clustered night lights, a richer atmospheric limb, progressive sunrise light, coupled cabin spill, and a warm air/harmonic ambience.
- `The Great Weather`: a brighter domain-warped storm giant with coloured cloud belts, embedded vortices and filaments, atmospheric depth, ring shadow, irregular translucent ring structure, a cratered moon transit, coupled cabin spill, and a deep planetary-scale rumble.
- `The Long Formation`: one original command ship and two substantial escorts, all with bevelled hard-surface geometry, functional scale cues, animated drive glow, shared navigation-light grammar, and three LODs. The full formation now traverses a broad roughly 100-second Living / 120-second Quiet flight arc with readable position and scale change inside ten seconds; ships make independent station-keeping corrections, while Living adds a coordinated ten-degree course change and engine chorus.
- A deterministic Blender 4.5 LTS asset pipeline that produces the editable `.blend`, twelve FBXs, five shared PBR maps, Quest import settings, URP materials, and repeatable silhouette renders without third-party art.
- `Quiet` plus `Still` as the default; optional extremely slow drift; destination-specific Living events now arrive after 12–15 minutes and unfold over 72–110 seconds without looping spectacle.
- Five destination-specific procedural spatial beds and restrained event cues, faded in with a slower comfort blackout so exterior, cabin light, and sound settle together.
- Thirty-one fixed-seat, event, traffic, and formation-motion captures across all five vistas, including dedicated 0/10/45-second formation checkpoints.
- Automated harbour validation samples every route against station safety envelopes and checks 1,200 seconds of craft-to-craft motion, including the release-timed cutter departure. The report passes with at least 1.99 m station clearance and 2.80 m craft separation after safety radii are deducted.

M1.1 incorporates the first headset review: it removes diffraction spikes and the over-bright blue wash, restores the stronger V3-style brightness distribution at direction-space density, corrects equirectangular star shape, and prevents procedural cell boundaries from leaking as line artifacts. The result is a darker field of clean point stars that remains anchored beyond the glass.

All five vistas are immediately selectable with the controller's primary button. Tap the secondary button (`B` on the right controller) to change Quiet/Living; hold it for about one second to preview the current event immediately. M6.2 preserves release pacing while making its timing state deterministic and adding the telemetry needed for the final on-device gate. It remains a Release Candidate rather than a release: live frame evidence, in-headset art/audio sign-off, 20-minute comfort sessions, and the 45-minute thermal run remain open.

### Implemented

- Native Quest APK build and sideload path proven.
- OpenXR immersive mode and Quest 3 head tracking working.
- Comfort-first seated baseline: no visible artificial translation; anchor hops fade to black.
- Procedural Crew Quarters V2 room shell with 55 degree glazed hull slope and four window panes.
- Furniture, palette, console strips, plants, chess/library decor, and fitted book labels.
- Seat anchors for couch, bed-sit, bed-recline, and desk.
- URP migration, baked cove lighting, and one mixed runtime light.
- Star shader V3 dark-sky view: point stars, diffraction spikes, halos, galactic band, dust lanes, warm/cool tints, filmic tone map, capped twinkle, shooting stars, nebula mode, and lateral motion.
- Ambient audio V2: layered engine bed, brown noise, air circulation, and softer panel beeps.
- Media/video wall from M5 retired in M8 because Quest system overlays can provide media apps without distracting from star-gazing.

### Roadmap Discipline

This README is the source of truth for what is implemented versus still roadmap. Each future milestone should update this section in the same commit that implements the feature.

## Design Direction: Starship Cabin 2.0 — The Quiet Watch

The room is shelter. The view is vocation.

The next major iteration keeps the tested cabin as its stable foundation and rebuilds the exterior experience. The player is an off-duty starship officer returning to a warm private room after the mission. The view should restore awe, peace, and a sense of why this life was chosen.

Version 2.0 is five genuinely distinct living vistas rather than one planet with material presets:

- **The First Question** — stars only; curiosity
- **The Great Weather** — a dimensional ringed gas giant; wonder
- **The Long Formation** — original ships travelling together; fellowship
- **Harbour of Ten Thousand Lights** — a colossal orbital station; belonging
- **Blue Morning** — an Earth-like horizon entering dawn; home

Each vista gets its own composition, scale ladder, lighting, sound, physical event logic, and comfort behaviour. Manual selection is immediate. Auto-tour is optional and off by default. Each normal session contains long stillness and at most one authored destination-specific grace note.

Full replacement vision:

- [The Quiet Watch — browser document](docs/design/the-quiet-watch.html)
- [The Quiet Watch — shareable PDF](docs/design/starship-cabin-2.0-the-quiet-watch.pdf)
- [Detailed implementation roadmap](ROADMAP.md)

The older [Crew Quarters V2 concept](docs/design/quarters-concept-v2.html) remains useful for cabin geometry and interior design, but no longer defines the exterior roadmap.

### Concept targets

These images establish composition, shelter, scale, light, and mood. They are art-direction targets, not gameplay screenshots or asset-fidelity promises.

#### The First Question

![Warm cabin looking into a deep galactic star field](docs/design/quiet-watch/first-question.jpg)

#### Harbour of Ten Thousand Lights

![Warm cabin docked beside a colossal orbital station](docs/design/quiet-watch/harbour-ten-thousand-lights.jpg)

#### The Great Weather

![Warm cabin overlooking a close ringed gas giant with moons](docs/design/quiet-watch/great-weather.jpg)

#### Blue Morning and The Long Formation

![Warm cabin above an Earth-like dawn with a distant ship formation](docs/design/quiet-watch/blue-morning.jpg)

### Existing cabin design references

#### Section — glass in the slope

![Section through the lounge](docs/design/section.svg)

### Plan — two zones, four perspectives

![Plan view with seat anchors](docs/design/plan.svg)

### The window wall from the couch

![Window wall elevation](docs/design/window-wall.svg)

### Palette — soft bright

![Palette](docs/design/palette.svg)

### Completed milestones

1. **Shell + glazing** — procedural room shell with 55° glazed hull slope, four rounded-trapezoid window frames, shader-based starfield (replaces the star-dot cubes and particle box).
2. **Furniture + palette** — couch, bed, alcove platform, desk, console strips, plants; soft bright material set.
3. **Seat anchors** — `SeatAnchorController`, fade transitions, four anchors (couch / bed-sit / bed-lie / desk).
4. **URP migration + baked lighting** — cove-lit baked GI, single runtime light, legacy cleanup.
5. **Audio V2 + media wall + star motion** — layered ambient bed, brown noise, lateral star motion, local video wall, and light fixes.
6. **Decor pass** — procedural chess set and library decor.
7. **Book labels + starfield V3** — fitted labels and the dark-sky star shader upgrade.
8. **Clear the deck + HDR trial** — retire the media/video wall, remove `MediaScreenController`, enable HDR + bloom, add fixed foveated rendering, and verify on Quest.

### Active 2.0 roadmap

0. **Clean break** — environment lifecycle, capture harness, frame-time evidence, and removal of weak/inactive 2.0 claims.
1. **The First Question** — rebuild stars-only space as the benchmark and ship the real selector. Corrected playable built; Quest performance and comfort sign-off remain.
2. **The Harbour** — Blender-authored benchmark, collision-validated Quiet/Living traffic, docking detail, light spill, and destination sound built and installed; refine scale and night-cycle event from headset feedback.
3. **Blue Morning** — atmosphere/cloud art, dawn response, cabin coupling, and destination sound built and installed; validate `Still`/`Drift` comfort.
4. **The Great Weather** — storm/ring art, cratered moon-emergence event, cabin response, and destination sound built and installed; refine from headset feedback.
5. **The Long Formation** — three substantial Blender-authored ships now travel continuously with animated drives, station keeping, a coordinated turn, and destination sound; refine from headset feedback.
6. **The Quiet Watch** — Release Candidate unifies sound, light, persistence, release-length timing, transitions, event review, and 72 Hz/thermal instrumentation; live performance, comfort, thermal testing, and final headset sign-off remain.

Every milestone ends with a regenerated APK, Quest installation, fixed-seat captures, frame-time evidence, and on-device review. Procedural code remains useful but is no longer a purity rule: compatibly licensed authored meshes, textures, matte layers, and baked assets are allowed when they materially improve the headset result.

Original, generic sci-fi only — see the IP Boundary section below.

## Requirements

- Unity 6000.5.2f1 or a compatible Unity 6 editor
- Blender 4.5 LTS only when rebuilding the authored exterior assets
- Android Build Support
- Android SDK and NDK Tools
- OpenJDK
- Meta Quest 3 or compatible Quest headset
- Developer Mode enabled on the headset
- USB debugging authorized

## Project Layout

- `Assets/Scripts/` - runtime C# scripts for starfield, ambience, session logic, and XR input
- `Assets/Editor/` - editor automation for scene setup and Android APK build
- `Assets/Art/QuietWatch/` - exported Quest LOD models and shared PBR maps
- `ArtSource/QuietWatchVisualBenchmark.blend` - editable command ship, escorts, and harbour source
- `tools/blender/build_quiet_watch_benchmark.py` - deterministic source-to-FBX/texture pipeline
- `Assets/Scenes/Cabin_Seated_MVP.unity` - older MVP scene
- `Assets/Scenes/Cabin_Quarters_V2.unity` - generated current Quarters scene after running the setup menu; not checked in
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

The Quiet Watch build output is:

`Builds/StarshipCabin-QuietWatch-MultiVista.apk`

To rebuild the original exterior art before regenerating the Unity scene:

```bash
/Applications/Blender.app/Contents/MacOS/Blender \
  --background \
  --python tools/blender/build_quiet_watch_benchmark.py
```

## Sideload

With the Quest connected and authorized:

```bash
adb install -r Builds/StarshipCabin-MVP.apk
adb shell monkey -p jp.openclaw.starshipcabin 1
```

For the Quiet Watch build:

```bash
adb install -r Builds/StarshipCabin-QuietWatch-MultiVista.apk
adb shell monkey -p jp.openclaw.starshipcabin.quietwatch 1
```

## IP Boundary

Do not add copyrighted franchise names, logos, interface layouts, sound effects, music, voice clips, meshes, or fan assets unless their license is explicit and compatible with this repository. Keep the project original and generic.

## License

MIT. See `LICENSE`.
