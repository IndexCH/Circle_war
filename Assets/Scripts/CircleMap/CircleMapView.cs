using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class CircleMapView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;
        private const string RoadSegmentDefinitionResourceFolder = "GameData/RoadSegments";
        private const string SeasonDefinitionResourceFolder = "GameData/Seasons";
        private const string SpringSeasonId = "spring";
        private const string SummerSeasonId = "summer";
        private const string FallSeasonId = "fall";
        private const string WinterSeasonId = "winter";
        private const string DialogueInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_dialogue";
        private const string EventInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_investigate";
        private const string ResourceInteractionPromptResourcePath = "Scence/UI/InteractionPrompts/press_e_collect";

        [SerializeField] private SpriteRenderer backgroundRenderer, circleRingRenderer;
        [SerializeField] private SpriteMask backgroundCircleMask;

        [SerializeField] private int totalRoadSegmentCount = 40;
        [SerializeField] private int visibleSegmentCount = 8;
        [SerializeField] private float segmentInsetFromRing = 0.22f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [SerializeField] private float segmentScale = 0.4f;
        [SerializeField] private float interactionPromptHorizontalOffset;
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
        private readonly List<SeasonDefinition> loadedSeasonDefinitions = new List<SeasonDefinition>();
        private readonly List<GameObject> spawnedEnemyObjects = new List<GameObject>();
        private readonly Dictionary<int, ICombatEnemy> spawnedEnemiesByRoadIndex = new Dictionary<int, ICombatEnemy>();
        private readonly Dictionary<int, CombatEnemyProgressBinding> bossProgressBindingsByRoadIndex =
            new Dictionary<int, CombatEnemyProgressBinding>();
        private readonly HashSet<int> spawnedEnemyRoadIndices = new HashSet<int>();
        private readonly HashSet<int> startedBossRoadIndices = new HashSet<int>();

        private Transform circleRotatingRoot;
        private GameRuntimeData subscribedRuntimeData;
        private GameRuntimeData observedRuntimeData;
        private SeasonDefinition activeSeason;
        private int activeSeasonIndex = -1;
        private int observedRunRevision = -1;
        private float currentRoadPosition;
        private float playerRadius;
        private bool hasPlayerRadius;
        private bool isMapInitialized;
        private int lastDisplayedAnchorIndex = -1;
        private int lastCompletedInteractionCount = -1;
        private string observedRunId = string.Empty;

        public static CircleMapView Active { get; private set; }
        public SeasonDefinition ActiveSeason => activeSeason;
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
            GameHud hud = ResolveGameHud();
            EnsureRuntimeDataSubscription(false);
            LoadSeasonDefinitions();
            if (!ActivateSeason(ResolveInitialSeasonIndex(hud?.RuntimeData.State.CurrentSeasonId), false))
            {
                BuildBlackMask();
                BuildRoadSegmentList();
            }
            ResolveInteractionPromptSprites();
            BuildVisibleSegments();
            InvalidateVisibleSegmentCache();
            TryRefreshVisibleSegments();
            ApplyCircleRotation();
            isMapInitialized = true;
        }

        private void OnEnable()
        {
            if (isMapInitialized)
            {
                EnsureRuntimeDataSubscription(true);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeData();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuntimeData();
            ClearSpawnedEnemies();

            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            EnsureRuntimeDataSubscription(true);
            GameHud hud = ResolveGameHud();
            bool isDialogueVisible = hud != null && hud.HudData.Dialogue.IsVisible.Value;
            if (!isDialogueVisible)
            {
                bool wantsMoveForward = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                bool wantsMoveBackward = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                bool isForwardBlocked = wantsMoveForward && GroundEnemy.IsAnyMeleeBlockingForward(this);
                bool pressedMoveForward = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

                if (pressedMoveForward &&
                    !isForwardBlocked &&
                    IsAtEndOfCurrentSeason() &&
                    !IsTerminalCombatBlockingSeasonAdvance() &&
                    TryAdvanceToNextSeason())
                {
                    return;
                }

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
                    float previousRoadPosition = currentRoadPosition;
                    float maxPosition = Mathf.Max(0f, roadSegmentList.Count - 1);
                    currentRoadPosition = Mathf.Clamp(
                        currentRoadPosition + moveInput * moveSpeed * Time.deltaTime,
                        0f,
                        maxPosition);

                    if (!Mathf.Approximately(previousRoadPosition, currentRoadPosition))
                    {
                        hud?.RuntimeData.SetRoadPosition(currentRoadPosition);
                    }
                }

                if (Input.GetKeyDown(interactKey))
                {
                    TryInteractWithCurrentRoadSegment();
                }
            }

            TryRefreshVisibleSegments();
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
            if (segment == null)
            {
                return;
            }

            GameHud hud = ResolveGameHud();
            if (hud == null)
            {
                return;
            }

            switch (segment.contentType)
            {
                case SegmentContentType.Npc when segment.dialogue != null &&
                                                     !hud.RuntimeData.IsInteractionCompleted(segment.dialogue):
                    hud.ShowDialogue(segment.dialogue, segment.character);
                    break;
                case SegmentContentType.Event when segment.gameEvent != null &&
                                                       !hud.RuntimeData.IsInteractionCompleted(segment.gameEvent):
                    hud.ShowDialogueEvent(segment.gameEvent);
                    break;
                case SegmentContentType.Resource when
                    !hud.RuntimeData.IsInteractionCompleted(segment.segmentId):
                    hud.RuntimeData.TryCollectRoadSegmentResource(segment.segmentId, segment.rewards);
                    break;
            }
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

        private void EnsureRuntimeDataSubscription(bool resetOnRunChange)
        {
            GameRuntimeData runtimeData = ResolveGameHud()?.RuntimeData;
            bool runtimeChanged = !ReferenceEquals(observedRuntimeData, runtimeData);
            bool revisionChanged = runtimeData != null && observedRunRevision != runtimeData.RunRevision;
            string currentRunId = runtimeData != null ? runtimeData.State.RunId : string.Empty;
            bool runIdChanged = !string.Equals(observedRunId, currentRunId, StringComparison.Ordinal);

            if (!ReferenceEquals(subscribedRuntimeData, runtimeData))
            {
                UnsubscribeFromRuntimeData();
                subscribedRuntimeData = runtimeData;
                if (subscribedRuntimeData != null)
                {
                    subscribedRuntimeData.NewRunStarted += HandleNewRunStarted;
                }
            }

            observedRuntimeData = runtimeData;
            observedRunRevision = runtimeData != null ? runtimeData.RunRevision : -1;
            observedRunId = currentRunId;

            if (resetOnRunChange &&
                isMapInitialized &&
                runtimeData != null)
            {
                if (runtimeChanged)
                {
                    SynchronizeMapWithRuntimeData(runtimeData);
                }
                else if (revisionChanged || runIdChanged)
                {
                    ResetMapForNewRun();
                }
            }
        }

        private void UnsubscribeFromRuntimeData()
        {
            if (subscribedRuntimeData != null)
            {
                subscribedRuntimeData.NewRunStarted -= HandleNewRunStarted;
                subscribedRuntimeData = null;
            }
        }

        private void HandleNewRunStarted()
        {
            if (subscribedRuntimeData == null)
            {
                return;
            }

            observedRuntimeData = subscribedRuntimeData;
            observedRunRevision = subscribedRuntimeData.RunRevision;
            observedRunId = subscribedRuntimeData.State.RunId;
            if (isMapInitialized)
            {
                ResetMapForNewRun();
            }
        }

        private void ResetMapForNewRun()
        {
            LoadSeasonDefinitions();
            int springSeasonIndex = FindSeasonIndex(SpringSeasonId);
            if (springSeasonIndex >= 0)
            {
                ActivateSeason(springSeasonIndex, visibleSegmentList.Count > 0);
                return;
            }

            ClearSpawnedEnemies();
            activeSeasonIndex = -1;
            activeSeason = null;
            currentRoadPosition = 0f;
            hasPlayerRadius = false;
            ApplyCircleRotation();
            BuildBlackMask();
            BuildRoadSegmentList();
            InvalidateVisibleSegmentCache();
            if (visibleSegmentList.Count > 0)
            {
                RefreshVisibleSegmentLayout();
                TryRefreshVisibleSegments();
            }
        }

        private void SynchronizeMapWithRuntimeData(GameRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                return;
            }

            LoadSeasonDefinitions();
            int seasonIndex = ResolveInitialSeasonIndex(runtimeData.State.CurrentSeasonId);
            if (!ActivateSeason(
                    seasonIndex,
                    visibleSegmentList.Count > 0,
                    runtimeData.State.CurrentRoadPosition))
            {
                ResetMapForNewRun();
            }
        }

        private void TryRefreshVisibleSegments()
        {
            int anchorIndex = Mathf.FloorToInt(currentRoadPosition);
            int completedInteractionCount = ResolveGameHud()?.RuntimeData.State.Events.Count ?? 0;
            if (anchorIndex == lastDisplayedAnchorIndex &&
                completedInteractionCount == lastCompletedInteractionCount)
            {
                return;
            }

            lastDisplayedAnchorIndex = anchorIndex;
            lastCompletedInteractionCount = completedInteractionCount;
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

                CircleRoadSegmentData roadSegment = roadSegmentList[roadSegmentIndex];
                segment.Show(roadSegment, ShouldShowInteractionPrompt(roadSegment));
            }
        }

        private bool ShouldShowInteractionPrompt(CircleRoadSegmentData segment)
        {
            if (segment == null)
            {
                return false;
            }

            GameRuntimeData runtimeData = ResolveGameHud()?.RuntimeData;
            switch (segment.contentType)
            {
                case SegmentContentType.Npc:
                    return segment.dialogue != null &&
                           (runtimeData == null || !runtimeData.IsInteractionCompleted(segment.dialogue));
                case SegmentContentType.Event:
                    return segment.gameEvent != null &&
                           (runtimeData == null || !runtimeData.IsInteractionCompleted(segment.gameEvent));
                case SegmentContentType.Resource:
                    return !string.IsNullOrWhiteSpace(segment.segmentId) &&
                           (runtimeData == null || !runtimeData.IsInteractionCompleted(segment.segmentId));
                default:
                    return true;
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
            if (segment == null)
            {
                return;
            }

            bool isWinterTerminalBoss = roadIndex == roadSegmentList.Count - 1 &&
                                        activeSeason != null &&
                                        string.Equals(
                                            activeSeason.DefinitionId,
                                            WinterSeasonId,
                                            StringComparison.OrdinalIgnoreCase);
            if (segment.contentType == SegmentContentType.Boss || isWinterTerminalBoss)
            {
                CombatEnemyProgressBinding progressBinding = StartBossEncounter(roadIndex, segment);
                SpawnSegmentEnemyOnce(roadIndex, segment, progressBinding);
                return;
            }

            if (segment.contentType == SegmentContentType.Monster)
            {
                SpawnSegmentEnemyOnce(roadIndex, segment);
            }
        }

        private CombatEnemyProgressBinding StartBossEncounter(int roadIndex, CircleRoadSegmentData segment)
        {
            if (bossProgressBindingsByRoadIndex.TryGetValue(roadIndex, out CombatEnemyProgressBinding existingBinding))
            {
                return existingBinding;
            }

            if (startedBossRoadIndices.Contains(roadIndex))
            {
                return null;
            }

            string fallbackBossId = !string.IsNullOrWhiteSpace(segment.segmentId)
                ? segment.segmentId + "_boss"
                : (activeSeason != null ? activeSeason.DefinitionId : WinterSeasonId) + "_boss";
            string fallbackBossName = string.IsNullOrWhiteSpace(segment.segmentName)
                ? "年度危机"
                : segment.segmentName;

            GameRuntimeData runtimeData = ResolveGameHud()?.RuntimeData;
            if (runtimeData != null &&
                runtimeData.StartBossEncounter(
                    segment.boss,
                    fallbackBossId,
                    fallbackBossName,
                    out CombatEnemyProgressBinding progressBinding))
            {
                startedBossRoadIndices.Add(roadIndex);
                if (progressBinding != null)
                {
                    bossProgressBindingsByRoadIndex[roadIndex] = progressBinding;
                }

                return progressBinding;
            }

            return null;
        }

        private void SpawnSegmentEnemyOnce(
            int roadIndex,
            CircleRoadSegmentData segment,
            CombatEnemyProgressBinding progressBinding = null)
        {
            if (segment.enemy == null || spawnedEnemyRoadIndices.Contains(roadIndex))
            {
                return;
            }

            if (progressBinding != null && progressBinding.CurrentHealth <= 0)
            {
                spawnedEnemyRoadIndices.Add(roadIndex);
                return;
            }

            ICombatEnemy spawnedEnemy = SpawnEnemy(segment, progressBinding);
            if (spawnedEnemy != null)
            {
                spawnedEnemyRoadIndices.Add(roadIndex);
                spawnedEnemiesByRoadIndex[roadIndex] = spawnedEnemy;
            }
        }

        private ICombatEnemy SpawnEnemy(
            CircleRoadSegmentData segment,
            CombatEnemyProgressBinding progressBinding)
        {
            switch (segment.enemy.AttackType)
            {
                case EnemyAttackType.GroundMelee:
                    return SpawnGroundEnemy(segment, EnemyAttackType.GroundMelee, progressBinding);
                case EnemyAttackType.GroundRanged:
                    return SpawnGroundEnemy(segment, EnemyAttackType.GroundRanged, progressBinding);
                case EnemyAttackType.FlyingRobotRanged:
                    return SpawnFlyingRobotEnemy(segment, progressBinding);
            }

            return null;
        }

        private FlyingRobotEnemy SpawnFlyingRobotEnemy(
            CircleRoadSegmentData segment,
            CombatEnemyProgressBinding progressBinding)
        {
            string runtimeEnemyName = segment.boss != null
                ? segment.boss.BossName
                : segment.enemy.EnemyName;
            GameObject enemyObject = new GameObject(runtimeEnemyName);
            enemyObject.transform.SetParent(enemyRuntimeRoot != null ? enemyRuntimeRoot : transform.parent, false);
            enemyObject.transform.position = new Vector3(flyingRobotSpawnPosition.x, flyingRobotSpawnPosition.y, 0f);
            spawnedEnemyObjects.Add(enemyObject);

            FlyingRobotEnemy flyingRobot = enemyObject.AddComponent<FlyingRobotEnemy>();
            flyingRobot.ConfigureViewAnchored(
                this,
                ResolvePlayerTarget(),
                segment.enemy,
                flyingRobotSpawnPosition,
                flyingRobotOrbitCenter,
                progressBinding,
                segment.boss);
            return flyingRobot;
        }

        private GroundEnemy SpawnGroundEnemy(
            CircleRoadSegmentData segment,
            EnemyAttackType attackType,
            CombatEnemyProgressBinding progressBinding)
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
            spawnedEnemyObjects.Add(enemyObject);

            float angularSpeed = 0f;
            if (attackType == EnemyAttackType.GroundMelee)
            {
                float roadSegmentsPerSecond = segment.enemy.Speed > 0f
                    ? segment.enemy.Speed
                    : moveSpeed * Mathf.Max(1.01f, meleeEnemySpeedMultiplier);
                roadSegmentsPerSecond *= 0.5f;
                angularSpeed = roadSegmentsPerSecond * RoadSegmentAngleDegrees;
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
                angularSpeed,
                progressBinding);
            return groundEnemy;
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
            spawnedEnemiesByRoadIndex.Clear();
            bossProgressBindingsByRoadIndex.Clear();
            spawnedEnemyRoadIndices.Clear();
            startedBossRoadIndices.Clear();
            roadSegmentList.AddRange(roadMapBuilder.BuildRoadSegmentList(
                totalRoadSegmentCount,
                spriteFactory,
                GetRoadSegmentDefinitions(),
                activeSeason));
        }

        private void LoadSeasonDefinitions()
        {
            if (loadedSeasonDefinitions.Count > 0)
            {
                return;
            }

            loadedSeasonDefinitions.AddRange(Resources.LoadAll<SeasonDefinition>(SeasonDefinitionResourceFolder));
            loadedSeasonDefinitions.Sort(CompareSeasonDefinitions);
        }

        private int ResolveInitialSeasonIndex(string currentSeasonId)
        {
            int currentIndex = FindSeasonIndex(currentSeasonId);
            if (currentIndex >= 0)
            {
                return currentIndex;
            }

            int springIndex = FindSeasonIndex(SpringSeasonId);
            return springIndex >= 0 ? springIndex : (loadedSeasonDefinitions.Count > 0 ? 0 : -1);
        }

        private int FindSeasonIndex(string seasonId)
        {
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return -1;
            }

            for (int index = 0; index < loadedSeasonDefinitions.Count; index++)
            {
                SeasonDefinition season = loadedSeasonDefinitions[index];
                if (season != null &&
                    string.Equals(season.DefinitionId, seasonId, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsAtEndOfCurrentSeason()
        {
            float maxPosition = Mathf.Max(0f, roadSegmentList.Count - 1);
            return currentRoadPosition >= maxPosition - 0.0001f;
        }

        private bool IsTerminalCombatBlockingSeasonAdvance()
        {
            if (!IsAtEndOfCurrentSeason())
            {
                return false;
            }

            CircleRoadSegmentData segment = GetCurrentRoadSegment();
            if (segment == null ||
                (segment.contentType != SegmentContentType.Monster &&
                 segment.contentType != SegmentContentType.Boss))
            {
                return false;
            }

            int roadIndex = Mathf.Clamp(
                Mathf.FloorToInt(currentRoadPosition),
                0,
                roadSegmentList.Count - 1);
            if (!spawnedEnemyRoadIndices.Contains(roadIndex))
            {
                RefreshCombatForCurrentRoadSegment(roadIndex);
            }

            if (!spawnedEnemiesByRoadIndex.TryGetValue(roadIndex, out ICombatEnemy enemy))
            {
                return false;
            }

            if (IsCombatEnemyAlive(enemy))
            {
                return true;
            }

            spawnedEnemiesByRoadIndex.Remove(roadIndex);
            return false;
        }

        private static bool IsCombatEnemyAlive(ICombatEnemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (enemy is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return enemy.IsAlive;
        }

        private bool TryAdvanceToNextSeason()
        {
            int nextSeasonIndex = activeSeasonIndex + 1;
            if (activeSeasonIndex < 0 || nextSeasonIndex >= loadedSeasonDefinitions.Count)
            {
                return false;
            }

            return ActivateSeason(nextSeasonIndex, true);
        }

        private bool ActivateSeason(
            int seasonIndex,
            bool refreshExistingMap,
            float initialRoadPosition = 0f)
        {
            if (seasonIndex < 0 || seasonIndex >= loadedSeasonDefinitions.Count)
            {
                return false;
            }

            ClearSpawnedEnemies();
            activeSeasonIndex = seasonIndex;
            activeSeason = loadedSeasonDefinitions[seasonIndex];
            currentRoadPosition = Mathf.Max(0f, initialRoadPosition);
            hasPlayerRadius = false;

            ApplyActiveSeasonVisuals();
            ApplyCircleRotation();
            BuildBlackMask();
            BuildRoadSegmentList();
            currentRoadPosition = Mathf.Clamp(
                currentRoadPosition,
                0f,
                Mathf.Max(0f, roadSegmentList.Count - 1));
            ApplyActiveSeasonRuntimeContext(currentRoadPosition);
            InvalidateVisibleSegmentCache();

            if (refreshExistingMap)
            {
                RefreshVisibleSegmentLayout();
                TryRefreshVisibleSegments();
            }

            return true;
        }

        private void ApplyActiveSeasonVisuals()
        {
            if (activeSeason == null)
            {
                return;
            }

            if (backgroundRenderer != null && activeSeason.BackgroundSprite != null)
            {
                backgroundRenderer.sprite = activeSeason.BackgroundSprite;
            }

            if (circleRingRenderer != null && activeSeason.CircleRingSprite != null)
            {
                circleRingRenderer.sprite = activeSeason.CircleRingSprite;
            }
        }

        private void ApplyActiveSeasonRuntimeContext(float roadPosition)
        {
            if (activeSeason == null)
            {
                return;
            }

            RegionDefinition region = activeSeason.Region;
            if (region == null)
            {
                for (int index = 0; index < roadSegmentList.Count; index++)
                {
                    if (roadSegmentList[index].region != null)
                    {
                        region = roadSegmentList[index].region;
                        break;
                    }
                }
            }

            ResolveGameHud()?.RuntimeData.SetSeasonContext(activeSeason, region, roadPosition);
        }

        private void InvalidateVisibleSegmentCache()
        {
            lastDisplayedAnchorIndex = -1;
            lastCompletedInteractionCount = -1;
        }

        private void RefreshVisibleSegmentLayout()
        {
            for (int index = 0; index < visibleSegmentList.Count; index++)
            {
                CircleMapSegment segment = visibleSegmentList[index];
                segment.transform.localPosition = GetLocalPositionOnCircle(index);
                segment.transform.localEulerAngles = new Vector3(0f, 0f, index * GetOneSegmentAngle());
                segment.transform.localScale = new Vector3(segmentScale, segmentScale, 1f);
            }
        }

        private void ClearSpawnedEnemies()
        {
            for (int index = 0; index < spawnedEnemyObjects.Count; index++)
            {
                if (spawnedEnemyObjects[index] != null)
                {
                    spawnedEnemyObjects[index].SetActive(false);
                    Destroy(spawnedEnemyObjects[index]);
                }
            }

            spawnedEnemyObjects.Clear();
            spawnedEnemiesByRoadIndex.Clear();
            bossProgressBindingsByRoadIndex.Clear();
            spawnedEnemyRoadIndices.Clear();
            startedBossRoadIndices.Clear();
        }

        private static int CompareSeasonDefinitions(SeasonDefinition left, SeasonDefinition right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int orderComparison = left.SeasonOrder.CompareTo(right.SeasonOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int canonicalComparison = GetCanonicalSeasonOrder(left).CompareTo(GetCanonicalSeasonOrder(right));
            return canonicalComparison != 0
                ? canonicalComparison
                : string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetCanonicalSeasonOrder(SeasonDefinition season)
        {
            if (season == null || string.IsNullOrWhiteSpace(season.DefinitionId))
            {
                return int.MaxValue;
            }

            switch (season.DefinitionId.ToLowerInvariant())
            {
                case SpringSeasonId:
                    return 0;
                case SummerSeasonId:
                    return 1;
                case FallSeasonId:
                    return 2;
                case WinterSeasonId:
                    return 3;
                default:
                    return int.MaxValue;
            }
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
                interactionPromptHorizontalOffset);
            return segment;
        }

        private Vector3 GetLocalPositionOnCircle(int index)
        {
            float angle = CircleStartAngle + index * GetOneSegmentAngle();
            float radius = GetCircleRingLocalSize().x / 2f - segmentInsetFromRing;
            return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f);
        }

        private Vector2 GetCircleRingLocalSize()
        {
            if (circleRingRenderer == null || circleRingRenderer.sprite == null)
            {
                return Vector2.zero;
            }

            Vector2 spriteSize = circleRingRenderer.sprite.bounds.size;
            Vector3 localScale = circleRingRenderer.transform.localScale;
            return new Vector2(
                spriteSize.x * Mathf.Abs(localScale.x),
                spriteSize.y * Mathf.Abs(localScale.y));
        }

        private Vector2 GetCircleRingWorldSize()
        {
            if (circleRingRenderer == null || circleRingRenderer.sprite == null)
            {
                return Vector2.zero;
            }

            Vector2 spriteSize = circleRingRenderer.sprite.bounds.size;
            Vector3 worldScale = circleRingRenderer.transform.lossyScale;
            return new Vector2(
                spriteSize.x * Mathf.Abs(worldScale.x),
                spriteSize.y * Mathf.Abs(worldScale.y));
        }

        private float GetOneSegmentAngle()
        {
            return 360f / visibleSegmentCount;
        }

        // 黑屏遮罩
        private void BuildBlackMask()
        {
            if (backgroundRenderer == null ||
                circleRingRenderer == null ||
                backgroundCircleMask == null ||
                backgroundCircleMask.sprite == null)
            {
                return;
            }

            Vector2 ringSize = GetCircleRingWorldSize();
            Vector2 maskSize = backgroundCircleMask.sprite.bounds.size;
            if (ringSize.x <= Mathf.Epsilon || ringSize.y <= Mathf.Epsilon ||
                maskSize.x <= Mathf.Epsilon || maskSize.y <= Mathf.Epsilon)
            {
                return;
            }

            Transform maskParent = backgroundCircleMask.transform.parent;
            Vector3 maskParentScale = maskParent != null ? maskParent.lossyScale : Vector3.one;
            float parentScaleX = Mathf.Max(Mathf.Abs(maskParentScale.x), Mathf.Epsilon);
            float parentScaleY = Mathf.Max(Mathf.Abs(maskParentScale.y), Mathf.Epsilon);
            backgroundCircleMask.transform.position = circleRingRenderer.transform.TransformPoint(
                circleRingRenderer.sprite.bounds.center);
            backgroundCircleMask.transform.localScale = new Vector3(
                ringSize.x / maskSize.x / parentScaleX,
                ringSize.y / maskSize.y / parentScaleY,
                1f);
            backgroundRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }
}
