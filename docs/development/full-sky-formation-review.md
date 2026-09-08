# M13 — Full sky, readable travel, smooth Formation

Base: merged M12 **67f0b40**. Candidate: **2.0.0-m13-full-sky / 20019**. This is a source candidate for OpenClaw review and Unity build. No Unity or Quest execution was available on the authoring computer.

## What the comparison found

The M5/M7 implementation (`931cd64`, `6ae1bf3`, `StarWindow.shader`) filled the glass with a procedural field. Four layers had distinctly different apparent speeds (1, .62, .36, .22), a wide brightness range and occasional clearly visible streaks. These were useful perceptual cues. Its UV-space scroll, time-dependent coordinates and rapid meteor lanes are not suitable to transplant directly into the current world-space/stereo rendering.

M12's catalogue instead constrained every ordinary star to `Y = +/-0.72 * -Z`. That gives an absolute elevation ceiling of **35.75 degrees**. Looking above it could only reveal black, irrespective of star count. The old tests checked four central seated projections and sufficient total counts; they did not inspect the rest of the sky. M12 also distributed most points through a relatively narrow depth range, with little clearly separated foreground travel. Its 2.1-degree comet had a nucleus small enough to become subpixel at review resolutions.

M13 restores full coverage, stronger depth contrast and a readable celestial event within the existing coherent translation model. It does not revive the earlier near-window particle system or add dust clouds.

## First Question

- **24,576 stellar points** occupy complete circular cross-sections around the ship's travel axis, at three transverse scales. Stars exist above, below, behind and alongside the cabin. Each scale has a broad range of actual distances; apparent motion comes from projection.
- The ship's implied bow remains deskward, **-X**. Every stellar source shares **+X** cabin-relative translation at **144 proxy units/second**, up from 96. The four seated window views show leftward travel. Looking towards the flight axis naturally changes the projected flow; the camera and room remain stationary.
- Near references are numerous enough to remain visible from the desk, with median screen travel over twice that of the far population in each reference seat at the tested times. No fixed star layer is left underneath. The existing two-second integrated start/stop easing is preserved.
- Warm/cool doubles and small star associations remain. Rare bright points regain a restrained, constant optical halo, while the majority stay fine points. No atmospheric twinkle, animated fog, star trails or full-screen flashes.
- Near/mid/far cells recycle invisibly at their boundaries. Near cells extend farther along the travel axis to retain motion references in oblique window views, with deterministic new transverse positions. Their periods divide the global shader period exactly, so bounded offsets preserve phase through long sessions. A minimum transverse distance of 8,000 proxy units bounds angular speed to approximately **1.03 degrees/second**, before start/stop easing. These are artistic distance/speed units, not a simulation of interstellar travel at ordinary spacecraft velocities.
- The comet now has a **six-degree tail**, wider warm dust fan, narrow blue ion tail and a resolved nucleus. It still develops over two minutes and shares the stellar translation. Its placement clears the four reference seats at preview start, including head offsets. It appears once after **three uninterrupted Living minutes**, instead of thirteen; hold B still brings it directly into view. Quiet cancels it. It has no notification sound. Cruise may subsequently carry it behind a frame.

### Cost and limits

The point mesh uses one 32-bit-indexed draw, **98,304 vertices / 49,152 triangles**, plus the separate comet quad. This doubles M12's star vertex/triangle count and adds a small halo footprint to rare bright points. There are no new textures, lights, per-frame mesh uploads or managed allocations in the field update. The shared procedural sky layers remain bypassed for First Question. Profile the actual GPU cost; the source budget is not a performance pass.

## Formation

The ordinary sinusoidal flight and station-keeping curves were already continuous. Two concrete discontinuities existed around them:

1. The generic ship builder used three LODs with **no crossfade**. Crossing a threshold changed hull geometry abruptly. The three Formation heroes now instantiate only LOD0 with a zero culling threshold, keeping their silhouettes and details stable. Harbour traffic retains its three LODs. The detailed formation meshes cost more GPU work than their lower LODs would have; check the live triangle count and frame timing.
2. Preview/replay directly set the manoeuvre progress to its readable phase; Quiet directly reset it to zero. Both values fed the fleet rotation immediately. A critically damped response now preserves both the displayed attitude and angular velocity while approaching the new target. A hold becomes readable within a few seconds; cancellation glides back. Event scheduling/reset state still follows the existing Quiet/Living rules.

