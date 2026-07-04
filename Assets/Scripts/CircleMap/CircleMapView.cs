using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;
        private const string RoadSegmentDefinitionResourceFolder = "GameData/RoadSegments";
        private const string DialogueInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_dialogue";
        private const string EventInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_investigate";
        private const string ResourceInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_collect";

        [SerializeField] private SpriteRenderer backgroundRenderer, circleRingRenderer;
        [SerializeField] private SpriteMask backgroundCircleMask;

        [SerializeField] private int totalRoadSegmentCount = 20;
        [SerializeField] private int visibleSegmentCount = 8;
        [SerializeField] private float segmentInsetFromRing = 0.22f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [SerializeField] private float segmentScale = 0.4f;
        [SerializeField] private Vector2 interactionPromptOffset = new Vector2(0f, 0.12f);
        [SerializeField] private float interactionPromptScale = 1f;
        [SerializeField] private Sprite npcInteractionPromptSprite;
        [SerializeField] private Sprite eventInteractionPromptSprite;
        [SerializeField] private Sprite resourceInteractionPromptSprite;
        [SerializeField] private GameHud gameHud;
        [SerializeField] private List<RoadSegmentDefinition> roadSegmentDefinitions = new List<RoadSegmentDefinition>();

        [Header("Combat")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform enemyRuntimeRoot;
        [SerializeField] private Vector2 flyingRobotSpawnPosition = new Vector2(-6.2f, 4f);
        [SerializeField] private Vector2 flyingRobotOrbitCenter = new Vector2(0f, 1.55f);
        [SerializeField] private float groundRangedSpawnViewAngle = 82f;
        [SerializeField] private float groundMeleeSpawnViewAngle = 105f;
        [SerializeField] private float groundEnemyRadiusOffset = -0.18f;
        [SerializeField] private float meleeEnemySpeedMultiplier = 1.08f;

        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();
        private readonly List<RoadSegmentDefinition> loadedRoadSegmentDefinitions = new List<RoadSegmentDefinition>();
        private readonly HashSet<int> spawnedEnemyRoadIndices = new HashSet<int>();

        private Transform circleRotatingRoot;
        private float currentRoadPosition;
        private float playerRadius;
        private bool hasPlayerRadius;
        private int lastDisplayedAnchorIndex = -1;

        public static CircleMapView Active { get; private set; }
        public Vector2 DiskCenter => GetDiskCenter();
        public float PlayerAngleDegrees => GetPlayerAngleDegrees();
        public float PlayerRadius => GetPlayerRadius();
        public float RoadSegmentAngleDegrees => GetOneSegmentAngle();
        public CircleWorldSpace CurrentWorldSpace => new CircleWorldSpace(DiskCenter, PlayerAngleDegrees, PlayerRadius);
        public Vector2 PlayerWorldPosition => CurrentWorldSpace.PlayerWorldPosition;

        private void Awake()
        {
            Active = this;
        }

        private void Start()
        {
            circleRotatingRoot = circleRingRenderer != null ? circleRingRenderer.transform.parent : null;
            ResolveGameHud();
            BuildBlackMask();
            BuildRoadSegmentList();
            ResolveInteractionPromptSprites();
            BuildVisibleSegments();
            TryRefreshVisibleSegments();
            ApplyCircleRotation();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            bool wantsMoveForward = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool wantsMoveBackward = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool isForwardBlocked = wantsMoveForward && GroundEnemy.IsAnyMeleeBlockingForward(this);

            float moveInput = 0f;
            if (wantsMoveForward && !isForwardBlocked)
            {
                moveInput += 1f;
            }

            if (wantsMoveBackward)
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

            if (Input.GetKeyDown(interactKey))
            {
                TryInteractWithCurrentRoadSegment();
            }

            ApplyCircleRotation();
        }

        private void ApplyCircleRotation()
        {
            if (circleRotatingRoot == null)
            {
                return;
            }

            float angle = CurrentWorldSpace.ViewAngleDegrees;
            circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        public Vector2 WorldToViewPosition(Vector2 worldPosition)
        {
            return CurrentWorldSpace.WorldToViewPosition(worldPosition);
        }

        public Vector2 ViewToWorldPosition(Vector2 viewPosition)
        {
            return CurrentWorldSpace.ViewToWorldPosition(viewPosition);
        }

        public Vector2 WorldToViewDirection(Vector2 worldDirection)
        {
            return CurrentWorldSpace.WorldToViewDirection(worldDirection);
        }

        public Vector2 ViewToWorldDirection(Vector2 viewDirection)
        {
            return CurrentWorldSpace.ViewToWorldDirection(viewDirection);
        }

        private void TryInteractWithCurrentRoadSegment()
        {
            CircleRoadSegmentData segment = GetCurrentRoadSegment();
            if (segment == null || segment.contentType != SegmentContentType.Npc || segment.dialogue == null)
            {
                return;
            }

            GameHud hud = ResolveGameHud();
            if (hud == null)
            {
                return;
            }

            hud.ShowDialogue(segment.dialogue, segment.character);
        }

        private CircleRoadSegmentData GetCurrentRoadSegment()
        {
            if (roadSegmentList.Count == 0)
            {
                return null;
            }

            int roadIndex = Mathf.Clamp(Mathf.FloorToInt(currentRoadPosition), 0, roadSegmentList.Count - 1);
            return roadSegmentList[roadIndex];
        }

        private GameHud ResolveGameHud()
        {
            if (gameHud == null)
            {
                gameHud = FindAnyObjectByType<GameHud>();
            }

            return gameHud;
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
            RefreshCombatForCurrentRoadSegment(anchorIndex);
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

                segment.Show(roadSegmentList[roadSegmentIndex]);
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

        private void RefreshCombatForCurrentRoadSegment(int roadIndex)
        {
            CircleRoadSegmentData segment = GetCurrentRoadSegment();
            if (segment == null ||
                segment.contentType != SegmentContentType.Monster ||
                segment.enemy == null)
            {
                return;
            }

            if (spawnedEnemyRoadIndices.Contains(roadIndex))
            {
                return;
            }

            if (SpawnEnemy(segment))
            {
                spawnedEnemyRoadIndices.Add(roadIndex);
            }
        }

        private bool SpawnEnemy(CircleRoadSegmentData segment)
        {
            switch (segment.enemy.AttackType)
            {
                case EnemyAttackType.GroundMelee:
                    SpawnGroundEnemy(segment, EnemyAttackType.GroundMelee);
                    return true;
                case EnemyAttackType.GroundRanged:
                    SpawnGroundEnemy(segment, EnemyAttackType.GroundRanged);
                    return true;
                case EnemyAttackType.FlyingRobotRanged:
                    SpawnFlyingRobotEnemy(segment);
                    return true;
            }

            return false;
        }

        private void SpawnFlyingRobotEnemy(CircleRoadSegmentData segment)
        {
            GameObject enemyObject = new GameObject(segment.enemy.EnemyName);
            enemyObject.transform.SetParent(enemyRuntimeRoot != null ? enemyRuntimeRoot : transform.parent, false);
            enemyObject.transform.position = new Vector3(flyingRobotSpawnPosition.x, flyingRobotSpawnPosition.y, 0f);

            FlyingRobotEnemy flyingRobot = enemyObject.AddComponent<FlyingRobotEnemy>();
            flyingRobot.ConfigureViewAnchored(this, ResolvePlayerTarget(), segment.enemy, flyingRobotSpawnPosition, flyingRobotOrbitCenter);
        }

        private void SpawnGroundEnemy(CircleRoadSegmentData segment, EnemyAttackType attackType)
        {
            float spawnViewAngle = attackType == EnemyAttackType.GroundMelee
                ? groundMeleeSpawnViewAngle
                : groundRangedSpawnViewAngle;
            float worldAngle = GetWorldAngleFromViewAngle(spawnViewAngle);
            float radius = Mathf.Max(0.01f, PlayerRadius + groundEnemyRadiusOffset);
            Vector2 worldPosition = DiskCenter + CircleWorldSpace.DirectionFromAngleDegrees(worldAngle) * radius;
            Vector2 viewPosition = WorldToViewPosition(worldPosition);

            GameObject enemyObject = new GameObject(segment.enemy.EnemyName);
            enemyObject.transform.SetParent(enemyRuntimeRoot != null ? enemyRuntimeRoot : transform.parent, false);
            enemyObject.transform.position = new Vector3(viewPosition.x, viewPosition.y, 0f);

            float angularSpeed = 0f;
            if (attackType == EnemyAttackType.GroundMelee)
            {
                angularSpeed = moveSpeed * RoadSegmentAngleDegrees * Mathf.Max(1.01f, meleeEnemySpeedMultiplier);
            }

            GroundEnemy groundEnemy = enemyObject.AddComponent<GroundEnemy>();
            groundEnemy.Configure(
                this,
                ResolvePlayerTarget(),
                ResolveGameHud(),
                segment.enemy,
                attackType,
                worldAngle,
                radius,
                angularSpeed);
        }

        private float GetWorldAngleFromViewAngle(float viewAngleDegrees)
        {
            Vector2 viewDirection = CircleWorldSpace.DirectionFromAngleDegrees(viewAngleDegrees);
            Vector2 worldDirection = ViewToWorldDirection(viewDirection);
            return Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
        }

        private Transform ResolvePlayerTarget()
        {
            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.Find("Player");
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
            }

            return playerTarget;
        }

        private Vector2 GetDiskCenter()
        {
            if (circleRingRenderer != null)
            {
                return circleRingRenderer.bounds.center;
            }

            if (circleRotatingRoot != null)
            {
                return circleRotatingRoot.position;
            }

            return transform.position;
        }

        private float GetPlayerAngleDegrees()
        {
            return CircleWorldSpace.SixClockAngleDegrees + currentRoadPosition * GetOneSegmentAngle();
        }

        private float GetPlayerRadius()
        {
            if (hasPlayerRadius)
            {
                return playerRadius;
            }

            Transform target = ResolvePlayerTarget();
            if (target != null)
            {
                playerRadius = Vector2.Distance(target.position, GetDiskCenter());
            }

            if (playerRadius <= Mathf.Epsilon && circleRingRenderer != null)
            {
                playerRadius = circleRingRenderer.bounds.extents.y;
            }

            hasPlayerRadius = true;
            return Mathf.Max(0.01f, playerRadius);
        }

        // 构建道路段落列表DataList
        private void BuildRoadSegmentList()
        {
            roadSegmentList.Clear();
            spawnedEnemyRoadIndices.Clear();
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

        private void ResolveInteractionPromptSprites()
        {
            if (npcInteractionPromptSprite == null)
            {
                npcInteractionPromptSprite = Resources.Load<Sprite>(DialogueInteractionPromptResourcePath);
            }

            if (eventInteractionPromptSprite == null)
            {
                eventInteractionPromptSprite = Resources.Load<Sprite>(EventInteractionPromptResourcePath);
            }

            if (resourceInteractionPromptSprite == null)
            {
                resourceInteractionPromptSprite = Resources.Load<Sprite>(ResourceInteractionPromptResourcePath);
            }
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

            GameObject promptObject = new GameObject("Interaction Prompt Image");
            promptObject.transform.SetParent(segmentObject.transform, false);
            float resolvedPromptScale = interactionPromptScale > 0f ? interactionPromptScale : 1f;
            promptObject.transform.localScale = new Vector3(resolvedPromptScale, resolvedPromptScale, 1f);

            SpriteRenderer promptRenderer = promptObject.AddComponent<SpriteRenderer>();
            promptRenderer.sortingOrder = 8;
            promptRenderer.enabled = false;

            CircleMapSegment segment = segmentObject.AddComponent<CircleMapSegment>();
            segment.Setup(
                segmentRenderer,
                promptRenderer,
                npcInteractionPromptSprite,
                eventInteractionPromptSprite,
                resourceInteractionPromptSprite,
                interactionPromptOffset);
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
