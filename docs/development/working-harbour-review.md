# M10 — working Harbour, stellar cruising and cabin craft

Candidate **2.0.0-m10-working-harbour / 20016**, based on merged M9 `62fd098`. The user reports that M9 is much better, but Harbour lacks scale, lighting and activity; separate blue discs float behind formation ships; First Question should emphasize stars and empty space; the room needs more convincing objects. This pass addresses those points. It is source work pending Unity and Quest validation.

## Harbour design

The port now has a large curved drydock arch, segmented guidance lighting, an elevated transit concourse, terraced residential towers, terminal halls and an asymmetric long docking pier. The combined silhouette spans roughly 1.2 km in fleet metres. There are 70 authored blocks and one arch, retained in the original three LOD levels and five district material batches per LOD. Existing imported core/rings, berth and cutter departure remain.

Occupied windows use an opaque unlit HDR material. Previously, the coarsest district LOD deleted all windows; lower detail now merges neighbouring apertures while keeping similar luminous area. Work strips are physical ceiling fixtures inside the berth. The hull shader adds warm analytic illumination limited to its interior, using station-local positions/normals stored in generated mesh UV channels. It has no extra realtime lights or transparent light cones; this is an art-directed approximation, not ray-traced local illumination. Inspect ribs for any missing local occlusion and large panels for interpolation artifacts.

Three additional lanes bring the total to six: a passenger liner behind the concourse, an upper courier and a nearer cross-port commuter. Five craft can run in Quiet; the customs cutter still belongs to the Living departure. The commuter's arrival position projects through a pane from all four reference seats. Extended through-routes reset beyond the window sightlines. The existing tender still docks at 32 seconds in Living and is occluded by the torus during its return. Additional low harmonics make Harbour's machinery bed less dependent on very low bass; volume is unchanged.

Every new architectural block and the arch's conservative envelope participate in traffic validation. CPU checks sample 4,001 points per route and two hours of traffic in Quiet, Living and mixed clocks. This establishes sampled clearance, not a formal continuous proof for every possible preview or mode-switch history. Unity's independent imported-mesh envelope and scene route audit must also pass.

## Detached engine lights

The source FBXs already contain recessed, emissive engine apertures in all three ship LODs. The runtime generator added separate spheres at generic positions: command aft Z=10.4 and escort Z=6.8 with common escort X=±1.15. The source pods differ by family and include their own offset/radius. Those additional spheres can sit outside the actual nozzle geometry, particularly after the broadside composition exposes the separation.

The redundant sphere generator is removed. All ships now use their actual imported aperture geometry/materials. No FBX axis or ship transform is changed. The old sphere pulse component is no longer instantiated on new ships; hull navigation/emission remains. Unity review asserts no `Drive Glow` object and luminous imported material slots at every ship LOD. Inspect fore/aft/side views on the real build to confirm the reported artifact is gone.

## First Question

The galactic/dust exposure is zero for First Question. Other vistas retain their low background galactic exposure. The distant starfield remains fixed, while one additional draw contains 1,536 depth-resolved stellar points. Optional forward travel moves these through a bounded volume, fading completely before depth recycling. The shader expands small circular cores per eye; there are no streaks, camera translation or cabin movement. This is a perceptual cruise effect, not a simulation of realistic interstellar velocities.

- **Tap B in First Question:** start/stop cruise. Stick-click controls the same motion mode.
- **Tap B elsewhere:** existing Quiet/Living toggle.
- **Hold B (~0.85 s):** existing event preview; in First Question, the comet starts from its beginning. A hold does not also toggle cruising on release.
- **Entry/reentry to First Question:** starts stopped, including application restart. Leaving cancels cruising so it cannot carry into other vistas.
- Cruise uses the shared analytically integrated timeline with a two-second easing time constant. Stop/start preserves accumulated position. Pause/focus loss freezes it. Captures evaluate the same integrated distance.

The previous global motion choice is reset when crossing First Question's boundary. First Question's short B no longer changes Quiet/Living; holding B still enables Living for the comet, and changing life elsewhere remains available. Reassess that interaction after trying the new cruise control. Check both eyes, tiny head movements, repeated start/stop, sustained travel, focus loss, and leaving/reentering. Still must remain an entirely valid way to enjoy this vista. The new visible optic flow requires fresh comfort approval.

## Cabin craft

Books now have separate cloth bindings, covers, recessed page blocks and fine spine rules. Labels follow the actual rotated binding. Original titles and chess position are preserved. Small original 512px wood/weave/paper maps add restrained material detail with mipmaps and ASTC compression. Couch/stool cushions and pillows use rounded, softly crowned meshes; the stool has real legs and a seat rail.

A compact dark computer and keyboard fit beside the books, facing the desk seat. Its dim static reading-page display is decoration, not an interactive computer or backdrop indicator. Drawer hardware and a ceramic tea cup/tray beside the chessboard add a few domestic details. The exterior-view sign remains absent. No alerts, task UI, speech or new screen interaction is added.

## OpenClaw validation and handoff

1. Regenerate with Unity 6000.5.2f1; confirm **20016**, correct source stamp and all new assets. Run scene, shader, imported engine-emission and six-route validation. The new procedural star mesh intentionally skips lightmap UV generation because its point quads expand only in the vertex shader.
2. Perform a fresh cabin bake. Inspect books/page edges, keyboard, rounded upholstery and cup for lightmap artifacts; retain the existing chess probe/material checks. Confirm the generated hull meshes include the two port-space UV channels and lighting evidence files.
3. Capture **81 PNGs**: prior 62 plus 16 cruise frames (0/4/12/45 seconds from four seats) and three cabin craft views. Compare Harbour arrival/10/32/87-second views against M9. The CPU massing study is explicitly labelled **not a Unity render** and cannot validate materials, bloom, stereo or final lighting.
4. Record short Quest clips of Harbour from all four seats, the formation from aft/side angles, First Question start/cruise/stop, and the desk. Check window LOD transitions, geometry scale, visible ship activity, no light discs, black space, stable stellar cores, no visible recycling and cabin material finish. Check traffic endpoints at extreme head angles as well as reference views.
5. Profile a warm 30-minute session at 72 Hz. New cost: three LOD-controlled traffic craft, expanded station geometry, retained far windows, two additional hull UV channels and a local illumination branch, one 3,072-triangle star draw, rounded upholstery and small prop meshes. There are no additional runtime lights or physics. Source/CI success is not a GPU, stereo or thermal approval.

Local evidence: 46 actual clock/ray checks, C# syntax parsing, all existing geometry/artwork checks, extended harbour clearance/framing checks. Unity API/shader compilation, bake, captures, APK and headset acceptance remain pending.
