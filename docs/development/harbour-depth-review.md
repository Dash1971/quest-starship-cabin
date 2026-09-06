# M8 review candidate: inhabited Harbour and eclipse clarity

Headset feedback on M7 was positive but the eclipse was difficult to notice. This candidate enlarges its moon and shared shadow and makes Harbour's everyday composition more substantial, with visible destinations for traffic. It builds on main `95aa3a1`. Proposed APK: **2.0.0-m8-harbour-depth / 20014**. Unity and Quest validation of this candidate have not been run here.

## What changes

- **Great Weather:** eclipse moon radius 1.8 → 2.7 proxy units (physical diameter 5,400 km). Its occulting sphere uses that same radius, increasing cross-sectional shadow area by 2.25×. The reviewed orbital path, sunlight and shared planet/ring/atmosphere shadow calculation remain coherent. This is still a local shadow on the cloud tops, not a whole-window blackout.
- **Harbour architecture:** the existing imported torus gains an asymmetric freight gantry/control tower, a deep open berth with depth ribs, stepped residences, an offset utility spine and radiator banks. The berth was raised during CPU framing review; the docked tender's centre projects through a pane from all four reference seats. Different seats reveal different parts of the station.
- **Traffic:** a small service tender arrives in a real berth, dwells, and backs out along the same corridor under manoeuvring thrusters. Its round trip is continuous across both endpoints. It is visible in Quiet as well as Living. A larger vessel using the existing CommandShip hull crosses well behind the station on a slower, distant lane. The cutter retains its one Living departure event.
- **Stable infrastructure:** remove the station's tiny attitude oscillation so its structures, berth and flight corridors remain aligned. Ship motion supplies the depth cues.

Holding **right B for 0.85 seconds** still seeks the current vista's event and gives a haptic pulse; release after the pulse. It is not a fast-forward control. Great Weather's unattended event still begins after 15 uninterrupted Living minutes and lasts six minutes. Harbour's everyday traffic needs no B press or event wait; its cutter departure remains the deliberate event preview. There is no in-room backdrop sign.

## Implementation and budget

`ArtSource/harbour-layout.json` is the original authored district and route layout. The generator reads it relative to the Unity project, and build provenance now hashes JSON generator inputs. Regenerate the scene; loading an old generated scene cannot demonstrate these changes.

The 42 structural solids are batched into five additional material renderers at LOD0/1 and four at LOD2, attached to the station's existing LODGroup. Coarse structural meshes use ordinary boxes; near meshes use bevels. Small windows thin at LOD1 and disappear at LOD2. No new texture assets, lights, transparent layers or runtime physics are added. Actual draw calls and GPU costs still need measurement. The distant freighter reuses the CommandShip model/LODs/materials already present in Formation.

The existing pipeline has an eight-metre real-time shadow distance. New district depth therefore relies on geometry, directional face lighting, dark recess materials and occlusion; this change does not promise distant URP shadow-map self-shadowing or enlarge the shadow budget. Judge the berth's lighting in actual captures before approving its materials.

The tender uses an allocation-free clock: 40% outward travel, 10% docked, 40% return and 10% rest at origin. One cycle takes 180 seconds in Living and 260 in Quiet. Integrated mode clocks preserve its pose on Quiet/Living changes. It retains corridor-facing orientation while backing out, without an instantaneous turn at the berth. The far vessel has a 360/480-second pass with endpoints well away from the reference forward composition; inspect extreme head angles for any visible loop reset.

Clearance audits now check each imported ship's mesh bounds against its envelope, not just the origin. The old cutter envelope was too small; enlarge it and move its berth two station units outward. New solids generate matching oriented-box clearance volumes, while the imported torus/core/piers retain their conservative sphere envelopes.

## Source-side evidence

- 36 tests run the actual C# event/shuttle clocks, including dwell, continuous cycle boundaries, reentry, mode changes and replay.
- C# parser checks cover 39 files; these do **not** substitute for Unity API compilation.
- 4,001 samples per route clear the imported station envelopes and every new structural solid. Minimum sampled gaps: cutter 13.83 m, tender 17.70 m, freighter 192.81 m.
- Traffic separation passes two hours of samples at 0.25-second intervals for Quiet, Living and a blended travel clock. This is sampled evidence, not a continuous collision proof.
- The docked tender's centre clears the four fixed-seat glazing openings. At Living 87 seconds, the couch ray to the tender intersects the actual outer torus band while passing through the cabin glazing: the station can provide the intended occlusion cue.
- Existing eclipse orbit, shared-shadow, per-eye ray and pane-centre tests pass with the enlarged radius. These do not prove that every edge of the moon/shadow stays unobstructed during head movement.
- Existing weather, blue-world atlas and cloud-relief checks remain part of CI.

`tools/preview_harbour_layout.py` creates labelled CPU massing studies for the couch. They approximate the imported ring and ship boxes and mask the cabin panes. They omit real asset detail, lighting, stereo and image processing and must not be presented as Unity captures or artistic acceptance.

## OpenClaw review and build

1. Use Unity **6000.5.2f1**, regenerate, bake and build through **Starship Cabin → Quiet Watch → Regenerate, Bake and Build Review APK**. Confirm version **20014** and matching source/bake/APK provenance.
2. Run the scene and traffic audits. Added Unity assertions check district LOD membership, lighting layers, shader validity, nondegenerate geometry, ship bounds, tender dwell, mode continuity and seek order.
3. Run **Capture Fixed Seats** after the bake. Expect **58 PNGs**: the previous 42 plus `harbour-depth-{000,010,032,087}s-{seat}.png` for all four seats. Keep the manifest, chess-lighting report and traffic-clearance report with the build.
4. Compare against M7 from the same seats. At Harbour 0/10 seconds, assess whether the station feels different immediately; at 32 seconds inspect the occupied berth; at 87 seconds inspect torus occlusion. Check ship noses, return motion, berth lip clearance, frame cropping, far-ship visibility and LOD transitions. Take a short continuous recording as well as stills; stills cannot establish motion quality.
5. In Great Weather, test the actual controller: hold right B until its pulse, release, and inspect the larger cloud shadow. Compare Quiet with the preview from each seat and both eyes. If still hard to recognize, adjust composition from headset evidence before adding another effect. Check that holding B through a vista transition does not confuse review; wait for the transition to finish first.
6. Check Quiet/Living, Still/Drift, leaving/reentering, pause/focus loss, chess and the absent sign. Then run a warm 30-minute Quest 3 session at the requested 72 Hz, with GPU/CPU timing and thermal evidence. No comfort/performance approval is implied by source tests.

M8 remains an art-review candidate until the first-ten-seconds comparison and sustained headset session support it. Next, refine the winning station/ship shapes and materials from those captures; then carry the scale/detail treatment into Formation and move on to Blue Morning's atmosphere and sunrise.
