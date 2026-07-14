using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public enum DialogueChoiceResultType
    {
        EndDialogue,
        NextDialogueNode,
        AddResource,
        IncrementPlotInt,
        ApplyEventChoice
    }

    public sealed class DialogueNodeRuntimeData
    {
        public const int MinChoiceCount = 1;
        public const int MaxChoiceCount = 3;

        private readonly List<DialogueChoiceRuntimeData> choices = new List<DialogueChoiceRuntimeData>();

        public CharacterDefinition Character { get; private set; }
        public string NodeId { get; private set; }
        public string CharacterName { get; private set; }
        public Sprite Portrait { get; private set; }
        public string BodyText { get; private set; }
        public IReadOnlyList<DialogueChoiceRuntimeData> Choices => choices;

        public DialogueNodeRuntimeData(
            CharacterDefinition character,
            string bodyText,
            IEnumerable<DialogueChoiceRuntimeData> choices,
            string nodeId = null,
            Sprite portraitOverride = null)
        {
            Set(character, bodyText, choices, nodeId, portraitOverride);
        }

        public DialogueNodeRuntimeData(
            string characterName,
            Sprite portrait,
            string bodyText,
            IEnumerable<DialogueChoiceRuntimeData> choices,
            string nodeId = null)
        {
            Set(characterName, portrait, bodyText, choices, nodeId);
        }

        public void Set(
            CharacterDefinition character,
            string bodyText,
            IEnumerable<DialogueChoiceRuntimeData> choices,
            string nodeId = null,
            Sprite portraitOverride = null)
        {
            Character = character;
            NodeId = nodeId ?? string.Empty;
            CharacterName = character == null ? string.Empty : character.CharacterName;
            Portrait = portraitOverride != null || character == null ? portraitOverride : character.Portrait;
            BodyText = bodyText ?? string.Empty;
            SetChoices(choices);
        }

        public void Set(
            string characterName,
            Sprite portrait,
            string bodyText,
            IEnumerable<DialogueChoiceRuntimeData> choices,
            string nodeId = null)
        {
            Character = null;
            NodeId = nodeId ?? string.Empty;
            CharacterName = characterName ?? string.Empty;
            Portrait = portrait;
            BodyText = bodyText ?? string.Empty;
            SetChoices(choices);
        }

        private void SetChoices(IEnumerable<DialogueChoiceRuntimeData> newChoices)
        {
            choices.Clear();

            if (newChoices != null)
            {
                foreach (DialogueChoiceRuntimeData choice in newChoices)
                {
                    if (choice != null)
                    {
                        choices.Add(choice);
                    }
                }
            }

            if (choices.Count < MinChoiceCount || choices.Count > MaxChoiceCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newChoices),
                    "Dialogue node choices must contain at least 1 and at most 3 options.");
            }
        }
    }

    public sealed class DialogueChoiceRuntimeData
    {
        public string ChoiceId { get; private set; }
        public string Text { get; private set; }
        public bool IsEnabled { get; private set; }
        public DialogueChoiceResultRuntimeData Result { get; private set; }
        public string SourceInteractionId { get; private set; }
        public bool ConsumeInteractionOnSelect { get; private set; }

        public DialogueChoiceRuntimeData(
            string text,
            DialogueChoiceResultRuntimeData result,
            bool isEnabled = true,
            string choiceId = null,
            string sourceInteractionId = null,
            bool consumeInteractionOnSelect = true)
        {
            ChoiceId = choiceId ?? string.Empty;
            Text = text ?? string.Empty;
            Result = result ?? DialogueChoiceResultRuntimeData.EndDialogue();
            IsEnabled = isEnabled;
            SourceInteractionId = sourceInteractionId ?? string.Empty;
            ConsumeInteractionOnSelect = consumeInteractionOnSelect;
        }
    }

    public sealed class DialogueChoiceResultRuntimeData
    {
        private DialogueChoiceResultRuntimeData(DialogueChoiceResultType resultType)
        {
            ResultType = resultType;
        }

        public DialogueChoiceResultType ResultType { get; private set; }
        public DialogueNodeRuntimeData NextDialogueNode { get; private set; }
        public string NextDialogueNodeId { get; private set; }
        public string ResourceId { get; private set; }
        public int ResourceAmount { get; private set; }
        public string PlotIntId { get; private set; }
        public GameEventDefinition EventDefinition { get; private set; }
        public GameEventChoiceDefinition EventChoice { get; private set; }

        public static DialogueChoiceResultRuntimeData EndDialogue()
        {
            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.EndDialogue);
        }

        public static DialogueChoiceResultRuntimeData NextNode(DialogueNodeRuntimeData nextDialogueNode)
        {
            if (nextDialogueNode == null)
            {
                throw new ArgumentNullException(nameof(nextDialogueNode));
            }

            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.NextDialogueNode)
            {
                NextDialogueNode = nextDialogueNode
            };
        }

        public static DialogueChoiceResultRuntimeData NextNode(string nextDialogueNodeId)
        {
            RequireId(nextDialogueNodeId, nameof(nextDialogueNodeId));

            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.NextDialogueNode)
            {
                NextDialogueNodeId = nextDialogueNodeId
            };
        }

        public static DialogueChoiceResultRuntimeData AddResource(string resourceId, int amount)
        {
            RequireId(resourceId, nameof(resourceId));

            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.AddResource)
            {
                ResourceId = resourceId,
                ResourceAmount = Math.Max(0, amount)
            };
        }

        public static DialogueChoiceResultRuntimeData IncrementPlotInt(string plotIntId)
        {
            RequireId(plotIntId, nameof(plotIntId));

            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.IncrementPlotInt)
            {
                PlotIntId = plotIntId
            };
        }

        public static DialogueChoiceResultRuntimeData ApplyEventChoice(
            GameEventDefinition eventDefinition,
            GameEventChoiceDefinition eventChoice)
        {
            if (eventDefinition == null)
            {
                throw new ArgumentNullException(nameof(eventDefinition));
            }

            if (eventChoice == null)
            {
                throw new ArgumentNullException(nameof(eventChoice));
            }

            return new DialogueChoiceResultRuntimeData(DialogueChoiceResultType.ApplyEventChoice)
            {
                EventDefinition = eventDefinition,
                EventChoice = eventChoice
            };
        }

        private static void RequireId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Dialogue result ids cannot be empty.", parameterName);
            }
        }
    }
}
