using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CircleWar.UI
{
    /*
     * 这个脚本负责搭建圆形地图演示。
     * 它不使用 UGUI，也不使用通用 RecyclerView，而是用普通 GameObject、Transform、SpriteRenderer
     * 做出一个更适合课堂讲解的“12 个可见格子 + 30 段真实道路数据”的圆形窗口。
     */
    public sealed class CircleMapRecyclerViewDemo : MonoBehaviour
    {
        private const int PlayerVisibleSlotIndex = 0;
        private const int FrontEntryVisibleSlotIndex = 6;
        private const float CircleStartAngle = -90f;

        [Header("资源路径")]
        [SerializeField, Tooltip("Resources 下的背景图片路径，不要写文件后缀。")]
        private string backgroundSpritePath = "Scence/盐碱地/Gemini_Generated_Image_ckvihockvihockvi";

        [SerializeField, Tooltip("Resources 下的圆圈图片路径，不要写文件后缀。")]
        private string ringSpritePath = "Scence/盐碱地/Gemini_Generated_Image_wwab5bwwab5bwwab (1) (1)";

        [SerializeField, Tooltip("Resources 下的人物 Prefab 路径。")]
        private string playerPrefabPath = "Prefab/frame_006";

        [Header("圆形地图设置")]
        [FormerlySerializedAs("circleAnchoredPosition")]
        [SerializeField, Tooltip("圆形地图在世界坐标中的中心位置。")]
        private Vector2 circleCenterPosition = new Vector2(0f, -0.2f);

        [FormerlySerializedAs("circleDiameter")]
        [SerializeField, Tooltip("圆圈半径，单位是 Unity 世界单位。")]
        private float circleRadius = 2.7f;

        [FormerlySerializedAs("segmentSize")]
        [SerializeField, Tooltip("每个段位图片的世界尺寸。")]
        private Vector2 segmentWorldSize = new Vector2(0.55f, 0.55f);

        [SerializeField, Tooltip("真实道路数据总数。走完这些段后进入大地图。")]
        private int totalRoadSegmentCount = 30;

        [SerializeField, Tooltip("圆圈上同时显示多少个段位。这里固定为 12 更容易对应钟表位置。")]
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

        [SerializeField, Tooltip("是否播放人物 Prefab 自带的 Animator 动画。")]
        private bool animatePlayer = true;

        [Header("交互设置")]
        [SerializeField, Tooltip("勾选后，游戏开始时自动搭建演示场景。")]
        private bool buildOnStart = true;

        [SerializeField, Tooltip("勾选后，可以用 A/D 或左右方向键前进后退。")]
        private bool enableLegacyKeyboardInput = true;

        [Header("教学调试")]
        [SerializeField, Tooltip("勾选后，每次移动都会在 Console 打印当前道路段。")]
        private bool printMoveLog = true;

        private readonly List<CircleRoadSegmentData> roadDataList = new List<CircleRoadSegmentData>();
        private readonly List<CircleMapSegment> visibleSegmentList = new List<CircleMapSegment>();
        private readonly List<Sprite> generatedSpriteList = new List<Sprite>();

        private GameObject createdMapRoot;
        private Transform circleRotatingRoot;
        private Transform segmentRoot;
        private GameObject playerObject;
        private GameObject bigMapObject;

        private Sprite treeSegmentSprite;
        private Sprite emptySegmentSprite;
        private Sprite resourceSegmentSprite;
        private Sprite eventSegmentSprite;
        private Sprite enemySegmentSprite;
        private Sprite factorySegmentSprite;
        private Sprite crisisSegmentSprite;
        private Sprite exitSegmentSprite;

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
            DestroyGeneratedSprites();
        }

        public void Build()
        {
            ClearOldMapObjects();
            CreateIconSprites();
            CreateRoadData();

            createdMapRoot = new GameObject("Circle Map Runtime Root");
            createdMapRoot.transform.SetParent(transform, false);
            createdMapRoot.transform.position = Vector3.zero;

            CreateBackground();
            CreateCircleRoot();
            CreateRing();
            CreateVisibleSegments();
            CreatePlayer();

            currentRoadSegmentIndex = 0;
            hasEnteredBigMap = false;
            targetCircleAngle = 0f;

            RefreshVisibleSegments();
            SetCircleAngleImmediately(targetCircleAngle);
        }

        public void StepForward()
        {
            if (hasEnteredBigMap)
            {
                return;
            }

            int nextRoadSegmentIndex = currentRoadSegmentIndex + 1;
            if (nextRoadSegmentIndex >= roadDataList.Count)
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
            if (roadDataList.Count == 0)
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

            if (index >= roadDataList.Count)
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
                CircleRoadSegmentData currentData = roadDataList[currentRoadSegmentIndex];
                Debug.Log("当前位置：" + currentRoadSegmentIndex + " - " + currentData.segmentName);
            }
        }

        private void ReadKeyboardInput()
        {
            if (!enableLegacyKeyboardInput)
            {
                return;
            }

            /*
             * Input.GetKeyDown 是 Unity 旧输入系统的写法。
             * 它只会在按键刚按下的那一帧返回 true，所以适合“一格一格前进”的操作。
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
             * 用它乘速度，能让转动在不同电脑帧率下尽量保持一致。
             */
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
            for (int visibleSlotIndex = 0; visibleSlotIndex < visibleSegmentList.Count; visibleSlotIndex++)
            {
                int roadSegmentIndex = GetRoadDataIndexForVisibleSlot(visibleSlotIndex);
                CircleMapSegment visibleSegment = visibleSegmentList[visibleSlotIndex];

                if (roadSegmentIndex < 0)
                {
                    visibleSegment.ShowEmptyLand(emptySegmentSprite, "起点后方空地");
                    continue;
                }

                if (roadSegmentIndex >= roadDataList.Count)
                {
                    visibleSegment.ShowRoadData(
                        roadSegmentIndex,
                        "通向大地图",
                        exitSegmentSprite,
                        new Color(1f, 0.83f, 0.32f, 1f),
                        false);
                    continue;
                }

                CircleRoadSegmentData roadData = roadDataList[roadSegmentIndex];
                bool isPlayerSlot = visibleSlotIndex == PlayerVisibleSlotIndex;
                visibleSegment.ShowRoadData(
                    roadSegmentIndex,
                    roadData.segmentName,
                    roadData.iconSprite,
                    roadData.segmentColor,
                    isPlayerSlot);
            }
        }

        private int GetRoadDataIndexForVisibleSlot(int visibleSlotIndex)
        {
            if (visibleSlotIndex == PlayerVisibleSlotIndex)
            {
                return currentRoadSegmentIndex;
            }

            if (visibleSlotIndex <= FrontEntryVisibleSlotIndex)
            {
                return currentRoadSegmentIndex + visibleSlotIndex;
            }

            int behindStepCount = visibleSegmentCount - visibleSlotIndex;
            return currentRoadSegmentIndex - behindStepCount;
        }

        private void CreateRoadData()
        {
            roadDataList.Clear();

            for (int index = 0; index < totalRoadSegmentCount; index++)
            {
                CircleRoadSegmentData roadData = CreateRoadDataByIndex(index);
                roadDataList.Add(roadData);
            }
        }

        private CircleRoadSegmentData CreateRoadDataByIndex(int index)
        {
            if (index <= 6)
            {
                return new CircleRoadSegmentData("树林道路 " + index, treeSegmentSprite, new Color(0.36f, 0.62f, 0.34f, 1f));
            }

            if (index == totalRoadSegmentCount - 1)
            {
                return new CircleRoadSegmentData("年度危机 / 进入大地图", exitSegmentSprite, new Color(1f, 0.82f, 0.28f, 1f));
            }

            int patternIndex = index % 6;
            if (patternIndex == 0)
            {
                return new CircleRoadSegmentData("采集资源点 " + index, resourceSegmentSprite, new Color(0.78f, 0.68f, 0.3f, 1f));
            }

            if (patternIndex == 1)
            {
                return new CircleRoadSegmentData("沿路探索 " + index, eventSegmentSprite, new Color(0.48f, 0.64f, 0.75f, 1f));
            }

            if (patternIndex == 2)
            {
                return new CircleRoadSegmentData("触发事件 " + index, eventSegmentSprite, new Color(0.74f, 0.54f, 0.32f, 1f));
            }

            if (patternIndex == 3)
            {
                return new CircleRoadSegmentData("遭遇敌人 " + index, enemySegmentSprite, new Color(0.75f, 0.3f, 0.26f, 1f));
            }

            if (patternIndex == 4)
            {
                return new CircleRoadSegmentData("建设设施 " + index, factorySegmentSprite, new Color(0.52f, 0.54f, 0.46f, 1f));
            }

            return new CircleRoadSegmentData("推进关系 " + index, crisisSegmentSprite, new Color(0.55f, 0.45f, 0.72f, 1f));
        }

        private void CreateIconSprites()
        {
            treeSegmentSprite = CreateSimpleDiscSprite("Tree Segment Sprite", new Color(0.31f, 0.58f, 0.31f, 1f), new Color(0.1f, 0.18f, 0.1f, 1f));
            emptySegmentSprite = CreateSimpleDiscSprite("Empty Segment Sprite", new Color(0.43f, 0.4f, 0.32f, 0.48f), new Color(0.18f, 0.17f, 0.13f, 0.72f));
            resourceSegmentSprite = CreateSimpleDiscSprite("Resource Segment Sprite", new Color(0.79f, 0.7f, 0.34f, 1f), new Color(0.32f, 0.26f, 0.1f, 1f));
            eventSegmentSprite = CreateSimpleDiscSprite("Event Segment Sprite", new Color(0.45f, 0.63f, 0.76f, 1f), new Color(0.12f, 0.2f, 0.28f, 1f));
            enemySegmentSprite = CreateSimpleDiscSprite("Enemy Segment Sprite", new Color(0.72f, 0.26f, 0.22f, 1f), new Color(0.28f, 0.06f, 0.05f, 1f));
            factorySegmentSprite = CreateSimpleDiscSprite("Factory Segment Sprite", new Color(0.55f, 0.55f, 0.46f, 1f), new Color(0.22f, 0.22f, 0.18f, 1f));
            crisisSegmentSprite = CreateSimpleDiscSprite("Crisis Segment Sprite", new Color(0.5f, 0.42f, 0.7f, 1f), new Color(0.18f, 0.13f, 0.3f, 1f));
            exitSegmentSprite = CreateSimpleDiscSprite("Exit Segment Sprite", new Color(0.92f, 0.76f, 0.28f, 1f), new Color(0.38f, 0.28f, 0.06f, 1f));
        }

        private void CreateBackground()
        {
            Sprite backgroundSprite = LoadFirstSprite(backgroundSpritePath);
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(createdMapRoot.transform, false);
            backgroundObject.transform.position = new Vector3(0f, 0f, 5f);

            SpriteRenderer backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.sortingOrder = -20;

            FitSpriteToCamera(backgroundObject.transform, backgroundRenderer);
        }

        private void CreateCircleRoot()
        {
            GameObject circleRootObject = new GameObject("Circle Rotating Root");
            circleRootObject.transform.SetParent(createdMapRoot.transform, false);
            circleRootObject.transform.position = new Vector3(circleCenterPosition.x, circleCenterPosition.y, 0f);
            circleRotatingRoot = circleRootObject.transform;

            GameObject segmentRootObject = new GameObject("Visible Segment Root");
            segmentRootObject.transform.SetParent(circleRotatingRoot, false);
            segmentRootObject.transform.localPosition = Vector3.zero;
            segmentRoot = segmentRootObject.transform;
        }

        private void CreateRing()
        {
            Sprite ringSprite = LoadFirstSprite(ringSpritePath);
            GameObject ringObject = new GameObject("Circle Ring");
            ringObject.transform.SetParent(circleRotatingRoot, false);
            ringObject.transform.localPosition = Vector3.zero;

            SpriteRenderer ringRenderer = ringObject.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = ringSprite;
            ringRenderer.sortingOrder = -5;

            float ringDiameter = circleRadius * 2f;
            SetSpriteWorldSize(ringObject.transform, ringRenderer, new Vector2(ringDiameter, ringDiameter));
        }

        private void CreateVisibleSegments()
        {
            visibleSegmentList.Clear();

            for (int visibleSlotIndex = 0; visibleSlotIndex < visibleSegmentCount; visibleSlotIndex++)
            {
                GameObject segmentObject = new GameObject("Visible Segment " + visibleSlotIndex);
                segmentObject.transform.SetParent(segmentRoot, false);
                segmentObject.transform.localPosition = GetLocalPositionOnCircle(visibleSlotIndex);

                SpriteRenderer segmentRenderer = segmentObject.AddComponent<SpriteRenderer>();
                segmentRenderer.sortingOrder = 5;

                CircleMapSegment segment = segmentObject.AddComponent<CircleMapSegment>();
                segment.Setup(segmentRenderer, segmentWorldSize);

                visibleSegmentList.Add(segment);
            }
        }

        private void CreatePlayer()
        {
            GameObject playerPrefab = Resources.Load<GameObject>(playerPrefabPath);
            if (playerPrefab != null)
            {
                /*
                 * Instantiate 会复制一个 Prefab 到场景里。
                 * 这里复制人物，是为了保留 Prefab 自带的 SpriteRenderer 和 Animator。
                 */
                playerObject = Instantiate(playerPrefab, createdMapRoot.transform);
                playerObject.name = "Fixed Player";
            }
            else
            {
                playerObject = new GameObject("Fixed Player");
                playerObject.transform.SetParent(createdMapRoot.transform, false);

                SpriteRenderer fallbackRenderer = playerObject.AddComponent<SpriteRenderer>();
                fallbackRenderer.sprite = treeSegmentSprite;
            }

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

            RefreshPlayerPosition();
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

            if (bigMapObject == null)
            {
                CreateBigMapPlaceholder();
            }

            bigMapObject.SetActive(true);

            if (printMoveLog)
            {
                Debug.Log("30 段道路已经走完，切换到大地图。");
            }
        }

        private void CreateBigMapPlaceholder()
        {
            bigMapObject = new GameObject("Big Map Placeholder");
            bigMapObject.transform.SetParent(createdMapRoot.transform, false);
            bigMapObject.transform.position = new Vector3(0f, 0f, 0f);

            TextMesh textMesh = bigMapObject.AddComponent<TextMesh>();
            textMesh.text = "大地图\n30 段探索完成";
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.35f;
            textMesh.color = new Color(0.12f, 0.16f, 0.12f, 1f);

            MeshRenderer meshRenderer = bigMapObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 30;
            }
        }

        private Vector3 GetLocalPositionOnCircle(int visibleSlotIndex)
        {
            float angle = CircleStartAngle + visibleSlotIndex * GetOneSegmentAngle();
            float radians = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians) * circleRadius;
            float y = Mathf.Sin(radians) * circleRadius;
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

        private Sprite LoadFirstSprite(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            return null;
        }

        private Sprite CreateSimpleDiscSprite(string spriteName, Color fillColor, Color borderColor)
        {
            int textureSize = 96;
            float center = (textureSize - 1) * 0.5f;
            float outerRadius = textureSize * 0.5f - 2f;
            float innerRadius = outerRadius - 7f;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.name = spriteName + " Texture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color pixelColor = Color.clear;

                    if (distance <= outerRadius)
                    {
                        if (distance >= innerRadius)
                        {
                            pixelColor = borderColor;
                        }
                        else
                        {
                            pixelColor = fillColor;
                        }
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = spriteName;
            generatedSpriteList.Add(sprite);
            return sprite;
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

        private void ClearOldMapObjects()
        {
            if (createdMapRoot != null)
            {
                /*
                 * Destroy 是运行时删除物体，DestroyImmediate 是编辑器里立刻删除物体。
                 * 这里区分两种情况，方便老师在编辑器中手动调用 Build 时也不会留下重复物体。
                 */
                if (Application.isPlaying)
                {
                    Destroy(createdMapRoot);
                }
                else
                {
                    DestroyImmediate(createdMapRoot);
                }
            }

            createdMapRoot = null;
            circleRotatingRoot = null;
            segmentRoot = null;
            playerObject = null;
            bigMapObject = null;
            visibleSegmentList.Clear();

            DestroyGeneratedSprites();
        }

        private void DestroyGeneratedSprites()
        {
            for (int i = 0; i < generatedSpriteList.Count; i++)
            {
                Sprite sprite = generatedSpriteList[i];
                if (sprite == null)
                {
                    continue;
                }

                Texture2D texture = sprite.texture;

                /*
                 * Destroy 适合游戏运行时使用。
                 * DestroyImmediate 适合编辑器模式下立即清理，比如老师在 Inspector 里手动调用 Build。
                 */
                if (Application.isPlaying)
                {
                    Destroy(sprite);
                    Destroy(texture);
                }
                else
                {
                    DestroyImmediate(sprite);
                    DestroyImmediate(texture);
                }
            }

            generatedSpriteList.Clear();
        }

        private sealed class CircleRoadSegmentData
        {
            public string segmentName;
            public Sprite iconSprite;
            public Color segmentColor;

            public CircleRoadSegmentData(string newSegmentName, Sprite newIconSprite, Color newSegmentColor)
            {
                segmentName = newSegmentName;
                iconSprite = newIconSprite;
                segmentColor = newSegmentColor;
            }
        }
    }
}
