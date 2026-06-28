#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "Circle War/Definitions/Dialogue")]
    public sealed class DialogueDefinition : GameDefinition
    {
        [SerializeField] private string startNodeId;
        [SerializeField] private List<DialogueNodeDefinition> nodes = new List<DialogueNodeDefinition>();

        public string StartNodeId => startNodeId;
        public IReadOnlyList<DialogueNodeDefinition> Nodes => nodes;

        public DialogueNodeDefinition StartNode
        {
            get
            {
                DialogueNodeDefinition node = FindNode(startNodeId);
                return node ?? (nodes.Count > 0 ? nodes[0] : null);
            }
        }

        public DialogueNodeDefinition FindNode(string nodeId)
        {
            if (nodes == null || string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                DialogueNodeDefinition node = nodes[index];
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (nodes == null)
            {
                nodes = new List<DialogueNodeDefinition>();
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                nodes[index]?.ValidateChoiceCount();
            }
        }
    }

    [Serializable]
    public sealed class DialogueNodeDefinition
    {
        [SerializeField] private string nodeId;
        [SerializeField] private CharacterDefinition character;
        [SerializeField] private string speakerName;
        [SerializeField] private Sprite portraitOverride;
        [TextArea(3, 8)]
        [SerializeField] private string bodyText;
        [SerializeField] private List<DialogueChoiceDefinition> choices = new List<DialogueChoiceDefinition>();

        public string NodeId => nodeId;
        public CharacterDefinition Character => character;
        public string SpeakerName => string.IsNullOrWhiteSpace(speakerName)
            ? (character == null ? string.Empty : character.CharacterName)
            : speakerName;
        public Sprite Portrait => portraitOverride != null || character == null ? portraitOverride : character.Portrait;
        public string BodyText => bodyText;
        public IReadOnlyList<DialogueChoiceDefinition> Choices => choices;

        public void ValidateChoiceCount()
        {
            if (choices == null)
            {
                choices = new List<DialogueChoiceDefinition>();
            }

            while (choices.Count < DialogueNodeRuntimeData.MinChoiceCount)
            {
                choices.Add(new DialogueChoiceDefinition("继续", DialogueChoiceResultType.EndDialogue));
            }

            while (choices.Count > DialogueNodeRuntimeData.MaxChoiceCount)
            {
                choices.RemoveAt(choices.Count - 1);
            }
        }
    }

    [Serializable]
    public sealed class DialogueChoiceDefinition
    {
        [SerializeField] private string choiceId;
        [TextArea(1, 3)]
        [SerializeField] private string choiceText;
        [SerializeField] private DialogueChoiceResultType resultType = DialogueChoiceResultType.EndDialogue;
        [SerializeField] private string nextNodeId;
        [SerializeField] private string resourceId;
        [SerializeField] private int resourceAmount;
        [SerializeField] private string plotIntId;

        public DialogueChoiceDefinition()
        {
        }

        public DialogueChoiceDefinition(string choiceText, DialogueChoiceResultType resultType)
        {
            this.choiceText = choiceText ?? string.Empty;
            this.resultType = resultType;
        }

        public string ChoiceId => choiceId;
        public string ChoiceText => choiceText;
        public DialogueChoiceResultType ResultType => resultType;
        public string NextNodeId => nextNodeId;
        public string ResourceId => resourceId;
        public int ResourceAmount => resourceAmount;
        public string PlotIntId => plotIntId;
    }
}

#pragma warning restore 0649
