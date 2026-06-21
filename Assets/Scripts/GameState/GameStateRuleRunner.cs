using System;
using System.Collections.Generic;

namespace CircleWar
{
    public static class GameStateRuleRunner
    {
        public static bool AreConditionsMet(GameState gameState, IEnumerable<GameCondition> conditions)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (conditions == null)
            {
                return true;
            }

            foreach (GameCondition condition in conditions)
            {
                if (condition != null && !IsConditionMet(gameState, condition))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsConditionMet(GameState gameState, GameCondition condition)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (condition == null)
            {
                return true;
            }

            switch (condition.TargetType)
            {
                case ConditionTargetType.Resource:
                    return CompareInt(gameState.GetResourceAmount(condition.TargetId), condition.Value, condition.Comparison);
                case ConditionTargetType.Flag:
                    return CompareBool(gameState.GetFlag(condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.CharacterFavor:
                    return CompareInt(gameState.GetCharacterFavor(condition.TargetId), condition.Value, condition.Comparison);
                case ConditionTargetType.EventCompleted:
                    return CompareBool(gameState.IsEventCompleted(condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.RegionVisited:
                    return CompareBool(gameState.IsRegionVisited(condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.FacilityBuilt:
                    return CompareBool(gameState.IsFacilityModuleBuilt(condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.EnemyDefeatCount:
                    return CompareInt(gameState.GetEnemyDefeatCount(condition.TargetId), condition.Value, condition.Comparison);
                case ConditionTargetType.BossDefeated:
                    return CompareBool(gameState.IsBossDefeated(condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.EscapeInclination:
                    return CompareInt(gameState.EscapeInclination, condition.Value, condition.Comparison);
                case ConditionTargetType.MilitarizationInclination:
                    return CompareInt(gameState.MilitarizationInclination, condition.Value, condition.Comparison);
                case ConditionTargetType.Day:
                    return CompareInt(gameState.CurrentDay, condition.Value, condition.Comparison);
                case ConditionTargetType.Season:
                    return CompareBool(HasId(gameState.CurrentSeasonId, condition.TargetId), condition.BoolValue, condition.Comparison);
                case ConditionTargetType.CustomValue:
                    return CompareInt(gameState.GetCustomValue(condition.TargetId), condition.Value, condition.Comparison);
                default:
                    return false;
            }
        }

        public static bool HasResources(GameState gameState, IEnumerable<ResourceAmount> costs)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (costs == null)
            {
                return true;
            }

            foreach (ResourceAmount cost in costs)
            {
                if (cost != null && gameState.GetResourceAmount(cost.ResourceId) < cost.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        public static void SpendResources(GameState gameState, IEnumerable<ResourceAmount> costs)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (costs == null)
            {
                return;
            }

            foreach (ResourceAmount cost in costs)
            {
                if (cost != null)
                {
                    gameState.AddResource(cost.ResourceId, -cost.Amount);
                }
            }
        }

        public static void ApplyEffects(GameState gameState, IEnumerable<GameEffect> effects)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (effects == null)
            {
                return;
            }

            foreach (GameEffect effect in effects)
            {
                ApplyEffect(gameState, effect);
            }
        }

        public static void ApplyEffect(GameState gameState, GameEffect effect)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (effect == null)
            {
                return;
            }

            switch (effect.EffectType)
            {
                case GameEffectType.AddResource:
                    gameState.AddResource(effect.TargetId, effect.Amount);
                    break;
                case GameEffectType.SetResource:
                    gameState.SetResourceAmount(effect.TargetId, effect.Amount);
                    break;
                case GameEffectType.AddCharacterFavor:
                    gameState.AddCharacterFavor(effect.TargetId, effect.Amount);
                    break;
                case GameEffectType.SetFlag:
                    gameState.SetFlag(effect.TargetId, effect.BoolValue);
                    break;
                case GameEffectType.MarkEventCompleted:
                    gameState.MarkEventCompleted(effect.TargetId);
                    break;
                case GameEffectType.MarkRegionVisited:
                    gameState.MarkRegionVisited(effect.TargetId);
                    break;
                case GameEffectType.BuildFacilityModule:
                    gameState.MarkFacilityModuleBuilt(effect.TargetId);
                    break;
                case GameEffectType.UnlockRegion:
                    gameState.MarkRegionUnlocked(effect.TargetId);
                    break;
                case GameEffectType.StartBoss:
                    gameState.StartBoss(effect.TargetId, effect.Amount, effect.TextValue);
                    break;
                case GameEffectType.CompleteBoss:
                    gameState.MarkBossDefeated(effect.TargetId);
                    break;
                case GameEffectType.AddEscapeInclination:
                    gameState.AddEscapeInclination(effect.Amount);
                    break;
                case GameEffectType.SetEscapeInclination:
                    gameState.SetEscapeInclination(effect.Amount);
                    break;
                case GameEffectType.AddMilitarizationInclination:
                    gameState.AddMilitarizationInclination(effect.Amount);
                    break;
                case GameEffectType.SetMilitarizationInclination:
                    gameState.SetMilitarizationInclination(effect.Amount);
                    break;
                case GameEffectType.AddCustomValue:
                    gameState.AddCustomValue(effect.TargetId, effect.Amount);
                    break;
                case GameEffectType.SetCustomValue:
                    gameState.SetCustomValue(effect.TargetId, effect.Amount);
                    break;
            }
        }

        private static bool CompareInt(int currentValue, int expectedValue, ValueComparison comparison)
        {
            switch (comparison)
            {
                case ValueComparison.Equal:
                    return currentValue == expectedValue;
                case ValueComparison.NotEqual:
                    return currentValue != expectedValue;
                case ValueComparison.GreaterThan:
                    return currentValue > expectedValue;
                case ValueComparison.GreaterOrEqual:
                    return currentValue >= expectedValue;
                case ValueComparison.LessThan:
                    return currentValue < expectedValue;
                case ValueComparison.LessOrEqual:
                    return currentValue <= expectedValue;
                default:
                    return false;
            }
        }

        private static bool CompareBool(bool currentValue, bool expectedValue, ValueComparison comparison)
        {
            if (comparison == ValueComparison.NotEqual)
            {
                return currentValue != expectedValue;
            }

            return currentValue == expectedValue;
        }

        private static bool HasId(string currentId, string expectedId)
        {
            return string.Equals(currentId, expectedId, StringComparison.Ordinal);
        }
    }
}
