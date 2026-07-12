# Roadmap - Crew Quarters V2: The View

This roadmap continues from the tested Quarters V2 Milestone 7 build.

Current tested baseline:

- Commit: `6ae1bf3` (`Add Quarters V2 milestone 7: fitted book labels + starfield V3`)
- Stable rollback tag: `quarters-v2-m7-tested-20260711`
- Tested on: Meta Quest 3
- Installed/launched: 2026-07-11
- Unity: 6000.5.2f1
- Package: `jp.openclaw.starshipcabin`
- APK SHA-256: `4e65a1df79b4cb87fa7d9512757c624d0d8625ce65a02f239c89c6c8137572d4`

## Direction

The next phase is tightly focused on visual awe and ambience:

- a world in the glass
- a menu of destinations
- a living sky
- per-world sound

Do not expand into movable objects, inventories, persistence, or room-memory features. The cabin should remain a comfort-first star-gazing space.

## Concept Panels

### Target View

![Target view concept: Jovian Dawn through the cabin glass](docs/design/view-roadmap-target.svg)

### Destination Moods

![Six destination mood concepts](docs/design/view-roadmap-destinations.svg)

### Roadmap Flow

![Roadmap from tested M7 baseline through M12](docs/design/view-roadmap-milestones.svg)

## Implemented

Milestones 1-7 are shipped and tested:

1. Shell + glazing: procedural Crew Quarters V2 shell, 55 degree glazed hull slope, four rounded-trapezoid window panes, shader starfield.
2. Furniture + palette: couch, bed, raised alcove, desk, console strips, plants, soft-bright material set.
3. Seat anchors: couch, bed-sit, bed-recline, and desk anchors with fade transitions.
4. URP + baked lighting: cove-lit baked GI, one mixed runtime light, legacy cleanup.
5. Audio V2 + media wall + star motion: layered ambient bed, brown noise, local video wall, lateral star motion, light fixes.
6. Decor pass: procedural chess set and library decor.
7. Book labels + starfield V3: fitted book labels and dark-sky star shader upgrade.

The M5 media/video wall remains in the M7 baseline but is being retired because Quest system overlays can provide media apps without distracting from star-gazing.

## Active Milestones

### M8 - Clear the Deck + HDR Trial

- Retire the media/video wall.
- Remove `MediaScreenController` and generated scene objects.
- Enable HDR + bloom.
- Pair the visual trial with fixed foveated rendering.
- Build, install, and verify frame comfort on Quest before adding planet geometry.

Pass condition: the existing V3 stars and cabin lighting look better on headset without a comfort/performance regression.

Fallback: revert HDR/bloom and continue with emissive materials, shader halos, and tone-map tricks.

### M9 - Planet + Destination Engine

- Add one hero world: `Jovian Dawn`.
- Build a procedural gas-giant view with banded clouds, storm, terminator, atmospheric limb, rings, and slow destination switching.
- Composite the planet in front of the existing V3 starfield.

Pass condition: the view is genuinely more awe-inspiring on Quest, not just more complex.

### M10 - More Worlds + Per-Scene Sound

- Add `The Ringed Giant`, `Aurora World`, and `Deep Quiet`.
- Add per-world audio layers on top of the M5 ambient bed.
- Cross-fade view, palette, mixed runtime light, and sound together.

Pass condition: each destination feels emotionally distinct and remains calm.

### M11 - Living Sky

- Extend the shooting-star system into a weighted event pool.
- Add comfort-capped distant ships, comets, asteroids, moon transits, meteor showers, and aurora ripples.
- Keep events slow, distant, sparse, and never head-triggered.

Pass condition: the sky feels alive without demanding attention or inducing vection.

### M12 - Awe Pass

- Add `Binary Eclipse`.
- Add `Nebula Drift`.
- Add a very rare `Leviathan` event only if the core planet/sky experience already works without it.
- Run a final comfort and performance sweep on Quest.

Pass condition: the full scene set still respects the comfort baseline and feels original, not derivative.

## Guardrails

- Comfort first: no artificial locomotion, no visible forced translation, no sudden large motion, no head-triggered sky events.
- Visual motion must stay vast, slow, and distant.
- Quest frame budget is authoritative. On-device testing decides.
- Geometry and shaders stay procedural/code-reviewable unless a later asset decision is explicit.
- Original sci-fi only: no franchise planets, ships, UI, sound effects, logos, or motifs.
- README must be updated with each implementation milestone so implemented status and roadmap status stay current.
