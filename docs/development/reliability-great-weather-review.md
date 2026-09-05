# Quiet Watch reliability and Great Weather scale study

This is the first implementation package from the September 2026 audit: R0 reliability work plus the R1 Great Weather distance/lighting study. Base: `b589b09625f2403c3b22598acecae90bc97388fd`. It is a review candidate for OpenClaw/Codes, not a headset-approved release. The existing installed M6.2 and rollback records in README are historical evidence for those builds only.

## Intended result

The modest cabin remains the reference for human scale. Outside Great Weather, the planet, ring system and moons share a distant projection: leaning 15 cm should move the window frame substantially without making the astronomical scenery slide like nearby props. The existing angular composition is retained at the couch reference eye. A 58 m proxy diameter represents a 58,000 km giant; every weather body uses the same one-million distance multiplier.

The giant now samples an original 2048 × 1024 weather atlas with large storms and fine cloud shear. Slow longitudinal flow remains live; multi-octave planet noise no longer runs per fragment. A smooth 128 × 64 sphere improves the silhouette (16,128 triangles). Android imports the atlas as ASTC 6×6 with mipmaps. This trades texture bandwidth and more vertices for less fragment arithmetic; measure the net GPU effect.

A shared sun lights the giant, moons and rings. Analytic sun rays intersect the actual ring plane and its density bands; the planet also shadows the rings and moons. The primary moon starts behind a ring band, then clears its outer shadow over four minutes after 15 uninterrupted Living minutes. The other moon stays fixed. The cabin fill stays restrained and stable: the moon's emergence no longer flashes the whole room. Rings use one double-sided sheet instead of a transparent closed slab.

This is an art-directed distant-space approximation, not an orbital physics or volumetric atmosphere simulation. Weather bodies keep compressed depth values to work with the cabin camera's clip planes. `DistantVistaBounds` expands CPU culling bounds for the shader's room-scale displacement. Keep all weather members on one scale; do not add nearby interactive objects to that projection. Eye-by-eye depth, edge culling, transparent-glass ordering and apparent scale must pass headset review.

## Reliability changes and behavior

- Rapid destination requests during a seat/vista fade are ignored. A refused or missing fader cannot expose an immediate vista swap. Disabling the fader clears its overlay and releases the completion callback once. The overlay shader includes URP stereo/instancing support.
- A double-precision observation timeline integrates future movement instead of multiplying the entire session age by a newly selected speed. Drift and traffic activity ease over two seconds. Application pause/focus loss stops visual clocks; large stall deltas are capped at 0.1 s instead of catching up visibly.
- Quiet pauses an underway event at its current pose. Returning to Living resumes it; before an event starts, changing Life mode resets its dwell. Each entry permits one event. Hold B previews continuously at 8× event speed, without jumping into the middle. The already-short First Question comet previews at real time. Completed events require reentry to replay. Formation course changes require Drift; Still settles the formation to rest and rejects a preview that would force it to move.
- Audio uses two destination sources to fade the outgoing bed while the incoming bed arrives. Event audio has a dedicated cancellable source. Destination/event synthesis is prewarmed during runtime startup; editor captures do not synthesize audio. Startup time and audio memory still need profiling.
- Android foveated rendering is enabled, with SRP foveation and a conservative requested level of 0.5. A reported level is not proof that the device is foveating.
- Release frame timing is enabled. Logs distinguish missing CPU/GPU data from zero, deduplicate timing samples, separate vista/mode/transition windows, and discard the resume gap. CPU/GPU figures are valid-sample averages. p95/p99 measure application frame deltas, **not** GPU percentiles or compositor missed frames. Use device profiling alongside these logs.
- Scene generation stamps a hash of source C#, shaders and authored geometry/image inputs. Build refuses stale generation or a missing verified bake. A successful synchronous bake marks and saves the scene. APK provenance records its SHA-256, source hash, Unity version and build GUID. This checks generated-source freshness, not arbitrary manual scene edits or every external toolchain setting; rebuild from a clean checkout for release evidence.
- Captures use explicit observation times, immutable pose origins, a unique run directory and a manifest. The saved scene is reopened afterward so preview transforms and lighting cannot leak into a later build. Current coverage is four seats × five vistas plus harbour/formation review frames, three Great Weather shadow phases and two comet phases (33 images when all expected capture points exist). The manifest records the actual files and whether lighting was baked.

## Checks performed without Unity

Run from the repository root with .NET 8 and Python 3.12+/NumPy 2.3.5:

```sh
dotnet run --project tests/QuietWatch.Checks -- .
python3 tests/check_weather_geometry.py
python3 tests/check_weather_atlas.py
git diff --check
```

