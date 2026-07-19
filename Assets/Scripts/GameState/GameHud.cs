#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CircleWar
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private bool bootstrapMockDataOnStart = true;
        [SerializeField] private bool autoResolveReferences = true;

        [Header("Calendar")]
        [SerializeField] private Text yearText;
        [SerializeField] private Text seasonText;
        [SerializeField] private Text timeText;

        [Header("System")]
        [SerializeField] private Text systemStatusText;

        [Header("Boss")]
        [SerializeField] private Text bossNameText;
        [SerializeField] private Text bossHealthText;
        [SerializeField] private Slider bossHealthFill;

        [Header("Region")]
        [SerializeField] private Text regionStatusText;
        [SerializeField] private Text[] regionFeedTexts;

        [Header("Player")]
        [SerializeField] private Text playerHpText;
        [SerializeField] private Text playerFoodText;
        [SerializeField] private Text playerMaterialsText;
        [SerializeField] private Text playerStatusText;

        [Header("Dialogue")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private Image dialoguePortraitImage;
        [SerializeField] private Text dialogueSpeakerText;
        [SerializeField] private Text dialogueBodyText;
        [SerializeField] private Text[] dialogueChoiceTexts;
        [SerializeField] private Selectable[] dialogueChoiceSelectables;

        [Header("Facility")]
        [SerializeField] private Text facilityProgressText;
        [SerializeField] private FacilitySegmentedProgressBar facilityProgressBar;

        private GameRuntimeData runtimeData = new GameRuntimeData();
        private CompositeUnRegister bindings;

        public GameRuntimeData RuntimeData => runtimeData;
        public GameHudRuntimeData HudData => runtimeData.Hud;

        private void Awake()
        {
            if (autoResolveReferences)
            {
                ResolveMissingReferences();
                EnsureDialogueFallbackWidgets();
            }

            Bind(HudData);
        }

        private void Start()
        {
            if (bootstrapMockDataOnStart)
            {
                runtimeData.LoadCurrentUiMockup();
            }
            else
            {
                runtimeData.RefreshHudFromState();
            }
        }

        private void OnDestroy()
        {
            bindings?.UnRegister();
            bindings = null;
        }

        public void SetRuntimeData(GameRuntimeData newRuntimeData)
        {
            runtimeData = newRuntimeData ?? new GameRuntimeData();
            Bind(HudData);
            runtimeData.RefreshHudFromState();
        }

        public void SetHudRuntimeData(GameHudRuntimeData hudRuntimeData)
        {
            if (hudRuntimeData == null)
            {
                throw new ArgumentNullException(nameof(hudRuntimeData));
            }

            runtimeData.Hud.CopyFrom(hudRuntimeData);
        }

        public void Refresh()
        {
            runtimeData.RefreshHudFromState();
        }

        public void ShowDialogue(DialogueDefinition dialogueDefinition, CharacterDefinition fallbackCharacter = null)
        {
            runtimeData.ShowDialogueDefinition(dialogueDefinition, fallbackCharacter);
        }

        public void ShowDialogueEvent(GameEventDefinition gameEvent, CharacterDefinition fallbackCharacter = null)
        {
            runtimeData.ShowDialogueEvent(gameEvent, fallbackCharacter);
        }

        public void ChooseDialogueOption(int choiceIndex)
        {
            runtimeData.ChooseDialogueOption(choiceIndex);
        }

        public void ChooseFirstDialogueOption()
        {
            ChooseDialogueOption(0);
        }

        public void ChooseSecondDialogueOption()
        {
            ChooseDialogueOption(1);
        }

        public void ChooseThirdDialogueOption()
        {
            ChooseDialogueOption(2);
        }

        private void Bind(GameHudRuntimeData data)
        {
            bindings?.UnRegister();
            bindings = new CompositeUnRegister();

            BindValue(data.Calendar.Year, RefreshCalendar);
            BindValue(data.Calendar.SeasonName, RefreshCalendar);
            BindValue(data.Calendar.LocalHour, RefreshCalendar);
            BindValue(data.Calendar.LocalMinute, RefreshCalendar);

            BindText(systemStatusText, data.SystemStatus.DisplayText);

            BindText(bossNameText, data.Boss.DisplayName);
            BindValue(data.Boss.HealthPercent, RefreshBossHealth);

            BindValue(data.RegionStatus.IsLiveFeed, RefreshRegionStatus);
            BindValue(data.RegionStatus.RegionId, RefreshRegionStatus);
            BindValue(data.RegionStatus.DisplayName, RefreshRegionStatus);
            BindValue(data.RegionStatus.FeedEntries, RefreshRegionFeed);

            BindValue(data.PlayerStats.Hp, RefreshPlayerStats);
            BindValue(data.PlayerStats.MaxHp, RefreshPlayerStats);
            BindValue(data.PlayerStats.Food, RefreshPlayerStats);
            BindValue(data.PlayerStats.Materials, RefreshPlayerStats);
            BindText(playerStatusText, data.PlayerStats.StatusText);

            BindValue(data.Dialogue.IsVisible, RefreshDialogue);
            BindValue(data.Dialogue.CharacterName, RefreshDialogue);
            BindValue(data.Dialogue.Portrait, RefreshDialogue);
            BindValue(data.Dialogue.BodyText, RefreshDialogue);
            BindValue(data.Dialogue.ChoiceCount, RefreshDialogue);
            BindValue(data.Dialogue.ActiveChoiceIndex, RefreshDialogue);
            BindValue(data.Dialogue.Choices, RefreshDialogue);

            BindValue(data.Facility.ProgressPercent, RefreshFacility);
            BindValue(data.Facility.FilledBlockCount, RefreshFacility);
            BindValue(data.Facility.TotalBlockCount, RefreshFacility);
        }

        private void RefreshCalendar()
        {
            HudCalendarRuntimeData calendar = HudData.Calendar;
            SetText(yearText, "Year " + calendar.Year.Value);
            SetText(seasonText, calendar.SeasonName.Value);
            SetText(timeText, string.Format("Local Time {0:00}:{1:00}", calendar.LocalHour.Value, calendar.LocalMinute.Value));
        }

        private void RefreshBossHealth()
        {
            int healthPercent = HudData.Boss.HealthPercent.Value;
            SetText(bossHealthText, healthPercent + "%");

            if (bossHealthFill != null)
            {
                bossHealthFill.minValue = 0f;
                bossHealthFill.maxValue = 1f;
                bossHealthFill.SetValueWithoutNotify(Mathf.Clamp01(healthPercent / 100f));
            }
        }

        private void RefreshRegionStatus()
        {
            HudRegionStatusRuntimeData region = HudData.RegionStatus;
            string label = string.IsNullOrWhiteSpace(region.DisplayName.Value) ? region.RegionId.Value : region.DisplayName.Value;
            SetText(regionStatusText, region.IsLiveFeed.Value ? "LIVE FEED " + label : label);
        }

        private void RefreshRegionFeed()
        {
            IReadOnlyList<HudFeedEntryRuntimeData> feedEntries = HudData.RegionStatus.FeedEntries.Value;
            for (int index = 0; regionFeedTexts != null && index < regionFeedTexts.Length; index++)
            {
                if (regionFeedTexts[index] == null)
                {
                    continue;
                }

                if (feedEntries != null && index < feedEntries.Count)
                {
                    HudFeedEntryRuntimeData entry = feedEntries[index];
                    regionFeedTexts[index].text = string.Format("{0:00}:{1:00} {2}", entry.Hour, entry.Minute, entry.Message);
                }
                else
                {
                    regionFeedTexts[index].text = string.Empty;
                }
            }
        }

        private void RefreshPlayerStats()
        {
            HudPlayerStatsRuntimeData player = HudData.PlayerStats;
            SetText(playerHpText, string.Format("HP {0}/{1}", player.Hp.Value, player.MaxHp.Value));
            SetText(playerFoodText, "Food " + player.Food.Value);
            SetText(playerMaterialsText, "Industry " + player.Materials.Value);
        }

        private void RefreshDialogue()
        {
            HudDialogueRuntimeData dialogue = HudData.Dialogue;
            bool isVisible = dialogue.IsVisible.Value;
            IReadOnlyList<DialogueChoiceRuntimeData> choices = isVisible ? dialogue.Choices.Value : null;
            int choiceCount = isVisible ? dialogue.ChoiceCount.Value : 0;

            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(isVisible);
            }

            SetText(dialogueSpeakerText, isVisible ? dialogue.CharacterName.Value : string.Empty);
            SetText(dialogueBodyText, isVisible ? dialogue.BodyText.Value : string.Empty);
            RefreshDialoguePortrait(isVisible ? dialogue.Portrait.Value : null);
            RefreshDialogueChoices(choices, choiceCount);
        }

        private void RefreshDialoguePortrait(Sprite portrait)
        {
            if (dialoguePortraitImage == null)
            {
                return;
            }

            dialoguePortraitImage.sprite = portrait;
            dialoguePortraitImage.enabled = portrait != null;
        }

        private void RefreshDialogueChoices(IReadOnlyList<DialogueChoiceRuntimeData> choices, int choiceCount)
        {
            for (int index = 0; dialogueChoiceTexts != null && index < dialogueChoiceTexts.Length; index++)
            {
                DialogueChoiceRuntimeData choice = choices != null && index < choiceCount && index < choices.Count ? choices[index] : null;
                Text choiceText = dialogueChoiceTexts[index];
                if (choiceText == null)
                {
                    continue;
                }

                choiceText.gameObject.SetActive(choice != null);
                choiceText.text = choice == null ? string.Empty : choice.Text;
            }

            for (int index = 0; dialogueChoiceSelectables != null && index < dialogueChoiceSelectables.Length; index++)
            {
                DialogueChoiceRuntimeData choice = choices != null && index < choiceCount && index < choices.Count ? choices[index] : null;
                Selectable selectable = dialogueChoiceSelectables[index];
                if (selectable == null)
                {
                    continue;
                }

                selectable.gameObject.SetActive(choice != null);
                selectable.interactable = choice != null && choice.IsEnabled;
            }
        }

        private void RefreshFacility()
        {
            HudFacilityRuntimeData facility = HudData.Facility;
            SetText(facilityProgressText, facility.ProgressPercent.Value.ToString("000") + "%");

            if (facilityProgressBar != null)
            {
                facilityProgressBar.SetProgressPercent(facility.ProgressPercent.Value);
            }
        }

        private void BindText<T>(Text target, IReadonlyBindableProperty<T> property, Func<T, string> formatter = null)
        {
            if (target == null)
            {
                return;
            }

            bindings.Add(target.BindText(property, formatter));
        }

        private void BindValue<T>(IReadonlyBindableProperty<T> property, Action refresh)
        {
            bindings.Add(property.RegisterWithInitValue(_ => refresh.Invoke()));
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private void ResolveMissingReferences()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            Image[] images = GetComponentsInChildren<Image>(true);

            yearText = yearText == null ? FindTextByName(texts, "Year") : yearText;
            seasonText = seasonText == null ? FindTextByName(texts, "Season") : seasonText;
            timeText = timeText == null ? FindTextByName(texts, "Time") : timeText;
            systemStatusText = systemStatusText == null ? FindTextByName(texts, "SYSTEM") : systemStatusText;
            bossNameText = bossNameText == null ? FindTextByName(texts, "BOSS") : bossNameText;
            bossHealthText = bossHealthText == null ? FindTextByName(texts, "BossHealth", "Boss HP", "HP") : bossHealthText;
            regionStatusText = regionStatusText == null ? FindTextByName(texts, "RegionStatus", "Region") : regionStatusText;
            playerHpText = playerHpText == null ? FindTextByName(texts, "PlayerHp", "Player HP", "HP") : playerHpText;
            playerFoodText = playerFoodText == null ? FindTextByName(texts, "Food") : playerFoodText;
            playerMaterialsText = playerMaterialsText == null ? FindTextByName(texts, "Materials") : playerMaterialsText;
            playerStatusText = playerStatusText == null ? FindTextByName(texts, "PlayerStatus", "Status") : playerStatusText;
            dialogueSpeakerText = dialogueSpeakerText == null ? FindTextByName(texts, "Speaker", "SpeakerName", "DialogueSpeaker") : dialogueSpeakerText;
            dialogueBodyText = dialogueBodyText == null ? FindTextByName(texts, "DialogueBody", "Body", "Dialogue") : dialogueBodyText;
            facilityProgressText = facilityProgressText == null ? FindTextByName(texts, "FacilityProgress", "Facility") : facilityProgressText;
            facilityProgressBar = facilityProgressBar == null
                ? GetComponentInChildren<FacilitySegmentedProgressBar>(true)
                : facilityProgressBar;
            dialoguePortraitImage = dialoguePortraitImage == null
                ? FindImageByName(images, "DialoguePortrait", "Portrait", "Avatar", "dialogue_avatar_frame")
                : dialoguePortraitImage;

            if (dialogueRoot == null)
            {
                Transform dialogueTransform = FindChildByName(transform, "right_dialogue_panel_frame");
                dialogueRoot = dialogueTransform == null ? null : dialogueTransform.gameObject;
            }
        }

        private void EnsureDialogueFallbackWidgets()
        {
            if (dialogueRoot == null)
            {
                return;
            }

            if (dialoguePortraitImage == null)
            {
                dialoguePortraitImage = CreateDialogueImage("DialoguePortrait", new Vector2(-78f, 160f), new Vector2(58f, 58f));
            }

            if (dialogueSpeakerText == null)
            {
                dialogueSpeakerText = CreateDialogueText("DialogueSpeaker", new Vector2(28f, 177f), new Vector2(145f, 28f), 18, TextAnchor.MiddleLeft);
            }

            if (dialogueBodyText == null)
            {
                dialogueBodyText = CreateDialogueText("DialogueBody", new Vector2(0f, 78f), new Vector2(205f, 140f), 16, TextAnchor.UpperLeft);
            }

            EnsureDialogueChoiceArrays();

            for (int index = 0; index < DialogueNodeRuntimeData.MaxChoiceCount; index++)
            {
                if (dialogueChoiceTexts[index] == null || dialogueChoiceSelectables[index] == null)
                {
                    CreateDialogueChoice(index);
                }
            }
        }

        private void EnsureDialogueChoiceArrays()
        {
            if (dialogueChoiceTexts == null || dialogueChoiceTexts.Length != DialogueNodeRuntimeData.MaxChoiceCount)
            {
                Array.Resize(ref dialogueChoiceTexts, DialogueNodeRuntimeData.MaxChoiceCount);
            }

            if (dialogueChoiceSelectables == null || dialogueChoiceSelectables.Length != DialogueNodeRuntimeData.MaxChoiceCount)
            {
                Array.Resize(ref dialogueChoiceSelectables, DialogueNodeRuntimeData.MaxChoiceCount);
            }
        }

        private Image CreateDialogueImage(string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(dialogueRoot.transform, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private Text CreateDialogueText(string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            return CreateDialogueText(dialogueRoot.transform, objectName, anchoredPosition, size, fontSize, alignment);
        }

        private void CreateDialogueChoice(int index)
        {
            GameObject choiceObject = new GameObject("DialogueChoice" + (index + 1));
            choiceObject.transform.SetParent(dialogueRoot.transform, false);

            RectTransform choiceTransform = choiceObject.AddComponent<RectTransform>();
            choiceTransform.anchorMin = new Vector2(0.5f, 0.5f);
            choiceTransform.anchorMax = new Vector2(0.5f, 0.5f);
            choiceTransform.pivot = new Vector2(0.5f, 0.5f);
            choiceTransform.anchoredPosition = new Vector2(0f, -80f - index * 46f);
            choiceTransform.sizeDelta = new Vector2(205f, 36f);

            Image choiceBackground = choiceObject.AddComponent<Image>();
            choiceBackground.color = new Color(0f, 0f, 0f, 0.35f);

            Button choiceButton = choiceObject.AddComponent<Button>();
            choiceButton.targetGraphic = choiceBackground;
            int choiceIndex = index;
            choiceButton.onClick.AddListener(() => ChooseDialogueOption(choiceIndex));

            Text choiceText = CreateDialogueText(choiceObject.transform, "Text", Vector2.zero, new Vector2(192f, 28f), 14, TextAnchor.MiddleCenter);
            choiceText.color = new Color(0.028f, 0.974f, 1f, 1f);

            dialogueChoiceTexts[index] = choiceText;
            dialogueChoiceSelectables[index] = choiceButton;
        }

        private Text CreateDialogueText(
            Transform parent,
            string objectName,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = GetFallbackFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Font GetFallbackFont()
        {
            if (yearText != null && yearText.font != null)
            {
                return yearText.font;
            }

            if (systemStatusText != null && systemStatusText.font != null)
            {
                return systemStatusText.font;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Text FindTextByName(Text[] texts, params string[] names)
        {
            if (texts == null || names == null)
            {
                return null;
            }

            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    Text text = texts[textIndex];
                    if (text != null && string.Equals(text.gameObject.name, names[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        private static Image FindImageByName(Image[] images, params string[] names)
        {
            if (images == null || names == null)
            {
                return null;
            }

            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
                {
                    Image image = images[imageIndex];
                    if (image != null && string.Equals(image.gameObject.name, names[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return image;
                    }
                }
            }

            return null;
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

#pragma warning restore 0649
