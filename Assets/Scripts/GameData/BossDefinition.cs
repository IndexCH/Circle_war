#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "BossDefinition", menuName = "Circle War/Definitions/Boss")]
    public sealed class BossDefinition : GameDefinition
    {
        [SerializeField] private Sprite portrait;
        [Min(1)]
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private List<BossPhaseDefinition> phases = new List<BossPhaseDefinition>();
        [SerializeField] private List<AttackPatternDefinition> defaultAttackPatterns = new List<AttackPatternDefinition>();
        [SerializeField] private List<GameCondition> winConditions = new List<GameCondition>();
        [SerializeField] private List<GameCondition> lossConditions = new List<GameCondition>();

        public string BossName => DisplayName;
        public Sprite Portrait => portrait;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<BossPhaseDefinition> Phases => phases;
        public IReadOnlyList<AttackPatternDefinition> DefaultAttackPatterns => defaultAttackPatterns;
        public IReadOnlyList<GameCondition> WinConditions => winConditions;
        public IReadOnlyList<GameCondition> LossConditions => lossConditions;
    }
}

#pragma warning restore 0649
