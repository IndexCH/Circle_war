#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "RegionDefinition", menuName = "Circle War/Definitions/Region")]
    public sealed class RegionDefinition : GameDefinition
    {
        [SerializeField] private List<ResourceAmount> baseOutputs = new List<ResourceAmount>();
        [SerializeField] private List<WeightedEnemyEntry> enemyTable = new List<WeightedEnemyEntry>();
        [SerializeField] private List<EnvironmentParameter> environmentParameters = new List<EnvironmentParameter>();

        public string RegionName => DisplayName;
        public IReadOnlyList<ResourceAmount> BaseOutputs => baseOutputs;
        public IReadOnlyList<WeightedEnemyEntry> EnemyTable => enemyTable;
        public IReadOnlyList<EnvironmentParameter> EnvironmentParameters => environmentParameters;
    }
}

#pragma warning restore 0649
