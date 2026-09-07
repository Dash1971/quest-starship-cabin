# M11 — A busy, calm port and a personal chess terminal

Base: merged M10 `290b4d7`. Proposed build: **2.0.0-m11-calm-harbour-cruise / 20017**. Source candidate for OpenClaw review; regenerate the scene and perform a fresh bake. Existing M10 APKs cannot show these changes.

## Harbour

Six audited corridors now contain **32 craft**, with **31 ambient craft available immediately in Quiet** and one Living grace-route cutter. Counts are 12 commuters, eight inner couriers, six passenger liners, four freighters, one berth tender and one cutter. The through-traffic copies use evenly spaced phases on the existing slow schedules. The courier corridor moves below the upper district into a more useful sightline, with clearance retested. No need to wait for the grace event to see ordinary traffic.

Small craft previously culled at 2.5% screen height. Their lowest LOD now remains down to 0.4%, with intermediate thresholds at 16% and 4.5%. Each corridor bakes one template, and its copies share the resulting meshes/materials. This does increase visible draws; it does not multiply imported art or the geometric bake by 32.

The old harbour cabin-fill oscillation and grace recolouring are removed. District windows and berth strips now use the **same renderer objects in every station LOD**, eliminating changing window patterns at LOD boundaries. Imported station emission and excessively bright cyan/amber fixtures are reduced. Inspection found no time-driven blinking in the station emission shader; tiny bright apertures and changing LOD patterns are plausible additional causes of the reported flashing, requiring headset verification. Ship engine apertures retain OpenClaw's keyword-independent emission fix and their existing brightness.

CPU checks cover all six paths against station solids, all 496 craft pairs over two hours in Quiet/Living/blended clocks, berth dwell, and through-route resets outside the reference glazing. A separate projection check requires at least 3x mean traffic cadence at the couch/bed seats and 2x at the desk compared with one craft per corridor. **Projected cadence is not rendered visibility:** it excludes station occlusion, final LOD bounds, pixel readability and headset FOV. Thirty-two is the scene population, not a promise that 32 ships are simultaneously visible.

## First Question

Tap B to start/stop cruise, as before. Near stars now translate **right-to-left at fixed depth**, while the distant star layers participate in a slower leftward flow. The old forward approach/expansion cue is gone. The same integrated two-second start/stop easing drives both layers; pausing motion retains accumulated positions. The cabin and camera stay fixed. Maximum central near-star angular speed is approximately 1.3 degrees/second, falling with distance; far stars move at up to 0.144 degrees/second. The lateral volume fades at its wrap edges. Dust remains off.

Hold B still previews the comet; it does not speed up cruise. Entry/reentry still starts stopped. Source clock tests and Unity property-block checks cover deterministic seeking and stop/start; motion comfort and perceived travel direction require a headset clip.

## Desk lighting and terminal

The books and computer were still URP Lit objects receiving lightmaps. They had not received the chess pieces' final Baked Lit + shared light-probe treatment. Twenty-three desk surfaces now use that same diffuse-only contract: five books' bindings/pages/rules, six computer/keyboard surfaces, the lamp stem and drawer hardware. Eighteen additional probes and a shared anchor supply illumination instead of compressed small-face lightmap charts; reflection probes are disabled. Book textures, colours and UV tiling are preserved. Dedicated material copies avoid changing graphite used elsewhere. The successful chess setup remains intact. Book labels remain their existing unlit text, and the lamp's intended luminous head stays luminous.

The screen is one opaque, unlit textured quad with no specular path, runtime UI, chess engine, cursor blinking or new controls. Its original muted-green bitmap shows **Fischer–Spassky, Reykjavik 1972, game 6, after 21.f4**, Black to move. Filled pieces are White; outlined pieces are Black. The legal game sequence/FEN and provenance are in `ArtSource/chess-terminal.json`, transcribed from [ChessBase's game record](https://fr.chessbase.com/post/fischer-vs-spassky-1972-il-y-a-cinquante-ans-50-serie-4-sixieme-partie). No article annotations, screenshots, third-party fonts or piece art are copied. NumPy reproduces the texture; the independent Python chess library legally replays all 41 plies and checks diagram orientation. Python/chess are build-source checks only, not APK dependencies.

## OpenClaw handoff

1. Review and regenerate in Unity 6000.5.2f1. Run the existing scene/harbour/shader audits. Expect 32 traffic components sharing six baked templates, steady station window renderers across LODs, and the static terminal quad. Preserve the source stamp, imported engine emission, validated LOD meshes and portable lightmapper corrections already on main.
2. Run **Regenerate, Bake and Build Review APK**, or the existing equivalent pipeline, with a fresh bake. Build validation now rejects desk surfaces receiving lightmaps, missing shared anchors, missing book textures, unsupported shaders, and black/unbaked desk probes. Confirm version **20017**. Do not reuse the old generated scene or bake.
3. Capture **95 PNGs**: M10's 81 plus harbour Quiet at 120/300/600 seconds from four seats and two desk views offset by ±12 cm. Existing desk/cabin and chess captures remain. `desk-lighting.json` records all 23 surfaces' actual shaders, anchors, GI and lightmap indices alongside `chess-lighting.json` and the source-stamped manifest.
4. Record 2–5-minute harbour clips from each seat, in Quiet and Living, plus modest head movement through LOD thresholds. Look for readable continuous shipping activity, natural occlusion, no bright blinking or geometric window changes, and no visible route resets. Check desk books, computer body and screen while moving the head; compare against the already approved chess pieces. Verify chess diagram readability from the desk seat and lack of flickering fine lines.
5. Record First Question start/cruise/stop: stars should pass to the left, with a calmer distant reference layer and no approaching-the-cabin cue. Verify long-hold preview, short-B suppression after a hold, reentry reset and motion comfort.
6. Complete a warm 30-minute 72 Hz session and report GPU/CPU/late frames and thermal telemetry. Added cost is more visible LOD-controlled traffic, retained near-window geometry at distant station LODs, 18 baked probes and a mipmapped 1024×640 ASTC screen texture (roughly 0.84 MiB including mips). No new runtime lights, shadow maps, physics or UI canvases.

Local checks cover clocks/rays, C# syntax, geometry/atlas integrity, route spacing/cadence and the chess asset. They cannot establish Unity API/shader compilation, baked lighting quality, final visible population or Quest performance. Those remain OpenClaw/headset validation steps.
