#ifndef CIRCLE_WAR_ORIGINAL_SPRITE_COLOR
#define CIRCLE_WAR_ORIGINAL_SPRITE_COLOR

TEXTURE3D(_OriginalColorLut);
SAMPLER(sampler_OriginalColorLut);

half4 RestoreOriginalSpriteColor(half4 pixel)
{
    if (_OriginalColorEnabled < 0.5) return pixel;
    float3 encoded = saturate(pixel.rgb);
    #ifndef UNITY_COLORSPACE_GAMMA
    encoded = lerp(encoded * 12.92,
        1.055 * pow(max(encoded, 0.0), 1.0 / 2.4) - 0.055,
        step(0.0031308, encoded));
    #endif
    // The numeric 32-cubed table maps the pixel drawing to its own source palette.
    // Keep alpha and the pixel grid unchanged, and apply SpriteRenderer tint later.
    float3 uvw = saturate(encoded) * (31.0 / 32.0) + (0.5 / 32.0);
    float3 restored = SAMPLE_TEXTURE3D(_OriginalColorLut, sampler_OriginalColorLut, uvw).rgb;
    #ifdef UNITY_COLORSPACE_GAMMA
    restored = lerp(restored * 12.92,
        1.055 * pow(max(restored, 0.0), 1.0 / 2.4) - 0.055,
        step(0.0031308, restored));
    #endif
    return half4(restored, pixel.a);
}

#endif
