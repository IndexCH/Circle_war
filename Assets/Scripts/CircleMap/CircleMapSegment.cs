using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapSegment : MonoBehaviour
    {
        private const float InteractionPromptHeight = 3f;
        private const string NpcIdleAnimationResourceRoot = "Scence/NpcIdleAnimations";
        private const string StaticNpcCharacterId = "eli";

        private SpriteRenderer segmentSpriteRenderer;
        private Animator segmentAnimator;
        private SpriteRenderer interactionPromptRenderer;
        private Sprite npcInteractionPromptSprite;
        private Sprite eventInteractionPromptSprite;
        private Sprite resourceInteractionPromptSprite;
        private float interactionPromptHorizontalOffset;

        public void Setup(
            SpriteRenderer renderer,
            SpriteRenderer promptRenderer,
            Sprite npcPromptSprite,
            Sprite eventPromptSprite,
            Sprite resourcePromptSprite,
            float promptHorizontalOffset)
        {
            segmentSpriteRenderer = renderer;
            segmentAnimator = renderer.GetComponent<Animator>();
            if (segmentAnimator == null)
            {
                segmentAnimator = renderer.gameObject.AddComponent<Animator>();
            }

            segmentAnimator.enabled = false;
            segmentAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            interactionPromptRenderer = promptRenderer;
            npcInteractionPromptSprite = npcPromptSprite;
            eventInteractionPromptSprite = eventPromptSprite;
            resourceInteractionPromptSprite = resourcePromptSprite;
            interactionPromptHorizontalOffset = promptHorizontalOffset;

            if (interactionPromptRenderer != null)
            {
                interactionPromptRenderer.enabled = false;
            }
        }

        public void Show(CircleRoadSegmentData segment)
        {
            segmentAnimator.enabled = false;
            segmentAnimator.runtimeAnimatorController = null;

            Sprite sprite = segment != null ? segment.sprite : null;
            segmentSpriteRenderer.enabled = sprite != null;
            segmentSpriteRenderer.sprite = sprite;
            RuntimeAnimatorController idleController = LoadNpcIdleAnimatorController(segment);
            if (idleController != null)
            {
                segmentAnimator.runtimeAnimatorController = idleController;
                segmentAnimator.enabled = true;
                segmentAnimator.Play("Idle", 0, 0f);
                segmentAnimator.Update(0f);
                segmentSpriteRenderer.enabled = segmentSpriteRenderer.sprite != null;
            }

            AlignSpriteBottomCenter(segment != null ? segment.y : 0f);
            ApplySpriteLocalRotation(segment != null ? segment.z : 0f);
            SetInteractionPromptVisible(segment, false);
        }

        private static RuntimeAnimatorController LoadNpcIdleAnimatorController(
            CircleRoadSegmentData segment)
        {
            if (segment == null ||
                segment.contentType != SegmentContentType.Npc ||
                segment.character == null)
            {
                return null;
            }

            string characterId = segment.character.DefinitionId;
            if (characterId == StaticNpcCharacterId)
            {
                return null;
            }

            string resourcePath = NpcIdleAnimationResourceRoot + "/" + characterId + "/" +
                characterId + "_idle_controller";
            return Resources.Load<RuntimeAnimatorController>(resourcePath);
        }

        public void SetInteractionPromptVisible(CircleRoadSegmentData segment, bool isVisible)
        {
            if (interactionPromptRenderer == null)
            {
                return;
            }

            Sprite promptSprite = segment != null && isVisible
                ? GetInteractionPromptSprite(segment.contentType)
                : null;
            interactionPromptRenderer.enabled = promptSprite != null;
            interactionPromptRenderer.sprite = promptSprite;

            if (promptSprite != null)
            {
                SetInteractionPromptFixedHeight();
            }
        }

        private void AlignSpriteBottomCenter(float localYOffset)
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
                -bottomCenter.y * scale.y + localYOffset,
                0f);
        }

        private void ApplySpriteLocalRotation(float localZRotation)
        {
            if (segmentSpriteRenderer.transform == transform)
            {
                return;
            }

            segmentSpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, localZRotation);
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

        private void SetInteractionPromptFixedHeight()
        {
            interactionPromptRenderer.transform.localPosition = new Vector3(
                interactionPromptHorizontalOffset,
                InteractionPromptHeight,
                0f);
        }
    }
}
