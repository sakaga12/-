using UnityEngine;

namespace BlueLike.Core
{
    public struct DamageResult
    {
        public float amount;
        public bool isCrit;
        public bool blockedByCover;
        public bool missed;
        public float typeMultiplier;

        public string Label
        {
            get
            {
                if (missed) return "MISS";
                if (blockedByCover) return "BLOCK";
                return Mathf.RoundToInt(amount).ToString();
            }
        }
    }

    public struct DamageRequest
    {
        public UnitStats attacker;
        public UnitStats defender;
        /// <summary>Fraction of the attacker's ATK this instance of damage carries.</summary>
        public float powerRatio;
        public TerrainType terrain;
        /// <summary>True when the defender is currently behind cover.</summary>
        public bool defenderInCover;
        /// <summary>Runtime multiplier from buffs on the attacker (1 = no buff).</summary>
        public float attackerBuff;
        /// <summary>Runtime multiplier from buffs on the defender (1 = no buff).</summary>
        public float defenderDamageTaken;
        /// <summary>EX skills and similar ignore the miss roll.</summary>
        public bool cannotMiss;
    }

    /// <summary>
    /// The single place damage is computed. Nothing else in the project may invent its own
    /// formula - if a number feels wrong, it is wrong here or in <see cref="DamageTable"/>.
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Compute(DamageRequest req)
        {
            var result = new DamageResult();
            UnitStats a = req.attacker;
            UnitStats d = req.defender;

            float typeMult = DamageTable.TypeMultiplier(a.attackType, d.armorType);
            result.typeMultiplier = typeMult;

            // --- miss roll ---------------------------------------------------------------
            if (!req.cannotMiss)
            {
                float hitChance = Mathf.Clamp(a.accuracy / Mathf.Max(1f, a.accuracy + d.evasion * 0.5f), 0.25f, 1f);
                if (Random.value > hitChance)
                {
                    result.missed = true;
                    return result;
                }
            }

            // --- cover roll ---------------------------------------------------------------
            if (req.defenderInCover)
            {
                TerrainGrade grade = d.GradeFor(req.terrain);
                float coverRate = DamageTable.CoverRateFor(grade);
                // Cover only soaks the hit for ranged attackers; melee walks right up to you.
                if (a.rangeBand != RangeBand.Melee && Random.value < coverRate)
                {
                    result.blockedByCover = true;
                    return result;
                }
            }

            // --- base damage ---------------------------------------------------------------
            float terrainBonus = 1f + DamageTable.AttackBonusFor(a.GradeFor(req.terrain));
            float buff = req.attackerBuff <= 0f ? 1f : req.attackerBuff;
            float taken = req.defenderDamageTaken <= 0f ? 1f : req.defenderDamageTaken;

            float damage = a.attack * req.powerRatio * typeMult * terrainBonus * buff;
            damage *= DamageTable.DefenceMultiplier(d.defence);
            damage *= taken;

            // --- stability spread -----------------------------------------------------------
            float roll = Random.Range(a.stability, 1f);
            damage *= roll;

            // --- crit -----------------------------------------------------------------------
            float critChance = DamageTable.CritChance(a.critRate, d.critResist);
            if (Random.value < critChance)
            {
                result.isCrit = true;
                damage *= a.critDamage;
            }

            result.amount = Mathf.Max(1f, damage);
            return result;
        }

        /// <summary>Healing uses the support's healPower, ignores armour entirely.</summary>
        public static float ComputeHeal(UnitStats healer, float powerRatio)
        {
            float basis = healer.healPower > 0f ? healer.healPower : healer.attack;
            return Mathf.Max(1f, basis * powerRatio * Random.Range(healer.stability, 1f));
        }
    }
}
