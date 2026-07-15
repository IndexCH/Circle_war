using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class BossDroneConfigurationTests
    {
        [TestCase(0, 4)]
        [TestCase(1, 3)]
        [TestCase(-1, 4)]
        [TestCase(20, 1)]
        public void DroneCountUsesFourAsDefaultAndAppliesReduction(int reduction, int expectedCount)
        {
            Assert.That(BossDroneCountResolver.Resolve(reduction), Is.EqualTo(expectedCount));
        }

        [Test]
        public void DroneCountReadsReductionFromGameState()
        {
            GameState state = new GameState();
            state.AddCustomValue(BossDroneCountResolver.ReductionValueId, 1);

            Assert.That(BossDroneCountResolver.Resolve(state), Is.EqualTo(3));
        }

        [Test]
        public void DroneCountDefaultsWhenGameStateIsUnavailable()
        {
            Assert.That(
                BossDroneCountResolver.Resolve((GameState)null),
                Is.EqualTo(BossDroneCountResolver.DefaultDroneCount));
        }

        [Test]
        public void PatrolDroneRebootChoiceAddsOneBossDroneReduction()
        {
            GameEventDefinition patrolDrone = Resources.LoadAll<GameEventDefinition>("GameData/Events")
                .Single(gameEvent => string.Equals(
                    gameEvent.DefinitionId,
                    "summer_low_humidity_patrol_drone",
                    StringComparison.OrdinalIgnoreCase));
            GameEventChoiceDefinition rebootChoice = patrolDrone.Choices.Single(choice => choice.ChoiceId == "reboot_drone");
            GameEffect reduction = rebootChoice.Results.Single(result =>
                result.EffectType == GameEffectType.AddCustomValue &&
                result.TargetId == BossDroneCountResolver.ReductionValueId);

            Assert.That(reduction.Amount, Is.EqualTo(1));
        }

    }
}
