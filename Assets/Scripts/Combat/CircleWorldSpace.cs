using UnityEngine;

namespace CircleWar
{
    public readonly struct CircleWorldSpace
    {
        public const float SixClockAngleDegrees = -90f;

        private readonly Vector2 center;
        private readonly float playerAngleDegrees;
        private readonly float playerRadius;

        public CircleWorldSpace(Vector2 center, float playerAngleDegrees, float playerRadius)
        {
            this.center = center;
            this.playerAngleDegrees = playerAngleDegrees;
            this.playerRadius = Mathf.Max(0f, playerRadius);
        }

        public Vector2 Center => center;
        public float PlayerAngleDegrees => playerAngleDegrees;
        public float PlayerRadius => playerRadius;
        public float ViewAngleDegrees => GetViewAngleDegrees(playerAngleDegrees);
        public Vector2 PlayerWorldPosition => center + DirectionFromAngleDegrees(playerAngleDegrees) * playerRadius;
        public Vector2 PlayerViewPosition => WorldToViewPosition(PlayerWorldPosition);

        public Vector2 WorldToViewPosition(Vector2 worldPosition)
        {
            return center + Rotate(worldPosition - center, ViewAngleDegrees);
        }

        public Vector2 ViewToWorldPosition(Vector2 viewPosition)
        {
            return center + Rotate(viewPosition - center, -ViewAngleDegrees);
        }

        public Vector2 WorldToViewDirection(Vector2 worldDirection)
        {
            return Rotate(worldDirection, ViewAngleDegrees);
        }

        public Vector2 ViewToWorldDirection(Vector2 viewDirection)
        {
            return Rotate(viewDirection, -ViewAngleDegrees);
        }

        public static float GetViewAngleDegrees(float playerAngleDegrees)
        {
            return SixClockAngleDegrees - playerAngleDegrees;
        }

        public static Vector2 DirectionFromAngleDegrees(float angleDegrees)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        }

        public static Vector2 Rotate(Vector2 vector, float angleDegrees)
        {
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(angleRadians);
            float sine = Mathf.Sin(angleRadians);

            return new Vector2(
                vector.x * cosine - vector.y * sine,
                vector.x * sine + vector.y * cosine);
        }
    }
}
