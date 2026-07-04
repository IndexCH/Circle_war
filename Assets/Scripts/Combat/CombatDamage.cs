using UnityEngine;

namespace CircleWar
{
    public static class CombatDamage
    {
        public static bool TryApplyPlayerDamage(GameHud gameHud, int damage)
        {
            int safeDamage = Mathf.Max(0, damage);
            if (safeDamage <= 0)
            {
                return false;
            }

            GameHud resolvedGameHud = gameHud != null ? gameHud : Object.FindAnyObjectByType<GameHud>();
            if (resolvedGameHud == null)
            {
                return false;
            }

            GameRuntimeData runtimeData = resolvedGameHud.RuntimeData;
            HudPlayerStatsRuntimeData playerStats = runtimeData.Hud.PlayerStats;
            runtimeData.SetPlayerStats(
                playerStats.Hp.Value - safeDamage,
                playerStats.MaxHp.Value,
                playerStats.Food.Value,
                playerStats.Materials.Value,
                "under_attack",
                "UNDER ATTACK");
            return true;
        }
    }
}
