using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapSegment : MonoBehaviour
    {
        private const float InteractionPromptHeight = 3f;
        private const string NpcIdleAnimationResourceRoot = "Scence/NpcIdleAnimations";
        private const string StaticNpcCharacterId = "eli";

        private SpriteRenderer segmentSpriteRenderer;
        private SpriteRenderer npcSpriteRenderer;
        private Animator segmentAnimator;
        private Animator npcAnimator;
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
            Setup(
                renderer,
                null,
                promptRenderer,
                npcPromptSprite,
                eventPromptSprite,
                resourcePromptSprite,
                promptHorizontalOffset);
        }

        public void Setup(
            SpriteRenderer renderer,
            SpriteRenderer npcRenderer,
            SpriteRenderer promptRenderer,
            Sprite npcPromptSprite,
            Sprite eventPromptSprite,
            Sprite resourcePromptSprite,
            float promptHorizontalOffset)
        {
            segmentSpriteRenderer = renderer;
            npcSpriteRenderer = npcRenderer;
            segmentAnimator = GetOrCreateAnimator(segmentSpriteRenderer);
            npcAnimator = npcSpriteRenderer != null ? GetOrCreateAnimator(npcSpriteRenderer) : null;

            DisableAnimator(segmentAnimator);
            DisableAnimator(npcAnimator);
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
            DisableAnimator(segmentAnimator);
            DisableAnimator(npcAnimator);

            Sprite sprite = segment != null ? segment.sprite : null;
            segmentSpriteRenderer.enabled = sprite != null;
            segmentSpriteRenderer.sprite = sprite;

            SpriteRenderer animatedNpcRenderer = GetNpcSpriteRenderer(segment);
            Animator animatedNpcAnimator = GetNpcAnimator(segment);
            if (npcSpriteRenderer != null)
            {
                Sprite npcSprite = segment != null ? segment.npcSprite : null;
                npcSpriteRenderer.enabled = npcSprite != null;
                npcSpriteRenderer.sprite = npcSprite;
            }

            RuntimeAnimatorController idleController = LoadNpcIdleAnimatorController(segment);
            if (idleController != null && animatedNpcAnimator != null)
            {
                animatedNpcAnimator.runtimeAnimatorController = idleController;
                animatedNpcAnimator.enabled = true;
                animatedNpcAnimator.Play("Idle", 0, 0f);
                animatedNpcAnimator.Update(0f);
                animatedNpcRenderer.enabled = animatedNpcRenderer.sprite != null;
            }

            AlignSpriteBottomCenter(segmentSpriteRenderer, 0f, segment != null ? segment.y : 0f);
            ApplySpriteLocalRotation(segmentSpriteRenderer, segment != null ? segment.z : 0f);

            if (npcSpriteRenderer != null)
            {
                Vector2 npcOffset = segment != null ? segment.npcSpriteOffset : Vector2.zero;
                AlignSpriteBottomCenter(npcSpriteRenderer, npcOffset.x, npcOffset.y);
                ApplySpriteLocalRotation(npcSpriteRenderer, segment != null ? segment.z : 0f);
            }

            SetInteractionPromptVisible(segment, false);
        }

        private static RuntimeAnimatorController LoadNpcIdleAnimatorController(
            CircleRoadSegmentData segment)
        {
            if (segment == null || segment.character == null)
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

        private static Animator GetOrCreateAnimator(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            Animator animator = renderer.GetComponent<Animator>();
            if (animator == null)
            {
                animator = renderer.gameObject.AddComponent<Animator>();
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return animator;
        }

        private static void DisableAnimator(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = false;
            animator.runtimeAnimatorController = null;
        }

        private SpriteRenderer GetNpcSpriteRenderer(CircleRoadSegmentData segment)
        {
            return segment != null && segment.npcSprite != null && npcSpriteRenderer != null
                ? npcSpriteRenderer
                : segmentSpriteRenderer;
        }

        private Animator GetNpcAnimator(CircleRoadSegmentData segment)
        {
            return segment != null && segment.npcSprite != null && npcAnimator != null
                ? npcAnimator
                : segmentAnimator;
        }

        private void AlignSpriteBottomCenter(SpriteRenderer renderer, float localXOffset, float localYOffset)
        {
            if (renderer == null || renderer.sprite == null || renderer.transform == transform)
            {
                return;
            }

            Bounds bounds = renderer.sprite.bounds;
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, 0f);
            Vector3 scale = renderer.transform.localScale;
            renderer.transform.localPosition = new Vector3(
                -bottomCenter.x * scale.x + localXOffset,
                -bottomCenter.y * scale.y + localYOffset,
                0f);
        }

        private void ApplySpriteLocalRotation(SpriteRenderer renderer, float localZRotation)
        {
            if (renderer == null || renderer.transform == transform)
            {
                return;
            }

            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, localZRotation);
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
