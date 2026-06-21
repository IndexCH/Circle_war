#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "EndingRuleDefinition", menuName = "Circle War/Definitions/Ending Rule")]
    public sealed class EndingRuleDefinition : GameDefinition
    {
        [SerializeField] private List<GameCondition> conditions = new List<GameCondition>();
        [SerializeField] private int priority;
        [SerializeField] private string endingSceneName;
        [SerializeField] private Sprite endingIllustration;
        [TextArea(3, 8)]
        [SerializeField] private string endingText;

        public IReadOnlyList<GameCondition> Conditions => conditions;
        public int Priority => priority;
        public string EndingSceneName => endingSceneName;
        public Sprite EndingIllustration => endingIllustration;
        public string EndingText => endingText;
    }
}

#pragma warning restore 0649
