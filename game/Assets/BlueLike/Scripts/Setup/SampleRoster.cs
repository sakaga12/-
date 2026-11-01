using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;

namespace BlueLike.Setup
{
    /// <summary>
    /// The placeholder squad and enemy wave. These are deliberately original designs - swap
    /// the names, numbers and colours freely, this file is meant to be edited.
    ///
    /// The roster is built to teach the type chart: each Striker covers one attack type, and
    /// the enemy wave contains one of each armour class, so you can see 2x and 0.5x land.
    /// </summary>
    public static class SampleRoster
    {
        public struct Entry
        {
            public UnitStats stats;
            public Color bodyColor;
            public Color haloColor;
            public int lane;
        }

        private static readonly Color Rose = new Color(0.96f, 0.46f, 0.60f);
        private static readonly Color Azure = new Color(0.42f, 0.66f, 0.98f);
        private static readonly Color Violet = new Color(0.69f, 0.52f, 0.96f);
        private static readonly Color Amber = new Color(0.98f, 0.76f, 0.36f);
        private static readonly Color Mint = new Color(0.47f, 0.92f, 0.76f);
        private static readonly Color Ivory = new Color(0.94f, 0.92f, 0.88f);

        public static List<Entry> BuildPlayerSquad()
        {
            var squad = new List<Entry>();

            // --- Strikers ----------------------------------------------------------------
            squad.Add(new Entry
            {
                lane = 1,
                bodyColor = Rose,
                haloColor = Rose,
                stats = new UnitStats
                {
                    displayName = "Hazuki",
                    role = UnitRole.Striker,
                    attackType = AttackType.Explosive,
                    armorType = ArmorType.Light,
                    rangeBand = RangeBand.Mid,
                    maxHp = 11800f, attack = 1620f, defence = 380f,
                    magazineSize = 25, fireInterval = 0.14f, reloadSeconds = 2.0f,
                    normalAttackRatio = 0.22f, stability = 0.72f, moveSpeed = 3.4f,
                    streetGrade = TerrainGrade.A, outdoorGrade = TerrainGrade.B, indoorGrade = TerrainGrade.C,
                    exCost = 3, exSkillName = "Suppressing Volley",
                    exSkillDescription = "Pours fire into the target zone.",
                    exKind = ExSkillKind.MultiHit, exPowerRatio = 4.2f, exHitCount = 6
                }
            });

            squad.Add(new Entry
            {
                lane = 0,
                bodyColor = Azure,
                haloColor = Azure,
                stats = new UnitStats
                {
                    displayName = "Mizuki",
                    role = UnitRole.Striker,
                    attackType = AttackType.Piercing,
                    armorType = ArmorType.Light,
                    rangeBand = RangeBand.Long,
                    maxHp = 9400f, attack = 2240f, defence = 300f,
                    magazineSize = 4, fireInterval = 0.95f, reloadSeconds = 2.8f,
                    normalAttackRatio = 1.05f, stability = 0.92f, moveSpeed = 2.8f,
                    critRate = 520f, critDamage = 1.80f,
                    streetGrade = TerrainGrade.B, outdoorGrade = TerrainGrade.S, indoorGrade = TerrainGrade.D,
                    exCost = 4, exSkillName = "Piercing Shot",
                    exSkillDescription = "One armour-breaking round.",
                    exKind = ExSkillKind.SingleBurst, exPowerRatio = 7.5f, exHitCount = 1
                }
            });

            squad.Add(new Entry
            {
                lane = 2,
                bodyColor = Violet,
                haloColor = Violet,
                stats = new UnitStats
                {
                    displayName = "Saera",
                    role = UnitRole.Striker,
                    attackType = AttackType.Mystic,
                    armorType = ArmorType.Special,
                    rangeBand = RangeBand.Short,
                    maxHp = 10200f, attack = 1780f, defence = 340f,
                    magazineSize = 30, fireInterval = 0.11f, reloadSeconds = 1.8f,
                    normalAttackRatio = 0.18f, stability = 0.68f, moveSpeed = 3.8f,
                    streetGrade = TerrainGrade.S, outdoorGrade = TerrainGrade.C, indoorGrade = TerrainGrade.A,
                    exCost = 4, exSkillName = "Resonance Burst",
                    exSkillDescription = "Detonates a mystic field around the target.",
                    exKind = ExSkillKind.AreaBlast, exPowerRatio = 5.4f, exHitCount = 2, exRadius = 4.5f
                }
            });

            squad.Add(new Entry
            {
                lane = 1,
                bodyColor = Amber,
                haloColor = Amber,
                stats = new UnitStats
                {
                    displayName = "Tsumugi",
                    role = UnitRole.Striker,
                    attackType = AttackType.Sonic,
                    armorType = ArmorType.Heavy,
                    rangeBand = RangeBand.Melee,
                    maxHp = 19600f, attack = 1240f, defence = 980f,
                    magazineSize = 2, fireInterval = 0.8f, reloadSeconds = 2.4f,
                    normalAttackRatio = 1.10f, stability = 0.85f, moveSpeed = 3.0f,
                    critResist = 320f,
                    streetGrade = TerrainGrade.A, outdoorGrade = TerrainGrade.C, indoorGrade = TerrainGrade.S,
                    exCost = 2, exSkillName = "Bulwark",
                    exSkillDescription = "Shields the squad against incoming fire.",
                    exKind = ExSkillKind.Shield, exPowerRatio = 4.0f
                }
            });

            // --- Specials ----------------------------------------------------------------
            squad.Add(new Entry
            {
                lane = 0,
                bodyColor = Mint,
                haloColor = Mint,
                stats = new UnitStats
                {
                    displayName = "Nagisa",
                    role = UnitRole.Special,
                    attackType = AttackType.Mystic,
                    armorType = ArmorType.Light,
                    rangeBand = RangeBand.Long,
                    maxHp = 8800f, attack = 980f, defence = 260f, healPower = 2400f,
                    magazineSize = 8, fireInterval = 0.55f, reloadSeconds = 2.4f,
                    normalAttackRatio = 0.35f, moveSpeed = 2.6f,
                    exSkillName = "Field Treatment",
                    exSkillDescription = "Heals the most wounded ally.",
                    exKind = ExSkillKind.Heal, exPowerRatio = 1.6f
                }
            });

            squad.Add(new Entry
            {
                lane = 2,
                bodyColor = Ivory,
                haloColor = new Color(1f, 0.85f, 0.55f),
                stats = new UnitStats
                {
                    displayName = "Kotoha",
                    role = UnitRole.Special,
                    attackType = AttackType.Explosive,
                    armorType = ArmorType.Light,
                    rangeBand = RangeBand.Long,
                    maxHp = 9100f, attack = 1120f, defence = 280f,
                    magazineSize = 10, fireInterval = 0.5f, reloadSeconds = 2.2f,
                    normalAttackRatio = 0.40f, moveSpeed = 2.6f,
                    exSkillName = "Fire Support Order",
                    exSkillDescription = "Raises the squad's attack.",
                    exKind = ExSkillKind.AttackBuff, exPowerRatio = 3.0f
                }
            });

            return squad;
        }

