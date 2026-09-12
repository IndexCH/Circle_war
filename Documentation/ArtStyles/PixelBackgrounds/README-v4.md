# 盐碱地阴天背景

两张均为 16:9 像素背景，使用低饱和灰蓝与灰白尘霾表现干燥阴天。

- `pixel-background-salt-biome-overcast-v4.png`：平视远景，低矮荒山位于画面下方，只有远山轮廓，没有向观察者延伸的地表。
- `pixel-background-upward-overcast-v4.png`：从地面接近垂直仰视的云底，保持近远云层的体积和透视。

Unity 导入为 Sprite Single，PPU 100、Point、Clamp、sRGB、无压缩、无 mipmap，不改变原始图片尺寸。旧版本保留，场景接入独立于本次图片修订。

使用内置 image_gen 分别编辑已有图片，完整提示词见 `prompts-v4.md`。未通过脚本重绘或调色。
