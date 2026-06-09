using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    /*
     * 这个脚本只负责一件事：从 Resources 里读取圆环点位用的场景物体图片。
     *
     * 这里的图片属于“表现资源”，和道路数据（CircleRoadSegmentData）是分开的：
     *   - 这个工厂只管“读取图片”和“修正图片 pivot”。
     *   - 至于哪一段路用哪张图片，是 CircleRoadMapBuilder 决定的。
     *
     * 注意：这是普通 C# 类，不是 MonoBehaviour。
     */
    public class CircleSegmentSpriteFactory
    {
        private const string GeneratedAssetsResourceFolder = "Scence/盐碱地/GeneratedAssets";
        private const string OreCrystalClusterResourcePath = GeneratedAssetsResourceFolder + "/ore_crystal_cluster";

        // 下面这些就是准备好的图片，供别的脚本直接取用。
        // 字段名直接说明它代表哪一种格子。
        public Sprite treeSegmentSprite;
        public Sprite emptySegmentSprite;
        public Sprite resourceSegmentSprite;
        public Sprite eventSegmentSprite;
        public Sprite enemySegmentSprite;
        public Sprite factorySegmentSprite;
        public Sprite crisisSegmentSprite;
        public Sprite exitSegmentSprite;

        /*
         * 我们会用 Sprite.Create 复制一份 Sprite，只改 pivot，不复制 Texture 像素。
         * 这些运行时 Sprite 需要统一清理，原始 png 资源不用销毁。
         */
        private readonly List<Sprite> createdSpriteList = new List<Sprite>();

        // 一次性把所有需要用到的道路图片都准备出来。
        public void CreateAllSegmentSprites()
        {
            Sprite[] loadedAssetSprites = LoadRandomSegmentSpritePool();

            treeSegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            emptySegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            resourceSegmentSprite = LoadOreCrystalClusterSprite();
            eventSegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            enemySegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            factorySegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            crisisSegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
            exitSegmentSprite = GetRandomLoadedSprite(loadedAssetSprites);
        }

        private Sprite LoadOreCrystalClusterSprite()
        {
            Sprite sprite = Resources.Load<Sprite>(OreCrystalClusterResourcePath);
            if (sprite != null)
            {
                return CreateBottomCenterPivotSpriteCopy(sprite);
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(OreCrystalClusterResourcePath);
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            return CreateBottomCenterPivotSpriteCopy(sprites[0]);
        }

        private Sprite[] LoadRandomSegmentSpritePool()
        {
            Sprite[] allSprites = Resources.LoadAll<Sprite>(GeneratedAssetsResourceFolder);
            if (allSprites == null || allSprites.Length == 0)
            {
                return allSprites;
            }

            List<Sprite> randomSpriteList = new List<Sprite>();
            for (int index = 0; index < allSprites.Length; index++)
            {
                Sprite sprite = allSprites[index];
                if (sprite == null || sprite.name.StartsWith("ore_crystal_cluster"))
                {
                    continue;
                }

                Sprite anchoredSprite = CreateBottomCenterPivotSpriteCopy(sprite);
                if (anchoredSprite != null)
                {
                    randomSpriteList.Add(anchoredSprite);
                }
            }

            return randomSpriteList.ToArray();
        }

        /*
         * 这些场景小物件会沿着圆环摆放，所以它们的“落脚点”应该是底部中点。
         * 原始自动切图的 pivot 有时在左下角，会让顶端和侧边的物体明显偏出去。
         */
        private Sprite CreateBottomCenterPivotSpriteCopy(Sprite sourceSprite)
        {
            if (sourceSprite == null || sourceSprite.texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                sourceSprite.texture,
                sourceSprite.rect,
                new Vector2(0.5f, 0f),
                sourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                sourceSprite.border);
            sprite.name = sourceSprite.name + " Bottom Center Pivot";

            createdSpriteList.Add(sprite);
            return sprite;
        }

        private Sprite GetRandomLoadedSprite(Sprite[] loadedAssetSprites)
        {
            if (loadedAssetSprites == null || loadedAssetSprites.Length == 0)
            {
                return null;
            }

            return loadedAssetSprites[Random.Range(0, loadedAssetSprites.Length)];
        }

        /*
         * 把这个工厂通过 Sprite.Create 创建过的 Sprite 包装对象删掉。
         * 注意：这里只删运行时 Sprite，不删原始 png 的 Texture。
         */
        public void DestroyAllSegmentSprites()
        {
            for (int index = 0; index < createdSpriteList.Count; index++)
            {
                Sprite sprite = createdSpriteList[index];
                if (sprite == null)
                {
                    continue;
                }

                /*
                 * Destroy 适合游戏运行时使用。
                 * DestroyImmediate 适合编辑器模式下立即清理，比如老师在 Inspector 里手动调用 Build。
                 */
                if (Application.isPlaying)
                {
                    Object.Destroy(sprite);
                }
                else
                {
                    Object.DestroyImmediate(sprite);
                }
            }

            createdSpriteList.Clear();
        }
    }
}
