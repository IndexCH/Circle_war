using UnityEngine;

namespace CircleWar
{
    /*
     * 这是一个“枚举（enum）”，用来表示一段路上出现的内容是哪一种。
     *
     * 为什么用枚举，而不用三个 bool（hasMonster / hasNpc / hasResource）？
     * 因为题目要求“怪物、NPC、资源点只会出现一个”。
     * 如果用三个 bool，就可能不小心出现“怪物和 NPC 同时为 true”的错误状态。
     * 用枚举，一个格子的内容“同一时间只能是其中一个值”，天然不会出错。
     *
     * 枚举的每一项就是一个名字，读起来像普通英文单词，新手也能看懂。
     */
    public enum SegmentContentType
    {
        None,      // 空：这一段什么都没有。
        Monster,   // 怪物：踩到这一段会遇到怪物。
        Npc,       // NPC：踩到这一段会遇到一个可以对话的人物。
        Resource   // 资源点：踩到这一段可以采集资源。
    }

    /*
     * 这个脚本只描述“一段路的数据”，不负责任何显示。
     *
     * 为什么要单独拆出来？
     * 这是“数据和表现分离”的入门例子：
     *   - 数据：这一段叫什么名字、用哪张图片、是什么颜色、上面有什么内容。
     *   - 表现：怎么把它画到屏幕上（那是 CircleMapSegment 的事）。
     * 把两件事分开，新手就能先看懂“游戏里有哪些段”，
     * 再单独去看“它们是怎么被画出来的”。
     *
     * 注意：这是一个普通的 C# 类，不是 MonoBehaviour，
     * 所以它不能挂在 GameObject 上，只是用来存放数据。
     */
    public class CircleRoadSegmentData
    {
        // 这一段在课堂上显示的名字，例如“树林道路 3”。
        public string segmentName;

        // 这一段使用的图标图片。
        public Sprite iconSprite;

        // 这一段的颜色，用来让不同类型的格子看起来不一样。
        public Color segmentColor;

        /*
         * 这一段上出现的内容：怪物、NPC、资源点，或者什么都没有。
         * 默认是 None（空），需要时再单独赋值，例如：
         *   roadSegment.contentType = SegmentContentType.Monster;
         * 因为它是枚举，所以永远只会是其中一个值，符合“只会出现一个”的要求。
         */
        public SegmentContentType contentType = SegmentContentType.None;

        // 构造方法：创建这段数据时，一次性把名字、图片、颜色填好。
        // contentType 不在这里填，默认就是 None；谁需要谁再去赋值，读起来更清楚。
        public CircleRoadSegmentData(string newSegmentName, Sprite newIconSprite, Color newSegmentColor)
        {
            segmentName = newSegmentName;
            iconSprite = newIconSprite;
            segmentColor = newSegmentColor;
        }
    }
}
