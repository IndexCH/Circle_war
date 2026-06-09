using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    /*
     * 这个脚本只负责一件事：按规则“造出整条路的数据”。
     *
     * 它做的事情很纯粹：
     *   输入：一共多少段路、以及一批可用的图片（来自 CircleSegmentSpriteFactory）。
     *   输出：一个装着 30 段 CircleRoadSegmentData 的列表。
     *
     * 它完全不碰屏幕、不创建 GameObject，只是“算出数据”。
     * 这就是“数据和表现分离”：先把关卡数据准备好，
     * 之后再由别的脚本决定怎么把它画出来。
     *
     * 注意：这是普通 C# 类，不是 MonoBehaviour。
     */
    public class CircleRoadMapBuilder
    {
        // 造出一整条路：从第 0 段到最后一段，每一段都生成一条数据。
        public List<CircleRoadSegmentData> BuildRoadSegmentList(int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory)
        {
            List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();

            for (int index = 0; index < totalRoadSegmentCount; index++)
            {
                CircleRoadSegmentData roadSegment = CreateRoadSegmentByIndex(index, totalRoadSegmentCount, spriteFactory);
                roadSegmentList.Add(roadSegment);
            }

            return roadSegmentList;
        }

        /*
         * 根据这一段是第几段（index），决定它是什么类型。
         * 这里故意用一连串简单的 if 来写，而不是用查表或更“聪明”的写法，
         * 因为这样老师可以一条一条念给学生听：“第几段是什么”。
         */
        private CircleRoadSegmentData CreateRoadSegmentByIndex(int index, int totalRoadSegmentCount, CircleSegmentSpriteFactory spriteFactory)
        {
            // 前 7 段（0~6）都是树林路，作为新手的平缓开局。
            if (index <= 6)
            {
                return new CircleRoadSegmentData("树林道路 " + index, spriteFactory.treeSegmentSprite, new Color(0.36f, 0.62f, 0.34f, 1f));
            }

            // 最后一段是“年度危机”，走过它就进入大地图。
            if (index == totalRoadSegmentCount - 1)
            {
                return new CircleRoadSegmentData("年度危机 / 进入大地图", spriteFactory.exitSegmentSprite, new Color(1f, 0.82f, 0.28f, 1f));
            }

            // 中间这些段，用“每 6 段一个循环”的方式轮流出现不同类型。
            // patternIndex 取值是 0~5，对应 6 种不同的格子。
            int patternIndex = index % 6;

            if (patternIndex == 0)
            {
                // 这一段是资源点：先造好数据，再单独标上“它的内容是资源点”。
                // 这份内容类型留给后续采集、事件触发等玩法逻辑判断。
                CircleRoadSegmentData resourceSegment = new CircleRoadSegmentData("采集资源点 " + index, spriteFactory.resourceSegmentSprite, new Color(0.78f, 0.68f, 0.3f, 1f));
                resourceSegment.contentType = SegmentContentType.Resource;
                return resourceSegment;
            }

            if (patternIndex == 1)
            {
                // 这一段安排一个 NPC，内容类型留给后续对话逻辑使用。
                CircleRoadSegmentData npcSegment = new CircleRoadSegmentData("沿路探索 " + index, spriteFactory.eventSegmentSprite, new Color(0.48f, 0.64f, 0.75f, 1f));
                npcSegment.contentType = SegmentContentType.Npc;
                return npcSegment;
            }

            if (patternIndex == 2)
            {
                return new CircleRoadSegmentData("触发事件 " + index, spriteFactory.eventSegmentSprite, new Color(0.74f, 0.54f, 0.32f, 1f));
            }

            if (patternIndex == 3)
            {
                // 这一段是怪物，内容类型留给后续战斗逻辑使用。
                CircleRoadSegmentData monsterSegment = new CircleRoadSegmentData("遭遇敌人 " + index, spriteFactory.enemySegmentSprite, new Color(0.75f, 0.3f, 0.26f, 1f));
                monsterSegment.contentType = SegmentContentType.Monster;
                return monsterSegment;
            }

            if (patternIndex == 4)
            {
                return new CircleRoadSegmentData("建设设施 " + index, spriteFactory.factorySegmentSprite, new Color(0.52f, 0.54f, 0.46f, 1f));
            }

            // 走到这里说明 patternIndex == 5。
            return new CircleRoadSegmentData("推进关系 " + index, spriteFactory.crisisSegmentSprite, new Color(0.55f, 0.45f, 0.72f, 1f));
        }
    }
}
