using UnityEngine;

namespace CircleWar.UI
{
    /*
     * 这个脚本只负责“圆圈上的一个可见段位”。
     * 它不知道完整道路有 30 段，也不知道玩家怎么移动；
     * 主脚本把某一段道路数据交给它，它就把图片、颜色和选中状态显示出来。
     */
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CircleMapSegment : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField, Tooltip("真正显示段位图片的 SpriteRenderer。")]
        private SpriteRenderer segmentSpriteRenderer;

        [Header("显示设置")]
        [SerializeField, Tooltip("普通状态下的大小。")]
        private Vector3 normalScale = Vector3.one;

        [SerializeField, Tooltip("玩家所在位置对应的段位会稍微放大。")]
        private Vector3 selectedScale = new Vector3(1.18f, 1.18f, 1f);

        [Header("运行时变量")]
        [SerializeField, Tooltip("当前显示的是第几段真实道路。-1 表示起点后方的空地。")]
        private int roadSegmentIndex = -1;

        [SerializeField, Tooltip("当前段位在课堂上展示用的名字。")]
        private string roadSegmentName = "未设置";

        private Vector2 targetWorldSize = new Vector2(0.55f, 0.55f);

        private void Awake()
        {
            if (segmentSpriteRenderer == null)
            {
                /*
                 * GetComponent 会在同一个 GameObject 上寻找组件。
                 * 这里这样写，是为了即使老师忘了在 Inspector 里拖引用，脚本也能自己找到 SpriteRenderer。
                 */
                segmentSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Setup(SpriteRenderer newSegmentSpriteRenderer, Vector2 newTargetWorldSize)
        {
            segmentSpriteRenderer = newSegmentSpriteRenderer;
            targetWorldSize = newTargetWorldSize;
            normalScale = Vector3.one;
            selectedScale = new Vector3(1.18f, 1.18f, 1f);
        }

        public void ShowEmptyLand(Sprite emptySprite, string emptyName)
        {
            roadSegmentIndex = -1;
            roadSegmentName = emptyName;
            SetSpriteAndColor(emptySprite, new Color(0.55f, 0.52f, 0.42f, 0.55f));
            SetSelected(false);
        }

        public void ShowRoadData(int newRoadSegmentIndex, string newRoadSegmentName, Sprite iconSprite, Color segmentColor, bool isPlayerSlot)
        {
            roadSegmentIndex = newRoadSegmentIndex;
            roadSegmentName = newRoadSegmentName;
            SetSpriteAndColor(iconSprite, segmentColor);
            SetSelected(isPlayerSlot);
        }

        private void SetSpriteAndColor(Sprite newSprite, Color newColor)
        {
            if (segmentSpriteRenderer == null)
            {
                return;
            }

            segmentSpriteRenderer.sprite = newSprite;
            segmentSpriteRenderer.color = newColor;
            ResizeSpriteToTargetSize();
        }

        private void SetSelected(bool isSelected)
        {
            if (isSelected)
            {
                transform.localScale = selectedScale;
            }
            else
            {
                transform.localScale = normalScale;
            }
        }

        private void ResizeSpriteToTargetSize()
        {
            if (segmentSpriteRenderer == null || segmentSpriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 spriteSize = segmentSpriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float scaleX = targetWorldSize.x / spriteSize.x;
            float scaleY = targetWorldSize.y / spriteSize.y;
            normalScale = new Vector3(scaleX, scaleY, 1f);
            selectedScale = new Vector3(scaleX * 1.18f, scaleY * 1.18f, 1f);
        }
    }
}
