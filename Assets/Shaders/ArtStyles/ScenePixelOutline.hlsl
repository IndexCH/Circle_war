#ifndef CIRCLE_WAR_SCENE_PIXEL_OUTLINE
#define CIRCLE_WAR_SCENE_PIXEL_OUTLINE

half SceneSpriteAlpha(float2 uv)
{
    // Clamp import mode must not extend the opaque edge when sampling past the sprite.
    half inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside;
}

half4 SampleSceneSprite(float2 uv, float2 worldPosition)
{
    float density = max(_ReferencePixelSize > 0 ? 108.0 / _ReferencePixelSize : _PixelsPerUnit, 1.0);
    float2 cellUV = ScenePixelUV(uv, worldPosition, density);
    half4 color = RestoreOriginalSpriteColor(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, cellUV));
    if (_SpringOutlineEnabled < 0.5) return color;

    // One existing world-grid cell inward. Never expand geometry or change alpha,
    // placement, terrain masking, palette lookup, or the ground/prop pixel density.
    float2 worldDx = ddx(worldPosition), worldDy = ddy(worldPosition);
    float2 uvDx = ddx(uv), uvDy = ddy(uv);
    float determinant = worldDx.x * worldDy.y - worldDx.y * worldDy.x;
    if (abs(determinant) < 1e-12) return color;
    float2 stepX = (uvDx * worldDy.y - uvDy * worldDx.y) / (determinant * density);
    float2 stepY = (-uvDx * worldDy.x + uvDy * worldDx.x) / (determinant * density);
    half neighbor = min(min(SceneSpriteAlpha(cellUV + stepX), SceneSpriteAlpha(cellUV - stepX)),
                        min(SceneSpriteAlpha(cellUV + stepY), SceneSpriteAlpha(cellUV - stepY)));
    if (color.a >= 0.5 && neighbor < 0.5) color.rgb = _SpringOutlineColor.rgb;
    return color;
}
#endif
