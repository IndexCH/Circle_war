#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "FacilityModuleDefinition", menuName = "Circle War/Definitions/Facility Module")]
    public sealed class FacilityModuleDefinition : GameDefinition
    {
        [SerializeField] private List<ResourceAmount> costs = new List<ResourceAmount>();
        [SerializeField] private List<FacilityModuleDefinition> prerequisiteModules = new List<FacilityModuleDefinition>();
        [Range(-100, 100)]
        [SerializeField] private int escapeInclination;
        [Range(-100, 100)]
        [SerializeField] private int militarizationInclination;
        [SerializeField] private List<GameEffect> buildEffects = new List<GameEffect>();

        public IReadOnlyList<ResourceAmount> Costs => costs;
        public IReadOnlyList<FacilityModuleDefinition> PrerequisiteModules => prerequisiteModules;
        public int EscapeInclination => escapeInclination;
        public int MilitarizationInclination => militarizationInclination;
        public IReadOnlyList<GameEffect> BuildEffects => buildEffects;
    }
}

#pragma warning restore 0649
