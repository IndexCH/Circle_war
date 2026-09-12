# 夕阳背景接入

当前 `Assets/Scenes/SpringPixelPreview.unity` 已启用并显示 `pixel-background-upward-salt-dusk-v6.png` 仰视云底版。v6 以游戏中盐碱地的灰米白和灰褐色为参考，减弱原 v5 的橙金饱和度，同时保留昏黄夕照和仰视云层构图。

- `SpringPixelArtStyle.asset` 的春季背景映射改为 v6，该背景原有色表保持为空，使新素材显示协调后的夕阳配色；其他素材色表保留。
- 背景等比缩放并居中覆盖圆形遮罩。背景替换不再按旧图分别补偿宽度、高度，避免把 16:9 图片挤成 4:3。
- 场景背景启用，白色 UI 圆环保持隐藏；实际环形地面正常显示。

运行验证：v6 背景尺寸比例为 1.776833，与源图相同；背景世界尺寸约 14.64 × 8.24，完整覆盖 8.40 × 8.24 遮罩。背景还原色表关闭，白色 UI 圆环隐藏。此次仅更新背景 PNG、导入设置、像素背景映射及场景背景引用，无脚本修改。

实际截图均为 1920 × 1080：`game-dusk-background.png` 是平视远山 v5；`game-upward-dusk-background.png` 是仰视云底 v5；`game-upward-salt-dusk-v6.png` 是当前配色协调后的仰视云底 v6。预览结束后已退出 Play Mode。原始 `SampleScene.unity` 文件未修改。v6 使用内置 image_gen 编辑，提示词及参考图用途见 `prompts-v6.md`。
