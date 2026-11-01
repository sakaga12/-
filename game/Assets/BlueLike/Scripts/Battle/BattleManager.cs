using System;
using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;
using BlueLike.Skills;

namespace BlueLike.Battle
{
    /// <summary>
    /// Owns the battle: rosters, the clock, the phase machine, targeting queries and the
    /// hit reporting channel the presentation layer listens to.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Stage")]
        public TerrainType Terrain = TerrainType.Outdoor;
        public float BattleSeconds = DamageTable.DefaultBattleSeconds;

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
        public float TimeRemaining { get; private set; }

        public readonly List<BattleUnit> PlayerUnits = new List<BattleUnit>();
        public readonly List<BattleUnit> EnemyUnits = new List<BattleUnit>();

        public CostManager Cost { get; private set; }
        public SkillCardManager Cards { get; private set; }

        /// <summary>Raised for every resolved hit so the HUD can throw up a damage number.</summary>
        public event Action<BattleUnit, BattleUnit, DamageResult, bool> HitReported;
        public event Action<BattlePhase> PhaseChanged;

        private void Awake()
        {
            Instance = this;
            Cost = GetComponent<CostManager>() ?? gameObject.AddComponent<CostManager>();
            Cards = GetComponent<SkillCardManager>() ?? gameObject.AddComponent<SkillCardManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterUnit(BattleUnit unit)
        {
            List<BattleUnit> list = unit.Faction == Faction.Player ? PlayerUnits : EnemyUnits;
            if (!list.Contains(unit)) list.Add(unit);
            unit.Died += OnUnitDied;
        }

        public void StartBattle()
        {
            TimeRemaining = BattleSeconds;
            Cards.BuildDeck(PlayerUnits);
            Cost.ResetCost();
            SetPhase(BattlePhase.Fighting);
        }

        private void Update()
        {
            if (Phase != BattlePhase.Fighting && Phase != BattlePhase.ExCinematic) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                // Running out of time is a loss, same as a wipe.
                SetPhase(BattlePhase.Defeat);
            }
        }

        private void OnUnitDied(BattleUnit unit)
        {
            if (AllDead(EnemyUnits)) SetPhase(BattlePhase.Victory);
            else if (AllDead(PlayerUnits)) SetPhase(BattlePhase.Defeat);
        }

        private static bool AllDead(List<BattleUnit> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].IsDead) return false;
            }
            return true;
        }

        public void SetPhase(BattlePhase phase)
        {
            if (Phase == phase) return;
            if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat) return; // terminal
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        // ---------------------------------------------------------------------------------
        // Targeting
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Nearest living enemy, preferring the unit's own lane so the three lanes read as
        /// three separate fights rather than one blob.
        /// </summary>
        public BattleUnit FindTargetFor(BattleUnit unit)
        {
            List<BattleUnit> pool = unit.Faction == Faction.Player ? EnemyUnits : PlayerUnits;
            BattleUnit best = null;
            float bestScore = float.MaxValue;
            Vector3 from = unit.transform.position;

            for (int i = 0; i < pool.Count; i++)
            {
                BattleUnit candidate = pool[i];
                if (candidate == null || candidate.IsDead) continue;

                float score = Mathf.Abs(candidate.transform.position.x - from.x);
                if (candidate.Lane != unit.Lane) score += 6f; // same-lane preference
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        public List<BattleUnit> LivingEnemiesOf(Faction faction)
        {
            List<BattleUnit> source = faction == Faction.Player ? EnemyUnits : PlayerUnits;
            var result = new List<BattleUnit>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !source[i].IsDead) result.Add(source[i]);
            }
            return result;
        }

        public List<BattleUnit> LivingAlliesOf(Faction faction)
        {
            List<BattleUnit> source = faction == Faction.Player ? PlayerUnits : EnemyUnits;
            var result = new List<BattleUnit>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !source[i].IsDead) result.Add(source[i]);
            }
            return result;
        }

        public void ReportHit(BattleUnit attacker, BattleUnit defender, DamageResult result, bool fromEx)
        {
            HitReported?.Invoke(attacker, defender, result, fromEx);
        }

        /// <summary>Midpoint of the fighting, used by the camera rig.</summary>
        public Vector3 ActionCentre
        {
            get
            {
                float sum = 0f;
                int count = 0;
                for (int i = 0; i < PlayerUnits.Count; i++)
                {
                    if (PlayerUnits[i] != null && !PlayerUnits[i].IsDead)
                    {
                        sum += PlayerUnits[i].transform.position.x;
                        count++;
                    }
                }
                if (count == 0) return Vector3.zero;
                return new Vector3(sum / count, 0f, 0f);
            }
        }
    }
}
