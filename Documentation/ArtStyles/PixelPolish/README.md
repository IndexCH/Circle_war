# Pixel presentation refinement

Scene: `Assets/Scenes/SpringPixelPreview.unity`.

## Ground

The circular terrain and its thin decorative overlay use `CircleGroundPixelDensity.mat`, with `_ReferencePixelSize = 1.3`. This represents a 1.3-screen-pixel sampling interval at the existing 1080p / orthographic-half-height-5 camera, or approximately 83.076923 samples per world unit. The world grid remains stable when sprites rotate or change scale. Background and scenery props keep their previous 54 samples per world unit. Ring size, child mask compensation, collisions, and map paths are unchanged.

Fractional 1.3-pixel cells necessarily occupy alternating physical screen pixels; 1.3 is the sampling interval, not a fractional hardware pixel.

## Text

The previous body font was an 8-dot glyph enlarged to 16 pixels. Its reduced Chinese strokes made words hard to distinguish. Body text now uses native 16-dot GNU Unifont at 16 pixels, with a 20-pixel line advance. Pixel edges use HintedRaster and Point sampling. The existing larger Fusion faces remain for 24px headings and 30/36px feedback messages, which were already legible. The existing HUD panel shapes and placement are retained.

Font: GNU Unifont 17.0.04, official unmodified OpenType release.

Source: https://www.unifoundry.com/unifont/index.html

Download: https://unifoundry.com/pub/unifont/unifont-17.0.04/font-builds/unifont-17.0.04.otf

License: SIL OFL 1.1 (also offered under GPL 2+ with the font embedding exception). Official notices are included in `Assets/ArtStyles/Fonts/Unifont/`.

## Monsters

GroundEnemy and FlyingRobotEnemy apply a shared pixel material to their existing body portraits and fallback guns. This includes definitions across seasons and boss portraits, without redrawing or modifying the original raster assets. The shader uses source texel centers, crisp alpha edges, and the same 1.3 reference sampling interval as the ground. It preserves sprite tint, flip, sorting, world size, attack behavior, and hitboxes. Hit flashes still use the existing FEEL sprite tint.

This is a rendering change built from the project's installed URP sprite shader. No image-generation output or new monster artwork is required. F6 restores original enemy materials as well as the existing scene style; enemies spawned while pixel mode is off also switch correctly when it is enabled again.

Validation screenshots and measurements are captured from the actual Unity Game View; temporary probe enemies are not saved into the scene.

## Completed validation

- 1920×1080 Game View: circular terrain and overlay both use the 1.3 reference material; measured sampling interval is 1.3px. Ring dimensions are identical before/after style switching.
- All 708 Chinese characters in first-party code/game data have glyphs. All 178 authored dialogue body and choice strings fit their existing boxes with the new font.
- All 17 EnemyDefinition assets and the BossDefinition use the pixel material while retaining the original sprite reference. All 18 restore their original material; re-enabling pixel mode also updates an enemy spawned while it was off (19/19).
- The corrected point sampler was visually compared against the original portraits. Four displayed monster types retain their hit tint, and an independent death effect is created on death.
- Final Unity Console: 0 errors, 0 warnings. `git diff --check` passes. Original SampleScene SHA256 remains `54D0919C43AE33177E037238E7FEC4214BA75B9AE49DA5E96FC597DAA52A9169`.
- No standalone player build, commit, or push was requested.
