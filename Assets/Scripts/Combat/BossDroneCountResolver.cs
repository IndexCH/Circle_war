using System;

namespace CircleWar
{
    public static class BossDroneCountResolver
    {
        public const string ReductionValueId = "boss_drone_reduction";
        public const int DefaultDroneCount = 4;
        public const int MinimumDroneCount = 1;

        public static int Resolve(GameState gameState)
        {
            int reduction = gameState == null ? 0 : gameState.GetCustomValue(ReductionValueId);
            return Resolve(reduction);
        }

        public static int Resolve(int reduction)
        {
            int safeReduction = Math.Max(0, reduction);
            return Math.Max(MinimumDroneCount, DefaultDroneCount - safeReduction);
        }
    }
}
