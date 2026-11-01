using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;
using BlueLike.Presentation;

namespace BlueLike.Skills
{
    /// <summary>
    /// Executes an EX skill: freezes the owner, runs a short cinematic beat, applies the
    /// effect, then hands control back to the auto-battle brain.
    /// Add new skill behaviour by extending <see cref="ExSkillKind"/> and the switch in
    /// <see cref="Resolve"/> - everything else here is reusable staging.
    /// </summary>
    public class ExSkillRunner : MonoBehaviour
    {
        [Header("Cinematic")]
        [Tooltip("Seconds of zoomed-in framing before the skill lands.")]
        public float WindupSeconds = 0.45f;
        [Tooltip("Seconds of framing held after the skill lands.")]
        public float FollowThroughSeconds = 0.35f;
        [Tooltip("Time scale during the cinematic. 1 disables the slow-motion beat.")]
        [Range(0.05f, 1f)] public float CinematicTimeScale = 0.35f;

        public bool IsBusy { get; private set; }

        public void Run(SkillCard card)
        {
            if (IsBusy) return;
            StartCoroutine(Sequence(card));
        }

        private IEnumerator Sequence(SkillCard card)
        {
            IsBusy = true;
            BattleUnit caster = card.Owner;
            BattleManager mgr = BattleManager.Instance;

            caster.State = UnitState.CastingEx;
            caster.InCover = false;

            // Lock the target at cast time so it cannot change mid-animation.
            BattleUnit target = caster.Target;
            if (target == null || target.IsDead) target = mgr.FindTargetFor(caster);

            mgr.SetPhase(BattlePhase.ExCinematic);
            BattleCameraRig rig = BattleCameraRig.Instance;
            if (rig != null) rig.BeginExFocus(caster, target);

            float previousScale = Time.timeScale;
            Time.timeScale = CinematicTimeScale;

            yield return new WaitForSecondsRealtime(WindupSeconds);

            ResolveEffect(caster, target, mgr);

            yield return new WaitForSecondsRealtime(FollowThroughSeconds);

            Time.timeScale = previousScale;
            if (rig != null) rig.EndExFocus();
            mgr.SetPhase(BattlePhase.Fighting);

            if (!caster.IsDead) caster.State = UnitState.Idle;
            IsBusy = false;
        }

        /// <summary>
        /// Fires a unit's skill effect with no cost, no card and no cinematic. This is how
        /// Specials work: their skill goes off on its own timer in the background.
        /// </summary>
        public void RunAuto(BattleUnit caster)
        {
            BattleManager mgr = BattleManager.Instance;
            if (mgr == null || caster == null || caster.IsDead) return;

            BattleUnit target = caster.Target;
            if (target == null || target.IsDead) target = mgr.FindTargetFor(caster);
            ResolveEffect(caster, target, mgr);
        }

        private void ResolveEffect(BattleUnit caster, BattleUnit target, BattleManager mgr)
        {
            UnitStats s = caster.Stats;

            switch (s.exKind)
            {
                case ExSkillKind.SingleBurst:
                    if (target != null) HitOne(caster, target, s.exPowerRatio, mgr);
                    break;

                case ExSkillKind.MultiHit:
                {
                    if (target == null) break;
                    int hits = Mathf.Max(1, s.exHitCount);
                    float per = s.exPowerRatio / hits;
                    for (int i = 0; i < hits; i++) HitOne(caster, target, per, mgr);
                    break;
                }

                case ExSkillKind.AreaBlast:
                {
                    Vector3 centre = target != null ? target.transform.position : caster.transform.position;
                    float radius = s.exRadius > 0f ? s.exRadius : 4f;
                    List<BattleUnit> pool = mgr.LivingEnemiesOf(caster.Faction);
                    int hits = Mathf.Max(1, s.exHitCount);
                    float per = s.exPowerRatio / hits;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (Vector3.Distance(pool[i].transform.position, centre) > radius) continue;
                        for (int h = 0; h < hits; h++) HitOne(caster, pool[i], per, mgr);
                    }
                    break;
                }

                case ExSkillKind.Heal:
                {
                    List<BattleUnit> allies = mgr.LivingAlliesOf(caster.Faction);
                    BattleUnit worst = null;
                    float lowest = float.MaxValue;
                    for (int i = 0; i < allies.Count; i++)
                    {
                        if (allies[i].HpFraction < lowest)
                        {
                            lowest = allies[i].HpFraction;
                            worst = allies[i];
                        }
                    }
                    if (worst != null)
                    {
                        float healed = DamageCalculator.ComputeHeal(s, s.exPowerRatio);
                        worst.Heal(healed);
                        FloatingTextLayer.Spawn(worst.transform.position + Vector3.up * 1.8f,
                            "+" + Mathf.RoundToInt(healed), new Color(0.45f, 1f, 0.6f), 1.1f);
                    }
                    break;
                }

                case ExSkillKind.AttackBuff:
                {
                    List<BattleUnit> allies = mgr.LivingAlliesOf(caster.Faction);
                    float mult = 1f + s.exPowerRatio * 0.1f;
                    for (int i = 0; i < allies.Count; i++) allies[i].ApplyAttackBuff(mult, 12f);
                    FloatingTextLayer.Spawn(caster.transform.position + Vector3.up * 2f,
                        "ATK UP", new Color(1f, 0.8f, 0.3f), 1.2f);
                    break;
                }

                case ExSkillKind.Shield:
                {
                    List<BattleUnit> allies = mgr.LivingAlliesOf(caster.Faction);
                    float mult = Mathf.Clamp(1f - s.exPowerRatio * 0.08f, 0.3f, 0.95f);
                    for (int i = 0; i < allies.Count; i++) allies[i].ApplyDamageReduction(mult, 12f);
                    FloatingTextLayer.Spawn(caster.transform.position + Vector3.up * 2f,
                        "GUARD", new Color(0.55f, 0.8f, 1f), 1.2f);
                    break;
                }
            }
        }

        private static void HitOne(BattleUnit caster, BattleUnit target, float ratio, BattleManager mgr)
        {
            if (target == null || target.IsDead) return;

            var req = new DamageRequest
            {
                attacker = caster.Stats,
                defender = target.Stats,
                powerRatio = ratio,
                terrain = mgr.Terrain,
                // EX skills punch through cover and never miss - that is the point of paying cost.
                defenderInCover = false,
                attackerBuff = caster.AttackBuff,
                defenderDamageTaken = target.DamageTakenMultiplier,
                cannotMiss = true
            };

            DamageResult result = DamageCalculator.Compute(req);
            target.ApplyDamage(result);
            mgr.ReportHit(caster, target, result, true);
        }
    }
}
