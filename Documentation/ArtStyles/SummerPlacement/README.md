# 夏季物体摆放检查

场景：`Assets/Scenes/SummerPixelPreview.unity`。

检查全部 40 个道路节点。36 个节点有美术内容，共 67 个图层（63 个环境图层、4 个 NPC 图层），另外 4 个为空节点。

## 修正内容

- 按显示图片的可见根部／脚底校准 Offset，横向偏移考虑圆弧高度。图片采样仅用于离线检查，不改变 PNG、导入设置或颜色。
- 夏季图片围绕落脚点倾斜，避免先贴底再绕图片中心旋转导致移位。动画 NPC 在首帧实际显示后重新对齐脚底。
- 植物倾斜角随所在位置的圆弧调整，保留小幅自然倾斜。错开第 10、26、31、32、37、38、39 号节点中遮住主体或挤在一起的植物／箱子。
- 夏季像素环境物体使用现有圆形背景遮罩，使地面外侧的根部和碎片隐藏；切换原图、春季或空节点时重置遮罩。没有遮罩的场景不启用该行为。
- 浮标、两种净水器来源与逃生舱复用苔藓弧形衔接。箱子的弧形遮角保留。

## 修改范围

- `Assets/Scripts/CircleMap/CircleMapSegment.cs`：夏季旋转贴底、NPC 初始帧对齐和遮罩切换。
- `Assets/Scripts/CircleMap/CircleMapView.cs`：提供当前背景遮罩是否可用的只读状态。
- `Assets/Resources/GameData/RoadSegments/SummerLowHumidityShore*.asset`：36 个有美术的节点，仅调整图层位置、缩放和角度。
- `Assets/Resources/ArtStyles/SummerPixelArtStyle.asset`：新增 4 个苔藓衔接引用。

40 个节点的 ID、名称、描述、季节、区域、内容类型、NPC／对话／事件／敌人／Boss／设施引用、奖励和成本与本轮修改前一致。SummerPixelPreview、SpringPixelPreview、SampleScene 与春季美术配置的文件哈希保持不变。

## 验证资料

- `before-0.png` 至 `before-3.png`：修改前全部 40 个节点。
- `final-0.png` 至 `final-3.png`：最终全部 40 个节点。
- `game-summer-grounded.png`：实际游戏截图。
- `ValidateSummerGrounding.cs.txt`：各物体在圆周八个方位的实际根部／脚底位置、贴图、遮罩及复用检查。
- `CaptureSummerNodes.cs.txt`：临时节点对照拍摄，结束自动还原相机、UI、玩家和游戏时间。
- `validation.txt`：最终运行验证结果。

监察官 41 张动画帧的可见脚底行完全一致（alpha ≥ 180 时 y=578），没有帧间脚底抖动。

未进行独立平台构建。本轮没有新增常驻测试对象，也没有提交或推送 Git。
