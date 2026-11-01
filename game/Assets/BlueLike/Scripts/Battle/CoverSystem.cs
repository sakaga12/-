using System.Collections.Generic;
using UnityEngine;

namespace BlueLike.Battle
{
    /// <summary>
    /// Registry of cover points on the stage. Units ask it for the best cover in their lane
    /// rather than pathfinding, which keeps the whole thing cheap enough for mobile.
    /// </summary>
    public class CoverSystem : MonoBehaviour
    {
        public static CoverSystem Instance { get; private set; }

        private readonly List<CoverPoint> _points = new List<CoverPoint>();

        private void Awake()
        {
            Instance = this;
        }

        public void Register(CoverPoint point)
        {
            if (point != null && !_points.Contains(point)) _points.Add(point);
        }

        /// <summary>
        /// Best free cover for this unit: same lane, ahead of the unit in its facing
        /// direction, and as close to its preferred firing distance as possible.
        /// </summary>
        public CoverPoint FindCover(BattleUnit unit, float targetX)
        {
            CoverPoint best = null;
            float bestScore = float.MaxValue;
            float preferred = unit.Stats.PreferredDistance;

            for (int i = 0; i < _points.Count; i++)
            {
                CoverPoint p = _points[i];
                if (p == null) continue;
                if (p.lane != unit.Lane) continue;
                if (!p.IsFree && p.occupant != unit) continue;

                // Never pick cover that sits past the enemy. Projecting onto the unit's
                // advance direction makes this one comparison instead of a sign dance:
                // positive means the target is still ahead of the cover.
                float aheadOfCover = (targetX - p.StandPosition.x) * unit.Forward.x;
                if (aheadOfCover < 0f) continue;

                float distToTarget = Mathf.Abs(p.StandPosition.x - targetX);
                float score = Mathf.Abs(distToTarget - preferred);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            return best;
        }

        public void Release(BattleUnit unit)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                if (_points[i] != null && _points[i].occupant == unit) _points[i].occupant = null;
            }
        }
    }
}
