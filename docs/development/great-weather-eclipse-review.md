# M7 Great Weather: cloud relief and a shared eclipse

This is the first implementation slice of the post-M6 Great Weather milestone. It builds on PR #11's chess-lighting/table cleanup branch, so OpenClaw can review and merge that work separately. No merge was performed here. Until #11 is merged, this PR targets that branch and a build of this branch includes both sets of changes.

## What should look different

The existing large planet, 4K weather art and ring composition remain the foundation. A new original 2K linear texture stores shallow cloud slopes, upper-cloud coverage and height. The surface shader uses those slopes for directional illumination and samples a nearby sunward height for local cloud shadowing. This is a shallow shading approximation; it does not add volumetric cloud geometry or a raymarch.

The shared sun is angled slightly lower so the eclipse moon and its shadow fit together in the default preview from all four reference capture directions. Broad ring bands, fine structure and the principal gap fade with their pixel footprint. Grazing/backlit illumination and shadow contrast are revised together. Moon lighting now grades across the surface rather than using an almost uniformly lit disc; its rim contribution is reduced.

A new 3,600 km-diameter moon follows a bounded arc at 90,000 km from the giant's centre. Its actual transform supplies one occulting sphere to the planet, ring and atmosphere shaders. These calculate the same sun-ray intersection and approximate finite-source penumbra. The shadow can fall on the rings before it reaches the globe. It is not a texture decal animated independently of the moon.

Living retains the 15-minute uninterrupted dwell, then unfolds the authored arc over **six minutes**. Hold-B still jumps directly to the existing 55% timeline preview (smoothed phase 0.57475), now chosen through orbital framing to show both moon and planetary shadow. Quiet, exit and reentry restore the starting pose; ordinary weather rotation continues as before. The existing smaller moon emergence shares the longer event. The new moon settles with its eclipse still visible; this is an authored observation segment, not a complete transit or a simulation of Keplerian orbital timing. The small local shadow does not dim the entire cabin or trigger flashes.

## Cost and checks

- One extra 2048×1024 RGBA ASTC 6×6 map with mipmaps: approximately **1.2 MiB compressed GPU storage**, excluding driver overhead. Its channel encoding preserves small slopes across 8-bit quantization; import is linear, repeat/clamp, trilinear.
- Gas surface: three texture samples instead of one, plus bounded normal/shadow arithmetic. No extra gas surface pass or cloud shell.
- One extra moon sphere/draw using the existing moon shader. Three existing receivers get the shared eclipse sphere through cached property blocks, without a separate Update clock or per-frame managed array allocation.
- No additional realtime shadow maps, postprocessing pass, or change to the room's lighting pipeline.

Passing source-side evidence:

- The existing 26 timeline checks and C# syntax parsing across 37 files.
- 1,001 orbital samples clear the giant, finite ring sheet and observer.
- Default preview and final shadows lie on the visible hemisphere; direct sun rays agree with the sphere shadow model in both eyes.
- The finite-source shadow reference is bounded and monotonic and leaves sunward points unshadowed.
- Moon and shadow fit all four reference capture cones at the hold-B phase, with radius margin. This does **not** establish visibility through each actual glazing opening/frame.
- Relief matches deterministic generation, with valid slope/coverage/height ranges, longitude continuity and linear import.

Unity scene assertions additionally check sphere/mesh radius agreement, uniform/transform synchronization for all three receivers, preview phase, seek order, Quiet reset and relief import. These assertions have been added but cannot be run on this machine without Unity.

The CPU composition studies were inspected and used to correct framing. They deliberately freeze cloud texture motion for comparison and approximate material shading. They omit the cabin/frame occlusion, full moon terrain shader, texture mips/compression, URP, stereo rendering and postprocessing. They are labelled **not Unity or headset captures** and are not visual approval.

## OpenClaw build and review

1. Check out this PR branch with its PR #11 base, using Unity **6000.5.2f1**. Run **Starship Cabin → Quiet Watch → Regenerate, Bake and Build Review APK**. The inherited chess probes require a fresh bake. Proposed version: `2.0.0-m7-weather-eclipse` / `20013`, existing Quiet Watch package and review APK path.
2. Run **Capture Fixed Seats** after baking. Expect **42 PNGs**: the previous 36, two extra Great Weather event checkpoints, and four exact hold-B eclipse views. Keep `manifest.json`, `chess-lighting.json` and APK provenance together.
3. Review the Great Weather event at 900, 990, 1080, 1170 and 1260 seconds from the couch. Then inspect `great-weather-eclipse-preview-{seat}.png` for Couch, Bed Sitting, Bed Reclining and Desk. Compare framing/lighting with M6.4. Ensure the moon/shadow are distinguishable through the real window frames, the ring shadow remains coherent, and the new cloud slopes do not resemble embossed rock or shimmer.
4. Test Quiet reset, Still/Drift, hold-B preview, pause/focus loss, leaving and reentering the vista. Confirm the moon and shader shadow reset together. Check both eyes and small head movements. The source ray/cone checks cannot detect a stereo shader failure or actual frame occlusion.
5. Measure before/after GPU timing on Quest, especially with the globe occupying most of the view and during an eclipse. Record actual refresh, late frames, memory and a warm 30-minute thermal run at the requested 72 Hz. If over budget, first reduce relief strength/disable the extra cloud-shadow sample as an explicit comparison; retain the causal eclipse and framing while measuring.

For local source checks:

```sh
dotnet run --project tests/QuietWatch.Checks -- .
python tests/check_weather_geometry.py
python tests/check_weather_eclipse.py
python tests/check_weather_atlas.py
python tests/check_blue_atlas.py
python tests/check_weather_relief.py
```

Python checks need NumPy 2.3.5. Unity builds consume the checked-in texture; Python is only needed for regeneration (`python tools/generate_weather_relief.py`). Optional CPU studies also need Pillow: `python tools/preview_weather_composition.py`, writing ignored `Builds/ArtReview/weather-eclipse-{arrival,preview,settled}.png`.

Unity API/shader compilation, bake, real captures, APK and headset acceptance remain OpenClaw review work. M7 is not artistically finished until the headset comparison supports the result. Later harbour/Blue Morning/event-variety work remains on the roadmap.
