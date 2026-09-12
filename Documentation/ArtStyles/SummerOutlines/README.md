# 夏季物品描边与平视设施

用户要求：物品描边参照春季，浮标和净水装置稍作修改为平视；地面 1 像素、物品 2 像素维持原值。

## 已接入素材

- `Assets/ArtStyles/SummerPixel/summer-navigation-buoy-eye-level.png`
- `Assets/ArtStyles/SummerPixel/summer-water-purifier-eye-level.png`

使用内置 image_gen 编辑两张参考图，完整提示词见 `prompts.json`。保留旧 PNG 作为回退素材；SummerPixelArtStyle 三条源图映射已指向新图。原来的颜色映射表与地面接触素材继续使用。
沿用用户已授权的脚本清理透明背景、裁掉空白边缘，脚本未修改任何 RGB 通道；检查记录见 `cleanup-report.json` 和 `check_alpha.py`。

## 描边及摆放

夏季地图物品使用与春季深棕灰轮廓相近的颜色，统一为一格物品世界像素网格的内描边；缩放与旋转不会改变描边的世界宽度。原图内部细节保留，描边不扩张轮廓、不改变 alpha。默认关闭，只在夏季替换后的地图物品开启。春季参考素材的渲染行为、天空、地面、接触覆盖和角色保持原有处理。
地面材质密度 108（1080p/正交尺寸5时为1屏幕像素），物品材质密度54（同视图为2屏幕像素），未更改材质密度数值。
重新校准节点 05、19、32、33 中两张新图的底部高度，其他摆放参数和玩法字段未改动。

## 验证

[Log] PASS: 80 spring/summer nodes; 576 sprite draws and 126 contact draws at scales 0.2/0.4/0.7 and rotations 0/37; common density 54; 10 live scene renderers: ground stays 108 (1 px), props/sky stay 54 (2 px); texture binding, summer-only prop outlines and original-style reset valid.
[Log] PASS: 40 summer nodes, 536 roots/feet across 8 circle angles; max radial error 0.000019; current pixel textures, ground masks, style toggling, empty slots and spring mask reset.
- 新图 alpha 清理后可见 RGB 与生成结果逐像素一致。
- Unity ShaderUtil.ShaderHasError(ScenePixelDensity) 为 false；最终 Console 0 Error、0 Warning。
- `front-views-0.png`：两张新素材在实际夏季地面上的渲染。
- `preview-0.png`：设施与植被的描边检查。
- `game-summer.png`：恢复正常相机与 UI 后的游戏画面。
- 未执行独立打包。检查用临时对象已清理，退出 Play 模式。

入口场景：`Assets/Scenes/SummerPixelPreview.unity`。

