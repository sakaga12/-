using UnityEngine;
using BlueLike.Core;

namespace BlueLike.Skills
{
    /// <summary>
    /// The shared cost pool that gates EX skills. It fills continuously during the fight,
    /// which is what makes the battle a resource-timing game instead of a cooldown game.
    /// </summary>
    public class CostManager : MonoBehaviour
    {
        [Tooltip("Cost points regenerated per second. Raise this to make fights more spammy.")]
        public float RegenPerSecond = DamageTable.BaseCostRegenPerSecond;

        [Tooltip("Multiplier applied to regen, e.g. from a Special's passive.")]
        public float RegenMultiplier = 1f;

        public float Current { get; private set; }
        public float Max => DamageTable.MaxCost;

        /// <summary>Whole cost points available to spend right now.</summary>
        public int Usable => Mathf.FloorToInt(Current);

        /// <summary>0..1 fill of the fractional part, for the segmented HUD gauge.</summary>
        public float PartialFill => Current - Mathf.Floor(Current);

        public void ResetCost()
        {
            Current = DamageTable.StartingCost;
        }

        private void Update()
        {
            var mgr = Battle.BattleManager.Instance;
            if (mgr == null) return;
            if (mgr.Phase != BattlePhase.Fighting) return;

            Current = Mathf.Min(Max, Current + RegenPerSecond * RegenMultiplier * Time.deltaTime);
        }

        public bool CanAfford(int cost) => Usable >= cost;

        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Current -= cost;
            if (Current < 0f) Current = 0f;
            return true;
        }

        /// <summary>Grants cost directly, for skills and passives that refund.</summary>
        public void Grant(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0f, Max);
        }
    }
}
