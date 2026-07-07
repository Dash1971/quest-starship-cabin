# Roadmap

## Baseline

- Keep `Atmosphere V3` as the stable comfort baseline.
- Preserve seated/static comfort: no artificial locomotion in the default experience.
- Keep procedural audio and geometry until the art direction is worth replacing with real assets.

## Near Term

- Done: implement proper XR controller input for ambience cycling.
- Done: add one tested control first, using A/B/X/Y to cycle Drift / Orbit / Nebula.
- Done: add small diegetic console-strip feedback plus haptic pulse for mode changes.
- Replace the failed large text panel with a small, tidy diegetic console surface.
- Add a visible but unobtrusive session timer after controls work.
- Add adjustable audio volume presets.
- Add brightness presets: dim, normal, bright.

## Atmosphere

- Done: make the forward starfield visible with transparent observation glass and window star dots.
- Improve cabin architecture with cleaner wall modules and less box-like geometry.
- Add a better observation window frame.
- Add more starfield depth and optional slow nebula mode.
- Add optional sleep-safe brown noise layer.
- Add rain-on-hull or distant-space-weather ambience.

## Comfort And Performance

- Verify stable framerate on Quest 3.
- Add a comfort QA checklist for every APK build.
- Avoid flicker, forced motion, artificial walking, and sudden audio events.
- Add simple build/version labels for APK testing without cluttering the scene.

## Tooling

- Add a documented build script for macOS.
- Add a release checklist.
- Add optional APK artifact publishing through GitHub Releases, not the source tree.
- Document tested Unity and OpenXR package versions.

## Later

- Add hand/controller ray interaction.
- Add a seated recentering flow.
- Add passthrough portal experiment.
- Add personal object slots such as a book, chessboard, or quiet desk.
- Explore lightweight custom 3D assets once the procedural prototype stabilizes.
