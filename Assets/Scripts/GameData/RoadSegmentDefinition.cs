#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "RoadSegmentDefinition", menuName = "Circle War/Definitions/Road Segment")]
    public sealed class RoadSegmentDefinition : GameDefinition
    {
        [Min(0)]
        [SerializeField] private int roadIndex;
        [SerializeField] private SeasonDefinition season;
        [SerializeField] private RegionDefinition region;
        [SerializeField] private SegmentContentType contentType = SegmentContentType.None;
        [SerializeField] private Sprite mapSprite;
        [Tooltip("MapSprite 沿道路段本地 Y 轴的偏移。正值朝圆心，负值朝圆外。")]
        [SerializeField] private float y;
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
        public Sprite MapSprite => mapSprite;
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
    }
}

#pragma warning restore 0649
