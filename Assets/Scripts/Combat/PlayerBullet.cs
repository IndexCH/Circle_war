using UnityEngine;

namespace CircleWar
{
    public sealed class PlayerBullet : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;
        [SerializeField] private float lifetime = 2f;
        [Min(1)]
        [SerializeField] private int damage = 1;
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.12f;

        private CircleMapView circleMapView;
        private Vector2 viewPosition;
        private Vector2 viewVelocity = Vector2.right;
        private float age;

        public Vector2 ViewPosition => viewPosition;
        public Vector2 Velocity => viewVelocity;

        public void Launch(Vector2 newDirection, float newSpeed, float newLifetime)
        {
            Launch(newDirection, newSpeed, newLifetime, damage, hitRadius);
        }

        public void Launch(Vector2 newDirection, float newSpeed, float newLifetime, int newDamage, float newHitRadius)
        {
            Launch(CircleMapView.Active, transform.position, newDirection, newSpeed, newLifetime, newDamage, newHitRadius);
        }

        public void Launch(CircleMapView newCircleMapView, Vector2 newViewPosition, Vector2 newDirection, float newSpeed, float newLifetime)
        {
            Launch(newCircleMapView, newViewPosition, newDirection, newSpeed, newLifetime, damage, hitRadius);
        }

        public void Launch(CircleMapView newCircleMapView, Vector2 newViewPosition, Vector2 newDirection, float newSpeed, float newLifetime, int newDamage, float newHitRadius)
        {
            speed = Mathf.Max(0f, newSpeed);
            lifetime = Mathf.Max(0.01f, newLifetime);
            damage = Mathf.Max(1, newDamage);
            hitRadius = Mathf.Max(0.01f, newHitRadius);
            Vector2 direction = newDirection.sqrMagnitude <= Mathf.Epsilon ? Vector2.right : newDirection.normalized;
            circleMapView = newCircleMapView != null ? newCircleMapView : CircleMapView.Active;
            viewPosition = newViewPosition;
            viewVelocity = direction * speed;
            age = 0f;
            ApplyViewTransform();
        }

        private void Update()
        {
            Vector2 previousViewPosition = viewPosition;
            viewPosition += viewVelocity * Time.deltaTime;

            if (CombatEnemyRegistry.TryHitEnemyInView(ResolveCircleMapView(), previousViewPosition, viewPosition, hitRadius, damage))
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

        private void ApplyViewTransform()
        {
            transform.position = new Vector3(viewPosition.x, viewPosition.y, transform.position.z);

            if (viewVelocity.sqrMagnitude > Mathf.Epsilon)
            {
                transform.right = viewVelocity.normalized;
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
    }
}
