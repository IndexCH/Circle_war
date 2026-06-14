using UnityEngine;

namespace CircleWar
{

    public enum SegmentContentType
    {
        None,      // 空：这一段什么都没有。
        Monster,   // 怪物：踩到这一段会遇到怪物。
        Npc,       // NPC：踩到这一段会遇到一个可以对话的人物。
        Resource   // 资源点：踩到这一段可以采集资源。
    }


    public class CircleRoadSegmentData
    {
        // 这一段在课堂上显示的名字，例如“树林道路 3”。
        public string segmentName;

     
        public SegmentContentType contentType = SegmentContentType.None;

        // 构造方法：创建这段数据时，一次性把名字、图片、颜色填好。
        // contentType 不在这里填，默认就是 None；谁需要谁再去赋值，读起来更清楚。
        public CircleRoadSegmentData(string newSegmentName)
        {
            segmentName = newSegmentName;
        }
    }
}
