#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "GameEventDefinition", menuName = "Circle War/Definitions/Game Event")]
    public sealed class GameEventDefinition : GameDefinition
    {
        [Min(0)]
        [SerializeField] private int earliestDay;
        [Min(0)]
        [SerializeField] private int latestDay;
        [SerializeField] private EventTriggerPhase triggerPhase = EventTriggerPhase.Any;
        [SerializeField] private RegionDefinition region;
        [SerializeField] private string locationId;
        [SerializeField] private List<GameCondition> conditions = new List<GameCondition>();

        [SerializeField] private string title;
        [TextArea(3, 8)]
        [SerializeField] private string bodyText;
        [SerializeField] private List<GameEventChoiceDefinition> choices = new List<GameEventChoiceDefinition>();
        [SerializeField] private List<GameEffect> automaticResults = new List<GameEffect>();

        public int EarliestDay => earliestDay;
        public int LatestDay => latestDay;
        public EventTriggerPhase TriggerPhase => triggerPhase;
        public RegionDefinition Region => region;
        public string LocationId => locationId;
        public IReadOnlyList<GameCondition> Conditions => conditions;
        public string Title => string.IsNullOrWhiteSpace(title) ? DisplayName : title;
        public string BodyText => bodyText;
        public IReadOnlyList<GameEventChoiceDefinition> Choices => choices;
        public IReadOnlyList<GameEffect> AutomaticResults => automaticResults;
    }
}

#pragma warning restore 0649
