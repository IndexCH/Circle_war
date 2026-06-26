using System.Collections.Generic;

namespace CircleWar
{
    // ponytail: runtime HUD DTO only; no Unity serialization until a real asset/save format needs it.
    public class GameHudRuntimeData
    {
        public HudCalendarRuntimeData Calendar = new HudCalendarRuntimeData();
        public HudSystemRuntimeData SystemStatus = new HudSystemRuntimeData();
        public HudBossRuntimeData Boss = new HudBossRuntimeData();
        public HudRegionStatusRuntimeData RegionStatus = new HudRegionStatusRuntimeData();
        public HudPlayerStatsRuntimeData PlayerStats = new HudPlayerStatsRuntimeData();
        public HudDialogueRuntimeData Dialogue = new HudDialogueRuntimeData();
        public HudFacilityRuntimeData Facility = new HudFacilityRuntimeData();

        public static GameHudRuntimeData CreateCurrentUiMockup()
        {
            return new GameHudRuntimeData
            {
                Calendar = new HudCalendarRuntimeData { Year = 2, SeasonId = "summer", SeasonName = "SUMMER", LocalHour = 10, LocalMinute = 25 },
                SystemStatus = new HudSystemRuntimeData { StatusId = "online", DisplayText = "SYSTEM ONLINE...", SignalPipCount = 4 },
                Boss = new HudBossRuntimeData { BossId = "current_boss", DisplayName = "BOSS", HealthPercent = 65 },
                RegionStatus = new HudRegionStatusRuntimeData
                {
                    RegionId = "salt_dust_plain",
                    IsLiveFeed = true,
                    FeedEntries = new List<HudFeedEntryRuntimeData>
                    {
                        new HudFeedEntryRuntimeData { Hour = 10, Minute = 24, Message = "TWO SCAVENGERS SPOTTED NEAR FIRE." },
                        new HudFeedEntryRuntimeData { Hour = 10, Minute = 25, Message = "LARGE HOSTILE SIGNAL DETECTED IN THE DISTANCE." }
                    }
                },
                PlayerStats = new HudPlayerStatsRuntimeData { Hp = 80, MaxHp = 100, Food = 120, Materials = 85, StatusId = "on_the_move", StatusText = "ON THE MOVE" },
                Dialogue = new HudDialogueRuntimeData
                {
                    CharacterId = "scavenger",
                    SpeakerName = "SCAVENGER",
                    BodyText = "THIS AREA IS EXTREMELY DANGEROUS, YOU'D BETTER BE CAUTIOUS.",
                    ActiveChoiceIndex = 0,
                    Choices = new List<HudDialogueChoiceRuntimeData>
                    {
                        new HudDialogueChoiceRuntimeData { ChoiceId = "know_place", Text = "WHAT DO YOU KNOW ABOUT THIS PLACE?", IsEnabled = true },
                        new HudDialogueChoiceRuntimeData { ChoiceId = "suggestions", Text = "ANY SUGGESTIONS?", IsEnabled = true },
                        new HudDialogueChoiceRuntimeData { ChoiceId = "go_first", Text = "I'LL GO FIRST.", IsEnabled = true }
                    }
                },
                Facility = new HudFacilityRuntimeData { FacilityId = "main_facility", ProgressPercent = 42, FilledBlockCount = 5, TotalBlockCount = 12 }
            };
        }
    }

    public class HudCalendarRuntimeData
    {
        public int Year = 1;
        public string SeasonId = string.Empty;
        public string SeasonName = string.Empty;
        public int LocalHour;
        public int LocalMinute;
    }

    public class HudSystemRuntimeData
    {
        public string StatusId = string.Empty;
        public string DisplayText = string.Empty;
        public int SignalPipCount;
    }

    public class HudBossRuntimeData
    {
        public string BossId = string.Empty;
        public string DisplayName = string.Empty;
        public int HealthPercent;
    }

    public class HudRegionStatusRuntimeData
    {
        public string RegionId = string.Empty;
        public bool IsLiveFeed;
        public List<HudFeedEntryRuntimeData> FeedEntries = new List<HudFeedEntryRuntimeData>();
    }

    public class HudFeedEntryRuntimeData
    {
        public int Hour;
        public int Minute;
        public string Message = string.Empty;
    }

    public class HudPlayerStatsRuntimeData
    {
        public int Hp;
        public int MaxHp = 1;
        public int Food;
        public int Materials;
        public string StatusId = string.Empty;
        public string StatusText = string.Empty;
    }

    public class HudDialogueRuntimeData
    {
        public string CharacterId = string.Empty;
        public string SpeakerName = string.Empty;
        public string AvatarResourcePath = string.Empty;
        public string BodyText = string.Empty;
        public int ActiveChoiceIndex;
        public List<HudDialogueChoiceRuntimeData> Choices = new List<HudDialogueChoiceRuntimeData>();
    }

    public class HudDialogueChoiceRuntimeData
    {
        public string ChoiceId = string.Empty;
        public string Text = string.Empty;
        public bool IsEnabled = true;
    }

    public class HudFacilityRuntimeData
    {
        public string FacilityId = string.Empty;
        public int ProgressPercent;
        public int FilledBlockCount;
        public int TotalBlockCount = 1;
    }
}
