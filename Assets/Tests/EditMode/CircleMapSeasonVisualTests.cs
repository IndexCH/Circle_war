using System;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class CircleMapSeasonVisualTests
    {
        [Test]
        public void SeasonalRingSpritesHaveIdenticalDimensions()
        {
            SeasonDefinition[] seasons = Resources.LoadAll<SeasonDefinition>("GameData/Seasons");
            SeasonDefinition spring = Array.Find(
                seasons,
                season => season != null && string.Equals(season.DefinitionId, "spring", StringComparison.OrdinalIgnoreCase));
            Assert.That(spring, Is.Not.Null);

            Sprite reference = spring.CircleRingSprite;
            Assert.That(reference, Is.Not.Null);
            foreach (SeasonDefinition season in seasons)
            {
                Assert.That(season, Is.Not.Null);
                Assert.That(season.CircleRingSprite, Is.Not.Null, season.DefinitionId);
                Assert.That(season.CircleRingSprite.rect.size, Is.EqualTo(reference.rect.size), season.DefinitionId);
                Assert.That(season.CircleRingSprite.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit), season.DefinitionId);
                Assert.That(season.CircleRingSprite.bounds.size, Is.EqualTo(reference.bounds.size), season.DefinitionId);
            }
        }
    }

}
