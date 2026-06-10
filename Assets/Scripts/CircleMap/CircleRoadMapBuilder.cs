using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public class CircleRoadMapBuilder
    {
        public List<CircleRoadSegmentData> BuildRoadSegmentList(int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory)
        {
            List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();

            for (int index = 0; index < totalRoadSegmentCount; index++)
            {
                roadSegmentList.Add(CreateRoadSegmentByIndex(index, totalRoadSegmentCount, spriteFactory));
            }

            return roadSegmentList;
        }

        private CircleRoadSegmentData CreateRoadSegmentByIndex(int index, int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory)
        {
            if (index <= 6)
            {
                return new CircleRoadSegmentData("树林道路 " + index, spriteFactory.GetSegmentSprite("plant_blue_berry_grass"), new Color(0.36f, 0.62f, 0.34f, 1f));
            }

            if (index == totalRoadSegmentCount - 1)
            {
                return new CircleRoadSegmentData("年度危机 / 进入大地图", spriteFactory.GetSegmentSprite("wall_ruin_corner_ore"), new Color(1f, 0.82f, 0.28f, 1f));
            }

            int patternIndex = index % 6;

            if (patternIndex == 0)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("采集资源点 " + index, spriteFactory.GetSegmentSprite("ore_crystal_cluster"), new Color(0.78f, 0.68f, 0.3f, 1f));
                segment.contentType = SegmentContentType.Resource;
                return segment;
            }

            if (patternIndex == 1)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("沿路探索 " + index, spriteFactory.GetSegmentSprite("plant_alien_succulent"), new Color(0.48f, 0.64f, 0.75f, 1f));
                segment.contentType = SegmentContentType.Npc;
                return segment;
            }

            if (patternIndex == 2)
            {
                return new CircleRoadSegmentData("触发事件 " + index, spriteFactory.GetSegmentSprite("plant_spiky_agave"), new Color(0.74f, 0.54f, 0.32f, 1f));
            }

            if (patternIndex == 3)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("遭遇敌人 " + index, spriteFactory.GetSegmentSprite("wall_salt_alkali_stone"), new Color(0.75f, 0.3f, 0.26f, 1f));
                segment.contentType = SegmentContentType.Monster;
                return segment;
            }

            if (patternIndex == 4)
            {
                return new CircleRoadSegmentData("建设设施 " + index, spriteFactory.GetSegmentSprite("wall_brick_ore_chunk"), new Color(0.52f, 0.54f, 0.46f, 1f));
            }

            return new CircleRoadSegmentData("推进关系 " + index, spriteFactory.GetSegmentSprite("plant_twisted_vine"), new Color(0.55f, 0.45f, 0.72f, 1f));
        }
    }
}
