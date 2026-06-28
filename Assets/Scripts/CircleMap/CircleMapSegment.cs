using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapSegment : MonoBehaviour
    {
        private SpriteRenderer segmentSpriteRenderer;
        private SpriteRenderer interactionPromptRenderer;
        private Sprite npcInteractionPromptSprite;
        private Sprite eventInteractionPromptSprite;
        private Sprite resourceInteractionPromptSprite;
        private Vector2 interactionPromptOffset;

        public void Setup(
            SpriteRenderer renderer,
            SpriteRenderer promptRenderer,
            Sprite npcPromptSprite,
            Sprite eventPromptSprite,
            Sprite resourcePromptSprite,
            Vector2 promptOffset)
        {
            segmentSpriteRenderer = renderer;
            interactionPromptRenderer = promptRenderer;
            npcInteractionPromptSprite = npcPromptSprite;
            eventInteractionPromptSprite = eventPromptSprite;
            resourceInteractionPromptSprite = resourcePromptSprite;
            interactionPromptOffset = promptOffset;

            if (interactionPromptRenderer != null)
            {
                interactionPromptRenderer.enabled = false;
            }
        }

        public void Show(CircleRoadSegmentData segment)
        {
            Sprite sprite = segment != null ? segment.sprite : null;
            segmentSpriteRenderer.enabled = sprite != null;
            segmentSpriteRenderer.sprite = sprite;
            AlignSpriteBottomCenter();
            RefreshInteractionPrompt(segment);
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

        private void RefreshInteractionPrompt(CircleRoadSegmentData segment)
        {
            if (interactionPromptRenderer == null)
            {
                return;
            }

            Sprite promptSprite = segment != null ? GetInteractionPromptSprite(segment.contentType) : null;
            interactionPromptRenderer.enabled = promptSprite != null;
            interactionPromptRenderer.sprite = promptSprite;

            if (promptSprite == null)
            {
                return;
            }

            AlignInteractionPromptAboveSegment();
        }

        private Sprite GetInteractionPromptSprite(SegmentContentType contentType)
        {
            switch (contentType)
            {
                case SegmentContentType.Npc:
                    return npcInteractionPromptSprite;
                case SegmentContentType.Event:
                    return eventInteractionPromptSprite;
                case SegmentContentType.Resource:
                    return resourceInteractionPromptSprite;
                default:
                    return null;
            }
        }

        private void AlignInteractionPromptAboveSegment()
        {
            if (interactionPromptRenderer.sprite == null)
            {
                return;
            }

            float segmentTopY = 0f;
            if (segmentSpriteRenderer != null && segmentSpriteRenderer.enabled && segmentSpriteRenderer.sprite != null)
            {
                Bounds segmentBounds = segmentSpriteRenderer.sprite.bounds;
                Vector3 segmentScale = segmentSpriteRenderer.transform.localScale;
                segmentTopY = segmentSpriteRenderer.transform.localPosition.y + segmentBounds.max.y * segmentScale.y;
            }

            Bounds promptBounds = interactionPromptRenderer.sprite.bounds;
            Vector3 promptScale = interactionPromptRenderer.transform.localScale;
            Vector3 promptBottomCenter = new Vector3(
                promptBounds.center.x * promptScale.x,
                promptBounds.min.y * promptScale.y,
                0f);
            Vector3 desiredBottomCenter = new Vector3(
                interactionPromptOffset.x,
                segmentTopY + interactionPromptOffset.y,
                0f);

            interactionPromptRenderer.transform.localPosition = desiredBottomCenter - promptBottomCenter;
        }
    }
}
