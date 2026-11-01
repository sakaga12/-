using UnityEngine;

namespace BlueLike.Battle
{
    /// <summary>
    /// One sandbag or crate a single unit can hide behind.
    ///
    /// Cover is the reason the three lanes play differently. A unit behind cover rolls
    /// against its terrain adaptation grade to soak incoming ranged fire entirely, which is
    /// why a squad with bad terrain adaptation melts even at equal stats.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        [Tooltip("Which of the three lanes this cover sits in.")]
        public int lane;

        [HideInInspector] public BattleUnit occupant;

        public bool IsFree => occupant == null || occupant.IsDead;

        /// <summary>Where a unit actually stands when using this cover.</summary>
        public Vector3 StandPosition => transform.position;
    }
}
