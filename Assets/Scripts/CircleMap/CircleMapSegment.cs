using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapSegment : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer segmentSpriteRenderer;
        [SerializeField] private int roadSegmentIndex = -1;
        [SerializeField] private string roadSegmentName = "未设置";

        private void Awake()
        {
            if (segmentSpriteRenderer == null)
            {
                segmentSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void Setup(SpriteRenderer newSegmentSpriteRenderer, Vector2 unusedSize)
        {
            segmentSpriteRenderer = newSegmentSpriteRenderer;
        }

        public void ShowEmptyLand(Sprite emptySprite, string emptyName)
        {
            ShowRoadData(-1, emptyName, emptySprite);
        }

        public void ShowRoadData(int newRoadSegmentIndex, string newRoadSegmentName, Sprite iconSprite)
        {
            roadSegmentIndex = newRoadSegmentIndex;
            roadSegmentName = newRoadSegmentName;
            segmentSpriteRenderer.sprite = iconSprite;
            MoveSpriteRootToBottomCenter();
        }

        public void ShowRoadData(int newRoadSegmentIndex, string newRoadSegmentName, Sprite iconSprite, Color unusedColor, bool unusedSelected)
        {
            ShowRoadData(newRoadSegmentIndex, newRoadSegmentName, iconSprite);
        }

        private void MoveSpriteRootToBottomCenter()
        {
            if (segmentSpriteRenderer == null || segmentSpriteRenderer.sprite == null || segmentSpriteRenderer.transform == transform)
            {
                return;
            }

            Bounds spriteBounds = segmentSpriteRenderer.sprite.bounds;
            Vector3 bottomCenter = new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0f);
            Vector3 spriteScale = segmentSpriteRenderer.transform.localScale;
            segmentSpriteRenderer.transform.localPosition = new Vector3(-bottomCenter.x * spriteScale.x, -bottomCenter.y * spriteScale.y, 0f);
        }
    }
}
