#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "SeasonDefinition", menuName = "Circle War/Definitions/Season")]
    public sealed class SeasonDefinition : GameDefinition
    {
        [Min(0)]
        [SerializeField] private int seasonOrder;
        [SerializeField] private float movementMultiplier = 1f;
        [SerializeField] private float consumptionMultiplier = 1f;
        [SerializeField] private float hitRateMultiplier = 1f;
        [SerializeField] private List<ResourceMultiplier> resourceMultipliers = new List<ResourceMultiplier>();

        public int SeasonOrder => seasonOrder;
        public float MovementMultiplier => movementMultiplier;
        public float ConsumptionMultiplier => consumptionMultiplier;
        public float HitRateMultiplier => hitRateMultiplier;
        public IReadOnlyList<ResourceMultiplier> ResourceMultipliers => resourceMultipliers;
    }
}

#pragma warning restore 0649
