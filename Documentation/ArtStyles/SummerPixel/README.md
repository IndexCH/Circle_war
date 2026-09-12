# 夏季像素场景

打开 `Assets/Scenes/SummerPixelPreview.unity`，点击 Play。场景在常规初始化结束后切换到夏季「低湿季沼岸」，18:00；A/D 或左右键沿路行走，F6 切换原图与像素风格。

## 内容

- 沿用已确认的像素风格、原 UI 布局和 Fusion Pixel 中文字体。UI 边框色沿用 Summer 季节配置（白色），没有重绘布局。
- 夏季地面为苔藓、藤根与灰褐土层；地面参考颗粒保持 1，景物使用 54 世界像素密度。独立场景将夏季圆环归一到原预览的 8.60 × 8.53 世界尺寸，人物、圆弧接地和天空窗口保持一致。
- 暮色背景为仰望云底的天空，只有天空，没有地面或地平线；保持纵横比并完整覆盖圆窗。
- 新增 15 张图：天空、苔藓圆环、苔藓衔接条；7 种植被；储物舱、导航浮标、净水装置、废弃入口和逃生舱 5 种设施。
- 复用 5 张已有像素素材：竖箱、宽箱、终端、断线和工业废墟。夏季全部 40 个道路节点使用独立映射；NPC 与玩家保留原有立绘和动画。
- 竖箱、宽箱与储物舱复用已确认的弧形网格逻辑，底边跟随圆形地面，一侧苔藓隆起并遮住下角。共 8 个夏季节点、9 个实例。不会把春季盐壳带入夏季。

## 资源与代码

- `Assets/ArtStyles/SummerPixel/`：全部新增 PNG、13 张 Texture3D 原色表。
- `Assets/Resources/ArtStyles/SummerPixelArtStyle.asset`：21 个 Sprite 对应关系（部分原图共用像素版本）。
- `SeasonArtPreview.cs`：仅挂在新场景的启动组件；通过已有运行时接口选择夏季、暮色时间和到达信息。
- `SpringPixelArtStyle.cs`：保留旧类型与序列化结构，按春/夏读取各自映射、颜色表和箱底纹理。
- `CircleMapView.cs`、`GameHud.cs`：夏季接入原有的等比天空填充、像素切换和 UI 像素材质。
- `ScenePixelDensity.cs`：修复切换风格/季节时读取并回写整个 MaterialPropertyBlock 导致旧 `_MainTex` 被固定的问题。静态场景图每次应用风格时明确绑定当前 Sprite 贴图；还原时释放覆盖参数，NPC 与敌人的动画帧仍由 Unity 绑定。

## 美术来源与处理

使用内置 image_gen 工具，逐项参考项目原夏季素材生成；提示词为 `prompts-terrain.json` 和 `prompts-props.json`，生成原文件路径见 `generated-files.json`。

遵循用户已有授权，脚本仅清理被画入的棋盘格背景／低透明噪点并裁切空白，保留可见 RGB；圆环保留居中画布。`clean_backgrounds.py` 和 `cleanup-report.json` 记录处理。Unity 以 Point、无压缩、无 mipmap、sRGB 导入。

与春季一致，通过逐素材的 32³ 数值颜色表在渲染时接近原素材的配色与明暗分布，不覆盖原图或对 PNG 做统一染色。统计校色不是逐像素相同；数据见 `profiles.json` 和 `build_palette_tables.py`。

## 验证

检查 40 个夏季道路节点的 63 个环境图层映射和世界尺寸，4 个 NPC 图层保持原有资源。验证 19 个春季映射、盐壳引用保持原样。

箱底验证包含弧线半径、单侧隆起、排序、额外旋转 71°、网格复用、原图/像素切换、空节点与其他季节隐藏。天空纵横比和圆窗覆盖、原白色 UI 圆环关闭均通过。

实际游戏截图为 `game-summer-pixel.png`；`three-summer-crates-closeup.png` 是临时放大展示，展示对象与相机修改均已清理恢复。完成 Unity 编译和 Play Mode 检查，未执行独立平台构建。

原 `SampleScene`、`SpringPixelPreview` 和春季美术配置文件未改写。未提交或推送 Git。
