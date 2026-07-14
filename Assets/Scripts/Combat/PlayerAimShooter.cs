using System;
using UnityEngine;

namespace CircleWar
{
    public sealed class PlayerAimShooter : MonoBehaviour
    {
        private const string DefaultHandName = "Hand";
        private const string DefaultShootPointName = "ShootPoint";
        private const int BulletTextureSize = 16;

        [Header("References")]
        [SerializeField] private Transform hand;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private SpriteRenderer bodyRenderer;

        [SerializeField] private SpriteRenderer leftHand;
        [SerializeField] private GameHud gameHud;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private CircleMapView circleMapView;

        [Header("Facing")]
        [SerializeField] private bool mirrorHandPositionWhenFacingLeft = true;
        [SerializeField] private bool mirrorHandVisualWhenFacingLeft = true;
        [Min(0f)]
        [SerializeField] private float turnDeadZone = 0.01f;

        [Header("Ammo")]
        [SerializeField] private string industryResourceId = "industry";
        [Min(0)]
        [SerializeField] private int industryCostPerShot = 1;

        [Header("Projectile")]
        [Min(0.01f)]
        [SerializeField] private float fireCooldown = 0.15f;
        [Min(0.1f)]
        [SerializeField] private float bulletSpeed = 14f;
        [Min(0.1f)]
        [SerializeField] private float bulletLifetime = 2f;
        [Min(1)]
        [SerializeField] private int bulletDamage = 1;
        [Min(0.01f)]
        [SerializeField] private float bulletWorldSize = 0.12f;
        [Min(0.01f)]
        [SerializeField] private float bulletHitRadius = 0.12f;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private Color bulletColor = new Color(1f, 0.84f, 0.22f, 1f);
        [SerializeField] private int bulletSortingOrder = 35;

        private Sprite runtimeBulletSprite;
        private Vector3 initialHandLocalPosition;

        private Vector3 initialLeftHandPosition;
        private Vector3 initialHandLocalScale;

        private Vector3 initialLeftHandLocalScale;
        private bool initialBodyFlipX;
        private bool cachedInitialPose;
        private bool isFacingRight = true;
        private float nextAllowedFireTime;

        private void Awake()
        {
            ResolveReferences();
            CacheInitialPose();
        }

        private void Update()
        {
            ResolveReferences();
            CacheInitialPose();
            AimHandAtMouse();

            if (Input.GetMouseButtonDown(0))
            {
                TryShoot();
            }
        }

