using System;
using UnityEngine;

namespace BlueLike.Core
{
    /// <summary>
    /// Flat stat block for one unit at one level. Kept as a plain serializable class so the
    /// bootstrap can build a whole roster in code without you authoring a single asset file.
    /// When you move to real content authoring, wrap it in <see cref="UnitDataAsset"/>.
    /// </summary>
    [Serializable]
    public class UnitStats
    {
        [Header("Identity")]
        public string displayName = "Unit";
        public UnitRole role = UnitRole.Striker;
        public AttackType attackType = AttackType.Explosive;
        public ArmorType armorType = ArmorType.Light;
        public RangeBand rangeBand = RangeBand.Mid;

        [Header("Core stats")]
        public float maxHp = 10000f;
        public float attack = 1500f;
        public float defence = 400f;
        public float healPower = 0f;

        [Header("Critical")]
        public float critRate = 300f;
        public float critResist = 100f;
        public float critDamage = 1.50f;

        [Header("Accuracy / evasion")]
        public float accuracy = 1000f;
        public float evasion = 200f;

        [Header("Weapon")]
        [Tooltip("Shots fired before a reload cycle. Set to 0 for a weapon that never reloads.")]
        public int magazineSize = 6;
        [Tooltip("Seconds between shots inside one magazine.")]
        public float fireInterval = 0.45f;
        [Tooltip("Seconds spent reloading once the magazine is empty.")]
        public float reloadSeconds = 2.2f;
        [Tooltip("Fraction of the base attack each normal shot deals.")]
        public float normalAttackRatio = 0.55f;
        [Tooltip("Lowest roll of the damage spread. 1 means perfectly stable damage.")]
        [Range(0.3f, 1f)] public float stability = 0.80f;

        [Header("Movement")]
        public float moveSpeed = 3.2f;

        [Header("Terrain adaptation")]
        public TerrainGrade streetGrade = TerrainGrade.B;
        public TerrainGrade outdoorGrade = TerrainGrade.B;
        public TerrainGrade indoorGrade = TerrainGrade.B;

        [Header("EX skill")]
        [Tooltip("Cost charged when this unit's EX card is played.")]
        public int exCost = 3;
        public string exSkillName = "EX Skill";
        public string exSkillDescription = "Deals damage to the target.";
        [Tooltip("Fraction of base attack delivered by the EX skill, summed over all its hits.")]
        public float exPowerRatio = 4.0f;
        public int exHitCount = 1;
        [Tooltip("Radius of the EX effect in world units. 0 means single target.")]
        public float exRadius = 0f;
        public ExSkillKind exKind = ExSkillKind.SingleBurst;

        public TerrainGrade GradeFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Street: return streetGrade;
                case TerrainType.Outdoor: return outdoorGrade;
                default: return indoorGrade;
            }
        }

        /// <summary>Preferred stand-off distance in world units for this range band.</summary>
        public float PreferredDistance
        {
            get
            {
                switch (rangeBand)
                {
                    case RangeBand.Melee: return 1.6f;
                    case RangeBand.Short: return 5f;
                    case RangeBand.Mid: return 9f;
                    default: return 15f;
                }
            }
        }

        public UnitStats Clone()
        {
            return (UnitStats)MemberwiseClone();
        }
    }

    public enum ExSkillKind
    {
        /// <summary>One big hit on the current target.</summary>
        SingleBurst,
        /// <summary>Several hits spread over a short window on the current target.</summary>
        MultiHit,
        /// <summary>Damage to everything inside exRadius of the target.</summary>
        AreaBlast,
        /// <summary>Restores HP to the most wounded ally.</summary>
        Heal,
        /// <summary>Temporary attack buff for the whole squad.</summary>
        AttackBuff,
        /// <summary>Temporary damage reduction for the whole squad.</summary>
        Shield
    }
}
