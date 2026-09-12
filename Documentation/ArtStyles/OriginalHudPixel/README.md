# 原 UI 像素化与背景缩放修复

保留原有 UI 图片、布局、位置、尺寸、橙青配色、文字、按钮状态及回调。在春季像素场景中使用 `OriginalHudPixel.mat` 对原 UI 的边框、图标、按钮和进度格做 2 屏幕像素采样；未生成或替换 UI 图片，也未更换字体或人物肖像。

- `Assets/Shaders/UI/OriginalHudPixel.shader`：原图像素采样，保留顶点颜色、透明度、Stencil、UI 裁切与 Alpha Clip。细线采用同一像素格内的原图采样保留。
- `Assets/Resources/ArtStyles/OriginalHudPixel.mat`：`Pixel Size` 控制像素颗粒，当前 2。
- `Assets/Scripts/ArtStyles/OriginalHudPixelStyle.cs`：复用一个共享材质，仅处理默认材质的 Image；人物肖像和已有特殊材质跳过。
- `GameHud.ApplySeasonTheme`：接入像素材质，按 F6 同时对比原图 UI 和像素 UI；其他季节继续原 UI。
- `HudFeelFeedback`：信息更新高亮沿用面板的材质。

背景变小的根因是更换圆环图片后的尺寸补偿也缩小了子物体 `circleRenderer`，遮罩按这个子物体的尺寸生成。现在补偿子物体的继承缩放；切换风格时重新计算遮罩，并将春季背景等比居中铺满遮罩。道路、圆环世界尺寸与玩家路径保持不变。

误解需求产生的灰白 UI、重新布局、新标签和进度条改写已从 Unity 项目撤回。场景文件和原 UI 图片未修改。

## 验证

Unity 6000.4.4f1 Editor Play，1920 × 1080。
`checks.txt` 记录 14 项检查，包括原 UI 属性保持、人物肖像、背景比例、多次切换、玩家路径、进度动画、信息屏 FEEL 高亮和原对话按钮回调。原 UI 图像处理数为 34。

`original-ui.png` / `pixel-ui.png` 使用同一个修复后的像素场景，用于只对比 UI。
`pixel-ui-dialogue.png` 展示原对话按钮与建设进度。

控制台最终无错误、无警告；未做独立 Player 构建。
