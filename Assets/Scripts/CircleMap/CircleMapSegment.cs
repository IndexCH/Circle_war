using System.Collections.Generic;
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
        private readonly List<SpriteRenderer> mapSpriteRenderers = new List<SpriteRenderer>();
        private readonly List<CrateGroundContact> groundContactRenderers = new List<CrateGroundContact>();
        private Vector3 segmentSpriteBaseLocalScale = Vector3.one;
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
            mapSpriteRenderers.Clear();
            if (segmentSpriteRenderer != null)
            {
                mapSpriteRenderers.Add(segmentSpriteRenderer);
                segmentSpriteBaseLocalScale = segmentSpriteRenderer.transform.localScale;
            }
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

            IReadOnlyList<RoadSegmentMapSpriteLayer> mapSpriteLayers =
                segment != null ? segment.mapSpriteLayers : null;
            RuntimeAnimatorController idleController = LoadNpcIdleAnimatorController(segment);
            bool primarySpriteIsAnimated = idleController != null &&
                GetNpcSpriteRenderer(segment) == segmentSpriteRenderer;
            ApplyMapSprites(mapSpriteLayers, segment != null ? segment.season : null, primarySpriteIsAnimated);

            SpriteRenderer animatedNpcRenderer = GetNpcSpriteRenderer(segment);
            Animator animatedNpcAnimator = GetNpcAnimator(segment);
            if (npcSpriteRenderer != null)
            {
                Sprite npcSprite = segment != null ? segment.npcSprite : null;
                npcSpriteRenderer.enabled = npcSprite != null;
                npcSpriteRenderer.sprite = npcSprite;
            }

            if (idleController != null && animatedNpcAnimator != null)
            {
                animatedNpcAnimator.runtimeAnimatorController = idleController;
                animatedNpcAnimator.enabled = true;
                animatedNpcAnimator.Play("Idle", 0, 0f);
                animatedNpcAnimator.Update(0f);
                animatedNpcRenderer.enabled = animatedNpcRenderer.sprite != null;

                // Summer placement is authored against the feet of the displayed frame.
                // The idle frame can have different bounds from the map's source sprite.
                if (IsSummer(segment.season) && animatedNpcRenderer == segmentSpriteRenderer &&
                    mapSpriteLayers != null && mapSpriteLayers.Count > 0)
                {
                    RoadSegmentMapSpriteLayer layer = mapSpriteLayers[0];
                    AlignSpriteBottomCenter(animatedNpcRenderer, layer.Offset.x, layer.Offset.y, layer.Z);
                }
            }

            if (npcSpriteRenderer != null)
            {
                Vector2 npcOffset = segment != null ? segment.npcSpriteOffset : Vector2.zero;
                AlignSpriteBottomCenter(npcSpriteRenderer, npcOffset.x, npcOffset.y);
                ApplySpriteLocalRotation(npcSpriteRenderer, segment != null ? segment.z : 0f);
            }

            SetInteractionPromptVisible(segment, false);
        }

        private void ApplyMapSprites(
            IReadOnlyList<RoadSegmentMapSpriteLayer> layers, SeasonDefinition season, bool primarySpriteIsAnimated)
        {
            int layerCount = layers != null ? layers.Count : 0;
            EnsureMapSpriteRendererCount(layerCount);
            int baseSortingOrder = segmentSpriteRenderer != null
                ? segmentSpriteRenderer.sortingOrder
                : 0;

            for (int index = 0; index < mapSpriteRenderers.Count; index++)
            {
                SpriteRenderer renderer = mapSpriteRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                RoadSegmentMapSpriteLayer layer = index < layerCount ? layers[index] : null;
                Sprite sourceSprite = layer != null ? layer.Sprite : null;
                Vector3 artScale = Vector3.one;
                // Some NPCs animate the primary map renderer instead of the dedicated NPC renderer.
                Sprite sprite = index == 0 && primarySpriteIsAnimated
                    ? sourceSprite
                    : SpringPixelArtStyle.Resolve(season, sourceSprite, out artScale);
                renderer.enabled = sprite != null;
                renderer.sprite = sprite;
                ScenePixelDensity.Apply(renderer, sprite != null && sprite != sourceSprite, IsSummer(season));
                // Reuse the map's circular terrain window to hide buried roots and
                // rubble outside the ground. Reset this when the pooled slot changes style.
                renderer.maskInteraction = IsSummer(season) && sprite != sourceSprite &&
                    CircleMapView.Active != null && CircleMapView.Active.HasBackgroundMask
                    ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
                // Reserve the next order for contact details over this prop's bottom edge.
                renderer.sortingOrder = baseSortingOrder + index * 2;
                renderer.transform.localScale = layer != null
                    ? Vector3.Scale(segmentSpriteBaseLocalScale, layer.Scale)
                    : segmentSpriteBaseLocalScale;
                renderer.transform.localScale = Vector3.Scale(renderer.transform.localScale, artScale);

                if (sprite == null)
                {
                    ApplyGroundContact(index, renderer, null);
                    continue;
                }

                Vector2 offset = layer.Offset;
                AlignSpriteBottomCenter(renderer, offset.x, offset.y, IsSummer(season) ? layer.Z : 0f);
                ApplySpriteLocalRotation(renderer, layer.Z);
                ApplyGroundContact(index, renderer, index == 0 && primarySpriteIsAnimated
                    ? null : SpringPixelArtStyle.GroundContact(season, sourceSprite));
            }

            if (npcSpriteRenderer != null)
            {
                npcSpriteRenderer.sortingOrder = baseSortingOrder + mapSpriteRenderers.Count * 2;
            }

            if (interactionPromptRenderer != null)
            {
                interactionPromptRenderer.sortingOrder = baseSortingOrder + mapSpriteRenderers.Count * 2 + 1;
            }
        }

        private void ApplyGroundContact(int index, SpriteRenderer owner, Sprite sprite)
        {
            CrateGroundContact contact = index < groundContactRenderers.Count ? groundContactRenderers[index] : null;
            CircleMapView map = CircleMapView.Active;
            if (sprite == null || !owner.enabled || map == null)
            {
                if (contact != null) contact.Hide();
                return;
            }

            if (contact == null)
            {
                while (groundContactRenderers.Count <= index) groundContactRenderers.Add(null);
                GameObject contactObject = new GameObject("Crate Ground Contact");
                contactObject.hideFlags = owner.gameObject.hideFlags;
                contactObject.transform.hideFlags = owner.transform.hideFlags;
                contactObject.layer = owner.gameObject.layer;
                contactObject.transform.SetParent(owner.transform, false);
                contact = contactObject.AddComponent<CrateGroundContact>();
                contact.hideFlags = owner.hideFlags;
                groundContactRenderers[index] = contact;
            }

            contact.Configure(owner, sprite, map.DiskCenter, map.GroundSurfaceRadius);
        }

        private void EnsureMapSpriteRendererCount(int spriteCount)
        {
            if (segmentSpriteRenderer == null)
            {
                return;
            }

            if (mapSpriteRenderers.Count == 0)
            {
                mapSpriteRenderers.Add(segmentSpriteRenderer);
            }

            while (mapSpriteRenderers.Count < spriteCount)
            {
                SpriteRenderer renderer = CreateAdditionalMapSpriteRenderer(mapSpriteRenderers.Count + 1);
                mapSpriteRenderers.Add(renderer);
            }
        }

        private SpriteRenderer CreateAdditionalMapSpriteRenderer(int layerNumber)
        {
            GameObject imageObject = new GameObject("Map Sprite Image " + layerNumber);
            imageObject.hideFlags = segmentSpriteRenderer.gameObject.hideFlags;
            imageObject.transform.hideFlags = segmentSpriteRenderer.transform.hideFlags;
            imageObject.transform.SetParent(transform, false);
            imageObject.transform.localScale = segmentSpriteBaseLocalScale;

            SpriteRenderer renderer = imageObject.AddComponent<SpriteRenderer>();
            renderer.hideFlags = segmentSpriteRenderer.hideFlags;
            renderer.sortingLayerID = segmentSpriteRenderer.sortingLayerID;
            renderer.sortingOrder = segmentSpriteRenderer.sortingOrder;
            renderer.enabled = false;
            return renderer;
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

        private static bool IsSummer(SeasonDefinition season) =>
            season != null && season.DefinitionId == "summer";

        private void AlignSpriteBottomCenter(
            SpriteRenderer renderer, float localXOffset, float localYOffset, float groundRotation = 0f)
        {
            if (renderer == null || renderer.sprite == null || renderer.transform == transform)
            {
                return;
            }

            Bounds bounds = renderer.sprite.bounds;
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, 0f);
            Vector3 scale = renderer.transform.localScale;
            // Rotate the anchor as well as the image, so tilting a summer prop
            // does not swing its base away from the authored ground position.
            Vector3 rotatedBottom = Quaternion.Euler(0f, 0f, groundRotation) *
                Vector3.Scale(bottomCenter, scale);
            renderer.transform.localPosition = new Vector3(localXOffset, localYOffset, 0f) - rotatedBottom;
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