The linked production `VistaTimeline.cs` passes 27 assertions covering event boundaries, one event per entry, preview continuity, multi-hour mode changes, easing, invalid deltas and seek versus 72 Hz simulation. Roslyn parses the project's C# using Editor/Android symbol sets. Independent geometry reference checks cover each eye, head sway, a reclining seat, and the moon's shadow path. The actual atlas generator repeats deterministically and agrees with the checked-in PNG within one byte of platform rounding. GitHub Actions runs these same checks.

**These are not a Unity compilation, shader compilation, desktop render, APK build or Quest performance test.** This implementation environment has no Unity editor or attached headset. None of those results is claimed here. `QuietWatchReviewChecks.Run` supplies additional real Unity scene checks, but has not been run here.

## Maintainer build and review

1. Use a clean checkout of this PR with Unity **6000.5.2f1**, the committed package versions and Android IL2CPP support. Preserve the known-good APK and device settings before testing. Keep the PR in draft until the following evidence is attached.
2. Run **Starship Cabin → Quiet Watch → Regenerate, Bake and Build Review APK**. This regenerates the scene, runs the Unity scene checks, bakes synchronously and builds. In batch mode (requires graphics for lightmapping; do not use `-nographics`):

```sh
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod StarshipCabin.EditorTools.QuartersSceneSetup.RegenerateBakeAndBuildReview \
  -logFile Builds/review-build.log
```

Create `Builds/` first when using a fresh checkout. Output: `Builds/StarshipCabin-QuietWatch-Review.apk` plus `.provenance.txt`. Candidate version: `2.0.0-scale-study.1`, Android version code `20010`, existing Quiet Watch package ID. A maintainer choosing another version code should update the source and regenerate.

3. Run **Capture Fixed Seats** twice, reversing the order of manual seeks as an additional check. Compare the two manifests and corresponding image pixels, not directory names. The Unity scene checks already check repeated seeks and reentry, shared materials, moon clearance, sphere winding and supported weather shaders. Attach the baked captures; inspect poles, meridian seam, shadow edges, moon visibility and every seat. Captures made with `RegenerateAndCaptureAll` are explicitly unbaked unless lighting is separately baked first.
4. In Play Mode and then Quest, alternate seat hops, vista changes and mode input rapidly. Verify full black in each eye, no exposed swaps, no stuck busy state, and correct settings after a declined preview. Disable/re-enable the fader during each half of a transition. Test headset removal/resume before and during an event.
5. Test Great Weather at all seats with 15 cm head sway. Compare the window's stereo disparity against the astronomical bodies. Check ring/glass sorting and silhouettes when leaning at the window edges. Watch the normal 15-minute dwell and full four-minute emergence as well as preview. Quiet/Still should support a convincing uninterrupted 20-minute stay.
6. Collect Quest CPU/GPU/compositor and thermal evidence at 72 Hz, first cold and then after a two-hour soak. Compare M6.2 and this candidate from matching seats/modes. Aim for CPU and GPU application work individually ≤11 ms, with tails recorded, within the 13.89 ms refresh interval. This is a target, not a measured result. Compare foveation 0 and 0.5, especially peripheral stars and ring bands. Do not infer success from `requested_level` or application delta statistics.

## Remaining roadmap

| Next gate | Work | Evidence required |
| --- | --- | --- |
| Finish R0 | Resolve any Unity/device regressions, compare fresh-clone bake and captures, record exact reviewed SHA/APK | Both-eye fade, reproducible build, truthful device measurements |
| Finish R1 | Tune Great Weather composition, exposure, ring softness and atmospheric limb in Quest | Same-seat comparison preferred by the owner; quiet 20-minute review; thermal headroom |
| R2 | Improve couch facing, furniture ergonomics, modest material detail and personal possessions | Every seat comfortable and naturally aimed toward the view |
| R3 | Extend successful scale treatment to Harbour/Formation and improve Blue Morning | Shared visual rules, credible detail scale, no new diorama effect |
| R4 | Refine rare events and long stays; add full traffic schedule/transition coverage | Two-hour sessions without visible loops, intrusive sound or motion surprises |

Known limits retained for separate work: harbour lane visibility still changes immediately with Quiet/Living; its clearance validator covers nominal routes and a fixed Living schedule, not every possible delayed/preview/mode-transition schedule or the station's animated clearance transform. The sky and Blue Morning remain procedural. This PR does not claim all audit findings or the complete R0/R1 headset acceptance gates are closed.

## API references

The OpenXR 1.17 [settings API](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/api/UnityEngine.XR.OpenXR.OpenXRSettings.html) exposes the SRP foveation setting; [FoveatedRenderingFeature](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/api/UnityEngine.XR.OpenXR.Features.FoveatedRenderingFeature.html) supplies the feature. Unity documents [synchronous lightmap baking](https://docs.unity.cn/6000.2/Documentation/ScriptReference/Lightmapping.Bake.html). These references establish API intent, not validation of this particular player build.
