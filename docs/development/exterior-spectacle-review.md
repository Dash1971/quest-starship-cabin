# M6.4 exterior scale and material study

**Post-review note (September 6):** merged as `46447d5`, whose record reports Unity/bake/capture/APK/install validation. The user reports a substantial exterior improvement but unchanged chess highlights. The evidence sections below describe the original authoring stage; see [M6.5 chess diagnosis](chess-lighting-review.md) for the follow-up.

The M6.3 headset feedback was clear: cabin illumination improved, but the exterior change was hard to notice, and chess pieces caught distracting mirror-like highlights. This candidate makes the exterior composition visibly larger and adds layered planetary scenery. It starts from OpenClaw's reviewed `637b15f` and retains its controls, privacy, timeline and portable build fixes.

## Intended visible changes

| View | Change to inspect |
| --- | --- |
| Great Weather | Giant apparent diameter grows from roughly 42° to 80° at the reference seat, with a 140,000 km physical diameter. The window crops the globe and broad rings. Original 4096×2048 weather adds a large vortex, layered shear and neutral cream belts. Directional shading, a separate atmosphere limb and shared ring/moon shadows establish shape. The moon begins behind a dense band, then fully clears it over the existing event. |
| Blue Morning | A much larger curved horizon with original fictional continents, coastal shelves, cloud systems, cloud shadows, ocean glint and masked night settlements. Surface and external atmosphere use the same sunrise direction and deterministic preview clock. |
| Formation | The existing flight paths and models are scaled together by 12: vessels now occupy hundreds of metres of space. Quarter-side poses expose hull length; small occupied-deck windows and service markings add human scale. A crescent world sits behind the flight. |
| Harbour | Station, traffic routes and clearance volumes receive the same 12× scale. Several hundred warm residence apertures and a planetary horizon add depth behind existing docking activity. Clearance reports now account for transformed radii in world metres. |
| Chess | Ivory and ebony pieces use matte nonmetallic materials with specular highlights and environment reflections disabled. Baked and diffuse room lighting still illuminate them. |

Quiet still resets event state; hold-B still seeks to a readable preview; Formation remains visibly underway in Still. First Question retains its existing sky. The shader distance projection continues to calculate each eye independently.

## Cost and limitations

Blue-world detail is baked into two shared 2K RGBA textures instead of evaluated through the former runtime fractal shader. Its surface uses four texture samples; atmosphere is an analytic shell, without a volumetric raymarch or fullscreen pass. Android textures use ASTC 6×6 and mipmaps. The new maps plus the 4K weather upgrade add approximately 6 MiB of compressed texture storage with mip chains, before driver overhead. This is an estimate, not measured device allocation.

Each atmosphere is another draw and 16,128 triangles; each new blue backdrop also adds a 16,128-triangle surface. Only the selected vista renders, though loaded textures can remain resident. Command detail uses two combined meshes included in LOD0/1; harbour residence windows use one. Fine procedural ring bands fade with pixel footprint to reduce aliasing. The camera far plane increases to 5 km to contain the physical fleet and planetary proxy geometry. The 8 m room shadow distance is retained; distant ship and station relief continues to rely on directional lighting, geometry and authored material occlusion rather than new long-range shadow maps.

The atmosphere is an artistic scattering approximation, and the planets are fictional, original procedural assets. A larger planet does not by itself establish convincing scale or long-session comfort. Unity shader compilation, fresh lighting, seat framing, stereo, depth precision, atmosphere/ring ordering, sustained 72 Hz and thermal behavior remain unverified for this candidate.

## Evidence available here

- All 26 existing timeline checks pass, including Quiet reset and direct readable preview behavior.
- C# syntax parses in all 36 source files with Editor/Android and Android symbols. This is not Unity API compilation.
- Independent geometry checks pass: moon visibility and shadow clearance, 80.1° apparent diameter, physical diameter and per-eye/reference-seat projection.
- Weather and blue-world asset checks pass: generator agreement, deterministic output, channel contracts, seam continuity and import colour space.
- Atlas images and a CPU Great Weather composition study were inspected. The CPU study excludes the cabin, URP, XR, MSAA, bloom and actual headset display; it is not a rendered Unity capture or visual sign-off.

Run source checks with .NET 8 and Python/NumPy 2.3.5:

```sh
dotnet run --project tests/QuietWatch.Checks -- .
python tests/check_weather_geometry.py
python tests/check_weather_atlas.py
python tests/check_blue_atlas.py
```

The checked-in images are build inputs; regeneration is optional unless editing their generators:

```sh
python tools/generate_great_weather.py
python tools/generate_blue_world.py
```

An optional diagnostic requires Pillow as well: `python tools/preview_weather_composition.py` writes an explicitly labelled CPU study under ignored `Builds/ArtReview/`.

## OpenClaw review and build

1. Check out this PR branch in Unity **6000.5.2f1**. Regenerate the scene; opening the old saved scene alone will not apply these changes. Use **Starship Cabin → Quiet Watch → Regenerate, Bake and Build Review APK**. This runs scene checks, performs the existing portable CPU bake and builds. In batch mode use `StarshipCabin.EditorTools.QuartersSceneSetup.RegenerateBakeAndBuildReview`, with graphics available for the bake.
2. Run **Starship Cabin → Quiet Watch → Capture Fixed Seats** after the bake, or execute `StarshipCabin.EditorTools.QuietWatchCaptureTool.CaptureAll`. Expect **34 images**: the existing 33 plus `chess-matte-material-review.png`. Preserve the source-stamped manifest and baked APK provenance. New scene checks reject missing backdrop meshes, unsupported shaders, incorrect world scale and glossy chess materials.
3. Compare Great Weather and Blue Morning across all four seats with the installed M6.3 build. Inspect ring-shadow/limb aliasing, transparent ordering, visible moon emergence, city-light legibility and the sunrise surface/atmosphere match. Check Formation at 0/10/45 seconds and Harbour's traffic clearances and residence windows. Check new hull markings sit on the mesh. Inspect the chess close-up after the real bake for both diffuse readability and disappearance of sharp reflections.
4. Build/package audit as for M6.3, then review on Quest 3. Proposed version: `2.0.0-m6.4-exterior-study` / `20011`; output remains `Builds/StarshipCabin-QuietWatch-Review.apk`. Keep OpenClaw's no-eye-tracking permission requirement and existing ARM64/signature checks.
5. In headset, check both eyes, each seat, small sideways head movements, distant-sky stability, foreground occlusion and room depth precision. Compare GPU time, memory and late-frame percentage per vista, then run the warmest vista for at least 30 minutes at the requested 72 Hz. Record actual refresh, thermal status and comfort; desktop captures cannot settle these questions.

If the cost exceeds budget, first reduce atmosphere sphere tessellation or the weather import size and compare captured detail. Preserve the changed composition and physical scale when assessing these tradeoffs. Further art decisions should follow the headset comparison: the next step is authored, vista-specific hero geometry and richer slow events once this scale/lighting foundation is convincing.
