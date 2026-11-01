using System;
using UnityEngine;
using BlueLike.Core;

namespace BlueLike.Battle
{
    /// <summary>
    /// A single combatant. Holds state and executes orders; the decision making lives in
    /// <see cref="UnitBrain"/> so you can swap the AI without touching the body.
    /// </summary>
    public class BattleUnit : MonoBehaviour
    {
        [SerializeField] private UnitStats stats = new UnitStats();

        public UnitStats Stats => stats;
        public Faction Faction { get; private set; }
        public int Lane { get; private set; }
        public UnitState State { get; set; }
        public float CurrentHp { get; private set; }
        public bool IsDead => State == UnitState.Dead;
        public float HpFraction => stats.maxHp <= 0f ? 0f : Mathf.Clamp01(CurrentHp / stats.maxHp);

        /// <summary>+X for the player side, -X for the enemy side.</summary>
        public Vector3 Forward => Faction == Faction.Player ? Vector3.right : Vector3.left;

        public CoverPoint Cover { get; set; }
        public bool InCover { get; set; }
        public BattleUnit Target { get; set; }

        /// <summary>Where projectiles and muzzle flashes originate. Falls back to chest height.</summary>
        public Transform Muzzle;
        /// <summary>Root of the visual model, so presentation code can swap or tint it.</summary>
        public Transform ModelRoot;
        public Renderer BodyRenderer;

        // --- weapon cycle -------------------------------------------------------------------
        public int AmmoInMagazine { get; private set; }
        private float _nextShotTime;
        private float _reloadDoneTime;

        // --- buffs --------------------------------------------------------------------------
        private float _attackBuff = 1f;
        private float _attackBuffUntil;
        private float _damageTaken = 1f;
        private float _damageTakenUntil;

        public float AttackBuff => Time.time < _attackBuffUntil ? _attackBuff : 1f;
        public float DamageTakenMultiplier => Time.time < _damageTakenUntil ? _damageTaken : 1f;

        public event Action<BattleUnit> Died;
        public event Action<BattleUnit, DamageResult> DamageTakenEvent;

        public void Initialise(UnitStats source, Faction faction, int lane)
        {
            stats = source.Clone();
            Faction = faction;
            Lane = lane;
            CurrentHp = stats.maxHp;
            AmmoInMagazine = stats.magazineSize <= 0 ? int.MaxValue : stats.magazineSize;
            State = UnitState.Idle;
            name = (faction == Faction.Player ? "P_" : "E_") + stats.displayName;
        }

        // ---------------------------------------------------------------------------------
        // Movement
        // ---------------------------------------------------------------------------------

        /// <summary>Walks toward a world X, staying inside its own lane. Returns true on arrival.</summary>
        public bool MoveTowardX(float worldX, float deltaTime)
        {
            Vector3 pos = transform.position;
            float delta = worldX - pos.x;
            if (Mathf.Abs(delta) < 0.12f) return true;

            float step = stats.moveSpeed * deltaTime;
            pos.x += Mathf.Clamp(delta, -step, step);
            transform.position = pos;
            FaceEnemySide();
            return false;
        }

        public void FaceEnemySide()
        {
            if (ModelRoot == null) return;
            ModelRoot.rotation = Quaternion.LookRotation(Forward, Vector3.up);
        }

        // ---------------------------------------------------------------------------------
        // Weapon
        // ---------------------------------------------------------------------------------

        public bool CanFire => !IsDead && Time.time >= _nextShotTime && State != UnitState.Reloading
                                       && State != UnitState.CastingEx;

        public bool NeedsReload => stats.magazineSize > 0 && AmmoInMagazine <= 0;

        public void BeginReload()
        {
            State = UnitState.Reloading;
            _reloadDoneTime = Time.time + stats.reloadSeconds;
        }

        public bool TickReload()
        {
            if (Time.time < _reloadDoneTime) return false;
            AmmoInMagazine = stats.magazineSize;
            State = UnitState.Idle;
            return true;
        }

        /// <summary>Fires one normal shot at the current target. Returns the resolved damage.</summary>
        public DamageResult FireNormalShot(TerrainType terrain)
        {
            _nextShotTime = Time.time + stats.fireInterval;
            if (stats.magazineSize > 0) AmmoInMagazine--;

            var req = new DamageRequest
            {
                attacker = stats,
                defender = Target.Stats,
                powerRatio = stats.normalAttackRatio,
                terrain = terrain,
                defenderInCover = Target.InCover,
                attackerBuff = AttackBuff,
                defenderDamageTaken = Target.DamageTakenMultiplier,
                cannotMiss = false
            };

            DamageResult result = DamageCalculator.Compute(req);
            Target.ApplyDamage(result);
            return result;
        }

        public Vector3 MuzzlePosition => Muzzle != null
            ? Muzzle.position
            : transform.position + Vector3.up * 1.1f + Forward * 0.35f;

        // ---------------------------------------------------------------------------------
        // Health
        // ---------------------------------------------------------------------------------

        public void ApplyDamage(DamageResult result)
        {
            if (IsDead) return;
            if (result.missed || result.blockedByCover)
            {
                DamageTakenEvent?.Invoke(this, result);
                return;
            }

            CurrentHp -= result.amount;
            DamageTakenEvent?.Invoke(this, result);

            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                Kill();
            }
        }

        public float Heal(float amount)
        {
            if (IsDead) return 0f;
            float before = CurrentHp;
            CurrentHp = Mathf.Min(stats.maxHp, CurrentHp + amount);
            return CurrentHp - before;
        }

        private void Kill()
        {
            State = UnitState.Dead;
            InCover = false;
            if (CoverSystem.Instance != null) CoverSystem.Instance.Release(this);
            Cover = null;
            Died?.Invoke(this);
        }

        // ---------------------------------------------------------------------------------
        // Buffs
        // ---------------------------------------------------------------------------------

        public void ApplyAttackBuff(float multiplier, float duration)
        {
            _attackBuff = multiplier;
            _attackBuffUntil = Time.time + duration;
        }

        public void ApplyDamageReduction(float multiplier, float duration)
        {
            _damageTaken = multiplier;
            _damageTakenUntil = Time.time + duration;
        }
    }
}
