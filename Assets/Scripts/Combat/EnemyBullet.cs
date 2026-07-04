using UnityEngine;

namespace CircleWar
{
    public sealed class EnemyBullet : MonoBehaviour
    {
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float lifetime = 4f;
        [Min(0)]
        [SerializeField] private int damage = 1;
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.12f;
        [Min(0.01f)]
        [SerializeField] private float playerHitRadius = 0.35f;
        [SerializeField] private GameHud gameHud;

        private CircleMapView circleMapView;
        private Vector2 worldPosition;
        private Vector2 velocity = Vector2.down;
        private float age;

        public Vector2 WorldPosition => worldPosition;
        public Vector2 Velocity => velocity;

        public void Launch(Vector2 newDirection, float newSpeed, float newLifetime)
        {
            Launch(newDirection, newSpeed, newLifetime, damage);
        }

        public void Launch(Vector2 newDirection, float newSpeed, float newLifetime, int newDamage)
        {
            CircleMapView activeMapView = CircleMapView.Active;
            Vector2 startWorldPosition = activeMapView != null
                ? activeMapView.ViewToWorldPosition(transform.position)
                : (Vector2)transform.position;
            Vector2 worldDirection = activeMapView != null
                ? activeMapView.ViewToWorldDirection(newDirection)
                : newDirection;
            Launch(activeMapView, startWorldPosition, worldDirection, newSpeed, newLifetime, newDamage);
        }

        public void Launch(CircleMapView newCircleMapView, Vector2 newWorldPosition, Vector2 newDirection, float newSpeed, float newLifetime)
        {
            Launch(newCircleMapView, newWorldPosition, newDirection, newSpeed, newLifetime, damage);
        }

        public void Launch(CircleMapView newCircleMapView, Vector2 newWorldPosition, Vector2 newDirection, float newSpeed, float newLifetime, int newDamage)
        {
            speed = Mathf.Max(0f, newSpeed);
            lifetime = Mathf.Max(0.01f, newLifetime);
            damage = Mathf.Max(0, newDamage);
            Vector2 direction = newDirection.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : newDirection.normalized;
            circleMapView = newCircleMapView != null ? newCircleMapView : CircleMapView.Active;
            worldPosition = newWorldPosition;
            velocity = direction * speed;
            age = 0f;
            ApplyViewTransform();
        }

        private void Update()
        {
            Vector2 previousWorldPosition = worldPosition;
            worldPosition += velocity * Time.deltaTime;

            if (TryHitPlayer(previousWorldPosition, worldPosition))
            {
                Destroy(gameObject);
                return;
            }

            ApplyViewTransform();
            age += Time.deltaTime;

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private bool TryHitPlayer(Vector2 previousWorldPosition, Vector2 currentWorldPosition)
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            if (resolvedMapView == null)
            {
                return false;
            }

            float combinedRadius = hitRadius + playerHitRadius;
            if (!CombatHitMath.SegmentIntersectsCircle(previousWorldPosition, currentWorldPosition, resolvedMapView.PlayerWorldPosition, combinedRadius))
            {
                return false;
            }

            CombatDamage.TryApplyPlayerDamage(ResolveGameHud(), damage);
            return true;
        }

        private void ApplyViewTransform()
        {
            CircleMapView resolvedMapView = ResolveCircleMapView();
            Vector2 viewPosition = resolvedMapView != null ? resolvedMapView.WorldToViewPosition(worldPosition) : worldPosition;
            transform.position = new Vector3(viewPosition.x, viewPosition.y, transform.position.z);

            Vector2 viewDirection = resolvedMapView != null ? resolvedMapView.WorldToViewDirection(velocity) : velocity;
            if (viewDirection.sqrMagnitude > Mathf.Epsilon)
            {
                transform.right = viewDirection.normalized;
            }
        }

        private CircleMapView ResolveCircleMapView()
        {
            if (circleMapView == null)
            {
                circleMapView = CircleMapView.Active;
            }

            return circleMapView;
        }

        private GameHud ResolveGameHud()
        {
            if (gameHud == null)
            {
                gameHud = FindAnyObjectByType<GameHud>();
            }

            return gameHud;
        }
    }
}
