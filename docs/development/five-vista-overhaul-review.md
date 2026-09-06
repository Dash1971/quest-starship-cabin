# Five-vista overhaul — M9 review candidate

This pass responds to the request for a much larger visual step across **all five backdrops**. It builds on merged M8 (`9ce2d58`), rather than extending an unmerged branch. Proposed version: **2.0.0-m9-five-vista-overhaul / 20015**. This is source implementation, not headset visual acceptance; Unity compilation, regeneration, captures, APK and device testing remain OpenClaw work.

## Arrival compositions

| Vista | Immediate change |
| --- | --- |
| First Question | Original 2K spherical galactic panorama with an asymmetric stellar bulge, broad dust lane, smaller extinction filaments and faint emission regions. It replaces the low-contrast procedural haze. Existing clean stars remain in front of it, with reduced stellar light through dense dust. Planetary vistas use much lower galactic exposure. |
| Harbour | A much larger blue planetary horizon behind the station, with consistent sunlight across the world and structures. The station receives baked geometric visibility and ambient occlusion; ships get geometric ambient occlusion and directional hull shading. This supplies structure beyond the old eight-metre shadow-map range. Existing districts, berth service and cutter departure remain. |
| Blue Morning | A close polar-orbit composition, approximately 133° planetary diameter at the reference eye, using upgraded 4K terrain/cloud/city maps. Clouds occupy a separate shell at roughly 11.5 km altitude. An auroral oval at roughly 153 km sits on the visible twilight/night side. The sunset lighting, green/violet curtains and cloud separation are present without waiting for an event. |
| Great Weather | Regenerated 4K cloud art with a much larger turbulent anticyclone, darker belts, brighter zones and stronger relief. A 14,000 km companion moon sits outside the giant's silhouette, with a geometric shadow shared by the planet, rings and atmosphere. The established moving eclipse remains a separate event. |
| Long Formation | Flagship scale 0.96 → 1.65, broadside orientation and enlarged, repositioned escorts. The flagship is approximately 436 m long using the source hull length. The ocean-world horizon is substantially larger. Hull shading gains geometric occlusion and warm directional sunlight with cool ambient fill. |

The room, chess treatment and absent backdrop sign are preserved. Quiet/Living and Still/Drift remain available. New atmosphere animation consumes the existing deterministic observation clock; no global shader time or independent runtime update loop is introduced. The companion moon has a permanent shadow in Quiet; the smaller moving eclipse resets with the event timeline. Holding B remains an event seek, not time acceleration.

## Lighting implementation

`QuietWatchHullLighting` reads the highest-detail source geometry and builds an editor-only triangle BVH. Four hemisphere rays per vertex estimate local ambient occlusion. A sun ray adds fixed station visibility. Moving ships retain only ambient occlusion, avoiding a baked sun shadow stuck to a rotating hull. The actual ray implementation is exercised in the .NET source harness, including two-sided/parallel/bounded rays and 1,000 BVH-versus-brute-force queries.

The generator creates separate meshes/materials under `Assets/Generated/ExteriorLighting`, leaving FBXs and source materials intact. Visibility and occlusion use four-byte vertex colours. Imported UVs, tangent frames, material submeshes, hull textures, normal/metal/smoothness/occlusion maps and emission are retained. Repeated generation reuses the generated asset paths. Per-assembly `Builds/Validation/lighting-*.json` reports record mesh/vertex counts and nontrivial shading/shadow counts.

This is vertex shading, not a high-resolution shadow atlas. Coarse triangles can smear shadow boundaries; inspect large station panels and LOD changes carefully. The station remains fixed to its baked sunlight. Moving ships do not gain mutual shadow casting in this pass. No runtime physics, extra lights or expanded shadow cascades are introduced.

## Framing and depth

The first aurora placement was outside the visible polar latitudes. Source-space ray checks now put its oval in visible night-side glazing from all four seats. This proves a sampled geometric opportunity to see it, not its final brightness, stereo quality or appearance.

Larger planetary proxies initially risked depth-testing in front of physically nearer ships. Harbour/Formation proxies now sit at least 1,800 proxy metres behind the reference eye, with physical scale adjusted to preserve Earth radius and apparent size. The camera far plane is 20 km to contain their atmosphere meshes; the cabin near plane is unchanged. Verify thin hull details for depth artifacts on Quest.

The Great Weather companion's mesh and static shadow uniform share one position/radius. It clears the giant's volume. The existing eclipse orbit, ring emergence, shared shadow geometry, per-eye projection and preview pane-centre checks remain part of validation.

## Cost and tradeoffs

- First Question trades multiple fragment-noise evaluations for one panorama sample. The 2K panorama adds approximately 1.2 MiB of ASTC 6×6 texture/mip storage.
- Two world maps grow from 2K to 4K. Together with the panorama, the approximate additional compressed texture storage is 8.3 MiB, excluding driver alignment and working memory.
- Blue Morning adds **two transparent sphere draws**: clouds and aurora. These occupy a large part of the view; measure their overdraw/GPU time on the actual headset. The other two blue-world vistas retain their single-surface cloud treatment.
- Hull vertex colours and generated geometry/material copies add memory. The existing LOD groups are preserved, but increased ship screen coverage can select higher LODs more often.
- The additional static companion shadow adds a second analytic sphere test to Great Weather receivers. No volumetric raymarch, fullscreen post effect, particle field or new real-time light is introduced.

Source checks do not establish 72 Hz. If device profiling identifies a problem, preserve the intended composition while optimizing the expensive pass. Do not approve a desktop frame as a substitute for stereo and thermal evidence.

## OpenClaw build and review

1. Regenerate, bake and build in Unity **6000.5.2f1** using the existing review menu. The new geometric lighting bake runs during scene generation, before the cabin lightmap bake. Confirm version **20015**, matching source/bake/APK hashes, and nonempty lighting evidence files.
2. Run the Unity scene checks. New assertions verify hull shader compilation, baked vertex channels, nontrivial shadows/occlusion, raised cloud/aurora layers, observation-clock propagation, galactic exposure reset and the far plane. Retain existing scene, traffic, chess, permission and provenance checks.
3. Capture fixed seats after baking. Expect **62 PNGs**: the previous 58 plus `blue-morning-orbital-600s-{seat}.png` from four seats. Preserve the timestamped capture directory and manifest. Compare the 20 Quiet arrival views against M8 at identical camera positions.
4. Record a short headset clip of each vista on arrival and during small head movements. Priorities: First Question dust-lane colour/seams; Harbour shaded structure and readable berth; Blue Morning cloud depth/aurora stability; Great Weather storm placement, companion and both shadows; Formation broadside size, separation and hull detail. Check both bed positions as well as couch and desk.
5. Test the actual B-button preview, transitions, Quiet reset, Still/Drift, pause/focus and reentry. Inspect aurora and cloud animation at arrival and ten minutes. There should be no flashes, sudden camera motion, exposure leakage or event-state leakage.
6. Run a warm 30-minute Quest 3 session at the requested 72 Hz. Capture CPU/GPU/frame-time and thermal evidence, including Blue Morning's two new layers and the close Formation's LOD0 cost. Approve appearance, comfort and performance separately.

CPU weather composition studies remain explicitly labelled **not Unity/headset captures**; they approximate shading and omit room occlusion and complete moon terrain. All new sky/weather imagery is generated from the checked-in original scripts, with no external image licensing dependency.
