#ifndef CIRCLE_WAR_GROUND_PIXEL_COLOR
#define CIRCLE_WAR_GROUND_PIXEL_COLOR

half4 SampleGroundColor(float2 uv, float2 detailDx, float2 detailDy)
{
    half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, uv);
    if (_DetailReduction <= 0) return center;

    // Filter only color: retain the original salt-crust silhouette and holes.
    // Alpha-weighting excludes transparent texels from the color average.
    float3 sum = center.rgb * center.a * 2.0;
    float weight = center.a * 2.0;
    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            half4 neighbor = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp,
                uv + x * detailDx + y * detailDy);
            sum += neighbor.rgb * neighbor.a;
            weight += neighbor.a;
        }
    }
    float3 rgb = lerp(center.rgb, sum / max(weight, 0.0001), _DetailReduction);
    // Fewer nearby shades make the small mineral speckles less busy.
    float steps = max(_PaletteSteps, 2.0);
    float3 palette = floor(saturate(rgb) * steps + 0.5) / steps;
    return half4(lerp(rgb, palette, _DetailReduction), center.a);
}

#endif
