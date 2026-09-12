#ifndef CIRCLE_WAR_SCENE_PIXEL_GRID
#define CIRCLE_WAR_SCENE_PIXEL_GRID

// All environment sprites sample the same world grid. Their texture size, PPU,
// transform scale and rotation therefore cannot change the displayed pixel size.
float2 ScenePixelUV(float2 uv, float2 worldPosition, float pixelsPerUnit)
{
    float density = max(pixelsPerUnit, 1.0);
    float2 worldDx = ddx(worldPosition);
    float2 worldDy = ddy(worldPosition);
    float2 uvDx = ddx(uv);
    float2 uvDy = ddy(uv);
    float determinant = worldDx.x * worldDy.y - worldDx.y * worldDy.x;
    if (abs(determinant) < 1e-12) return uv;

    float2 cellCenter = (floor(worldPosition * density) + 0.5) / density;
    float2 delta = cellCenter - worldPosition;
    float2 screenOffset = float2(
        delta.x * worldDy.y - delta.y * worldDy.x,
        worldDx.x * delta.y - worldDx.y * delta.x) / determinant;
    return uv + uvDx * screenOffset.x + uvDy * screenOffset.y;
}
#endif
