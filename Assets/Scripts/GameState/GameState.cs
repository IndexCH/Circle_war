using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [Serializable]
    public sealed class GameState
    {
        [SerializeField] private string runId = Guid.NewGuid().ToString("N");
        [SerializeField] private int currentDay = 1;
        [SerializeField] private string currentSeasonId;
        [SerializeField] private string currentRegionId;
        [SerializeField] private float currentRoadPosition;
        [SerializeField] private int escapeInclination;
        [SerializeField] private int militarizationInclination;

        [SerializeField] private List<ResourceState> resources = new List<ResourceState>();
        [SerializeField] private List<FlagState> flags = new List<FlagState>();
        [SerializeField] private List<IntValueState> customValues = new List<IntValueState>();
        [SerializeField] private List<RegionProgressState> regions = new List<RegionProgressState>();
        [SerializeField] private List<CharacterProgressState> characters = new List<CharacterProgressState>();
        [SerializeField] private List<EventProgressState> events = new List<EventProgressState>();
        [SerializeField] private List<FacilityModuleProgressState> facilityModules = new List<FacilityModuleProgressState>();
        [SerializeField] private List<EnemyProgressState> enemies = new List<EnemyProgressState>();
        [SerializeField] private List<BossProgressState> bosses = new List<BossProgressState>();

        public string RunId => runId;
        public int CurrentDay => currentDay;
        public string CurrentSeasonId => currentSeasonId;
        public string CurrentRegionId => currentRegionId;
        public float CurrentRoadPosition => currentRoadPosition;
        public int EscapeInclination => escapeInclination;
        public int MilitarizationInclination => militarizationInclination;
        public IReadOnlyList<ResourceState> Resources => resources;
        public IReadOnlyList<FlagState> Flags => flags;
        public IReadOnlyList<IntValueState> CustomValues => customValues;
        public IReadOnlyList<RegionProgressState> Regions => regions;
        public IReadOnlyList<CharacterProgressState> Characters => characters;
        public IReadOnlyList<EventProgressState> Events => events;
        public IReadOnlyList<FacilityModuleProgressState> FacilityModules => facilityModules;
        public IReadOnlyList<EnemyProgressState> Enemies => enemies;
        public IReadOnlyList<BossProgressState> Bosses => bosses;

        public void StartNewRun(string newRunId = null)
        {
            runId = string.IsNullOrWhiteSpace(newRunId) ? Guid.NewGuid().ToString("N") : newRunId;
            currentDay = 1;
            currentSeasonId = string.Empty;
            currentRegionId = string.Empty;
            currentRoadPosition = 0f;
            escapeInclination = 0;
            militarizationInclination = 0;
            resources.Clear();
            flags.Clear();
            customValues.Clear();
            regions.Clear();
            characters.Clear();
            events.Clear();
            facilityModules.Clear();
            enemies.Clear();
            bosses.Clear();
        }

        public void SetCalendar(int day, string seasonId)
        {
            currentDay = Math.Max(1, day);
            currentSeasonId = seasonId ?? string.Empty;
        }

        public void AdvanceDay()
        {
            currentDay++;
        }

        public void SetLocation(string regionId, float roadPosition)
        {
            RequireId(regionId, nameof(regionId));
            currentRegionId = regionId;
            currentRoadPosition = roadPosition;
            MarkRegionVisited(regionId);
        }

        public int GetResourceAmount(string resourceId)
        {
            ResourceState state = FindResource(resourceId);
            return state == null ? 0 : state.Amount;
        }

        public void SetResourceAmount(string resourceId, int amount)
        {
            GetOrCreateResource(resourceId).SetAmount(amount);
        }

        public void AddResource(string resourceId, int amount)
        {
            ResourceState state = GetOrCreateResource(resourceId);
            state.SetAmount(state.Amount + amount);
        }

        public bool GetFlag(string flagId)
        {
            FlagState state = FindFlag(flagId);
            return state != null && state.Value;
        }

        public void SetFlag(string flagId, bool value)
        {
            GetOrCreateFlag(flagId).SetValue(value);
        }

        public int GetCustomValue(string valueId)
        {
            IntValueState state = FindCustomValue(valueId);
            return state == null ? 0 : state.Value;
        }

        public void SetCustomValue(string valueId, int value)
        {
            GetOrCreateCustomValue(valueId).SetValue(value);
        }

        public void AddCustomValue(string valueId, int amount)
        {
            IntValueState state = GetOrCreateCustomValue(valueId);
            state.SetValue(state.Value + amount);
        }

        public void SetEscapeInclination(int value)
        {
            escapeInclination = value;
        }

        public void AddEscapeInclination(int amount)
        {
            escapeInclination += amount;
        }

        public void SetMilitarizationInclination(int value)
        {
            militarizationInclination = value;
        }

        public void AddMilitarizationInclination(int amount)
        {
            militarizationInclination += amount;
        }

        public bool IsRegionUnlocked(string regionId)
        {
            RegionProgressState state = FindRegion(regionId);
            return state != null && state.IsUnlocked;
        }

        public bool IsRegionVisited(string regionId)
        {
            RegionProgressState state = FindRegion(regionId);
            return state != null && state.IsVisited;
        }

        public void MarkRegionUnlocked(string regionId)
        {
            GetOrCreateRegion(regionId).MarkUnlocked();
        }

        public void MarkRegionVisited(string regionId)
        {
            GetOrCreateRegion(regionId).MarkVisited();
        }

        public int GetCharacterFavor(string characterId)
        {
            CharacterProgressState state = FindCharacter(characterId);
            return state == null ? 0 : state.Favor;
        }

        public void SetCharacterFavor(string characterId, int favor)
        {
            GetOrCreateCharacter(characterId).SetFavor(favor);
        }

        public void AddCharacterFavor(string characterId, int amount)
        {
            CharacterProgressState state = GetOrCreateCharacter(characterId);
            state.SetFavor(state.Favor + amount);
        }

        public bool IsEventCompleted(string eventId)
        {
            EventProgressState state = FindEvent(eventId);
            return state != null && state.IsCompleted;
        }

        public void MarkEventCompleted(string eventId, string selectedChoiceId = null)
        {
            GetOrCreateEvent(eventId).MarkCompleted(currentDay, selectedChoiceId);
        }

        public bool IsFacilityModuleBuilt(string moduleId)
        {
            FacilityModuleProgressState state = FindFacilityModule(moduleId);
            return state != null && state.IsBuilt;
        }

        public void MarkFacilityModuleBuilt(string moduleId)
        {
            GetOrCreateFacilityModule(moduleId).MarkBuilt(currentDay);
        }

        public int GetEnemyDefeatCount(string enemyId)
        {
            EnemyProgressState state = FindEnemy(enemyId);
            return state == null ? 0 : state.DefeatCount;
        }

        public void AddEnemyDefeat(string enemyId, int amount = 1)
        {
            GetOrCreateEnemy(enemyId).AddDefeat(amount);
        }

        public BossProgressState GetBossProgress(string bossId)
        {
            return FindBoss(bossId);
        }

        public void StartBoss(string bossId, int maxHealth, string phaseId = null)
        {
            GetOrCreateBoss(bossId).Start(maxHealth, phaseId);
        }

        public void SetBossHealth(string bossId, int currentHealth)
        {
            GetOrCreateBoss(bossId).SetCurrentHealth(currentHealth);
        }

        public void SetBossPhase(string bossId, string phaseId)
        {
            GetOrCreateBoss(bossId).SetPhase(phaseId);
        }

        public bool IsBossDefeated(string bossId)
        {
            BossProgressState state = FindBoss(bossId);
            return state != null && state.IsDefeated;
        }

        public void MarkBossDefeated(string bossId)
        {
            GetOrCreateBoss(bossId).MarkDefeated(currentDay);
        }

        private ResourceState FindResource(string resourceId)
        {
            RequireId(resourceId, nameof(resourceId));
            return resources.Find(item => HasId(item.ResourceId, resourceId));
        }

        private ResourceState GetOrCreateResource(string resourceId)
        {
            ResourceState state = FindResource(resourceId);
            if (state != null)
            {
                return state;
            }

            state = new ResourceState(resourceId);
            resources.Add(state);
            return state;
        }

        private FlagState FindFlag(string flagId)
        {
            RequireId(flagId, nameof(flagId));
            return flags.Find(item => HasId(item.FlagId, flagId));
        }

        private FlagState GetOrCreateFlag(string flagId)
        {
            FlagState state = FindFlag(flagId);
            if (state != null)
            {
                return state;
            }

            state = new FlagState(flagId);
            flags.Add(state);
            return state;
        }

        private IntValueState FindCustomValue(string valueId)
        {
            RequireId(valueId, nameof(valueId));
            return customValues.Find(item => HasId(item.ValueId, valueId));
        }

        private IntValueState GetOrCreateCustomValue(string valueId)
        {
            IntValueState state = FindCustomValue(valueId);
            if (state != null)
            {
                return state;
            }

            state = new IntValueState(valueId);
            customValues.Add(state);
            return state;
        }

        private RegionProgressState FindRegion(string regionId)
        {
            RequireId(regionId, nameof(regionId));
            return regions.Find(item => HasId(item.RegionId, regionId));
        }

        private RegionProgressState GetOrCreateRegion(string regionId)
        {
            RegionProgressState state = FindRegion(regionId);
            if (state != null)
            {
                return state;
            }

            state = new RegionProgressState(regionId);
            regions.Add(state);
            return state;
        }

        private CharacterProgressState FindCharacter(string characterId)
        {
            RequireId(characterId, nameof(characterId));
            return characters.Find(item => HasId(item.CharacterId, characterId));
        }

        private CharacterProgressState GetOrCreateCharacter(string characterId)
        {
            CharacterProgressState state = FindCharacter(characterId);
            if (state != null)
            {
                return state;
            }

            state = new CharacterProgressState(characterId);
            characters.Add(state);
            return state;
        }

        private EventProgressState FindEvent(string eventId)
        {
            RequireId(eventId, nameof(eventId));
            return events.Find(item => HasId(item.EventId, eventId));
        }

        private EventProgressState GetOrCreateEvent(string eventId)
        {
            EventProgressState state = FindEvent(eventId);
            if (state != null)
            {
                return state;
            }

            state = new EventProgressState(eventId);
            events.Add(state);
            return state;
        }

        private FacilityModuleProgressState FindFacilityModule(string moduleId)
        {
            RequireId(moduleId, nameof(moduleId));
            return facilityModules.Find(item => HasId(item.ModuleId, moduleId));
        }

        private FacilityModuleProgressState GetOrCreateFacilityModule(string moduleId)
        {
            FacilityModuleProgressState state = FindFacilityModule(moduleId);
            if (state != null)
            {
                return state;
            }

            state = new FacilityModuleProgressState(moduleId);
            facilityModules.Add(state);
            return state;
        }

        private EnemyProgressState FindEnemy(string enemyId)
        {
            RequireId(enemyId, nameof(enemyId));
            return enemies.Find(item => HasId(item.EnemyId, enemyId));
        }

        private EnemyProgressState GetOrCreateEnemy(string enemyId)
        {
            EnemyProgressState state = FindEnemy(enemyId);
            if (state != null)
            {
                return state;
            }

            state = new EnemyProgressState(enemyId);
            enemies.Add(state);
            return state;
        }

        private BossProgressState FindBoss(string bossId)
        {
            RequireId(bossId, nameof(bossId));
            return bosses.Find(item => HasId(item.BossId, bossId));
        }

        private BossProgressState GetOrCreateBoss(string bossId)
        {
            BossProgressState state = FindBoss(bossId);
            if (state != null)
            {
                return state;
            }

            state = new BossProgressState(bossId);
            bosses.Add(state);
            return state;
        }

        private static bool HasId(string currentId, string requestedId)
        {
            return string.Equals(currentId, requestedId, StringComparison.Ordinal);
        }

        private static void RequireId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("State ids cannot be empty.", parameterName);
            }
        }
    }

    [Serializable]
    public sealed class ResourceState
    {
        [SerializeField] private string resourceId;
        [SerializeField] private int amount;

        public ResourceState()
        {
        }

        public ResourceState(string resourceId)
        {
            this.resourceId = resourceId;
        }

        public string ResourceId => resourceId;
        public int Amount => amount;

        public void SetAmount(int amount)
        {
            this.amount = amount;
        }
    }

    [Serializable]
    public sealed class FlagState
    {
        [SerializeField] private string flagId;
        [SerializeField] private bool value;

        public FlagState()
        {
        }

        public FlagState(string flagId)
        {
            this.flagId = flagId;
        }

        public string FlagId => flagId;
        public bool Value => value;

        public void SetValue(bool value)
        {
            this.value = value;
        }
    }

    [Serializable]
    public sealed class IntValueState
    {
        [SerializeField] private string valueId;
        [SerializeField] private int value;

        public IntValueState()
        {
        }

        public IntValueState(string valueId)
        {
            this.valueId = valueId;
        }

        public string ValueId => valueId;
        public int Value => value;

        public void SetValue(int value)
        {
            this.value = value;
        }
    }

    [Serializable]
    public sealed class RegionProgressState
    {
        [SerializeField] private string regionId;
        [SerializeField] private bool isUnlocked;
        [SerializeField] private bool isVisited;
        [SerializeField] private int visitCount;

        public RegionProgressState()
        {
        }

        public RegionProgressState(string regionId)
        {
            this.regionId = regionId;
        }

        public string RegionId => regionId;
        public bool IsUnlocked => isUnlocked;
        public bool IsVisited => isVisited;
        public int VisitCount => visitCount;

        public void MarkUnlocked()
        {
            isUnlocked = true;
        }

        public void MarkVisited()
        {
            isUnlocked = true;
            isVisited = true;
            visitCount++;
        }
    }

    [Serializable]
    public sealed class CharacterProgressState
    {
        [SerializeField] private string characterId;
        [SerializeField] private int favor;

        public CharacterProgressState()
        {
        }

        public CharacterProgressState(string characterId)
        {
            this.characterId = characterId;
        }

        public string CharacterId => characterId;
        public int Favor => favor;

        public void SetFavor(int favor)
        {
            this.favor = favor;
        }
    }

    [Serializable]
    public sealed class EventProgressState
    {
        [SerializeField] private string eventId;
        [SerializeField] private bool isCompleted;
        [SerializeField] private string selectedChoiceId;
        [SerializeField] private int completedDay;

        public EventProgressState()
        {
        }

        public EventProgressState(string eventId)
        {
            this.eventId = eventId;
        }

        public string EventId => eventId;
        public bool IsCompleted => isCompleted;
        public string SelectedChoiceId => selectedChoiceId;
        public int CompletedDay => completedDay;

        public void MarkCompleted(int currentDay, string selectedChoiceId)
        {
            isCompleted = true;
            completedDay = currentDay;
            this.selectedChoiceId = selectedChoiceId ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class FacilityModuleProgressState
    {
        [SerializeField] private string moduleId;
        [SerializeField] private bool isBuilt;
        [SerializeField] private int builtDay;

        public FacilityModuleProgressState()
        {
        }

        public FacilityModuleProgressState(string moduleId)
        {
            this.moduleId = moduleId;
        }

        public string ModuleId => moduleId;
        public bool IsBuilt => isBuilt;
        public int BuiltDay => builtDay;

        public void MarkBuilt(int currentDay)
        {
            isBuilt = true;
            builtDay = currentDay;
        }
    }

    [Serializable]
    public sealed class EnemyProgressState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int defeatCount;

        public EnemyProgressState()
        {
        }

        public EnemyProgressState(string enemyId)
        {
            this.enemyId = enemyId;
        }

        public string EnemyId => enemyId;
        public int DefeatCount => defeatCount;

        public void AddDefeat(int amount)
        {
            defeatCount += Math.Max(0, amount);
        }
    }

    [Serializable]
    public sealed class BossProgressState
    {
        [SerializeField] private string bossId;
        [SerializeField] private bool isStarted;
        [SerializeField] private bool isDefeated;
        [SerializeField] private int maxHealth;
        [SerializeField] private int currentHealth;
        [SerializeField] private string currentPhaseId;
        [SerializeField] private int defeatedDay;

        public BossProgressState()
        {
        }

        public BossProgressState(string bossId)
        {
            this.bossId = bossId;
        }

        public string BossId => bossId;
        public bool IsStarted => isStarted;
        public bool IsDefeated => isDefeated;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public string CurrentPhaseId => currentPhaseId;
        public int DefeatedDay => defeatedDay;

        public void Start(int maxHealth, string phaseId)
        {
            isStarted = true;
            isDefeated = false;
            this.maxHealth = Math.Max(1, maxHealth);
            currentHealth = this.maxHealth;
            currentPhaseId = phaseId ?? string.Empty;
            defeatedDay = 0;
        }

        public void SetCurrentHealth(int currentHealth)
        {
            this.currentHealth = Math.Max(0, currentHealth);
        }

        public void SetPhase(string phaseId)
        {
            currentPhaseId = phaseId ?? string.Empty;
        }

        public void MarkDefeated(int currentDay)
        {
            isStarted = true;
            isDefeated = true;
            currentHealth = 0;
            defeatedDay = currentDay;
        }
    }
}
