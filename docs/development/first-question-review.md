# M12 — First Question: a coherent passage through quiet space

Base: merged M11 **a3f2754**, including OpenClaw's chess-display UV/import fixes. Proposed build: **2.0.0-m12-first-question / 20018**. This is a source candidate; Unity compilation, GPU audit, bake, final captures and Quest acceptance remain pending.

## What failed in the previous motion

The cabin window faces world -Z. In Unity, a camera looking that way has its right axis along **-X**. M11 moved the stellar points along -X, so they moved right on screen. Its numerical test checked positive accumulated travel without projecting through the real camera. The desk/bow lies toward -X, so the correct relative motion of space is **+X**, which projects left through the window.

M11 also combined a moving point volume with separate angularly scrolling shader layers. Their independent speeds and depth models could read as particles sliding over a static background. M12 replaces the entire First Question star display with one spatial catalogue, used in both Still and cruise. The shared background shader bypasses its old star layers, galaxy texture and comet in this vista. The legacy background update loop also stops while the unified field owns the view. Other vistas retain their existing background.

## Art direction and implementation

The composition keeps black space, broad variation in stellar brightness, restrained warm/cool colours, two warm/cool doubles, and two small associations made entirely of individual stars. There are no dust/cloud cards, diffraction spikes or time-driven twinkle. Steady stellar points fit the absence of atmospheric turbulence; [NASA explains the atmospheric origin of twinkling](https://starchild.gsfc.nasa.gov/docs/StarChild/questions/question26.html).

The original deterministic catalogue contains **12,288 points** in one 16-bit-indexed draw: 49,152 vertices / 24,576 triangles. This is the total catalogue, not a simultaneous visible count. All points share the same +X translation at 96 proxy units per second; depth varies from 8,000–44,000 proxy units. The typical angular travel is slow, with a central maximum of approximately 0.69 degrees per second. Different speeds on screen emerge from projection and distance. There is no separate stationary star layer, camera translation, attitude wobble or near-plane approach effect.

The existing two-second integrated easing governs start and stop. Positions are conserved through stop/start and Quiet/Living changes. Entry/reentry still starts stopped. Sector recycling occurs at zero opacity; recycled stars receive deterministic new depth/height instead of repeating the same constellation loop. Double-precision accumulated travel and a bounded shader offset preserve long-session stability. Proxy distances and speed are art-direction units; the shader preserves their per-eye rays while projecting geometry at 12 km to fit the camera range.

Point cores use energy-normalised Gaussian filtering with a minimum pixel footprint, restrained size variation and bounded radiance. This addresses subpixel blinking without adding atmospheric twinkle or elongated streaks. The new catalogue costs more vertex processing than M11's 1,536-point overlay, while the five procedural background star layers are bypassed in First Question. Measure the actual GPU result on Quest; this source pass cannot establish a net performance improvement.

## The comet

The former effect was a huge UV-space head and tail crossing much of the sky in eight seconds. It has been removed. A separate distant object now has a **2.1-degree tail**, a small filtered nucleus, a faint curved dust tail and a narrow ion tail oriented along a world-space anti-sun direction. It develops over **120 seconds** with slow intrinsic motion and shares the cabin-relative cruise translation. The appearance is informed by [NASA's description of comet structure](https://science.nasa.gov/learn/basics-of-space-flight/chapter1-3/), with original procedural artwork.

It appears after 13 uninterrupted Living minutes. Hold B starts at a readable 36-second phase, runs at real time, and positions the comet in the current view even after a long cruise. Quiet cancels it immediately. The clock now records exact travel at the event boundary, so direct captures and 72 Hz playback agree. There is no comet arrival sound or alert. The cabin's existing quiet ambience remains.

A source projection check verifies the full small tail through all four reference seats, including ±8 cm head offsets at preview start. Normal cruise can subsequently carry it behind a window frame; that is ordinary occlusion, not an event reset.

## OpenClaw build and acceptance

1. Review, regenerate and bake in Unity 6000.5.2f1. Confirm **20018**, source stamp and the preserved desk/chess fixes. Run the existing scene, shader and traffic checks. The star/comet meshes intentionally contain collapsed point quads that expand in the vertex shader; they must skip lightmap UV generation. The star shader requires target 3.5 for deterministic integer hashing.
2. Run the capture workflow on a working graphics device. It now includes an actual **GPU motion audit**: render a single catalogue point at a time, measure its image centroid before/after 20 seconds, and compare it with Unity's `WorldToViewportPoint` prediction. Sixteen samples cover near/far and left/right view regions across four seats. They must all move left and agree within 0.8 pixels. A reversed shader, mirrored render, missing point or incorrect projection fails capture. Results are source-stamped in `Builds/Validation/first-question-motion.json`. This isolates projection/shader behaviour; it does not establish headset comfort or final stereo quality.
3. Expect **118 normal PNG captures** (M11's 95 plus three longer comet checkpoints, twelve longer cruise views, and eight seated comet-preview views). Cruise checkpoints are 0/4/12/45/120/600/3600 seconds. Comet checkpoints include arrival, development, late phase and disappearance, plus hold-preview at 0/20 seconds from every seat. Inspect First Question at native pixel resolution: circular stable cores, black gaps, distinct doubles/associations, no grid seams, no old meteor or fixed star layer. Check the other four vistas and desk/chess regression captures.
4. In the headset, use **short B** to start/stop, then watch at least 30–60 seconds from the couch and desk. The ship should feel headed toward the desk; the whole exterior should pass left with coherent depth. Turn and translate the head gently: the field should stay outside, the room remain stationary, and stars remain steady. Verify short-B suppression after a hold, pause/focus, stopping mid-cruise and reentry reset. Use **hold B** for the small comet; ordinary B is cruise, not a fast-forward button.
5. Record clips from all seats and profile a warm 30-minute 72 Hz session, including later sector transitions. Check pixel shimmer, no visible recycling, no distracting bloom, comfort, late frames, GPU/CPU time and thermal status. Source checks are not a substitute for this acceptance.

## Reproducible source studies

The .NET harness runs **64 clock/ray/field checks**, syntax-parses the C# sources, and can export the exact generated catalogue:

```sh
dotnet run --project tests/QuietWatch.Checks -- . /tmp/first-question-catalogue.json
python tests/check_first_question.py /tmp/first-question-catalogue.json
python tools/preview_first_question.py /tmp/first-question-catalogue.json /tmp/first-question-study
```

The last command additionally needs Pillow. Its images are labelled **source projection only** and include the real glazing mask, point positions/filtering and Unity camera handedness. They omit room rendering, glass, bloom, MSAA and headset optics. CI uses the exported catalogue to verify all visible points move left through all four seat projections at 0/10/60/120 minutes, sufficient field coverage, fixed depth, preview tail framing and exclusion of the old backdrop layers. It also runs all existing geometry/atlas/chess suites.
