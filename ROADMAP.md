# Starship Cabin 2.0 Roadmap — The Quiet Watch

This is the active roadmap on branch `v2/quiet-watch`. It replaces the previous palette-swap destination and random-event direction.

Full vision:

- [The Quiet Watch — browser document](docs/design/the-quiet-watch.html)
- [The Quiet Watch — shareable PDF](docs/design/starship-cabin-2.0-the-quiet-watch.pdf)

## Product thesis

Starship Cabin is a restorative starship-officer fantasy.

The room is shelter. The view is vocation.

The player returns from duty to a warm private cabin and looks through the glass at something vast enough to recall why they chose this life: curiosity, wonder, fellowship, belonging, and home.

Version 2.0 is not a catalogue of colour presets. It is five genuinely different living vistas, each with:

- its own silhouette and spatial composition
- a coherent scale ladder
- destination-specific lighting and sound
- slow, causal environmental motion
- one authored grace note after a long period of stillness
- explicit comfort behaviour

## Current foundation

The branch begins from the tested Milestone 8 `main` baseline at `25e1d818`. It deliberately does not inherit the experimental planet/destination code.

The proven foundation remains:

1. Procedural Crew Quarters shell and sloped four-pane observation window.
2. Warm interior, couch, bed, desk, plants, chess/library decor, and fitted book labels.
3. Couch, bed-sit, bed-lie, and desk anchors with fade transitions.
4. OpenXR head tracking, URP, baked cabin lighting, HDR/bloom trial, and fixed foveated rendering.
5. Layered cabin ambience and the V3 star shader.
6. A sideloaded Quest 3 build path and stable rollback points.

The historical `beta/m9-opus-fix` and `newhope` branches remain available for comparison. They are not the 2.0 foundation or active roadmap.

## Delivery status

- **M0 architecture — implemented:** vista lifecycle, central transition owner, local settings, fixed capture points, and frame telemetry are in code.
- **M1 first playable — built, validation open:** `The First Question`, the one-vista diegetic selector, and comfort choices build successfully as isolated package `jp.openclaw.starshipcabin.quietwatch`. Fixed-seat desktop renders pass. Quest installation, live telemetry, and the required sustained comfort/performance sessions remain before M1 exit.
- **M2–M6 — not started:** the four later vistas and release-unification work remain roadmap items.

## The five living windows

### I. The First Question — curiosity

Stars only: a deep galactic river, dark dust structure, varied stellar colour, and true black negative space. This is the purest expression of being in space.

Grace note: one distant comet crossing a small part of the field after prolonged stillness.

### II. The Great Weather — wonder

A ringed gas giant rebuilt from first principles: dimensional cloud flow, storms, atmospheric limb, ring shadow, moons, and a coherent day/night terminator.

Grace note: a moon slowly emerges from ring shadow.

### III. The Long Formation — fellowship

Original ships hold station at multiple distances. Minute navigation corrections, shared light grammar, and disciplined spacing create companionship without combat.

Grace note: the formation slowly turns toward a distant objective.

### IV. Harbour of Ten Thousand Lights — belonging

A colossal orbital station extends beyond the window. Habitat rhythms, docking spars, beacons, and tiny service craft provide a readable scale ladder and the feeling of safe harbour after a long voyage.

Grace note: a station hemisphere enters its night cycle.

### V. Blue Morning — home

An Earth-like curved horizon with layered cloud decks, ocean and land breakup, sparse night lights, and dawn. `Still` presents an orbital tableau; optional `Drift` adds slow atmospheric passage.

Grace note: sunrise reaches the cabin and warms its reflected light.

## Living-vista format

A normal session has a quiet dramatic shape:

1. **Arrival — 0:00–1:30:** a short blackout masks the swap; exterior, cabin light, and sound settle together.
2. **Stillness — 1:30–12:00:** the environment moves only at geological, orbital, or formation speed.
3. **Grace note — 12:00–18:00:** one destination-specific authored event arrives.
4. **Rest — 18:00 onward:** the scene returns to quiet continuity and does not loop spectacles.

The officer makes only three decisions:

- vista
- `Quiet` or `Living`
- `Still` or `Drift`

Manual choice is immediate. Auto-tour is optional and off by default.

## Delivery sequence

Every milestone ends with:

- a regenerated Unity scene and clean APK
- installation and launch on Quest 3
- fixed-seat captures from couch, bed-sit, bed-lie, and desk
- frame-time evidence
- a 20-minute comfort review where relevant
- README/roadmap status updated in the same commit

### M0 — Clean break and vista architecture

- Remove weak or inactive 2.0 claims.
- Keep existing stable, beta, and New Hope apps/branches as rollback evidence.
- Implement a clean environment lifecycle: visual root, lighting profile, sound profile, comfort policy, transition hooks, and deterministic unload.
- Add fixed capture viewpoints and lightweight CPU/GPU frame-time logging.
- Establish source and license provenance for every public/runtime asset.

