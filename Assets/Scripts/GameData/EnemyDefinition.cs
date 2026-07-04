#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Circle War/Definitions/Enemy")]
    public sealed class EnemyDefinition : GameDefinition
    {
        [SerializeField] private Sprite portrait;
        [SerializeField] private EnemyAttackType attackType = EnemyAttackType.GroundMelee;
        [Min(1)]
        [SerializeField] private int maxHealth = 1;
        [Min(0f)]
        [SerializeField] private float speed;
        [Min(0)]
        [SerializeField] private int attackPower;
        [SerializeField] private List<DropEntry> drops = new List<DropEntry>();

        public string EnemyName => DisplayName;
        public Sprite Portrait => portrait;
        public EnemyAttackType AttackType => attackType;
        public int MaxHealth => maxHealth;
        public float Speed => speed;
        public int AttackPower => attackPower;
        public IReadOnlyList<DropEntry> Drops => drops;
    }
}

#pragma warning restore 0649
