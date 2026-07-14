using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class GameRuntimeData
    {
        private const int MaxRegionFeedEntryCount = 2;
        private const int FallResourceFacilityProgressPercent = 5;
        private const int FallResourceFacilityFilledBlocks = 1;
        private const string FallSeasonId = "fall";

        public const string CalendarYearValueId = "calendar_year";
        public const string CalendarLocalHourValueId = "calendar_local_hour";
        public const string CalendarLocalMinuteValueId = "calendar_local_minute";
        public const string PlayerHpValueId = "player_hp";
        public const string PlayerMaxHpValueId = "player_max_hp";
        public const string PlayerStatusTextValueId = "player_status_text";
        public const string FacilityProgressValueId = "facility_progress_percent";
        public const string FacilityFilledBlockCountValueId = "facility_filled_block_count";
        public const string FacilityTotalBlockCountValueId = "facility_total_block_count";

        public readonly GameState State = new GameState();
        public readonly GameHudRuntimeData Hud = new GameHudRuntimeData();

        public event Action NewRunStarted;
        public int RunRevision { get; private set; }

        private readonly Dictionary<string, DialogueNodeRuntimeData> activeDialogueNodes =
            new Dictionary<string, DialogueNodeRuntimeData>();
        private string foodResourceId = "food";
        private string materialsResourceId = "industry";
        private string activeBossId = string.Empty;
        private string activeBossDisplayName = string.Empty;
        private string activeFacilityId = string.Empty;
        private string activePlayerStatusId = string.Empty;
        private string activePlayerStatusText = string.Empty;
        private string activeRegionId = string.Empty;
        private string activeRegionDisplayName = string.Empty;

        public string FoodResourceId
        {
            get => foodResourceId;
            set => foodResourceId = string.IsNullOrWhiteSpace(value) ? "food" : value;
        }

        public string MaterialsResourceId
        {
            get => materialsResourceId;
            set => materialsResourceId = string.IsNullOrWhiteSpace(value) ? "industry" : value;
        }

        public void StartNewRun(string runId = null)
        {
            RunRevision++;
            State.StartNewRun(runId);
            activeBossId = string.Empty;
            activeBossDisplayName = string.Empty;
            activeFacilityId = string.Empty;
            activePlayerStatusId = string.Empty;
            activePlayerStatusText = string.Empty;
            activeRegionId = string.Empty;
            activeRegionDisplayName = string.Empty;
            activeDialogueNodes.Clear();

            SetCalendar(1, "spring", "春", 8, 0);
            SetPlayerStats(100, 100, 0, 0, string.Empty, string.Empty);
            SetSystemStatus("boot", "SYSTEM BOOTING...", 0);
            SetBoss(string.Empty, string.Empty, 0);
            SetRegionStatus(string.Empty, string.Empty, false, null);
            ClearDialogue();
            SetFacilityProgress(string.Empty, 0, 0, 1);
            NewRunStarted?.Invoke();
        }

        public void LoadCurrentUiMockup()
        {
            StartNewRun("mock_runtime");
            SetCalendar(1, "spring", "春", 8, 0);
            SetSystemStatus("online", "SYSTEM ONLINE...", 4);
            SetBoss(string.Empty, string.Empty, 0);
            SetRegionStatus(
                "salt_dust_plain",
                "盐尘平原",
                true,
                new[]
                {
                    new HudFeedEntryRuntimeData(7, 59, "春季盐壳正在融化。"),
                    new HudFeedEntryRuntimeData(8, 0, "盐尘平原通行风险上升。")
                });
            SetPlayerStats(80, 100, 120, 85, "on_the_move", "ON THE MOVE");
            ClearDialogue();
            SetFacilityProgress("main_facility", 42, 5, 12);
        }

        public void SetCalendar(int year, string seasonId, string seasonName, int localHour, int localMinute)
        {
            int safeYear = Math.Max(1, year);
            int safeHour = Clamp(localHour, 0, 23);
            int safeMinute = Clamp(localMinute, 0, 59);

            State.SetCalendar(safeYear, seasonId ?? string.Empty);
            State.SetCustomValue(CalendarYearValueId, safeYear);
            State.SetCustomValue(CalendarLocalHourValueId, safeHour);
            State.SetCustomValue(CalendarLocalMinuteValueId, safeMinute);
            Hud.Calendar.Set(safeYear, seasonId, seasonName, safeHour, safeMinute);
        }

        public void AdvanceTime(int minuteDelta)
        {
            int totalMinutes = Hud.Calendar.LocalHour.Value * 60 + Hud.Calendar.LocalMinute.Value + minuteDelta;
            while (totalMinutes < 0)
            {
                totalMinutes += 24 * 60;
            }

            totalMinutes %= 24 * 60;
            SetCalendar(
                Hud.Calendar.Year.Value,
                Hud.Calendar.SeasonId.Value,
                Hud.Calendar.SeasonName.Value,
                totalMinutes / 60,
                totalMinutes % 60);
        }

        public void SetSeasonContext(
            SeasonDefinition season,
            RegionDefinition region,
            float roadPosition)
        {
            if (season == null)
            {
                return;
            }

            SetCalendar(
                Math.Max(1, Hud.Calendar.Year.Value),
                season.DefinitionId,
                season.DisplayName,
                Hud.Calendar.LocalHour.Value,
                Hud.Calendar.LocalMinute.Value);

            string regionId = region != null && !string.IsNullOrWhiteSpace(region.DefinitionId)
                ? region.DefinitionId
                : season.DefinitionId + "_region";
            string regionDisplayName = region != null
                ? region.DisplayName
                : season.DisplayName;
            IReadOnlyList<HudFeedEntryRuntimeData> feedEntries = Hud.RegionStatus.FeedEntries.Value;

            activeRegionId = regionId;
            activeRegionDisplayName = regionDisplayName;
            State.SetLocation(activeRegionId, Math.Max(0f, roadPosition));
            Hud.RegionStatus.Set(activeRegionId, activeRegionDisplayName, true, feedEntries);
        }

        public void SetRoadPosition(float roadPosition)
        {
            if (string.IsNullOrWhiteSpace(activeRegionId))
            {
                return;
            }

            State.SetLocation(activeRegionId, Math.Max(0f, roadPosition));
        }

        public void SetSystemStatus(string statusId, string displayText, int signalPipCount)
        {
            if (!string.IsNullOrWhiteSpace(statusId))
            {
                State.SetFlag(statusId, true);
            }

            Hud.SystemStatus.Set(statusId, displayText, signalPipCount);
        }

        public void SetBoss(string bossId, string displayName, int healthPercent)
        {
            activeBossId = bossId ?? string.Empty;
            activeBossDisplayName = displayName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(activeBossId))
            {
                BossProgressState progress = State.GetBossProgress(activeBossId);
                if (progress == null || !progress.IsStarted)
                {
                    State.StartBoss(activeBossId, 100);
                    progress = State.GetBossProgress(activeBossId);
                }

                int maxHealth = progress != null ? Math.Max(1, progress.MaxHealth) : 100;
                int currentHealth = Clamp(
                    (int)Math.Round(maxHealth * Clamp(healthPercent, 0, 100) / 100f),
                    0,
                    maxHealth);
                State.SetBossHealth(activeBossId, currentHealth);
            }

            Hud.Boss.Set(activeBossId, activeBossDisplayName, healthPercent);
        }

        public void SetBossHealthPercent(int healthPercent)
        {
            SetBoss(activeBossId, activeBossDisplayName, healthPercent);
        }

        public bool StartBossEncounter(
            BossDefinition boss,
            string fallbackBossId,
            string fallbackDisplayName)
        {
            return StartBossEncounter(boss, fallbackBossId, fallbackDisplayName, out _);
        }

        public bool StartBossEncounter(
            BossDefinition boss,
            string fallbackBossId,
            string fallbackDisplayName,
            out CombatEnemyProgressBinding progressBinding)
        {
            progressBinding = null;
            string bossId = boss != null ? boss.DefinitionId : fallbackBossId;
            if (string.IsNullOrWhiteSpace(bossId))
            {
                return false;
            }

            string displayName = boss != null ? boss.DisplayName : fallbackDisplayName;
            int maxHealth = boss != null ? Math.Max(1, boss.MaxHealth) : 100;
            BossProgressState progress = State.GetBossProgress(bossId);
            if (progress == null || !progress.IsStarted)
            {
                State.StartBoss(bossId, maxHealth);
                progress = State.GetBossProgress(bossId);
            }

            activeBossId = bossId;
            activeBossDisplayName = string.IsNullOrWhiteSpace(displayName) ? bossId : displayName;
            int healthPercent = progress == null || progress.MaxHealth <= 0
                ? 100
                : Clamp((int)Math.Round((float)progress.CurrentHealth / progress.MaxHealth * 100f), 0, 100);
            Hud.Boss.Set(activeBossId, activeBossDisplayName, healthPercent);

            int currentHealth = progress != null ? progress.CurrentHealth : maxHealth;
            int resolvedMaxHealth = progress != null ? progress.MaxHealth : maxHealth;
            bool isAlreadyDefeated = progress != null && progress.IsDefeated;
            string resolvedBossDisplayName = activeBossDisplayName;
            progressBinding = new CombatEnemyProgressBinding(
                resolvedMaxHealth,
                currentHealth,
                (updatedHealth, updatedMaxHealth) => UpdateBossCombatProgress(
                    bossId,
                    resolvedBossDisplayName,
                    updatedHealth,
                    updatedMaxHealth),
                () => CompleteBossEncounter(bossId, resolvedBossDisplayName),
                isAlreadyDefeated);
            return true;
        }

        private void UpdateBossCombatProgress(
            string bossId,
            string displayName,
            int currentHealth,
            int maxHealth)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                return;
            }

            int safeMaxHealth = Math.Max(1, maxHealth);
            BossProgressState progress = State.GetBossProgress(bossId);
            if (progress == null || !progress.IsStarted)
            {
                State.StartBoss(bossId, safeMaxHealth);
            }

            int safeCurrentHealth = Clamp(currentHealth, 0, safeMaxHealth);
            State.SetBossHealth(bossId, safeCurrentHealth);
            activeBossId = bossId;
            activeBossDisplayName = string.IsNullOrWhiteSpace(displayName) ? bossId : displayName;
            int healthPercent = Clamp(
                (int)Math.Round((float)safeCurrentHealth / safeMaxHealth * 100f),
                0,
                100);
            Hud.Boss.Set(activeBossId, activeBossDisplayName, healthPercent);
        }

        private void CompleteBossEncounter(string bossId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                return;
            }

            State.MarkBossDefeated(bossId);
            activeBossId = bossId;
            activeBossDisplayName = string.IsNullOrWhiteSpace(displayName) ? bossId : displayName;
            Hud.Boss.Set(activeBossId, activeBossDisplayName, 0);
        }

        public void SetRegionStatus(string regionId, string displayName, bool isLiveFeed, IEnumerable<HudFeedEntryRuntimeData> feedEntries)
        {
            activeRegionId = regionId ?? string.Empty;
            activeRegionDisplayName = displayName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(activeRegionId))
            {
                State.SetLocation(activeRegionId, State.CurrentRoadPosition);
            }

            Hud.RegionStatus.Set(activeRegionId, activeRegionDisplayName, isLiveFeed, feedEntries);
        }

        public void PushRegionFeed(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            List<HudFeedEntryRuntimeData> entries = new List<HudFeedEntryRuntimeData>(Hud.RegionStatus.FeedEntries.Value);
            entries.Add(new HudFeedEntryRuntimeData(Hud.Calendar.LocalHour.Value, Hud.Calendar.LocalMinute.Value, message));

            while (entries.Count > MaxRegionFeedEntryCount)
            {
                entries.RemoveAt(0);
            }

            SetRegionStatus(activeRegionId, activeRegionDisplayName, true, entries);
        }

        public bool IsInteractionCompleted(GameDefinition definition)
        {
            return definition != null &&
                   IsInteractionCompleted(definition.DefinitionId);
        }

        public bool IsInteractionCompleted(string interactionId)
        {
            return !string.IsNullOrWhiteSpace(interactionId) &&
                   State.IsEventCompleted(interactionId);
        }

        public bool TryCollectRoadSegmentResource(string segmentId, IEnumerable<ResourceAmount> rewards)
        {
            if (string.IsNullOrWhiteSpace(segmentId) || IsInteractionCompleted(segmentId))
            {
                return false;
            }

            if (rewards != null)
            {
                foreach (ResourceAmount reward in rewards)
                {
                    if (reward == null || string.IsNullOrWhiteSpace(reward.ResourceId) || reward.Amount == 0)
                    {
                        continue;
                    }

                    State.AddResource(reward.ResourceId, reward.Amount);
                }
            }

            State.MarkEventCompleted(segmentId);
            AdvanceFacilityFromFallResourceCollection();
            RefreshHudFromState();
            return true;
        }

        private void AdvanceFacilityFromFallResourceCollection()
        {
            if (!string.Equals(State.CurrentSeasonId, FallSeasonId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int totalBlockCount = Math.Max(1, State.GetCustomValue(FacilityTotalBlockCountValueId));
            int progressPercent = Math.Min(
                100,
                State.GetCustomValue(FacilityProgressValueId) + FallResourceFacilityProgressPercent);
            int filledBlockCount = Math.Min(
                totalBlockCount,
                State.GetCustomValue(FacilityFilledBlockCountValueId) + FallResourceFacilityFilledBlocks);

            SetFacilityProgress(
                activeFacilityId,
                progressPercent,
                filledBlockCount,
                totalBlockCount);
        }

        public void SetPlayerStats(int hp, int maxHp, int food, int materials, string statusId, string statusText)
        {
            int safeMaxHp = Math.Max(1, maxHp);
            int safeHp = Clamp(hp, 0, safeMaxHp);

            activePlayerStatusId = statusId ?? string.Empty;
            activePlayerStatusText = statusText ?? string.Empty;
            State.SetCustomValue(PlayerHpValueId, safeHp);
            State.SetCustomValue(PlayerMaxHpValueId, safeMaxHp);
            State.SetCustomValue(PlayerStatusTextValueId, StableTextHash(activePlayerStatusText));
            State.SetResourceAmount(FoodResourceId, Math.Max(0, food));
            State.SetResourceAmount(MaterialsResourceId, Math.Max(0, materials));
            Hud.PlayerStats.Set(safeHp, safeMaxHp, food, materials, activePlayerStatusId, activePlayerStatusText);
        }

        public void SetFacilityProgress(string facilityId, int progressPercent, int filledBlockCount, int totalBlockCount)
        {
            activeFacilityId = facilityId ?? string.Empty;
            State.SetCustomValue(FacilityProgressValueId, Clamp(progressPercent, 0, 100));
            State.SetCustomValue(FacilityFilledBlockCountValueId, Math.Max(0, filledBlockCount));
            State.SetCustomValue(FacilityTotalBlockCountValueId, Math.Max(1, totalBlockCount));

            if (!string.IsNullOrWhiteSpace(activeFacilityId) && progressPercent >= 100)
            {
                State.MarkFacilityModuleBuilt(activeFacilityId);
            }

            Hud.Facility.Set(activeFacilityId, progressPercent, filledBlockCount, totalBlockCount);
        }

        public void ShowDialogueEvent(GameEventDefinition gameEvent)
        {
            ShowDialogueEvent(gameEvent, null);
        }

        public void ShowDialogueEvent(GameEventDefinition gameEvent, CharacterDefinition character)
        {
            activeDialogueNodes.Clear();

            if (gameEvent == null)
            {
                ClearDialogue();
                return;
            }

            List<DialogueChoiceRuntimeData> choices = new List<DialogueChoiceRuntimeData>();
            IReadOnlyList<GameEventChoiceDefinition> eventChoices = gameEvent.Choices;
            for (int index = 0; eventChoices != null && index < eventChoices.Count && choices.Count < DialogueNodeRuntimeData.MaxChoiceCount; index++)
            {
                GameEventChoiceDefinition choice = eventChoices[index];
                if (choice == null)
                {
                    continue;
                }

                choices.Add(new DialogueChoiceRuntimeData(
                    choice.ChoiceText,
                    CreateDialogueResultFromEventChoice(gameEvent, choice),
                    GameStateRuleRunner.AreConditionsMet(State, choice.Conditions),
                    choice.ChoiceId,
                    gameEvent.DefinitionId,
                    choice.ConsumeInteractionOnSelect));
            }

            if (choices.Count == 0)
            {
                choices.Add(new DialogueChoiceRuntimeData(
                    "CONTINUE",
                    DialogueChoiceResultRuntimeData.IncrementPlotInt(gameEvent.DefinitionId),
                    true,
                    "continue",
                    gameEvent.DefinitionId,
                    true));
            }

            DialogueNodeRuntimeData node = character == null
                ? new DialogueNodeRuntimeData(
                    string.Empty,
                    (UnityEngine.Sprite)null,
                    gameEvent.BodyText,
                    choices,
                    gameEvent.DefinitionId)
                : new DialogueNodeRuntimeData(
                    character,
                    gameEvent.BodyText,
                    choices,
                    gameEvent.DefinitionId);

            ShowDialogueNode(node);
        }

        public void ShowDialogueDefinition(DialogueDefinition dialogueDefinition, CharacterDefinition fallbackCharacter = null)
        {
            activeDialogueNodes.Clear();

            if (dialogueDefinition == null || dialogueDefinition.Nodes == null || dialogueDefinition.Nodes.Count == 0)
            {
                ClearDialogue();
                return;
            }

            List<DialogueNodeRuntimeData> runtimeNodes = new List<DialogueNodeRuntimeData>();
            DialogueNodeRuntimeData startNode = null;
            DialogueNodeDefinition startNodeDefinition = dialogueDefinition.StartNode;

            for (int index = 0; index < dialogueDefinition.Nodes.Count; index++)
            {
                DialogueNodeDefinition nodeDefinition = dialogueDefinition.Nodes[index];
                if (nodeDefinition == null)
                {
                    continue;
                }

                DialogueNodeRuntimeData runtimeNode = CreateRuntimeDialogueNode(
                    dialogueDefinition,
                    nodeDefinition,
                    fallbackCharacter);
                runtimeNodes.Add(runtimeNode);

                if (!string.IsNullOrWhiteSpace(runtimeNode.NodeId))
                {
                    activeDialogueNodes[runtimeNode.NodeId] = runtimeNode;
                }

                if (ReferenceEquals(nodeDefinition, startNodeDefinition))
                {
                    startNode = runtimeNode;
                }
            }

            if (startNode == null && runtimeNodes.Count > 0)
            {
                startNode = runtimeNodes[0];
            }

            Hud.Dialogue.ShowNode(startNode);
        }

        public void ShowDialogueNode(DialogueNodeRuntimeData dialogueNode)
        {
            activeDialogueNodes.Clear();
            Hud.Dialogue.ShowNode(dialogueNode);
        }

        public bool ChooseDialogueOption(int choiceIndex)
        {
            IReadOnlyList<DialogueChoiceRuntimeData> choices = Hud.Dialogue.Choices.Value;
            if (!Hud.Dialogue.IsVisible.Value || choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                return false;
            }

            DialogueChoiceRuntimeData choice = choices[choiceIndex];
            if (choice == null || !choice.IsEnabled)
            {
                return false;
            }

            Hud.Dialogue.ActiveChoiceIndex.Value = choiceIndex;
            bool resultApplied = ApplyDialogueChoiceResult(choice.Result);
            if (!resultApplied)
            {
                return false;
            }

            if (choice.ConsumeInteractionOnSelect && !string.IsNullOrWhiteSpace(choice.SourceInteractionId))
            {
                State.MarkEventCompleted(choice.SourceInteractionId, choice.ChoiceId);
            }

            return true;
        }

        public void ClearDialogue()
        {
            activeDialogueNodes.Clear();
            Hud.Dialogue.Clear();
        }

        public void RefreshHudFromState()
        {
            int year = Math.Max(1, State.GetCustomValue(CalendarYearValueId));
            if (year <= 1 && State.CurrentDay > 1)
            {
                year = State.CurrentDay;
            }

            Hud.Calendar.Set(
                year,
                State.CurrentSeasonId,
                Hud.Calendar.SeasonName.Value,
                State.GetCustomValue(CalendarLocalHourValueId),
                State.GetCustomValue(CalendarLocalMinuteValueId));

            Hud.PlayerStats.Set(
                State.GetCustomValue(PlayerHpValueId),
                Math.Max(1, State.GetCustomValue(PlayerMaxHpValueId)),
                State.GetResourceAmount(FoodResourceId),
                State.GetResourceAmount(MaterialsResourceId),
                activePlayerStatusId,
                activePlayerStatusText);

            if (!string.IsNullOrWhiteSpace(activeBossId))
            {
                BossProgressState bossProgress = State.GetBossProgress(activeBossId);
                int healthPercent = 0;

                if (bossProgress != null && bossProgress.MaxHealth > 0)
                {
                    healthPercent = Clamp((int)Math.Round((float)bossProgress.CurrentHealth / bossProgress.MaxHealth * 100f), 0, 100);
                }

                Hud.Boss.Set(activeBossId, activeBossDisplayName, healthPercent);
            }

            Hud.Facility.Set(
                activeFacilityId,
                State.GetCustomValue(FacilityProgressValueId),
                State.GetCustomValue(FacilityFilledBlockCountValueId),
                Math.Max(1, State.GetCustomValue(FacilityTotalBlockCountValueId)));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private static int StableTextHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                for (int index = 0; index < value.Length; index++)
                {
                    hash = hash * 31 + value[index];
                }

                return hash;
            }
        }

        private bool ApplyDialogueChoiceResult(DialogueChoiceResultRuntimeData result)
        {
            if (result == null)
            {
                ClearDialogue();
                return true;
            }

            switch (result.ResultType)
            {
                case DialogueChoiceResultType.NextDialogueNode:
                    if (result.NextDialogueNode != null)
                    {
                        Hud.Dialogue.ShowNode(result.NextDialogueNode);
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(result.NextDialogueNodeId) ||
                        !activeDialogueNodes.TryGetValue(result.NextDialogueNodeId, out DialogueNodeRuntimeData nextNode))
                    {
                        return false;
                    }

                    Hud.Dialogue.ShowNode(nextNode);
                    return true;
                case DialogueChoiceResultType.AddResource:
                    if (string.IsNullOrWhiteSpace(result.ResourceId))
                    {
                        return false;
                    }

                    State.AddResource(result.ResourceId, result.ResourceAmount);
                    RefreshHudFromState();
                    ClearDialogue();
                    return true;
                case DialogueChoiceResultType.IncrementPlotInt:
                    if (string.IsNullOrWhiteSpace(result.PlotIntId))
                    {
                        return false;
                    }

                    State.AddCustomValue(result.PlotIntId, 1);
                    RefreshHudFromState();
                    ClearDialogue();
                    return true;
                case DialogueChoiceResultType.ApplyEventChoice:
                    return ApplyEventChoiceResult(result);
                case DialogueChoiceResultType.EndDialogue:
                default:
                    ClearDialogue();
                    return true;
            }
        }

        private static DialogueChoiceResultRuntimeData CreateDialogueResultFromEventChoice(
            GameEventDefinition gameEvent,
            GameEventChoiceDefinition choice)
        {
            return DialogueChoiceResultRuntimeData.ApplyEventChoice(gameEvent, choice);
        }

        private bool ApplyEventChoiceResult(DialogueChoiceResultRuntimeData result)
        {
            GameEventDefinition eventDefinition = result.EventDefinition;
            GameEventChoiceDefinition eventChoice = result.EventChoice;
            if (eventDefinition == null || eventChoice == null)
            {
                return false;
            }

            ApplyEventEffects(eventDefinition.AutomaticResults);
            ApplyEventEffects(eventChoice.Results);

            ClampPlayerHealthToValidRange();
            RefreshHudFromState();
            ClearDialogue();
            return true;
        }

        private void ApplyEventEffects(IEnumerable<GameEffect> effects)
        {
            if (effects == null)
            {
                return;
            }

            foreach (GameEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (effect.EffectType == GameEffectType.PushRegionFeed)
                {
                    PushRegionFeed(effect.TextValue);
                    continue;
                }

                GameStateRuleRunner.ApplyEffect(State, effect);
            }
        }

        private void ClampPlayerHealthToValidRange()
        {
            int maxHp = Math.Max(1, State.GetCustomValue(PlayerMaxHpValueId));
            int hp = Clamp(State.GetCustomValue(PlayerHpValueId), 0, maxHp);
            State.SetCustomValue(PlayerHpValueId, hp);
        }

        private static DialogueNodeRuntimeData CreateRuntimeDialogueNode(
            DialogueDefinition dialogueDefinition,
            DialogueNodeDefinition nodeDefinition,
            CharacterDefinition fallbackCharacter)
        {
            List<DialogueChoiceRuntimeData> choices = CreateRuntimeDialogueChoices(dialogueDefinition, nodeDefinition);
            CharacterDefinition character = nodeDefinition.Character ?? fallbackCharacter;
            string speakerName = nodeDefinition.SpeakerName;
            Sprite portrait = nodeDefinition.Portrait;

            if (character != null)
            {
                if (string.IsNullOrWhiteSpace(speakerName))
                {
                    speakerName = character.CharacterName;
                }

                if (portrait == null)
                {
                    portrait = character.Portrait;
                }
            }

            return new DialogueNodeRuntimeData(
                speakerName,
                portrait,
                nodeDefinition.BodyText,
                choices,
                nodeDefinition.NodeId);
        }

        private static List<DialogueChoiceRuntimeData> CreateRuntimeDialogueChoices(
            DialogueDefinition dialogueDefinition,
            DialogueNodeDefinition nodeDefinition)
        {
            List<DialogueChoiceRuntimeData> choices = new List<DialogueChoiceRuntimeData>();
            IReadOnlyList<DialogueChoiceDefinition> choiceDefinitions = nodeDefinition.Choices;

            for (int index = 0; choiceDefinitions != null &&
                                index < choiceDefinitions.Count &&
                                choices.Count < DialogueNodeRuntimeData.MaxChoiceCount; index++)
            {
                DialogueChoiceDefinition choiceDefinition = choiceDefinitions[index];
                if (choiceDefinition == null)
                {
                    continue;
                }

                choices.Add(new DialogueChoiceRuntimeData(
                    choiceDefinition.ChoiceText,
                    CreateDialogueResultFromDialogueChoice(dialogueDefinition, choiceDefinition),
                    true,
                    choiceDefinition.ChoiceId,
                    dialogueDefinition.DefinitionId,
                    choiceDefinition.ConsumeInteractionOnSelect));
            }

            if (choices.Count == 0)
            {
                choices.Add(new DialogueChoiceRuntimeData("继续", DialogueChoiceResultRuntimeData.EndDialogue()));
            }

            return choices;
        }

        private static DialogueChoiceResultRuntimeData CreateDialogueResultFromDialogueChoice(
            DialogueDefinition dialogueDefinition,
            DialogueChoiceDefinition choiceDefinition)
        {
            switch (choiceDefinition.ResultType)
            {
                case DialogueChoiceResultType.NextDialogueNode:
                    return string.IsNullOrWhiteSpace(choiceDefinition.NextNodeId)
                        ? DialogueChoiceResultRuntimeData.EndDialogue()
                        : DialogueChoiceResultRuntimeData.NextNode(choiceDefinition.NextNodeId);
                case DialogueChoiceResultType.AddResource:
                    return string.IsNullOrWhiteSpace(choiceDefinition.ResourceId)
                        ? DialogueChoiceResultRuntimeData.EndDialogue()
                        : DialogueChoiceResultRuntimeData.AddResource(
                            choiceDefinition.ResourceId,
                            choiceDefinition.ResourceAmount);
                case DialogueChoiceResultType.IncrementPlotInt:
                    string plotIntId = choiceDefinition.PlotIntId;
                    if (string.IsNullOrWhiteSpace(plotIntId))
                    {
                        plotIntId = string.IsNullOrWhiteSpace(choiceDefinition.ChoiceId)
                            ? dialogueDefinition.DefinitionId + "_choice"
                            : choiceDefinition.ChoiceId;
                    }

                    return DialogueChoiceResultRuntimeData.IncrementPlotInt(plotIntId);
                case DialogueChoiceResultType.EndDialogue:
                default:
                    return DialogueChoiceResultRuntimeData.EndDialogue();
            }
        }

        private static DialogueNodeRuntimeData CreateMockDialogueRoot()
        {
            DialogueNodeRuntimeData rewardNode = new DialogueNodeRuntimeData(
                "SCAVENGER",
                null,
                "TAKE THESE SUPPLIES. YOU WILL NEED THEM OUT THERE.",
                new[]
                {
                    new DialogueChoiceRuntimeData(
                        "THANKS.",
                        DialogueChoiceResultRuntimeData.AddResource("food", 15))
                },
                "scavenger_reward");

            return new DialogueNodeRuntimeData(
                "SCAVENGER",
                null,
                "THIS AREA IS EXTREMELY DANGEROUS, YOU'D BETTER BE CAUTIOUS.",
                new[]
                {
                    new DialogueChoiceRuntimeData(
                        "WHAT DO YOU KNOW ABOUT THIS PLACE?",
                        DialogueChoiceResultRuntimeData.NextNode(rewardNode),
                        true,
                        "know_place"),
                    new DialogueChoiceRuntimeData(
                        "ANY SUGGESTIONS?",
                        DialogueChoiceResultRuntimeData.IncrementPlotInt("scavenger_suggestion"),
                        true,
                        "suggestions"),
                    new DialogueChoiceRuntimeData(
                        "I'LL GO FIRST.",
                        DialogueChoiceResultRuntimeData.AddResource("industry", 5),
                        true,
                        "go_first")
                },
                "scavenger_start");
        }
    }
}