Exit: test environments swap without stale geometry, audio, lighting, or allocations; captures and frame-time evidence are repeatable.

Status: architecture and tooling implemented in the M1 first playable. Multi-vista unload verification remains part of the M2 implementation because only one production vista exists today.

Planning range: 2–4 focused days.

### M1 — The First Question plus real selector

- Rebuild stars-only space as the visual benchmark.
- Separate infinity, galactic structure, and restrained near-depth cues.
- Add the minimal diegetic destination selector.
- Make `Quiet` and `Still` the default.
- Save the last vista and comfort settings locally.

Exit: the view reads as space beyond glass rather than a texture on glass; it holds attention without a planet; selection works within five seconds; Quest 3 sustains the performance target.

Status: first playable built and fixed-seat desktop renders validated on 2026-09-03. Quest installation, live 72 Hz evidence, and sustained headset review remain open exit checks.

Planning range: 4–7 focused days.

### M2 — Harbour of Ten Thousand Lights

Build the station before another planet. This is the first proof of original art direction, modular geometry, scale cues, contextual motion, and selective asset authoring.

- Develop a modular station kit with authored silhouettes and LODs.
- Compose the hero station beyond the window.
- Add restrained beacons, habitat lighting, docking spars, and service routes.
- Add destination light spill and sound.
- Author the night-cycle grace note.

Exit: the station reads as kilometres wide within five seconds; service craft provide scale; traffic is calm; no hero object reads as a primitive.

Planning range: 1.5–2.5 focused weeks.

### M3 — Blue Morning

- Build the curved horizon, atmosphere, land/ocean breakup, cloud layers, and sparse night lights.
- Implement `Still` and `Drift` as meaningfully different comfort modes.
- Couple dawn colour to restrained cabin light.
- Author the sunrise grace note.

Exit: `Still` and `Drift` pass a 20-minute seated comfort test; horizon/cloud layers remain stable under head movement; motion never starts unexpectedly.

Planning range: 1.5–2.5 focused weeks.

### M4 — The Great Weather

- Replace the experimental gas giant rather than extending its palette framework.
- Add dimensional band and storm flow, atmosphere scattering, ring shadow, moons, and day/night logic.
- Add destination-specific light and sound.
- Author the moonrise grace note.

Exit: the world is visibly different from a flat striped sphere; rings and moons establish scale; the night side and reflected cabin light remain coherent.

Planning range: 1–2 focused weeks.

### M5 — The Long Formation

- Extend the original ship language established around the station.
- Create two ship families and three distance tiers.
- Add minute station-keeping corrections and readable navigation lights.
- Author one slow formation turn; no combat or patrol loop.

Exit: ships read as designed vessels rather than boxes or specks; formation depth is immediate; motion does not feel like sprites sliding across the window.

Planning range: 1–2 focused weeks.

### M6 — The Quiet Watch release pass

- Unify vista transitions, cabin-light response, sound, persistence, and living-vista timing.
- Tune or cut any weak grace notes.
- Remove placeholder-quality content and inactive headline features.
- Complete 45-minute thermal/performance sessions and 20-minute comfort sessions.
- Update public documentation to match exactly what passed on-device.

Exit: all five vistas satisfy the release contract below.

Planning range: 1–2 focused weeks.

The likely planning range is 7–12 focused weeks. M1 is the calibration milestone; its actual Quest performance and reference-to-headset visual gap determine the revised forecast.

## Quest production strategy

Breathtaking does not require modelling the universe. It requires spending the budget where human vision reads scale:

1. **Far field:** sky dome or procedural star field, galactic dust, nebular haze, and atmosphere at infinity.
2. **Hero field:** one planet, station, or fleet composition with authored silhouettes, LODs, baked detail, and selective shaders.
3. **Scale cues:** sparse moons, docking craft, windows, cloud shadows, and navigation lights.
4. **Cabin response:** one controlled exterior spill light, restrained glass response, and destination sound.

Procedural code remains useful, but it is not a purity rule. Authored meshes, textures, matte layers, and baked assets are allowed when they materially improve the Quest result and have explicit compatible provenance.

Target sustained refresh: 72 Hz minimum on Quest 3, with target CPU and GPU frame times at or below 11 ms and no thermal collapse during a 45-minute session.

## 2.0 release contract

Version 2.0 is complete only when:

- all five vistas pass a blind silhouette test before labels and colour
- any vista can be selected and entered within five seconds
- no vista is a palette variant of another
- movement follows the destination's physical logic
- each vista contains no more than one grace note per session
- there is no forced camera motion
- atmospheric and formation scenes default to `Still`
- relevant 20-minute comfort tests pass
- Quest 3 sustains 72 Hz with measured headroom
- no blob characters, primitive hero objects, inactive headline features, or unverified roadmap claims remain
- all content is original or compatibly licensed, with recorded provenance

The final test: the player takes off the headset and feels that the real room has the wrong window.
