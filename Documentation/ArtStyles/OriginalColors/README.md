# 按原图校正像素场景配色

当前 `SpringPixelPreview` 的场景显示使用逐素材原色校正：盐壳回到米灰白、木材回到棕色，并保留水晶等部位的原有彩色差异。背景继续隐藏，地面参考像素尺寸保持 1。

校正覆盖 `SpringPixelArtStyle` 的 19 组替换关系，共 18 张不同的像素场景贴图。每张贴图对应一个从原始 Sprite 可见区域测量得到的 32×32×32 数值颜色表，使用线性的半精度浮点存储，并区分中性/木质颜色与绿色、蓝色、紫色部位。材质在贴图采样后、SpriteRenderer 着色和灯光之前应用校正。

这是 Unity 渲染时的颜色校正，不是重新生成或覆盖 PNG：像素版的轮廓、纹理、透明度和像素网格保持原样。项目窗口中直接查看旧 PNG 仍能看到其重绘时的颜色，游戏内通过配色表恢复接近原图的色调及明暗分布。统计匹配不等于与构图不同的原图逐像素相同。

原先地面为减少杂色而进行的 RGB 混合与色阶简化已关闭（`_DetailReduction=0`），避免额外改色。UI、怪物和刚生成的两张天空背景不使用这些校色表。

文件：

- `Assets/ArtStyles/OriginalColorLuts/`：18 个 Texture3D 数值资源，由 Unity 创建和导入。
- `SpringPixelArtStyle.cs`：保存原图、像素图、颜色表的对应关系。
- `ScenePixelDensity.cs`：复用渲染器的 MaterialPropertyBlock 接入颜色表，切回原画时关闭校色。
- `OriginalSpriteColor.hlsl`：在 Linear/Gamma 项目中正确解释颜色表，保留透明度。
- `profiles.json`：原图、修改前和校正后的颜色统计；不包含对图片文件的改写。

验证：19/19 颜色表引用有效；当前运行场景 6/6 对应渲染器已应用；原画/像素切换正确；圆环尺寸未变；背景保持隐藏；两个场景着色器无编译错误；Unity 控制台无错误和警告。最终游戏画面见 `scene-restored-colors.png`。未执行独立平台构建。
