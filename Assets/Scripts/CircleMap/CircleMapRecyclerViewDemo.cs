using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CircleWar
{
    /*
     * 这个脚本是圆形地图的“总指挥”。
     *
     * 新手更容易理解的做法是：场景里的物体由老师/同学在 Hierarchy 里提前摆好，
     * 脚本只负责读取这些物体、刷新图片、处理键盘移动。
     *
     * 需要的层级大致是：
     *   CircleMapRecyclerViewDemo
     *     Circle Map Runtime Root
     *       Background
     *       Circle Rotating Root
     *         Visible Segment Root
     *           Visible Segment 0
     *           Visible Segment 1
     *           ...
     *         Circle Ring
     *       Player
     */
    public sealed class CircleMapRecyclerViewDemo : MonoBehaviour
    {
        private const int PlayerBottomOffset = 0;
        private const int FrontEntryVisibleSlotIndex = 6;
        private const float CircleStartAngle = -90f;

        [Header("场景物体引用")]
        [SerializeField, Tooltip("圆形地图总根节点。把场景里的 Circle Map Runtime Root 拖到这里。")]
        private Transform mapRoot = null;

        [SerializeField, Tooltip("会旋转的圆环父物体。把场景里的 Circle Rotating Root 拖到这里。")]
        private Transform circleRotatingRoot = null;

        [SerializeField, Tooltip("所有可见点位的父物体。把场景里的 Visible Segment Root 拖到这里。")]
        private Transform segmentRoot = null;

        [SerializeField, Tooltip("背景图的 SpriteRenderer。把 Background 上的 SpriteRenderer 拖到这里。")]
        private SpriteRenderer backgroundRenderer = null;

        [SerializeField, Tooltip("圆环底图的 SpriteRenderer。把 Circle Ring 上的 SpriteRenderer 拖到这里。")]
        private SpriteRenderer ringRenderer = null;

        [SerializeField, Tooltip("场景里已有的人物物体。把 Player 拖到这里。")]
        private GameObject playerObject = null;

        [SerializeField, Tooltip("走完全程后显示的大地图占位物体，可不填。")]
        private GameObject bigMapObject = null;

        [Header("圆形地图设置")]
        [FormerlySerializedAs("circleAnchoredPosition")]
        [SerializeField, Tooltip("圆形地图在世界坐标中的中心位置。")]
        private Vector2 circleCenterPosition = new Vector2(0f, -0.2f);

        [FormerlySerializedAs("circleDiameter")]
        [SerializeField, Tooltip("圆圈半径，单位是 Unity 世界单位。")]
        private float circleRadius = 2.7f;

        [FormerlySerializedAs("segmentSize")]
        [SerializeField, Tooltip("每个点位图片允许占用的最大世界尺寸，会保持素材原始宽高比例。")]
        private Vector2 segmentWorldSize = new Vector2(2f, 2f);

        [SerializeField, Tooltip("点位物体从圆环外缘往内收多少，避免贴到圆环外侧。")]
        private float segmentAnchorInset = 0.22f;

        [SerializeField, Tooltip("真实道路数据总数。走完这些段后进入大地图。")]
        private int totalRoadSegmentCount = 30;

        [SerializeField, Tooltip("圆圈上同时显示多少个点位。这里固定为 12 更容易对应钟表位置。")]
        private int visibleSegmentCount = 12;

        [SerializeField, Tooltip("圆圈转动速度。数值越大，切换时越快。")]
        private float circleRotateSpeed = 8f;

        [Header("人物设置")]
        [FormerlySerializedAs("playerSize")]
        [SerializeField, Tooltip("人物在世界中的显示尺寸。")]
        private Vector2 playerWorldSize = new Vector2(0.8f, 1.2f);

        [FormerlySerializedAs("playerBottomOffset")]
        [SerializeField, Tooltip("人物从圆圈最低点往上抬多少，避免脚底贴边。")]
        private float playerBottomOffset = 0.25f;

        [SerializeField, Tooltip("是否播放人物自带的 Animator 动画。")]
        private bool animatePlayer = true;

        [Header("交互设置")]
        [SerializeField, Tooltip("勾选后，游戏开始时自动初始化场景里已有的圆形地图。")]
        private bool buildOnStart = true;

        [SerializeField, Tooltip("勾选后，可以用 A/D 或左右方向键前进后退。")]
        private bool enableLegacyKeyboardInput = true;

        [Header("教学调试")]
        [SerializeField, Tooltip("勾选后，每次移动都会在 Console 打印当前道路段。")]
        private bool printMoveLog = true;

        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();

        private int currentRoadSegmentIndex;
        private float targetCircleAngle;
        private bool hasEnteredBigMap;

        private void Start()
        {
            if (buildOnStart)
            {
                Build();
            }
        }

        private void Update()
        {
            ReadKeyboardInput();
            RotateCircleTowardTarget();
        }

        private void OnDestroy()
        {
            spriteFactory.DestroyAllSegmentSprites();
        }

        public void Build()
        {
            spriteFactory.DestroyAllSegmentSprites();
            spriteFactory.CreateAllSegmentSprites();
            BuildRoadData();

            if (!CheckSceneReferences())
            {
                return;
            }

            PrepareSceneObjects();

            currentRoadSegmentIndex = 0;
            hasEnteredBigMap = false;
            targetCircleAngle = 0f;

            RefreshVisibleSegments();
            RefreshPlayerPosition();
            SetCircleAngleImmediately(targetCircleAngle);
        }

        private bool CheckSceneReferences()
        {
            bool hasRequiredObjects = true;
            hasRequiredObjects &= CheckRequiredObject(mapRoot, "Map Root");
            hasRequiredObjects &= CheckRequiredObject(circleRotatingRoot, "Circle Rotating Root");
            hasRequiredObjects &= CheckRequiredObject(segmentRoot, "Visible Segment Root");
            hasRequiredObjects &= CheckRequiredObject(backgroundRenderer, "Background SpriteRenderer");
            hasRequiredObjects &= CheckRequiredObject(ringRenderer, "Circle Ring SpriteRenderer");
            hasRequiredObjects &= CheckRequiredObject(playerObject, "Player");

            return hasRequiredObjects;
        }

        private bool CheckRequiredObject(Object objectValue, string objectName)
        {
            if (objectValue != null)
            {
                return true;
            }

            Debug.LogError(objectName + " 没有拖引用。请在 Inspector 里把场景里的对应物体拖到这个脚本字段上。", this);
            return false;
        }

        private void PrepareSceneObjects()
        {
            mapRoot.gameObject.SetActive(true);
            circleRotatingRoot.gameObject.SetActive(true);
            playerObject.SetActive(true);

            circleRotatingRoot.position = new Vector3(circleCenterPosition.x, circleCenterPosition.y, 0f);

            PrepareBackground();
            PrepareRing();
            PreparePlayer();
            PrepareVisibleSegments();

            if (bigMapObject != null)
            {
                bigMapObject.SetActive(false);
            }
        }

        private void PrepareBackground()
        {
            backgroundRenderer.sortingOrder = -20;
            FitSpriteToCamera(backgroundRenderer.transform, backgroundRenderer);
        }

        private void PrepareRing()
        {
            ringRenderer.sortingOrder = -5;

            float ringDiameter = circleRadius * 2f;
            SetSpriteWorldSize(ringRenderer.transform, ringRenderer, new Vector2(ringDiameter, ringDiameter));
        }

        private void PreparePlayer()
        {
            SpriteRenderer playerRenderer = playerObject.GetComponentInChildren<SpriteRenderer>();
            if (playerRenderer != null)
            {
                playerRenderer.sortingOrder = 20;
                SetSpriteWorldSize(playerRenderer.transform, playerRenderer, playerWorldSize);
            }

            Animator playerAnimator = playerObject.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = animatePlayer;
            }
        }

        private void PrepareVisibleSegments()
        {
            visibleSegmentList.Clear();

            if (segmentRoot.childCount < visibleSegmentCount)
            {
                Debug.LogWarning(
                    "Visible Segment Root 下面只有 " + segmentRoot.childCount +
                    " 个子物体。脚本会直接使用现有子物体；如果要显示 12 个点位，请在场景里补齐子物体。",
                    this);
            }

            int usableSegmentCount = Mathf.Min(visibleSegmentCount, segmentRoot.childCount);
            for (int visibleSlotIndex = 0; visibleSlotIndex < usableSegmentCount; visibleSlotIndex++)
            {
                Transform segmentTransform = segmentRoot.GetChild(visibleSlotIndex);
                Vector2 localPosition = GetLocalPositionOnCircle(visibleSlotIndex);
                segmentTransform.localPosition = localPosition;
                segmentTransform.localEulerAngles = new Vector3(0f, 0f, GetSpriteRotationAngleOnCircle(localPosition));

                SpriteRenderer segmentRenderer = segmentTransform.GetComponent<SpriteRenderer>();
                if (segmentRenderer == null)
                {
                    segmentRenderer = segmentTransform.gameObject.AddComponent<SpriteRenderer>();
                }

                segmentRenderer.sortingOrder = 5;

                CircleMapSegment segment = segmentTransform.GetComponent<CircleMapSegment>();
                if (segment == null)
                {
                    segment = segmentTransform.gameObject.AddComponent<CircleMapSegment>();
                }

                segment.Setup(segmentRenderer, segmentWorldSize);
                visibleSegmentList.Add(segment);
            }
        }

        private void BuildRoadData()
        {
            roadSegmentList.Clear();

            List<CircleRoadSegmentData> newRoadList = roadMapBuilder.BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory);
            for (int index = 0; index < newRoadList.Count; index++)
            {
                roadSegmentList.Add(newRoadList[index]);
            }
        }

        public void StepForward()
        {
            if (hasEnteredBigMap)
            {
                return;
            }

            int nextRoadSegmentIndex = currentRoadSegmentIndex + 1;
            if (nextRoadSegmentIndex >= roadSegmentList.Count)
            {
                EnterBigMap();
                return;
            }

            MoveToRoadSegment(nextRoadSegmentIndex, true);
        }

        public void StepBackward()
        {
            if (hasEnteredBigMap)
            {
                return;
            }

            if (currentRoadSegmentIndex <= 0)
            {
                if (printMoveLog)
                {
                    Debug.Log("已经在道路起点，后面还是空地，不能后退。");
                }

                return;
            }

            MoveToRoadSegment(currentRoadSegmentIndex - 1, true);
        }

        public void ScrollToSegment(int index, bool smooth)
        {
            if (roadSegmentList.Count == 0)
            {
                return;
            }

            if (index < 0)
            {
                if (printMoveLog)
                {
                    Debug.Log("目标段在起点之前，那里是空地，不能移动过去。");
                }

                return;
            }

            if (index >= roadSegmentList.Count)
            {
                EnterBigMap();
                return;
            }

            MoveToRoadSegment(index, smooth);
        }

        private void MoveToRoadSegment(int newRoadSegmentIndex, bool smooth)
        {
            currentRoadSegmentIndex = newRoadSegmentIndex;
            targetCircleAngle = -currentRoadSegmentIndex * GetOneSegmentAngle();

            RefreshVisibleSegments();
            RefreshPlayerPosition();

            if (!smooth)
            {
                SetCircleAngleImmediately(targetCircleAngle);
            }

            if (printMoveLog)
            {
                CircleRoadSegmentData currentData = roadSegmentList[currentRoadSegmentIndex];
                Debug.Log("当前位置：" + currentRoadSegmentIndex + " - " + currentData.segmentName);
            }
        }

        private void ReadKeyboardInput()
        {
            if (!enableLegacyKeyboardInput)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                StepForward();
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                StepBackward();
            }
        }

        private void RotateCircleTowardTarget()
        {
            if (circleRotatingRoot == null)
            {
                return;
            }

            float currentAngle = circleRotatingRoot.localEulerAngles.z;
            float newAngle = Mathf.LerpAngle(currentAngle, targetCircleAngle, Time.deltaTime * circleRotateSpeed);
            circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, newAngle);
        }

        private void SetCircleAngleImmediately(float angle)
        {
            if (circleRotatingRoot != null)
            {
                circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, angle);
            }
        }

        private void RefreshVisibleSegments()
        {
            for (int tileIndex = 0; tileIndex < visibleSegmentList.Count; tileIndex++)
            {
                int ringOffset = GetRingOffsetFromBottom(tileIndex);
                int roadSegmentIndex = currentRoadSegmentIndex + ringOffset;
                bool isBottomTile = ringOffset == PlayerBottomOffset;
                CircleMapSegment visibleSegment = visibleSegmentList[tileIndex];

                if (roadSegmentIndex < 0)
                {
                    visibleSegment.ShowEmptyLand(spriteFactory.emptySegmentSprite, "起点后方空地");
                    continue;
                }

                if (roadSegmentIndex >= roadSegmentList.Count)
                {
                    visibleSegment.ShowRoadData(
                        roadSegmentIndex,
                        "通向大地图",
                        spriteFactory.exitSegmentSprite,
                        new Color(1f, 0.83f, 0.32f, 1f),
                        false);
                    continue;
                }

                CircleRoadSegmentData roadData = roadSegmentList[roadSegmentIndex];
                visibleSegment.ShowRoadData(
                    roadSegmentIndex,
                    roadData.segmentName,
                    roadData.iconSprite,
                    roadData.segmentColor,
                    isBottomTile);
            }
        }

        private int GetRingOffsetFromBottom(int tileIndex)
        {
            int rawOffset = tileIndex - currentRoadSegmentIndex;
            int wrappedOffset = ((rawOffset % visibleSegmentCount) + visibleSegmentCount) % visibleSegmentCount;

            if (wrappedOffset <= FrontEntryVisibleSlotIndex)
            {
                return wrappedOffset;
            }

            return wrappedOffset - visibleSegmentCount;
        }

        private void RefreshPlayerPosition()
        {
            if (playerObject == null)
            {
                return;
            }

            float playerY = circleCenterPosition.y - circleRadius + playerBottomOffset;
            playerObject.transform.position = new Vector3(circleCenterPosition.x, playerY, 0f);
        }

        private void EnterBigMap()
        {
            hasEnteredBigMap = true;

            if (circleRotatingRoot != null)
            {
                circleRotatingRoot.gameObject.SetActive(false);
            }

            if (playerObject != null)
            {
                playerObject.SetActive(false);
            }

            if (bigMapObject != null)
            {
                bigMapObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("已经走完道路，但没有设置 Big Map Placeholder，所以这里只隐藏圆环和人物。", this);
            }

            if (printMoveLog)
            {
                Debug.Log("30 段道路已经走完，切换到大地图。");
            }
        }

        private Vector3 GetLocalPositionOnCircle(int visibleSlotIndex)
        {
            float angle = CircleStartAngle + visibleSlotIndex * GetOneSegmentAngle();
            float radians = angle * Mathf.Deg2Rad;
            float anchorRadius = GetSegmentAnchorRadius();
            float x = Mathf.Cos(radians) * anchorRadius;
            float y = Mathf.Sin(radians) * anchorRadius;
            return new Vector3(x, y, 0f);
        }

        private float GetSegmentAnchorRadius()
        {
            return Mathf.Max(0f, circleRadius - segmentAnchorInset);
        }

        private float GetSpriteRotationAngleOnCircle(Vector2 localPosition)
        {
            if (localPosition.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            float outwardAngle = Mathf.Atan2(localPosition.y, localPosition.x) * Mathf.Rad2Deg;
            return outwardAngle + 90f;
        }

        private float GetOneSegmentAngle()
        {
            if (visibleSegmentCount <= 0)
            {
                return 30f;
            }

            return 360f / visibleSegmentCount;
        }

        private void FitSpriteToCamera(Transform spriteTransform, SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic)
            {
                SetSpriteWorldSize(spriteTransform, spriteRenderer, new Vector2(10f, 6f));
                return;
            }

            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector2 cameraSize = new Vector2(cameraWidth, cameraHeight);
            SetSpriteWorldSize(spriteTransform, spriteRenderer, cameraSize);
        }

        private void SetSpriteWorldSize(Transform spriteTransform, SpriteRenderer spriteRenderer, Vector2 targetWorldSize)
        {
            if (spriteTransform == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float scaleX = targetWorldSize.x / spriteSize.x;
            float scaleY = targetWorldSize.y / spriteSize.y;
            spriteTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

    }
}
