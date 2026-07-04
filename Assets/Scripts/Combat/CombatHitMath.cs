using UnityEngine;

namespace CircleWar
{
    public static class CombatHitMath
    {
        public static bool SegmentIntersectsCircle(Vector2 start, Vector2 end, Vector2 center, float radius)
        {
            float safeRadius = Mathf.Max(0f, radius);
            float distanceSquared = DistanceSquaredFromPointToSegment(center, start, end);
            return distanceSquared <= safeRadius * safeRadius;
        }

        private static float DistanceSquaredFromPointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - segmentStart).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
            Vector2 closestPoint = segmentStart + segment * t;
            return (point - closestPoint).sqrMagnitude;
        }
    }
}