        /// <summary>
        /// One enemy per armour class, so the type chart is visible the moment you press play.
        /// </summary>
        public static List<Entry> BuildEnemyWave()
        {
            var wave = new List<Entry>();

            wave.Add(new Entry
            {
                lane = 0,
                bodyColor = new Color(0.86f, 0.36f, 0.34f),
                haloColor = new Color(0.9f, 0.3f, 0.3f),
                stats = new UnitStats
                {
                    displayName = "Scout",
                    role = UnitRole.Striker,
                    attackType = AttackType.Explosive,
                    armorType = ArmorType.Light,
                    rangeBand = RangeBand.Mid,
                    maxHp = 14000f, attack = 1150f, defence = 240f,
                    magazineSize = 20, fireInterval = 0.18f, reloadSeconds = 2.2f,
                    normalAttackRatio = 0.20f, moveSpeed = 2.4f,
                    streetGrade = TerrainGrade.B, outdoorGrade = TerrainGrade.B, indoorGrade = TerrainGrade.B
                }
            });

            wave.Add(new Entry
            {
                lane = 1,
                bodyColor = new Color(0.56f, 0.56f, 0.62f),
                haloColor = new Color(0.7f, 0.7f, 0.8f),
                stats = new UnitStats
                {
                    displayName = "Bulwark",
                    role = UnitRole.Striker,
                    attackType = AttackType.Piercing,
                    armorType = ArmorType.Heavy,
                    rangeBand = RangeBand.Short,
                    maxHp = 34000f, attack = 1480f, defence = 1250f,
                    magazineSize = 6, fireInterval = 0.5f, reloadSeconds = 2.6f,
                    normalAttackRatio = 0.55f, moveSpeed = 1.9f, critResist = 400f,
                    streetGrade = TerrainGrade.B, outdoorGrade = TerrainGrade.B, indoorGrade = TerrainGrade.B
                }
            });

            wave.Add(new Entry
            {
                lane = 2,
                bodyColor = new Color(0.55f, 0.34f, 0.78f),
                haloColor = new Color(0.72f, 0.4f, 1f),
                stats = new UnitStats
                {
                    displayName = "Caster",
                    role = UnitRole.Striker,
                    attackType = AttackType.Mystic,
                    armorType = ArmorType.Special,
                    rangeBand = RangeBand.Long,
                    maxHp = 17000f, attack = 1620f, defence = 420f,
                    magazineSize = 5, fireInterval = 0.7f, reloadSeconds = 3.0f,
                    normalAttackRatio = 0.70f, moveSpeed = 2.0f,
                    streetGrade = TerrainGrade.B, outdoorGrade = TerrainGrade.B, indoorGrade = TerrainGrade.B
                }
            });

            wave.Add(new Entry
            {
                lane = 1,
                bodyColor = new Color(0.92f, 0.60f, 0.26f),
                haloColor = new Color(1f, 0.6f, 0.2f),
                stats = new UnitStats
                {
                    displayName = "Breaker",
                    role = UnitRole.Striker,
                    attackType = AttackType.Sonic,
                    armorType = ArmorType.Elastic,
                    rangeBand = RangeBand.Melee,
                    maxHp = 26000f, attack = 1700f, defence = 760f,
                    magazineSize = 3, fireInterval = 0.65f, reloadSeconds = 2.2f,
                    normalAttackRatio = 0.85f, moveSpeed = 2.8f,
                    streetGrade = TerrainGrade.B, outdoorGrade = TerrainGrade.B, indoorGrade = TerrainGrade.B
                }
            });

            return wave;
        }
    }
}
