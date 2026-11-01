using UnityEngine;
using BlueLike.Battle;

namespace BlueLike.Skills
{
    /// <summary>One EX skill card belonging to one Striker.</summary>
    public class SkillCard
    {
        public BattleUnit Owner;
        public int Cost;
        public string SkillName;
        public string Description;

        /// <summary>Game time before which this card cannot be drawn again.</summary>
        public float AvailableAt;

        public bool IsReady => Time.time >= AvailableAt;
        public bool IsPlayable => Owner != null && !Owner.IsDead;

        public SkillCard(BattleUnit owner)
        {
            Owner = owner;
            Cost = owner.Stats.exCost;
            SkillName = owner.Stats.exSkillName;
            Description = owner.Stats.exSkillDescription;
            AvailableAt = 0f;
        }
    }
}
