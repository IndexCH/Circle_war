#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [System.Serializable]
    public sealed class RoadSegmentMapSpriteLayer
    {
        [SerializeField] private Sprite sprite;
        [Tooltip("相对节点底部居中的本地偏移。X 控制左右，Y 控制沿道路段本地 Y 轴偏移。")]
        [SerializeField] private Vector2 offset;
        [SerializeField] private Vector2 scale = Vector2.one;
        [Tooltip("绕道路段本地 Z 轴旋转的角度，单位为度。")]
        [SerializeField] private float z;

        public RoadSegmentMapSpriteLayer()
        {
        }

        public RoadSegmentMapSpriteLayer(Sprite sprite, Vector2 offset, Vector2 scale, float z)
        {
            this.sprite = sprite;
            this.offset = offset;
            this.scale = scale;
            this.z = z;
        }

        public Sprite Sprite => sprite;
        public Vector2 Offset => offset;
        public Vector3 Scale => new Vector3(
            scale.x == 0f ? 1f : scale.x,
            scale.y == 0f ? 1f : scale.y,
            1f);
        public float Z => z;
    }

    [CreateAssetMenu(fileName = "RoadSegmentDefinition", menuName = "Circle War/Definitions/Road Segment")]
    public sealed class RoadSegmentDefinition : GameDefinition
    {
        [Min(0)]
        [SerializeField] private int roadIndex;
        [SerializeField] private SeasonDefinition season;
        [SerializeField] private RegionDefinition region;
        [SerializeField] private SegmentContentType contentType = SegmentContentType.None;
        [Tooltip("节点上的全部 Map Sprite 图层。每层都可以单独设置 Sprite、Offset、Scale、Z Rotation。")]
        [SerializeField] private List<RoadSegmentMapSpriteLayer> mapSpriteLayers =
            new List<RoadSegmentMapSpriteLayer>();
        [HideInInspector]
        [SerializeField] private Sprite mapSprite;
        [HideInInspector]
        [Tooltip("额外叠加在同一节点上的 Map Sprite。MapSprite 会作为第一层，这里按顺序追加更多素材。")]
        [SerializeField] private List<Sprite> additionalMapSprites = new List<Sprite>();
        [Tooltip("NPC 节点可选的额外人物 Sprite。配置后 mapSprite 作为静态道具层保留，人物层单独播放 idle 动画。")]
        [SerializeField] private Sprite npcMapSprite;
        [Tooltip("NpcMapSprite 相对节点底部居中的本地偏移。X 控制左右，Y 控制沿道路段本地 Y 轴偏移。")]
        [SerializeField] private Vector2 npcMapSpriteOffset;
        [HideInInspector]
        [Tooltip("MapSprite 沿道路段本地 Y 轴的偏移。正值朝圆心，负值朝圆外。")]
        [SerializeField] private float y;
        [HideInInspector]
        [Tooltip("MapSprite 绕道路段本地 Z 轴旋转的角度，单位为度。")]
        [SerializeField] private float z;
        [SerializeField] private CharacterDefinition character;
        [SerializeField] private DialogueDefinition dialogue;
        [SerializeField] private GameEventDefinition gameEvent;
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField] private BossDefinition boss;
        [SerializeField] private FacilityModuleDefinition facilityModule;
        [SerializeField] private List<ResourceAmount> rewards = new List<ResourceAmount>();
        [SerializeField] private List<ResourceAmount> costs = new List<ResourceAmount>();

        public int RoadIndex => roadIndex;
        public SeasonDefinition Season => season;
        public RegionDefinition Region => region;
        public SegmentContentType ContentType => contentType;
        public Sprite MapSprite => MapSpriteLayers.Count > 0 ? MapSpriteLayers[0].Sprite : mapSprite;
        public IReadOnlyList<RoadSegmentMapSpriteLayer> MapSpriteLayers =>
            mapSpriteLayers != null && mapSpriteLayers.Count > 0
                ? mapSpriteLayers
                : BuildLegacyMapSpriteLayers();
        public IReadOnlyList<Sprite> AdditionalMapSprites => additionalMapSprites;
        public Sprite NpcMapSprite => npcMapSprite;
        public Vector2 NpcMapSpriteOffset => npcMapSpriteOffset;
        public float Y => y;
        public float Z => z;
        public CharacterDefinition Character => character;
        public DialogueDefinition Dialogue => dialogue;
        public GameEventDefinition GameEvent => gameEvent;
        public EnemyDefinition Enemy => enemy;
        public BossDefinition Boss => boss;
        public FacilityModuleDefinition FacilityModule => facilityModule;
        public IReadOnlyList<ResourceAmount> Rewards => rewards;
        public IReadOnlyList<ResourceAmount> Costs => costs;

        private IReadOnlyList<RoadSegmentMapSpriteLayer> BuildLegacyMapSpriteLayers()
        {
            List<RoadSegmentMapSpriteLayer> layers = new List<RoadSegmentMapSpriteLayer>();
            if (mapSprite != null)
            {
                layers.Add(new RoadSegmentMapSpriteLayer(
                    mapSprite,
                    new Vector2(0f, y),
                    Vector2.one,
                    z));
            }

            if (additionalMapSprites != null)
            {
                for (int index = 0; index < additionalMapSprites.Count; index++)
                {
                    Sprite additionalSprite = additionalMapSprites[index];
                    if (additionalSprite != null)
                    {
                        layers.Add(new RoadSegmentMapSpriteLayer(
                            additionalSprite,
                            new Vector2(0f, y),
                            Vector2.one,
                            z));
                    }
                }
            }

            return layers;
        }

        private void OnValidate()
        {
            if (mapSpriteLayers == null)
            {
                mapSpriteLayers = new List<RoadSegmentMapSpriteLayer>();
            }

            if (mapSpriteLayers.Count > 0)
            {
                return;
            }

            IReadOnlyList<RoadSegmentMapSpriteLayer> legacyLayers = BuildLegacyMapSpriteLayers();
            for (int index = 0; index < legacyLayers.Count; index++)
            {
                RoadSegmentMapSpriteLayer legacyLayer = legacyLayers[index];
                if (legacyLayer != null)
                {
                    mapSpriteLayers.Add(new RoadSegmentMapSpriteLayer(
                        legacyLayer.Sprite,
                        legacyLayer.Offset,
                        legacyLayer.Scale,
                        legacyLayer.Z));
                }
            }
        }
    }
}

#pragma warning restore 0649
