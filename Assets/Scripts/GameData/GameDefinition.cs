#pragma warning disable 0649

using UnityEngine;

namespace CircleWar
{
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField] private string definitionId;
        [SerializeField] private string displayName;
        [TextArea(2, 5)]
        [SerializeField] private string description;

        public string DefinitionId => string.IsNullOrWhiteSpace(definitionId) ? name : definitionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
    }
}

#pragma warning restore 0649
