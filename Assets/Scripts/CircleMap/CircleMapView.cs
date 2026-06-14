using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;
        private const int FrontVisibleSlotCount = 6;

        [SerializeField] private SpriteRenderer backgroundRenderer, circleRingRenderer;
        [SerializeField] private SpriteMask backgroundCircleMask;

        [SerializeField] private int totalRoadSegmentCount = 30;
        [SerializeField] private int visibleSegmentCount = 12;
        [SerializeField] private float segmentInsetFromRing = 0.22f;
        [SerializeField] private float moveSpeed = 1.5f;

        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();

        private Transform circleRotatingRoot;
        private float currentRoadPosition;
        private int lastDisplayedAnchorIndex = -1;

        private void Start()
        {
            circleRotatingRoot = circleRingRenderer.transform.parent;
            BuildBlackMask();
            BuildRoadSegmentList();
            BuildVisibleSegments();
            TryRefreshVisibleSegments();
            ApplyCircleRotation();
        }

        private void Update()
        {
            float moveInput = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                moveInput += 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                moveInput -= 1f;
            }

            if (moveInput != 0f)
            {
                float maxPosition = Mathf.Max(0f, roadSegmentList.Count - 1);
                currentRoadPosition = Mathf.Clamp(
                    currentRoadPosition + moveInput * moveSpeed * Time.deltaTime,
                    0f,
                    maxPosition);
                TryRefreshVisibleSegments();
            }

            ApplyCircleRotation();
        }

        private void ApplyCircleRotation()
        {
            float angle = -currentRoadPosition * GetOneSegmentAngle();
            circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void TryRefreshVisibleSegments()
        {
            int anchorIndex = Mathf.FloorToInt(currentRoadPosition);
            if (anchorIndex == lastDisplayedAnchorIndex)
            {
                return;
            }

            lastDisplayedAnchorIndex = anchorIndex;
            RefreshVisibleSegments(anchorIndex);
        }

        private void RefreshVisibleSegments(int anchorRoadIndex)
        {
            for (int slotIndex = 0; slotIndex < visibleSegmentList.Count; slotIndex++)
            {
                int roadSegmentIndex = anchorRoadIndex + GetRoadOffsetFromPlayer(slotIndex, anchorRoadIndex);
                CircleMapSegment segment = visibleSegmentList[slotIndex];

                if (roadSegmentIndex < 0)
                {
                    segment.Show(spriteFactory.GetSegmentSprite());
                    continue;
                }

                if (roadSegmentIndex >= roadSegmentList.Count)
                {
                    segment.Show(spriteFactory.GetSegmentSprite());
                    continue;
                }

                segment.Show(spriteFactory.GetSegmentSprite());
            }
        }

        private int GetRoadOffsetFromPlayer(int visibleSlotIndex, int anchorRoadIndex)
        {
            int playerSlotIndex = anchorRoadIndex % visibleSegmentCount;
            int offset = visibleSlotIndex - playerSlotIndex;

            if (offset < 0)
            {
                offset += visibleSegmentCount;
            }

            if (offset > FrontVisibleSlotCount)
            {
                offset -= visibleSegmentCount;
            }

            return offset;
        }

        private void BuildRoadSegmentList()
        {
            roadSegmentList.Clear();
            roadSegmentList.AddRange(roadMapBuilder.BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory));
        }

        private void BuildVisibleSegments()
        {
            visibleSegmentList.Clear();

            for (int index = 0; index < visibleSegmentCount; index++)
            {
                CircleMapSegment segment = CreateSegment(index);
                segment.transform.localPosition = GetLocalPositionOnCircle(index);
                segment.transform.localEulerAngles = new Vector3(0f, 0f, index * GetOneSegmentAngle());
                segment.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
                visibleSegmentList.Add(segment);
            }
        }

        private CircleMapSegment CreateSegment(int index)
        {
            GameObject segmentObject = new GameObject("Visible Segment " + index);
            segmentObject.transform.SetParent(circleRingRenderer.transform.parent.GetChild(0), false);

            GameObject imageObject = new GameObject("Image");
            imageObject.transform.SetParent(segmentObject.transform, false);

            SpriteRenderer segmentRenderer = imageObject.AddComponent<SpriteRenderer>();
            segmentRenderer.sortingOrder = 5;

            CircleMapSegment segment = segmentObject.AddComponent<CircleMapSegment>();
            segment.Setup(segmentRenderer);
            return segment;
        }

        private Vector3 GetLocalPositionOnCircle(int index)
        {
            float angle = CircleStartAngle + index * GetOneSegmentAngle();
            float radius = circleRingRenderer.bounds.size.x / 2f - segmentInsetFromRing;
            return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f);
        }

        private float GetOneSegmentAngle()
        {
            return 360f / visibleSegmentCount;
        }

        private void BuildBlackMask()
        {
            Bounds ringBounds = circleRingRenderer.bounds;
            Vector2 maskSize = backgroundCircleMask.sprite.bounds.size;
            backgroundCircleMask.transform.position = ringBounds.center;
            backgroundCircleMask.transform.localScale = new Vector3(ringBounds.size.x / maskSize.x, ringBounds.size.y / maskSize.y, 1f);
            backgroundRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }
}
