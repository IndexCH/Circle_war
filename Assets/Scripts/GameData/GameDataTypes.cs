#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public enum ValueComparison
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual
    }

    public enum ConditionTargetType
    {
        Resource,
        Flag,
        CharacterFavor,
        EventCompleted,
        RegionVisited,
        FacilityBuilt,
        EnemyDefeatCount,
        BossDefeated,
        EscapeInclination,
        MilitarizationInclination,
        Day,
        Season,
        CustomValue
    }

    public enum GameEffectType
    {
        AddResource,
        SetResource,
        AddCharacterFavor,
        SetFlag,
        MarkEventCompleted,
        MarkRegionVisited,
        BuildFacilityModule,
        UnlockRegion,
        StartBoss,
        CompleteBoss,
        AddEscapeInclination,
        SetEscapeInclination,
        AddMilitarizationInclination,
        SetMilitarizationInclination,
        AddCustomValue,
        SetCustomValue,
        Custom,
        PushRegionFeed
    }

    public enum EventTriggerPhase
    {
        Any,
        DayStart,
        BeforeMove,
        AfterMove,
        EnterRegion,
        CombatStart,
        CombatEnd,
        DayEnd
    }

    public enum PassiveAbilityType
    {
        Movement,
        Consumption,
        HitRate,
        ResourceGain,
        CombatAttack,
        CombatDefense,
        EventChoice,
        Custom
    }

    public enum AttackPatternType
    {
        Melee,
        Ranged,
        Area,
        Summon,
        Status,
        Custom
    }

    public enum EnemyAttackType
    {
        GroundMelee,
        GroundRanged,
        FlyingRobotRanged
    }

    [Serializable]
    public sealed class ResourceAmount
    {
        [SerializeField] private string resourceId;
        [SerializeField] private int amount;

        public string ResourceId => resourceId;
        public int Amount => amount;
    }

    [Serializable]
    public sealed class ResourceMultiplier
    {
        [SerializeField] private string resourceId;
        [SerializeField] private float multiplier = 1f;

        public string ResourceId => resourceId;
        public float Multiplier => multiplier;
    }

    [Serializable]
    public sealed class WeightedEnemyEntry
    {
        [SerializeField] private EnemyDefinition enemy;
        [Min(1)]
        [SerializeField] private int weight = 1;

        public EnemyDefinition Enemy => enemy;
        public int Weight => weight;
    }

    [Serializable]
    public sealed class EnvironmentParameter
    {
        [SerializeField] private string parameterId;
        [SerializeField] private float value;
        [TextArea(1, 3)]
        [SerializeField] private string note;

        public string ParameterId => parameterId;
        public float Value => value;
        public string Note => note;
    }

    [Serializable]
    public sealed class GameCondition
    {
        [SerializeField] private ConditionTargetType targetType;
        [SerializeField] private string targetId;
        [SerializeField] private ValueComparison comparison = ValueComparison.GreaterOrEqual;
        [SerializeField] private int value;
        [SerializeField] private bool boolValue = true;

        public ConditionTargetType TargetType => targetType;
        public string TargetId => targetId;
        public ValueComparison Comparison => comparison;
        public int Value => value;
        public bool BoolValue => boolValue;
    }

    [Serializable]
    public sealed class GameEffect
    {
        [SerializeField] private GameEffectType effectType;
        [SerializeField] private string targetId;
        [SerializeField] private int amount;
        [SerializeField] private bool boolValue = true;
        [SerializeField] private string textValue;

        public GameEffectType EffectType => effectType;
        public string TargetId => targetId;
        public int Amount => amount;
        public bool BoolValue => boolValue;
        public string TextValue => textValue;
    }

    [Serializable]
    public sealed class GameEventChoiceDefinition
    {
        [SerializeField] private string choiceId;
        [TextArea(1, 3)]
        [SerializeField] private string choiceText;
        [SerializeField] private bool consumeInteractionOnSelect = true;
        [SerializeField] private List<GameCondition> conditions = new List<GameCondition>();
        [SerializeField] private List<GameEffect> results = new List<GameEffect>();

        public string ChoiceId => choiceId;
        public string ChoiceText => choiceText;
        public bool ConsumeInteractionOnSelect => consumeInteractionOnSelect;
        public IReadOnlyList<GameCondition> Conditions => conditions;
        public IReadOnlyList<GameEffect> Results => results;
    }

    [Serializable]
    public sealed class FavorThreshold
    {
        [SerializeField] private int favor;
        [SerializeField] private string thresholdName;
        [TextArea(1, 3)]
        [SerializeField] private string description;

        public int Favor => favor;
        public string ThresholdName => thresholdName;
        public string Description => description;
    }

    [Serializable]
    public sealed class PassiveAbilityDefinition
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string abilityName;
        [SerializeField] private PassiveAbilityType abilityType;
        [SerializeField] private float value;
        [TextArea(1, 4)]
        [SerializeField] private string description;

        public string AbilityId => abilityId;
        public string AbilityName => abilityName;
        public PassiveAbilityType AbilityType => abilityType;
        public float Value => value;
        public string Description => description;
    }

    [Serializable]
    public sealed class DropEntry
    {
        [SerializeField] private string resourceId;
        [SerializeField] private int minAmount;
        [SerializeField] private int maxAmount;
        [Range(0f, 1f)]
        [SerializeField] private float dropChance = 1f;

        public string ResourceId => resourceId;
        public int MinAmount => minAmount;
        public int MaxAmount => maxAmount;
        public float DropChance => dropChance;
    }

    [Serializable]
    public sealed class AttackPatternDefinition
    {
        [SerializeField] private string patternId;
        [SerializeField] private string patternName;
        [SerializeField] private AttackPatternType patternType;
        [SerializeField] private int power;
        [SerializeField] private float cooldownSeconds;
        [TextArea(1, 4)]
        [SerializeField] private string description;

        public string PatternId => patternId;
        public string PatternName => patternName;
        public AttackPatternType PatternType => patternType;
        public int Power => power;
        public float CooldownSeconds => cooldownSeconds;
        public string Description => description;
    }

    [Serializable]
    public sealed class BossPhaseDefinition
    {
        [SerializeField] private string phaseId;
        [SerializeField] private string phaseName;
        [Range(0f, 1f)]
        [SerializeField] private float healthPercentThreshold = 1f;
        [SerializeField] private List<AttackPatternDefinition> attackPatterns = new List<AttackPatternDefinition>();

        public string PhaseId => phaseId;
        public string PhaseName => phaseName;
        public float HealthPercentThreshold => healthPercentThreshold;
        public IReadOnlyList<AttackPatternDefinition> AttackPatterns => attackPatterns;
    }
}

#pragma warning restore 0649
