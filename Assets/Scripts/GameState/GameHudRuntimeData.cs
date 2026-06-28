using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public sealed class GameHudRuntimeData
    {
        public readonly HudCalendarRuntimeData Calendar = new HudCalendarRuntimeData();
        public readonly HudSystemRuntimeData SystemStatus = new HudSystemRuntimeData();
        public readonly HudBossRuntimeData Boss = new HudBossRuntimeData();
        public readonly HudRegionStatusRuntimeData RegionStatus = new HudRegionStatusRuntimeData();
        public readonly HudPlayerStatsRuntimeData PlayerStats = new HudPlayerStatsRuntimeData();
        public readonly HudDialogueRuntimeData Dialogue = new HudDialogueRuntimeData();
        public readonly HudFacilityRuntimeData Facility = new HudFacilityRuntimeData();

        public void CopyFrom(GameHudRuntimeData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Calendar.CopyFrom(source.Calendar);
            SystemStatus.CopyFrom(source.SystemStatus);
            Boss.CopyFrom(source.Boss);
            RegionStatus.CopyFrom(source.RegionStatus);
            PlayerStats.CopyFrom(source.PlayerStats);
            Dialogue.CopyFrom(source.Dialogue);
            Facility.CopyFrom(source.Facility);
        }

        public static GameHudRuntimeData CreateCurrentUiMockup()
        {
            GameHudRuntimeData data = new GameHudRuntimeData();
            data.Calendar.Set(2, "summer", "SUMMER", 10, 25);
            data.SystemStatus.Set("online", "SYSTEM ONLINE...", 4);
            data.Boss.Set("current_boss", "BOSS", 65);
            data.RegionStatus.Set(
                "salt_dust_plain",
                "SALT DUST PLAIN",
                true,
                new[]
                {
                    new HudFeedEntryRuntimeData(10, 24, "TWO SCAVENGERS SPOTTED NEAR FIRE."),
                    new HudFeedEntryRuntimeData(10, 25, "LARGE HOSTILE SIGNAL DETECTED IN THE DISTANCE.")
                });
            data.PlayerStats.Set(80, 100, 120, 85, "on_the_move", "ON THE MOVE");
            data.Dialogue.Clear();
            data.Facility.Set("main_facility", 42, 5, 12);
            return data;
        }
    }

    public sealed class HudCalendarRuntimeData
    {
        public readonly BindableProperty<int> Year = new BindableProperty<int>(1);
        public readonly BindableProperty<string> SeasonId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<string> SeasonName = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<int> LocalHour = new BindableProperty<int>();
        public readonly BindableProperty<int> LocalMinute = new BindableProperty<int>();

        public void Set(int year, string seasonId, string seasonName, int localHour, int localMinute)
        {
            Year.Value = Math.Max(1, year);
            SeasonId.Value = seasonId ?? string.Empty;
            SeasonName.Value = seasonName ?? string.Empty;
            LocalHour.Value = Clamp(localHour, 0, 23);
            LocalMinute.Value = Clamp(localMinute, 0, 59);
        }

        public void CopyFrom(HudCalendarRuntimeData source)
        {
            Set(source.Year.Value, source.SeasonId.Value, source.SeasonName.Value, source.LocalHour.Value, source.LocalMinute.Value);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }

    public sealed class HudSystemRuntimeData
    {
        public readonly BindableProperty<string> StatusId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<string> DisplayText = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<int> SignalPipCount = new BindableProperty<int>();

        public void Set(string statusId, string displayText, int signalPipCount)
        {
            StatusId.Value = statusId ?? string.Empty;
            DisplayText.Value = displayText ?? string.Empty;
            SignalPipCount.Value = Math.Max(0, signalPipCount);
        }

        public void CopyFrom(HudSystemRuntimeData source)
        {
            Set(source.StatusId.Value, source.DisplayText.Value, source.SignalPipCount.Value);
        }
    }

    public sealed class HudBossRuntimeData
    {
        public readonly BindableProperty<string> BossId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<string> DisplayName = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<int> HealthPercent = new BindableProperty<int>();

        public void Set(string bossId, string displayName, int healthPercent)
        {
            BossId.Value = bossId ?? string.Empty;
            DisplayName.Value = displayName ?? string.Empty;
            HealthPercent.Value = ClampPercent(healthPercent);
        }

        public void CopyFrom(HudBossRuntimeData source)
        {
            Set(source.BossId.Value, source.DisplayName.Value, source.HealthPercent.Value);
        }

        private static int ClampPercent(int value)
        {
            return Math.Min(100, Math.Max(0, value));
        }
    }

    public sealed class HudRegionStatusRuntimeData
    {
        public readonly BindableProperty<string> RegionId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<string> DisplayName = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<bool> IsLiveFeed = new BindableProperty<bool>();
        public readonly BindableProperty<IReadOnlyList<HudFeedEntryRuntimeData>> FeedEntries =
            new BindableProperty<IReadOnlyList<HudFeedEntryRuntimeData>>(new List<HudFeedEntryRuntimeData>());

        public void Set(string regionId, string displayName, bool isLiveFeed, IEnumerable<HudFeedEntryRuntimeData> feedEntries)
        {
            RegionId.Value = regionId ?? string.Empty;
            DisplayName.Value = displayName ?? string.Empty;
            IsLiveFeed.Value = isLiveFeed;
            FeedEntries.Value = CopyFeedEntries(feedEntries);
        }

        public void CopyFrom(HudRegionStatusRuntimeData source)
        {
            Set(source.RegionId.Value, source.DisplayName.Value, source.IsLiveFeed.Value, source.FeedEntries.Value);
        }

        private static IReadOnlyList<HudFeedEntryRuntimeData> CopyFeedEntries(IEnumerable<HudFeedEntryRuntimeData> feedEntries)
        {
            List<HudFeedEntryRuntimeData> entries = new List<HudFeedEntryRuntimeData>();
            if (feedEntries == null)
            {
                return entries;
            }

            foreach (HudFeedEntryRuntimeData entry in feedEntries)
            {
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }
    }

    public sealed class HudFeedEntryRuntimeData
    {
        public int Hour;
        public int Minute;
        public string Message = string.Empty;

        public HudFeedEntryRuntimeData()
        {
        }

        public HudFeedEntryRuntimeData(int hour, int minute, string message)
        {
            Hour = hour;
            Minute = minute;
            Message = message ?? string.Empty;
        }
    }

    public sealed class HudPlayerStatsRuntimeData
    {
        public readonly BindableProperty<int> Hp = new BindableProperty<int>();
        public readonly BindableProperty<int> MaxHp = new BindableProperty<int>(1);
        public readonly BindableProperty<int> Food = new BindableProperty<int>();
        public readonly BindableProperty<int> Materials = new BindableProperty<int>();
        public readonly BindableProperty<string> StatusId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<string> StatusText = new BindableProperty<string>(string.Empty);

        public void Set(int hp, int maxHp, int food, int materials, string statusId, string statusText)
        {
            int safeMaxHp = Math.Max(1, maxHp);
            MaxHp.Value = safeMaxHp;
            Hp.Value = Math.Min(safeMaxHp, Math.Max(0, hp));
            Food.Value = Math.Max(0, food);
            Materials.Value = Math.Max(0, materials);
            StatusId.Value = statusId ?? string.Empty;
            StatusText.Value = statusText ?? string.Empty;
        }

        public void CopyFrom(HudPlayerStatsRuntimeData source)
        {
            Set(source.Hp.Value, source.MaxHp.Value, source.Food.Value, source.Materials.Value, source.StatusId.Value, source.StatusText.Value);
        }
    }

    public sealed class HudDialogueRuntimeData
    {
        public readonly BindableProperty<bool> IsVisible = new BindableProperty<bool>();
        public readonly BindableProperty<string> CharacterName = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<Sprite> Portrait = new BindableProperty<Sprite>();
        public readonly BindableProperty<string> BodyText = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<int> ChoiceCount = new BindableProperty<int>();
        public readonly BindableProperty<int> ActiveChoiceIndex = new BindableProperty<int>();
        public readonly BindableProperty<IReadOnlyList<DialogueChoiceRuntimeData>> Choices =
            new BindableProperty<IReadOnlyList<DialogueChoiceRuntimeData>>(new List<DialogueChoiceRuntimeData>());

        public DialogueNodeRuntimeData CurrentNode { get; private set; }

        public void ShowNode(DialogueNodeRuntimeData node)
        {
            if (node == null)
            {
                Clear();
                return;
            }

            CurrentNode = node;
            IsVisible.Value = true;
            CharacterName.Value = node.CharacterName;
            Portrait.Value = node.Portrait;
            BodyText.Value = node.BodyText;
            Choices.Value = node.Choices;
            ChoiceCount.Value = node.Choices.Count;
            ActiveChoiceIndex.Value = ClampChoiceIndex(0, node.Choices.Count);
        }

        public void Clear()
        {
            CurrentNode = null;
            IsVisible.Value = false;
            CharacterName.Value = string.Empty;
            Portrait.Value = null;
            BodyText.Value = string.Empty;
            Choices.Value = new List<DialogueChoiceRuntimeData>();
            ChoiceCount.Value = 0;
            ActiveChoiceIndex.Value = 0;
        }

        public void CopyFrom(HudDialogueRuntimeData source)
        {
            if (source == null || source.CurrentNode == null)
            {
                Clear();
                return;
            }

            ShowNode(source.CurrentNode);
            ActiveChoiceIndex.Value = ClampChoiceIndex(source.ActiveChoiceIndex.Value, ChoiceCount.Value);
        }

        private static int ClampChoiceIndex(int value, int choiceCount)
        {
            if (choiceCount <= 0)
            {
                return 0;
            }

            return Math.Min(choiceCount - 1, Math.Max(0, value));
        }
    }

    public sealed class HudFacilityRuntimeData
    {
        public readonly BindableProperty<string> FacilityId = new BindableProperty<string>(string.Empty);
        public readonly BindableProperty<int> ProgressPercent = new BindableProperty<int>();
        public readonly BindableProperty<int> FilledBlockCount = new BindableProperty<int>();
        public readonly BindableProperty<int> TotalBlockCount = new BindableProperty<int>(1);

        public void Set(string facilityId, int progressPercent, int filledBlockCount, int totalBlockCount)
        {
            int safeTotalBlockCount = Math.Max(1, totalBlockCount);
            FacilityId.Value = facilityId ?? string.Empty;
            ProgressPercent.Value = Math.Min(100, Math.Max(0, progressPercent));
            TotalBlockCount.Value = safeTotalBlockCount;
            FilledBlockCount.Value = Math.Min(safeTotalBlockCount, Math.Max(0, filledBlockCount));
        }

        public void CopyFrom(HudFacilityRuntimeData source)
        {
            Set(source.FacilityId.Value, source.ProgressPercent.Value, source.FilledBlockCount.Value, source.TotalBlockCount.Value);
        }
    }
}
