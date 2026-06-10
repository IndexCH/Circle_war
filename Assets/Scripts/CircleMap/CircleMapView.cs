using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;

        [SerializeField] private SpriteRenderer backgroundRenderer, circleRingRenderer;
        [SerializeField] private SpriteMask backgroundCircleMask;

        [SerializeField] private int totalRoadSegmentCount = 30;
        [SerializeField] private int visibleSegmentCount = 12;
        [SerializeField] private float segmentInsetFromRing = 0.22f;

        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();

        private void Start()
        {
            BuildBlackMask();
            BuildRoadSegmentList();
            BuildVisibleSegments();
        }

        private void BuildRoadSegmentList()
        {
            roadSegmentList.Clear();
            roadSegmentList.AddRange(roadMapBuilder.BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory));
        }

        private void BuildVisibleSegments()
        {
            for (int index = 0; index < visibleSegmentCount; index++)
            {
                CircleMapSegment segment = CreateSegment(index);
                CircleRoadSegmentData roadSegment = roadSegmentList[index];
                segment.transform.localPosition = GetLocalPositionOnCircle(index);
                segment.transform.localEulerAngles = new Vector3(0f, 0f, index * 360f / visibleSegmentCount);
                segment.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
                segment.ShowRoadData(index, roadSegment.segmentName, roadSegment.iconSprite);
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
            segment.Setup(segmentRenderer, Vector2.zero);
            return segment;
        }

        private Vector3 GetLocalPositionOnCircle(int index)
        {
            float angle = CircleStartAngle + index * 360f / visibleSegmentCount;
            float radius = circleRingRenderer.bounds.size.x / 2f;
            return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f);
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
