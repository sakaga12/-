using UnityEngine;

namespace BlueLike.Core
{
    /// <summary>
    /// Central balance tables. Every multiplier in the combat maths lives here so you can
    /// retune the whole game from one file instead of hunting through behaviours.
    /// Values are deliberately round numbers - treat them as a starting point, not gospel.
    /// </summary>
    public static class DamageTable
    {
        // ---- Attack type vs armour type -------------------------------------------------
        // Rows: AttackType (Explosive, Piercing, Mystic, Sonic)
        // Cols: ArmorType  (Light, Heavy, Special, Elastic)
        private static readonly float[,] TypeChart =
        {
            /* Explosive */ { 2.00f, 0.50f, 1.00f, 0.90f },
            /* Piercing  */ { 0.50f, 2.00f, 1.00f, 0.90f },
            /* Mystic    */ { 1.00f, 1.00f, 2.00f, 0.90f },
            /* Sonic     */ { 1.00f, 1.00f, 0.80f, 2.00f },
        };

        public static float TypeMultiplier(AttackType attack, ArmorType armor)
        {
            return TypeChart[(int)attack, (int)armor];
        }

        /// <summary>Human readable label for the HUD ("EFFECTIVE" / "RESIST").</summary>
        public static string TypeLabel(AttackType attack, ArmorType armor)
        {
            float m = TypeMultiplier(attack, armor);
            if (m >= 1.5f) return "EFFECTIVE";
            if (m <= 0.75f) return "RESIST";
            return string.Empty;
        }

        // ---- Terrain adaptation ---------------------------------------------------------
        // Attack bonus applied to outgoing damage, indexed by TerrainGrade.
        private static readonly float[] TerrainAttackBonus = { 0.30f, 0.20f, 0.10f, 0.00f, -0.20f };

        // Chance that an incoming hit is soaked by cover, indexed by TerrainGrade.
        private static readonly float[] TerrainCoverRate = { 0.80f, 0.75f, 0.70f, 0.60f, 0.00f };

        public static float AttackBonusFor(TerrainGrade grade) => TerrainAttackBonus[(int)grade];
        public static float CoverRateFor(TerrainGrade grade) => TerrainCoverRate[(int)grade];

        // ---- Defence curve --------------------------------------------------------------
        /// <summary>
        /// Scale constant for the DEF softening curve. Higher means armour matters less.
        /// Reduction = DEF / (DEF + K), so at DEF == K an attacker loses exactly half its damage.
        /// </summary>
        public const float DefenceConstant = 1200f;

        public static float DefenceMultiplier(float defence)
        {
            if (defence <= 0f) return 1f;
            return DefenceConstant / (DefenceConstant + defence);
        }

        // ---- Critical hits --------------------------------------------------------------
        public const float CritConstant = 1000f;

        /// <summary>
        /// Effective crit chance after the defender's resistance is applied.
        /// Both values are flat stat points, not percentages.
        /// </summary>
        public static float CritChance(float critRate, float critResist)
        {
            float net = Mathf.Max(0f, critRate - critResist);
            return Mathf.Clamp01(net / (net + CritConstant));
        }

        // ---- Cost economy ---------------------------------------------------------------
        public const float MaxCost = 10f;
        /// <summary>Cost points regenerated per second at the baseline regen stat.</summary>
        public const float BaseCostRegenPerSecond = 0.70f;
        /// <summary>Cost the squad starts the battle with.</summary>
        public const float StartingCost = 3f;

        // ---- Battle pacing --------------------------------------------------------------
        public const float DefaultBattleSeconds = 180f;
        /// <summary>Seconds a used EX card waits before its owner can be drawn again.</summary>
        public const float DefaultCardCooldown = 6f;
    }
}
