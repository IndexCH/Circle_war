using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircleWar
{
    public interface ICombatEnemy
    {
        bool IsAlive { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
        Vector2 WorldPosition { get; }
        float HitRadius { get; }
        CircleMapView CircleMapView { get; }
        bool TryTakeDamage(int damage);
    }

    public sealed class CombatEnemyProgressBinding
    {
        private readonly Action<int, int> healthChanged;
        private readonly Action defeated;
        private bool hasReportedDefeat;

        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }

        public CombatEnemyProgressBinding(
            int maxHealth,
            int currentHealth,
            Action<int, int> healthChanged,
            Action defeated,
            bool isAlreadyDefeated = false)
        {
            MaxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
            this.healthChanged = healthChanged;
            this.defeated = defeated;
            hasReportedDefeat = isAlreadyDefeated;
        }

        public int ApplyDamage(int damage)
        {
            int safeDamage = Mathf.Max(0, damage);
            if (safeDamage <= 0 || CurrentHealth <= 0)
            {
                return CurrentHealth;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - safeDamage);
            healthChanged?.Invoke(CurrentHealth, MaxHealth);
            return CurrentHealth;
        }

        public void ReportDefeated()
        {
            if (hasReportedDefeat)
            {
                return;
            }

            hasReportedDefeat = true;
            if (CurrentHealth != 0)
            {
                CurrentHealth = 0;
                healthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            defeated?.Invoke();
        }
    }

    public static class CombatEnemyRegistry
    {
        private static readonly List<ICombatEnemy> Enemies = new List<ICombatEnemy>();

        public static void Register(ICombatEnemy enemy)
        {
            if (enemy == null || Enemies.Contains(enemy))
            {
                return;
            }

            Enemies.Add(enemy);
        }

        public static void Unregister(ICombatEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            Enemies.Remove(enemy);
        }

        public static bool TryHitEnemy(CircleMapView mapView, Vector2 startWorldPosition, Vector2 endWorldPosition, float hitRadius, int damage)
        {
            for (int index = Enemies.Count - 1; index >= 0; index--)
            {
                ICombatEnemy enemy = Enemies[index];
                if (IsMissing(enemy))
                {
                    Enemies.RemoveAt(index);
                    continue;
                }

                if (!enemy.IsAlive)
                {
                    continue;
                }

                CircleMapView enemyMapView = enemy.CircleMapView;
                if (mapView != null && enemyMapView != null && !ReferenceEquals(mapView, enemyMapView))
                {
                    continue;
                }

                float combinedRadius = Mathf.Max(0f, hitRadius) + Mathf.Max(0f, enemy.HitRadius);
                if (!CombatHitMath.SegmentIntersectsCircle(startWorldPosition, endWorldPosition, enemy.WorldPosition, combinedRadius))
                {
                    continue;
                }

                enemy.TryTakeDamage(damage);
                return true;
            }

            return false;
        }

        public static bool TryHitEnemyInView(CircleMapView mapView, Vector2 startViewPosition, Vector2 endViewPosition, float hitRadius, int damage)
        {
            for (int index = Enemies.Count - 1; index >= 0; index--)
            {
                ICombatEnemy enemy = Enemies[index];
                if (IsMissing(enemy))
                {
                    Enemies.RemoveAt(index);
                    continue;
                }

                if (!enemy.IsAlive)
                {
                    continue;
                }

                CircleMapView enemyMapView = enemy.CircleMapView;
                if (mapView != null && enemyMapView != null && !ReferenceEquals(mapView, enemyMapView))
                {
                    continue;
                }

                Vector2 enemyViewPosition = GetEnemyViewPosition(enemy, enemyMapView);
                float combinedRadius = Mathf.Max(0f, hitRadius) + Mathf.Max(0f, enemy.HitRadius);
                if (!CombatHitMath.SegmentIntersectsCircle(startViewPosition, endViewPosition, enemyViewPosition, combinedRadius))
                {
                    continue;
                }

                enemy.TryTakeDamage(damage);
                return true;
            }

            return false;
        }

        private static Vector2 GetEnemyViewPosition(ICombatEnemy enemy, CircleMapView enemyMapView)
        {
            if (enemy is Component component)
            {
                return component.transform.position;
            }

            return enemyMapView != null ? enemyMapView.WorldToViewPosition(enemy.WorldPosition) : enemy.WorldPosition;
        }

        private static bool IsMissing(ICombatEnemy enemy)
        {
            if (enemy == null)
            {
                return true;
            }

            if (enemy is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            return false;
        }
    }
}
