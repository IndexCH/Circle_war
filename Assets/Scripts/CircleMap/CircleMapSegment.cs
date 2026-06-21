using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapSegment : MonoBehaviour
    {
        private SpriteRenderer segmentSpriteRenderer;

        //Todo 添加NPC

        public void Setup(SpriteRenderer renderer)
        {
            segmentSpriteRenderer = renderer;
        }

        public void Show(Sprite sprite)
        {
            segmentSpriteRenderer.enabled = sprite != null;
            segmentSpriteRenderer.sprite = sprite;
            AlignSpriteBottomCenter();
        }

        private void AlignSpriteBottomCenter()
        {
            if (segmentSpriteRenderer.sprite == null || segmentSpriteRenderer.transform == transform)
            {
                return;
            }

            Bounds bounds = segmentSpriteRenderer.sprite.bounds;
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, 0f);
            Vector3 scale = segmentSpriteRenderer.transform.localScale;
            segmentSpriteRenderer.transform.localPosition = new Vector3(
                -bottomCenter.x * scale.x,
                -bottomCenter.y * scale.y,
                0f);
        }
    }
}
