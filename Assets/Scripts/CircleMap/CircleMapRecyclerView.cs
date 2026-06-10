using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    /*
     * 这个脚本是“教学版圆形地图”的主脚本。
     *
     * 它参考了 CircleMapRecyclerViewDemo，但刻意减少场景层级和复杂逻辑：
     *   Circle Map Runtime Root
     *     Background
     *     Circle Rotating Root
     *       Circle Ring
     *       Visible Segment 0
     *       Visible Segment 1
     *       ...
     *     Player
     *
     * 它负责把几个小模块拼起来：
     *   1. 准备图片资源。
     *   2. 准备 30 段道路数据。
     *   3. 准备背景、圆圈、人物。
     *   4. 准备 12 个可见段位。
     *   5. 根据玩家当前位置刷新显示。
     */
    public sealed class CircleMapRecyclerView : MonoBehaviour
    {
        private const float CircleStartAngle = -90f;
        private const int FrontVisibleSlotCount = 6;

        [Header("场景物体引用")]
        [SerializeField, Tooltip("圆形地图总根节点。不填时默认使用挂着本脚本的 GameObject。")]
        private Transform mapRoot = null;

        [SerializeField, Tooltip("背景图的 SpriteRenderer。可以拖 Background 上的 SpriteRenderer。")]
        private SpriteRenderer backgroundRenderer = null;

        [SerializeField, Tooltip("会旋转的圆圈父物体。可以拖 Circle Rotating Root。")]
        private Transform circleRotatingRoot = null;

        [SerializeField, Tooltip("圆环底图的 SpriteRenderer。可以拖 Circle Ring 上的 SpriteRenderer。")]
        private SpriteRenderer circleRingRenderer = null;

        [SerializeField, Tooltip("人物物体。可以拖 Player。人物不会跟着圆圈旋转。")]
        private GameObject playerObject = null;

        [Header("圆形地图设置")]
        [SerializeField, Tooltip("圆圈中心在世界坐标里的位置。")]
        private Vector2 circleCenterPosition = new Vector2(0f, -0.2f);

        [SerializeField, Tooltip("圆圈半径，单位是 Unity 世界单位。")]
        private float circleRadius = 2.7f;

        [SerializeField, Tooltip("段位图片显示的目标大小，单位是 Unity 世界单位。")]
        private Vector2 segmentWorldSize = new Vector2(2f, 2f);

        [SerializeField, Tooltip("段位离圆环外边缘往里收多少，避免图片太贴边。")]
        private float segmentInsetFromRing = 0.22f;

        [SerializeField, Tooltip("真实道路一共有多少段。走完后就停止在大地图入口。")]
        private int totalRoadSegmentCount = 30;

        [SerializeField, Tooltip("圆圈上同时显示多少个段位。12 个最像钟表，适合课堂讲。")]
        private int visibleSegmentCount = 12;

        [SerializeField, Tooltip("圆圈旋转速度。数值越大，切换越快。")]
        private float circleRotateSpeed = 8f;

        [Header("人物设置")]
        [SerializeField, Tooltip("人物显示大小，单位是 Unity 世界单位。")]
        private Vector2 playerWorldSize = new Vector2(0.8f, 1.2f);

        [SerializeField, Tooltip("人物从圆圈最低点向上抬多少，避免脚底贴住圆边。")]
        private float playerBottomOffset = 0.25f;

        [SerializeField, Tooltip("是否播放人物 Animator 动画。")]
        private bool animatePlayer = true;

        [Header("交互设置")]
        [SerializeField, Tooltip("勾选后，游戏开始时自动初始化圆形地图。")]
        private bool buildOnStart = true;

        [SerializeField, Tooltip("勾选后，可以用 A/D 或左右方向键前进后退。")]
        private bool enableLegacyKeyboardInput = true;

        [SerializeField, Tooltip("勾选后，每次移动都会在 Console 打印当前段位。")]
        private bool printMoveLog = true;

        private readonly CircleSegmentSpriteFactory spriteFactory = new CircleSegmentSpriteFactory();
        private readonly CircleRoadMapBuilder roadMapBuilder = new CircleRoadMapBuilder();
        private readonly List<CircleRoadSegmentData> roadSegmentList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();

        private int currentRoadSegmentIndex;
        private float targetCircleAngle;
        private bool hasFinishedRoad;

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
        }

        public void Build()
        {
            PrepareSpriteModule();
            PrepareRoadDataModule();
            FindSceneObjectsIfNeeded();

            if (!CheckSceneObjects())
            {
                return;
            }

            PrepareBackgroundModule();
            PrepareCircleModule();
            PreparePlayerModule();
            PrepareVisibleSegmentModule();
            ResetMoveState();
            RefreshVisibleSegments();
            RefreshPlayerPosition();
            SetCircleAngleImmediately(targetCircleAngle);
        }

        private void PrepareSpriteModule()
        {
        }

        private void PrepareRoadDataModule()
        {
            roadSegmentList.Clear();

            List<CircleRoadSegmentData> newRoadSegmentList = roadMapBuilder.BuildRoadSegmentList(totalRoadSegmentCount, spriteFactory);
            for (int index = 0; index < newRoadSegmentList.Count; index++)
            {
                roadSegmentList.Add(newRoadSegmentList[index]);
            }
        }

        private void FindSceneObjectsIfNeeded()
        {
            if (mapRoot == null)
            {
                mapRoot = transform;
            }

            if (backgroundRenderer == null)
            {
                Transform backgroundTransform = mapRoot.Find("Background");
                if (backgroundTransform != null)
                {
                    backgroundRenderer = backgroundTransform.GetComponent<SpriteRenderer>();
                }
            }

            if (circleRotatingRoot == null)
            {
                circleRotatingRoot = mapRoot.Find("Circle Rotating Root");
            }

            if (circleRingRenderer == null && circleRotatingRoot != null)
            {
                Transform circleRingTransform = circleRotatingRoot.Find("Circle Ring");
                if (circleRingTransform != null)
                {
                    circleRingRenderer = circleRingTransform.GetComponent<SpriteRenderer>();
                }
            }

            if (playerObject == null)
            {
                Transform playerTransform = mapRoot.Find("Player");
                if (playerTransform != null)
                {
                    playerObject = playerTransform.gameObject;
                }
            }
        }

        private bool CheckSceneObjects()
        {
            bool hasAllRequiredObjects = true;
            hasAllRequiredObjects &= CheckRequiredObject(mapRoot, "Map Root");
            hasAllRequiredObjects &= CheckRequiredObject(backgroundRenderer, "Background SpriteRenderer");
            hasAllRequiredObjects &= CheckRequiredObject(circleRotatingRoot, "Circle Rotating Root");
            hasAllRequiredObjects &= CheckRequiredObject(circleRingRenderer, "Circle Ring SpriteRenderer");
            hasAllRequiredObjects &= CheckRequiredObject(playerObject, "Player");
            return hasAllRequiredObjects;
        }

        private bool CheckRequiredObject(Object objectValue, string objectName)
        {
            if (objectValue != null)
            {
                return true;
            }

            Debug.LogError(objectName + " 没有找到。请检查 Hierarchy 名字，或者在 Inspector 手动拖引用。", this);
            return false;
        }

        private void PrepareBackgroundModule()
        {
            backgroundRenderer.sortingOrder = -20;
            FitSpriteToCamera(backgroundRenderer.transform, backgroundRenderer);
        }

        private void PrepareCircleModule()
        {
            circleRotatingRoot.gameObject.SetActive(true);
            circleRotatingRoot.position = new Vector3(circleCenterPosition.x, circleCenterPosition.y, 0f);

            circleRingRenderer.sortingOrder = -5;

            float circleDiameter = circleRadius * 2f;
            SetSpriteWorldSize(circleRingRenderer.transform, circleRingRenderer, new Vector2(circleDiameter, circleDiameter));
        }

        private void PreparePlayerModule()
        {
            playerObject.SetActive(true);

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

        private void PrepareVisibleSegmentModule()
        {
            visibleSegmentList.Clear();

            for (int visibleSlotIndex = 0; visibleSlotIndex < visibleSegmentCount; visibleSlotIndex++)
            {
                GameObject segmentObject = GetOrCreateSegmentObject(visibleSlotIndex);
                segmentObject.transform.localPosition = GetLocalPositionOnCircle(visibleSlotIndex);
                segmentObject.transform.localEulerAngles = Vector3.zero;
                segmentObject.SetActive(true);

                SpriteRenderer segmentRenderer = segmentObject.GetComponent<SpriteRenderer>();
                if (segmentRenderer == null)
                {
                    segmentRenderer = segmentObject.AddComponent<SpriteRenderer>();
                }

                segmentRenderer.sortingOrder = 5;

                CircleMapSegment segment = segmentObject.GetComponent<CircleMapSegment>();
                if (segment == null)
                {
                    segment = segmentObject.AddComponent<CircleMapSegment>();
                }

                segment.Setup(segmentRenderer, segmentWorldSize);
                visibleSegmentList.Add(segment);
            }
        }

        private GameObject GetOrCreateSegmentObject(int visibleSlotIndex)
        {
            string segmentObjectName = "Visible Segment " + visibleSlotIndex;
            Transform segmentTransform = circleRotatingRoot.Find(segmentObjectName);

            if (segmentTransform != null)
            {
                return segmentTransform.gameObject;
            }

            /*
             * new GameObject 会在场景里创建一个空物体。
             * SetParent(..., false) 表示挂到圆圈下面，并保留本地坐标的简单写法。
             */
            GameObject segmentObject = new GameObject(segmentObjectName);
            segmentObject.transform.SetParent(circleRotatingRoot, false);
            return segmentObject;
        }

        private void ResetMoveState()
        {
            currentRoadSegmentIndex = 0;
            targetCircleAngle = 0f;
            hasFinishedRoad = false;
        }

        public void StepForward()
        {
            if (hasFinishedRoad)
            {
                return;
            }

            int nextRoadSegmentIndex = currentRoadSegmentIndex + 1;
            if (nextRoadSegmentIndex >= roadSegmentList.Count)
            {
                FinishRoad();
                return;
            }

            MoveToRoadSegment(nextRoadSegmentIndex, true);
        }

        public void StepBackward()
        {
            if (hasFinishedRoad)
            {
                return;
            }

            if (currentRoadSegmentIndex <= 0)
            {
                if (printMoveLog)
                {
                    Debug.Log("已经在起点，后面是空地，不能后退。");
                }

                return;
            }

            MoveToRoadSegment(currentRoadSegmentIndex - 1, true);
        }

        public void ScrollToSegment(int roadSegmentIndex, bool smooth)
        {
            if (roadSegmentIndex < 0)
            {
                return;
            }

            if (roadSegmentIndex >= roadSegmentList.Count)
            {
                FinishRoad();
                return;
            }

            MoveToRoadSegment(roadSegmentIndex, smooth);
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
                CircleRoadSegmentData currentRoadSegment = roadSegmentList[currentRoadSegmentIndex];
                Debug.Log("当前位置：" + currentRoadSegmentIndex + " - " + currentRoadSegment.segmentName);
            }
        }

        private void ReadKeyboardInput()
        {
            if (!enableLegacyKeyboardInput)
            {
                return;
            }

            /*
             * Input.GetKeyDown 只在按键按下的那一帧返回 true。
             * 所以它适合“一次按键走一格”的玩法。
             */
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

            /*
             * Time.deltaTime 表示上一帧到这一帧经过了多少秒。
             * 旋转速度乘上它，能让不同帧率下的转动速度更接近。
             */
            float currentAngle = circleRotatingRoot.localEulerAngles.z;
            float newAngle = Mathf.LerpAngle(currentAngle, targetCircleAngle, Time.deltaTime * circleRotateSpeed);
            circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, newAngle);
        }

        private void SetCircleAngleImmediately(float angle)
        {
            circleRotatingRoot.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void RefreshVisibleSegments()
        {
            for (int visibleSlotIndex = 0; visibleSlotIndex < visibleSegmentList.Count; visibleSlotIndex++)
            {
                int roadOffsetFromPlayer = GetRoadOffsetFromPlayer(visibleSlotIndex);
                int roadSegmentIndex = currentRoadSegmentIndex + roadOffsetFromPlayer;
                bool isPlayerSlot = roadOffsetFromPlayer == 0;
                CircleMapSegment visibleSegment = visibleSegmentList[visibleSlotIndex];

                if (roadSegmentIndex < 0)
                {
                    visibleSegment.ShowEmptyLand(spriteFactory.GetSegmentSprite("plant_blue_berry_grass"), "起点后方空地");
                    continue;
                }

                if (roadSegmentIndex >= roadSegmentList.Count)
                {
                    visibleSegment.ShowRoadData(
                        roadSegmentIndex,
                        "通向大地图",
                        spriteFactory.GetSegmentSprite("wall_ruin_corner_ore"),
                        new Color(1f, 0.83f, 0.32f, 1f),
                        false);
                    continue;
                }

                CircleRoadSegmentData roadSegment = roadSegmentList[roadSegmentIndex];
                visibleSegment.ShowRoadData(
                    roadSegmentIndex,
                    roadSegment.segmentName,
                    roadSegment.iconSprite,
                    roadSegment.segmentColor,
                    isPlayerSlot);
            }
        }

        private int GetRoadOffsetFromPlayer(int visibleSlotIndex)
        {
            int playerVisibleSlotIndex = currentRoadSegmentIndex % visibleSegmentCount;
            int offsetFromPlayer = visibleSlotIndex - playerVisibleSlotIndex;

            if (offsetFromPlayer < 0)
            {
                offsetFromPlayer += visibleSegmentCount;
            }

            if (offsetFromPlayer > FrontVisibleSlotCount)
            {
                offsetFromPlayer -= visibleSegmentCount;
            }

            return offsetFromPlayer;
        }

        private void RefreshPlayerPosition()
        {
            float playerY = circleCenterPosition.y - circleRadius + playerBottomOffset;
            playerObject.transform.position = new Vector3(circleCenterPosition.x, playerY, 0f);
        }

        private void FinishRoad()
        {
            hasFinishedRoad = true;

            if (printMoveLog)
            {
                Debug.Log("道路已经走完。这里可以接入真正的大地图切换。");
            }
        }

        private Vector3 GetLocalPositionOnCircle(int visibleSlotIndex)
        {
            float angle = CircleStartAngle + visibleSlotIndex * GetOneSegmentAngle();
            float radians = angle * Mathf.Deg2Rad;
            float segmentRadius = Mathf.Max(0f, circleRadius - segmentInsetFromRing);
            float x = Mathf.Cos(radians) * segmentRadius;
            float y = Mathf.Sin(radians) * segmentRadius;
            return new Vector3(x, y, 0f);
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
            SetSpriteWorldSize(spriteTransform, spriteRenderer, new Vector2(cameraWidth, cameraHeight));
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
