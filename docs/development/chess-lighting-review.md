# M6.5 chess lighting and a quieter table

## Finding

GitHub main `46447d5` merged PR #10 with the intended Lit material flags. Its commit message records Unity/bake/capture/APK/install review. The user reports a substantial exterior improvement but unchanged bright patches on the chess pieces. The generated quarters scene, chess material assets and installed APK are not checked into GitHub, so the installed material state cannot be independently inspected from the repository.

The earlier fix addressed specular reflections only. It kept the pieces in the same lightmapping path as the room: flat-shaded, millimetre-sized faces, generated UV charts, 16 texels/metre and compressed lightmaps. A nominal lightmap texel at that density spans 6.25 cm; piece bases are only 2–2.4 cm wide, and many faces are much smaller. UV packing and minimum chart allocation affect actual sampling, so this comparison is a warning about scale, not a measurement of the final atlas. Baked bright patches and facet contrast are plausible alternative explanations. The code alone does not prove which artifact the user is seeing.

[Unity's lighting troubleshooting guide](https://discussions.unity.com/t/lightmapping-troubleshooting-guide/895352/10) explains lightmap UV and resolution artifacts. [URP Baked Lit](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/baked-lit-shader.html) supplies a baked diffuse shading path. Those references support the proposed approach; they do not validate this scene's rendered result.

## Proposed correction

- The two piece materials now use URP Baked Lit, with opaque state explicitly reset. This removes the specular/reflection path rather than relying on Lit keyword toggles.
- Both combined piece renderers receive diffuse light probes instead of sampling tiny compressed lightmap charts. Eighteen probes surround free space above the board; both colours share one anchor. Reflection probe use is disabled.
- Pieces still contribute to the room bake, preserving their baked contact shadows on the board. Their geometry, chess position, colour and two combined renderers remain intact.
- Only the pieces take this shading path. Their diffuse illumination is baked, so they no longer receive the small real-time starlight tint; the room and board retain their existing lighting. This tradeoff favors stable matte pieces.
- Remove the backdrop title/status sign and the obsolete console pad/indicator strips beneath it. Existing A/B/stick controls and haptics remain. No replacement view label is added to the cabin.

The unused selector component is retained for compatibility with old generated scenes; regeneration no longer instantiates it. Current-scene checks reject a generated sign or console pad.

## Review procedure

1. Check out this PR branch in Unity 6000.5.2f1 and use **Regenerate, Bake and Build Review APK**. A material-only rebuild is insufficient: the new probes need a fresh bake. Version `2.0.0-m6.5-cabin-cleanup` / `20012`, existing package and review APK filename.
2. Scene checks require the diffuse shader, probe receivers, shared anchor and no reflection probes. The post-bake/build gate requires baked probes with finite nonblack lighting and rejects pieces still assigned a lightmap index. This verifies configuration, not visual quality.
3. Run **Capture Fixed Seats** after the bake. Expect **36 PNGs**: the existing 33 vista views plus the original chess close-up and two additional +/-12 cm head-offset views. `chess-lighting.json` records actual shader, keywords, base colour, GI mode, reflection mode, anchor and lightmap index for each colour, alongside the source-stamped capture manifest.
4. Compare the original chess angle against M6.4, then inspect the offset views and the table from the couch. Confirm carved ivory/ebony readability, warm diffuse shading, contact shadows and the empty space where the sign/pad stood. Confirm A changes vistas, tap B changes life mode, hold B previews an event, and stick-click changes motion mode.
5. Review in Quest with small head movements. Patches that travel over a surface suggest a view-dependent effect; patches fixed to faces suggest baked/geometry shading. If bright patches persist in this diffuse/probe build, inspect the captured material evidence, face normals and bloom contribution before another material tweak. Do not mark this fixed from source checks alone.

Local validation: 26 unchanged timeline checks, C# syntax for 36 files, and diff checks pass. Unity API compilation, fresh bake, captures and headset appearance remain for OpenClaw. The exterior art is unchanged from the user-approved direction in M6.4.
