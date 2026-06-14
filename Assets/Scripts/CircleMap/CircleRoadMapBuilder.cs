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
                return new CircleRoadSegmentData("树林道路 ");
            }

            if (index == totalRoadSegmentCount - 1)
            {
                return new CircleRoadSegmentData("年度危机 / 进入大地图"+ index);
            }

            int patternIndex = index % 6;

            if (patternIndex == 0)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("采集资源点 "+ index);
                segment.contentType = SegmentContentType.Resource;
                return segment;
            }

            if (patternIndex == 1)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("沿路探索 " + index);
                segment.contentType = SegmentContentType.Npc;
                return segment;
            }

            if (patternIndex == 2)
            {
                return new CircleRoadSegmentData("触发事件 " + index);
            }

            if (patternIndex == 3)
            {
                CircleRoadSegmentData segment = new CircleRoadSegmentData("遭遇敌人 " + index);
                segment.contentType = SegmentContentType.Monster;
                return segment;
            }

            if (patternIndex == 4)
            {
                return new CircleRoadSegmentData("建设设施 " + index);
            }

            return new CircleRoadSegmentData("推进关系 " + index);
        }
    }
}
