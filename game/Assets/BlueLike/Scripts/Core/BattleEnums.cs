namespace BlueLike.Core
{
    /// <summary>Attack element. Drives the type-advantage table in <see cref="DamageTable"/>.</summary>
    public enum AttackType { Explosive = 0, Piercing = 1, Mystic = 2, Sonic = 3 }

    /// <summary>Defensive armour class. Paired against <see cref="AttackType"/>.</summary>
    public enum ArmorType { Light = 0, Heavy = 1, Special = 2, Elastic = 3 }

    /// <summary>Stage biome. Each unit has an adaptation grade per biome.</summary>
    public enum TerrainType { Street = 0, Outdoor = 1, Indoor = 2 }

    /// <summary>Terrain adaptation grade. S is best, D is a hard penalty.</summary>
    public enum TerrainGrade { S = 0, A = 1, B = 2, C = 3, D = 4 }

    /// <summary>
    /// Strikers stand on the field and own an EX skill card.
    /// Specials support from the back line and fire automatically.
    /// </summary>
    public enum UnitRole { Striker = 0, Special = 1 }

    public enum Faction { Player = 0, Enemy = 1 }

    /// <summary>Range band. Decides how far a unit advances before it stops to shoot.</summary>
    public enum RangeBand { Melee = 0, Short = 1, Mid = 2, Long = 3 }

    public enum UnitState
    {
        Idle,
        Advancing,
        TakingCover,
        Firing,
        Reloading,
        CastingEx,
        Dead
    }

    public enum BattlePhase
    {
        NotStarted,
        Deploying,
        Fighting,
        ExCinematic,
        Victory,
        Defeat
    }
}
