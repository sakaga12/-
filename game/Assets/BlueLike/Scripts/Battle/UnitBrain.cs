using UnityEngine;
using BlueLike.Core;

namespace BlueLike.Battle
{
    /// <summary>
    /// The auto-battle loop for one unit: pick a target, close to firing range, grab cover,
    /// shoot, reload. Specials never advance - they sit at the back and support.
    /// Attach alongside <see cref="BattleUnit"/>; the bootstrap does this for you.
    /// </summary>
    [RequireComponent(typeof(BattleUnit))]
    public class UnitBrain : MonoBehaviour
    {
        private BattleUnit _unit;
        private float _retargetTimer;
        private const float RetargetInterval = 0.4f;

        private void Awake()
        {
            _unit = GetComponent<BattleUnit>();
        }

        private void Update()
        {
            if (_unit.IsDead) return;
            BattleManager mgr = BattleManager.Instance;
            if (mgr == null || mgr.Phase != BattlePhase.Fighting) return;

            // An EX cast owns the unit until it finishes.
            if (_unit.State == UnitState.CastingEx) return;

            float dt = Time.deltaTime;

            _retargetTimer -= dt;
            if (_retargetTimer <= 0f || _unit.Target == null || _unit.Target.IsDead)
            {
                _unit.Target = mgr.FindTargetFor(_unit);
                _retargetTimer = RetargetInterval;
            }

            if (_unit.Target == null)
            {
                _unit.State = UnitState.Idle;
                return;
            }

            if (_unit.State == UnitState.Reloading)
            {
                _unit.TickReload();
                return;
            }

            if (_unit.NeedsReload)
            {
                _unit.BeginReload();
                return;
            }

            float distance = Mathf.Abs(_unit.Target.transform.position.x - _unit.transform.position.x);
            float preferred = _unit.Stats.PreferredDistance;

            // Specials hold the back line and shoot from wherever they stand.
            if (_unit.Stats.role == UnitRole.Special)
            {
                _unit.InCover = true;
                Shoot(mgr);
                return;
            }

            if (distance > preferred)
            {
                AdvanceOrTakeCover(mgr, dt);
                return;
            }

            // In range. Settle into cover if there is any, then open fire.
            if (_unit.Cover != null && _unit.Cover.occupant == _unit)
            {
                _unit.InCover = true;
            }
            else
            {
                CoverPoint cover = CoverSystem.Instance != null
                    ? CoverSystem.Instance.FindCover(_unit, _unit.Target.transform.position.x)
                    : null;
                if (cover != null && Mathf.Abs(cover.StandPosition.x - _unit.transform.position.x) < 2.5f)
                {
                    ClaimCover(cover);
                }
                else
                {
                    _unit.InCover = false;
                }
            }

            _unit.FaceEnemySide();
            Shoot(mgr);
        }

        private void AdvanceOrTakeCover(BattleManager mgr, float dt)
        {
            CoverPoint cover = CoverSystem.Instance != null
                ? CoverSystem.Instance.FindCover(_unit, _unit.Target.transform.position.x)
                : null;

            float targetX = _unit.Target.transform.position.x;
            float standOffX = targetX - _unit.Forward.x * _unit.Stats.PreferredDistance;

            float destinationX = cover != null ? cover.StandPosition.x : standOffX;

            // Clamp to the stand-off line so a unit never walks through its own target to
            // reach a piece of cover on the far side of it.
            if (_unit.Forward.x > 0f) destinationX = Mathf.Min(destinationX, standOffX);
            else destinationX = Mathf.Max(destinationX, standOffX);

            _unit.State = UnitState.Advancing;
            _unit.InCover = false;

            bool arrived = _unit.MoveTowardX(destinationX, dt);

            // Only claim the cover if the unit actually ended up standing at it. The clamp
            // above can park a unit short of its chosen cover, and claiming it anyway would
            // hand out the cover damage reduction to someone stood in the open.
            if (arrived && cover != null &&
                Mathf.Abs(cover.StandPosition.x - _unit.transform.position.x) < 1.2f)
            {
                ClaimCover(cover);
            }
        }

        private void ClaimCover(CoverPoint cover)
        {
            if (_unit.Cover != null && _unit.Cover != cover) _unit.Cover.occupant = null;
            cover.occupant = _unit;
            _unit.Cover = cover;
            _unit.InCover = true;
            _unit.State = UnitState.TakingCover;
        }

        private void Shoot(BattleManager mgr)
        {
            if (!_unit.CanFire) return;
            if (_unit.Target == null || _unit.Target.IsDead) return;

            _unit.State = UnitState.Firing;
            DamageResult result = _unit.FireNormalShot(mgr.Terrain);
            mgr.ReportHit(_unit, _unit.Target, result, false);
        }
    }
}
