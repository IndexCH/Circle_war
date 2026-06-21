#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Circle War/Definitions/Character")]
    public sealed class CharacterDefinition : GameDefinition
    {
        [SerializeField] private Sprite portrait;
        [SerializeField] private List<Sprite> alternatePortraits = new List<Sprite>();
        [SerializeField] private List<FavorThreshold> favorThresholds = new List<FavorThreshold>();
        [SerializeField] private List<PassiveAbilityDefinition> passiveAbilities = new List<PassiveAbilityDefinition>();

        public string CharacterName => DisplayName;
        public Sprite Portrait => portrait;
        public IReadOnlyList<Sprite> AlternatePortraits => alternatePortraits;
        public IReadOnlyList<FavorThreshold> FavorThresholds => favorThresholds;
        public IReadOnlyList<PassiveAbilityDefinition> PassiveAbilities => passiveAbilities;
    }
}

#pragma warning restore 0649
