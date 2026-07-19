using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class FacilityProgressTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdGameObjects.Count - 1; index >= 0; index--)
            {
                if (createdGameObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdGameObjects[index]);
                }
            }

            createdGameObjects.Clear();
        }

        [Test]
        public void SegmentedBarDisplaysPartialProgressInsideTheCurrentBlock()
        {
            FacilitySegmentedProgressBar bar = CreateProgressBar();

            bar.SetProgressPercent(0, false);
            Assert.That(bar.ProgressPercent, Is.EqualTo(0));
            AssertAllSegments(bar, 0f);

            bar.SetProgressPercent(1, false);
            Assert.That(bar.GetSegmentFillAmount(0), Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(bar.GetSegmentFillAmount(1), Is.EqualTo(0f).Within(0.0001f));

            bar.SetProgressPercent(42, false);
            for (int index = 0; index < 4; index++)
            {
                Assert.That(bar.GetSegmentFillAmount(index), Is.EqualTo(1f).Within(0.0001f));
            }

            Assert.That(bar.GetSegmentFillAmount(4), Is.EqualTo(0.2f).Within(0.0001f));
            for (int index = 5; index < bar.SegmentCount; index++)
            {
                Assert.That(bar.GetSegmentFillAmount(index), Is.EqualTo(0f).Within(0.0001f));
            }

            bar.SetProgressPercent(100, false);
            AssertAllSegments(bar, 1f);
        }

        [Test]
        public void SegmentedBarClampsProgressToZeroAndOneHundredPercent()
        {
            FacilitySegmentedProgressBar bar = CreateProgressBar();

            bar.SetProgressPercent(-20, false);
            Assert.That(bar.ProgressPercent, Is.EqualTo(0));
            AssertAllSegments(bar, 0f);

            bar.SetProgressPercent(140, false);
            Assert.That(bar.ProgressPercent, Is.EqualTo(100));
            AssertAllSegments(bar, 1f);
        }

        [Test]
        public void NewRunAndUiMockupStartFacilityProgressAtZero()
        {
            GameRuntimeData runtime = new GameRuntimeData();
            runtime.StartNewRun("facility_progress_test");

            AssertFacilityProgress(runtime, 0, 0, 10);

            runtime.LoadCurrentUiMockup();
            AssertFacilityProgress(runtime, 0, 0, 10);

            GameHudRuntimeData hudMockup = GameHudRuntimeData.CreateCurrentUiMockup();
            Assert.That(hudMockup.Facility.ProgressPercent.Value, Is.EqualTo(0));
            Assert.That(hudMockup.Facility.FilledBlockCount.Value, Is.EqualTo(0));
            Assert.That(hudMockup.Facility.TotalBlockCount.Value, Is.EqualTo(10));
        }

        [Test]
        public void RoadRewardAdvancesOnceEvenWhenFoodAndIndustryAreGrantedTogether()
        {
            GameRuntimeData runtime = CreateRuntime();
            ResourceAmount[] rewards =
            {
                CreateResourceAmount("food", 10),
                CreateResourceAmount("industry", 5),
                CreateResourceAmount("steel", 1)
            };

            Assert.That(runtime.TryCollectRoadSegmentResource("combined_reward", rewards), Is.True);
            AssertFacilityProgress(runtime, 1, 0, 10);

            Assert.That(runtime.TryCollectRoadSegmentResource("combined_reward", rewards), Is.False);
            AssertFacilityProgress(runtime, 1, 0, 10);
        }

        [Test]
        public void UnrelatedNonPositiveAndSpentResourcesDoNotAdvanceFacilityProgress()
        {
            GameRuntimeData runtime = CreateRuntime();

            runtime.TryCollectRoadSegmentResource(
                "unrelated_reward",
                new[] { CreateResourceAmount("steel", 1) });
            runtime.TryCollectRoadSegmentResource(
                "non_positive_reward",
                new[]
                {
                    CreateResourceAmount("food", 0),
                    CreateResourceAmount("industry", -1)
                });

            runtime.State.SetResourceAmount("industry", 10);
            GameStateRuleRunner.SpendResources(
                runtime.State,
                new[] { CreateResourceAmount("industry", 5) });

            AssertFacilityProgress(runtime, 0, 0, 10);
        }

        [Test]
        public void DialogueFoodRewardAdvancesFacilityProgressOnce()
        {
            GameRuntimeData runtime = CreateRuntime();
            DialogueNodeRuntimeData dialogue = new DialogueNodeRuntimeData(
                "TEST",
                null,
                "Reward",
                new[]
                {
                    new DialogueChoiceRuntimeData(
                        "Take food",
                        DialogueChoiceResultRuntimeData.AddResource("food", 15))
                });

            runtime.ShowDialogueNode(dialogue);
            Assert.That(runtime.ChooseDialogueOption(0), Is.True);
            AssertFacilityProgress(runtime, 1, 0, 10);
        }

        [Test]
        public void EventWithRepeatedIndustryRewardsAdvancesOnlyOnce()
        {
            GameRuntimeData runtime = CreateRuntime();
            GameEventDefinition gameEvent = Resources.LoadAll<GameEventDefinition>("GameData/Events")
                .Single(definition => string.Equals(
                    definition.DefinitionId,
                    "autumn_old_frontline_crash_signal",
                    StringComparison.Ordinal));
            int choiceIndex = gameEvent.Choices
                .Select((choice, index) => new { choice, index })
                .Single(item => item.choice.ChoiceId == "collect_wreckage_first")
                .index;

            runtime.ShowDialogueEvent(gameEvent);
            Assert.That(runtime.ChooseDialogueOption(choiceIndex), Is.True);
            AssertFacilityProgress(runtime, 1, 0, 10);
        }

        [Test]
        public void FacilityProgressStopsAtOneHundredPercent()
        {
            GameRuntimeData runtime = CreateRuntime();
            runtime.SetFacilityProgress("main_facility", 99, 9, 10);

            runtime.TryCollectRoadSegmentResource(
                "finishing_reward",
                new[] { CreateResourceAmount("food", 10) });
            AssertFacilityProgress(runtime, 100, 10, 10);

            runtime.TryCollectRoadSegmentResource(
                "overflow_reward",
                new[] { CreateResourceAmount("industry", 5) });
            AssertFacilityProgress(runtime, 100, 10, 10);
        }

        private FacilitySegmentedProgressBar CreateProgressBar()
        {
            GameObject barObject = new GameObject("Facility Progress Bar", typeof(RectTransform));
            createdGameObjects.Add(barObject);
            FacilitySegmentedProgressBar bar = barObject.AddComponent<FacilitySegmentedProgressBar>();
            bar.RebuildVisuals();
            Assert.That(bar.SegmentCount, Is.EqualTo(10));
            return bar;
        }

        private static GameRuntimeData CreateRuntime()
        {
            GameRuntimeData runtime = new GameRuntimeData();
            runtime.StartNewRun("facility_progress_test");
            return runtime;
        }

        private static ResourceAmount CreateResourceAmount(string resourceId, int amount)
        {
            ResourceAmount reward = new ResourceAmount();
            SetPrivateField(reward, "resourceId", resourceId);
            SetPrivateField(reward, "amount", amount);
            return reward;
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void AssertFacilityProgress(
            GameRuntimeData runtime,
            int expectedProgress,
            int expectedFilledBlocks,
            int expectedTotalBlocks)
        {
            Assert.That(
                runtime.State.GetCustomValue(GameRuntimeData.FacilityProgressValueId),
                Is.EqualTo(expectedProgress));
            Assert.That(
                runtime.Hud.Facility.ProgressPercent.Value,
                Is.EqualTo(expectedProgress));
            Assert.That(
                runtime.Hud.Facility.FilledBlockCount.Value,
                Is.EqualTo(expectedFilledBlocks));
            Assert.That(
                runtime.Hud.Facility.TotalBlockCount.Value,
                Is.EqualTo(expectedTotalBlocks));
        }

        private static void AssertAllSegments(FacilitySegmentedProgressBar bar, float expectedFill)
        {
            Assert.That(bar.SegmentCount, Is.EqualTo(10));
            for (int index = 0; index < bar.SegmentCount; index++)
            {
                Assert.That(
                    bar.GetSegmentFillAmount(index),
                    Is.EqualTo(expectedFill).Within(0.0001f),
                    "Segment " + index);
            }
        }
    }
}
