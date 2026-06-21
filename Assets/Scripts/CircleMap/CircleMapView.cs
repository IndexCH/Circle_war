using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;
        private const string RoadSegmentDefinitionResourceFolder = "GameData/RoadSegments";

        [SerializeField] private SpriteRenderer backgroundRenderer, circleRingRenderer;
        [SerializeField] private SpriteMask backgroundCircleMask;

        [SerializeField] private int totalRoadSegmentCount = 20;
        [SerializeField] private int visibleSegmentCount = 8;
        [SerializeField] private float segmentInsetFromRing = 0.22f;
        [SerializeField] private float moveSpeed = 1.5f;

        [SerializeField] private float segmentScale = 0.4f;
        [SerializeField] private List<RoadSegmentDefinition> roadSegmentDefinitions = new List<RoadSegmentDefinition>();

        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();
        private readonly List<RoadSegmentDefinition> loadedRoadSegmentDefinitions = new List<RoadSegmentDefinition>();

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
                int roadSegmentIndex = GetRoadIndexForVisibleSlot(slotIndex, anchorRoadIndex);
                CircleMapSegment segment = visibleSegmentList[slotIndex];

                if (roadSegmentIndex < 0)
                {
                    segment.Show(null);
                    continue;
                }

                if (roadSegmentIndex >= roadSegmentList.Count)
                {
                    segment.Show(null);
                    continue;
                }

                segment.Show(roadSegmentList[roadSegmentIndex].sprite);
            }
        }

        private int GetRoadIndexForVisibleSlot(int visibleSlotIndex, int anchorRoadIndex)
        {
            // 12 点是唯一的刷新口：D 前进时新节点从这里进入，经过 6 点后再沿左半圈回到 12 点回收。
            int halfCircleSlotCount = GetHalfCircleSlotCount();
            int playerSlotIndex = PositiveModulo(anchorRoadIndex, visibleSegmentCount);
            int slotOffsetFromPlayer = PositiveModulo(visibleSlotIndex - playerSlotIndex, visibleSegmentCount);

            if (slotOffsetFromPlayer <= halfCircleSlotCount)
            {
                return anchorRoadIndex + slotOffsetFromPlayer;
            }

            return anchorRoadIndex - (visibleSegmentCount - slotOffsetFromPlayer);
        }

        private int GetHalfCircleSlotCount()
        {
            return visibleSegmentCount / 2;
        }

        private int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        // 构建道路段落列表DataList
        private void BuildRoadSegmentList()
        {
            roadSegmentList.Clear();
            roadSegmentList.AddRange(roadMapBuilder.BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory, GetRoadSegmentDefinitions()));
        }

        private IReadOnlyList<RoadSegmentDefinition> GetRoadSegmentDefinitions()
        {
            if (roadSegmentDefinitions != null && roadSegmentDefinitions.Count > 0)
            {
                return roadSegmentDefinitions;
            }

            if (loadedRoadSegmentDefinitions.Count == 0)
            {
                loadedRoadSegmentDefinitions.AddRange(Resources.LoadAll<RoadSegmentDefinition>(RoadSegmentDefinitionResourceFolder));
            }

            return loadedRoadSegmentDefinitions;
        }

        private void BuildVisibleSegments()
        {
            visibleSegmentList.Clear();

            for (int index = 0; index < visibleSegmentCount; index++)
            {
                CircleMapSegment segment = CreateSegment(index);
                segment.transform.localPosition = GetLocalPositionOnCircle(index);
                segment.transform.localEulerAngles = new Vector3(0f, 0f, index * GetOneSegmentAngle());
                segment.transform.localScale = new Vector3(segmentScale, segmentScale, 1f);
                visibleSegmentList.Add(segment);
            }
        }


        // 创建道路段落列表(还没有解决位置和角度问题)
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

        // 黑屏遮罩
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
