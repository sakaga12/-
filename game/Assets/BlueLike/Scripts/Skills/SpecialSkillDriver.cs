using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;

namespace BlueLike.Skills
{
    /// <summary>
    /// Specials do not get a card. Their skill fires on its own timer, free of cost - that
    /// asymmetry is what makes squad composition interesting rather than just "six damage".
    /// Attach to any unit whose role is <see cref="UnitRole.Special"/>.
    /// </summary>
    [RequireComponent(typeof(BattleUnit))]
    public class SpecialSkillDriver : MonoBehaviour
    {
        [Tooltip("Seconds between automatic casts.")]
        public float Interval = 14f;
        [Tooltip("Delay before the very first cast, so the squad is not front-loaded.")]
        public float InitialDelay = 6f;

        private BattleUnit _unit;
        private float _nextCastTime;
        private bool _armed;

        private void Awake()
        {
            _unit = GetComponent<BattleUnit>();
        }

        private void Update()
        {
            BattleManager mgr = BattleManager.Instance;
            if (mgr == null || mgr.Phase != BattlePhase.Fighting) return;
            if (_unit.IsDead) return;

            if (!_armed)
            {
                _nextCastTime = Time.time + InitialDelay;
                _armed = true;
                return;
            }

            if (Time.time < _nextCastTime) return;
            _nextCastTime = Time.time + Interval;

            var runner = mgr.GetComponent<ExSkillRunner>();
            if (runner != null) runner.RunAuto(_unit);
        }
    }
}
