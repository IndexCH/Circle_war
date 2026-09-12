# 箱底与盐碱地的圆弧过渡

三种春季像素箱子共用透明盐壳、碎石和积土纹理。过渡层沿真实环形地面弯曲，一侧堆高，遮住靠近地面的箱角。

- `SpringPixelArtStyle.SpritePair.groundContact` 仍是可选的过渡 Sprite，只配置三种箱子。
- `CircleMapView.GroundSurfaceRadius` 使用地图摆放物件时的半径与父物体缩放。`CrateGroundContact` 根据圆心和这个半径生成 32 段圆弧（66 个顶点、64 个三角形），将世界坐标转为箱体局部坐标；随圆圈旋转后仍贴合原圆弧。
- 过渡宽度为箱宽的 1.22 倍。低边沿固定半径埋入地面，高边在更深陷入地面的箱角附近局部隆起；两角深度近似时选择左角。堆高量依据箱高、摆放偏移和旋转计算。
- `CircleMapSegment` 为每个箱体图层复用一个 `CrateGroundContact` 子对象与网格，不增加碰撞和逐帧更新。网格随组件销毁。
- 使用 `CrateGroundContact.mat`，沿用 ScenePixelDensity 的 54 世界像素密度、URP 2D 光照与原纹理颜色。Shader 新增 `_UseMeshGeometry`：箱底网格启用，其余 Sprite 材质默认关闭，保留原有翻转、动画和颜色处理。
- 过渡排序在箱体之上，后续地图物件、NPC 和交互提示保持原顺序。空节点、非箱子、其他季节、原图模式隐藏过渡；主图层被 NPC 动画替换时也隐藏。

本次只改变几何形状和对应渲染方式，未修改箱体 PNG、颜色还原表或盐壳纹理。`crate-salt-ground-contact-v1.png` 是上一轮 image_gen 生成的素材，1628 × 164，Point、无压缩、无 mipmap、sRGB。此前依照用户授权仅清理 alpha <= 8 的噪点并裁切空白边缘，可见 RGB 保留。原提示词见 `prompt.md`。

## 验证

Unity 6000.4.4f1，SpringPixelPreview 场景实机运行检查通过：

- 9 个春季引用节点，实际 8 个箱子（1 个箱体图层被 NPC 动画替换）。
- 低边顶点保持等半径圆弧，单侧隆起、UV 有效、排序正确、无碰撞。
- 额外旋转 71° 后，顶点到圆心的距离保持不变。
- 重复 Show 复用同一个网格；原图/像素切换、空节点、其他季节正确隐藏或恢复。
- Shader 编译无错误，运行 Console 0 Error / 0 Warning。
- 三种箱子放大检查及正常游戏视角检查完成。临时展示对象和定义已移除，相机、UI、人物和时间缩放已恢复。

`three-crate-curved-contact.png` 是三种箱子在真实地面上的临时放大展示，`game-curved-crate-contact.png` 是正常游戏画面。旧版直条截图保留供比较。
