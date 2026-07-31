using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{

    public enum SegmentContentType
    {
        None,      // 空：这一段什么都没有。
        Monster,   // 怪物：踩到这一段会遇到怪物。
        Npc,       // NPC：踩到这一段会遇到一个可以对话的人物。
        Resource,  // 资源点：踩到这一段可以采集资源。
        Event,     // 事件：踩到这一段会触发剧情或选择。
        Facility,  // 设施：踩到这一段可以建设或修理设施。
        Boss       // 年度危机：踩到这一段会进入 Boss 或大型事件。
    }


    public class CircleRoadSegmentData
    {
        public readonly string segmentId;

        // 这一段在课堂上显示的名字，例如“树林道路 3”。
        public string segmentName;

        public readonly string description;
        public readonly Sprite sprite;
        public readonly float y;
        public readonly float z;
        public readonly int roadIndex;
        public readonly SeasonDefinition season;
        public readonly RegionDefinition region;
        public readonly CharacterDefinition character;
        public readonly DialogueDefinition dialogue;
        public readonly GameEventDefinition gameEvent;
        public readonly EnemyDefinition enemy;
        public readonly BossDefinition boss;
        public readonly FacilityModuleDefinition facilityModule;
        public readonly IReadOnlyList<ResourceAmount> rewards;
        public readonly IReadOnlyList<ResourceAmount> costs;

     
        public SegmentContentType contentType = SegmentContentType.None;

        // 兜底构造：没有 SO 配置时，Builder 明确告诉这一段是什么类型。
        public CircleRoadSegmentData(string newSegmentName, Sprite newSprite, SegmentContentType newContentType = SegmentContentType.None)
        {
            segmentId = string.Empty;
            segmentName = newSegmentName;
            description = string.Empty;
            sprite = newSprite;
            y = 0f;
            z = 0f;
            roadIndex = -1;
            rewards = new List<ResourceAmount>();
            costs = new List<ResourceAmount>();
            contentType = newContentType;
        }

        public CircleRoadSegmentData(RoadSegmentDefinition definition, Sprite fallbackSprite)
        {
            segmentId = definition.DefinitionId;
            segmentName = definition.DisplayName;
            description = definition.Description;
            sprite = definition.MapSprite;
            y = definition.Y;
            z = definition.Z;
            roadIndex = definition.RoadIndex;
            season = definition.Season;
            region = definition.Region;
            character = definition.Character;
            dialogue = definition.Dialogue;
            gameEvent = definition.GameEvent;
            enemy = definition.Enemy;
            boss = definition.Boss;
            facilityModule = definition.FacilityModule;
            rewards = definition.Rewards;
            costs = definition.Costs;
            contentType = definition.ContentType;
        }
    }
}
