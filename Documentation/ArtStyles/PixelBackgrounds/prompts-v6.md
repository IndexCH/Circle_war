# 仰视夕阳背景与盐碱地配色协调 v6

生成方式：内置 image_gen 编辑；第一张 v5 背景为编辑目标，第二张实际游戏截图仅作为环形盐碱地颜色参考。保留仰视云层构图和像素颗粒，削弱橙金饱和度，调向灰米白、灰褐色，同时保留低强度昏黄夕照。

输出：`pixel-background-upward-salt-dusk-v6.png`

Use case: lighting-weather / palette adjustment.
Asset type: pixel-art sky background for an existing Unity game, 16:9 landscape.

INPUT ROLES:
Image 1 is the ONLY EDIT TARGET: the upward-looking sunset cloud background.
Image 2 is a PALETTE REFERENCE ONLY: an actual game screenshot. Study the circular salt-crusted ground ring, specifically its off-white / gray-beige salt crust and warm gray-brown rock shadows. Ignore the screenshot UI, characters, props, circular framing and its current orange sky. Do NOT render or copy the screenshot. Output only the rectangular sky asset from Image 1.

Primary request: harmonize this sky with the salt ground colors. The user likes the current upward cloud composition and perspective; KEEP THEM. Only adjust lighting/color. The current sky is too orange/golden relative to the gray-white ground. Reduce orange and yellow saturation substantially (roughly 50–60% in feel), shift orange-brown toward warm stone-gray / muted taupe, and change the bright gold cloud edges to soft gray-ivory / dusty beige. It should feel like the same dry saline landscape, with a subdued dusty sunset still present.

Palette direction (approximate visual guides, not flat color fills): cloud shadow warm mineral gray around #827D73, diffuse cloud edge gray-ivory around #D1C6AC, exposed sky pale dusty beige around #B7AA8F. Retain varied tonal steps and believable volumes. A restrained dim warm-yellow trace in the thin cloud edges and atmospheric openings preserves the requested evening mood. Keep cloud shadows visibly darker than the ground's pale salt crust so the environment retains depth. Soften strong luminous rims; no bright orange halo. Do not turn the whole image white or wash away the cloud forms.

Strict preservation: retain the cloud silhouettes, their positions and relative sizes, overlap, empty-sky openings, fine pixel pattern scale, and 80–90 degree upward viewpoint showing overhead cloud undersides. No new clouds, horizon, mountains, ground, objects or sun disk. No circular crop. Same full wide rectangular composition as Image 1.
Crisp Terraria-like small square pixels and stepped edges, no blur or smooth photographic repaint. No pink, magenta, lavender, blue daytime sky, bright orange, saturated yellow, or green cast. One complete opaque image, same 16:9 dimensions, no text/UI/borders/watermark.
