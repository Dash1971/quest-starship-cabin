# Roadmap - Crew Quarters V2: The View

This roadmap continues from the tested Quarters V2 Milestone 9 hotfix 1 build.

Current tested build:

- Stable rollback tag: `quarters-v2-m7-tested-20260711` points to the pre-M8 tested M7 build.
- Tested on: Meta Quest 3
- Installed/launched: 2026-07-12 13:41:24 JST
- Unity: 6000.5.2f1
- Package: `jp.openclaw.starshipcabin`
- APK SHA-256: `2c27de443a27f7114ba94d6507d1ddde0082a8a86b453046b2f19420b75931af`

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

Milestones 1-9 are shipped and tested:

1. Shell + glazing: procedural Crew Quarters V2 shell, 55 degree glazed hull slope, four rounded-trapezoid window panes, shader starfield.
2. Furniture + palette: couch, bed, raised alcove, desk, console strips, plants, soft-bright material set.
3. Seat anchors: couch, bed-sit, bed-recline, and desk anchors with fade transitions.
4. URP + baked lighting: cove-lit baked GI, one mixed runtime light, legacy cleanup.
5. Audio V2 + media wall + star motion: layered ambient bed, brown noise, local video wall, lateral star motion, light fixes.
6. Decor pass: procedural chess set and library decor.
7. Book labels + starfield V3: fitted book labels and dark-sky star shader upgrade.
8. Clear the deck + HDR trial: retire the media/video wall, remove `MediaScreenController`, enable HDR + bloom, add fixed foveated rendering, and verify on Quest.
9. The planet: add `Jovian Dawn`, a procedural banded gas giant with storm, dawn terminator, atmospheric limb, and ring; reduce HDR-busy twinkle/lamp/reflection; keep destination switching for M10. Hotfix 1 restores the M7 starfield render path and moves the star surface far beyond the planet after headset testing showed the first M9 render-queue attempt made the four panes appear as opaque color blocks.

The M5 media/video wall was retired because Quest system overlays can provide media apps without distracting from star-gazing.

## Active Milestones

### M10 - More Worlds + Per-Scene Sound

- Add `The Ringed Giant`, `Aurora World`, and `Deep Quiet`.
- Add per-world audio layers on top of the M5 ambient bed.
- Add the destination-switching framework now that one hero world exists.
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
