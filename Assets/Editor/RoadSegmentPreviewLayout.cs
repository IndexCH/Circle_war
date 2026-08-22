using System.Collections.Generic;
using UnityEngine;

namespace CircleWar.EditorTools
{
    public readonly struct RoadSegmentPreviewSlot
    {
        public RoadSegmentPreviewSlot(
            int slotIndex,
            int roadIndex,
            bool isInRange,
            bool isSelected,
            float localAngleDegrees,
            float viewAngleDegrees)
        {
            SlotIndex = slotIndex;
            RoadIndex = roadIndex;
            IsInRange = isInRange;
            IsSelected = isSelected;
            LocalAngleDegrees = localAngleDegrees;
            ViewAngleDegrees = viewAngleDegrees;
        }

        public int SlotIndex { get; }
        public int RoadIndex { get; }
        public bool IsInRange { get; }
        public bool IsSelected { get; }
        public float LocalAngleDegrees { get; }
        public float ViewAngleDegrees { get; }
    }

    public static class RoadSegmentPreviewLayout
    {
        public const float CircleStartAngle = -90f;

        public static IReadOnlyList<RoadSegmentPreviewSlot> Build(
            int selectedRoadIndex,
            int totalRoadSegmentCount,
            int visibleSegmentCount)
        {
            List<RoadSegmentPreviewSlot> slots = new List<RoadSegmentPreviewSlot>();
            if (visibleSegmentCount <= 0)
            {
                return slots;
            }

            float segmentAngle = 360f / visibleSegmentCount;
            float rootRotation = GetPreviewRootRotationDegrees(
                selectedRoadIndex,
                visibleSegmentCount);
            int playerSlotIndex = PositiveModulo(selectedRoadIndex, visibleSegmentCount);

            for (int slotIndex = 0; slotIndex < visibleSegmentCount; slotIndex++)
            {
                int roadIndex = GetRoadIndexForVisibleSlot(
                    slotIndex,
                    selectedRoadIndex,
                    playerSlotIndex,
                    visibleSegmentCount);
                float localAngle = CircleStartAngle + (slotIndex + 0.5f) * segmentAngle;
                float viewAngle = localAngle + rootRotation;
                bool isInRange = roadIndex >= 0 && roadIndex < totalRoadSegmentCount;

                slots.Add(new RoadSegmentPreviewSlot(
                    slotIndex,
                    roadIndex,
                    isInRange,
                    roadIndex == selectedRoadIndex,
                    localAngle,
                    viewAngle));
            }

            return slots;
        }

        public static float GetPreviewRootRotationDegrees(
            int selectedRoadIndex,
            int visibleSegmentCount)
        {
            if (visibleSegmentCount <= 0)
            {
                return 0f;
            }

            float segmentAngle = 360f / visibleSegmentCount;
            float previewRoadPosition = selectedRoadIndex + 0.5f;
            return -previewRoadPosition * segmentAngle;
        }

        private static int GetRoadIndexForVisibleSlot(
            int visibleSlotIndex,
            int anchorRoadIndex,
            int playerSlotIndex,
            int visibleSegmentCount)
        {
            int halfCircleSlotCount = visibleSegmentCount / 2;
            int slotOffsetFromPlayer = PositiveModulo(
                visibleSlotIndex - playerSlotIndex,
                visibleSegmentCount);

            if (slotOffsetFromPlayer <= halfCircleSlotCount)
            {
                return anchorRoadIndex + slotOffsetFromPlayer;
            }

            return anchorRoadIndex - (visibleSegmentCount - slotOffsetFromPlayer);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
