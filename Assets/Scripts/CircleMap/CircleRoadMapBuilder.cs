using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public class CircleRoadMapBuilder
    {
        public List<CircleRoadSegmentData> BuildRoadSegmentList(int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory)
        {
            return BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory, null);
        }

        public List<CircleRoadSegmentData> BuildRoadSegmentList(int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory, IReadOnlyList<RoadSegmentDefinition> roadSegmentDefinitions)
        {
            List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();

            for (int index = 0; index < totalRoadSegmentCount; index++)
            {
                roadSegmentList.Add(CreateRoadSegmentByIndex(index, totalRoadSegmentCount, spriteFactory, roadSegmentDefinitions));
            }

            return roadSegmentList;
        }

        private CircleRoadSegmentData CreateRoadSegmentByIndex(int index, int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory, IReadOnlyList<RoadSegmentDefinition> roadSegmentDefinitions)
        {
            Sprite sprite = spriteFactory.GetSegmentSprite();
            RoadSegmentDefinition definition = FindRoadSegmentDefinition(index, roadSegmentDefinitions);
            if (definition != null)
            {
                // SO 节点没有配置 mapSprite 时，运行时保持 null，让显示层隐藏 SpriteRenderer。
                return new CircleRoadSegmentData(definition, sprite);
            }

            if (index <= 6)
            {
                return new CircleRoadSegmentData("树林道路 ", sprite);
            }

            if (index == totalRoadSegmentCount - 1)
            {
                return new CircleRoadSegmentData("年度危机 / 进入大地图"+ index, sprite, SegmentContentType.Boss);
            }

            int patternIndex = index % 6;

            if (patternIndex == 0)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("采集资源点 "+ index, sprite);
                segment.contentType = SegmentContentType.Resource;
                return segment;
            }

            if (patternIndex == 1)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("沿路探索 " + index, sprite);
                segment.contentType = SegmentContentType.Npc;
                return segment;
            }

            if (patternIndex == 2)
            {
                return new CircleRoadSegmentData("触发事件 " + index, sprite, SegmentContentType.Event);
            }

            if (patternIndex == 3)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("遭遇敌人 " + index, sprite);
                segment.contentType = SegmentContentType.Monster;
                return segment;
            }

            if (patternIndex == 4)
            {
                return new CircleRoadSegmentData("建设设施 " + index, sprite, SegmentContentType.Facility);
            }

            return new CircleRoadSegmentData("推进关系 " + index, sprite, SegmentContentType.Npc);
        }

        private RoadSegmentDefinition FindRoadSegmentDefinition(int roadIndex, IReadOnlyList<RoadSegmentDefinition> roadSegmentDefinitions)
        {
            if (roadSegmentDefinitions == null)
            {
                return null;
            }

            for (int index = 0; index < roadSegmentDefinitions.Count; index++)
            {
                RoadSegmentDefinition definition = roadSegmentDefinitions[index];
                if (definition != null && definition.RoadIndex == roadIndex)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