The flight phases now stay double precision until the final sine/cosine results are converted for transforms, reducing long-session quantisation. Ships remain underway in Quiet/Still. Models, hull lighting, engines, course amplitudes and other vista choreography are preserved.

These changes remove source-level snapping paths. They do not establish that every reported jerk had the same cause; device frame-time spikes or reprojection still require headset profiling.

## Checks and capture handoff

Run the normal source/geometry/atlas/chess suites and:

```sh
dotnet run --project tests/QuietWatch.Checks -- . /tmp/first-question-catalogue.json
python tests/check_first_question.py /tmp/first-question-catalogue.json
```

The .NET harness exercises the actual field and manoeuvre response, including continuity across commands, settling, frame partitions and scheduled capture/playback agreement. It exports actual runtime position checkpoints so the Python projection model can be compared against the C# results. Python checks:

- 108 sky cones at each of 0/10/60/120 minutes, including high elevations and both ends of the flight axis; all must contain stars. M12's slab fails this test.
- Every pane and bottom/middle/top sky neighbourhood from four seats, three standing positions and head offsets. Grazing window tiles can contain few stars because their solid angle is tiny; fixed-angle neighbourhoods prevent that geometry from masking a missing upper sky.
- Leftward seated motion, enough near travel references, and over 2x near/far median screen displacement at each tested time. All 132 GPU audit selections must have persistent references, including diagonal standing views and the global offset rollover.
- Complete comet extent through all reference seats at preview start, including head offsets; no legacy dust, fixed stars or UV-space comet.

In Unity, regenerate and bake, then run the existing scene review and capture workflow. Scene checks now require full-sky mesh data and stable hero LODs, and exercise Formation preview/cancellation without an immediate attitude jump. The GPU centroid audit expands from 16 to **132 samples**: three depths and both view halves across four seated, four upward and three standing views, both on arrival and across the global travel-offset rollover around 15 minutes. This also exercises the shader’s regenerated-star path. It checks the real shader against Unity's projection, retaining the leftward requirement for seated side views. Evidence remains `Builds/Validation/first-question-motion.json`.

The normal capture set adds **21 upward/standing PNGs**, for **139 stills** in the current scene. Comet event checkpoints now follow its actual delay constant. Review new views with the room and glass present; source projections cannot establish their rendered appearance.

Also run **Starship Cabin → Quiet Watch → Capture Motion Clips**, or `StarshipCabin.EditorTools.QuietWatchMotionCaptureTool.CaptureMotionClips` in the normal graphics-enabled batch workflow. This optional pass writes five 18-second PNG sequences at 24 fps, 960×640, under a new `Builds/Motion` directory:

- Cruise from Couch, Desk, Bed Sitting and Standing upper window.
- Formation from the couch: hold preview at 2 seconds, Quiet at 8, Living/replay at 12. A numerical attitude-step guard accompanies the rendered frames.

The output includes source/bake metadata and frame rate. Encode each sequence without changing playback speed, for example from its directory:

```sh
ffmpeg -framerate 24 -i %04d.png -c:v libx264 -crf 16 -pix_fmt yuv420p ../cruise-desk.mp4
```

This pass produces **2,160 frames** and is separate from routine still capture to keep storage and review work deliberate. Do not infer 72 Hz performance from offline capture speed.

For headset acceptance, use short B to start/stop cruise and hold B for the comet. Inspect the upper window from every seat and while standing. Watch at least 30 seconds at the desk and couch, and compare with the bed. The desired result is a stationary cabin moving through layered space, with visible but unhurried passage. Check the comet at preview start and after 20 seconds of cruise. Observe Formation during ordinary travel and repeated previews/Life changes, including after 15+ minutes. Record sustained 72 Hz frame times, missed frames/reprojection and thermal behaviour; report any remaining jerk with a clip and approximate event time.