        private void ResolveReferences()
        {
            if (hand == null)
            {
                hand = FindChildByName(transform, DefaultHandName);
            }

            if (shootPoint == null && hand != null)
            {
                shootPoint = FindChildByName(hand, DefaultShootPointName);
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (gameHud == null)
            {
                gameHud = FindAnyObjectByType<GameHud>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (circleMapView == null)
            {
                circleMapView = CircleMapView.Active != null ? CircleMapView.Active : FindAnyObjectByType<CircleMapView>();
            }
        }

        private void AimHandAtMouse()
        {
            if (hand == null || aimCamera == null)
            {
                return;
            }

            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            Vector2 aimDirection = mouseWorldPosition - hand.position;
            if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            RefreshFacing(aimDirection);
            aimDirection = mouseWorldPosition - hand.position;
            if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            hand.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void CacheInitialPose()
        {
            if (cachedInitialPose || hand == null)
            {
                return;
            }

            initialHandLocalPosition = hand.localPosition;
            initialLeftHandPosition = leftHand.transform.localPosition;
            initialHandLocalScale = hand.localScale;
            initialLeftHandLocalScale = leftHand.transform.localScale;
            initialBodyFlipX = bodyRenderer != null && bodyRenderer.flipX;
            cachedInitialPose = true;
        }

        private void RefreshFacing(Vector2 aimDirection)
        {
            if (!cachedInitialPose)
            {
                return;
            }

            if (aimDirection.x > turnDeadZone)
            {
                isFacingRight = true;
            }
            else if (aimDirection.x < -turnDeadZone)
            {
                isFacingRight = false;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = isFacingRight ? initialBodyFlipX : !initialBodyFlipX;
              //  leftHand.flipX = isFacingRight ? initialBodyFlipX : !initialBodyFlipX;
            }

            if (mirrorHandPositionWhenFacingLeft)
            {
                Vector3 handPosition = initialHandLocalPosition;
                handPosition.x = isFacingRight ? initialHandLocalPosition.x : -initialHandLocalPosition.x;
                hand.localPosition = handPosition;

                // Vector3 LeftHandPosition = initialLeftHandPosition;
                // LeftHandPosition.x = isFacingRight ? initialLeftHandPosition.x : -initialLeftHandPosition.x;
                // leftHand.transform.localPosition = LeftHandPosition;

                
                
            }

            if (mirrorHandVisualWhenFacingLeft)
            {
                Vector3 handScale = initialHandLocalScale;
                handScale.y = isFacingRight ? initialHandLocalScale.y : -initialHandLocalScale.y;
                hand.localScale = handScale;

                // Vector3 lefthandScale = initialLeftHandLocalScale;
                // lefthandScale.y = isFacingRight ? initialLeftHandLocalScale.y : -initialLeftHandLocalScale.y;
                // leftHand.transform.localScale = lefthandScale;
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextAllowedFireTime || shootPoint == null)
            {
                return;
            }

            Vector2 fireViewDirection = shootPoint.right;
            if (fireViewDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (!TrySpendIndustry())
            {
                return;
            }

            Vector2 shootViewPosition = shootPoint.position;

            SpawnBullet(shootViewPosition, fireViewDirection.normalized);
            nextAllowedFireTime = Time.time + fireCooldown;
        }

        private bool TrySpendIndustry()
        {
            if (industryCostPerShot <= 0)
            {
                return true;
            }

            if (gameHud == null || string.IsNullOrWhiteSpace(industryResourceId))
            {
                return false;
            }

            GameRuntimeData runtimeData = gameHud.RuntimeData;
            GameState state = runtimeData.State;
            int currentIndustry = state.GetResourceAmount(industryResourceId);
            if (currentIndustry < industryCostPerShot)
            {
                return false;
            }

            state.SetResourceAmount(industryResourceId, currentIndustry - industryCostPerShot);
            runtimeData.RefreshHudFromState();
            return true;
        }

        private void SpawnBullet(Vector2 viewPosition, Vector2 viewDirection)
        {
            GameObject bulletObject = new GameObject("Player Bullet");
            bulletObject.layer = gameObject.layer;
            bulletObject.transform.position = new Vector3(viewPosition.x, viewPosition.y, transform.position.z);
            bulletObject.transform.right = viewDirection.normalized;
            bulletObject.transform.localScale = new Vector3(bulletWorldSize, bulletWorldSize, 1f);

            SpriteRenderer renderer = bulletObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRuntimeBulletSprite();
            renderer.color = bulletColor;
            renderer.sortingOrder = bulletSortingOrder;

            PlayerBullet bullet = bulletObject.AddComponent<PlayerBullet>();
            bullet.Launch(ResolveCircleMapView(), viewPosition, viewDirection, bulletSpeed, bulletLifetime, bulletDamage, bulletHitRadius);
        }

        private Sprite GetRuntimeBulletSprite()
        {
            if (bulletSprite != null)
            {
                return bulletSprite;
            }

            if (runtimeBulletSprite != null)
            {
                return runtimeBulletSprite;
            }

            Texture2D texture = new Texture2D(BulletTextureSize, BulletTextureSize, TextureFormat.RGBA32, false);
            texture.name = "Runtime Player Bullet Texture";
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2((BulletTextureSize - 1) * 0.5f, (BulletTextureSize - 1) * 0.5f);
            float radius = BulletTextureSize * 0.42f;
            for (int y = 0; y < BulletTextureSize; y++)
            {
                for (int x = 0; x < BulletTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            runtimeBulletSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, BulletTextureSize, BulletTextureSize),
                new Vector2(0.5f, 0.5f),
                BulletTextureSize);
            runtimeBulletSprite.name = "Runtime Player Bullet";
            return runtimeBulletSprite;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPosition = Input.mousePosition;
            mouseScreenPosition.z = Mathf.Abs(aimCamera.transform.position.z - transform.position.z);
            Vector3 worldPosition = aimCamera.ScreenToWorldPoint(mouseScreenPosition);
            worldPosition.z = transform.position.z;
            return worldPosition;
        }

        private CircleMapView ResolveCircleMapView()
        {
            if (circleMapView == null)
            {
                circleMapView = CircleMapView.Active != null ? CircleMapView.Active : FindAnyObjectByType<CircleMapView>();
            }

            return circleMapView;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                Transform result = FindChildByName(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
